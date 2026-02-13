//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using PoliceRecruitmentAPI.Core.ModelDtos;
//using RFIDReaderPortal.Models;
//using System.Collections.Generic;
//using System.Net.Http.Headers;
//using System.Text;

//namespace RFIDReaderPortal.Services
//{
//    public class ApiService : IApiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly string _baseUrl;
//        private ILogger<ApiService> _logger;
//        private Dictionary<string, DateTime> _processedTags = new Dictionary<string, DateTime>();

//        public ApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ApiService> logger)
//        {
//            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            _baseUrl = configuration["ApiBaseUrl"];
//            //var environment = configuration["ASPNETCORE_ENVIRONMENT"];
//            //_baseUrl = environment == "Development"
//            //    ? configuration["ApiBaseUrl:Azure"]
//            //    : configuration["ApiBaseUrl:Local"];

//            if (string.IsNullOrEmpty(_baseUrl))
//            {
//                _logger.LogError("ApiBaseUrl is not configured in the application settings.");
//                throw new InvalidOperationException("ApiBaseUrl is not configured in the application settings.");
//            }

//            _logger.LogInformation($"ApiService initialized with BaseUrl: {_baseUrl}");
//        }


//        public async Task<object> GetAllRecruitEventsAsync(string accessToken, string userid, string recruitid, string sessionid, string ipaddress)
//        {
//            var url = $"{_baseUrl}RecruitmentEvent/GetAllRecruitEvent?userid={userid}&recruitid={recruitid}&sessionid={sessionid}&ipaddress={ipaddress}";

//            var request = new HttpRequestMessage(HttpMethod.Get, url);
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);


//            var response = await _httpClient.SendAsync(request);

//            if (response.IsSuccessStatusCode)
//            {
//                string content = await response.Content.ReadAsStringAsync();
//                dynamic deserializedResponse = JsonConvert.DeserializeObject(content);
//                return deserializedResponse; // This should now have the 'outcome' and 'data'
//            }

//            throw new Exception("Failed to fetch recruitment events.");
//        }

//        public async Task<object> GetAllCategorysync(string accessToken, string userid, string recruitid)
//        {
//            var url = $"{_baseUrl}CategoryMaster/GetAll?userid={userid}&recConfId={recruitid}";

//            var request = new HttpRequestMessage(HttpMethod.Get, url);
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//            var response = await _httpClient.SendAsync(request);

//            if (response.IsSuccessStatusCode)
//            {
//                dynamic content = await response.Content.ReadAsStringAsync();
//                dynamic desobj = JsonConvert.DeserializeObject(content);
//                return desobj;
//            }
//            throw new Exception("Failed to fetch recruitment events.");
//        }
//        //public async Task<List<ChestBarcodeDto>> GetAllChestNoAsync(string accessToken,string userid,string recruitid,string sessionid,string ipaddress)
//        //{
//        //    var url = $"{_baseUrl}RFIDChestNoMapping/GetChestno?userid={userid}&recruitid={recruitid}&sessionid={sessionid}&ipaddress={ipaddress}";

//        //    var request = new HttpRequestMessage(HttpMethod.Get, url);
//        //    request.Headers.Authorization =
//        //        new AuthenticationHeaderValue("Bearer", accessToken);

//        //    var response = await _httpClient.SendAsync(request);

//        //        throw new Exception("Failed to fetch chest barcode data.");

//        //    var json = await response.Content.ReadAsStringAsync();

//        //    // 🔥 STEP 1: Deserialize wrapper
//        //    var result = JsonConvert.DeserializeObject<Result>(json);

//        //    // 🔥 STEP 2: Extract Data safely
//        //    if (result?.Data == null)
//        //        return new List<ChestBarcodeDto>();

//        //    var dataJson = result.Data.ToString();

//        //    // 🔥 STEP 3: Deserialize actual list
//        //    return JsonConvert.DeserializeObject<List<ChestBarcodeDto>>(dataJson)
//        //           ?? new List<ChestBarcodeDto>();
//        //}


