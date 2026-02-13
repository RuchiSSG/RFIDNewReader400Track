//using Microsoft.VisualBasic;
//using RFIDReaderPortal.Models;
//using System.Collections.Concurrent;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace RFIDReaderPortal.Services
//{
//    public class TcpListenerService : ITcpListenerService
//    {
//        private TcpListener _tcpListener;
//        private ConcurrentDictionary<string, RfidData> _receivedDataDict;
//        private readonly ConcurrentDictionary<string, DateTime> _lastProcessed = new();

//        private string[] _hexString;
//        private int _hexdataCount;
//        private readonly object _lock = new object();
//        private readonly object _hexLock = new object();
//        private volatile bool _raceStarted = false;
//        private string _accessToken;
//        private string _userid;
//        private string _recruitid;
//        private string _deviceId;
//        private string _location;
//        private string _eventName;
//        private string _eventId;
//        private string _sessionid;
//        private string _ipaddress;
//        private DateTime? _raceStartTime;
//        private DateTime _lastClearTime = DateTime.MinValue;
//        private readonly IApiService _apiService;
//        private readonly ILogger<TcpListenerService> _logger;

//        // Reduced window for better tag detection
//        //private readonly TimeSpan _duplicatePreventionWindow = TimeSpan.FromSeconds(2);
//        private readonly TimeSpan _duplicatePreventionWindow = TimeSpan.FromMilliseconds(300);
//        private readonly ConcurrentDictionary<string, bool> _allowedTags = new();
//        private readonly ConcurrentQueue<(string epc, DateTime time)> _epcQueue = new();
//        private readonly ConcurrentDictionary<string, DateTime> _lastSeenScan
//   = new();
//        public bool IsRunning { get; private set; }
//        private readonly ConcurrentDictionary<string, DateTime> _lastLapUpdate = new();
//        private const int MAX_DATA_COUNT = 3000;
//        private const int BUFFER_SIZE = 65536; // Increased to 16KB
//        private ConcurrentDictionary<string, RfidData> _storedRfidData
//    = new ConcurrentDictionary<string, RfidData>();
//        private List<RfidData> _snapshotData = new List<RfidData>();
//        TimeSpan minGap;
//        // Buffer to accumulate incomplete hex data
//        private StringBuilder _hexBuffer = new StringBuilder();

//        public TcpListenerService(IApiService apiService, ILogger<TcpListenerService> logger, int port = 9090)
//        {
//            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            _tcpListener = new TcpListener(IPAddress.Any, port);
//            _receivedDataDict = new ConcurrentDictionary<string, RfidData>();
//            _hexString = new string[MAX_DATA_COUNT];
//            _hexdataCount = 0;
//        }

//        public void SetParameters(string accessToken, string userid, string recruitid,
//                                  string deviceId, string location, string eventName, string eventId,
//                                  string ipaddress, string sessionid)
//        {
//            _accessToken = accessToken;
//            _userid = userid;
//            _recruitid = recruitid;
//            _eventId = eventId;
//            _deviceId = deviceId;
//            _location = location;
//            _eventName = eventName;
//            _sessionid = sessionid;
//            _ipaddress = ipaddress;
//            //IsRunning = false;
//        }

//        public void Start()
//        {
//            if (!IsRunning)
//            {
//                _tcpListener.Start();
//                IsRunning = true;
//                StartEpcProcessor();
//                _logger.LogInformation("TCP Listener started on port 9090");
//                Task.Run(async () => await ListenAsync());
//            }
//        }
//        public void Stop()
//        {
//            if (!IsRunning)
//                return;

//            IsRunning = false;
//            _tcpListener.Stop();

//            // ✅ SNAPSHOT FROM MAIN DICTIONARY
//            _snapshotData = _receivedDataDict.Values
//                .Where(d => d.LapTimes.Count > 0)
//                .Select(d => new RfidData
//                {
//                    TagId = d.TagId,
//                    Timestamp = d.Timestamp,
//                    LapTimes = new List<DateTime>(d.LapTimes),
//                    IsCompleted = d.IsCompleted
//                })
//                .OrderBy(d => d.TagId)
//                .ToList();

//            _logger.LogInformation(
//                $"TCP Listener stopped. Snapshot count = {_snapshotData.Count}");
//        }

//        //public void Stop()
//        //{
//        //    if (IsRunning)
//        //    {
//        //        IsRunning = false;
//        //        _tcpListener.Stop();
//        //        _logger.LogInformation("TCP Listener stopped");
//        //    }
//        //}
//        public void StartRace()
//        {
//            // 🔥 ENSURE LISTENER IS RUNNING
//            if (!IsRunning)
//            {
//                _tcpListener.Start();
//                IsRunning = true;
//                StartEpcProcessor();
//                Task.Run(async () => await ListenAsync());
//                _logger.LogInformation("TCP Listener restarted for new race");
//            }

//            _raceStarted = true;
//            _raceStartTime = DateTime.Now;

//            _receivedDataDict.Clear();
//            _storedRfidData.Clear();
//            _snapshotData.Clear();
//            _lastProcessed.Clear();

