using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed class CustomerLookupClient(HttpClient httpClient)
{
    public async Task<CustomerLookupResult> ExistsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync($"customers/{customerId}", cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => CustomerLookupResult.Exists,
                HttpStatusCode.NotFound => CustomerLookupResult.NotFound,
                _ => CustomerLookupResult.Unavailable
            };
        }
        catch (HttpRequestException)
        {
            return CustomerLookupResult.Unavailable;
        }
    }

    public async Task<CustomerTierLookupResult> GetSupportTierAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync($"customers/{customerId}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new(CustomerLookupResult.NotFound, null);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new(CustomerLookupResult.Unavailable, null);
            }

            var customer = await response.Content.ReadFromJsonAsync<CustomerTierResponse>(
                CustomerJsonOptions,
                cancellationToken);
            return customer is null
                ? new(CustomerLookupResult.Unavailable, null)
                : new(CustomerLookupResult.Exists, customer.SupportTier);
        }
        catch (HttpRequestException)
        {
            return new(CustomerLookupResult.Unavailable, null);
        }
        catch (JsonException)
        {
            return new(CustomerLookupResult.Unavailable, null);
        }
    }

    private static readonly JsonSerializerOptions CustomerJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record CustomerTierResponse(Guid Id, SupportTier SupportTier);
}

public enum CustomerLookupResult
{
    Exists,
    NotFound,
    Unavailable
}

public sealed record CustomerTierLookupResult(
    CustomerLookupResult Status,
    SupportTier? SupportTier);
