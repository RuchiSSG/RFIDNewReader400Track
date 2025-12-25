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
        public async Task<bool> PostRFIDRunningLogAsync(
            string accessToken, string userid, string recruitid, string DeviceId,
            string Location, string eventName, string eventId, List<RfidData> rfidDataList,
            string sessionid, string ipaddress)
        {
            try
            {
                var url = $"{_baseUrl}RFIDChestNoMapping/RFIDRunningLog?userid={userid}&recruitid={recruitid}&deviceid={DeviceId}&Location={Location}&eventName={eventName}&eventId={eventId}&sessionid={sessionid}&ipaddress={ipaddress}";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                // Group by TagId
                var groupedData = rfidDataList
                    .GroupBy(x => x.TagId)
                    .Select(g => new
                    {
                        TagId = g.Key,
                        Laps = g.OrderBy(x => x.Timestamp).ToList()
                    }).ToList();

                // Decide lap count based on event
                int totalLaps = eventName == "1600 Meter Running" ? 5 :
                              eventName == "800 Meter Running" ? 3 :
                                1;

                // Prepare final request data
                var requestData = groupedData.Select(x => new
                {
                   RFIDdtagata = x.TagId,
                    //Lap1 = x.Laps.Count >= 1 && x.Laps[0].Timestamp.HasValue ? x.Laps[0].Timestamp.Value.ToString("HH:mm:ss:fff") : null,
                    //Lap2 = x.Laps.Count >= 2 && x.Laps[1].Timestamp.HasValue ? x.Laps[1].Timestamp.Value.ToString("HH:mm:ss:fff") : null,
                    //Lap3 = x.Laps.Count >= 3 && x.Laps[2].Timestamp.HasValue ? x.Laps[2].Timestamp.Value.ToString("HH:mm:ss:fff") : null,
                    //Lap4 = x.Laps.Count >= 4 && x.Laps[3].Timestamp.HasValue ? x.Laps[3].Timestamp.Value.ToString("HH:mm:ss:fff") : null,
                    //Lap5 = x.Laps.Count >= 5 && x.Laps[4].Timestamp.HasValue ? x.Laps[4].Timestamp.Value.ToString("HH:mm:ss:fff") : null,

                    Lap1 = x.Laps.Count >= 1 ? x.Laps[0].Timestamp.ToString("HH:mm:ss:fff") : null,
                    Lap2 = x.Laps.Count >= 2 ? x.Laps[1].Timestamp.ToString("HH:mm:ss:fff") : null,
                    Lap3 = x.Laps.Count >= 3 ? x.Laps[2].Timestamp.ToString("HH:mm:ss:fff") : null,
                    Lap4 = x.Laps.Count >= 4 ? x.Laps[3].Timestamp.ToString("HH:mm:ss:fff") : null,
                    Lap5 = x.Laps.Count >= 5 ? x.Laps[4].Timestamp.ToString("HH:mm:ss:fff") : null,


                    TotalLaps = totalLaps,
                    EventName = eventName
                }).ToList();


                // Send request
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while inserting RFID data");
                return false;
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


        public async Task<DeleteRfid> DeleteRFIDRecordsAsync(string accessToken, string userid, string recruitid, string deviceId, string location, string eventName, string sessionid, string ipaddress)
        {
            try
            {
                var url = $"{_baseUrl}RFIDChestNoMapping/RIFDRunningDelete?userid={userid}&recruitid={recruitid}&deviceid={deviceId}&location={location}&eventName={eventName}&sessionid={sessionid}&ipaddress={ipaddress}";
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