//            while (_epcQueue.TryDequeue(out _)) { }

//            lock (_hexBuffer)
//            {
//                _hexBuffer.Clear();
//            }

//            _lastClearTime = DateTime.Now;

//            _logger.LogInformation("Race officially STARTED - old data cleared");
//        }




//        public void StopRace()
//        {
//            _raceStarted = false;
//            _logger.LogInformation("Race STOPPED");
//        }

//        private async Task ListenAsync()
//        {
//            while (IsRunning)
//            {
//                try
//                {
//                    var client = await _tcpListener.AcceptTcpClientAsync();
//                    _logger.LogInformation($"Client connected from {client.Client.RemoteEndPoint}");
//                    _ = Task.Run(() => ProcessClientAsync(client));
//                }
//                catch (Exception ex)
//                {
//                    if (IsRunning)
//                    {
//                        _logger.LogError(ex, "Error accepting client connection");
//                    }
//                }
//            }
//        }
//        private async Task ProcessClientAsync(TcpClient client)
//        {
//            _logger.LogInformation($"MATCHED EPC FROM BUFFER 11:{DateTime.UtcNow.Ticks}");
//            // _logger.LogInformation($"Client connected from {client.Client.RemoteEndPoint}");
//            using (client)
//            using (var stream = client.GetStream())
//            {
//                var buffer = new byte[BUFFER_SIZE];
//                var clientBuffer = new StringBuilder();

//                try
//                {
//                    _logger.LogInformation($"MATCHED EPC FROM BUFFER2:{DateTime.UtcNow.Ticks}");
//                    while (client.Connected && IsRunning)
//                    {
//                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
//                        Console.WriteLine("=================================");
//                        Console.WriteLine($"BYTES READ: {bytesRead}");

//                        if (bytesRead <= 0)
//                        {
//                            _logger.LogInformation("Client disconnected");
//                            break;
//                        }
//                        _logger.LogInformation($"MATCHED EPC FROM BUFFER3:{DateTime.UtcNow.Ticks}");
//                        // Convert bytes to hex
//                        string hexData = BytesToHex(buffer, bytesRead);
//                        _logger.LogInformation("RAW HEX FROM READER:");
//                        _logger.LogInformation(hexData);
//                        _logger.LogInformation("=================================");
//                        _logger.LogDebug($"Received {bytesRead} bytes: {hexData.Substring(0, Math.Min(100, hexData.Length))}...");

//                        // Store hex for debugging
//                        lock (_hexLock)
//                        {
//                            if (_hexdataCount < MAX_DATA_COUNT)
//                                _hexString[_hexdataCount++] = hexData;
//                        }

//                        // ---------------- MULTI-ANTENNA SAFE ----------------
//                        lock (_hexBuffer)
//                        {
//                            _hexBuffer.Append(hexData);

//                            // Keep buffer reasonable
//                            if (_hexBuffer.Length > 65536) // 64 KB max
//                                _hexBuffer.Remove(0, _hexBuffer.Length - 65536);
//                            _logger.LogInformation("----- CURRENT HEX BUFFER -----");
//                            _logger.LogInformation(_hexBuffer.ToString());
//                            ProcessHexBuffer(_hexBuffer);
//                        }
//                    }
//                }
//                catch (IOException ioEx)
//                {
//                    _logger.LogWarning($"IO Exception in client processing: {ioEx.Message}");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error processing client data");
//                }
//            }
//        }

//        //private async Task ProcessClientAsync(TcpClient client)
//        //{
//        //    using (client)
//        //    using (var stream = client.GetStream())
//        //    {
//        //        var buffer = new byte[BUFFER_SIZE];
//        //        var clientBuffer = new StringBuilder();

//        //        try
//        //        {
//        //            while (client.Connected && IsRunning)
//        //            {
//        //                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

//        //                if (bytesRead <= 0)
//        //                {
//        //                    _logger.LogInformation("Client disconnected");
//        //                    break;
//        //                }

//        //                // Convert bytes to hex
//        //                string hexData = BytesToHex(buffer, bytesRead);

//        //                _logger.LogDebug($"Received {bytesRead} bytes: {hexData.Substring(0, Math.Min(100, hexData.Length))}...");

//        //                // Store hex for debugging
//        //                lock (_hexLock)
//        //                {
//        //                    if (_hexdataCount < MAX_DATA_COUNT)
//        //                        _hexString[_hexdataCount++] = hexData;
//        //                }

//        //                // ---------------- MULTI-ANTENNA SAFE ----------------
//        //                lock (_hexBuffer)
//        //                {
//        //                    _hexBuffer.Append(hexData);

//        //                    // Keep buffer reasonable
//        //                    if (_hexBuffer.Length > 65536) // 64 KB max
//        //                        _hexBuffer.Remove(0, _hexBuffer.Length - 65536);

