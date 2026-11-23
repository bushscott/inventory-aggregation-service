using System.Net;
using System.Text.Json.Nodes;
using InventoryAggregation.Api;
using Microsoft.Extensions.Logging;

namespace InventoryAggregation.Tests;

/// <summary>What our caller receives when the summary cannot be produced.</summary>
public class InventorySummaryErrorResponseTests
{
    private const string ApiKey = "test-api-key";

    /// <summary>A retried failure is tried this often: the first try and the standard handler's 3 retries.</summary>
    private const int AllAttempts = 4;

    [Fact]
    public async Task Returns503NamingTheStatusTheInventoryApiAnsweredWith()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWithStatus(HttpStatusCode.InternalServerError, times: AllAttempts);

        var response = await GetSummary(inventoryApi);

        await AssertProblemDetails(response, HttpStatusCode.ServiceUnavailable,
            title: "Inventory API unavailable",
            detail: "The Inventory API answered with status 500 (InternalServerError).");
    }

    [Fact]
    public async Task Returns503WhenTheInventoryApiCannotBeReached()
    {
        var inventoryApi = new FakeInventoryApi().Throws(
            new HttpRequestException(HttpRequestError.ConnectionError, "Connection refused (inventory-api.test:80)"),
            times: AllAttempts);

        var response = await GetSummary(inventoryApi);

        await AssertProblemDetails(response, HttpStatusCode.ServiceUnavailable,
            title: "Inventory API unavailable",
            detail: "The Inventory API could not be reached.");
    }

    [Fact]
    public async Task Returns503WhenEveryAttemptTimesOut()
    {
        var inventoryApi = new FakeInventoryApi().NeverResponds(times: AllAttempts);

        var response = await GetSummary(inventoryApi, attemptTimeout: TimeSpan.FromMilliseconds(100));

        await AssertProblemDetails(response, HttpStatusCode.ServiceUnavailable,
            title: "Inventory API unavailable",
            detail: "The Inventory API did not answer in time.");
    }

    [Fact]
    public async Task Returns503WhenHttpClientsOwnTimeoutElapses()
    {
        // What HttpClient throws when its own Timeout elapses. The resilience handler's timeouts
        // only cover waiting for the response headers, so this is what a body that stalls produces.
        var inventoryApi = new FakeInventoryApi().Throws(new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.",
            new TimeoutException()));

        var response = await GetSummary(inventoryApi);

        await AssertProblemDetails(response, HttpStatusCode.ServiceUnavailable,
            title: "Inventory API unavailable",
            detail: "The Inventory API did not answer in time.");
    }

    [Fact]
    public async Task Returns503WhenTheConnectionDropsWhileTheResponseIsBeingRead()
    {
        // What HttpClient throws when the connection drops after the headers but before the body ends.
        var inventoryApi = new FakeInventoryApi().Throws(
            new HttpIOException(HttpRequestError.ResponseEnded, "The response ended prematurely."));

        var response = await GetSummary(inventoryApi);

        await AssertProblemDetails(response, HttpStatusCode.ServiceUnavailable,
            title: "Inventory API unavailable",
            detail: "The connection to the Inventory API dropped before its response was complete.");
    }

    [Fact]
    public async Task Returns502NamingWhereTheResponseCouldNotBeRead()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith("""
            {
              "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 1,
              "records": [
                { "id": 1001, "name": "Tent", "category": "Outdoor", "price": 80.00, "quantity": 1, "lastUpdated": "yesterday" }
              ]
            }
            """);

        var response = await GetSummary(inventoryApi);

        await AssertProblemDetails(response, HttpStatusCode.BadGateway,
            title: "Invalid dataset",
            detail: "The Inventory API's response could not be read at $.records[0].lastUpdated.");
    }

    [Fact]
    public async Task Returns503WithNoPartialFiguresWhenALaterPageFails()
    {
        var inventoryApi = new FakeInventoryApi()
            .RespondsWith("""
                {
                  "page": 1, "pageSize": 25, "totalPages": 2, "totalRecords": 2,
                  "records": [
                    { "id": 1001, "name": "Tent", "category": "Outdoor", "price": 80.00, "quantity": 1, "lastUpdated": null }
                  ]
                }
                """)
            .RespondsWithStatus(HttpStatusCode.BadRequest);

        var response = await GetSummary(inventoryApi);

        await AssertProblemDetails(response, HttpStatusCode.ServiceUnavailable,
            title: "Inventory API unavailable",
            detail: "The Inventory API answered with status 400 (BadRequest).");
    }

    [Fact]
    public async Task Returns502NamingAnUnknownCategory()
    {
        var inventoryApi = new FakeInventoryApi().RespondsWith("""
            {
              "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 1,
              "records": [
                { "id": 1001, "name": "Rake", "category": "Garden", "price": 15.00, "quantity": 2, "lastUpdated": null }
              ]
            }
            """);

        var response = await GetSummary(inventoryApi);

        await AssertProblemDetails(response, HttpStatusCode.BadGateway,
            title: "Invalid dataset",
            detail: "Unknown category \"Garden\". Expected one of: Electronics, Home & Kitchen, Office Supplies, Outdoor, Toys, Discontinued.");
    }

    [Fact]
    public async Task Returns500WithAGenericDetailForAnyOtherFailure()
    {
        var inventoryApi = new FakeInventoryApi().Throws(
            new InvalidOperationException("A secret detail from /Users/someone/src/Internal.cs"));

        var response = await GetSummary(inventoryApi);

        await AssertProblemDetails(response, HttpStatusCode.InternalServerError,
            title: "Unexpected error",
            detail: "An unexpected error occurred.");
    }

    [Fact]
    public async Task LogsEachFailureOnceAtErrorLevelWithItsCause()
    {
        var logs = new CapturedLogs();
        var inventoryApi = new FakeInventoryApi().RespondsWithStatus(HttpStatusCode.InternalServerError, times: AllAttempts);

        await GetSummary(inventoryApi, logs);

        var error = Assert.Single(logs.Entries, entry => entry.Level >= LogLevel.Error);
        var exception = Assert.IsType<InventoryApiUnavailableException>(error.Exception);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task AnswersWithProblemDetailsEvenWhenTheCallerDoesNotAskForJson()
    {
        var logs = new CapturedLogs();
        var inventoryApi = new FakeInventoryApi().RespondsWithStatus(HttpStatusCode.InternalServerError, times: AllAttempts);

        var response = await GetSummary(inventoryApi, logs, accept: "text/plain");

        await AssertProblemDetails(response, HttpStatusCode.ServiceUnavailable,
            title: "Inventory API unavailable",
            detail: "The Inventory API answered with status 500 (InternalServerError).");
        Assert.Single(logs.Entries, entry => entry.Level >= LogLevel.Error);
    }

    private static async Task<HttpResponseMessage> GetSummary(
        FakeInventoryApi inventoryApi, CapturedLogs? logs = null, string? accept = null, TimeSpan? attemptTimeout = null)
    {
        await using var app = TestApp.Create(
            apiKey: ApiKey, inventoryApi: inventoryApi, logs: logs, attemptTimeout: attemptTimeout);
        using var client = app.CreateClient();
        if (accept is not null)
        {
            client.DefaultRequestHeaders.Add("Accept", accept);
        }

        return await client.GetAsync("/api/inventory/summary");
    }

    /// <summary>
    /// Checks the response is problem details with exactly the expected fields. Because nothing
    /// else is allowed in the body, no stack trace or internal path can be in it either.
    /// </summary>
    private static async Task AssertProblemDetails(
        HttpResponseMessage response, HttpStatusCode status, string title, string detail)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ApiKey, body);

        var problem = JsonNode.Parse(body)!.AsObject();
        Assert.Equal(["detail", "status", "title", "traceId"], problem.Select(field => field.Key).Order());
        Assert.Equal((int)status, (int?)problem["status"]);
        Assert.Equal(title, (string?)problem["title"]);
        Assert.Equal(detail, (string?)problem["detail"]);
        Assert.False(string.IsNullOrWhiteSpace((string?)problem["traceId"]));
    }
}
