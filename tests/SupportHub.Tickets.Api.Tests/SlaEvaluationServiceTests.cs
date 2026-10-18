using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupportHub.Tickets.Api.Features.Tickets;

namespace SupportHub.Tickets.Api.Tests;

public sealed class SlaEvaluationServiceTests
{
    [Fact]
    public async Task ScanAsync_ReturnsSuccessfulItemsAndExplicitCustomerFailures()
    {
        var goodCustomer = Guid.NewGuid();
        var missingCustomer = Guid.NewGuid();
        var unavailableCustomer = Guid.NewGuid();
        var handler = new CustomerTierHandler(customerId =>
            customerId == goodCustomer
                ? new(HttpStatusCode.OK, "Premium")
                : customerId == missingCustomer
                    ? new(HttpStatusCode.NotFound, null)
                    : new(HttpStatusCode.ServiceUnavailable, null));
        await using var db = CreateDatabase(
            CreateTicket(goodCustomer),
            CreateTicket(missingCustomer),
            CreateTicket(unavailableCustomer));
        var service = CreateService(db, handler);

        var result = await service.ScanAsync(new(MaxItems: 10), CancellationToken.None);

        Assert.Equal(1, result.EvaluatedCount);
        Assert.Equal(2, result.FailedCount);
        Assert.Contains(result.Failures, failure => failure.Code == "CustomerNotFound");
        Assert.Contains(result.Failures, failure => failure.Code == "CustomerLookupUnavailable");
        Assert.Equal(SupportTier.Premium, Assert.Single(result.Items).SupportTier);
    }

    [Fact]
    public async Task ScanAsync_RespectsItemCapAndCustomerLookupConcurrency()
    {
        var handler = new CustomerTierHandler(
            _ => new(HttpStatusCode.OK, "Standard"),
            delay: TimeSpan.FromMilliseconds(10));
        var tickets = Enumerable.Range(0, 51)
            .Select(_ => CreateTicket(Guid.NewGuid()))
            .ToArray();
        await using var db = CreateDatabase(tickets);
        var service = CreateService(db, handler);

        var result = await service.ScanAsync(new(MaxItems: 50), CancellationToken.None);

        Assert.Equal(50, result.EvaluatedCount);
        Assert.Empty(result.Failures);
        Assert.InRange(handler.MaximumConcurrency, 1, SlaScanLimits.MaximumConcurrentCustomerLookups);
    }

    [Fact]
    public async Task ScanAsync_ReportsAllFailureResultsForUnavailableCustomerDependency()
    {
        var handler = new CustomerTierHandler(
            _ => new(HttpStatusCode.ServiceUnavailable, null));
        await using var db = CreateDatabase(CreateTicket(Guid.NewGuid()));
        var service = CreateService(db, handler);

        var result = await service.ScanAsync(new(MaxItems: 1), CancellationToken.None);

        Assert.Equal(0, result.EvaluatedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal("CustomerLookupUnavailable", Assert.Single(result.Failures).Code);
    }

    private static TicketDbContext CreateDatabase(params Ticket[] tickets)
    {
        var options = new DbContextOptionsBuilder<TicketDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new TicketDbContext(options);
        db.Tickets.AddRange(tickets);
        db.SaveChanges();
        return db;
    }

    private static SlaEvaluationService CreateService(
        TicketDbContext db,
        HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new("http://customers")
        };
        var timeProvider = new FixedTimeProvider(
            new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        return new(
            db,
            new CustomerLookupClient(httpClient),
            new SlaEvaluator(timeProvider),
            timeProvider);
    }

    private static Ticket CreateTicket(Guid customerId) =>
        new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "SLA test ticket",
            Description = "SLA test ticket",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Medium,
            CreatedAt = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero)
        };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class CustomerTierHandler(
        Func<Guid, (HttpStatusCode StatusCode, string? SupportTier)> responseFactory,
        TimeSpan? delay = null) : HttpMessageHandler
    {
        private int _currentConcurrency;

        public int MaximumConcurrency { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var customerId = Guid.Parse(request.RequestUri!.Segments[^1]);
            var concurrency = Interlocked.Increment(ref _currentConcurrency);
            MaximumConcurrency = Math.Max(MaximumConcurrency, concurrency);
            try
            {
                if (delay.HasValue)
                {
                    await Task.Delay(delay.Value, cancellationToken);
                }

                var response = responseFactory(customerId);
                var message = new HttpResponseMessage(response.StatusCode);
                if (response.SupportTier is not null)
                {
                    message.Content = JsonContent.Create(new
                    {
                        id = customerId,
                        supportTier = response.SupportTier
                    });
                }

                return message;
            }
            finally
            {
                Interlocked.Decrement(ref _currentConcurrency);
            }
        }
    }
}