//        //                    ProcessHexBuffer(_hexBuffer);
//        //                }
//        //            }
//        //        }
//        //        catch (IOException ioEx)
//        //        {
//        //            _logger.LogWarning($"IO Exception in client processing: {ioEx.Message}");
//        //        }
//        //        catch (Exception ex)
//        //        {
//        //            _logger.LogError(ex, "Error processing client data");
//        //        }
//        //    }
//        //}
//        private static string BytesToHex(byte[] buffer, int length)
//        {
//            char[] c = new char[length * 2];
//            int b;

//            for (int i = 0; i < length; i++)
//            {
//                b = buffer[i] >> 4;
//                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));

//                b = buffer[i] & 0xF;
//                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
//            }

//            return new string(c);
//        }

//        private void ProcessHexBuffer(StringBuilder buffer)
//        {
//            string data = buffer.ToString();

//            var matches = Regex.Matches(
//                data,
//                @"E2801170000002[0-9A-F]{10}",
//                RegexOptions.IgnoreCase
//            );

//            foreach (Match m in matches)
//            {
//                var epc = m.Value.ToUpperInvariant();
//                //_logger.LogInformation($"MATCHED EPC FROM BUFFER: {epc}");
//                long datetime = DateTime.Now.Ticks;
//                _epcQueue.Enqueue((epc, DateTime.Now));
//                // _logger.LogInformation($"MATCHED EPC FROM BUFFER: {epc},{DateTime.UtcNow}");
//                _logger.LogInformation($"MATCHED EPC FROM BUFFER 4444:{epc},{datetime}");
//                _logger.LogInformation($"MATCHED EPC FROM BUFFER: ");
//            }

//            // 🔥 REMOVE processed part completely
//            if (matches.Count > 0)
//            {
//                int lastIndex = matches[matches.Count - 1].Index +
//                                matches[matches.Count - 1].Length;

//                buffer.Remove(0, lastIndex);
//            }
//        }
//        //private void ProcessHexBuffer(StringBuilder buffer)
//        //{
//        //    string data = buffer.ToString();

//        //    var matches = Regex.Matches(
//        //        data,
//        //        @"E2801170000002[0-9A-F]{10}",
//        //        RegexOptions.IgnoreCase
//        //    );

//        //    foreach (Match m in matches)
//        //    {
//        //        var epc = m.Value.ToUpperInvariant();
//        //        _epcQueue.Enqueue((epc, DateTime.Now));
//        //    }

//        //    // 🔥 REMOVE processed part completely
//        //    if (matches.Count > 0)
//        //    {
//        //        int lastIndex = matches[matches.Count - 1].Index +
//        //                        matches[matches.Count - 1].Length;

//        //        buffer.Remove(0, lastIndex);
//        //    }
//        //}

//        private void StartEpcProcessor()
//        {
//            Task.Run(async () =>
//            {
//                while (IsRunning)
//                {
//                    if (_epcQueue.TryDequeue(out var item))
//                    {
//                        Console.WriteLine($"RAW EPC FROM READER: {item.epc}");
//                        ProcessTag(item.epc, item.time);
//                    }
//                    else
//                    {
//                        await Task.Delay(1);
//                    }
//                }
//            });
//        }




//        //first scan time code 
//        //private void ProcessTag(string epc, DateTime timestamp)
//        //{
//        //    // ❌ IGNORE tag if not mapped to chest/group
//        //    if (!_allowedTags.ContainsKey(epc))
//        //    {
//        //        _logger.LogDebug($"Ignored unknown tag: {epc}");
//        //        return;
//        //    }
//        //    var rfidData = _receivedDataDict.GetOrAdd(epc, _ => new RfidData
//        //    {
//        //        TagId = epc,
//        //        Timestamp = DateTime.MinValue,   // 🔥 FIX
//        //        LapTimes = new List<DateTime>(),
//        //        IsCompleted = false
//        //    });

//        //    //if (rfidData.IsCompleted)
//        //    //    return;

//        //    // ✅ Duplicate prevention AFTER first valid lap
//        //    if (rfidData.LapTimes.Count > 0)
//        //    {
//        //        var gap = timestamp - rfidData.Timestamp;
//        //        if (gap < _duplicatePreventionWindow)
//        //            return;
//        //    }

//        //    rfidData.Timestamp = timestamp;
//        //    bool shouldStore = false;

//        //    // 🟢 SINGLE LAP EVENTS
//        //    if (_eventName == "100 Meter Running"
//        //     || _eventName == "500 meter Running"
//        //     || _eventName == "800 Meter Running")
//        //    {
//        //        if (rfidData.LapTimes.Count == 0)
//        //        {
//        //            // First scan (Start)
//        //            rfidData.LapTimes.Add(timestamp);
//        //            rfidData.Timestamp = timestamp;
//        //            shouldStore = true;
//        //        }
//        //        else
//        //        {
//        //            // Keep updating with latest scan (Finish keeps updating)
//        //            rfidData.Timestamp = timestamp;
//        //            rfidData.LapTimes[0] = timestamp;  // 🔥 overwrite with latest time
//        //            shouldStore = true;
//        //        }

//        //        return;
//        //    }

