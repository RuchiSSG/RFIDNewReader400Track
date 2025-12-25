//using Microsoft.AspNetCore.Mvc.Razor;
//using Microsoft.Extensions.DependencyInjection;
//using RFIDReaderPortal.Models;
//using RFIDReaderPortal.Services;
//using System.Runtime.InteropServices;

//var builder = WebApplication.CreateBuilder(args);

//Console.WriteLine($"OS Description: {RuntimeInformation.OSDescription}");
//Console.WriteLine($"Is Windows: {RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}");

//// Load configuration
//builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
//builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
//builder.Configuration.AddEnvironmentVariables();

//// Setup logging
//builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
//builder.Logging.AddConsole();

//// Get logger
//var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

//// Get API base URL
//var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

//logger.LogInformation($"Environment: {builder.Environment.EnvironmentName}");
//logger.LogInformation($"ApiBaseUrl: {apiBaseUrl}");

//if (string.IsNullOrEmpty(apiBaseUrl))
//{
//    logger.LogError("ApiBaseUrl is not configured in the application settings.");
//    throw new InvalidOperationException("ApiBaseUrl is not configured in the application settings.");
//}

//// Add services to the container
//builder.Services.AddHttpClient("ApiClient", client =>
//{
//    client.BaseAddress = new Uri(apiBaseUrl);
//    client.DefaultRequestHeaders.Add("Accept", "application/json");
//});

//builder.Services.AddControllersWithViews()
//    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

//// Register API Service (only once, using factory)
//builder.Services.AddScoped<IApiService>(sp =>
//{
//    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
//    var httpClient = httpClientFactory.CreateClient("ApiClient");
//    var configuration = sp.GetRequiredService<IConfiguration>();
//    var serviceLogger = sp.GetRequiredService<ILogger<ApiService>>();
//    return new ApiService(httpClient, configuration, serviceLogger);
//});

//// Register RFID Discovery Service
//builder.Services.AddScoped<IRFIDDiscoveryService, RFIDDiscoveryService>();
//builder.Services.AddScoped<IApiService, ApiService>();
//builder.Services.AddSingleton<ITcpListenerService, TcpListenerService>();
////builder.Services.AddHostedService<RfidTcpBackgroundService>();

//// ⚠️ REMOVED OLD SERVICES - We're using UhfRfidService as static singleton in controller
//// builder.Services.AddSingleton<ITcpListenerService, TcpListenerService>();
//// builder.Services.AddScoped<UhfReaderService>();

//// Add ApiBaseUrl to DI container
//builder.Services.AddSingleton(new ApiConfig { BaseUrl = apiBaseUrl });

//var app = builder.Build();

//// ⚠️ REMOVED - No longer using TcpListenerService
//// var tcpListenerService = app.Services.GetRequiredService<ITcpListenerService>();
//// tcpListenerService.Start();

//Console.WriteLine("🚀 Application starting...");
//Console.WriteLine("📡 RFID reader will connect when you visit /RFID/Reader page");

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();
//}
//else
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Account}/{action=Login}/{id?}");

//app.Run();
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using RFIDReaderPortal.Models;
using RFIDReaderPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Setup logging
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();

// Get logger
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

// Get API base URL
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

// Log configuration information
logger.LogInformation($"Environment: {builder.Environment.EnvironmentName}");
logger.LogInformation($"ApiBaseUrl: {apiBaseUrl}");

//// Determine which API base URL to use
//var apiBaseUrl = builder.Environment.IsDevelopment()
//    ? builder.Configuration["ApiBaseUrl:Azure"]
//    : builder.Configuration["ApiBaseUrl:Local"];

//// Log configuration information
//logger.LogInformation($"Environment: {builder.Environment.EnvironmentName}");
//logger.LogInformation($"ApiBaseUrl:Local: {builder.Configuration["ApiBaseUrl:Local"]}");
//logger.LogInformation($"ApiBaseUrl:Azure: {builder.Configuration["ApiBaseUrl:Azure"]}");
//logger.LogInformation($"Selected ApiBaseUrl: {apiBaseUrl}");

if (string.IsNullOrEmpty(apiBaseUrl))
{
    logger.LogError("ApiBaseUrl is not configured in the application settings.");
    throw new InvalidOperationException("ApiBaseUrl is not configured in the application settings.");
}

// Add services to the container.
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

// Modify the ApiService registration to use a factory method
builder.Services.AddSingleton<IApiService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("ApiClient");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var serviceLogger = sp.GetRequiredService<ILogger<ApiService>>();
    return new ApiService(httpClient, configuration, serviceLogger);
});

builder.Services.AddScoped<IRFIDDiscoveryService, RFIDDiscoveryService>();
builder.Services.AddSingleton<ITcpListenerService, TcpListenerService>();

// Add ApiBaseUrl to DI container
builder.Services.AddSingleton(new ApiConfig { BaseUrl = apiBaseUrl });

var app = builder.Build();

var tcpListenerService = app.Services.GetRequiredService<ITcpListenerService>();
//tcpListenerService.Start();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();