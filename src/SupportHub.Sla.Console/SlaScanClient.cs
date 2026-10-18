using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportHub.Sla.ConsoleApp;

public sealed class SlaScanClient(IHttpClientFactory httpClientFactory)
{
    public async Task<SlaScanResult> ScanAsync(
        SlaScanRequest request,
        CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("tickets");
        using var response = await client.PostAsJsonAsync(
            "/tickets/sla/scan",
            request,
            SlaJsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new SlaScanHttpException(response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<SlaScanResult>(
            SlaJsonOptions,
            cancellationToken);
        return result ?? throw new SlaScanContractException("The Tickets API returned an empty SLA scan response.");
    }

    private static readonly JsonSerializerOptions SlaJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed record SlaScanRequest(
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    int MaxItems = 50);

public sealed record SlaScanResult(
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<SlaStatusResult> Items,
    int EvaluatedCount,
    int FailedCount,
    IReadOnlyList<SlaScanFailure> Failures);

public sealed record SlaStatusResult(
    Guid TicketId,
    Guid CustomerId,
    TicketPriority Priority,
    SupportTier SupportTier,
    TicketStatus TicketStatus,
    DateTimeOffset CreatedAtUtc,
    TimeSpan TargetDuration,
    DateTimeOffset DeadlineUtc,
    DateTimeOffset EvaluatedAtUtc,
    SlaState State,
    TimeSpan? Remaining,
    TimeSpan? Lateness,
    string PolicyVersion);

public sealed record SlaScanFailure(
    Guid TicketId,
    Guid CustomerId,
    string Code,
    string Message);

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

public enum SupportTier
{
    Standard,
    Premium,
    Enterprise
}

public enum SlaState
{
    OnTrack,
    AtRisk,
    Breached,
    Completed
}

public sealed class SlaScanHttpException(HttpStatusCode statusCode)
    : Exception($"The Tickets API SLA scan returned HTTP {(int)statusCode} ({statusCode}).")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class SlaScanContractException(string message) : Exception(message);
