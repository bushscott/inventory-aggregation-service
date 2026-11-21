using InventoryAggregation.Api;

namespace InventoryAggregation.Tests;

public class InventoryAggregatorTests
{
    private readonly InventoryAggregator aggregator = new();

    [Fact]
    public void SummarisesTheAvailableRecordsInACategory()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1001, "Electronics", price: 19.99m, quantity: 3, name: "Desk Lamp",
                lastUpdated: new DateTimeOffset(2026, 9, 18, 21, 2, 0, TimeSpan.Zero)),
            Record(1002, "Electronics", price: 5.00m, quantity: 2, name: "USB Cable"),
        ]);

        Assert.Equivalent(
            new[]
            {
                new CategorySummary(
                    Category: "Electronics",
                    TotalInventoryValue: 69.97m,
                    AvailableRecordCount: 2,
                    MostRecentlyUpdated: null,
                    AvailableRecords:
                    [
                        new AvailableRecord(1001, "Desk Lamp", 19.99m, 3,
                            new DateTime(2026, 9, 18, 21, 2, 0, DateTimeKind.Utc)),
                        new AvailableRecord(1002, "USB Cable", 5.00m, 2, LastUpdated: null),
                    ]),
            },
            summaries,
            strict: true);
    }

    [Fact]
    public void DoesNotRoundTheTotalInventoryValue()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1001, "Toys", price: 19.99m, quantity: 3),
            Record(1002, "Toys", price: 0.125m, quantity: 1),
        ]);

        Assert.Equal(60.095m, Assert.Single(summaries).TotalInventoryValue);
    }

    [Fact]
    public void LeavesRecordsWithNoStockOutOfEveryFigure()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1001, "Outdoor", price: 10.00m, quantity: 2, name: "Tent"),
            Record(1002, "Outdoor", price: 100.00m, quantity: 0, name: "Kayak"),
            Record(1003, "Outdoor", price: 1000.00m, quantity: -1, name: "Canoe"),
        ]);

        Assert.Equivalent(
            new[]
            {
                new CategorySummary(
                    Category: "Outdoor",
                    TotalInventoryValue: 20.00m,
                    AvailableRecordCount: 1,
                    MostRecentlyUpdated: null,
                    AvailableRecords: [new AvailableRecord(1001, "Tent", 10.00m, 2, LastUpdated: null)]),
            },
            summaries,
            strict: true);
    }

    [Fact]
    public void OmitsACategoryWithNoAvailableRecords()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1001, "Toys", quantity: 4),
            Record(1002, "Discontinued", quantity: 0),
            Record(1003, "Discontinued", quantity: 0),
        ]);

        Assert.Equal(["Toys"], summaries.Select(summary => summary.Category));
    }

    [Fact]
    public void GroupsSpellingsOfACategoryUnderTheInventoryApisDisplayName()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1001, " home & kitchen"),
            Record(1002, "OFFICE SUPPLIES"),
            Record(1003, "Home & Kitchen  "),
        ]);

        Assert.Equal(
            [("Home & Kitchen", 2), ("Office Supplies", 1)],
            summaries.Select(summary => (summary.Category, summary.AvailableRecordCount)));
    }

    [Fact]
    public void KeepsCategoriesAndTheirRecordsInArrivalOrder()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1003, "Toys"),
            Record(1002, "Electronics"),
            Record(1001, "Toys"),
            Record(1005, "Outdoor"),
            Record(1004, "Electronics"),
        ]);

        Assert.Equal(
            [("Toys", [1003, 1001]), ("Electronics", [1002, 1004]), ("Outdoor", [1005])],
            summaries.Select(summary => (summary.Category, summary.AvailableRecords.Select(record => record.Id).ToArray())));
    }

    [Fact]
    public void FailsOnAnUnknownCategoryEvenWhenTheRecordHasNoStock()
    {
        var error = Assert.Throws<InvalidDatasetException>(() => aggregator.Aggregate(
        [
            Record(1001, "Toys", quantity: 4),
            Record(1002, "Garden", quantity: 0),
        ]));

        Assert.StartsWith("Unknown category \"Garden\".", error.Message);
    }

    private static InventoryRecord Record(
        int id,
        string? category,
        decimal price = 1.00m,
        int quantity = 1,
        string? name = "Widget",
        DateTimeOffset? lastUpdated = null) =>
        new(id, name, category, price, quantity, lastUpdated);
}
