using System.Net;
using System.Net.Http.Json;

namespace SupportHub.Tickets.Api.Features.Activity;

public sealed class ActivityWorkerClient(HttpClient httpClient)
{
    public async Task<ActivityHistoryResponse> GetHistoryAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"internal/activity/{ticketId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            throw new ActivityWorkerUnavailableException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ActivityWorkerContractException();
        }

        return await response.Content.ReadFromJsonAsync<ActivityHistoryResponse>(cancellationToken: cancellationToken)
            ?? throw new ActivityWorkerContractException();
    }
}

public sealed record ActivityHistoryResponse(Guid TicketId, IReadOnlyList<ActivityHistoryItem> Items);

public sealed record ActivityHistoryItem(
    Guid Id,
    string EventType,
    Guid TicketId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    System.Text.Json.JsonElement Details);

public sealed class ActivityWorkerUnavailableException : Exception;

public sealed class ActivityWorkerContractException : Exception;
