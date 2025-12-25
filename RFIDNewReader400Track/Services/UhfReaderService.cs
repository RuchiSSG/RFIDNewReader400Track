using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RFIDReaderPortal.Models;
using UHFReaderModule;

namespace RFIDReaderPortal.Services
{
    public class UhfRfidService
    {
        private Reader _reader;
        private byte _comAddr = 0xFF;
        private CancellationTokenSource _scanCts;
        private Task _scanTask;

        private readonly object _lock = new();
        private readonly Dictionary<string, RfidData> _tagData = new();

        public bool IsConnected { get; private set; }
        public bool IsScanning { get; private set; }

        /* ================= CONNECT ================= */

        public bool Connect(string ip, int port)
        {
            Console.WriteLine($"🔌 Connecting to {ip}:{port}");

            _reader = new Reader();
            int result = _reader.OpenByTcp(ip, port, ref _comAddr);

            if (result != 0)
            {
                Console.WriteLine($"❌ OpenByTcp failed: {result} - {GetErrorDescription(result)}");
                return false;
            }

            IsConnected = true;
            Console.WriteLine($"✅ Connected. Addr=0x{_comAddr:X2}");

            // Get reader info
            ShowReaderInfo();

            // Automatically start scanning
            StartScanning();

            return true;
        }

        private void ShowReaderInfo()
        {
            try
            {
                byte addr = _comAddr;
                byte[] versionInfo = new byte[2];
                byte readerType = 0;
                byte trType = 0;
                byte dmaxfre = 0;
                byte dminfre = 0;
                byte powerDbm = 0;
                byte scanTime = 0;
                byte ant = 0;
                byte beepEn = 0;
                byte outputRep = 0;
                byte checkAnt = 0;

                int result = _reader.GetReaderInformation(
                    ref addr,
                    versionInfo,
                    ref readerType,
                    ref trType,
                    ref dmaxfre,
                    ref dminfre,
                    ref powerDbm,
                    ref scanTime,
                    ref ant,
                    ref beepEn,
                    ref outputRep,
                    ref checkAnt
                );

                if (result == 0)
                {
                    Console.WriteLine($"📋 Reader Info:");
                    Console.WriteLine($"   Version: {versionInfo[0]}.{versionInfo[1]}");
                    Console.WriteLine($"   Type: {readerType}");
                    Console.WriteLine($"   Power: {powerDbm} dBm");
                    Console.WriteLine($"   Antenna: {ant}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not get reader info: {ex.Message}");
            }
        }

        /* ================= START SCANNING (CONTINUOUS) ================= */

        public bool StartScanning()
        {
            if (!IsConnected)
            {
                Console.WriteLine("❌ Cannot scan - not connected");
                return false;
            }

            if (IsScanning)
            {
                Console.WriteLine("⚠️ Already scanning");
                return true;
            }

            IsScanning = true;
            _scanCts = new CancellationTokenSource();
            _scanTask = Task.Run(() => ContinuousScan(_scanCts.Token));

            Console.WriteLine("✅ Scanning started");
            return true;
        }

        private async Task ContinuousScan(CancellationToken ct)
        {
            Console.WriteLine("🔄 Starting continuous scan loop...");
            int cycleCount = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    cycleCount++;
                    Console.WriteLine($"\n🔄 Scan Cycle #{cycleCount} at {DateTime.Now:HH:mm:ss.fff}");

                    // Step 1: Clear buffer before each scan cycle
                    int clearResult = _reader.ClearBuffer_G2(ref _comAddr);
                    Console.WriteLine($"   Clear Buffer Result: {clearResult}");

                    // Step 2: Start inventory scan
                    byte qValue = 4;
                    byte session = 0;
                    byte maskMem = 0;
                    byte[] maskAdr = new byte[2] { 0, 0 };
                    byte maskLen = 0;
                    byte[] maskData = new byte[0];
                    byte maskFlag = 0;
                    byte adrTID = 0;
                    byte lenTID = 0;
                    byte tidFlag = 0;
                    byte target = 0;
                    byte inAnt = 0;      // 0 = all antennas
                    byte scanTime = 20;  // 2 seconds
                    byte fastFlag = 1;

                    int bufferCount = 0;
                    int tagNum = 0;

                    Console.WriteLine($"   Starting InventoryBuffer_G2 (scanTime={scanTime})...");

                    int inventoryResult = _reader.InventoryBuffer_G2(
                        ref _comAddr,
                        qValue,
                        session,
                        maskMem,
                        maskAdr,
                        maskLen,
                        maskData,
                        maskFlag,
                        adrTID,
                        lenTID,
                        tidFlag,
                        target,
                        inAnt,
                        scanTime,
                        fastFlag,
                        ref bufferCount,
                        ref tagNum
                    );

                    Console.WriteLine($"   Inventory Result: {inventoryResult} ({GetErrorDescription(inventoryResult)})");
                    Console.WriteLine($"   Buffer Count: {bufferCount}, Tag Num: {tagNum}");

                    // Step 3: Wait for scan to complete
                    int waitTime = scanTime * 100 + 300;
                    Console.WriteLine($"   Waiting {waitTime}ms for scan completion...");
                    await Task.Delay(waitTime, ct);

                    // Step 4: Read buffer if inventory was successful or timeout (0, 1, or 2)
                    if (inventoryResult == 0 || inventoryResult == 1 || inventoryResult == 2)
                    {
                        Console.WriteLine($"   📖 Attempting to read buffer...");
                        ReadBuffer();

                        // Also try real-time inventory as alternative
                        Console.WriteLine($"   📖 Also trying Inventory_G2 (real-time)...");
                        TryRealTimeInventory();
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠️ Inventory error: {inventoryResult} - {GetErrorDescription(inventoryResult)}");
                    }

                    // Show current tag count
                    Console.WriteLine($"   📊 Total unique tags in memory: {GetTagCount()}");

                    // Delay before next cycle
                    await Task.Delay(500, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Scan error: {ex.Message}");
                    Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                    await Task.Delay(1000, ct);
                }
            }

            Console.WriteLine("🛑 Scan loop stopped");
        }