//        //    // 🟢 MULTI LAP (1600m)
//        //    int maxLaps = _eventName == "1600 Meter Running" ? 2 : 1;
//        //    TimeSpan minGap = TimeSpan.FromSeconds(15);

//        //    if (rfidData.LapTimes.Count > 0)
//        //    {
//        //        var lastLap = rfidData.LapTimes.Last();
//        //        if (timestamp - lastLap < minGap)
//        //            return;
//        //    }

//        //    if (rfidData.LapTimes.Count < maxLaps)
//        //    {
//        //        rfidData.LapTimes.Add(timestamp);
//        //        shouldStore = true;

//        //        if (rfidData.LapTimes.Count == maxLaps)
//        //            rfidData.IsCompleted = true;
//        //    }

//        //    if (shouldStore)
//        //    {
//        //        lock (_storedRfidData)
//        //        {
//        //            _storedRfidData.Add(new RfidData
//        //            {
//        //                TagId = rfidData.TagId,
//        //                Timestamp = rfidData.Timestamp,
//        //                LapTimes = new List<DateTime>(rfidData.LapTimes),
//        //                IsCompleted = rfidData.IsCompleted
//        //            });
//        //        }
//        //    }
//        //}

//        //Latest updated time get from this 
//        private void ProcessTag(string epc, DateTime timestamp)
//        {

//            Console.WriteLine($"TAG READ: {epc} at {timestamp:HH:mm:ss.fff}");
//            if (!_allowedTags.ContainsKey(epc))
//            {
//                _logger.LogDebug($"Ignored unknown tag: {epc}");
//                return;
//            }

//            var rfidData = _receivedDataDict.GetOrAdd(epc, _ => new RfidData
//            {
//                TagId = epc,
//                Timestamp = DateTime.MinValue,
//                LapTimes = new List<DateTime>(),
//                IsCompleted = false
//            });

//            // 🔥 Duplicate fast scan prevention
//            if (rfidData.Timestamp != DateTime.MinValue)
//            {
//                var gap = timestamp - rfidData.Timestamp;
//                if (gap < _duplicatePreventionWindow)
//                    return;
//            }

//            rfidData.Timestamp = timestamp;
//            bool shouldStore = false;

//            // ====================================================
//            // 🟢 SINGLE LAP EVENTS
//            // ====================================================
//            if (_eventName == "100 Meter Running")
//            {
//                if (rfidData.LapTimes.Count == 0)
//                    rfidData.LapTimes.Add(timestamp);
//                else
//                    rfidData.LapTimes[0] = timestamp;  // 🔥 always update

//                shouldStore = true;
//            }

//            // ====================================================
//            // 🟢 MULTI LAP (1600 Meter Running)
//            // ====================================================
//            else
//            {
//                int maxLaps = 1;
//                TimeSpan minLapGap = TimeSpan.FromSeconds(20);

//                if (_eventName == "800 Meter Running")
//                    maxLaps = 3;

//                else if (_eventName == "1600 Meter Running")
//                    maxLaps = 5;

//                if (rfidData.IsCompleted)
//                    return;

//                // First scan
//                if (rfidData.LapTimes.Count == 0)
//                {
//                    rfidData.LapTimes.Add(timestamp);
//                    shouldStore = true;
//                }
//                else
//                {
//                    DateTime lastLapTime = rfidData.LapTimes.Last();
//                    var gap = timestamp - lastLapTime;

//                    if (gap >= minLapGap)
//                    {
//                        if (rfidData.LapTimes.Count < maxLaps)
//                        {
//                            rfidData.LapTimes.Add(timestamp);
//                            shouldStore = true;
//                        }

//                        if (rfidData.LapTimes.Count == maxLaps)
//                            rfidData.IsCompleted = true;
//                    }
//                    else
//                    {
//                        // Live update current lap
//                        rfidData.LapTimes[rfidData.LapTimes.Count - 1] = timestamp;
//                        shouldStore = true;
//                    }
//                }
//            }



//            // ====================================================
//            // 🟢 STORE SNAPSHOT FOR LIVE VIEW
//            // ====================================================
//            if (shouldStore)
//            {
//                _storedRfidData.AddOrUpdate(
//                    rfidData.TagId,
//                    new RfidData
//                    {
//                        TagId = rfidData.TagId,
//                        Timestamp = rfidData.Timestamp,
//                        LapTimes = new List<DateTime>(rfidData.LapTimes),
//                        IsCompleted = rfidData.IsCompleted
//                    },
//                    (key, oldValue) =>
//                    {
//                        oldValue.Timestamp = rfidData.Timestamp;
//                        oldValue.LapTimes = new List<DateTime>(rfidData.LapTimes);
//                        oldValue.IsCompleted = rfidData.IsCompleted;
//                        return oldValue;
//                    });
//            }


//        }

//        public void SetAllowedTags(IEnumerable<string> tagIds)
//        {
//            if (_raceStarted)
//            {
//                _logger.LogWarning("Group change ignored. Race already started.");
//                return;
//            }

