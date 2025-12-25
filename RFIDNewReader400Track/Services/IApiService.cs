using Microsoft.AspNetCore.Mvc;
using RFIDReaderPortal.Models;
using System.Collections.Generic;

namespace RFIDReaderPortal.Services
{
    public interface IApiService
    {

        Task<List<RecruitmentDto>> GetAllRecruitmentsAsync(string accessToken,string userid, string recruitid, string sesionid, string ipaddress);


        Task<object> GetAllRecruitEventsAsync(string accessToken, string userid, string recruitid, string sessionid, string ipaddress);
        Task<object> GetAllCategorysync(string accessToken, string userid, string recruitid);

        Task<object> GetAsync(string accessToken, string userid, string deviceid,string sessionid, string ipaddress);
        Task ProcessRFIDEventAsync(EventModel model, string accessToken);

        Task<dynamic> InsertDeviceConfigurationAsync(string accessToken, DeviceConfigurationDto formData, string sesionid,string ipaddress);
        Task<DeleteRfid> DeleteRFIDRecordsAsync(string accessToken, string userid, string recruitid, string deviceId, string location, string eventName, string sessionid, string ipaddress);
        Task<bool> PostRFIDRunningLogAsync(string accessToken, string userid, string recruitid, string DeviceId, string Location, string eventName, List<RfidData> rfidDataList, string sessionid, string ipaddress);

    }
}