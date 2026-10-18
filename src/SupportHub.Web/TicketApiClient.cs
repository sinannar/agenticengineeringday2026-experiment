using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportHub.Web;

public class TicketApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<TicketModel>> GetTicketsAsync(
        TicketFilterModel? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (filter?.Status is not null)
        {
            query.Add($"status={filter.Status}");
        }

        if (filter?.Priority is not null)
        {
            query.Add($"priority={filter.Priority}");
        }

        if (filter?.CustomerId is not null)
        {
            query.Add($"customerId={filter.CustomerId}");
        }

        if (!string.IsNullOrWhiteSpace(filter?.Assignee))
        {
            query.Add($"assignee={Uri.EscapeDataString(filter.Assignee)}");
        }

        var path = query.Count == 0 ? "tickets" : $"tickets?{string.Join("&", query)}";
        return await GetRequiredAsync<IReadOnlyList<TicketModel>>(path, cancellationToken);
    }

    public async Task<TicketModel?> GetTicketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"tickets/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TicketModel>(JsonOptions, cancellationToken)
            ?? throw new TicketApiException("The ticket API returned an empty response.");
    }

    public Task<TicketModel> CreateTicketAsync(TicketRequestModel request, CancellationToken cancellationToken = default) =>
        SendAsync<TicketModel>(HttpMethod.Post, "tickets", request, cancellationToken);

    public Task<TicketModel> UpdateTicketAsync(Guid id, TicketRequestModel request, CancellationToken cancellationToken = default) =>
        SendAsync<TicketModel>(HttpMethod.Put, $"tickets/{id}", request, cancellationToken);

    public async Task DeleteTicketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"tickets/{id}", cancellationToken);
        await EnsureSuccessAsync(response);
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new TicketApiException("The ticket API returned an empty response.");
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        TicketRequestModel request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        using var response = await httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new TicketApiException("The ticket API returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Please correct the highlighted ticket details.",
            HttpStatusCode.NotFound => "The ticket could not be found.",
            HttpStatusCode.ServiceUnavailable => "The customer service is unavailable. Please try again.",
            _ => "The ticket service is unavailable. Please try again."
        };

        throw new TicketApiException(message, response.StatusCode);
    }
}

public sealed record TicketModel(
    Guid Id,
    string Title,
    string Description,
    TicketStatusModel Status,
    TicketPriorityModel Priority,
    string? Assignee,
    Guid CustomerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class TicketRequestModel
{
    [Required]
    [StringLength(200)]
    public string? Title { get; set; }

    [Required]
    [StringLength(4_000)]
    public string? Description { get; set; }

    public TicketStatusModel Status { get; set; } = TicketStatusModel.New;
    public TicketPriorityModel Priority { get; set; } = TicketPriorityModel.Medium;

    [StringLength(200)]
    public string? Assignee { get; set; }

    [Required]
    public Guid CustomerId { get; set; }
}

public sealed class TicketFilterModel
{
    public TicketStatusModel? Status { get; set; }
    public TicketPriorityModel? Priority { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Assignee { get; set; }
}

public enum TicketStatusModel
{
    New,
    Open,
    InProgress,
    Resolved,
    Closed
}

public enum TicketPriorityModel
{
    Low,
    Medium,
    High
}

public sealed class TicketApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
