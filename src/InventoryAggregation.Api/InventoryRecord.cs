namespace InventoryAggregation.Api;

/// <summary>
/// One record as the Inventory API sends it. The category is the raw text, not yet matched to a
/// <see cref="Api.Category"/>. Name and last-updated time may be missing.
/// </summary>
public sealed record InventoryRecord(
    int Id,
    string? Name,
    string? Category,
    decimal Price,
    int Quantity,
    DateTimeOffset? LastUpdated);
