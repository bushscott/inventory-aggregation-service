using System.Text.Json.Serialization;

namespace InventoryAggregation.Api;

/// <summary>The figures reported for one category, computed from its available records.</summary>
/// <param name="Category">The category's display name, such as "Home &amp; Kitchen".</param>
/// <param name="TotalInventoryValue">The sum of price times quantity, not rounded.</param>
/// <param name="AvailableRecordCount">How many records in the category have stock.</param>
/// <param name="MostRecentlyUpdated">
/// The available record with the latest last-updated time, or null when none has one.
/// </param>
/// <param name="AvailableRecords">
/// The available records, in the order they arrived. Written as "items", the name the response
/// contract uses.
/// </param>
public sealed record CategorySummary(
    string Category,
    decimal TotalInventoryValue,
    int AvailableRecordCount,
    RecordReference? MostRecentlyUpdated,
    [property: JsonPropertyName("items")] IReadOnlyList<AvailableRecord> AvailableRecords);