        /* ================= READ BUFFER ================= */

        private void ReadBuffer()
        {
            try
            {
                byte[] epcBuffer = new byte[5000];
                int totalLen = 0;
                int cardNum = 0;

                int result = _reader.ReadBuffer_G2(
                    ref _comAddr,
                    ref totalLen,
                    ref cardNum,
                    epcBuffer
                );

                Console.WriteLine($"      ReadBuffer_G2 Result: {result}, Cards: {cardNum}, TotalLen: {totalLen}");

                if (result == 0 && cardNum > 0 && totalLen > 0)
                {
                    Console.WriteLine($"      📦 Found {cardNum} tags, {totalLen} bytes");
                    Console.WriteLine($"      Raw buffer (first {Math.Min(100, totalLen)} bytes): {BitConverter.ToString(epcBuffer, 0, Math.Min(100, totalLen))}");
                    ParseBufferTags(epcBuffer, cardNum, totalLen);
                }
                else if (result == 0 && cardNum == 0)
                {
                    Console.WriteLine($"      ℹ️ Buffer is empty (no tags detected)");
                }
                else if (result != 0)
                {
                    Console.WriteLine($"      ⚠️ ReadBuffer failed: {result} - {GetErrorDescription(result)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ ReadBuffer error: {ex.Message}");
                Console.WriteLine($"      Stack trace: {ex.StackTrace}");
            }
        }

        /* ================= REAL TIME INVENTORY ================= */

        private void TryRealTimeInventory()
        {
            try
            {
                byte qValue = 4;
                byte session = 0;
                byte maskMem = 0;
                byte[] maskAdr = new byte[2] { 0, 0 };
                byte maskLen = 0;
                byte[] maskData = new byte[0];
                byte maskFlag = 0;
                byte adrTID = 0;
                byte lenTID = 0;
                byte tidFlag = 0;
                byte target = 0;
                byte inAnt = 0;
                byte scanTime = 10;
                byte fastFlag = 1;

                byte[] epcList = new byte[5000];
                byte ant = 0;
                int totalLen = 0;
                int cardNum = 0;

                int result = _reader.Inventory_G2(
                    ref _comAddr,
                    qValue,
                    session,
                    maskMem,
                    maskAdr,
                    maskLen,
                    maskData,
                    maskFlag,
                    adrTID,
                    lenTID,
                    tidFlag,
                    target,
                    inAnt,
                    scanTime,
                    fastFlag,
                    epcList,
                    ref ant,
                    ref totalLen,
                    ref cardNum
                );

                Console.WriteLine($"      Inventory_G2 Result: {result}, Cards: {cardNum}, TotalLen: {totalLen}, Ant: {ant}");

                if (result == 0 && cardNum > 0 && totalLen > 0)
                {
                    Console.WriteLine($"      📦 Real-time found {cardNum} tags");
                    Console.WriteLine($"      Raw data (first {Math.Min(100, totalLen)} bytes): {BitConverter.ToString(epcList, 0, Math.Min(100, totalLen))}");
                    ParseRealTimeInventory(epcList, cardNum, totalLen, ant);
                }
                else if (result == 0 && cardNum == 0)
                {
                    Console.WriteLine($"      ℹ️ No tags found in real-time scan");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ RealTime Inventory error: {ex.Message}");
                Console.WriteLine($"      Stack trace: {ex.StackTrace}");
            }
        }

        /* ================= PARSE REAL-TIME INVENTORY ================= */

        private void ParseRealTimeInventory(byte[] buffer, int tagCount, int totalLen, byte antenna)
        {
            Console.WriteLine($"      📋 Parsing real-time inventory: {tagCount} tags, {totalLen} bytes, antenna: {antenna}");

            int index = 0;
            for (int i = 0; i < tagCount && index < totalLen; i++)
            {
                try
                {
                    if (index >= totalLen) break;

                    // Read EPC length
                    byte epcLen = buffer[index++];
                    Console.WriteLine($"         Tag {i + 1}: EPCLen={epcLen}");

                    if (index + epcLen > totalLen)
                    {
                        Console.WriteLine($"         ⚠️ Not enough data (need {epcLen}, have {totalLen - index})");
                        break;
                    }

                    // Read EPC
                    string epc = BitConverter.ToString(buffer, index, epcLen).Replace("-", "");
                    index += epcLen;

                    Console.WriteLine($"         🏷️ Tag {i + 1}: {epc}");
                    SaveTag(epc, 0, antenna); // RSSI unknown in this mode
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ⚠️ Parse error at index {index}: {ex.Message}");
                    break;
                }
            }
        }

        /* ================= PARSE BUFFER TAGS ================= */

        private void ParseBufferTags(byte[] buffer, int tagCount, int totalLen)
        {
            Console.WriteLine($"      📋 Parsing buffer: {tagCount} tags, {totalLen} bytes");

            int index = 0;

            for (int i = 0; i < tagCount && index < totalLen; i++)
            {
                try
                {
                    Console.WriteLine($"         Processing tag {i + 1} at index {index}");

                    if (index >= totalLen) break;

                    // Read antenna
                    byte ant = buffer[index++];
                    Console.WriteLine($"         Antenna: {ant}");

                    if (index >= totalLen) break;

                    // Read EPC length
                    byte epcLen = buffer[index++];
                    Console.WriteLine($"         EPC Length: {epcLen}");

                    if (index + epcLen > totalLen)
                    {
                        Console.WriteLine($"         ⚠️ Not enough data for EPC (need {epcLen}, have {totalLen - index})");
                        break;
                    }

                    // Read EPC
                    string epc = BitConverter.ToString(buffer, index, epcLen).Replace("-", "");
                    index += epcLen;
                    Console.WriteLine($"         EPC: {epc}");

                    // Check if there's RSSI data
                    byte rssi = 0;
                    if (index < totalLen)
                    {
                        rssi = buffer[index++];
                        Console.WriteLine($"         RSSI: {rssi}");
                    }

                    // Check if there's count data (2 bytes)
                    int count = 0;
                    if (index + 1 < totalLen)
                    {
                        count = (buffer[index] << 8) | buffer[index + 1];
                        index += 2;
                        Console.WriteLine($"         Count: {count}");
                    }

                    Console.WriteLine($"         📍 Saving: EPC={epc}, RSSI={rssi}, Ant={ant}, Count={count}");
                    SaveTag(epc, rssi, ant);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ⚠️ Parse error at index {index}: {ex.Message}");
                    Console.WriteLine($"      Stack trace: {ex.StackTrace}");
                    break;
                }
            }
        }

        private void SaveTag(string tagId, byte rssi, byte antenna)
        {
            lock (_lock)
            {
                if (!_tagData.ContainsKey(tagId))
                {
                    _tagData[tagId] = new RfidData
                    {
                        TagId = tagId,
                        Timestamp = DateTime.Now,
                        LapTimes = new List<DateTime> { DateTime.Now },
                        Rssi = rssi,
                        Antenna = antenna
                    };

                    Console.WriteLine($"🆕 ✅ NEW TAG SAVED TO MEMORY: {tagId} (RSSI: {rssi}, Ant: {antenna})");
                }
                else
                {
                    var tag = _tagData[tagId];
                    tag.LapTimes.Add(DateTime.Now);
                    tag.Rssi = rssi;
                    tag.Timestamp = DateTime.Now;
                    tag.Antenna = antenna;

                    Console.WriteLine($"🔄 ✅ LAP RECORDED: {tagId} (Total Laps: {tag.LapTimes.Count}, RSSI: {rssi})");
                }
            }
        }

        /* ================= STOP / DISCONNECT ================= */

        public void StopScanning()
        {
            if (!IsScanning) return;

            Console.WriteLine("🛑 Stopping scan...");
            IsScanning = false;

            _scanCts?.Cancel();
            _scanTask?.Wait(2000);

            if (IsConnected)
            {
                _reader.StopImmediately(ref _comAddr);
            }

            Console.WriteLine("✅ Scan stopped");
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            try
            {
                StopScanning();
                Thread.Sleep(200);
                _reader.CloseByTcp();
            }
            finally
            {
                IsConnected = false;
                _reader = null;
                _scanCts?.Dispose();
                Console.WriteLine("🔌 Disconnected");
            }
        }

        /* ================= DATA ACCESS ================= */

        public RfidData[] GetAllTags()
        {
            lock (_lock)
            {
                var tags = new List<RfidData>(_tagData.Values).ToArray();
                Console.WriteLine($"📤 GetAllTags called: Returning {tags.Length} tags");

                if (tags.Length > 0)
                {
                    Console.WriteLine($"   Tags in memory:");
                    foreach (var tag in tags)
                    {
                        Console.WriteLine($"   - {tag.TagId}: {tag.LapTimes.Count} laps, last seen {tag.Timestamp:HH:mm:ss.fff}");
                    }
                }

                return tags;
            }
        }

        public RfidData GetTag(string tagId)
        {
            lock (_lock)
            {
                return _tagData.TryGetValue(tagId, out var tag) ? tag : null;
            }
        }

        public int GetTagCount()
        {
            lock (_lock)
            {
                return _tagData.Count;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _tagData.Clear();
                Console.WriteLine("🗑️ Tag data cleared");
            }

            if (IsConnected)
            {
                _reader.ClearBuffer_G2(ref _comAddr);
            }
        }

        /* ================= MANUAL SINGLE SCAN (FOR TESTING) ================= */

        public string TestSingleScan()
        {
            if (!IsConnected)
            {
                return "❌ Reader not connected";
            }

            try
            {
                Console.WriteLine("\n🧪 MANUAL TEST SCAN STARTING...");

                // Clear buffer first
                _reader.ClearBuffer_G2(ref _comAddr);

                // Perform single inventory
                byte qValue = 4;
                byte session = 0;
                byte maskMem = 0;
                byte[] maskAdr = new byte[2] { 0, 0 };
                byte maskLen = 0;
                byte[] maskData = new byte[0];
                byte maskFlag = 0;
                byte adrTID = 0;
                byte lenTID = 0;
                byte tidFlag = 0;
                byte target = 0;
                byte inAnt = 0;
                byte scanTime = 30; // 3 seconds
                byte fastFlag = 1;

                byte[] epcList = new byte[5000];
                byte ant = 0;
                int totalLen = 0;
                int cardNum = 0;

                Console.WriteLine("   Executing Inventory_G2...");
                int result = _reader.Inventory_G2(
                    ref _comAddr,
                    qValue,
                    session,
                    maskMem,
                    maskAdr,
                    maskLen,
                    maskData,
                    maskFlag,
                    adrTID,
                    lenTID,
                    tidFlag,
                    target,
                    inAnt,
                    scanTime,
                    fastFlag,
                    epcList,
                    ref ant,
                    ref totalLen,
                    ref cardNum
                );

                Console.WriteLine($"   Result: {result} - {GetErrorDescription(result)}");
                Console.WriteLine($"   Cards: {cardNum}, TotalLen: {totalLen}, Antenna: {ant}");

                if (totalLen > 0)
                {
                    Console.WriteLine($"   Raw data: {BitConverter.ToString(epcList, 0, Math.Min(200, totalLen))}");
                }

                if (cardNum > 0)
                {
                    ParseRealTimeInventory(epcList, cardNum, totalLen, ant);
                    return $"✅ Found {cardNum} tags! Check console for details.";
                }
                else if (result == 2)
                {
                    return $"⚠️ Scan timeout - No tags detected. Make sure tag is VERY close to antenna.";
                }
                else if (result == 0xFB)
                {
                    return $"⚠️ No tags detected (0xFB). Check: 1) Tag is UHF, 2) Tag is within 2-3 inches, 3) Antenna connected";
                }
                else
                {
                    return $"⚠️ Scan result: {result} - {GetErrorDescription(result)}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test scan error: {ex.Message}");
                return $"❌ Error: {ex.Message}";
            }
        }

        /* ================= ADD TEST TAG (FOR DEBUGGING) ================= */

        public void AddTestTag(string tagId = "TEST123456789ABCDEF")
        {
            Console.WriteLine($"🧪 Adding test tag: {tagId}");
            SaveTag(tagId, 50, 1);
        }

        public string GetDiagnosticInfo()
        {
            return $"Connected: {IsConnected}, Scanning: {IsScanning}, Tags: {GetTagCount()}";
        }

        /* ================= TEST METHODS ================= */

        /// <summary>
        /// Manual single scan for testing
        /// </summary>
        //public string TestSingleScan()
        //{
        //    if (!IsConnected)
        //    {
        //        return "❌ Reader not connected";
        //    }

        //    try
        //    {
        //        Console.WriteLine("\n🧪 ========== MANUAL TEST SCAN ==========");

        //        // Clear buffer
        //        _reader.ClearBuffer_G2(ref _comAddr);

        //        // Single inventory
        //        byte qValue = 4;
        //        byte session = 0;
        //        byte maskMem = 0;
        //        byte[] maskAdr = new byte[2] { 0, 0 };
        //        byte maskLen = 0;
        //        byte[] maskData = new byte[0];
        //        byte maskFlag = 0;
        //        byte adrTID = 0;
        //        byte lenTID = 0;
        //        byte tidFlag = 0;
        //        byte target = 0;
        //        byte inAnt = 0;
        //        byte scanTime = 30; // 3 seconds
        //        byte fastFlag = 1;

        //        byte[] epcList = new byte[5000];
        //        byte ant = 0;
        //        int totalLen = 0;
        //        int cardNum = 0;

        //        Console.WriteLine("   🔄 Executing Inventory_G2...");
        //        int result = _reader.Inventory_G2(
        //            ref _comAddr,
        //            qValue,
        //            session,
        //            maskMem,
        //            maskAdr,
        //            maskLen,
        //            maskData,
        //            maskFlag,
        //            adrTID,
        //            lenTID,
        //            tidFlag,
        //            target,
        //            inAnt,
        //            scanTime,
        //            fastFlag,
        //            epcList,
        //            ref ant,
        //            ref totalLen,
        //            ref cardNum
        //        );

        //        Console.WriteLine($"   📊 Result: {result} - {GetErrorDescription(result)}");
        //        Console.WriteLine($"   📊 Cards: {cardNum}, TotalLen: {totalLen}, Antenna: {ant}");

        //        if (totalLen > 0)
        //        {
        //            Console.WriteLine($"   📦 Raw data: {BitConverter.ToString(epcList, 0, Math.Min(200, totalLen))}");
        //        }

        //        if (cardNum > 0 && totalLen > 0)
        //        {
        //            ParseRealTimeInventory(epcList, cardNum, totalLen, ant);
        //            return $"✅ Found {cardNum} tags! Check console for details.";
        //        }
        //        else if (result == 2 || result == 0x02)
        //        {
        //            return "⚠️ Timeout - No tags detected. Hold tag VERY close (2-3 inches)";
        //        }
        //        else if (result == 0xFB)
        //        {
        //            return "⚠️ No tags (0xFB). Check: 1) UHF tags? 2) Close enough? 3) Antenna connected?";
        //        }
        //        else if (result == 0)
        //        {
        //            return "⚠️ Success but no tags. Reader working but not detecting anything.";
        //        }
        //        else
        //        {
        //            return $"⚠️ Error: {result} - {GetErrorDescription(result)}";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Test scan error: {ex.Message}");
        //        return $"❌ Error: {ex.Message}";
        //    }
        //}

        /// <summary>
        /// Add a test tag manually (for debugging)
        /// </summary>
        //public void AddTestTag(string tagId = "TEST123456789ABCDEF")
        //{
        //    Console.WriteLine($"🧪 Adding test tag to memory: {tagId}");
        //    SaveTag(tagId, 50, 1);
        //}

        /* ================= ERROR CODES ================= */

        private string GetErrorDescription(int errorCode)
        {
            return errorCode switch
            {
                0x00 => "Success",
                0x01 => "Inventory completed successfully",
                0x02 => "Inventory timeout (no tags found)",
                0x05 => "Access password error",
                0x09 => "Kill password error",
                0x0E => "Fail to unlock protection",
                0x10 => "Tag memory locked",
                0x13 => "Failed to store parameters",
                0x14 => "Modification failed",
                0x15 => "Response within time",
                0x18 => "Reader memory full",
                0x30 => "Communication error",
                0x33 => "Reader busy",
                0x35 => "Port already opened",
                0xF8 => "Antenna check error",
                0xF9 => "Operation failed",
                0xFA => "Tag detected but communication failed",
                0xFB => "No tag detected",
                0xFC => "Tag returned error",
                0xFD => "Command length error",
                0xFE => "Illegal command",
                0xFF => "Parameter error",
                _ => $"Unknown error: 0x{errorCode:X2}"
            };
        }
    }
}