//        //public async Task<object> GetAllchestno(string accessToken, string userid, string recruitid, string sesionid, string ipaddress)
//        //{
//        //    var url = $"{_baseUrl}RFIDChestNoMapping/GetChestno?userid={userid}&recruitid={recruitid}&sessionid={sesionid}&ipaddress={ipaddress}";

//        //    var request = new HttpRequestMessage(HttpMethod.Get, url);
//        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//        //    var response = await _httpClient.SendAsync(request);

//        //    if (response.IsSuccessStatusCode)
//        //    {
//        //        dynamic content = await response.Content.ReadAsStringAsync();
//        //        dynamic desobj = JsonConvert.DeserializeObject(content);
//        //        return desobj;
//        //    }
//        //    throw new Exception("Failed to fetch recruitment events.");
//        //}
//        public async Task<List<RecruitmentDto>> GetAllRecruitmentsAsync(string accessToken, string userid, string recruitid, string sesionid, string ipaddress)
//        {
//            var url = $"{_baseUrl}RecruitmentEvent/GetAllRecruitEvent?userid={userid}&recruitid={recruitid}&sessionid={sesionid}&ipaddress={ipaddress}";
//            var request = new HttpRequestMessage(HttpMethod.Get, url);
//            //var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/Recruitment/GetAll");
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
//            var response = await _httpClient.SendAsync(request);

//            if (response.IsSuccessStatusCode)
//            {
//                var content = await response.Content.ReadAsStringAsync();
//                var dataObject = new { data = new List<RecruitmentDto>() };
//                var deserializedResponse = JsonConvert.DeserializeAnonymousType(content, dataObject);
//                return deserializedResponse.data;
//            }
//            throw new Exception("Failed to fetch recruitments.");
//        }


//        public async Task<object> GetAsync(string accessToken, string userid, string deviceid, string sessionid, string ipaddress)
//        {
//            var url = $"{_baseUrl}DeviceConfiguration/Get?userid={userid}&deviceid={deviceid}&sessionid={sessionid}&ipaddress={ipaddress}";

//            var request = new HttpRequestMessage(HttpMethod.Get, url);
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//            var response = await _httpClient.SendAsync(request);

//            if (response.IsSuccessStatusCode)
//            {
//                dynamic content = await response.Content.ReadAsStringAsync();
//                dynamic desobj = JsonConvert.DeserializeObject(content);
//                return desobj;
//            }
//            throw new Exception("Failed to fetch recruitment events.");
//        }
//        public async Task<dynamic> GetAllGroupsAsync(string accessToken, CandidateDto model)
//        {
//            var url =$"{_baseUrl}Candidate/GetGroup?UserId={model.UserId}&RecruitId={model.RecruitId}&EventId={model.EventId}&Category={model.Category}";

//            var request = new HttpRequestMessage(HttpMethod.Get, url);
//            request.Headers.Authorization =
//                new AuthenticationHeaderValue("Bearer", accessToken);

//            var response = await _httpClient.SendAsync(request);

//            // 🔴 DEBUG ke liye (important)
//            var errorText = await response.Content.ReadAsStringAsync();

//            if (!response.IsSuccessStatusCode)
//            {
//                throw new Exception($"API Error: {response.StatusCode} - {errorText}");
//            }

//            return JsonConvert.DeserializeObject(errorText);
//        }


//        //public async Task<bool> PostRFIDRunningLogAsync(
//        //    string accessToken, string userid, string recruitid, string DeviceId,
//        //    string Location, string eventName, string eventId, List<RfidData> rfidDataList,
//        //    string sessionid, string ipaddress)
//        //{
//        //    try
//        //    {
//        //        var url = $"{_baseUrl}RFIDChestNoMapping/RFIDRunningLog?userid={userid}&recruitid={recruitid}&deviceid={DeviceId}&Location={Location}&eventName={eventName}&eventId={eventId}&sessionid={sessionid}&ipaddress={ipaddress}";
//        //        var request = new HttpRequestMessage(HttpMethod.Post, url);

