using InventoryAggregation.Api;

namespace InventoryAggregation.Tests;

public class DatasetValidatorTests
{
    /// <summary>Small, so a test can show a full page, a short page and a page holding one extra.</summary>
    private const int RequestedPageSize = 2;

    private readonly DatasetValidator validator = new();

    [Fact]
    public void ReturnsTheRecordsFromEveryPageInArrivalOrder()
    {
        var records = validator.Validate(
        [
            Page(1, totalPages: 2, totalRecords: 3, [Record(1002), Record(1001)]),
            Page(2, totalPages: 2, totalRecords: 3, [Record(1003)]),
        ], RequestedPageSize);

        Assert.Equal([1002, 1001, 1003], records.Select(record => record.Id));
    }

    [Fact]
    public void KeepsARepeatedRecordWithIdenticalCopiesOnceWhereItFirstArrived()
    {
        var records = validator.Validate(
        [
            Page(1, totalPages: 2, totalRecords: 3, [Record(1001, name: "Tent"), Record(1002)]),
            Page(2, totalPages: 2, totalRecords: 3, [Record(1003), Record(1001, name: "Tent")]),
        ], RequestedPageSize);

        Assert.Equal([1001, 1002, 1003], records.Select(record => record.Id));
    }

    /// <summary>Copies of <c>Record(1001)</c>, each differing from it in one field.</summary>
    public static TheoryData<InventoryRecord> CopiesThatDifferInOneField =>
    [
        Record(1001, name: "Gadget"),
        Record(1001, category: "Outdoor"),
        Record(1001, price: 1.01m),
        Record(1001, quantity: 2),
        Record(1001, lastUpdated: new DateTimeOffset(2026, 9, 18, 21, 2, 0, TimeSpan.Zero)),
    ];

    [Theory]
    [MemberData(nameof(CopiesThatDifferInOneField))]
    public void FailsNamingARepeatedRecordWhoseCopiesDiffer(InventoryRecord differingCopy)
    {
        var error = Assert.Throws<InvalidDatasetException>(() => validator.Validate(
        [
            Page(1, totalPages: 2, totalRecords: 2, [Record(1001), Record(1002)]),
            Page(2, totalPages: 2, totalRecords: 2, [differingCopy]),
        ], RequestedPageSize));

        Assert.Equal("Record 1001 arrived more than once, and the copies differ.", error.Message);
    }

    [Fact]
    public void FailsNamingARecordWithANegativePrice()
    {
        var error = Assert.Throws<InvalidDatasetException>(() => validator.Validate(
        [
            Page(1, totalPages: 1, totalRecords: 2, [Record(1001), Record(1002, price: -0.01m)]),
        ], RequestedPageSize));

        Assert.Equal("Record 1002 has price -0.01, but a price cannot be negative.", error.Message);
    }

    [Fact]
    public void FailsWhenAPageReportsADifferentPageNumber()
    {
        var error = Assert.Throws<InvalidDatasetException>(() => validator.Validate(
        [
            Page(1, totalPages: 2, totalRecords: 3, [Record(1001), Record(1002)]),
            Page(3, totalPages: 2, totalRecords: 3, [Record(1003)]),
        ], RequestedPageSize));

        Assert.Equal("The Inventory API's page 2 reported page number 3.", error.Message);
    }

    [Fact]
    public void FailsWhenAPageReportsADifferentPageSize()
    {
        var error = Assert.Throws<InvalidDatasetException>(() => validator.Validate(
        [
            Page(1, totalPages: 2, totalRecords: 3, [Record(1001), Record(1002)]),
            Page(2, totalPages: 2, totalRecords: 3, [Record(1003)], pageSize: 3),
        ], RequestedPageSize));

        Assert.Equal("The Inventory API's page 2 reported page size 3, but 2 was requested.", error.Message);
    }

