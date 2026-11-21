namespace InventoryAggregation.Api;

/// <summary>
/// The fixed list of categories a record can belong to. To support a new category, add it here;
/// if its display name has a space or a symbol, also add it to <see cref="CategoryNormalizer"/>.
/// </summary>
public enum Category
{
    Electronics,
    HomeAndKitchen,
    OfficeSupplies,
    Outdoor,
    Toys,
    Discontinued,
}
