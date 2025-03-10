using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Configuration;
using TwentyTwenty.Storage.Azure;
using TwentyTwenty.Storage.Local;
using TwentyTwenty.Storage;
using Ci.Extension.Core;
using ScribbleOpeAIAnalysis.Services;
using ScribbleOpeAIAnalysis;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Register MVC Controllers and Views.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
// Register API Controllers.
builder.Services.AddControllers();

    // Bind GitHubOptions from appsettings.json
builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("GitHub"));

// Register GitHubService as a singleton
builder.Services.AddSingleton<GitHubService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HttpClient.
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<GitHubService>();
builder.Services.AddHttpContextAccessor();

var storageType = configuration.GetValue<string>("Storage:Type");
switch (storageType)
{
    case "Azure":
        var azureConnStr = configuration.GetValue<string>("Storage:Azure:ConnectionString");
        if (azureConnStr.IsNullOrWhiteSpace())
            throw new ArgumentNullException(nameof(azureConnStr));
        builder.Services.AddSingleton<IStorageProvider, AzureStorageProvider>(provider =>
            new AzureStorageProvider(new AzureProviderOptions() { ConnectionString = azureConnStr }));
        break;
    default:
        builder.Services.AddSingleton<IStorageProvider, LocalStorageProvider>(provider =>
            new LocalStorageProvider(Path.Combine(builder.Environment.WebRootPath, "")));
        break;
}

// Configure OpenTelemetry with Azure Monitor.
builder.Services.AddOpenTelemetry()
    .WithMetrics()
    .WithTracing()
    .UseAzureMonitor(o =>
    {
        o.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();

// Map default controller route.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();