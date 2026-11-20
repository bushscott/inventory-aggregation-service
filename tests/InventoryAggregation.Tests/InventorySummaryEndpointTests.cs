using System.Net;

namespace InventoryAggregation.Tests;

public class InventorySummaryEndpointTests
{
    [Fact]
    public async Task ReturnsEmptyCategoryList()
    {
        await using var app = TestApp.Create();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/inventory/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"categories":[]}""", await response.Content.ReadAsStringAsync());
    }
}
