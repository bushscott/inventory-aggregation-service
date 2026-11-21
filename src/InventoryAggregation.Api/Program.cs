using InventoryAggregation.Api;
using Microsoft.Extensions.Options;

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
});
builder.Services.AddSingleton<IInventoryApiClient, InventoryApiClient>();
builder.Services.AddSingleton<InventoryAggregator>();
builder.Services.AddSingleton<InventorySummaryService>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
