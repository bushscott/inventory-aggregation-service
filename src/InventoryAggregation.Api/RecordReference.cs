namespace InventoryAggregation.Api;

/// <summary>Points at one record by its id, name and last-updated time (in UTC).</summary>
public sealed record RecordReference(int Id, string? Name, DateTime? LastUpdated);
