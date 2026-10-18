using Microsoft.EntityFrameworkCore;

namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed class SlaEvaluationService(
    TicketDbContext db,
    CustomerLookupClient customers,
    SlaEvaluator evaluator,
    TimeProvider timeProvider)
{
    public async Task<SlaStatusLookupResult> GetStatusAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            return SlaStatusLookupResult.TicketNotFound;
        }

        var customer = await customers.GetSupportTierAsync(ticket.CustomerId, cancellationToken);
        return customer.Status switch
        {
            CustomerLookupResult.NotFound => SlaStatusLookupResult.CustomerNotFound(ticket.CustomerId),
            CustomerLookupResult.Unavailable => SlaStatusLookupResult.CustomerUnavailable(ticket.CustomerId),
            CustomerLookupResult.Exists when customer.SupportTier.HasValue =>
                SlaStatusLookupResult.Success(evaluator.Evaluate(
                    ticket,
                    customer.SupportTier.Value,
                    timeProvider.GetUtcNow())),
            _ => SlaStatusLookupResult.CustomerUnavailable(ticket.CustomerId)
        };
    }

    public async Task<SlaScanResponse> ScanAsync(
        SlaScanRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MaxItems is < 1 or > SlaScanLimits.MaximumItems)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.MaxItems,
                $"MaxItems must be between 1 and {SlaScanLimits.MaximumItems}.");
        }

        var evaluatedAt = timeProvider.GetUtcNow();
        var query = db.Tickets.AsNoTracking();
        if (request.Status.HasValue)
        {
            query = query.Where(ticket => ticket.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(ticket => ticket.Priority == request.Priority.Value);
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.UpdatedAt)
            .ThenBy(ticket => ticket.Id)
            .Take(request.MaxItems)
            .ToListAsync(cancellationToken);

        var items = new List<SlaStatusResponse>(tickets.Count);
        var failures = new List<SlaScanFailure>();
        for (var offset = 0; offset < tickets.Count; offset += SlaScanLimits.MaximumConcurrentCustomerLookups)
        {
            var batch = tickets
                .Skip(offset)
                .Take(SlaScanLimits.MaximumConcurrentCustomerLookups)
                .Select(ticket => EvaluateTicketAsync(ticket, evaluatedAt, cancellationToken))
                .ToArray();

            foreach (var result in await Task.WhenAll(batch))
            {
                if (result.Status is not null)
                {
                    items.Add(result.Status);
                }
                else if (result.Failure is not null)
                {
                    failures.Add(result.Failure);
                }
            }
        }

        return new(
            evaluatedAt,
            items,
            items.Count,
            failures.Count,
            failures);
    }

    private async Task<SlaTicketEvaluationResult> EvaluateTicketAsync(
        Ticket ticket,
        DateTimeOffset evaluatedAt,
        CancellationToken cancellationToken)
    {
        var customer = await customers.GetSupportTierAsync(ticket.CustomerId, cancellationToken);
        return customer.Status switch
        {
            CustomerLookupResult.NotFound => SlaTicketEvaluationResult.FromFailure(
                new(ticket.Id, ticket.CustomerId, "CustomerNotFound", "The ticket customer does not exist.")),
            CustomerLookupResult.Unavailable => SlaTicketEvaluationResult.FromFailure(
                new(ticket.Id, ticket.CustomerId, "CustomerLookupUnavailable", "The customer service was unavailable.")),
            CustomerLookupResult.Exists when customer.SupportTier.HasValue =>
                SlaTicketEvaluationResult.Success(evaluator.Evaluate(
                    ticket,
                    customer.SupportTier.Value,
                    evaluatedAt)),
            _ => SlaTicketEvaluationResult.FromFailure(
                new(ticket.Id, ticket.CustomerId, "CustomerLookupUnavailable", "The customer support tier was unavailable."))
        };
    }
}

internal sealed record SlaTicketEvaluationResult(
    SlaStatusResponse? Status,
    SlaScanFailure? Failure)
{
    public static SlaTicketEvaluationResult Success(SlaStatusResponse status) => new(status, null);

    public static SlaTicketEvaluationResult FromFailure(SlaScanFailure failure) => new(null, failure);
}

public sealed record SlaStatusLookupResult(
    SlaStatusResponse? Status,
    SlaStatusLookupFailure? Failure)
{
    public static SlaStatusLookupResult TicketNotFound { get; } =
        new(null, new(SlaStatusLookupFailureCode.TicketNotFound, Guid.Empty));

    public static SlaStatusLookupResult CustomerNotFound(Guid customerId) =>
        new(null, new(SlaStatusLookupFailureCode.CustomerNotFound, customerId));

    public static SlaStatusLookupResult CustomerUnavailable(Guid customerId) =>
        new(null, new(SlaStatusLookupFailureCode.CustomerLookupUnavailable, customerId));

    public static SlaStatusLookupResult Success(SlaStatusResponse status) =>
        new(status, null);
}

public sealed record SlaStatusLookupFailure(
    SlaStatusLookupFailureCode Code,
    Guid CustomerId);

public enum SlaStatusLookupFailureCode
{
    TicketNotFound,
    CustomerNotFound,
    CustomerLookupUnavailable
}
