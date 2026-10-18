namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed record TicketRequest(
    string? Title,
    string? Description,
    TicketStatus Status,
    TicketPriority Priority,
    string? Assignee,
    Guid CustomerId);

public sealed record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    string? Assignee,
    Guid CustomerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class TicketValidation
{
    public const int TitleMaximumLength = 200;
    public const int DescriptionMaximumLength = 4_000;
    public const int AssigneeMaximumLength = 200;

    public static Dictionary<string, string[]> Validate(TicketRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (request.Title.Trim().Length > TitleMaximumLength)
        {
            errors["title"] = [$"Title must be {TitleMaximumLength} characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors["description"] = ["Description is required."];
        }
        else if (request.Description.Trim().Length > DescriptionMaximumLength)
        {
            errors["description"] = [$"Description must be {DescriptionMaximumLength} characters or fewer."];
        }

        if (!Enum.IsDefined(request.Status))
        {
            errors["status"] = ["Status must be New, Open, InProgress, Resolved, or Closed."];
        }

        if (!Enum.IsDefined(request.Priority))
        {
            errors["priority"] = ["Priority must be Low, Medium, or High."];
        }

        if (request.Assignee?.Length > AssigneeMaximumLength)
        {
            errors["assignee"] = [$"Assignee must be {AssigneeMaximumLength} characters or fewer."];
        }

        if (request.CustomerId == Guid.Empty)
        {
            errors["customerId"] = ["Customer ID is required."];
        }

        return errors;
    }

    public static TicketResponse ToResponse(Ticket ticket) =>
        new(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.Assignee,
            ticket.CustomerId,
            ticket.CreatedAt,
            ticket.UpdatedAt);

    public static void Apply(Ticket ticket, TicketRequest request)
    {
        ticket.Title = request.Title!.Trim();
        ticket.Description = request.Description!.Trim();
        ticket.Status = request.Status;
        ticket.Priority = request.Priority;
        ticket.Assignee = string.IsNullOrWhiteSpace(request.Assignee) ? null : request.Assignee.Trim();
        ticket.CustomerId = request.CustomerId;
    }
}
