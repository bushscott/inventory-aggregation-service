using System.Diagnostics;
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
    IOptions<InventoryApiOptions> options,
    ILogger<InventorySummaryService> logger)
{
    public async Task<InventorySummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        // The time includes any retries, and the wait for the rate limit to reset.
        var stopwatch = Stopwatch.StartNew();
        var pages = await inventoryApi.FetchAllPagesAsync(cancellationToken);
        stopwatch.Stop();

        var records = validator.Validate(pages, options.Value.PageSize);

        // Validation passed, so every record received beyond the unique ones was an identical copy.
        var recordsReceived = pages.Sum(page => page.Records.Count);
        logger.LogInformation(
            "Fetched the dataset from the Inventory API. Pages: {PageCount}, unique records: {UniqueRecordCount}, repeated records removed: {RepeatedRecordCount}, time taken: {ElapsedMilliseconds} ms.",
            pages.Count, records.Count, recordsReceived - records.Count, stopwatch.ElapsedMilliseconds);

        return new InventorySummary(aggregator.Aggregate(records));
    }
}
