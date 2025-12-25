using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RFIDReaderPortal.Models;
using RFIDReaderPortal.Services;
using System.Text;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using System.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;


namespace RFIDReaderPortal.Controllers
{
    public class RFIDController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IRFIDDiscoveryService _rfidDiscoveryService;
        private readonly IConfiguration _configuration;
        private readonly ITcpListenerService _tcpListenerService;
        private readonly string _baseUrl;
        private readonly ILogger<ApiService> _logger;
        private static Dictionary<string, RfidData> _latestRfidData = new Dictionary<string, RfidData>();
        private static DateTime _lastClearTime = DateTime.MinValue;

        public RFIDController(
             IApiService apiService,
             IRFIDDiscoveryService rfidDiscoveryService,
             IConfiguration configuration,
             ITcpListenerService tcpListenerService,
             ILogger<ApiService> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _rfidDiscoveryService = rfidDiscoveryService ?? throw new ArgumentNullException(nameof(rfidDiscoveryService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _tcpListenerService = tcpListenerService ?? throw new ArgumentNullException(nameof(tcpListenerService));
           _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _baseUrl = configuration["ApiBaseUrl"];
            //// Use IConfiguration to get the ApiBaseUrl
            //_baseUrl = _configuration["ApiBaseUrl:Azure"] ?? _configuration["ApiBaseUrl:Local"];
            //if (string.IsNullOrEmpty(_baseUrl))
            //{
            //    throw new InvalidOperationException("ApiBaseUrl is not configured in the application settings.");
            //}
        }


        public async Task<IActionResult> Configuration()
        {
            try
            {
                var httpClient = new HttpClient();
                ApiService apiservice = new ApiService(httpClient, _configuration, _logger);
                string accessToken = Request.Cookies["accesstoken"];
                string userid = Request.Cookies["UserId"];
                string recruitid = Request.Cookies["recruitid"];
                string deviceid = Request.Cookies["DeviceId"];
                string ipaddress = Request.Cookies["IpAddress"];
                string sesionid = Request.Cookies["sessionid"];

                dynamic events = await apiservice.GetAllRecruitEventsAsync(accessToken, userid, recruitid, sesionid, ipaddress);
                dynamic responsemodel = events.outcome;
                IEnumerable<RecruitmentEventDto> eventData;

                if (events.data is JObject dataObject)
                {
                    eventData = new List<RecruitmentEventDto> { dataObject.ToObject<RecruitmentEventDto>() };
                }
                else if (events.data is JArray eventDataArray)
                {
                    eventData = eventDataArray.ToObject<List<RecruitmentEventDto>>();
                }
                else
                {
                    throw new InvalidOperationException("Unexpected data type received from API");
                }

                string newtoken = responsemodel?.tokens?.ToString();
                if (!string.IsNullOrEmpty(newtoken))
                {
                    ViewBag.Tokens = newtoken;
                    Response.Cookies.Append("accesstoken", newtoken);
                    accessToken = newtoken;
                }

                // Updated to use the new method that returns status message
                var (readerIPs, statusMessage) = await _rfidDiscoveryService.DiscoverRFIDReadersAsync();

                DeviceConfigurationDto model = new DeviceConfigurationDto();

                if (readerIPs.Any())
                {
                    // If IPs are found, use the first one as DeviceId
                    model.DeviceId = readerIPs.First();
                    model.statusmessage = statusMessage; // Add status message to model
                }
                else
                {
                    // If no IPs are found, handle accordingly
                    model.DeviceId = "No device found"; // Or set to null or any fallback value
                    model.statusmessage = statusMessage;
                    ViewBag.StatusMessage = statusMessage; // Set status message to ViewBag for immediate access
                }

                // Assign other properties like RecruitId, UserId
                model.RecruitId = recruitid;
                model.UserId = userid;

                // Save the deviceid in cookies
                deviceid = model.DeviceId;
                Response.Cookies.Append("DeviceId", deviceid);

                // Set additional ViewBag values for UserId and RecruitId
                ViewBag.UserId = Request.Cookies["UserId"];
                ViewBag.RecruitId = Request.Cookies["recruitid"];

                dynamic getAsyncResponse = await _apiService.GetAsync(accessToken, userid, deviceid, sesionid, ipaddress);

                string newTokenFromGetAsync = getAsyncResponse?.outcome?.tokens?.ToString();
                if (!string.IsNullOrEmpty(newTokenFromGetAsync))
                {
                    ViewBag.Tokens = newTokenFromGetAsync;
                    Response.Cookies.Append("accesstoken", newTokenFromGetAsync);
                    accessToken = newTokenFromGetAsync;
                }

                List<DeviceConfigurationDto> ipDataResponse = new List<DeviceConfigurationDto>();
                if (getAsyncResponse?.data is JArray dataArray)
                {
                    ipDataResponse = dataArray.ToObject<List<DeviceConfigurationDto>>() ?? new List<DeviceConfigurationDto>();
                }

                foreach (var item in ipDataResponse)
                {
                    //if (!string.IsNullOrEmpty(item.EventId))
                    //    Response.Cookies.Append("EventId", item.eventName);
                    if (!string.IsNullOrEmpty(item.Location))
                        Response.Cookies.Append("Location", item.Location);
                    if (!string.IsNullOrEmpty(item.eventName))
                        Response.Cookies.Append("EventName", item.EventId);
                }

                if (ipDataResponse.Count == 0)
                {
                    var viewModel = new RFIDViewModel
                    {
                        Events = eventData,
                        ReaderIPs = readerIPs,
                        StatusMessage=statusMessage
                    };

                    return View("Index", viewModel);
                }
                else
                {
                    var viewModel1 = new RFIDViewModel
                    {
                        IPDataResponse = ipDataResponse
                    };
                    return View("Reader", viewModel1);
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error occurred in Configuration");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }


        [HttpPost]
        [Consumes("application/json")] // Expecting JSON content
        public async Task<IActionResult> SubmitButton([FromBody] DeviceConfigurationDto formData)
        {
            try
            {
                string accessToken = Request.Cookies["accesstoken"];
                string ipaddress = Request.Cookies["IpAddress"];
                string sesionid = Request.Cookies["sessionid"];

                if (string.IsNullOrEmpty(accessToken))
                {
                    return BadRequest("Access token is missing.");
                }

                ViewData["AccessToken"] = accessToken;

                if (formData == null ||
                    string.IsNullOrEmpty(formData.DeviceId) ||
                    string.IsNullOrEmpty(formData.EventId) ||
                    string.IsNullOrEmpty(formData.Location) ||
                    string.IsNullOrEmpty(formData.UserId) ||
                    string.IsNullOrEmpty(formData.RecruitId))
                {
                    return BadRequest("All input fields are required.");
                }

                dynamic InsertRFID = await _apiService.InsertDeviceConfigurationAsync(accessToken, formData, sesionid, ipaddress);

                string newTokenFromGetAsync = InsertRFID?.outcome?.tokens?.ToString();
                if (!string.IsNullOrEmpty(newTokenFromGetAsync))
                {
                    ViewBag.Tokens = newTokenFromGetAsync;
                    Response.Cookies.Append("accesstoken", newTokenFromGetAsync);
                    accessToken = newTokenFromGetAsync;
                }

                string Eventid = formData.EventId;
                string Location = formData.Location;
                Response.Cookies.Append("EventName", Eventid);
                Response.Cookies.Append("Location", Location);
                return Json(new { success = true, redirectUrl = Url.Action("Reader", "RFID") });
            }
            catch (Exception ex)
            {
              //  _logger.LogError(ex, "Error occurred in SubmitButton");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RFID(EventModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", await PrepareViewModel());
            }

            try
            {
                string accessToken = Request.Cookies["accesstoken"];

                //await _apiService.ProcessRFIDEventAsync(model, accessToken);

                return View("Index", await PrepareViewModel());
            }
            catch (Exception ex)
            {
                // Log the exception
                ModelState.AddModelError(string.Empty, "An error occurred while processing your request.");
                return View("Index", await PrepareViewModel());
            }
        }

        private async Task<RFIDViewModel> PrepareViewModel()
        {
            string accessToken = Request.Cookies["accesstoken"];

            string userid = Request.Cookies["UserId"];
            string recruitid = Request.Cookies["recruitid"];
            string ipaddress = Request.Cookies["IpAddress"];
            string sesionid = Request.Cookies["sessionid"];
            //string recConfId = Request.Cookies["recruitid"];
            //string categoryid = Request.Cookies["categoryId"];
            //string deviceid = Request.Cookies["DeviceId"]; 
            //dynamic recruitmentsData = await _apiService.GetAllRecruitmentsAsync(accessToken);

            dynamic getRecResponse = await _apiService.GetAllRecruitmentsAsync(accessToken, userid, recruitid, sesionid, ipaddress);
            dynamic responsemodel = getRecResponse.outcome;
            //dynamic responsemdata = events.data;
            JArray responsemdata = (JArray)getRecResponse.data;
            IEnumerable<RecruitmentEventDto> data = responsemdata.ToObject<List<RecruitmentEventDto>>();

            string newtoken = responsemodel.tokens;

            if (!string.IsNullOrEmpty(newtoken))
            {
                ViewBag.Tokens = newtoken;
                Response.Cookies.Append("accesstoken", newtoken);
            }

            dynamic events = await _apiService.GetAllRecruitEventsAsync(accessToken, userid, recruitid, sesionid, ipaddress);
            dynamic responsemodel1 = events.outcome;
            JArray responsemdata1 = (JArray)events.data;
            IEnumerable<RecruitmentEventDto> eventdata = responsemdata1.ToObject<List<RecruitmentEventDto>>();

            string newtoken1 = responsemodel1.tokens;

            var readerIPs = await _rfidDiscoveryService.DiscoverRFIDReadersAsync();

            if (!string.IsNullOrEmpty(newtoken1))
            {
                ViewBag.Tokens = newtoken1;
                Response.Cookies.Append("accesstoken", newtoken1);
            }
            return new RFIDViewModel
            {
                Recruitments = getRecResponse,
                ReaderIPs = readerIPs.IpAddresses,
                StatusMessage = readerIPs.StatusMessage
            };
        }

        [HttpPost]
        public async Task<IActionResult> Stop()
        {
            await _tcpListenerService.InsertStoredRfidDataAsync(); // Call method to insert data

            return Json(new { success = true, message = "RFID data inserted successfully." });
        }

        public async Task<IActionResult> Reader()
        {
            try
            {
                string accessToken = Request.Cookies["accesstoken"];
                string userid = Request.Cookies["UserId"];
                string recruitid = Request.Cookies["recruitid"];
                string deviceId = Request.Cookies["DeviceId"];
                string location = Request.Cookies["Location"];
                string eventName = Request.Cookies["EventId"];
                string ipaddress = Request.Cookies["IpAddress"];
                string sesionid = Request.Cookies["sessionid"];

                if (!_tcpListenerService.IsRunning)
                {
                    _tcpListenerService.SetParameters(accessToken, userid, recruitid, deviceId, location, eventName, sesionid, ipaddress);
                    _tcpListenerService.Start();
                }

                ViewBag.IsRunning = _tcpListenerService.IsRunning;

                var rfidDataArray = _tcpListenerService.GetReceivedData();

                dynamic getAsyncResponse = await _apiService.GetAsync(accessToken, userid, deviceId, sesionid, ipaddress);

                // Handle token refresh if provided
                if (getAsyncResponse?.outcome?.tokens != null)
                {
                    string newToken = getAsyncResponse.outcome.tokens.ToString();
                    Response.Cookies.Append("accesstoken", newToken);
                }

                // Convert the dynamic data to List<DeviceConfigurationDto>
                List<DeviceConfigurationDto> ipDataResponse = new List<DeviceConfigurationDto>();
                if (getAsyncResponse?.data != null)
                {
                    ipDataResponse = JsonConvert.DeserializeObject<List<DeviceConfigurationDto>>(
                        JsonConvert.SerializeObject(getAsyncResponse.data)
                    );
                }

                // Create a list of RecruitmentEventDto for the eventname property
                IEnumerable<RecruitmentEventDto> eventsList = new List<RecruitmentEventDto>();
                if (!string.IsNullOrEmpty(eventName))
                {
                    eventsList = new List<RecruitmentEventDto>
            {
                new RecruitmentEventDto { eventName = eventName }
            };
                }
                else if (ipDataResponse.Any() && !string.IsNullOrEmpty(ipDataResponse[0].EventId))
                {
                    eventsList = new List<RecruitmentEventDto>
            {
                new RecruitmentEventDto { eventName = ipDataResponse[0].EventId }
            };
                }

                var viewModel = new RFIDViewModel
                {
                    RfidDataArray = rfidDataArray,
                    IsRunning = _tcpListenerService.IsRunning,
                    IPDataResponse = ipDataResponse,
                    eventname = eventsList // Now correctly typed as IEnumerable<RecruitmentEventDto>
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "Error occurred in Reader action");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [HttpPost]
        public IActionResult ClearData()
        {
            _tcpListenerService.ClearData();
            return Json(new { success = true, message = "Data cleared successfully." });
        }

        //[HttpGet]
        //public ActionResult GetData()
        //{
        //    string accessToken = Request.Cookies["accesstoken"];
        //    string userid = Request.Cookies["UserId"];
        //    string recruitid = Request.Cookies["recruitid"];
        //    string deviceId = Request.Cookies["DeviceId"];
        //    string location = Request.Cookies["Location"];
        //    string eventName = Request.Cookies["EventName"];
        //    string ipaddress = Request.Cookies["IpAddress"];
        //    string sesionid = Request.Cookies["sessionid"];

        //    _tcpListenerService.SetParameters(accessToken, userid, recruitid, deviceId, location, eventName,ipaddress, sesionid);

        //    //_tcpListenerService.Start();
        //    if (!_tcpListenerService.IsRunning)
        //    {
        //        _tcpListenerService.Start();
        //    }

        //    var rfidDataArray = _tcpListenerService.GetReceivedData();

        //    var hexStringArray = _tcpListenerService.GetHexData();

        //    Console.WriteLine($"GetData called. Data count: {rfidDataArray.Length}");

        //    return Json(new
        //    {
        //        rfidDataArray = rfidDataArray.Select(item => new
        //        {
        //            TagId = item.TagId,
        //            Timestamp = item.Timestamp.ToString("HH:mm:ss:fff")
        //        }),
        //        count = rfidDataArray.Length,
        //        isRunning = _tcpListenerService.IsRunning,
        //        hexString = hexStringArray
        //    });
        //}
        [HttpGet]
        public ActionResult GetData()
        {
            string accessToken = Request.Cookies["accesstoken"];
            string userid = Request.Cookies["UserId"];
            string recruitid = Request.Cookies["recruitid"];
            string deviceId = Request.Cookies["DeviceId"];
            string location = Request.Cookies["Location"];
            string eventName = Request.Cookies["EventName"];
            string eventId = Request.Cookies["EventId"];
            string ipaddress = Request.Cookies["IpAddress"];
            string sesionid = Request.Cookies["sessionid"];

            _tcpListenerService.SetParameters(accessToken, userid, recruitid, deviceId, location, eventName, ipaddress, sesionid);

            // Ensure the listener is running
            if (!_tcpListenerService.IsRunning)
            {
                _tcpListenerService.Start();
            }

            // Fetch full RFID data (including LapTimes)
            var rfidDataArray = _tcpListenerService.GetReceivedData();
            var hexStringArray = _tcpListenerService.GetHexData();

            Console.WriteLine($"GetData called. Data count: {rfidDataArray.Length}");

            return Json(new
            {
                rfidDataArray = rfidDataArray.Select(item =>
                {
                    var lastLapTime = item.LapTimes.LastOrDefault();

                    return new
                    {
                        tagId = item.TagId,

                        // Send ALL lap timestamps
                        lapTimes = item.LapTimes
                            .Select(t => t.ToString("HH:mm:ss:fff"))
                            .ToList(),

                        lapCount = item.LapTimes.Count,

                        // Safely handle empty LapTimes
                        lastLap = lastLapTime == default(DateTime)
                            ? null
                            : lastLapTime.ToString("HH:mm:ss:fff")
                    };
                }).ToList(),

                count = rfidDataArray.Length,
                isRunning = _tcpListenerService.IsRunning,
                hexString = hexStringArray
            });
        }



        [HttpPost]
        public async Task<IActionResult> Reset()
        {
            try
            {
                string accessToken = Request.Cookies["accesstoken"];
                string userid = Request.Cookies["UserId"];
                string recruitid = Request.Cookies["recruitid"];
                string deviceId = Request.Cookies["DeviceId"];
                string location = Request.Cookies["Location"];
                string eventName = Request.Cookies["EventName"];
                string ipaddress = Request.Cookies["IpAddress"];
                string sessionid = Request.Cookies["sessionid"];

                // Call the service method to get a strongly-typed response
                var InsertRFID = await _apiService.DeleteRFIDRecordsAsync(accessToken, userid, recruitid, deviceId, location, eventName, sessionid, ipaddress);

                // Extract the token and update the cookies
                if (InsertRFID?.outcome != null && !string.IsNullOrEmpty(InsertRFID.outcome.tokens))
                {
                    ViewBag.Tokens = InsertRFID.outcome.tokens;
                    Response.Cookies.Append("accesstoken", InsertRFID.outcome.tokens);
                    accessToken = InsertRFID.outcome.tokens;
                }

                // Check if the operation was successful
                //if (InsertRFID?.outcome?.success == true)
                //{
                _tcpListenerService.Stop();
                _latestRfidData.Clear();
                _lastClearTime = DateTime.MinValue;

                return Json(new { success = true, message = "RFID records deleted successfully and listener reset." });
                //}
                //else
                //{
                //    return Json(new { success = false, message = "Failed to delete RFID records." });
                //}
            }
            catch (Exception ex)
            {
              //  _logger.LogError(ex, "Error occurred while deleting RFID records");
                return StatusCode(500, new { success = false, message = "An error occurred while deleting RFID records." });
            }
        }
    }
}