//            _allowedTags.Clear();
//            foreach (var tag in tagIds)
//                _allowedTags[tag.ToUpperInvariant()] = true;

//            _receivedDataDict.Clear();
//            _lastProcessed.Clear();
//        }



//        public async Task<List<RFIDChestNoMappingDto>> InsertStoredRfidDataAsync()
//        {
//            if (_snapshotData == null || _snapshotData.Count == 0)
//            {
//                _logger.LogWarning("No snapshot RFID data to insert");
//                return new List<RFIDChestNoMappingDto>();
//            }

//            var dataToInsert = _snapshotData
//                .Where(d => d.LapTimes.Count > 0)
//                .ToList();

//            if (dataToInsert.Count == 0)
//            {
//                _logger.LogWarning("Snapshot exists but no valid lap data");
//                return new List<RFIDChestNoMappingDto>();
//            }

//            var result = await _apiService.PostRFIDRunningLogAsync(
//                _accessToken,
//                _userid,
//                _recruitid,
//                _deviceId,
//                _location,
//                _eventName,
//                _eventId,
//                dataToInsert,
//                _sessionid,
//                _ipaddress
//            );

//            return result ?? new List<RFIDChestNoMappingDto>();
//        }


//        public void ClearData()
//        {
//            _receivedDataDict.Clear();
//            _storedRfidData.Clear();   // ✅ ADD THIS
//            _snapshotData.Clear();
//            lock (_hexLock)
//            {
//                Array.Clear(_hexString, 0, _hexString.Length);
//                _hexdataCount = 0;
//            }
//            _lastClearTime = DateTime.Now;
//            _hexBuffer.Clear();
//            _logger.LogInformation("All RFID data cleared");
//        }
//        public RfidData[] GetReceivedData()
//        {
//            if (!IsRunning && _snapshotData != null)
//                return _snapshotData.OrderBy(d => d.TagId).ToArray();


//            return _receivedDataDict.Values
//                .Where(d => d.Timestamp > _lastClearTime)
//                .OrderBy(d => d.TagId)
//                .ToArray();
//        }

//        //public RfidData[] GetReceivedData()
//        //{
//        //    return _receivedDataDict.Values
//        //        .Where(d => d.Timestamp > _lastClearTime)
//        //        .OrderBy(d => d.TagId)
//        //        .ToArray();
//        //}

