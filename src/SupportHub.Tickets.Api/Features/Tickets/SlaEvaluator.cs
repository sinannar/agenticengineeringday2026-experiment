namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed class SlaEvaluator(TimeProvider timeProvider)
{
    private static readonly TimeSpan AtRiskMinimumWindow = TimeSpan.FromHours(1);

    public SlaStatusResponse Evaluate(Ticket ticket, SupportTier supportTier) =>
        Evaluate(ticket, supportTier, timeProvider.GetUtcNow());

    public SlaStatusResponse Evaluate(
        Ticket ticket,
        SupportTier supportTier,
        DateTimeOffset evaluatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var evaluatedAt = evaluatedAtUtc.ToUniversalTime();
        var createdAt = ticket.CreatedAt.ToUniversalTime();
        if (createdAt > evaluatedAt)
        {
            throw new ArgumentException("Ticket creation time cannot be in the future.", nameof(ticket));
        }

        var target = SlaPolicy.GetTarget(ticket.Priority, supportTier);
        DateTimeOffset deadline;
        try
        {
            deadline = createdAt.Add(target);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ticket),
                ticket.CreatedAt,
                "Ticket creation time and SLA target produce an invalid deadline.");
        }

        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            return new SlaStatusResponse(
                ticket.Id,
                ticket.CustomerId,
                ticket.Priority,
                supportTier,
                ticket.Status,
                createdAt,
                target,
                deadline,
                evaluatedAt,
                SlaState.Completed,
                null,
                null,
                SlaPolicyMetadata.Version);
        }

        var remaining = deadline - evaluatedAt;
        var state = remaining < TimeSpan.Zero
            ? SlaState.Breached
            : remaining <= TimeSpan.FromTicks(target.Ticks / 5) || remaining <= AtRiskMinimumWindow
                ? SlaState.AtRisk
                : SlaState.OnTrack;

        return new SlaStatusResponse(
            ticket.Id,
            ticket.CustomerId,
            ticket.Priority,
            supportTier,
            ticket.Status,
            createdAt,
            target,
            deadline,
            evaluatedAt,
            state,
            state == SlaState.Breached ? null : remaining,
            state == SlaState.Breached ? -remaining : null,
            SlaPolicyMetadata.Version);
    }
}
