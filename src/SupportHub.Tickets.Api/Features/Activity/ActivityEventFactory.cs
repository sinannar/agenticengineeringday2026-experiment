using System.Text.Json;
using System.Text.Json.Serialization;
using SupportHub.Activity.Contracts;
using SupportHub.Tickets.Api.Features.Tickets;

namespace SupportHub.Tickets.Api.Features.Activity;

public static class ActivityEventFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static OutboxEvent Created(Ticket ticket, DateTimeOffset occurredAtUtc) =>
        Create(
            ActivityEventTypes.TicketCreated,
            ticket,
            occurredAtUtc,
            new TicketSnapshot(
                ticket.Id,
                ticket.Title,
                ticket.Description,
                (TicketStatusValue)ticket.Status,
                (TicketPriorityValue)ticket.Priority,
                ticket.Assignee,
                ticket.CustomerId,
                ticket.CreatedAt,
                ticket.UpdatedAt));

    public static OutboxEvent Updated(
        Ticket before,
        Ticket after,
        DateTimeOffset occurredAtUtc)
    {
        var changes = new List<TicketFieldChange>();
        AddChange(changes, "title", before.Title, after.Title);
        AddChange(changes, "description", before.Description, after.Description);
        AddChange(changes, "status", (TicketStatusValue)before.Status, (TicketStatusValue)after.Status);
        AddChange(changes, "priority", (TicketPriorityValue)before.Priority, (TicketPriorityValue)after.Priority);
        AddChange(changes, "assignee", before.Assignee, after.Assignee);
        AddChange(changes, "customerId", before.CustomerId, after.CustomerId);

        return Create(ActivityEventTypes.TicketUpdated, after, occurredAtUtc, new { changes });
    }

    public static OutboxEvent StatusChanged(Ticket before, Ticket after, DateTimeOffset occurredAtUtc) =>
        Create(
            ActivityEventTypes.TicketStatusChanged,
            after,
            occurredAtUtc,
            new TicketStatusChange((TicketStatusValue)before.Status, (TicketStatusValue)after.Status));

    public static OutboxEvent AssignmentChanged(Ticket before, Ticket after, DateTimeOffset occurredAtUtc) =>
        Create(
            ActivityEventTypes.TicketAssignmentChanged,
            after,
            occurredAtUtc,
            new TicketAssignmentChange(before.Assignee, after.Assignee));

    private static void AddChange<T>(List<TicketFieldChange> changes, string field, T before, T after)
    {
        var beforeElement = JsonSerializer.SerializeToElement(before, JsonOptions);
        var afterElement = JsonSerializer.SerializeToElement(after, JsonOptions);
        if (!beforeElement.Equals(afterElement))
        {
            changes.Add(new TicketFieldChange(field, beforeElement, afterElement));
        }
    }

    private static OutboxEvent Create<T>(
        string eventType,
        Ticket ticket,
        DateTimeOffset occurredAtUtc,
        T details)
    {
        var eventId = Guid.NewGuid();
        var envelope = new ActivityEvent(
            eventId,
            eventType,
            1,
            ticket.Id,
            occurredAtUtc,
            null,
            JsonSerializer.SerializeToElement(details, JsonOptions));

        return new OutboxEvent
        {
            EventId = eventId,
            TicketId = ticket.Id,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(envelope, JsonOptions),
            Status = OutboxEventStatus.Pending,
            OccurredAtUtc = occurredAtUtc,
            NextAttemptAtUtc = occurredAtUtc
        };
    }
}
