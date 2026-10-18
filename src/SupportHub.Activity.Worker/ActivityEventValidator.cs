using System.Text.Json;
using System.Text.Json.Serialization;
using SupportHub.Activity.Contracts;

namespace SupportHub.Activity.Worker;

public static class ActivityEventValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static ActivityEvent Validate(string payload)
    {
        var activity = JsonSerializer.Deserialize<ActivityEvent>(payload, JsonOptions);
        if (activity is null || activity.EventId == Guid.Empty || activity.TicketId == Guid.Empty
            || activity.SchemaVersion != 1 || string.IsNullOrWhiteSpace(activity.EventType))
        {
            throw new InvalidDataException("The activity event envelope is invalid.");
        }

        return activity;
    }
}
