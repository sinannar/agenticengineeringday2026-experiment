extern alias ActivityWorker;

using System.Text.Json;
using ActivityWorker::SupportHub.Activity.Worker;

public class ActivityEventValidatorTests
{
    [Fact]
    public void RejectsMalformedPayload()
    {
        Assert.Throws<JsonException>(() => ActivityEventValidator.Validate("{"));
    }

    [Fact]
    public void RejectsUnsupportedSchemaVersion()
    {
        var payload = """
            {
              "eventId": "00000000-0000-0000-0000-000000000001",
              "eventType": "ticket.created",
              "schemaVersion": 2,
              "ticketId": "00000000-0000-0000-0000-000000000010",
              "occurredAtUtc": "2026-10-18T12:00:00Z",
              "actor": null,
              "details": {}
            }
            """;

        Assert.Throws<InvalidDataException>(() => ActivityEventValidator.Validate(payload));
    }
}
