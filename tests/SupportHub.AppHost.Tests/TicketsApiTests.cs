using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TicketsApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task TicketsApi_IsHealthy()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var client = app.CreateHttpClient("ticketsapi");

        using var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TicketsApi_ManagesTicketLifecycleAndFilters()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "Ada Lovelace", "ada@example.com");
        var secondCustomer = await CreateCustomerAsync(customersClient, "Grace Hopper", "grace@example.com");

        using var createResponse = await ticketsClient.PostAsJsonAsync("/tickets", new
        {
            title = "Cannot sign in",
            description = "The customer cannot sign in.",
            status = "Open",
            priority = "High",
            assignee = "Ada",
            customerId = customer.Id
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(customer.Id, created.CustomerId);
        Assert.Equal(created.CreatedAt, created.UpdatedAt);

        using var secondCreateResponse = await ticketsClient.PostAsJsonAsync("/tickets", new
        {
            title = "Password reset",
            description = "The customer needs a reset.",
            status = "New",
            priority = "Low",
            assignee = "Grace",
            customerId = secondCustomer.Id
        }, JsonOptions);
        secondCreateResponse.EnsureSuccessStatusCode();

        using var filteredResponse = await ticketsClient.GetAsync(
            $"/tickets?status=Open&priority=High&customerId={customer.Id}&assignee=Ada");
        filteredResponse.EnsureSuccessStatusCode();
        var filtered = await filteredResponse.Content.ReadFromJsonAsync<List<TicketResponse>>(JsonOptions);
        var filteredTicket = Assert.Single(filtered!);
        Assert.Equal(created.Id, filteredTicket.Id);

        using var updateResponse = await ticketsClient.PutAsJsonAsync($"/tickets/{created.Id}", new
        {
            title = "Cannot sign in - updated",
            description = "The customer still cannot sign in.",
            status = "Resolved",
            priority = "Medium",
            assignee = (string?)null,
            customerId = secondCustomer.Id
        }, JsonOptions);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(secondCustomer.Id, updated.CustomerId);
        Assert.True(updated.UpdatedAt >= updated.CreatedAt);

        using var invalidCustomerResponse = await ticketsClient.PostAsJsonAsync("/tickets", new
        {
            title = "Invalid customer",
            description = "This should fail.",
            status = "New",
            priority = "Low",
            assignee = (string?)null,
            customerId = Guid.NewGuid()
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCustomerResponse.StatusCode);

        using var deleteResponse = await ticketsClient.DeleteAsync($"/tickets/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var missingResponse = await ticketsClient.GetAsync($"/tickets/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    private static void ConfigureTestEnvironment()
    {
        Environment.SetEnvironmentVariable("SUPPORTHUB_TEST_MODE", "true");
        Environment.SetEnvironmentVariable("SUPPORTHUB_APPLY_MIGRATIONS", "true");
    }

    private static async Task<CustomerResponse> CreateCustomerAsync(
        HttpClient client,
        string name,
        string email)
    {
        using var response = await client.PostAsJsonAsync("/customers", new
        {
            name,
            email,
            phoneNumber = (string?)null,
            supportTier = "Standard"
        }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions))!;
    }
}

public sealed record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    string? Assignee,
    Guid CustomerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

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
