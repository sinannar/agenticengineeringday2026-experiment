public class TicketsApiTests
{
    [Fact]
    public async Task TicketsApi_ReturnsGreeting()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");

        var client = app.CreateHttpClient("ticketsapi");

        var response = await client.GetAsync("/hello");

        Assert.Equal("Hello World! from Tickets Api", await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TicketsApi_IsHealthy()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");

        var client = app.CreateHttpClient("ticketsapi");

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}