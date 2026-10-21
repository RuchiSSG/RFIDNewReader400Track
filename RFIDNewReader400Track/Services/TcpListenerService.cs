using RFIDReaderPortal.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RFIDReaderPortal.Services
{
    public class TcpListenerService : ITcpListenerService
    {
        private TcpListener _tcpListener;
        private ConcurrentDictionary<string, RfidData> _receivedDataDict;

        // per tag last seen time (reader noise filter)
        private readonly ConcurrentDictionary<string, DateTime> _lastSeenScan = new();

        private string[] _hexString;
        private int _hexdataCount;
        private readonly object _lock = new object();
        private readonly object _hexLock = new object();
        private volatile bool _raceStarted = false;
        private volatile bool _snapshotMode = false;   // true only after Stop() until next Start/StartRace/ClearData
        private string _accessToken;
        private string _userid;
        private string _recruitid;
        private string _deviceId;
        private string _location;
        private string _eventName;
        private string _eventId;
        private string _sessionid;
        private string _ipaddress;
        private string _categoryId;
        private string _categoryName;
        private readonly int _port;

        // Allowed tags (group / chest mapping)
        private readonly ConcurrentDictionary<string, bool> _allowedTags = new();
        private bool _isTagFilterActive = false;

        private DateTime _lastClearTime = DateTime.MinValue;
        private readonly IApiService _apiService;
        private readonly ILogger<TcpListenerService> _logger;

        public bool IsRunning { get; private set; }
        private readonly ConcurrentQueue<(string epc, DateTime time)> _epcQueue = new();

        private const int MAX_DATA_COUNT = 1000;
        private const int BUFFER_SIZE = 16384; // 16KB
        private List<RfidData> _snapshotData = new List<RfidData>();

        // EPC settings (dynamic - no hardcoded tag pattern)
        private const int EPC_LEN = 24;            // 12 bytes = 24 hex chars
        private const string EPC_PREFIX = "E2";    // Gen2 EPCs start with E2. Use "" to accept any 24 hex chars

        // Buffer to accumulate incomplete hex data
        private readonly StringBuilder _hexBuffer = new StringBuilder();

        // Throttle for "race not started" log
        private long _lastNotStartedLogTick = 0;

        public TcpListenerService(IApiService apiService, ILogger<TcpListenerService> logger, int port = 9090)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _port = port;
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _receivedDataDict = new ConcurrentDictionary<string, RfidData>();
            _hexString = new string[MAX_DATA_COUNT];
            _hexdataCount = 0;
        }

        public void SetParameters(string accessToken, string userid, string recruitid,
                                  string deviceId, string location, string eventName, string eventId,
                                  string ipaddress, string sessionid, string categoryId, string categoryName)
        {
            _accessToken = accessToken;
            _userid = userid;
            _recruitid = recruitid;
            _eventId = eventId;
            _deviceId = deviceId;
            _location = location;
            _eventName = eventName;
            _sessionid = sessionid;
            _ipaddress = ipaddress;
            _categoryId = categoryId;
            _categoryName = categoryName;
            // NOTE: IsRunning is intentionally NOT touched here. Setting it to false
            // while the listener was started killed the read loops.
        }

        public void SetAllowedTags(IEnumerable<string> tagIds, bool isActive = false)
        {
            _allowedTags.Clear();
            foreach (var tag in tagIds)
                _allowedTags[tag.ToUpperInvariant()] = true;

            _isTagFilterActive = isActive;

            _receivedDataDict.Clear();
            _lastSeenScan.Clear();
        }

        public void Start()
        {
            lock (_lock)
            {
                if (IsRunning)
                    return;

                _tcpListener.Start();
                IsRunning = true;
                _snapshotMode = false;
                StartEpcProcessor();
                _logger.LogInformation($"TCP Listener started on port {_port}");
                Task.Run(async () => await ListenAsync());
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!IsRunning)
                    return;

                IsRunning = false;
                _tcpListener.Stop();

                // SNAPSHOT FROM MAIN DICTIONARY
                _snapshotData = _receivedDataDict.Values
                    .Where(d => d.LapTimes.Count > 0)
                    .Select(d => new RfidData
                    {
                        TagId = d.TagId,
                        Timestamp = d.Timestamp,
                        LapTimes = new List<DateTime>(d.LapTimes),
                        IsCompleted = d.IsCompleted
                    })
                    .OrderBy(d => d.TagId)
                    .ToList();

                _snapshotMode = true;

                _logger.LogInformation(
                    $"TCP Listener stopped. Snapshot count = {_snapshotData.Count}");
            }
        }

        public void StartRace()
        {
            // Make sure the reader listener is actually running
            if (!IsRunning)
                Start();

            _raceStarted = true;
            _snapshotMode = false;

            // CLEAR EVERYTHING BEFORE START
            _receivedDataDict.Clear();
            _lastSeenScan.Clear();

            while (_epcQueue.TryDequeue(out _)) { }

            lock (_hexBuffer)
            {
                _hexBuffer.Clear();
            }

            _lastClearTime = DateTime.Now;

            _logger.LogInformation("Race officially STARTED - old data cleared");
        }

        public void StopRace()
        {
            _raceStarted = false;
            _logger.LogInformation("Race STOPPED");
        }

        private async Task ListenAsync()
        {
            while (IsRunning)
            {
                try
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    _logger.LogInformation($"Client connected from {client.Client.RemoteEndPoint}");
                    _ = Task.Run(() => ProcessClientAsync(client));
                }
                catch (Exception ex)
                {
                    if (IsRunning)
                    {
                        _logger.LogError(ex, "Error accepting client connection");
                        await Task.Delay(200); // avoid a tight error loop
                    }
                }
            }
        }

        private async Task ProcessClientAsync(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[BUFFER_SIZE];

                try
                {
                    while (client.Connected && IsRunning)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                        if (bytesRead <= 0)
                        {
                            _logger.LogInformation("Client disconnected");
                            break;
                        }

                        // Convert to HEX immediately
                        string hexData = BytesToHex(buffer, bytesRead);

                        _logger.LogDebug($"Received {bytesRead} bytes: {hexData.Substring(0, Math.Min(100, hexData.Length))}...");

                        // Store hex data for debugging
                        lock (_hexLock)
                        {
                            if (_hexdataCount < MAX_DATA_COUNT)
                            {
                                _hexString[_hexdataCount++] = hexData;
                            }
                        }

                        lock (_hexBuffer)
                        {
                            _hexBuffer.Append(hexData);
                            ProcessHexBuffer(_hexBuffer);
                        }
                    }
                }
                catch (IOException ioEx)
                {
                    _logger.LogWarning($"IO Exception in client processing: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing client data");
                }
            }
        }

        private static string BytesToHex(byte[] buffer, int length)
        {
            char[] c = new char[length * 2];
            int b;

            for (int i = 0; i < length; i++)
            {
                b = buffer[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));

                b = buffer[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }

            return new string(c);
        }

        private void ProcessHexBuffer(StringBuilder buffer)
        {
            if (buffer.Length < EPC_LEN)
                return;

            string hex = buffer.ToString();

            // Dynamic EPC detection (no hardcoded tag pattern)
            var epcs = FindEpcs(hex, out int consumedUpTo);

            var now = DateTime.Now;

            foreach (var epc in epcs)
            {
                _epcQueue.Enqueue((epc, now));
            }

            // Drop everything already consumed so old EPCs are never re-detected
            if (consumedUpTo > 0)
                buffer.Remove(0, consumedUpTo);

            // Keep only a short tail (max partial EPC = 22 hex chars) for EPCs split across packets
            int keep = EPC_LEN - 2;
            if (buffer.Length > keep)
                buffer.Remove(0, buffer.Length - keep);
        }

        /// <summary>
        /// Slides byte by byte (2 hex chars) over the hex string and returns every
        /// 24-char EPC that is valid hex and starts with EPC_PREFIX.
        /// consumedUpTo = index just after the last EPC found (0 if none).
        /// No tag-specific pattern is hardcoded.
        /// </summary>
        private static List<string> FindEpcs(string hex, out int consumedUpTo)
        {
            var result = new List<string>();
            consumedUpTo = 0;

            if (string.IsNullOrWhiteSpace(hex) || hex.Length < EPC_LEN)
                return result;

            hex = hex.ToUpperInvariant();

            int i = 0;
            while (i <= hex.Length - EPC_LEN)
            {
                string candidate = hex.Substring(i, EPC_LEN);

                if ((EPC_PREFIX.Length == 0 || candidate.StartsWith(EPC_PREFIX, StringComparison.Ordinal))
                    && IsHex(candidate))
                {
                    result.Add(candidate);
                    i += EPC_LEN;       // jump past this EPC
                    consumedUpTo = i;
                }
                else
                {
                    i += 2;             // move one byte
                }
            }

            return result;
        }

        private static bool IsHex(string s)
        {
            foreach (char c in s)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }

        private void StartEpcProcessor()
        {
            int workers = Environment.ProcessorCount >= 4 ? 4 : 2;

            for (int i = 0; i < workers; i++)
            {
                Task.Run(async () =>
                {
                    while (IsRunning)
                    {
                        if (_epcQueue.TryDequeue(out var item))
                        {
                            ProcessTag(item.epc, item.time);
                        }
                        else
                        {
                            await Task.Delay(1);
                        }
                    }
                });
            }
        }

        private void ProcessTag(string epc, DateTime timestamp)
        {
            if (!_raceStarted)
            {
                // Log at most once every 5 seconds so the console shows why nothing is recorded
                long nowTick = Environment.TickCount64;
                if (nowTick - _lastNotStartedLogTick > 5000)
                {
                    _lastNotStartedLogTick = nowTick;
                    _logger.LogInformation($"Tag {epc} seen but race is NOT started - ignored (StartRace not called)");
                }
                return;
            }

            // Ignore tags that are not in the allowed group / chest mapping
            if (_isTagFilterActive && !_allowedTags.IsEmpty && !_allowedTags.ContainsKey(epc))
            {
                _logger.LogDebug($"Ignored unknown tag: {epc}");
                return;
            }

            // HARD reader noise filter (same tag rapid hits)
            var last = _lastSeenScan.GetOrAdd(epc, DateTime.MinValue);
            if (timestamp - last < TimeSpan.FromMilliseconds(800))
                return;

            _lastSeenScan[epc] = timestamp;

            var data = _receivedDataDict.GetOrAdd(epc, _ => new RfidData
            {
                TagId = epc,
                LapTimes = new List<DateTime>(),
                IsCompleted = false
            });

            lock (data)
            {
                int maxLaps =
                    _eventName == "100 Meter Running" ? 1 :
                    _eventName == "800 Meter Running" ? 3 :
                    _eventName == "1600 Meter Running" ? 5 : 1;

                // Already finished -> ignore
                if (data.IsCompleted)
                    return;

                // Max laps reached -> ignore
                if (data.LapTimes.Count >= maxLaps)
                    return;

                // MIN 15 SEC GAP BETWEEN LAPS
                if (data.LapTimes.Count > 0)
                {
                    var lastLapTime = data.LapTimes.Last();

                    if (timestamp - lastLapTime < TimeSpan.FromSeconds(15))
                    {
                        _logger.LogDebug(
                            $"IGNORED | {epc} | Early lap scan ({(timestamp - lastLapTime).TotalSeconds:F1}s)");
                        return;
                    }
                }

                // FIRST SCAN DECIDES START TIME
                if (data.LapTimes.Count == 0)
                    data.Timestamp = timestamp;

                // LOCK LAP
                data.LapTimes.Add(timestamp);

                // Completion
                if (data.LapTimes.Count == maxLaps)
                    data.IsCompleted = true;

                _logger.LogInformation(
                    $"LOCKED | {_eventName} | {epc} | Lap {data.LapTimes.Count}/{maxLaps} | {timestamp:HH:mm:ss:fff}");
            }
        }

        public async Task<List<RFIDChestNoMappingDto>> InsertStoredRfidDataAsync()
        {
            if (_snapshotData == null || _snapshotData.Count == 0)
            {
                _logger.LogWarning("No snapshot RFID data to insert");
                return new List<RFIDChestNoMappingDto>();
            }

            var dataToInsert = _snapshotData
                .Where(d => d.LapTimes.Count > 0)
                .ToList();

            if (dataToInsert.Count == 0)
            {
                _logger.LogWarning("Snapshot exists but no valid lap data");
                return new List<RFIDChestNoMappingDto>();
            }

            var result = await _apiService.PostRFIDRunningLogAsync(
                _accessToken,
                _userid,
                _recruitid,
                _deviceId,
                _location,
                _eventName,
                _eventId,
                dataToInsert,
                _sessionid,
                _ipaddress,
                _categoryId,
                _categoryName
            );

            return result ?? new List<RFIDChestNoMappingDto>();
        }

        public void ClearData()
        {
            _receivedDataDict.Clear();
            _lastSeenScan.Clear();
            lock (_hexLock)
            {
                Array.Clear(_hexString, 0, _hexString.Length);
                _hexdataCount = 0;
            }
            _lastClearTime = DateTime.Now;
            lock (_hexBuffer)
            {
                _hexBuffer.Clear();
            }
            _snapshotMode = false;   // live view again (snapshot itself is kept for InsertStoredRfidDataAsync)
            _logger.LogInformation("All RFID data cleared");
        }

        public RfidData[] GetReceivedData()
        {
            if (_snapshotMode && _snapshotData != null)
                return _snapshotData.OrderBy(d => d.TagId).ToArray();

            return _receivedDataDict.Values
                .Where(d => d.Timestamp > _lastClearTime)
                .OrderBy(d => d.TagId)
                .ToArray();
        }

        public string[] GetHexData()
        {
            lock (_hexLock)
            {
                return _hexString.Take(_hexdataCount).ToArray();
            }
        }
    }
}