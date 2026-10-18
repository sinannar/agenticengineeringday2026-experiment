using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Cosmos;
using SupportHub.Activity.Contracts;
using SupportHub.Activity.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureServiceBusClient("messaging");
builder.AddAzureCosmosClient("activitydb");

builder.Services.AddSingleton<ActivityStore>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.Services.GetRequiredService<ActivityStore>().EnsureCreatedAsync(CancellationToken.None);

host.MapGet("/internal/activity/{ticketId:guid}", async Task<Results<Ok<ActivityHistoryResponse>, StatusCodeHttpResult>> (
    Guid ticketId,
    ActivityStore store,
    CancellationToken cancellationToken) =>
{
    try
    {
        var items = await store.GetHistoryAsync(ticketId, 100, cancellationToken);
        return TypedResults.Ok(new ActivityHistoryResponse(
            ticketId,
            items.Select(x => new ActivityHistoryItem(
                Guid.Parse(x.Id),
                x.EventType,
                Guid.Parse(x.TicketId),
                DateTimeOffset.Parse(x.OccurredAtUtc),
                x.Actor,
                JsonDocument.Parse(x.DetailsJson).RootElement)).ToArray()));
    }
    catch (CosmosException)
    {
        return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});

host.MapDefaultEndpoints();
await host.RunAsync();

public sealed record ActivityHistoryResponse(Guid TicketId, IReadOnlyList<ActivityHistoryItem> Items);

public sealed record ActivityHistoryItem(
    Guid Id,
    string EventType,
    Guid TicketId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    System.Text.Json.JsonElement Details);
