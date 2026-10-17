using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class CustomerApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static void ConfigureTestEnvironment()
    {
        Environment.SetEnvironmentVariable("SUPPORTHUB_TEST_MODE", "true");
        Environment.SetEnvironmentVariable("SUPPORTHUB_APPLY_MIGRATIONS", "true");
    }

    [Fact]
    public async Task CustomerApi_ReturnsGreeting()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");

        var client = app.CreateHttpClient("customersapi");

        var response = await client.GetAsync("/hello");

        Assert.Equal("Hello World! from Customer Api", await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CustomerApi_IsHealthy()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");

        var client = app.CreateHttpClient("customersapi");

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CustomerApi_ManagesCustomerLifecycle()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");

        var client = app.CreateHttpClient("customersapi");
        var customer = new
        {
            name = "Ada Lovelace",
            email = "ada@example.com",
            phoneNumber = "+1 555 0100",
            supportTier = "Premium"
        };

        using var createResponse = await client.PostAsJsonAsync("/customers", customer, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Ada Lovelace", created.Name);
        Assert.Equal("Premium", created.SupportTier.ToString());

        using var getResponse = await client.GetAsync($"/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var updateResponse = await client.PutAsJsonAsync($"/customers/{created.Id}", new
        {
            name = "Augusta Ada King",
            email = "ada@example.com",
            phoneNumber = "+1 555 0100",
            supportTier = "Enterprise"
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var invalidResponse = await client.PostAsJsonAsync("/customers", new
        {
            name = "",
            email = "invalid",
            phoneNumber = (string?)null,
            supportTier = "Standard"
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal("application/problem+json", invalidResponse.Content.Headers.ContentType?.MediaType);

        using var deleteResponse = await client.DeleteAsync($"/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var missingResponse = await client.GetAsync($"/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }
}

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    SupportTier SupportTier);

public enum SupportTier
{
    Standard,
    Premium,
    Enterprise
}