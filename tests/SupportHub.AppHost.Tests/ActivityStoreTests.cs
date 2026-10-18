extern alias ActivityWorker;

using ActivityWorker::SupportHub.Activity.Worker;

public class ActivityStoreTests
{
    [Fact]
    public void EquivalentDocumentsRequireAllImmutableFieldsToMatch()
    {
        var original = Document();

        Assert.True(original.IsEquivalentTo(Document()));
        Assert.False(original.IsEquivalentTo(Document(actor: "different-actor")));
        Assert.False(original.IsEquivalentTo(Document(schemaVersion: 2)));
        Assert.False(original.IsEquivalentTo(Document(detailsJson: """{"changed":true}""")));
    }

    private static ActivityDocument Document(
        string actor = "actor",
        int schemaVersion = 1,
        string detailsJson = """{"value":true}""") =>
        new()
        {
            Id = "event-id",
            EventType = "ticket.updated",
            TicketId = "ticket-id",
            OccurredAtUtc = "2026-10-18T12:00:00.0000000Z",
            SchemaVersion = schemaVersion,
            Actor = actor,
            DetailsJson = detailsJson
        };
}
