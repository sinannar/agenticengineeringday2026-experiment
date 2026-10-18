namespace SupportHub.Tickets.Api.Features.Activity;

public enum OutboxEventStatus
{
    Pending,
    InProgress,
    Published,
    TerminalFailure
}

public sealed class OutboxEvent
{
    public Guid EventId { get; set; }
    public Guid TicketId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public OutboxEventStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public DateTimeOffset? ClaimExpiresAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset? LastFailedAtUtc { get; set; }
    public string? LastErrorCategory { get; set; }
}
