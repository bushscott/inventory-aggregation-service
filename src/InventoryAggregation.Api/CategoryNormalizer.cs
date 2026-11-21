namespace InventoryAggregation.Api;

/// <summary>Matches the Inventory API's category text to a <see cref="Category"/>.</summary>
public static class CategoryNormalizer
{
    /// <summary>
    /// Finds the category whose display name matches the text, ignoring letter case and
    /// surrounding whitespace (spaces, tabs, line breaks), because the Inventory API spells
    /// categories inconsistently.
    /// </summary>
    /// <exception cref="InvalidDatasetException">The text is missing or matches no category.</exception>
    public static Category Normalize(string? text)
    {
        var trimmed = text?.Trim();

        foreach (var category in Enum.GetValues<Category>())
        {
            if (string.Equals(category.DisplayName(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        var problem = text is null ? "Missing category" : $"Unknown category \"{text}\"";
        var known = string.Join(", ", Enum.GetValues<Category>().Select(category => category.DisplayName()));
        throw new InvalidDatasetException($"{problem}. Expected one of: {known}.");
    }
}
