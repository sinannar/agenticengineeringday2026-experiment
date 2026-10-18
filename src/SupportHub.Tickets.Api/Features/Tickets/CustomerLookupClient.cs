using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed class CustomerLookupClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions CustomerJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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

    public async Task<CustomerTierBatchLookupResult> GetSupportTiersAsync(
        IReadOnlyCollection<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        if (customerIds.Count is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(customerIds));
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "customers/support-tiers",
                new CustomerTierBatchRequest(customerIds.ToArray()),
                CustomerJsonOptions,
                cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return CustomerTierBatchLookupResult.Unavailable;
            }

            if (!response.IsSuccessStatusCode)
            {
                return CustomerTierBatchLookupResult.Unavailable;
            }

            var tiers = await response.Content.ReadFromJsonAsync<IReadOnlyList<CustomerTierResponse>>(
                CustomerJsonOptions,
                cancellationToken);
            return tiers is null
                ? CustomerTierBatchLookupResult.Unavailable
                : CustomerTierBatchLookupResult.Success(tiers);
        }
        catch (HttpRequestException)
        {
            return CustomerTierBatchLookupResult.Unavailable;
        }
        catch (JsonException)
        {
            return CustomerTierBatchLookupResult.Unavailable;
        }
    }
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

public sealed record CustomerTierBatchLookupResult(
    CustomerLookupResult Status,
    IReadOnlyList<CustomerTierResponse> Tiers)
{
    public static CustomerTierBatchLookupResult Unavailable { get; } =
        new(CustomerLookupResult.Unavailable, []);

    public static CustomerTierBatchLookupResult Success(IReadOnlyList<CustomerTierResponse> tiers) =>
        new(CustomerLookupResult.Exists, tiers);
}

public sealed record CustomerTierBatchRequest(IReadOnlyList<Guid> CustomerIds);

public sealed record CustomerTierResponse(Guid CustomerId, SupportTier SupportTier);
