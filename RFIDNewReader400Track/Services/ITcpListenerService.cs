//using RFIDReaderPortal.Models;
//using System.Collections;

//namespace RFIDReaderPortal.Services
//{
//    public interface ITcpListenerService
//    {
//        // Task InsertStoredRfidDataAsync();
//       // public Task InsertStoredRfidDataAsync();
//        bool IsRunning { get; }

//        void Start();

//        void Stop();

//        RfidData[] GetReceivedData();

//        string[] GetHexData();

//        void ClearData();
//        Task<List<RFIDChestNoMappingDto>> InsertStoredRfidDataAsync();
//        void SetParameters(string accessToken, string userid, string recruitid, string deviceId, string location, string eventName, string eventId, string ipaddress,string sesionid);
//    }
//}
using RFIDReaderPortal.Models;
using System.Collections;

namespace RFIDReaderPortal.Services
{
    public interface ITcpListenerService
    {
        Task<List<RFIDChestNoMappingDto>> InsertStoredRfidDataAsync();
        bool IsRunning { get; }
        void StartRace();   // ✅ ADD THIS
        void StopRace();

        void Start();

        void Stop();
        void SetAllowedTags(IEnumerable<string> tagIds);
        RfidData[] GetReceivedData();

        string[] GetHexData();

        void ClearData();

        void SetParameters(string accessToken, string userid, string recruitid, string deviceId, string location, string eventName, string eventId, string ipaddress, string sesionid);
    }
}