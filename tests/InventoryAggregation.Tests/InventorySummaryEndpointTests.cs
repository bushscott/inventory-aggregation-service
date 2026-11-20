using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InventoryAggregation.Tests;

public class InventorySummaryEndpointTests
{
    [Fact]
    public async Task ReturnsEmptyCategoryList()
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/inventory/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"categories":[]}""", await response.Content.ReadAsStringAsync());
    }
}