//        //        // Group by TagId
//        //        var groupedData = rfidDataList
//        //            .GroupBy(x => x.TagId)
//        //            .Select(g => new
//        //            {
//        //                TagId = g.Key,
//        //                Laps = g.OrderBy(x => x.Timestamp).ToList()
//        //            }).ToList();

//        //        // Decide lap count based on event
//        //        int totalLaps = eventName == "1600 Meter Running" ? 5 :
//        //                      eventName == "800 Meter Running" ? 3 :
//        //                        1;

//        //        // Prepare final request data
//        //        var requestData = groupedData.Select(x => new
//        //        {
//        //           RFIDdtagata = x.TagId,
//        //            Lap1 = x.Laps.Count >= 1 ? x.Laps[0].Timestamp.ToString("HH:mm:ss:fff") : null,
//        //            Lap2 = x.Laps.Count >= 2 ? x.Laps[1].Timestamp.ToString("HH:mm:ss:fff") : null,
//        //            Lap3 = x.Laps.Count >= 3 ? x.Laps[2].Timestamp.ToString("HH:mm:ss:fff") : null,
//        //            Lap4 = x.Laps.Count >= 4 ? x.Laps[3].Timestamp.ToString("HH:mm:ss:fff") : null,
//        //            Lap5 = x.Laps.Count >= 5 ? x.Laps[4].Timestamp.ToString("HH:mm:ss:fff") : null,


//        //            TotalLaps = totalLaps,
//        //            EventName = eventName
//        //        }).ToList();


//        //        // Send request
//        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
//        //        request.Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

//        //        var response = await _httpClient.SendAsync(request);
//        //        if (response.IsSuccessStatusCode)
//        //        {
//        //            var url1 = $"{_baseUrl}RFIDChestNoMapping/GetChestno?userid={userid}&recruitid={recruitid}&sessionid={sessionid}&ipaddress={ipaddress}";
//        //            var request1 = new HttpRequestMessage(HttpMethod.Get, url1);
//        //            var response1 = await _httpClient.SendAsync(request1);
//        //        }

//        //        return response.IsSuccessStatusCode;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, "Error occurred while inserting RFID data");
//        //        return false;
//        //    }
//        //}

//        public async Task<bool> PostRFIDRunningLogAsync(
//    string accessToken, string userid, string recruitid, string DeviceId,
//    string Location, string eventName, string eventId, List<RfidData> rfidDataList,
//    string sessionid, string ipaddress)
//        {
//            try
//            {
//                var url = $"{_baseUrl}RFIDChestNoMapping/RFIDRunningLog?userid={userid}&recruitid={recruitid}&deviceid={DeviceId}&Location={Location}&eventName={eventName}&eventId={eventId}&sessionid={sessionid}&ipaddress={ipaddress}";

//                var request = new HttpRequestMessage(HttpMethod.Post, url);

//                var groupedData = rfidDataList
//                    .GroupBy(x => x.TagId)
//                    .Select(g => new
//                    {
//                        RFIDdtagata = g.Key,
//                        Laps = g.OrderBy(x => x.Timestamp).ToList()
//                    }).ToList();

//                int totalLaps = eventName == "1600 Meter Running" ? 5 :
//                                eventName == "800 Meter Running" ? 3 : 1;

//                var requestData = groupedData.Select(x => new
//                {
//                    RFIDdtagata = x.RFIDdtagata,
//                    Lap1 = x.Laps.Count > 0 ? x.Laps[0].Timestamp.ToString("HH:mm:ss:fff") : null,
//                    Lap2 = x.Laps.Count > 1 ? x.Laps[1].Timestamp.ToString("HH:mm:ss:fff") : null,
//                    Lap3 = x.Laps.Count > 2 ? x.Laps[2].Timestamp.ToString("HH:mm:ss:fff") : null,
//                    Lap4 = x.Laps.Count > 3 ? x.Laps[3].Timestamp.ToString("HH:mm:ss:fff") : null,
//                    Lap5 = x.Laps.Count > 4 ? x.Laps[4].Timestamp.ToString("HH:mm:ss:fff") : null,
//                    TotalLaps = totalLaps,
//                    EventName = eventName
//                }).ToList();

