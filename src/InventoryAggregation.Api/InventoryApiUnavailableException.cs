namespace InventoryAggregation.Api;

/// <summary>
/// The Inventory API could not be reached or did not answer successfully, so trying again later
/// may work. The message is safe to show a caller; the lower-level cause is the inner exception.
/// </summary>
public sealed class InventoryApiUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
