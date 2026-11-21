namespace InventoryAggregation.Api;

/// <summary>Translates between the Inventory API's category text and <see cref="Category"/>.</summary>
public static class CategoryNormalizer
{
    /// <summary>Display names that cannot be written as an enum member's name.</summary>
    private static readonly Dictionary<Category, string> DisplayNameOverrides = new()
    {
        [Category.HomeAndKitchen] = "Home & Kitchen",
        [Category.OfficeSupplies] = "Office Supplies",
    };

    /// <summary>The category's name as the Inventory API spells it, such as "Home &amp; Kitchen".</summary>
    public static string DisplayName(Category category) =>
        DisplayNameOverrides.GetValueOrDefault(category, category.ToString());

    /// <summary>
    /// Finds the category whose display name matches the text, ignoring letter case and
    /// surrounding spaces, because the Inventory API spells categories inconsistently.
    /// </summary>
    /// <exception cref="InvalidDatasetException">The text is missing or matches no category.</exception>
    public static Category Normalize(string? text)
    {
        foreach (var category in Enum.GetValues<Category>())
        {
            if (string.Equals(DisplayName(category), text?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        var problem = text is null ? "Missing category" : $"Unknown category \"{text}\"";
        var known = string.Join(", ", Enum.GetValues<Category>().Select(DisplayName));
        throw new InvalidDatasetException($"{problem}. Expected one of: {known}.");
    }
}
