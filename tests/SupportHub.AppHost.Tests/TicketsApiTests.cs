public class TicketsApiTests
{
    [Fact]
    public async Task TicketsApi_IsHealthy()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var client = app.CreateHttpClient("ticketsapi");

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello World! from Tickets Api", content);

        response.EnsureSuccessStatusCode();
    }
}