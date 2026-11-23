using System.Diagnostics;
using System.Net;
using InventoryAggregation.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.Timeout;

namespace InventoryAggregation.Tests;

/// <summary>
/// How the client rides out failures, through the real HttpClient and resilience setup with the
/// fake Inventory API underneath.
/// </summary>
public class InventoryApiClientRetryTests
{
    private const string OneRecordPage = """
        {
          "page": 1, "pageSize": 25, "totalPages": 1, "totalRecords": 1,
          "records": [
            { "id": 1001, "name": "Tent", "category": "Outdoor", "price": 80.00, "quantity": 1, "lastUpdated": null }
          ]
        }
        """;

    [Fact]
    public async Task RetriesAServerErrorAndSucceedsWhenALaterAttemptSucceeds()
    {
        var inventoryApi = new FakeInventoryApi()
            .RespondsWithStatus(HttpStatusCode.InternalServerError)
            .RespondsWith(OneRecordPage);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        var page = await TestApp.FetchFirstPage(app);

        Assert.Equal(1001, Assert.Single(page.Records).Id);
        Assert.Equal(2, inventoryApi.Requests.Count);
    }

    [Fact]
    public async Task RetriesAConnectionFailure()
    {
        var inventoryApi = new FakeInventoryApi()
            .Throws(new HttpRequestException(HttpRequestError.ConnectionError, "Connection refused"))
            .RespondsWith(OneRecordPage);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        var page = await TestApp.FetchFirstPage(app);

        Assert.Equal(1001, Assert.Single(page.Records).Id);
        Assert.Equal(2, inventoryApi.Requests.Count);
    }

    [Fact]
    public async Task GivesUpAsUnavailableAfterThreeRetries()
    {
        // One more failure than the 4 attempts expected, so a fifth attempt would be answered and counted.
        var inventoryApi = new FakeInventoryApi().RespondsWithStatus(HttpStatusCode.ServiceUnavailable, times: 5);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        var error = await Assert.ThrowsAsync<InventoryApiUnavailableException>(() => TestApp.FetchFirstPage(app));

        Assert.Equal("The Inventory API answered with status 503 (ServiceUnavailable).", error.Message);
        Assert.Equal(4, inventoryApi.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task DoesNotRetryAFailureThatRetryingCannotFix(HttpStatusCode status)
    {
        var inventoryApi = new FakeInventoryApi()
            .RespondsWithStatus(status)
            .RespondsWith(OneRecordPage);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);

        await Assert.ThrowsAsync<InventoryApiUnavailableException>(() => TestApp.FetchFirstPage(app));

        Assert.Single(inventoryApi.Requests);
    }

    [Fact]
    public async Task WaitsAsLongAsRetryAfterSaysBeforeRetryingA429()
    {
        var inventoryApi = new FakeInventoryApi()
            .RespondsWithStatus(HttpStatusCode.TooManyRequests, retryAfter: TimeSpan.FromSeconds(1))
            .RespondsWith(OneRecordPage);
        await using var app = TestApp.Create(inventoryApi: inventoryApi);
        // Resolved before the stopwatch starts, so the app's start-up cannot count as the wait.
        var client = app.Services.GetRequiredService<IInventoryApiClient>();

        var stopwatch = Stopwatch.StartNew();
        var page = await client.FetchFirstPageAsync(CancellationToken.None);
        stopwatch.Stop();

        Assert.Equal(1001, Assert.Single(page.Records).Id);
        Assert.Equal(2, inventoryApi.Requests.Count);
        // Retries are otherwise immediate in tests (see TestApp), so this wait can only come from
        // the Retry-After header. The bound is a little under 1 second because timers can fire early.
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromSeconds(0.9), $"Retried after only {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task GivesUpAsUnavailableWhenEveryAttemptTimesOut()
    {
        // One more than the 4 attempts expected, so a fifth attempt would be counted.
        var inventoryApi = new FakeInventoryApi().NeverResponds(times: 5);
        await using var app = TestApp.Create(inventoryApi: inventoryApi, attemptTimeout: TimeSpan.FromMilliseconds(100));

        var error = await Assert.ThrowsAsync<InventoryApiUnavailableException>(() => TestApp.FetchFirstPage(app));

        Assert.Equal("The Inventory API did not answer in time.", error.Message);
        Assert.IsType<TimeoutRejectedException>(error.InnerException);
        Assert.Equal(4, inventoryApi.Requests.Count);
    }

    [Fact]
    public async Task LogsEachRetryAsAWarningWithItsReason()
    {
        var logs = new CapturedLogs();
        var inventoryApi = new FakeInventoryApi()
            .RespondsWithStatus(HttpStatusCode.InternalServerError, times: 2)
            .RespondsWith(OneRecordPage);
        await using var app = TestApp.Create(inventoryApi: inventoryApi, logs: logs);

        await TestApp.FetchFirstPage(app);

        // The resilience library writes these entries; "OnRetry" and "Result:" are its wording.
        var retries = logs.Entries.Where(entry => entry.Message.Contains("OnRetry")).ToList();
        Assert.Equal(2, retries.Count);
        Assert.All(retries, retry =>
        {
            Assert.Equal(LogLevel.Warning, retry.Level);
            Assert.Contains("Result: '500'", retry.Message);
        });
    }
}
