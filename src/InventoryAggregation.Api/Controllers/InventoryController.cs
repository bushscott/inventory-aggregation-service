using Microsoft.AspNetCore.Mvc;

namespace InventoryAggregation.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    [HttpGet("summary")]
    public InventorySummary GetSummary() => new(Categories: []);
}
