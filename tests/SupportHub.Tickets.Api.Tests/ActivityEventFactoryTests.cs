using System.Text.Json;
using SupportHub.Tickets.Api.Features.Activity;
using SupportHub.Tickets.Api.Features.Tickets;

public class ActivityEventFactoryTests
{
    [Fact]
    public void CreatedEventContainsSnapshotAndStableEnvelope()
    {
        var ticket = Ticket("title", "description", TicketStatus.Open, TicketPriority.High, "Ada");

        var result = ActivityEventFactory.Created(ticket, DateTimeOffset.Parse("2026-10-18T12:00:00Z"));
        using var document = JsonDocument.Parse(result.Payload);
        var root = document.RootElement;

        Assert.Equal(result.EventId.ToString(), root.GetProperty("eventId").GetString());
        Assert.Equal("ticket.created", root.GetProperty("eventType").GetString());
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("Open", root.GetProperty("details").GetProperty("status").GetString());
        Assert.Equal("Ada", root.GetProperty("details").GetProperty("assignee").GetString());
    }

    [Fact]
    public void UpdatedEventPreservesOrderedChangesAndSpecializedTransitions()
    {
        var before = Ticket("old", "old description", TicketStatus.Open, TicketPriority.Low, null);
        var after = Ticket("new", "new description", TicketStatus.Resolved, TicketPriority.High, "Grace");

        var updated = ActivityEventFactory.Updated(before, after, DateTimeOffset.UtcNow);
        var status = ActivityEventFactory.StatusChanged(before, after, DateTimeOffset.UtcNow);
        var assignment = ActivityEventFactory.AssignmentChanged(before, after, DateTimeOffset.UtcNow);

        using var updatedJson = JsonDocument.Parse(updated.Payload);
        var changes = updatedJson.RootElement.GetProperty("details").GetProperty("changes");
        Assert.Equal(6, changes.GetArrayLength());
        Assert.Equal("title", changes[0].GetProperty("field").GetString());
        Assert.Equal("customerId", changes[5].GetProperty("field").GetString());

        using var statusJson = JsonDocument.Parse(status.Payload);
        Assert.Equal("Open", statusJson.RootElement.GetProperty("details").GetProperty("before").GetString());
        Assert.Equal("Resolved", statusJson.RootElement.GetProperty("details").GetProperty("after").GetString());

        using var assignmentJson = JsonDocument.Parse(assignment.Payload);
        Assert.Equal(JsonValueKind.Null, assignmentJson.RootElement.GetProperty("details").GetProperty("before").ValueKind);
        Assert.Equal("Grace", assignmentJson.RootElement.GetProperty("details").GetProperty("after").GetString());
    }

    private static Ticket Ticket(
        string title,
        string description,
        TicketStatus status,
        TicketPriority priority,
        string? assignee) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Status = status,
            Priority = priority,
            Assignee = assignee,
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
