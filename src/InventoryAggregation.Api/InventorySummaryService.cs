namespace InventoryAggregation.Api;

/// <summary>Produces the inventory summary: fetches the records, then aggregates them.</summary>
public sealed class InventorySummaryService(IInventoryApiClient inventoryApi, InventoryAggregator aggregator)
{
    public async Task<InventorySummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var page = await inventoryApi.FetchFirstPageAsync(cancellationToken);

        return new InventorySummary(aggregator.Aggregate(page.Records));
    }
}
