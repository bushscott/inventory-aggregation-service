using System.Text.Json;
using InventoryAggregation.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryAggregation.Tests;

public class InventoryApiClientTests
{
    private const string EmptyPage = """
        { "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 0, "records": [] }
        """;

    [Fact]
    public async Task RequestsPageOneAtTheConfiguredPageSize()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(EmptyPage);
        await using var app = TestApp.Create(pageSize: 10, inventoryApi: inventoryApi);

        await FetchFirstPage(app);

        var request = Assert.Single(inventoryApi.Requests);
        Assert.Equal("http://inventory-api.test/api/records?page=1&pageSize=10", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task RequestsPageSize25WhenNoPageSizeIsConfigured()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(EmptyPage);
        await using var app = TestApp.Create(pageSize: null, inventoryApi: inventoryApi);

        await FetchFirstPage(app);

        var request = Assert.Single(inventoryApi.Requests);
        Assert.Equal("http://inventory-api.test/api/records?page=1&pageSize=25", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task SendsTheApiKeyInTheXApiKeyHeader()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(EmptyPage);
        await using var app = TestApp.Create(apiKey: "secret-key-123", inventoryApi: inventoryApi);

        await FetchFirstPage(app);

        var request = Assert.Single(inventoryApi.Requests);
        Assert.Equal(["secret-key-123"], request.Headers.GetValues("X-Api-Key"));
    }

    [Fact]
    public async Task ReturnsTheRecordsOnTheFirstPage()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith("""
            {
              "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 2,
              "records": [
                { "id": 1001, "name": "Desk Lamp", "category": " home & kitchen", "price": 19.99, "quantity": 3, "lastUpdated": "2026-09-18T21:02:00Z" },
                { "id": 1002, "name": null, "category": "Toys", "price": 4.5, "quantity": 0, "lastUpdated": null }
              ]
            }
            """);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        var page = await FetchFirstPage(app);

        Assert.Equal(
            [
                new InventoryRecord(1001, "Desk Lamp", " home & kitchen", 19.99m, 3,
                    new DateTimeOffset(2026, 9, 18, 21, 2, 0, TimeSpan.Zero)),
                new InventoryRecord(1002, Name: null, "Toys", 4.5m, 0, LastUpdated: null),
            ],
            page.Records);
    }

    [Theory]
    [InlineData("null", "$")]
    [InlineData("""{ "records": null }""", "$.records")]
    [InlineData(
        """{ "records": [{ "id": 1001, "name": "Tent", "category": "Outdoor", "price": 19.99, "quantity": 1, "lastUpdated": null }, null] }""",
        "$.records[1]")]
    public async Task FailsAsAnInvalidDatasetNamingWhereThePageItsRecordsOrARecordIsNull(string body, string location)
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(body);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        var error = await Assert.ThrowsAsync<InvalidDatasetException>(() => FetchFirstPage(app));

        Assert.Equal($"The Inventory API's response has null at {location}.", error.Message);
    }

    [Theory]
    [InlineData(
        """{ "records": [{ "id": 1001, "name": "Tent", "category": "Outdoor", "price": "19.99", "quantity": 1, "lastUpdated": null }] }""",
        "$.records[0].price")]
    [InlineData(
        """{ "records": [{ "id": 1001, "name": "Tent", "category": "Outdoor", "price": 19.99, "lastUpdated": null }] }""",
        "$.records[0]")]
    [InlineData(
        """{ "records": [{ "id": 1001, "category": "Outdoor", "price": 19.99, "quantity": 1, "lastUpdated": null }] }""",
        "$.records[0]")]
    [InlineData(
        """{ "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 0 }""",
        "$")]
    public async Task FailsAsAnInvalidDatasetOnANumberWrittenAsTextOrAMissingField(string body, string location)
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(body);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        var error = await Assert.ThrowsAsync<InvalidDatasetException>(() => FetchFirstPage(app));

        Assert.Equal($"The Inventory API's response could not be read at {location}.", error.Message);
        Assert.IsType<JsonException>(error.InnerException);
    }

    /// <summary>Calls the client the running app would use, with its real HttpClient setup.</summary>
    private static Task<InventoryPage> FetchFirstPage(WebApplicationFactory<Program> app) =>
        app.Services.GetRequiredService<IInventoryApiClient>().FetchFirstPageAsync(CancellationToken.None);
}
