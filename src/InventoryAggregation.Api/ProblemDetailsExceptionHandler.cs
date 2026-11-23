using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAggregation.Api;

/// <summary>
/// The one place a failure becomes a response. Logs the failure once, with its full cause, and
/// answers with problem details carrying a trace identifier.
/// </summary>
public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Our two exception types carry messages written to be shown to a caller. Any other
        // exception's message may hold internals, so it gets a generic detail instead.
        var (status, title, detail) = exception switch
        {
            InventoryApiUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Inventory API unavailable", exception.Message),
            InvalidDatasetException => (StatusCodes.Status502BadGateway, "Invalid dataset", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred."),
        };

        logger.LogError(exception, "Request failed with {Status} {Title}.", status, title);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            // The same identifier the framework would use, so a caller can quote it when reporting a problem.
            Extensions = { ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier },
        };

        // Always written as JSON, whatever the caller's Accept header says, as the success response is.
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}
