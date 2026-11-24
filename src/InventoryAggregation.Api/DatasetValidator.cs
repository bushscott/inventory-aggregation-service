namespace InventoryAggregation.Api;

/// <summary>
/// Checks that the fetched pages make a complete, consistent Dataset, and removes Repeated
/// Records. No HTTP, no state.
/// </summary>
public sealed class DatasetValidator
{
    /// <summary>
    /// Returns the unique records in the order they arrived. A Repeated Record keeps the position
    /// of its first copy.
    /// </summary>
    /// <param name="pages">The pages in the order they were requested, starting at page 1.</param>
    /// <param name="requestedPageSize">The page size every page was requested with.</param>
    /// <exception cref="InvalidDatasetException">
    /// A check fails. The message says what was expected and what arrived.
    /// </exception>
    public IReadOnlyList<InventoryRecord> Validate(IReadOnlyList<InventoryPage> pages, int requestedPageSize)
    {
        CheckEachPage(pages, requestedPageSize);

        var received = pages.SelectMany(page => page.Records).ToList();
        RejectNegativePrices(received);

        // GroupBy keeps the order in which each id first arrived.
        var unique = received.GroupBy(record => record.Id).Select(KeepOnce).ToList();

        var totalRecords = pages[0].TotalRecords;
        if (unique.Count != totalRecords)
        {
            throw new InvalidDatasetException(
                $"The Inventory API reported total records {totalRecords}, but the number of unique records was {unique.Count}.");
        }

        return unique;
    }

    /// <summary>
    /// Checks each page against the request it answered, and its totals against page 1's, which
    /// is the page that said how many pages to fetch.
    /// </summary>
    private static void CheckEachPage(IReadOnlyList<InventoryPage> pages, int requestedPageSize)
    {
        var firstPage = pages[0];

        for (var index = 0; index < pages.Count; index++)
        {
            var requestedPage = index + 1;
            var page = pages[index];

            if (page.Page != requestedPage)
            {
                throw new InvalidDatasetException(
                    $"The Inventory API's page {requestedPage} reported page number {page.Page}.");
            }

            if (page.PageSize != requestedPageSize)
            {
                throw new InvalidDatasetException(
                    $"The Inventory API's page {requestedPage} reported page size {page.PageSize}, but {requestedPageSize} was requested.");
            }

            if (page.TotalPages != firstPage.TotalPages)
            {
                throw new InvalidDatasetException(
                    $"The Inventory API's page {requestedPage} reported total pages {page.TotalPages}, but page 1 reported {firstPage.TotalPages}.");
            }

            if (page.TotalRecords != firstPage.TotalRecords)
            {
                throw new InvalidDatasetException(
                    $"The Inventory API's page {requestedPage} reported total records {page.TotalRecords}, but page 1 reported {firstPage.TotalRecords}.");
            }
        }

        if (pages.Count != firstPage.TotalPages)
        {
            throw new InvalidDatasetException(
                $"The Inventory API reported total pages {firstPage.TotalPages}, but the number of pages fetched was {pages.Count}.");
        }
    }

    private static void RejectNegativePrices(IEnumerable<InventoryRecord> records)
    {
        foreach (var record in records)
        {
            if (record.Price < 0)
            {
                // Invariant, so the price is written as the Inventory API writes it (-0.01, not -0,01).
                throw new InvalidDatasetException(FormattableString.Invariant(
                    $"Record {record.Id} has price {record.Price}, but a price cannot be negative."));
            }
        }
    }

    /// <summary>
    /// The one record that every copy of an id is identical to. <see cref="InventoryRecord"/> is a
    /// C# record, so == compares every field: a price by its value (1.0 equals 1.00) and a time by
    /// the instant it names, whatever its offset.
    /// </summary>
    /// <exception cref="InvalidDatasetException">The copies differ.</exception>
    private static InventoryRecord KeepOnce(IGrouping<int, InventoryRecord> copies)
    {
        var first = copies.First();
        if (copies.Any(copy => copy != first))
        {
            throw new InvalidDatasetException($"Record {first.Id} arrived more than once, and the copies differ.");
        }

        return first;
    }
}
