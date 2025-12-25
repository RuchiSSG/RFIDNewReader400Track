using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RFIDReaderPortal.Models;
using RFIDReaderPortal.Services;
using System.Text;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly IRFIDDiscoveryService _rfidDiscoveryService;
    private readonly string _baseUrl;

    public AccountController(IHttpClientFactory clientFactory, IConfiguration configuration, IRFIDDiscoveryService rfidDiscoveryService)
    {
        _clientFactory = clientFactory;
        _configuration = configuration;
        _rfidDiscoveryService = rfidDiscoveryService ?? throw new ArgumentNullException(nameof(rfidDiscoveryService));
        _baseUrl = configuration["ApiBaseUrl"];
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                model.SessionId = GenerateSessionId();
                model.Username = model.Email;
                string deviceid = "";

                var readerStatus = await _rfidDiscoveryService.DiscoverRFIDReadersAsync();
                List<DeviceConfigurationDto> data1 = readerStatus.IpAddresses
                 .Select(ip => new DeviceConfigurationDto
                 {

                     DeviceId = ip,                     // IP Address
                     statusmessage = readerStatus.StatusMessage // Status Message for all IPs
                 })
                 .ToList();

                // Check if no IP address is found, handle accordingly
                if (string.IsNullOrEmpty(deviceid) && readerStatus.IpAddresses.Any())
                {
                    // If IPs are found, set the first one to deviceid
                    deviceid = readerStatus.IpAddresses.First();
                }
                else if (string.IsNullOrEmpty(deviceid) && !readerStatus.IpAddresses.Any())
                {
                    // If no IP addresses are found, set a default value or handle the error
                    deviceid = "No IP address found";

                }
                // Assign the deviceid to model
                model.IpAddress = deviceid;




                var result = await LoginUser(model);

                if (result.IsSuccessStatusCode)
                {
                    var responseData = await result.Content.ReadAsStringAsync();
                    var rootObject = JsonConvert.DeserializeObject<dynamic>(responseData);
                    var responseModel = rootObject.result.outcome;
                    var responseData1 = rootObject.result.data;

                    if (responseModel.outcomeId == 1)
                    {
                        string accessToken = responseModel.tokens;
                        string recruitId = responseData1.RecruitId;
                        string userId = responseData1.UserId;

                        Response.Cookies.Append("UserId", userId);
                        Response.Cookies.Append("recruitid", recruitId);
                        Response.Cookies.Append("accesstoken", accessToken);
                        Response.Cookies.Append("sessionid", model.SessionId);
                        Response.Cookies.Append("IpAddress", model.IpAddress);

                        TempData["SuccessMessage"] = "Login successful! Welcome back.";
                        return RedirectToAction("Configuration", "RFID", new { accessToken });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Invalid username or password.";
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid username or password.";
                    ModelState.AddModelError(string.Empty, "An error occurred while processing your request.");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Please fill in all required fields.";
        }
        return View(model);
    }

    private async Task<HttpResponseMessage> LoginUser(LoginModel loginDto)
    {
        var client = _clientFactory.CreateClient("ApiClient");
        string data = JsonConvert.SerializeObject(loginDto);
        StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("Auth", content);
        return response;
    }

    private string GenerateSessionId()
    {
        return Guid.NewGuid().ToString();
    }
}