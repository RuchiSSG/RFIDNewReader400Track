using System.Net.Sockets;
using System.Net;
using System.Text;
using RFIDReaderPortal.Models;
using System.Collections.Concurrent;

namespace RFIDReaderPortal.Services
{
    public class TcpListenerService : ITcpListenerService
    {
        private TcpListener _tcpListener;
        private ConcurrentDictionary<string, RfidData> _receivedDataDict;
        private string[] _hexString;
        private int _hexdataCount;
        private readonly object _lock = new object();
        private readonly object _hexLock = new object();

        private string _accessToken;
        private string _userid;
        private string _recruitid;
        private string _deviceId;
        private string _location;
        private string _eventName;
        private string _eventId;
        private string _sessionid;
        private string _ipaddress;

        private DateTime _lastClearTime = DateTime.MinValue;
        private readonly IApiService _apiService;
        private readonly ILogger<TcpListenerService> _logger;

        // Reduced window for better tag detection
        private readonly TimeSpan _duplicatePreventionWindow = TimeSpan.FromSeconds(2);

        public bool IsRunning { get; private set; }

        private const int MAX_DATA_COUNT = 1000;
        private const int BUFFER_SIZE = 16384; // Increased to 16KB
        private readonly List<RfidData> _storedRfidData = new List<RfidData>();
        private List<RfidData> _snapshotData = new List<RfidData>();

        // Buffer to accumulate incomplete hex data
        private StringBuilder _hexBuffer = new StringBuilder();

        public TcpListenerService(IApiService apiService, ILogger<TcpListenerService> logger, int port = 9090)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _receivedDataDict = new ConcurrentDictionary<string, RfidData>();
            _hexString = new string[MAX_DATA_COUNT];
            _hexdataCount = 0;
        }

