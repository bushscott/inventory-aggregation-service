namespace InventoryAggregation.Api;

/// <summary>Turns records into one Category Summary per category. No HTTP, no state.</summary>
public sealed class InventoryAggregator
{
    /// <summary>
    /// Groups the available records by category. Categories and their records keep the order the
    /// records arrived in.
    /// </summary>
    /// <exception cref="InvalidDatasetException">A record has an Unknown Category.</exception>
    public IReadOnlyList<CategorySummary> Aggregate(IEnumerable<InventoryRecord> records) =>
        records
            // Match the category before leaving out records with no stock, so an Unknown
            // Category fails the request even on a record that would not be counted.
            .Select(record => (Category: CategoryNormalizer.Normalize(record.Category), Record: record))
            .Where(entry => IsAvailable(entry.Record))
            .GroupBy(entry => entry.Category, entry => entry.Record)
            .Select(group => new CategorySummary(
                Category: CategoryNormalizer.DisplayName(group.Key),
                TotalInventoryValue: group.Sum(record => record.Price * record.Quantity),
                AvailableRecordCount: group.Count(),
                MostRecentlyUpdated: null,
                Items: group.Select(ToAvailableRecord).ToList()))
            .ToList();

    /// <summary>Only records with stock count. Zero and negative quantities are left out of every figure.</summary>
    private static bool IsAvailable(InventoryRecord record) => record.Quantity > 0;

    private static AvailableRecord ToAvailableRecord(InventoryRecord record) =>
        new(record.Id, record.Name, record.Price, record.Quantity, record.LastUpdated?.UtcDateTime);
}
