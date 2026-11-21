namespace InventoryAggregation.Api;

/// <summary>Reads records from the Inventory API. The only part of the service that knows about HTTP.</summary>
public interface IInventoryApiClient
{
    /// <summary>Fetches page 1 of the records.</summary>
    Task<InventoryPage> FetchFirstPageAsync(CancellationToken cancellationToken);
}
