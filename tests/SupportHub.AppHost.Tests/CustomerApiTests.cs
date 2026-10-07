public class CustomerApiTests
{
    [Fact]
    public async Task CustomerApi_IsHealthy()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var client = app.CreateHttpClient("customersapi");

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello World! from Customer Api", content);

        response.EnsureSuccessStatusCode();
    }
}