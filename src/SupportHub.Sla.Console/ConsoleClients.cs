using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SupportHub.Presentation;

namespace SupportHub.Sla.ConsoleApp;

public sealed class CustomerConsoleClient(IHttpClientFactory httpClientFactory)
{
    public async Task<IReadOnlyList<ConsoleCustomer>> GetCustomersAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory.CreateClient("customers")
            .GetAsync("/customers", cancellationToken);
        return await ReadAsync<IReadOnlyList<ConsoleCustomer>>(response, "customers", cancellationToken);
    }

    public async Task<HttpStatusCode> GetHealthAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory.CreateClient("customers")
            .GetAsync("/health", cancellationToken);
        return response.StatusCode;
    }

    private static async Task<T> ReadAsync<T>(
        HttpResponseMessage response,
        string subject,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new ConsoleApiException(subject, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new ConsoleApiContractException($"The Tickets API returned an empty {subject} response.");
    }

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed class TicketConsoleClient(IHttpClientFactory httpClientFactory)
{
    public async Task<IReadOnlyList<ConsoleTicket>> GetTicketsAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory.CreateClient("tickets")
            .GetAsync("/tickets", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ConsoleApiException("tickets", response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<ConsoleTicket>>(
                   CustomerConsoleClient.JsonOptions,
                   cancellationToken)
               ?? throw new ConsoleApiContractException("The Tickets API returned an empty tickets response.");
    }

    public async Task<HttpStatusCode> GetHealthAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory.CreateClient("tickets")
            .GetAsync("/health", cancellationToken);
        return response.StatusCode;
    }
}

public sealed record ConsoleCustomer(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    SupportTier SupportTier);

public sealed record ConsoleTicket(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    string? Assignee,
    Guid CustomerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class ConsoleApiException(string subject, HttpStatusCode statusCode)
    : Exception($"The {subject} service returned HTTP {(int)statusCode} ({statusCode}).")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class ConsoleApiContractException(string message) : Exception(message);
