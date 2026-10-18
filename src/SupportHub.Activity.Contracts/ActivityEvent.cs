using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportHub.Activity.Contracts;

public static class ActivityEventTypes
{
    public const string TicketCreated = "ticket.created";
    public const string TicketUpdated = "ticket.updated";
    public const string TicketStatusChanged = "ticket.status-changed";
    public const string TicketAssignmentChanged = "ticket.assignment-changed";
}

public sealed record ActivityEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    Guid TicketId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    JsonElement Details);

public sealed record TicketSnapshot(
    Guid Id,
    string Title,
    string Description,
    TicketStatusValue Status,
    TicketPriorityValue Priority,
    string? Assignee,
    Guid CustomerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TicketFieldChange(string Field, JsonElement Before, JsonElement After);

public sealed record TicketStatusChange(TicketStatusValue? Before, TicketStatusValue After);

public sealed record TicketAssignmentChange(string? Before, string? After);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketStatusValue
{
    New,
    Open,
    InProgress,
    Resolved,
    Closed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketPriorityValue
{
    Low,
    Medium,
    High
}
