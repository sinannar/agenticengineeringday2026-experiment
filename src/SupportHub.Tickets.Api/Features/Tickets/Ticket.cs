namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed class Ticket
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public string? Assignee { get; set; }
    public Guid CustomerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum TicketStatus
{
    New,
    Open,
    InProgress,
    Resolved,
    Closed
}

public enum TicketPriority
{
    Low,
    Medium,
    High
}
