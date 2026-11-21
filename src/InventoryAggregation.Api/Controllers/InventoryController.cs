using Microsoft.AspNetCore.Mvc;

namespace InventoryAggregation.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController(InventorySummaryService summaryService) : ControllerBase
{
    [HttpGet("summary")]
    public Task<InventorySummary> GetSummary(CancellationToken cancellationToken) =>
        summaryService.GetSummaryAsync(cancellationToken);
}
