namespace SupportHub.Tickets.Api.Features.Tickets;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/tickets/dashboard-summary", GetSummaryAsync)
            .WithTags("Tickets");
        return endpoints;
    }

    private static async Task<IResult> GetSummaryAsync(
        DashboardSummaryService dashboard,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await dashboard.GetSummaryAsync(cancellationToken));
        }
        catch (DashboardSummaryUnavailableException exception)
        {
            loggerFactory
                .CreateLogger("SupportHub.Tickets.Api.Dashboard")
                .LogWarning(exception, "Dashboard summary unavailable with code {Code}.", exception.Code);

            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: $"https://supporthub.dev/problems/{exception.Code}",
                title: "Dashboard summary unavailable",
                detail: exception.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = exception.Code
                });
        }
    }
}