//        public string[] GetHexData()
//        {
//            lock (_hexLock)
//            {
//                return _hexString.Take(_hexdataCount).ToArray();
//            }
//        }
//    }
//}
using Microsoft.VisualBasic;
using RFIDReaderPortal.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace RFIDReaderPortal.Services
{
    public class TcpListenerService : ITcpListenerService
    {
        private TcpListener _tcpListener;
        private ConcurrentDictionary<string, RfidData> _receivedDataDict;
        private readonly ConcurrentDictionary<string, DateTime> _lastProcessed = new();

        private string[] _hexString;
        private int _hexdataCount;
        private readonly object _lock = new object();
        private readonly object _hexLock = new object();
        private volatile bool _raceStarted = false;
        private string _accessToken;
        private string _userid;
        private string _recruitid;
        private string _deviceId;
        private string _location;
        private string _eventName;
        private string _eventId;
        private string _sessionid;
        private string _ipaddress;
        private DateTime? _raceStartTime;
        private DateTime _lastClearTime = DateTime.MinValue;
        private readonly IApiService _apiService;
        private readonly ILogger<TcpListenerService> _logger;

        // Reduced window for better tag detection
        //private readonly TimeSpan _duplicatePreventionWindow = TimeSpan.FromSeconds(2);
        private readonly TimeSpan _duplicatePreventionWindow = TimeSpan.FromMilliseconds(300);
        private readonly ConcurrentDictionary<string, bool> _allowedTags = new();
        private readonly ConcurrentQueue<(string epc, DateTime time)> _epcQueue = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastSeenScan
   = new();
        public bool IsRunning { get; private set; }
        private readonly ConcurrentDictionary<string, DateTime> _lastLapUpdate = new();
        private const int MAX_DATA_COUNT = 3000;
        private const int BUFFER_SIZE = 65536; // Increased to 16KB
        private ConcurrentDictionary<string, RfidData> _storedRfidData
    = new ConcurrentDictionary<string, RfidData>();
        private List<RfidData> _snapshotData = new List<RfidData>();
        TimeSpan minGap;
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
            //IsRunning = false;
        }

        public void Start()
        {
            if (!IsRunning)
            {
                _tcpListener.Start();
                IsRunning = true;
                StartEpcProcessor();
                _logger.LogInformation("TCP Listener started on port 9090");
                Task.Run(async () => await ListenAsync());
            }
        }
        public void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            _tcpListener.Stop();

            // ✅ SNAPSHOT FROM MAIN DICTIONARY
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

            _logger.LogInformation(
                $"TCP Listener stopped. Snapshot count = {_snapshotData.Count}");
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
        public void StartRace()
        {
            // 🔥 ENSURE LISTENER IS RUNNING
            if (!IsRunning)
            {
                _tcpListener.Start();
                IsRunning = true;
                StartEpcProcessor();
                Task.Run(async () => await ListenAsync());
                _logger.LogInformation("TCP Listener restarted for new race");
            }

            _raceStarted = true;
            _raceStartTime = DateTime.Now;

            _receivedDataDict.Clear();
            _storedRfidData.Clear();
            _snapshotData.Clear();
            _lastProcessed.Clear();

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
                    }
                }
            }
        }
        private async Task ProcessClientAsync(TcpClient client)
        {
            _logger.LogInformation($"MATCHED EPC FROM BUFFER 11:{DateTime.UtcNow.Ticks}");
            // _logger.LogInformation($"Client connected from {client.Client.RemoteEndPoint}");
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[BUFFER_SIZE];
                var clientBuffer = new StringBuilder();

                try
                {
                    _logger.LogInformation($"MATCHED EPC FROM BUFFER2:{DateTime.UtcNow.Ticks}");
                    while (client.Connected && IsRunning)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        Console.WriteLine("=================================");
                        Console.WriteLine($"BYTES READ: {bytesRead}");

                        if (bytesRead <= 0)
                        {
                            _logger.LogInformation("Client disconnected");
                            break;
                        }
                        _logger.LogInformation($"MATCHED EPC FROM BUFFER3:{DateTime.UtcNow.Ticks}");
                        // Convert bytes to hex
                        string hexData = BytesToHex(buffer, bytesRead);
                        _logger.LogInformation("RAW HEX FROM READER:");
                        _logger.LogInformation(hexData);
                        _logger.LogInformation("=================================");
                        _logger.LogDebug($"Received {bytesRead} bytes: {hexData.Substring(0, Math.Min(100, hexData.Length))}...");

                        // Store hex for debugging
                        lock (_hexLock)
                        {
                            if (_hexdataCount < MAX_DATA_COUNT)
                                _hexString[_hexdataCount++] = hexData;
                        }

                        // ---------------- MULTI-ANTENNA SAFE ----------------
                        lock (_hexBuffer)
                        {
                            _hexBuffer.Append(hexData);

                            // Keep buffer reasonable
                            if (_hexBuffer.Length > 65536) // 64 KB max
                                _hexBuffer.Remove(0, _hexBuffer.Length - 65536);
                            _logger.LogInformation("----- CURRENT HEX BUFFER -----");
                            _logger.LogInformation(_hexBuffer.ToString());
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

        //private async Task ProcessClientAsync(TcpClient client)
        //{
        //    using (client)
        //    using (var stream = client.GetStream())
        //    {
        //        var buffer = new byte[BUFFER_SIZE];
        //        var clientBuffer = new StringBuilder();

        //        try
        //        {
        //            while (client.Connected && IsRunning)
        //            {
        //                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        //                if (bytesRead <= 0)
        //                {
        //                    _logger.LogInformation("Client disconnected");
        //                    break;
        //                }

        //                // Convert bytes to hex
        //                string hexData = BytesToHex(buffer, bytesRead);

        //                _logger.LogDebug($"Received {bytesRead} bytes: {hexData.Substring(0, Math.Min(100, hexData.Length))}...");

        //                // Store hex for debugging
        //                lock (_hexLock)
        //                {
        //                    if (_hexdataCount < MAX_DATA_COUNT)
        //                        _hexString[_hexdataCount++] = hexData;
        //                }

        //                // ---------------- MULTI-ANTENNA SAFE ----------------
        //                lock (_hexBuffer)
        //                {
        //                    _hexBuffer.Append(hexData);

        //                    // Keep buffer reasonable
        //                    if (_hexBuffer.Length > 65536) // 64 KB max
        //                        _hexBuffer.Remove(0, _hexBuffer.Length - 65536);

        //                    ProcessHexBuffer(_hexBuffer);
        //                }
        //            }
        //        }
        //        catch (IOException ioEx)
        //        {
        //            _logger.LogWarning($"IO Exception in client processing: {ioEx.Message}");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error processing client data");
        //        }
        //    }
        //}
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

        // 4️⃣ EXTRACT EPC FROM HEX BUFFER
        //-------------------------------------------------------------
        //Method: ProcessHexBuffer()
        private void ProcessHexBuffer(StringBuilder buffer)
        {
            string data = buffer.ToString();
            //- Uses Regex:
            //  @"E2801170000002[0-9A-F]{10}"
            var matches = Regex.Matches(
                data,
                @"E2801170000002[0-9A-F]{10}",
                RegexOptions.IgnoreCase
            );

            foreach (Match m in matches)
            {
                var epc = m.Value.ToUpperInvariant();
                //_logger.LogInformation($"MATCHED EPC FROM BUFFER: {epc}");
                long datetime = DateTime.Now.Ticks;
                _epcQueue.Enqueue((epc, DateTime.Now));
                // _logger.LogInformation($"MATCHED EPC FROM BUFFER: {epc},{DateTime.UtcNow}");
                _logger.LogInformation($"MATCHED EPC FROM BUFFER 4444:{epc},{datetime}");
                _logger.LogInformation($"MATCHED EPC FROM BUFFER: ");
            }

            // 🔥 REMOVE processed part completely
            if (matches.Count > 0)
            {
                int lastIndex = matches[matches.Count - 1].Index +
                                matches[matches.Count - 1].Length;

                buffer.Remove(0, lastIndex);
            }
        }
        //private void ProcessHexBuffer(StringBuilder buffer)
        //{
        //    string data = buffer.ToString();

        //    var matches = Regex.Matches(
        //        data,
        //        @"E2801170000002[0-9A-F]{10}",
        //        RegexOptions.IgnoreCase
        //    );

        //    foreach (Match m in matches)
        //    {
        //        var epc = m.Value.ToUpperInvariant();
        //        _epcQueue.Enqueue((epc, DateTime.Now));
        //    }

        //    // 🔥 REMOVE processed part completely
        //    if (matches.Count > 0)
        //    {
        //        int lastIndex = matches[matches.Count - 1].Index +
        //                        matches[matches.Count - 1].Length;

        //        buffer.Remove(0, lastIndex);
        //    }
        //}




        //5️⃣ EPC PROCESSING THREAD
        //-------------------------------------------------------------
        //Method: StartEpcProcessor()
        private void StartEpcProcessor()
        {
            Task.Run(async () =>
            {
                while (IsRunning)
                {
                    //- Dequeues EPC from _epcQueue.
                    if (_epcQueue.TryDequeue(out var item))
                    {
                        Console.WriteLine($"RAW EPC FROM READER: {item.epc}");
                        //- Calls ProcessTag(epc, timestamp).
                        ProcessTag(item.epc, item.time);
                    }
                    else
                    {
                        await Task.Delay(1);
                    }
                }
            });
        }




        //first scan time code 
        //private void ProcessTag(string epc, DateTime timestamp)
        //{
        //    // ❌ IGNORE tag if not mapped to chest/group
        //    if (!_allowedTags.ContainsKey(epc))
        //    {
        //        _logger.LogDebug($"Ignored unknown tag: {epc}");
        //        return;
        //    }
        //    var rfidData = _receivedDataDict.GetOrAdd(epc, _ => new RfidData
        //    {
        //        TagId = epc,
        //        Timestamp = DateTime.MinValue,   // 🔥 FIX
        //        LapTimes = new List<DateTime>(),
        //        IsCompleted = false
        //    });

        //    //if (rfidData.IsCompleted)
        //    //    return;

        //    // ✅ Duplicate prevention AFTER first valid lap
        //    if (rfidData.LapTimes.Count > 0)
        //    {
        //        var gap = timestamp - rfidData.Timestamp;
        //        if (gap < _duplicatePreventionWindow)
        //            return;
        //    }

        //    rfidData.Timestamp = timestamp;
        //    bool shouldStore = false;

        //    // 🟢 SINGLE LAP EVENTS
        //    if (_eventName == "100 Meter Running"
        //     || _eventName == "500 meter Running"
        //     || _eventName == "800 Meter Running")
        //    {
        //        if (rfidData.LapTimes.Count == 0)
        //        {
        //            // First scan (Start)
        //            rfidData.LapTimes.Add(timestamp);
        //            rfidData.Timestamp = timestamp;
        //            shouldStore = true;
        //        }
        //        else
        //        {
        //            // Keep updating with latest scan (Finish keeps updating)
        //            rfidData.Timestamp = timestamp;
        //            rfidData.LapTimes[0] = timestamp;  // 🔥 overwrite with latest time
        //            shouldStore = true;
        //        }

        //        return;
        //    }

        //    // 🟢 MULTI LAP (1600m)
        //    int maxLaps = _eventName == "1600 Meter Running" ? 2 : 1;
        //    TimeSpan minGap = TimeSpan.FromSeconds(15);

        //    if (rfidData.LapTimes.Count > 0)
        //    {
        //        var lastLap = rfidData.LapTimes.Last();
        //        if (timestamp - lastLap < minGap)
        //            return;
        //    }

        //    if (rfidData.LapTimes.Count < maxLaps)
        //    {
        //        rfidData.LapTimes.Add(timestamp);
        //        shouldStore = true;

        //        if (rfidData.LapTimes.Count == maxLaps)
        //            rfidData.IsCompleted = true;
        //    }

        //    if (shouldStore)
        //    {
        //        lock (_storedRfidData)
        //        {
        //            _storedRfidData.Add(new RfidData
        //            {
        //                TagId = rfidData.TagId,
        //                Timestamp = rfidData.Timestamp,
        //                LapTimes = new List<DateTime>(rfidData.LapTimes),
        //                IsCompleted = rfidData.IsCompleted
        //            });
        //        }
        //    }
        //}

        //Latest updated time get from this 
        //Latest updated time get from this 
        //6️⃣ TAG PROCESSING LOGIC
        //-------------------------------------------------------------
        //Method: ProcessTag()
        private void ProcessTag(string epc, DateTime timestamp)
        {

            Console.WriteLine($"TAG READ: {epc} at {timestamp:HH:mm:ss.fff}");
            // -Check if tag exists in _allowedTags.
            if (!_allowedTags.ContainsKey(epc))
            {
                _logger.LogDebug($"Ignored unknown tag: {epc}");
                return;
            }

            var rfidData = _receivedDataDict.GetOrAdd(epc, _ => new RfidData
            {
                TagId = epc,
                Timestamp = DateTime.MinValue,
                LapTimes = new List<DateTime>(),
                IsCompleted = false
            });

            // 🔥 Duplicate fast scan prevention
            if (rfidData.Timestamp != DateTime.MinValue)
            {
                var gap = timestamp - rfidData.Timestamp;
                if (gap < _duplicatePreventionWindow)
                    return;
            }

            rfidData.Timestamp = timestamp;
            bool shouldStore = false;

            // ====================================================
            // 🟢 SINGLE LAP EVENTS
            // ====================================================
            if (_eventName == "100 Meter Running")
            {
                if (rfidData.LapTimes.Count == 0)
                    rfidData.LapTimes.Add(timestamp);
                else
                    rfidData.LapTimes[0] = timestamp;  // 🔥 always update

                shouldStore = true;
            }

            // ====================================================
            // 🟢 MULTI LAP (1600 Meter Running)
            // ====================================================
            else
            {
                int maxLaps = 1;
                TimeSpan minLapGap = TimeSpan.FromSeconds(20);

                if (_eventName == "800 Meter Running")
                    maxLaps = 3;
                else if (_eventName == "1600 Meter Running")
                    maxLaps = 5;

                if (rfidData.LapTimes.Count == 0)
                {
                    rfidData.LapTimes.Add(timestamp);
                    shouldStore = true;
                }
                else
                {
                    DateTime lastLapTime = rfidData.LapTimes.Last();
                    var gap = timestamp - lastLapTime;

                    // ✅ If max laps already reached → ONLY update last lap
                    if (rfidData.LapTimes.Count >= maxLaps)
                    {
                        rfidData.LapTimes[maxLaps - 1] = timestamp;
                        rfidData.IsCompleted = true;
                        shouldStore = true;
                        return;
                    }

                    // ✅ Add new lap only if gap is valid
                    if (gap >= minLapGap)
                    {
                        rfidData.LapTimes.Add(timestamp);
                        shouldStore = true;

                        if (rfidData.LapTimes.Count == maxLaps)
                            rfidData.IsCompleted = true;
                    }
                    else
                    {
                        // Live update current lap
                        rfidData.LapTimes[rfidData.LapTimes.Count - 1] = timestamp;
                        shouldStore = true;
                    }
                }
            }




            // ====================================================
            // 🟢 STORE SNAPSHOT FOR LIVE VIEW
            // ====================================================
            if (shouldStore)
            {
                _storedRfidData.AddOrUpdate(
                    rfidData.TagId,
                    new RfidData
                    {
                        TagId = rfidData.TagId,
                        Timestamp = rfidData.Timestamp,
                        LapTimes = new List<DateTime>(rfidData.LapTimes),
                        IsCompleted = rfidData.IsCompleted
                    },
                    (key, oldValue) =>
                    {
                        oldValue.Timestamp = rfidData.Timestamp;
                        oldValue.LapTimes = new List<DateTime>(rfidData.LapTimes);
                        oldValue.IsCompleted = rfidData.IsCompleted;
                        return oldValue;
                    });
            }


        }

        //selected groups tag set here 
        public void SetAllowedTags(IEnumerable<string> tagIds)
        {
            if (_raceStarted)
            {
                _logger.LogWarning("Group change ignored. Race already started.");
                return;
            }

            _allowedTags.Clear();
            foreach (var tag in tagIds)
                _allowedTags[tag.ToUpperInvariant()] = true;

            _receivedDataDict.Clear();
            _lastProcessed.Clear();
        }


        // 8️⃣ SAVE DATA TO API
        //-------------------------------------------------------------
        //Method: InsertStoredRfidDataAsync()
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
                _ipaddress
            );

            return result ?? new List<RFIDChestNoMappingDto>();
        }

        //        9️⃣ CLEAR DATA
        //-------------------------------------------------------------
        //Method: ClearData()
        public void ClearData()
        {
            _receivedDataDict.Clear();
            _storedRfidData.Clear();   // ✅ ADD THIS
            _snapshotData.Clear();
            lock (_hexLock)
            {
                Array.Clear(_hexString, 0, _hexString.Length);
                _hexdataCount = 0;
            }
            _lastClearTime = DateTime.Now;
            _hexBuffer.Clear();
            _logger.LogInformation("All RFID data cleared");
        }

        // 🔟 LIVE DATA FETCH
        //-------------------------------------------------------------
        //Method: GetReceivedData()
        //- If race stopped → return snapshot
        //- If running → return current dictionary
        //- Ordered by TagId
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