        public void SetParameters(string accessToken, string userid, string recruitid,
                                  string deviceId, string location, string eventName, string eventId,
                                  string ipaddress, string sessionid)
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
            IsRunning = false;
        }

        public void Start()
        {
            if (!IsRunning)
            {
                _tcpListener.Start();
                IsRunning = true;
                _logger.LogInformation("TCP Listener started on port 9090");
                Task.Run(async () => await ListenAsync());
            }
        }
        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                _tcpListener.Stop();

                lock (_storedRfidData)
                {
                    _snapshotData = _storedRfidData
                        .Select(d => new RfidData
                        {
                            TagId = d.TagId,
                            Timestamp = d.Timestamp,
                            LapTimes = new List<DateTime>(d.LapTimes)
                        })
                        .ToList();
                }

                _logger.LogInformation("TCP Listener stopped and snapshot taken.");
            }
        }

        //public void Stop()
        //{
        //    if (IsRunning)
        //    {
        //        IsRunning = false;
        //        _tcpListener.Stop();
        //        _logger.LogInformation("TCP Listener stopped");
        //    }
        //}

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
                var clientBuffer = new StringBuilder();

                try
                {
                    stream.ReadTimeout = 5000; // 5 second timeout

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
                        ProcessHexBuffer(hexData);

                        // Append to buffer in case of fragmented messages
                        // clientBuffer.Append(hexData);

                        // Process accumulated buffer
                        //ProcessHexBuffer(clientBuffer.ToString());

                        // Keep only last 1000 characters to prevent memory issues
                        //if (clientBuffer.Length > 1000)
                        //{
                        //    clientBuffer.Clear();
                        //}
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

        private void ProcessHexBuffer(string hexData)
        {
            if (string.IsNullOrWhiteSpace(hexData))
                return;

            // Extract all EPCs from the hex data
            var epcs = ExtractEpcs(hexData);

            if (epcs.Count == 0)
            {
                _logger.LogDebug("No EPCs found in data");
                return;
            }

            _logger.LogInformation($"Extracted {epcs.Count} unique EPCs");

            var now = DateTime.Now;

            foreach (var epc in epcs)
            {
                ProcessTag(epc, now);
            }
        }

        private void ProcessTag(string epc, DateTime timestamp)
        {
            bool shouldStore = false;
            RfidData rfidData;

            // Use concurrent dictionary for better thread safety
            //rfidData = _receivedDataDict.GetOrAdd(epc, key => new RfidData
            //{
            //    TagId = key,
            //    Timestamp = timestamp,
            //    LapTimes = new List<DateTime> { timestamp }
            //});
            bool exists = _receivedDataDict.TryGetValue(epc, out rfidData);

            if (!exists)
            {
                rfidData = new RfidData
                {
                    TagId = epc,
                    Timestamp = timestamp,
                    LapTimes = new List<DateTime> { timestamp }
                };
                _receivedDataDict[epc] = rfidData;
                shouldStore = true;
            }

            // Check if this is a new lap (not the initial creation)
            if (rfidData.Timestamp != timestamp)
            {
                var timeSinceLastScan = timestamp - rfidData.Timestamp;

                if (timeSinceLastScan > _duplicatePreventionWindow)
                {
                    if (_eventName == "100 Meter Running")
                    {
                        if (rfidData.LapTimes.Count == 0)
                        {
                            rfidData.Timestamp = timestamp;
                            rfidData.LapTimes.Add(timestamp);
                            shouldStore = true;

                            _logger.LogInformation(
                                $"Captured FIRST 100m time for tag {epc}: {timestamp:HH:mm:ss:fff}");
                        }
                        else
                        {
                            // 🔒 Ignore duplicate scans
                            _logger.LogDebug($"Ignored duplicate 100m scan for {epc}");
                        }
                    }

                    //if (_eventName == "100 Meter Running") // 100m
                    //{
                    //    // Always update for 100m (single lap)
                    //    rfidData.Timestamp = timestamp;
                    //    if (rfidData.LapTimes.Count == 0)
                    //        rfidData.LapTimes.Add(timestamp);
                    //    else
                    //        rfidData.LapTimes[0] = timestamp;

                    //    shouldStore = true;
                    //    _logger.LogInformation($"Updated 100m time for tag {epc}: {timestamp:HH:mm:ss:fff}");
                    //}
                    //else if (_eventName == "500 meter running") // 500m
                    //{
                    //    // Always update for 100m (single lap)
                    //    rfidData.Timestamp = timestamp;
                    //    if (rfidData.LapTimes.Count == 0)
                    //        rfidData.LapTimes.Add(timestamp);
                    //    else
                    //        rfidData.LapTimes[0] = timestamp;

                    //    shouldStore = true;
                    //    _logger.LogInformation($"Updated 500m time for tag {epc}: {timestamp:HH:mm:ss:fff}");
                    //}

                    if (_eventName == "500 meter Running")
                    {
                        if (rfidData.LapTimes.Count == 0)
                        {
                            rfidData.Timestamp = timestamp;
                            rfidData.LapTimes.Add(timestamp);
                            shouldStore = true;

                            _logger.LogInformation(
                                $"Captured FIRST 100m time for tag {epc}: {timestamp:HH:mm:ss:fff}");
                        }
                        else
                        {
                            // 🔒 Ignore duplicate scans
                            _logger.LogDebug($"Ignored duplicate 100m scan for {epc}");
                        }
                    }
                    else
                    {
                        int maxLaps = _eventName == "1600 Meter Running" ? 5 :
                            _eventName == "800 Meter Running" ? 3 : 1;
                        TimeSpan minGap = TimeSpan.FromSeconds(20);

                        DateTime lastLapTime = rfidData.LapTimes.Last();

                        if (timestamp - lastLapTime < minGap)
                        {
                            _logger.LogInformation(
                                $"Ignored early lap for {epc}. Gap: {(timestamp - lastLapTime).TotalSeconds} sec");
                            return;
                        }

                        if (rfidData.LapTimes.Count < maxLaps)
                        {
                            rfidData.Timestamp = timestamp;
                            rfidData.LapTimes.Add(timestamp);
                            shouldStore = true;

                            _logger.LogInformation(
                                $"Recorded lap {rfidData.LapTimes.Count} for {epc} at {timestamp:HH:mm:ss:fff}");
                        }
                        else
                        {
                            _logger.LogDebug($"Max laps reached for {epc}");
                        }
                    }

                    //else
                    //{
                    //    int maxLaps = _eventName == "1600 Meter Running" ? 5 :
                    //     _eventName == "800 Meter Running" ? 3 : 1;

                    //    if (rfidData.LapTimes.Count < maxLaps)
                    //    {
                    //        rfidData.Timestamp = timestamp;
                    //        rfidData.LapTimes.Add(timestamp);
                    //        shouldStore = true;
                    //        _logger.LogInformation($"Recorded lap {rfidData.LapTimes.Count} for tag {epc}: {timestamp:HH:mm:ss:fff}");
                    //    }
                    //    else
                    //    {
                    //        _logger.LogDebug($"Tag {epc} already has maximum laps ({maxLaps})");
                    //    }
                    //}
                }
                else
                {
                    _logger.LogDebug($"Ignoring duplicate scan for {epc} (within {_duplicatePreventionWindow.TotalSeconds}s window)");
                }
            }
            else
            {
                // This was the initial creation
                shouldStore = true;
                _logger.LogInformation($"New tag detected: {epc} at {timestamp:HH:mm:ss:fff}");
            }

            // Store for batch insertion
            if (shouldStore)
            {
                lock (_storedRfidData)
                {
                    _storedRfidData.Add(new RfidData
                    {
                        TagId = rfidData.TagId,
                        Timestamp = rfidData.Timestamp,
                        LapTimes = new List<DateTime>(rfidData.LapTimes)
                    });
                }
            }
        }
        static readonly string[] EPC_PREFIXES =
        {
             "E280117000000212AC",
             "E28011700000020A3F2"
         };
        private static List<string> ExtractEpcs(string hex)
        {
            const int EPC_LEN = 24;

            HashSet<string> result = new HashSet<string>();

            if (string.IsNullOrWhiteSpace(hex))
                return result.ToList();

            foreach (var prefix in EPC_PREFIXES)
            {
                int index = 0;

                while ((index = hex.IndexOf(prefix, index, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    if (index + EPC_LEN <= hex.Length)
                    {
                        string epc = hex.Substring(index, EPC_LEN);
                        result.Add(epc);
                    }
                    index += prefix.Length;
                }
            }

            return result.ToList();
        }

        //private static List<string> ExtractEpcs(string hex)
        //{
        //    const int EPC_LEN = 24;
        //    const string EPC_PREFIX = "E280117000000212AC";

        //    HashSet<string> result = new HashSet<string>();

        //    if (string.IsNullOrWhiteSpace(hex))
        //        return result.ToList();

        //    int index = 0;
        //    while ((index = hex.IndexOf(EPC_PREFIX, index, StringComparison.Ordinal)) != -1)
        //    {
        //        if (index + EPC_LEN <= hex.Length)
        //        {
        //            string epc = hex.Substring(index, EPC_LEN);
        //            result.Add(epc);
        //        }
        //        index += EPC_PREFIX.Length;
        //    }

        //    return result.ToList();
        //}

        //public async Task InsertStoredRfidDataAsync()
        //{
        //    List<RfidData> dataToInsert;

        //    lock (_storedRfidData)
        //    {
        //        if (_storedRfidData.Count == 0)
        //        {
        //            _logger.LogInformation("No stored RFID data to insert");
        //            return;
        //        }

        //        dataToInsert = new List<RfidData>(_storedRfidData);
        //        _storedRfidData.Clear();
        //    }

        //    _logger.LogInformation($"Inserting {dataToInsert.Count} RFID records");

        //    await _apiService.PostRFIDRunningLogAsync(
        //        _accessToken, _userid, _recruitid, _deviceId,
        //        _location, _eventName, _eventId, dataToInsert,
        //        _sessionid, _ipaddress
        //    );
        //}
        public async Task<List<ChestBarcodeDto>> InsertStoredRfidDataAsync()
        {
            List<RfidData> dataToInsert;

            lock (_storedRfidData)
            {
                if (_storedRfidData.Count == 0)
                {
                    _logger.LogInformation("No stored RFID data to insert");
                    return new List<ChestBarcodeDto>();
                }

                dataToInsert = new List<RfidData>(_storedRfidData);
                _storedRfidData.Clear();
            }

            _logger.LogInformation($"Inserting {dataToInsert.Count} RFID records");

            return await _apiService.PostRFIDRunningLogAsync(
                _accessToken, _userid, _recruitid, _deviceId,
                _location, _eventName, _eventId, dataToInsert,
                _sessionid, _ipaddress
            );
        }


        public void ClearData()
        {
            _receivedDataDict.Clear();
            lock (_hexLock)
            {
                Array.Clear(_hexString, 0, _hexString.Length);
                _hexdataCount = 0;
            }
            _lastClearTime = DateTime.Now;
            _hexBuffer.Clear();
            _logger.LogInformation("All RFID data cleared");
        }
        public RfidData[] GetReceivedData()
        {
            if (!IsRunning && _snapshotData != null)
                return _snapshotData.OrderBy(d => d.TagId).ToArray();


            return _receivedDataDict.Values
                .Where(d => d.Timestamp > _lastClearTime)
                .OrderBy(d => d.TagId)
                .ToArray();
        }

        //public RfidData[] GetReceivedData()
        //{
        //    return _receivedDataDict.Values
        //        .Where(d => d.Timestamp > _lastClearTime)
        //        .OrderBy(d => d.TagId)
        //        .ToArray();
        //}

        public string[] GetHexData()
        {
            lock (_hexLock)
            {
                return _hexString.Take(_hexdataCount).ToArray();
            }
        }
    }
}
