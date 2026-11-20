namespace InventoryAggregation.Api;

/// <summary>The body of the summary endpoint's response: one Category Summary per category.</summary>
public sealed record InventorySummary(IReadOnlyList<CategorySummary> Categories);
