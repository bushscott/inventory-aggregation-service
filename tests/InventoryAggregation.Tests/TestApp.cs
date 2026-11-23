using InventoryAggregation.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryAggregation.Tests;

/// <summary>
/// Runs the whole service in memory with the settings a test chooses.
/// It runs in the "Testing" environment, where .NET does not load user secrets, so no test can
/// pick up your real API key or depend on what you have set. The settings are added last, so
/// they also win over environment variables.
/// </summary>
internal static class TestApp
{
    /// <param name="pageSize">
    /// The page size setting. Null leaves the setting out, so the default applies.
    /// (Setting the key to null instead would bind as 0.)
    /// </param>
    /// <param name="inventoryApi">
    /// A fake Inventory API to send the service's requests to, in place of the network.
    /// </param>
    /// <param name="logs">Collects what the service logs, in addition to the usual output.</param>
    public static WebApplicationFactory<Program> Create(
        string baseUrl = "http://inventory-api.test",
        string apiKey = "test-api-key",
        int? pageSize = null,
        FakeInventoryApi? inventoryApi = null,
        CapturedLogs? logs = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            var settings = new Dictionary<string, string?>
            {
                ["InventoryApi:BaseUrl"] = baseUrl,
                ["InventoryApi:ApiKey"] = apiKey,
            };
            if (pageSize is not null)
            {
                settings["InventoryApi:PageSize"] = pageSize.ToString();
            }

            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings));

            if (inventoryApi is not null)
            {
                builder.ConfigureTestServices(services =>
                    services.AddHttpClient(InventoryApiClient.HttpClientName)
                        .ConfigurePrimaryHttpMessageHandler(() => inventoryApi));
            }

            if (logs is not null)
            {
                builder.ConfigureLogging(logging => logging.AddProvider(logs));
            }
        });
}
