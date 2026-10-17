using System.Net;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportHub.Web;

public class CustomerApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<CustomerModel>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        await GetRequiredAsync<IReadOnlyList<CustomerModel>>("customers", cancellationToken);

    public async Task<CustomerModel?> GetCustomerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"customers/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CustomerModel>(JsonOptions, cancellationToken)
            ?? throw new CustomerApiException("The customer API returned an empty response.");
    }

    public Task<CustomerModel> CreateCustomerAsync(CustomerRequestModel request, CancellationToken cancellationToken = default) =>
        SendAsync<CustomerModel>(HttpMethod.Post, "customers", request, cancellationToken);

    public Task<CustomerModel> UpdateCustomerAsync(Guid id, CustomerRequestModel request, CancellationToken cancellationToken = default) =>
        SendAsync<CustomerModel>(HttpMethod.Put, $"customers/{id}", request, cancellationToken);

    public async Task DeleteCustomerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"customers/{id}", cancellationToken);
        await EnsureSuccessAsync(response);
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new CustomerApiException("The customer API returned an empty response.");
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        CustomerRequestModel request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        using var response = await httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new CustomerApiException("The customer API returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = response.StatusCode == HttpStatusCode.BadRequest
            ? "Please correct the highlighted customer details."
            : response.StatusCode == HttpStatusCode.NotFound
                ? "The customer could not be found."
                : "The customer service is unavailable. Please try again.";

        throw new CustomerApiException(message, response.StatusCode);
    }
}

public sealed record CustomerModel(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    SupportTierModel SupportTier);

public sealed class CustomerRequestModel
{
    [Required]
    [StringLength(200)]
    public string? Name { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? PhoneNumber { get; set; }
    public SupportTierModel SupportTier { get; set; } = SupportTierModel.Standard;
}

public enum SupportTierModel
{
    Standard,
    Premium,
    Enterprise
}

public sealed class CustomerApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
