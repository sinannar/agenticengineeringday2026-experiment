using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SupportHub.Tickets.Api.Features.Tickets;

namespace SupportHub.Tickets.Api.Tests;

public sealed class DashboardSummaryServiceTests
{
    [Fact]
    public async Task Summary_UsesOneTimestampAndCountsSlaStates()
    {
        var evaluatedAt = new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
        var customerId = Guid.NewGuid();
        var tickets = new[]
        {
            CreateTicket(customerId, TicketStatus.Open, TicketPriority.Low, evaluatedAt.AddHours(-1)),
            CreateTicket(customerId, TicketStatus.Open, TicketPriority.Medium, evaluatedAt.AddHours(-12)),
            CreateTicket(customerId, TicketStatus.Open, TicketPriority.High, evaluatedAt.AddHours(-3)),
            CreateTicket(customerId, TicketStatus.Resolved, TicketPriority.High, evaluatedAt.AddDays(-2))
        };
        await using var db = CreateDb(tickets);
        var customerLookup = new FakeCustomerLookupClient([customerId]);
        var service = CreateService(db, customerLookup, evaluatedAt);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(evaluatedAt, result.EvaluatedAtUtc);
        Assert.Equal(4, result.TotalTickets);
        Assert.Equal(3, result.OpenTickets);
        Assert.Equal(1, result.AtRiskSlaTickets);
        Assert.Equal(1, result.BreachedSlaTickets);
        Assert.Equal(4, result.StatusBreakdown.Sum(item => item.Count));
        Assert.Equal(4, result.PriorityBreakdown.Sum(item => item.Count));
    }

    [Fact]
    public async Task Summary_BatchesDistinctCustomersAtFiveHundred()
    {
        var evaluatedAt = new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
        var customerIds = Enumerable.Range(0, 1001).Select(_ => Guid.NewGuid()).ToArray();
        var tickets = customerIds
            .Select(customerId => CreateTicket(customerId, TicketStatus.Open, TicketPriority.Low, evaluatedAt))
            .ToArray();
        await using var db = CreateDb(tickets);
        var customerLookup = new FakeCustomerLookupClient(customerIds);
        var service = CreateService(db, customerLookup, evaluatedAt);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(1001, result.TotalTickets);
        Assert.Equal(3, customerLookup.BatchSizes.Count);
        Assert.All(customerLookup.BatchSizes, size => Assert.InRange(size, 1, 500));
    }

    [Fact]
    public async Task Summary_FailsClosedWhenCustomerTierIsMissing()
    {
        var evaluatedAt = new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
        var customerId = Guid.NewGuid();
        await using var db = CreateDb(
            [CreateTicket(customerId, TicketStatus.Open, TicketPriority.Low, evaluatedAt)]);
        var service = CreateService(db, new FakeCustomerLookupClient([]), evaluatedAt);

        var exception = await Assert.ThrowsAsync<DashboardSummaryUnavailableException>(
            () => service.GetSummaryAsync(CancellationToken.None));

        Assert.Equal("customer-support-tier-missing", exception.Code);
    }

    [Fact]
    public async Task Summary_RejectsPopulationAboveTenThousand()
    {
        var evaluatedAt = new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
        var tickets = Enumerable.Range(0, DashboardSummaryService.MaximumTicketPopulation + 1)
            .Select(_ => CreateTicket(Guid.NewGuid(), TicketStatus.Open, TicketPriority.Low, evaluatedAt))
            .ToArray();
        await using var db = CreateDb(tickets);
        var service = CreateService(db, new FakeCustomerLookupClient([]), evaluatedAt);

        var exception = await Assert.ThrowsAsync<DashboardSummaryUnavailableException>(
            () => service.GetSummaryAsync(CancellationToken.None));

        Assert.Equal("dashboard-population-limit-exceeded", exception.Code);
    }

    private static DashboardSummaryService CreateService(
        TicketDbContext db,
        ICustomerLookupClient customerLookup,
        DateTimeOffset evaluatedAt) =>
        new(
            db,
            customerLookup,
            new SlaEvaluator(new FixedTimeProvider(evaluatedAt)),
            new FixedTimeProvider(evaluatedAt),
            NullLogger<DashboardSummaryService>.Instance);

    private static TicketDbContext CreateDb(IReadOnlyCollection<Ticket> tickets)
    {
        var options = new DbContextOptionsBuilder<TicketDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new TicketDbContext(options);
        db.Tickets.AddRange(tickets);
        db.SaveChanges();
        return db;
    }

    private static Ticket CreateTicket(
        Guid customerId,
        TicketStatus status,
        TicketPriority priority,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = status,
            Priority = priority,
            Title = "Dashboard test",
            Description = "Dashboard test",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private sealed class FakeCustomerLookupClient(IEnumerable<Guid> customerIds) : ICustomerLookupClient
    {
        private readonly HashSet<Guid> customerIdSet = customerIds.ToHashSet();

        public List<int> BatchSizes { get; } = [];

        public Task<CustomerLookupResult> ExistsAsync(Guid customerId, CancellationToken cancellationToken) =>
            Task.FromResult(customerIdSet.Contains(customerId)
                ? CustomerLookupResult.Exists
                : CustomerLookupResult.NotFound);

        public Task<CustomerTierLookupResult> GetSupportTierAsync(
            Guid customerId,
            CancellationToken cancellationToken) =>
            Task.FromResult(customerIdSet.Contains(customerId)
                ? new CustomerTierLookupResult(CustomerLookupResult.Exists, SupportTier.Standard)
                : new CustomerTierLookupResult(CustomerLookupResult.NotFound, null));

        public Task<CustomerTierBatchLookupResult> GetSupportTiersAsync(
            IReadOnlyCollection<Guid> customerIds,
            CancellationToken cancellationToken)
        {
            BatchSizes.Add(customerIds.Count);
            var tiers = customerIds
                .Where(customerId => customerIdSet.Contains(customerId))
                .Select(customerId => new CustomerTierResponse(customerId, SupportTier.Standard))
                .ToArray();
            return Task.FromResult(CustomerTierBatchLookupResult.Success(tiers));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
