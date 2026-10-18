using System.Net;
using System.Net.Http.Json;
using SupportHub.Sla.ConsoleApp;

namespace SupportHub.AppHost.Tests;

public sealed class ConsoleApplicationTests
{
    [Fact]
    public async Task RunAsync_RendersHelpListsAndUnavailableSampleCommand()
    {
        var terminal = new FakeTerminal("h", "2", "3", "5", "0");
        var factory = new StaticHttpClientFactory(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/customers")
            {
                return JsonResponse(new[]
                {
                    new
                    {
                        id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        name = "Ada Lovelace",
                        email = "ada@example.com",
                        phoneNumber = (string?)null,
                        supportTier = "Premium"
                    }
                });
            }

            if (request.RequestUri?.AbsolutePath == "/tickets")
            {
                return JsonResponse(new[]
                {
                    new
                    {
                        id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        title = "Cannot sign in",
                        description = "Login fails",
                        status = "InProgress",
                        priority = "High",
                        assignee = "Support",
                        customerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        createdAt = DateTimeOffset.UtcNow.AddHours(-1),
                        updatedAt = DateTimeOffset.UtcNow
                    }
                });
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await new ConsoleApplication(
            terminal,
            new CustomerConsoleClient(factory),
            new TicketConsoleClient(factory),
            new SlaScanClient(factory)).RunAsync();

        var output = string.Join(Environment.NewLine, terminal.Output);
        Assert.Contains("Commands are read-only", output);
        Assert.Contains("Ada Lovelace", output);
        Assert.Contains("In Progress", output);
        Assert.Contains("Sample ticket data is unavailable", output);
    }

    private static HttpResponseMessage JsonResponse<T>(T value) =>
        new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(value)
        };

    private sealed class FakeTerminal(params string[] inputs) : IConsoleTerminal
    {
        private readonly Queue<string> _inputs = new(inputs);
        public List<string> Output { get; } = [];
        public bool IsInteractive => false;
        public void Clear() { }
        public string? ReadLine() => _inputs.Count == 0 ? null : _inputs.Dequeue();
        public bool TryReadKey(out ConsoleKey key)
        {
            key = default;
            return false;
        }
        public void Write(string value) => Output.Add(value);
        public void WriteLine(string value = "") => Output.Add(value);
    }

    private sealed class StaticHttpClientFactory(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(responseFactory))
        {
            BaseAddress = new("http://supporthub")
        };
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
