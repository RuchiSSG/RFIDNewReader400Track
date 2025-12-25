using RFIDReaderPortal.Models;
using System.Collections;

namespace RFIDReaderPortal.Services
{
    public interface ITcpListenerService
    {
        Task InsertStoredRfidDataAsync();
        bool IsRunning { get; }

        void Start();

        void Stop();

        RfidData[] GetReceivedData();

        string[] GetHexData();

        void ClearData();

        void SetParameters(string accessToken, string userid, string recruitid, string deviceId, string location, string eventName, string ipaddress,string sesionid);
    }
}