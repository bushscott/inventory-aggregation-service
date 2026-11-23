namespace InventoryAggregation.Api;

/// <summary>Produces the inventory summary: fetches every page of records, then aggregates them.</summary>
public sealed class InventorySummaryService(IInventoryApiClient inventoryApi, InventoryAggregator aggregator)
{
    public async Task<InventorySummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var pages = await inventoryApi.FetchAllPagesAsync(cancellationToken);
        var records = pages.SelectMany(page => page.Records);

        return new InventorySummary(aggregator.Aggregate(records));
    }
}
