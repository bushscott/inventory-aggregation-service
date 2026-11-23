using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace InventoryAggregation.Tests;

/// <summary>
/// A stand-in for the Inventory API. It plays back scripted responses in order and records every
/// request it receives, so a test can check what the service sent.
/// </summary>
internal sealed class FakeInventoryApi : HttpMessageHandler
{
    private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> script = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Adds a 200 response with this JSON body to the script.</summary>
    public FakeInventoryApi RespondsWith(string json) =>
        Add(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));

    /// <summary>
    /// Adds an empty response with this status code to the script, <paramref name="times"/> in a
    /// row, with a Retry-After header when <paramref name="retryAfter"/> is given.
    /// </summary>
    public FakeInventoryApi RespondsWithStatus(HttpStatusCode status, int times = 1, TimeSpan? retryAfter = null) =>
        Add(_ =>
        {
            var response = new HttpResponseMessage(status);
            if (retryAfter is not null)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter.Value);
            }
            return Task.FromResult(response);
        }, times);

    /// <summary>
    /// Adds a request that fails by throwing this exception, as a dropped connection does,
    /// <paramref name="times"/> in a row.
    /// </summary>
    public FakeInventoryApi Throws(Exception exception, int times = 1) =>
        Add(_ => throw exception, times);

    /// <summary>
    /// Adds a request that is never answered, as when the Inventory API hangs, <paramref name="times"/>
    /// in a row. It ends only when the caller gives up and cancels it.
    /// </summary>
    public FakeInventoryApi NeverResponds(int times = 1) =>
        Add(async cancellationToken =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("Unreachable: the delay above only ends by being cancelled.");
        }, times);

    private FakeInventoryApi Add(Func<CancellationToken, Task<HttpResponseMessage>> respond, int times = 1)
    {
        for (var attempt = 0; attempt < times; attempt++)
        {
            script.Enqueue(respond);
        }

        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (!script.TryDequeue(out var respond))
        {
            throw new InvalidOperationException(
                $"The fake Inventory API has no response scripted for request {Requests.Count}: {request.RequestUri}");
        }

        return respond(cancellationToken);
    }
}
