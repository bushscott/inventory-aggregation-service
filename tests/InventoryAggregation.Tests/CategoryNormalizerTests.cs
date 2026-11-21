using InventoryAggregation.Api;

namespace InventoryAggregation.Tests;

public class CategoryNormalizerTests
{
    [Theory]
    [InlineData("Electronics", Category.Electronics)]
    [InlineData("Home & Kitchen", Category.HomeAndKitchen)]
    [InlineData("Office Supplies", Category.OfficeSupplies)]
    [InlineData("Outdoor", Category.Outdoor)]
    [InlineData("Toys", Category.Toys)]
    [InlineData("Discontinued", Category.Discontinued)]
    public void MatchesEachCategoryByItsDisplayName(string text, Category expected)
    {
        Assert.Equal(expected, CategoryNormalizer.Normalize(text));
    }

    [Theory]
    [InlineData(" Home & Kitchen")]
    [InlineData("  home & kitchen ")]
    [InlineData("HOME & KITCHEN")]
    [InlineData("hOmE & kItChEn  ")]
    [InlineData("\tHome & Kitchen\n")]
    public void IgnoresSurroundingWhitespaceAndLetterCase(string text)
    {
        Assert.Equal(Category.HomeAndKitchen, CategoryNormalizer.Normalize(text));
    }

    [Theory]
    [InlineData("Garden")]
    [InlineData("Home and Kitchen")]
    [InlineData("")]
    [InlineData("   ")]
    public void FailsOnUnknownCategoryNamingTheText(string text)
    {
        var error = Assert.Throws<InvalidDatasetException>(() => CategoryNormalizer.Normalize(text));

        Assert.Equal(
            $"Unknown category \"{text}\". Expected one of: Electronics, Home & Kitchen, Office Supplies, Outdoor, Toys, Discontinued.",
            error.Message);
    }

    [Fact]
    public void FailsOnMissingCategory()
    {
        var error = Assert.Throws<InvalidDatasetException>(() => CategoryNormalizer.Normalize(null));

        Assert.Equal(
            "Missing category. Expected one of: Electronics, Home & Kitchen, Office Supplies, Outdoor, Toys, Discontinued.",
            error.Message);
    }
}
