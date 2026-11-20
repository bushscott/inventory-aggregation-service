namespace InventoryAggregation.Tests;

public class StartupConfigurationTests
{
    [Fact]
    public void FailsToStartWithoutBaseUrl()
    {
        using var app = TestApp.Create(baseUrl: "");

        var error = Assert.ThrowsAny<Exception>(() => app.CreateClient());

        Assert.Contains(
            """InventoryApi:BaseUrl is missing. Set it with: dotnet user-secrets set "InventoryApi:BaseUrl" "<base URL>" --project src/InventoryAggregation.Api""",
            error.Message);
    }

    [Fact]
    public void FailsToStartWithoutApiKey()
    {
        using var app = TestApp.Create(apiKey: "");

        var error = Assert.ThrowsAny<Exception>(() => app.CreateClient());

        Assert.Contains(
            """InventoryApi:ApiKey is missing. Set it with: dotnet user-secrets set "InventoryApi:ApiKey" "<API key>" --project src/InventoryAggregation.Api""",
            error.Message);
    }
}
