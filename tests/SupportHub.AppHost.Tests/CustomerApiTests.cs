public class CustomerApiTests
{
    [Fact]
    public async Task CustomerApi_ReturnsGreeting()
    {
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
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");

        var client = app.CreateHttpClient("customersapi");

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}