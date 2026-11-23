namespace InventoryAggregation.Api;

/// <summary>One page of records as the Inventory API sends it, with the page count it reports.</summary>
public sealed record InventoryPage(int TotalPages, IReadOnlyList<InventoryRecord> Records);
