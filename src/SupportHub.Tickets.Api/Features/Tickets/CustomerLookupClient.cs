using System.Net;

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
}

public enum CustomerLookupResult
{
    Exists,
    NotFound,
    Unavailable
}
