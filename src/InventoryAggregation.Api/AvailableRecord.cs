namespace InventoryAggregation.Api;

/// <summary>
/// A record with stock, as listed in a Category Summary's items.
/// <see cref="LastUpdated"/> is in UTC, so it is written with a "Z" suffix.
/// </summary>
public sealed record AvailableRecord(
    int Id,
    string? Name,
    decimal Price,
    int Quantity,
    DateTime? LastUpdated);
