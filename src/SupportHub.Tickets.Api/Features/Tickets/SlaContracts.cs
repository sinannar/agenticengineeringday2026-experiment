namespace SupportHub.Tickets.Api.Features.Tickets;

public enum SlaState
{
    OnTrack,
    AtRisk,
    Breached,
    Completed
}

public enum SupportTier
{
    Standard,
    Premium,
    Enterprise
}

public sealed record SlaStatusResponse(
    Guid TicketId,
    Guid CustomerId,
    TicketPriority Priority,
    SupportTier SupportTier,
    TicketStatus TicketStatus,
    DateTimeOffset CreatedAtUtc,
    TimeSpan TargetDuration,
    DateTimeOffset DeadlineUtc,
    DateTimeOffset EvaluatedAtUtc,
    SlaState State,
    TimeSpan? Remaining,
    TimeSpan? Lateness,
    string PolicyVersion);

public sealed record SlaScanRequest(
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    int MaxItems = SlaScanLimits.DefaultMaxItems);

public sealed record SlaScanFailure(
    Guid TicketId,
    Guid CustomerId,
    string Code,
    string Message);

public sealed record SlaScanResponse(
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<SlaStatusResponse> Items,
    int EvaluatedCount,
    int FailedCount,
    IReadOnlyList<SlaScanFailure> Failures);

public static class SlaScanLimits
{
    public const int DefaultMaxItems = 50;
    public const int MaximumItems = 50;
    public const int MaximumConcurrentCustomerLookups = 4;
}

public static class SlaPolicyMetadata
{
    public const string Version = "v1";
}