//                request.Headers.Authorization =
//                    new AuthenticationHeaderValue("Bearer", accessToken);

//                request.Content = new StringContent(
//                    JsonConvert.SerializeObject(requestData),
//                    Encoding.UTF8,
//                    "application/json");

//                var response = await _httpClient.SendAsync(request);

//              //  if (!response.IsSuccessStatusCode)
//                    return response.IsSuccessStatusCode;

//                // ✅ INSERT KE BAAD CHEST DATA LAO
//                //var url1 = $"{_baseUrl}RFIDChestNoMapping/GetChestno?userid={userid}&recruitid={recruitid}&eventName={eventName}&eventId={eventId}&sessionid={sessionid}&ipaddress={ipaddress}";

//                //var response1 = await _httpClient.GetAsync(url1);

//                //if (response1.IsSuccessStatusCode)
//                //{
//                //    var json = await response1.Content.ReadAsStringAsync();

//                //    var Result = JsonConvert.DeserializeObject<GetChestApiResponse>(json);

//                //    return Result?.data ?? new List<ChestBarcodeDto>();
//                //}

//                //return null;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error occurred while inserting RFID data");
//                return false;
//            }
//        }

//        //public async Task<bool> PostRFIDRunningLogAsync(string accessToken, string userid, string recruitid, string DeviceId, string Location, string eventName, List<RfidData> rfidDataList, string sessionid, string ipaddress)
//        //{
//        //    try
//        //    {
//        //        var url = $"{_baseUrl}RFIDChestNoMapping/RFIDRunningLog?userid={userid}&recruitid={recruitid}&deviceid={DeviceId}&Location={Location}&eventName={eventName}&sessionid={sessionid}&ipaddress={ipaddress}";
//        //        var request = new HttpRequestMessage(HttpMethod.Post, url);

//        //        var requestData = rfidDataList.Select(rfidData => new
//        //        {
//        //            RFIDdtagata = rfidData.TagId,
//        //            Timestamp = rfidData.Timestamp.ToString("HH:mm:ss:fff")
//        //          //  LapCount = rfidData.LapNo
//        //        }).ToList();

//        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
//        //        request.Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

//        //        var response = await _httpClient.SendAsync(request);

//        //        if (response.IsSuccessStatusCode)
//        //        {
//        //            _logger.LogInformation($"Successfully inserted {rfidDataList.Count} RFID records");
//        //            return true;
//        //        }
//        //        else
//        //        {
//        //            var content = await response.Content.ReadAsStringAsync();
//        //            _logger.LogError($"Failed to insert RFID data. Status: {response.StatusCode}, Content: {content}");
//        //            return false;
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, "Error occurred while inserting RFID data");
//        //        return false;
//        //    }
//        //}

//        public async Task ProcessRFIDEventAsync(EventModel model, string accessToken)
//        {
//            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}rfidevents");
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
//            request.Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

//            var response = await _httpClient.SendAsync(request);

//            if (!response.IsSuccessStatusCode)
//            {
//                throw new Exception("Failed to process RFID event.");
//            }
//        }
//        public async Task<dynamic> InsertDeviceConfigurationAsync(string accessToken, DeviceConfigurationDto formData, string sessionId, string ipAddress)
//        {
//            var url = $"{_baseUrl}DeviceConfiguration/Insert?sessionid={sessionId}&ipaddress={ipAddress}";

//            var jsonData = JsonConvert.SerializeObject(formData);
//            _logger.LogInformation($"Sending JSON data: {jsonData}");

//            var request = new HttpRequestMessage(HttpMethod.Post, url)
//            {
//                Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
//            };

//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//            try
//            {
//                var response = await _httpClient.SendAsync(request);

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    //var deserializedContent = JsonConvert.DeserializeObject<dynamic>(content);
//                    var deserializedContent = JsonConvert.DeserializeObject<dynamic>(content);

