using Azure.Monitor.OpenTelemetry.AspNetCore;
using TwentyTwenty.Storage.Azure;
using TwentyTwenty.Storage.Local;
using TwentyTwenty.Storage;
using Ci.Extension.Core;
using ScribbleOpeAIAnalysis.Services;
using ScribbleOpeAIAnalysis.Model;

var builder = WebApplication.CreateBuilder(args);

// Retrieve the application configuration.
var configuration = builder.Configuration;

// Register MVC Controllers and Views.
builder.Services.AddControllersWithViews(); // Adds support for MVC controllers and views.
builder.Services.AddRazorPages(); // Adds support for Razor Pages.
builder.Services.AddServerSideBlazor(); // Adds support for Blazor server-side components.
builder.Services.AddControllers(); // Adds support for API controllers.

// Bind GitHubOptions from appsettings.json.
builder.Services.Configure<GitHubOption>(builder.Configuration.GetSection("GitHub"));

// Register GitHubService as a singleton for dependency injection.
builder.Services.AddSingleton<GitHubService>();

// Configure Swagger/OpenAPI for API documentation.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HttpClient for making HTTP requests.
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<GitHubService>();

// Register FeedbackService as a singleton for dependency injection.
builder.Services.AddSingleton<FeedbackService>();

// Register HttpContextAccessor for accessing HTTP context.
builder.Services.AddHttpContextAccessor();

// Determine the storage type from configuration and register the appropriate storage provider.
var storageType = configuration.GetValue<string>("Storage:Type");
switch (storageType)
{
    case "Azure":
        // Retrieve Azure storage connection string from configuration.
        var azureConnStr = configuration.GetValue<string>("Storage:Azure:ConnectionString");
        if (azureConnStr.IsNullOrWhiteSpace())
            throw new ArgumentNullException(nameof(azureConnStr));
        // Register AzureStorageProvider as the storage provider.
        builder.Services.AddSingleton<IStorageProvider, AzureStorageProvider>(provider =>
            new AzureStorageProvider(new AzureProviderOptions() { ConnectionString = azureConnStr }));
        break;
    default:
        // Register LocalStorageProvider as the storage provider.
        builder.Services.AddSingleton<IStorageProvider, LocalStorageProvider>(provider =>
            new LocalStorageProvider(Path.Combine(builder.Environment.WebRootPath, "")));
        break;
}

// Configure OpenTelemetry with Azure Monitor for metrics and tracing.
builder.Services.AddOpenTelemetry()
    .WithMetrics() // Enables metrics collection.
    .WithTracing() // Enables tracing.
    .UseAzureMonitor(o =>
    {
        // Set the connection string for Azure Monitor.
        o.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
    });

// Build the application.
var app = builder.Build();

// Enable Swagger UI for API documentation.
app.UseSwagger();
app.UseSwaggerUI();

// Enable HTTPS redirection.
app.UseHttpsRedirection();

// Enable serving static files.
app.UseStaticFiles();

// Enable authorization middleware.
app.UseAuthorization();

// Map the default controller route.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Run the application.
app.Run();