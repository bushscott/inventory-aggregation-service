using System.Net;
using System.Text.Json.Nodes;

namespace InventoryAggregation.Tests;

public class InventorySummaryEndpointTests
{
    [Fact]
    public async Task ReturnsCategorySummariesAsJson()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith("""
            {
              "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 2,
              "records": [
                { "id": 1001, "name": "Desk Lamp", "category": " home & kitchen", "price": 19.99, "quantity": 3, "lastUpdated": "2026-09-18T21:02:00Z" },
                { "id": 1002, "name": "Kettle", "category": "HOME & KITCHEN", "price": 24.00, "quantity": 1, "lastUpdated": null }
              ]
            }
            """);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/inventory/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertJsonEqual(
            """
            {
              "categories": [
                {
                  "category": "Home & Kitchen",
                  "totalInventoryValue": 83.97,
                  "availableRecordCount": 2,
                  "mostRecentlyUpdated": { "id": 1001, "name": "Desk Lamp", "lastUpdated": "2026-09-18T21:02:00Z" },
                  "items": [
                    { "id": 1001, "name": "Desk Lamp", "price": 19.99, "quantity": 3, "lastUpdated": "2026-09-18T21:02:00Z" },
                    { "id": 1002, "name": "Kettle", "price": 24.00, "quantity": 1, "lastUpdated": null }
                  ]
                }
              ]
            }
            """,
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ReturnsLastUpdatedInUtcWithAZSuffix()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith("""
            {
              "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 1,
              "records": [
                { "id": 1001, "name": "Tent", "category": "Outdoor", "price": 80.00, "quantity": 1, "lastUpdated": "2026-09-18T23:02:00+02:00" }
              ]
            }
            """);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);
        using var client = app.CreateClient();

        var body = JsonNode.Parse(await client.GetStringAsync("/api/inventory/summary"));

        Assert.Equal("2026-09-18T21:02:00Z", (string?)body?["categories"]?[0]?["items"]?[0]?["lastUpdated"]);
    }

    /// <summary>
    /// Compares two JSON documents after rewriting both the same way, so layout and escaping
    /// (such as "&amp;" written as "&") do not matter, but names, values and order do.
    /// </summary>
    private static void AssertJsonEqual(string expected, string actual) =>
        Assert.Equal(JsonNode.Parse(expected)?.ToJsonString(), JsonNode.Parse(actual)?.ToJsonString());
}
