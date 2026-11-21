using System.Net;
using System.Text;

namespace InventoryAggregation.Tests;

/// <summary>
/// A stand-in for the Inventory API. It plays back scripted responses in order and records every
/// request it receives, so a test can check what the service sent.
/// </summary>
internal sealed class FakeInventoryApi : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Adds a 200 response with this JSON body to the script.</summary>
    public FakeInventoryApi RespondsWith(string json)
    {
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (!responses.TryDequeue(out var response))
        {
            throw new InvalidOperationException(
                $"The fake Inventory API has no response scripted for request {Requests.Count}: {request.RequestUri}");
        }

        return Task.FromResult(response);
    }
}
