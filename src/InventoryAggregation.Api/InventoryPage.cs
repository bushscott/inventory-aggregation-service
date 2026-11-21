namespace InventoryAggregation.Api;

/// <summary>One page of records as the Inventory API sends it.</summary>
public sealed record InventoryPage(IReadOnlyList<InventoryRecord> Records);
