using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace InventoryAggregation.Api;

/// <summary>
/// Calls the Inventory API with the named <see cref="HttpClient"/> set up in Program.cs, which
/// carries the base URL and the API key header.
/// </summary>
public sealed class InventoryApiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<InventoryApiOptions> options) : IInventoryApiClient
{
    /// <summary>The name of the <see cref="HttpClient"/> configured for the Inventory API.</summary>
    public const string HttpClientName = "InventoryApi";

    /// <summary>
    /// The usual web JSON settings, made strict so a malformed response fails instead of being
    /// read as something plausible.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        // A number written as text, such as "19.99", is a value in the wrong format.
        NumberHandling = JsonNumberHandling.Strict,
        // Every field must be present. Without this, a missing price or quantity would read as 0.
        // Name, category and last-updated time may still be null.
        RespectRequiredConstructorParameters = true,
    };

    public async Task<InventoryPage> FetchFirstPageAsync(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        var pageSize = options.Value.PageSize;

        InventoryPage? page;
        try
        {
            page = await httpClient.GetFromJsonAsync<InventoryPage>(
                $"/api/records?page=1&pageSize={pageSize}", JsonOptions, cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode is { } status)
        {
            throw new InventoryApiUnavailableException(
                $"The Inventory API answered with status {(int)status} ({status}).", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new InventoryApiUnavailableException("The Inventory API could not be reached.", exception);
        }
        catch (TimeoutRejectedException exception)
        {
            // The resilience handler gave up waiting for the response (its timeouts are in Program.cs).
            throw new InventoryApiUnavailableException("The Inventory API did not answer in time.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient's own timeout elapsed. The resilience handler's timeouts end once the
            // headers arrive, so this one is what stops a body that stalls. (When our own caller
            // cancels, that is not a failure to report.)
            throw new InventoryApiUnavailableException("The Inventory API did not answer in time.", exception);
        }
        catch (IOException exception)
        {
            // The body is read after the headers arrive, so a dropped connection can surface here.
            throw new InventoryApiUnavailableException(
                "The connection to the Inventory API dropped before its response was complete.", exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidDatasetException(
                $"The Inventory API's response could not be read at {exception.Path}.", exception);
        }

        RejectNulls(page);

        return page;
    }

    /// <summary>
    /// Fails when the response has null in place of the page, its records list or a record. The
    /// JSON reader lets these through, but everything after this relies on them being there.
    /// </summary>
    private static void RejectNulls([NotNull] InventoryPage? page)
    {
        if (page is null)
        {
            throw NullAt("$");
        }

        if (page.Records is null)
        {
            throw NullAt("$.records");
        }

        for (var index = 0; index < page.Records.Count; index++)
        {
            if (page.Records[index] is null)
            {
                throw NullAt($"$.records[{index}]");
            }
        }
    }

    /// <summary>The location is a JSON path, as in the JSON reader's own errors.</summary>
    private static InvalidDatasetException NullAt(string location) =>
        new($"The Inventory API's response has null at {location}.");
}
