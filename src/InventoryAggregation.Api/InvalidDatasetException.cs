namespace InventoryAggregation.Api;

/// <summary>
/// The Inventory API sent data that fails a check, so no summary can be trusted.
/// The message says what was expected and what arrived, and is safe to show a caller.
/// </summary>
public sealed class InvalidDatasetException(string message) : Exception(message);
