using System.ComponentModel.DataAnnotations;

namespace InventoryAggregation.Api;

/// <summary>
/// Settings for calling the Inventory API, read from the "InventoryApi" configuration section.
/// Locally they come from .NET user secrets; they are checked when the service starts.
/// </summary>
public sealed class InventoryApiOptions
{
    public const string SectionName = "InventoryApi";

    [Required(ErrorMessage = """InventoryApi:BaseUrl is missing. Set it with: dotnet user-secrets set "InventoryApi:BaseUrl" "<base URL>" --project src/InventoryAggregation.Api""")]
    public string BaseUrl { get; set; } = "";

    [Required(ErrorMessage = """InventoryApi:ApiKey is missing. Set it with: dotnet user-secrets set "InventoryApi:ApiKey" "<API key>" --project src/InventoryAggregation.Api""")]
    public string ApiKey { get; set; } = "";

    /// <summary>Records per page. 25 is the most the Inventory API allows, so it needs the fewest requests.</summary>
    public int PageSize { get; set; } = 25;
}
