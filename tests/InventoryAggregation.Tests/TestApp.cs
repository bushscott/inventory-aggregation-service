using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InventoryAggregation.Tests;

/// <summary>
/// Runs the whole service in memory with the settings a test chooses. These settings are
/// added last, so they win over any user secrets or environment variables on the machine.
/// </summary>
internal static class TestApp
{
    public static WebApplicationFactory<Program> Create(
        string baseUrl = "http://inventory-api.test",
        string apiKey = "test-api-key") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InventoryApi:BaseUrl"] = baseUrl,
                    ["InventoryApi:ApiKey"] = apiKey,
                })));
}