    [Fact]
    public void FailsWhenTotalPagesDifferBetweenPages()
    {
        var error = Assert.Throws<InvalidDatasetException>(() => validator.Validate(
        [
            Page(1, totalPages: 2, totalRecords: 3, [Record(1001), Record(1002)]),
            Page(2, totalPages: 3, totalRecords: 3, [Record(1003)]),
        ], RequestedPageSize));

        Assert.Equal("The Inventory API's page 2 reported total pages 3, but page 1 reported 2.", error.Message);
    }

    [Fact]
    public void FailsWhenTotalRecordsDifferBetweenPages()
    {
        var error = Assert.Throws<InvalidDatasetException>(() => validator.Validate(
        [
            Page(1, totalPages: 2, totalRecords: 3, [Record(1001), Record(1002)]),
            Page(2, totalPages: 2, totalRecords: 4, [Record(1003)]),
        ], RequestedPageSize));

        Assert.Equal("The Inventory API's page 2 reported total records 4, but page 1 reported 3.", error.Message);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(1)]
    public void FailsWhenTheNumberOfPagesFetchedDiffersFromTotalPages(int totalPages)
    {
        var error = Assert.Throws<InvalidDatasetException>(() => validator.Validate(
        [
            Page(1, totalPages, totalRecords: 3, [Record(1001), Record(1002)]),
            Page(2, totalPages, totalRecords: 3, [Record(1003)]),
        ], RequestedPageSize));

        Assert.Equal(
            $"The Inventory API reported total pages {totalPages}, but the number of pages fetched was 2.",
            error.Message);
    }

    /// <summary>Four records arrive, three of them unique, so 4 is wrong too.</summary>
    [Theory]
    [InlineData(4)]
    [InlineData(2)]
    public void FailsWhenTheNumberOfUniqueRecordsDiffersFromTotalRecords(int totalRecords)
    {
        var error = Assert.Throws<InvalidDatasetException>(() => validator.Validate(
        [
            Page(1, totalPages: 2, totalRecords, [Record(1001), Record(1002)]),
            Page(2, totalPages: 2, totalRecords, [Record(1002), Record(1003)]),
        ], RequestedPageSize));

        Assert.Equal(
            $"The Inventory API reported total records {totalRecords}, but the number of unique records was 3.",
            error.Message);
    }

    [Fact]
    public void AcceptsANegativeQuantityABlankOrMissingNameAndAMissingLastUpdatedTime()
    {
        var records = validator.Validate(
        [
            Page(1, totalPages: 3, totalRecords: 5, [Record(1001, quantity: -1), Record(1002, name: "")]),
            Page(2, totalPages: 3, totalRecords: 5, [Record(1003, name: "   "), Record(1004, name: null)]),
            Page(3, totalPages: 3, totalRecords: 5, [Record(1005, lastUpdated: null)]),
        ], RequestedPageSize);

        Assert.Equal([1001, 1002, 1003, 1004, 1005], records.Select(record => record.Id));
    }

    [Fact]
    public void AcceptsPagesHoldingMoreOrFewerRecordsThanThePageSize()
    {
        // As the Inventory API does: page 2 starts with a copy of page 1's last record, so it holds
        // one more than the page size, and the last page holds fewer.
        var records = validator.Validate(
        [
            Page(1, totalPages: 3, totalRecords: 5, [Record(1001), Record(1002)]),
            Page(2, totalPages: 3, totalRecords: 5, [Record(1002), Record(1003), Record(1004)]),
            Page(3, totalPages: 3, totalRecords: 5, [Record(1005)]),
        ], RequestedPageSize);

        Assert.Equal([1001, 1002, 1003, 1004, 1005], records.Select(record => record.Id));
    }

    private static InventoryPage Page(
        int page, int totalPages, int totalRecords, InventoryRecord[] records, int pageSize = RequestedPageSize) =>
        new(page, pageSize, totalPages, totalRecords, records);

    private static InventoryRecord Record(
        int id,
        decimal price = 1.00m,
        int quantity = 1,
        string? name = "Widget",
        string? category = "Toys",
        DateTimeOffset? lastUpdated = null) =>
        new(id, name, category, price, quantity, lastUpdated);
}
