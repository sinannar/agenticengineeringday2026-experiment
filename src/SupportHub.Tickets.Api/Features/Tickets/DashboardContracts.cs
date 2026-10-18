namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed record DashboardSummaryResponse(
    DateTimeOffset EvaluatedAtUtc,
    int TotalTickets,
    int OpenTickets,
    IReadOnlyList<DashboardStatusBreakdown> StatusBreakdown,
    IReadOnlyList<DashboardPriorityBreakdown> PriorityBreakdown,
    int AtRiskSlaTickets,
    int BreachedSlaTickets,
    DashboardDataQuality DataQuality);

public sealed record DashboardStatusBreakdown(TicketStatus Status, int Count);

public sealed record DashboardPriorityBreakdown(TicketPriority Priority, int Count);

public enum DashboardDataQuality
{
    Complete
}

public sealed class DashboardSummaryUnavailableException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}
