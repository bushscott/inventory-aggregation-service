namespace InventoryAggregation.Api;

/// <summary>
/// One page of records as the Inventory API sends it, with the page number, page size, page count
/// and record count it reports.
/// </summary>
public sealed record InventoryPage(
    int Page,
    int PageSize,
    int TotalPages,
    int TotalRecords,
    IReadOnlyList<InventoryRecord> Records);
