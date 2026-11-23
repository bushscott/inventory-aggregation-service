using Microsoft.Extensions.Options;

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

    public async Task<InventoryPage> FetchFirstPageAsync(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        var pageSize = options.Value.PageSize;

        InventoryPage? page;
        try
        {
            page = await httpClient.GetFromJsonAsync<InventoryPage>(
                $"/api/records?page=1&pageSize={pageSize}", cancellationToken);
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
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient gave up waiting. (When our own caller cancels, that is not a failure to report.)
            throw new InventoryApiUnavailableException("The Inventory API did not answer in time.", exception);
        }
        catch (IOException exception)
        {
            // The body is read after the headers arrive, so a dropped connection can surface here.
            throw new InventoryApiUnavailableException(
                "The connection to the Inventory API dropped before its response was complete.", exception);
        }

        return page!;
    }
}
