namespace SupportHub.Tickets.Api.Features.Tickets;

public static class SlaEndpoints
{
    public static IEndpointRouteBuilder MapSlaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/tickets/{id:guid}/sla", GetStatusAsync)
            .WithTags("Tickets");
        endpoints.MapPost("/tickets/sla/scan", ScanAsync)
            .WithTags("Tickets");

        return endpoints;
    }

    private static async Task<IResult> GetStatusAsync(
        Guid id,
        SlaEvaluationService sla,
        CancellationToken cancellationToken)
    {
        var result = await sla.GetStatusAsync(id, cancellationToken);
        if (result.Status is not null)
        {
            return Results.Ok(result.Status);
        }

        return result.Failure?.Code switch
        {
            SlaStatusLookupFailureCode.TicketNotFound => Results.NotFound(),
            SlaStatusLookupFailureCode.CustomerNotFound => Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["customerId"] = ["The ticket customer does not exist."]
                }),
            SlaStatusLookupFailureCode.CustomerLookupUnavailable => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://supporthub.dev/problems/customer-service-unavailable",
                title: "Customer service unavailable",
                detail: "The customer service could not provide the ticket support tier."),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "SLA evaluation failed",
                detail: "The ticket SLA could not be evaluated.")
        };
    }

    private static async Task<IResult> ScanAsync(
        SlaScanRequest request,
        SlaEvaluationService sla,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (request.MaxItems is < 1 or > SlaScanLimits.MaximumItems)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["maxItems"] = [$"MaxItems must be between 1 and {SlaScanLimits.MaximumItems}."]
            });
        }

        var result = await sla.ScanAsync(request, cancellationToken);
        var logger = loggerFactory.CreateLogger("SupportHub.Tickets.Api.SlaScan");
        logger.LogInformation(
            "SLA scan evaluated {EvaluatedCount} tickets and recorded {FailedCount} failures at {EvaluatedAtUtc} with max items {MaxItems}, status {Status}, priority {Priority}, and failure codes {FailureCodes}.",
            result.EvaluatedCount,
            result.FailedCount,
            result.EvaluatedAtUtc,
            request.MaxItems,
            request.Status,
            request.Priority,
            result.Failures.Select(failure => failure.Code).Distinct().ToArray());

        if (result.EvaluatedCount == 0 && result.FailedCount > 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://supporthub.dev/problems/sla-scan-unavailable",
                title: "SLA scan unavailable",
                detail: "No tickets could be evaluated because a required customer dependency was unavailable.");
        }

        return Results.Ok(result);
    }
}
