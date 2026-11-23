namespace InventoryAggregation.Api;

/// <summary>Reads records from the Inventory API. The only part of the service that knows about HTTP.</summary>
public interface IInventoryApiClient
{
    /// <summary>
    /// Fetches every page of records, one after another, from page 1 up to the page count that
    /// page 1 reports. Any page that fails fails the whole fetch.
    /// </summary>
    Task<IReadOnlyList<InventoryPage>> FetchAllPagesAsync(CancellationToken cancellationToken);
}
