using RFIDReaderPortal.Models;
using System.Collections;

namespace RFIDReaderPortal.Services
{
    public interface ITcpListenerService
    {
        Task <List<RFIDChestNoMappingDto>> InsertStoredRfidDataAsync();
        bool IsRunning { get; }

        void Start();

        void Stop();
        void StartRace();   // ✅ ADD THIS
        void StopRace();
        RfidData[] GetReceivedData();

        string[] GetHexData();

        void ClearData();

        void SetParameters(string accessToken, string userid, string recruitid, string deviceId, string location, string eventName, string eventId, string ipaddress,string sesionid);
    }
}