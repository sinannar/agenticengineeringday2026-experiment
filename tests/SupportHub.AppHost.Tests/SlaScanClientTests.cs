using System.Net;
using System.Net.Http.Json;
using WorkerClient = SupportHub.Sla.Worker.SlaScanClient;
using WorkerRequest = SupportHub.Sla.Worker.SlaScanRequest;
using WorkerHttpException = SupportHub.Sla.Worker.SlaScanHttpException;
using ConsoleClient = SupportHub.Sla.ConsoleApp.SlaScanClient;
using ConsoleRequest = SupportHub.Sla.ConsoleApp.SlaScanRequest;
using ConsoleContractException = SupportHub.Sla.ConsoleApp.SlaScanContractException;

namespace SupportHub.AppHost.Tests;

public sealed class SlaScanClientTests
{
    [Fact]
    public async Task WorkerClient_PostsScanAndDeserializesResult()
    {
        var ticketId = Guid.NewGuid();
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                evaluatedAtUtc = DateTimeOffset.UtcNow,
                items = new[]
                {
                    new
                    {
                        ticketId,
                        customerId = Guid.NewGuid(),
                        priority = "High",
                        supportTier = "Premium",
                        ticketStatus = "Open",
                        createdAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
                        targetDuration = "02:00:00",
                        deadlineUtc = DateTimeOffset.UtcNow.AddMinutes(90),
                        evaluatedAtUtc = DateTimeOffset.UtcNow,
                        state = "OnTrack",
                        remaining = "01:30:00",
                        lateness = (string?)null,
                        policyVersion = "v1"
                    }
                },
                evaluatedCount = 1,
                failedCount = 0,
                failures = Array.Empty<object>()
            })
        });

        var result = await new WorkerClient(new StaticHttpClientFactory(handler))
            .ScanAsync(new WorkerRequest(MaxItems: 1), CancellationToken.None);

        Assert.Equal(1, result.EvaluatedCount);
        Assert.Equal(ticketId, Assert.Single(result.Items).TicketId);
    }

    [Fact]
    public async Task WorkerClient_ReportsHttpFailure()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var exception = await Assert.ThrowsAsync<WorkerHttpException>(() =>
            new WorkerClient(new StaticHttpClientFactory(handler))
                .ScanAsync(new WorkerRequest(), CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Fact]
    public async Task ConsoleClient_RejectsEmptySuccessfulResponse()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create((object?)null)
        });

        await Assert.ThrowsAsync<ConsoleContractException>(() =>
            new ConsoleClient(new StaticHttpClientFactory(handler))
                .ScanAsync(new ConsoleRequest(), CancellationToken.None));
    }

    private sealed class StaticHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler)
        {
            BaseAddress = new("http://tickets")
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
