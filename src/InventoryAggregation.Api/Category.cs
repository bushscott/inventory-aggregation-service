namespace InventoryAggregation.Api;

/// <summary>
/// The fixed list of categories a record can belong to. To support a new category, add it here;
/// if its display name has a space or a symbol, also add it to the overrides below.
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

public static class CategoryExtensions
{
    /// <summary>Display names that cannot be written as an enum member's name.</summary>
    private static readonly Dictionary<Category, string> DisplayNameOverrides = new()
    {
        [Category.HomeAndKitchen] = "Home & Kitchen",
        [Category.OfficeSupplies] = "Office Supplies",
    };

    /// <summary>The category's name as the Inventory API spells it, such as "Home &amp; Kitchen".</summary>
    public static string DisplayName(this Category category) =>
        DisplayNameOverrides.GetValueOrDefault(category, category.ToString());
}