//                    return deserializedContent;
//                }
//                else
//                {
//                    var errorContent = await response.Content.ReadAsStringAsync();
//                    throw new Exception($"API call failed with status code: {response.StatusCode}, Response: {errorContent}");
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An error occurred while calling the InsertDeviceConfiguration API.");
//                throw;
//            }
//        }


//        public async Task<DeleteRfid> DeleteRFIDRecordsAsync(string accessToken, string userid, string recruitid, string deviceId, string location, string eventName, string eventId, string sessionid, string ipaddress)
//        {
//            try
//            {
//                var url = $"{_baseUrl}RFIDChestNoMapping/RIFDRunningDelete?userid={userid}&recruitid={recruitid}&deviceid={deviceId}&location={location}&eventName={eventName}&eventId={eventId}&sessionid={sessionid}&ipaddress={ipaddress}";
//                var request = new HttpRequestMessage(HttpMethod.Post, url);

//                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//                var response = await _httpClient.SendAsync(request);

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var jsonResponse = JsonConvert.DeserializeObject<DeleteRfid>(content);

//                    _logger.LogInformation($"Successfully deleted RFID records for device {deviceId} and event {eventName}");
//                    return jsonResponse;
//                }
//                else
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    _logger.LogError($"Failed to delete RFID records. Status: {response.StatusCode}, Content: {content}");
//                    return null;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error occurred while deleting RFID records");
//                return null;
//            }
//        }
//    }
//}
using Newtonsoft.Json;
using RFIDReaderPortal.Models;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace RFIDReaderPortal.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private ILogger<ApiService> _logger;
        private Dictionary<string, DateTime> _processedTags = new Dictionary<string, DateTime>();

        public ApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ApiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _baseUrl = configuration["ApiBaseUrl"];
            //var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            //_baseUrl = environment == "Development"
            //    ? configuration["ApiBaseUrl:Azure"]
            //    : configuration["ApiBaseUrl:Local"];

            if (string.IsNullOrEmpty(_baseUrl))
            {
                _logger.LogError("ApiBaseUrl is not configured in the application settings.");
                throw new InvalidOperationException("ApiBaseUrl is not configured in the application settings.");
            }

            _logger.LogInformation($"ApiService initialized with BaseUrl: {_baseUrl}");
        }


        public async Task<object> GetAllRecruitEventsAsync(string accessToken, string userid, string recruitid, string sessionid, string ipaddress)
        {
            var url = $"{_baseUrl}RecruitmentEvent/GetAllRecruitEvent?userid={userid}&recruitid={recruitid}&sessionid={sessionid}&ipaddress={ipaddress}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);


            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                dynamic deserializedResponse = JsonConvert.DeserializeObject(content);
                return deserializedResponse; // This should now have the 'outcome' and 'data'
            }

            throw new Exception("Failed to fetch recruitment events.");
        }

        public async Task<object> GetAllCategorysync(string accessToken, string userid, string recruitid)
        {
            var url = $"{_baseUrl}CategoryMaster/GetAll?userid={userid}&recConfId={recruitid}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                dynamic content = await response.Content.ReadAsStringAsync();
                dynamic desobj = JsonConvert.DeserializeObject(content);
                return desobj;
            }
            throw new Exception("Failed to fetch recruitment events.");
        }

        public async Task<List<RecruitmentDto>> GetAllRecruitmentsAsync(string accessToken, string userid, string recruitid, string sesionid, string ipaddress)
        {
            var url = $"{_baseUrl}RecruitmentEvent/GetAllRecruitEvent?userid={userid}&recruitid={recruitid}&sessionid={sesionid}&ipaddress={ipaddress}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            //var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/Recruitment/GetAll");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var dataObject = new { data = new List<RecruitmentDto>() };
                var deserializedResponse = JsonConvert.DeserializeAnonymousType(content, dataObject);
                return deserializedResponse.data;
            }
            throw new Exception("Failed to fetch recruitments.");
        }


        public async Task<object> GetAsync(string accessToken, string userid, string deviceid, string sessionid, string ipaddress)
        {
            var url = $"{_baseUrl}DeviceConfiguration/Get?userid={userid}&deviceid={deviceid}&sessionid={sessionid}&ipaddress={ipaddress}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                dynamic content = await response.Content.ReadAsStringAsync();
                dynamic desobj = JsonConvert.DeserializeObject(content);
                return desobj;
            }
            throw new Exception("Failed to fetch recruitment events.");
        }
        public async Task<List<RFIDChestNoMappingDto>> PostRFIDRunningLogAsync(
            string accessToken, string userid, string recruitid, string DeviceId,
            string Location, string eventName, string eventId, List<RfidData> rfidDataList,
            string sessionid, string ipaddress)
        {
            try
            {
                var url = $"{_baseUrl}RFIDChestNoMapping/RFIDRunningLog400meter?userid={userid}&recruitid={recruitid}&deviceid={DeviceId}&Location={Location}&eventName={eventName}&eventId={eventId}&sessionid={sessionid}&ipaddress={ipaddress}";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                // Group by TagId
                var groupedData = rfidDataList
                 .GroupBy(x => x.TagId)
                 .Select(g => new
                 {
                     TagId = g.Key,
                     Laps = g.OrderBy(x => x.Timestamp).ToList()
                 }).ToList();

                //// Decide lap count based on event
                //int totalLaps = eventName == "1600 Meter Running" ? 1 : 1;
                //// eventName == "800 Meter Running" ? 3 :


                // Prepare final request data
                var requestData = rfidDataList.Select(x => new
                {
                    RFIDdtagata = x.TagId,

                    Lap1 = x.LapTimes.Count >= 1 ? x.LapTimes[0].ToString("HH:mm:ss:fff") : null,
                    Lap2 = x.LapTimes.Count >= 2 ? x.LapTimes[1].ToString("HH:mm:ss:fff") : null,
                    Lap3 = x.LapTimes.Count >= 3 ? x.LapTimes[2].ToString("HH:mm:ss:fff") : null,
                    Lap4 = x.LapTimes.Count >= 4 ? x.LapTimes[3].ToString("HH:mm:ss:fff") : null,
                    Lap5 = x.LapTimes.Count >= 5 ? x.LapTimes[4].ToString("HH:mm:ss:fff") : null,

                    TotalLaps =
        eventName == "800 Meter Running" ? 3 :
        eventName == "1600 Meter Running" ? 5 :
        1, // default (100m)

                    EventName = eventName
                }).ToList();


                // Send request
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);


                if (!response.IsSuccessStatusCode)
                    return new List<RFIDChestNoMappingDto>();

                // ✅ Insert success → CALL GET ALL
                return await GetAllRFIDRunningLogAsync(accessToken, userid, recruitid, eventId, eventName, sessionid, ipaddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while inserting RFID data");
                //return false;
                return new List<RFIDChestNoMappingDto>();
            }
        }
        public async Task<List<RFIDChestNoMappingDto>> GetAllRFIDRunningLogAsync(
    string accessToken,
    string userid,
    string recruitid,
    string eventId,
    string eventName,
    string sessionid,
    string ipaddress)
        {
            try
            {
                var url = $"{_baseUrl}RFIDChestNoMapping/GetChestno?userid={userid}&recruitid={recruitid}&eventId={eventId}&eventName={eventName}&sessionid={sessionid}&ipaddress={ipaddress}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return new List<RFIDChestNoMappingDto>();

                var json = await response.Content.ReadAsStringAsync();

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<RFIDChestNoMappingDto>>>(json);

                return apiResponse?.data ?? new List<RFIDChestNoMappingDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching chest barcode data");
                return new List<RFIDChestNoMappingDto>();
            }
        }
        public async Task<List<GroupDto>> GetAllGroup(
string accessToken,
string userid,
string recruitid,
string eventId,
string eventName,
string sessionid,
string ipaddress)
        {
            try
            {
                var url = $"{_baseUrl}RFIDChestNoMapping/Getgroup?userid={userid}&recruitid={recruitid}&eventId={eventId}&eventName={eventName}&sessionid={sessionid}&ipaddress={ipaddress}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return new List<GroupDto>();

                var json = await response.Content.ReadAsStringAsync();

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<GroupDto>>>(json);

                return apiResponse?.data ?? new List<GroupDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching chest barcode data");
                return new List<GroupDto>();
            }
        }

        public async Task<List<ChestRFIDDto>> GetAllChestRFID(
string accessToken,
string userid,
string recruitid,
string eventId,
string eventName,
string sessionid,
string ipaddress, int groupId)
        {
            try
            {
                var url = $"{_baseUrl}RFIDChestNoMapping/GetChestRFID?userid={userid}&recruitid={recruitid}&eventId={eventId}&eventName={eventName}&sessionid={sessionid}&ipaddress={ipaddress}&groupid={groupId}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return new List<ChestRFIDDto>();

                var json = await response.Content.ReadAsStringAsync();

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<ChestRFIDDto>>>(json);

                return apiResponse?.data ?? new List<ChestRFIDDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching chest barcode data");
                return new List<ChestRFIDDto>();
            }
        }

        //public async Task<bool> PostRFIDRunningLogAsync(string accessToken, string userid, string recruitid, string DeviceId, string Location, string eventName, List<RfidData> rfidDataList, string sessionid, string ipaddress)
        //{
        //    try
        //    {
        //        var url = $"{_baseUrl}RFIDChestNoMapping/RFIDRunningLog?userid={userid}&recruitid={recruitid}&deviceid={DeviceId}&Location={Location}&eventName={eventName}&sessionid={sessionid}&ipaddress={ipaddress}";
        //        var request = new HttpRequestMessage(HttpMethod.Post, url);

        //        var requestData = rfidDataList.Select(rfidData => new
        //        {
        //            RFIDdtagata = rfidData.TagId,
        //            Timestamp = rfidData.Timestamp.ToString("HH:mm:ss:fff")
        //          //  LapCount = rfidData.LapNo
        //        }).ToList();

        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //        request.Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

        //        var response = await _httpClient.SendAsync(request);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            _logger.LogInformation($"Successfully inserted {rfidDataList.Count} RFID records");
        //            return true;
        //        }
        //        else
        //        {
        //            var content = await response.Content.ReadAsStringAsync();
        //            _logger.LogError($"Failed to insert RFID data. Status: {response.StatusCode}, Content: {content}");
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while inserting RFID data");
        //        return false;
        //    }
        //}

        public async Task ProcessRFIDEventAsync(EventModel model, string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}rfidevents");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to process RFID event.");
            }
        }
        public async Task<dynamic> InsertDeviceConfigurationAsync(string accessToken, DeviceConfigurationDto formData, string sessionId, string ipAddress)
        {
            var url = $"{_baseUrl}DeviceConfiguration/Insert?sessionid={sessionId}&ipaddress={ipAddress}";

            var jsonData = JsonConvert.SerializeObject(formData);
            _logger.LogInformation($"Sending JSON data: {jsonData}");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    //var deserializedContent = JsonConvert.DeserializeObject<dynamic>(content);
                    var deserializedContent = JsonConvert.DeserializeObject<dynamic>(content);

                    return deserializedContent;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API call failed with status code: {response.StatusCode}, Response: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling the InsertDeviceConfiguration API.");
                throw;
            }
        }


        public async Task<DeleteRfid> DeleteRFIDRecordsAsync(string accessToken, string userid, string recruitid, string deviceId, string location, string eventName, string eventId, string sessionid, string ipaddress)
        {
            try
            {
                var url = $"{_baseUrl}RFIDChestNoMapping/RIFDRunningDelete?userid={userid}&recruitid={recruitid}&deviceid={deviceId}&location={location}&eventName={eventName}&eventId={eventId}&sessionid={sessionid}&ipaddress={ipaddress}";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonConvert.DeserializeObject<DeleteRfid>(content);

                    _logger.LogInformation($"Successfully deleted RFID records for device {deviceId} and event {eventName}");
                    return jsonResponse;
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to delete RFID records. Status: {response.StatusCode}, Content: {content}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting RFID records");
                return null;
            }
        }
    }
}