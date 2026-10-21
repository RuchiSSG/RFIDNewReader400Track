
using Microsoft.AspNetCore.Mvc.Razor;
using RFIDReaderPortal.Models;
using RFIDReaderPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration.AddJsonFile(
    "appsettings.json",
    optional: false,
    reloadOnChange: true);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.json",
    optional: true,
    reloadOnChange: true);

builder.Configuration.AddEnvironmentVariables();

// Logging
builder.Logging.AddConfiguration(
    builder.Configuration.GetSection("Logging"));

builder.Logging.AddConsole();

// Read API base URL
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "ApiBaseUrl is not configured in appsettings.json.");
}

Console.WriteLine(
    $"Environment: {builder.Environment.EnvironmentName}");

Console.WriteLine($"ApiBaseUrl: {apiBaseUrl}");

// Listen on HTTP port 5008 only.
// Binding to localhost makes the website accessible only
// from this PC.
builder.WebHost.UseUrls("http://localhost:5008");

// HTTP client for the external API
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add(
        "Accept",
        "application/json");
});

// MVC
builder.Services.AddControllersWithViews()
    .AddViewLocalization(
        LanguageViewLocationExpanderFormat.Suffix);

// API service
builder.Services.AddSingleton<IApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("ApiClient");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var serviceLogger =
        sp.GetRequiredService<ILogger<ApiService>>();

    return new ApiService(
        httpClient,
        configuration,
        serviceLogger);
});

// RFID services
builder.Services.AddScoped<
    IRFIDDiscoveryService,
    RFIDDiscoveryService>();

builder.Services.AddSingleton<
    ITcpListenerService,
    TcpListenerService>();

// API configuration
builder.Services.AddSingleton(
    new ApiConfig { BaseUrl = apiBaseUrl });

var app = builder.Build();

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

// IMPORTANT:
// Do not use app.UseHttpsRedirection() when this app
// is intentionally configured to use HTTP only.

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

Console.WriteLine(
    "RFID application starting on http://localhost:5008");

app.Run();
