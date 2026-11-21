namespace InventoryAggregation.Api;

/// <summary>The figures reported for one category, computed from its available records.</summary>
/// <param name="Category">The category's display name, such as "Home &amp; Kitchen".</param>
/// <param name="TotalInventoryValue">The sum of price times quantity, not rounded.</param>
/// <param name="AvailableRecordCount">How many records in the category have stock.</param>
/// <param name="MostRecentlyUpdated">Not worked out yet, so always null.</param>
/// <param name="Items">The available records, in the order they arrived.</param>
public sealed record CategorySummary(
    string Category,
    decimal TotalInventoryValue,
    int AvailableRecordCount,
    RecordReference? MostRecentlyUpdated,
    IReadOnlyList<AvailableRecord> Items);
