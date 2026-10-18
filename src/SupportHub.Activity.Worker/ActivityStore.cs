using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using SupportHub.Activity.Contracts;

namespace SupportHub.Activity.Worker;

public sealed class ActivityStore(CosmosClient cosmosClient)
{
    private const string DatabaseName = "activitydb";
    private const string ContainerName = "ticket-activity";
    private Container Container => cosmosClient.GetContainer(DatabaseName, ContainerName);

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        var database = cosmosClient.GetDatabase(DatabaseName);
        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(ContainerName, "/ticketId"),
            cancellationToken: cancellationToken);
    }

    public async Task<bool> SaveAsync(ActivityEvent activityEvent, CancellationToken cancellationToken)
    {
        var document = new ActivityDocument
        {
            Id = activityEvent.EventId.ToString(),
            EventType = activityEvent.EventType,
            TicketId = activityEvent.TicketId.ToString(),
            OccurredAtUtc = activityEvent.OccurredAtUtc.UtcDateTime.ToString("O"),
            SchemaVersion = activityEvent.SchemaVersion,
            Actor = activityEvent.Actor,
            DetailsJson = activityEvent.Details.GetRawText()
        };

        try
        {
            await Container.CreateItemAsync(document, new PartitionKey(document.TicketId), cancellationToken: cancellationToken);
            return true;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            var existing = await Container.ReadItemAsync<ActivityDocument>(
                document.Id,
                new PartitionKey(document.TicketId),
                cancellationToken: cancellationToken);
            if (existing.Resource.IsEquivalentTo(document))
            {
                return false;
            }

            throw new InvalidDataException($"Activity event {document.Id} conflicts with an existing document.");
        }
    }

    public async Task<IReadOnlyList<ActivityDocument>> GetHistoryAsync(
        Guid ticketId,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT TOP @limit * FROM c WHERE c.ticketId = @ticketId ORDER BY c.occurredAtUtc DESC, c.id DESC")
            .WithParameter("@limit", limit)
            .WithParameter("@ticketId", ticketId.ToString());
        using var iterator = Container.GetItemQueryIterator<ActivityDocument>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(ticketId.ToString()) });
        var items = new List<ActivityDocument>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            items.AddRange(page);
        }

        return items;
    }
}

public sealed class ActivityDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("eventType")]
    public string EventType { get; set; } = string.Empty;
    [JsonProperty("ticketId")]
    public string TicketId { get; set; } = string.Empty;
    [JsonProperty("occurredAtUtc")]
    public string OccurredAtUtc { get; set; } = string.Empty;
    [JsonProperty("schemaVersion")]
    public int SchemaVersion { get; set; }
    [JsonProperty("actor")]
    public string? Actor { get; set; }
    [JsonProperty("detailsJson")]
    public string DetailsJson { get; set; } = "{}";

    public bool IsEquivalentTo(ActivityDocument other) =>
        Id == other.Id
        && EventType == other.EventType
        && TicketId == other.TicketId
        && OccurredAtUtc == other.OccurredAtUtc
        && SchemaVersion == other.SchemaVersion
        && Actor == other.Actor
        && DetailsJson == other.DetailsJson;
}
