using Microsoft.Extensions.Options;

namespace InventoryAggregation.Api;

/// <summary>
/// Produces the inventory summary: fetches every page of records, validates them, then
/// aggregates them.
/// </summary>
public sealed class InventorySummaryService(
    IInventoryApiClient inventoryApi,
    DatasetValidator validator,
    InventoryAggregator aggregator,
    IOptions<InventoryApiOptions> options)
{
    public async Task<InventorySummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var pages = await inventoryApi.FetchAllPagesAsync(cancellationToken);
        var records = validator.Validate(pages, options.Value.PageSize);

        return new InventorySummary(aggregator.Aggregate(records));
    }
}
