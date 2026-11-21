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
                    MostRecentlyUpdated: new RecordReference(1001, "Desk Lamp",
                        new DateTime(2026, 9, 18, 21, 2, 0, DateTimeKind.Utc)),
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
    public void NamesTheAvailableRecordWithTheLatestLastUpdatedTime()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1001, "Toys", name: "Kite", lastUpdated: new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)),
            Record(1002, "Toys", name: "Yo-yo", lastUpdated: new DateTimeOffset(2026, 9, 18, 23, 2, 0, TimeSpan.FromHours(2))),
            Record(1003, "Toys", name: "Puzzle", lastUpdated: new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero)),
        ]);

        Assert.Equal(
            new RecordReference(1002, "Yo-yo", new DateTime(2026, 9, 18, 21, 2, 0, DateTimeKind.Utc)),
            Assert.Single(summaries).MostRecentlyUpdated);
    }

    [Fact]
    public void ChoosesTheMostRecentlyUpdatedRecordOnlyFromAvailableRecords()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1001, "Outdoor", quantity: 2, name: "Tent", lastUpdated: new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)),
            Record(1002, "Outdoor", quantity: 0, name: "Kayak", lastUpdated: new DateTimeOffset(2026, 9, 18, 8, 0, 0, TimeSpan.Zero)),
            Record(1003, "Outdoor", quantity: -1, name: "Canoe", lastUpdated: new DateTimeOffset(2026, 9, 19, 8, 0, 0, TimeSpan.Zero)),
        ]);

        Assert.Equal(1001, Assert.Single(summaries).MostRecentlyUpdated?.Id);
    }

    [Fact]
    public void NeverChoosesARecordWithNoLastUpdatedTimeButStillCountsIt()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1001, "Toys", price: 50.00m, quantity: 2, name: "Drone", lastUpdated: null),
            Record(1002, "Toys", price: 5.00m, quantity: 1, name: "Kite",
                lastUpdated: new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)),
        ]);

        Assert.Equivalent(
            new[]
            {
                new CategorySummary(
                    Category: "Toys",
                    TotalInventoryValue: 105.00m,
                    AvailableRecordCount: 2,
                    MostRecentlyUpdated: new RecordReference(1002, "Kite",
                        new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc)),
                    AvailableRecords:
                    [
                        new AvailableRecord(1001, "Drone", 50.00m, 2, LastUpdated: null),
                        new AvailableRecord(1002, "Kite", 5.00m, 1,
                            new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc)),
                    ]),
            },
            summaries,
            strict: true);
    }

    [Fact]
    public void HasNoMostRecentlyUpdatedRecordWhenNoAvailableRecordHasALastUpdatedTime()
    {
        var summaries = aggregator.Aggregate(
        [
            Record(1001, "Toys", lastUpdated: null),
            Record(1002, "Toys", lastUpdated: null),
        ]);

        Assert.Null(Assert.Single(summaries).MostRecentlyUpdated);
    }

    [Fact]
    public void ChoosesTheFirstToArriveWhenRecordsShareTheLatestLastUpdatedTime()
    {
        var latest = new DateTimeOffset(2026, 9, 18, 21, 2, 0, TimeSpan.Zero);

        var summaries = aggregator.Aggregate(
        [
            Record(1001, "Toys", lastUpdated: new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)),
            Record(1002, "Toys", lastUpdated: latest),
            Record(1003, "Toys", lastUpdated: latest),
        ]);

        Assert.Equal(1002, Assert.Single(summaries).MostRecentlyUpdated?.Id);
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
