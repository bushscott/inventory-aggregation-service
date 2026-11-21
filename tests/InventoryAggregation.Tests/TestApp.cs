using InventoryAggregation.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryAggregation.Tests;

/// <summary>
/// Runs the whole service in memory with the settings a test chooses. These settings are
/// added last, so they win over any user secrets or environment variables on the machine.
/// </summary>
internal static class TestApp
{
    /// <param name="pageSize">
    /// The page size setting. Null leaves the setting out, so the default applies (or a page size
    /// in your user secrets, if you set one). Setting it to null instead would bind as 0.
    /// </param>
    /// <param name="inventoryApi">
    /// A fake Inventory API to send the service's requests to, in place of the network.
    /// </param>
    public static WebApplicationFactory<Program> Create(
        string baseUrl = "http://inventory-api.test",
        string apiKey = "test-api-key",
        int? pageSize = null,
        FakeInventoryApi? inventoryApi = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
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
        });
}
