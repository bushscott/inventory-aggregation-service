using System.Net;
using System.Text.Json;
using InventoryAggregation.Api;

namespace InventoryAggregation.Tests;

public class InventoryApiClientTests
{
    private const string EmptyPage = """
        { "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 0, "records": [] }
        """;

    [Fact]
    public async Task MakesExactlyOneRequestWhenTheDatasetFitsOnOnePage()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(EmptyPage);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        await TestApp.FetchAllPages(app);

        Assert.Single(inventoryApi.Requests);
    }

    [Fact]
    public async Task RequestsEveryPageOnceInOrderAtTheConfiguredPageSize()
    {
        var inventoryApi = new FakeInventoryApi()
            .RespondsWith(PageWithOneRecord(page: 1, totalPages: 3, recordId: 1001))
            .RespondsWith(PageWithOneRecord(page: 2, totalPages: 3, recordId: 1002))
            .RespondsWith(PageWithOneRecord(page: 3, totalPages: 3, recordId: 1003));
        await using var app = TestApp.Create(pageSize: 10, inventoryApi: inventoryApi);

        var pages = await TestApp.FetchAllPages(app);

        Assert.Equal(
            [
                "http://inventory-api.test/api/records?page=1&pageSize=10",
                "http://inventory-api.test/api/records?page=2&pageSize=10",
                "http://inventory-api.test/api/records?page=3&pageSize=10",
            ],
            inventoryApi.Requests.Select(request => request.RequestUri?.ToString()));
        Assert.Equal([1001, 1002, 1003], pages.SelectMany(page => page.Records).Select(record => record.Id));
    }

    [Fact]
    public async Task RetriesARateLimitedPageAndCarriesOnWithTheRest()
    {
        // Against the real Inventory API the sixth request in a window is rate-limited like this.
        var inventoryApi = new FakeInventoryApi()
            .RespondsWith(PageWithOneRecord(page: 1, totalPages: 3, recordId: 1001))
            .RespondsWithStatus(HttpStatusCode.TooManyRequests, retryAfter: TimeSpan.Zero)
            .RespondsWith(PageWithOneRecord(page: 2, totalPages: 3, recordId: 1002))
            .RespondsWith(PageWithOneRecord(page: 3, totalPages: 3, recordId: 1003));
        await using var app = TestApp.Create(pageSize: 10, inventoryApi: inventoryApi);

        var pages = await TestApp.FetchAllPages(app);

        Assert.Equal(
            [
                "http://inventory-api.test/api/records?page=1&pageSize=10",
                "http://inventory-api.test/api/records?page=2&pageSize=10",
                "http://inventory-api.test/api/records?page=2&pageSize=10",
                "http://inventory-api.test/api/records?page=3&pageSize=10",
            ],
            inventoryApi.Requests.Select(request => request.RequestUri?.ToString()));
        Assert.Equal([1001, 1002, 1003], pages.SelectMany(page => page.Records).Select(record => record.Id));
    }

    [Fact]
    public async Task RequestsPageSize25WhenNoPageSizeIsConfigured()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(EmptyPage);
        await using var app = TestApp.Create(pageSize: null, inventoryApi: inventoryApi);

        await TestApp.FetchAllPages(app);

        var request = Assert.Single(inventoryApi.Requests);
        Assert.Equal("http://inventory-api.test/api/records?page=1&pageSize=25", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task SendsTheApiKeyInTheXApiKeyHeader()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(EmptyPage);
        await using var app = TestApp.Create(apiKey: "secret-key-123", inventoryApi: inventoryApi);

        await TestApp.FetchAllPages(app);

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

        var pages = await TestApp.FetchAllPages(app);

        Assert.Equal(
            [
                new InventoryRecord(1001, "Desk Lamp", " home & kitchen", 19.99m, 3,
                    new DateTimeOffset(2026, 9, 18, 21, 2, 0, TimeSpan.Zero)),
                new InventoryRecord(1002, Name: null, "Toys", 4.5m, 0, LastUpdated: null),
            ],
            Assert.Single(pages).Records);
    }

    [Theory]
    [InlineData("null", "$")]
    [InlineData("""{ "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 1, "records": null }""", "$.records")]
    [InlineData(
        """{ "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 1, "records": [{ "id": 1001, "name": "Tent", "category": "Outdoor", "price": 19.99, "quantity": 1, "lastUpdated": null }, null] }""",
        "$.records[1]")]
    public async Task FailsAsAnInvalidDatasetNamingWhereThePageItsRecordsOrARecordIsNull(string body, string location)
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(body);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        var error = await Assert.ThrowsAsync<InvalidDatasetException>(() => TestApp.FetchAllPages(app));

        Assert.Equal($"The Inventory API's page 1 has null at {location}.", error.Message);
    }

    [Theory]
    [InlineData(
        """{ "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 1, "records": [{ "id": 1001, "name": "Tent", "category": "Outdoor", "price": "19.99", "quantity": 1, "lastUpdated": null }] }""",
        "$.records[0].price")]
    [InlineData(
        """{ "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 1, "records": [{ "id": 1001, "name": "Tent", "category": "Outdoor", "price": 19.99, "lastUpdated": null }] }""",
        "$.records[0]")]
    [InlineData(
        """{ "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 1, "records": [{ "id": 1001, "category": "Outdoor", "price": 19.99, "quantity": 1, "lastUpdated": null }] }""",
        "$.records[0]")]
    [InlineData(
        """{ "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 0 }""",
        "$")]
    public async Task FailsAsAnInvalidDatasetOnANumberWrittenAsTextOrAMissingField(string body, string location)
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith(body);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        var error = await Assert.ThrowsAsync<InvalidDatasetException>(() => TestApp.FetchAllPages(app));

        Assert.Equal($"The Inventory API's page 1 could not be read at {location}.", error.Message);
        Assert.IsType<JsonException>(error.InnerException);
    }

    /// <summary>
    /// A page as the Inventory API sends it, holding one record with this id. With one record a
    /// page, the total record count equals the page count.
    /// </summary>
    private static string PageWithOneRecord(int page, int totalPages, int recordId) => $$"""
        {
          "page": {{page}}, "pageSize": 10, "totalPages": {{totalPages}}, "totalRecords": {{totalPages}},
          "records": [
            { "id": {{recordId}}, "name": "Tent", "category": "Outdoor", "price": 80.00, "quantity": 1, "lastUpdated": null }
          ]
        }
        """;
}
