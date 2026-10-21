using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using RFIDReaderPortal.Models;
using RFIDReaderPortal.Services;

try
{
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

    // ApiService registration using a factory method
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

    // HTTP only (no certificate needed). 0.0.0.0 = reachable from other devices on the network.
    // Optionally override in appsettings.json with:  "AppUrls": "http://0.0.0.0:5008"
    var appUrls = builder.Configuration["AppUrls"] ?? "http://0.0.0.0:5008";
    builder.WebHost.UseUrls(appUrls);
    logger.LogInformation($"Listening on: {appUrls}");

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
        // HSTS and HTTPS redirection removed: app runs on HTTP only
    }

    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("STARTUP FAILED:");
    Console.WriteLine(ex);
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Press Enter to exit...");
    Console.ReadLine();
}