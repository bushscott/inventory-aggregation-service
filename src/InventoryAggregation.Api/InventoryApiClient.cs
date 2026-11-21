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

        var page = await httpClient.GetFromJsonAsync<InventoryPage>(
            $"/api/records?page=1&pageSize={pageSize}", cancellationToken);

        return page!;
    }
}
