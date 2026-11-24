using InventoryAggregation.Api;
using Microsoft.Extensions.Options;
using Polly.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<InventoryApiOptions>()
    .BindConfiguration(InventoryApiOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient(InventoryApiClient.HttpClientName, (services, httpClient) =>
{
    var options = services.GetRequiredService<IOptions<InventoryApiOptions>>().Value;
    httpClient.BaseAddress = new Uri(options.BaseUrl);
    httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
})
// Microsoft's defaults: up to 3 retries on 429, 5xx, 408, timeouts and connection failures,
// honouring Retry-After, with 10 seconds per attempt and 30 seconds per request in total.
.AddStandardResilienceHandler();
// The resilience library logs the attempt that uses up the last retry as an error. The central
// exception handler logs that failure as an error already, with its full cause, so the library's
// own entries stay at warning level at most.
builder.Services.Configure<TelemetryOptions>(telemetry =>
    telemetry.SeverityProvider = arguments =>
        arguments.Event.Severity > ResilienceEventSeverity.Warning
            ? ResilienceEventSeverity.Warning
            : arguments.Event.Severity);
builder.Services.AddSingleton<IInventoryApiClient, InventoryApiClient>();
builder.Services.AddSingleton<DatasetValidator>();
builder.Services.AddSingleton<InventoryAggregator>();
builder.Services.AddSingleton<InventorySummaryService>();

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
// Our handler writes every error response, but UseExceptionHandler() refuses to start without this.
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();

app.Run();
