using SupportHub.Tickets.Api.Features.Tickets;

namespace SupportHub.Tickets.Api.Tests;

public sealed class SlaEvaluatorTests
{
    [Theory]
    [InlineData(SupportTier.Standard, TicketPriority.Low, 24)]
    [InlineData(SupportTier.Standard, TicketPriority.Medium, 12)]
    [InlineData(SupportTier.Standard, TicketPriority.High, 2)]
    [InlineData(SupportTier.Premium, TicketPriority.Low, 24)]
    [InlineData(SupportTier.Premium, TicketPriority.Medium, 12)]
    [InlineData(SupportTier.Premium, TicketPriority.High, 2)]
    [InlineData(SupportTier.Enterprise, TicketPriority.Low, 24)]
    [InlineData(SupportTier.Enterprise, TicketPriority.Medium, 12)]
    [InlineData(SupportTier.Enterprise, TicketPriority.High, 2)]
    public void PolicyMapsEveryPriorityAndTierCombination(
        SupportTier supportTier,
        TicketPriority priority,
        int targetHours)
    {
        Assert.Equal(TimeSpan.FromHours(targetHours), SlaPolicy.GetTarget(priority, supportTier));
    }

    [Fact]
    public void Evaluator_ReturnsOnTrackBeforeAtRiskThreshold()
    {
        var createdAt = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
        var evaluator = new SlaEvaluator(new FixedTimeProvider(createdAt.AddHours(1)));
        var ticket = CreateTicket(createdAt, TicketPriority.Low);

        var result = evaluator.Evaluate(ticket, SupportTier.Standard);

        Assert.Equal(SlaState.OnTrack, result.State);
        Assert.Equal(TimeSpan.FromHours(23), result.Remaining);
        Assert.Equal(SlaPolicyMetadata.Version, result.PolicyVersion);
    }

    [Fact]
    public void Evaluator_MarksAtRiskAtExactTwentyPercentThreshold()
    {
        var createdAt = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
        var evaluator = new SlaEvaluator(new FixedTimeProvider(createdAt.AddHours(19).AddMinutes(12)));
        var ticket = CreateTicket(createdAt, TicketPriority.Low);

        var result = evaluator.Evaluate(ticket, SupportTier.Standard);

        Assert.Equal(SlaState.AtRisk, result.State);
        Assert.Equal(TimeSpan.FromHours(4) + TimeSpan.FromMinutes(48), result.Remaining);
    }

    [Fact]
    public void Evaluator_MarksAtRiskAtExactOneHourThreshold()
    {
        var createdAt = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
        var evaluator = new SlaEvaluator(new FixedTimeProvider(createdAt.AddHours(1)));
        var ticket = CreateTicket(createdAt, TicketPriority.High);

        var result = evaluator.Evaluate(ticket, SupportTier.Enterprise);

        Assert.Equal(SlaState.AtRisk, result.State);
        Assert.Equal(TimeSpan.FromHours(1), result.Remaining);
    }

    [Fact]
    public void Evaluator_MarksExactDeadlineAtRiskAndAfterDeadlineBreached()
    {
        var createdAt = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
        var ticket = CreateTicket(createdAt, TicketPriority.Medium);

        var atDeadline = new SlaEvaluator(new FixedTimeProvider(createdAt.AddHours(12)))
            .Evaluate(ticket, SupportTier.Standard);
        var afterDeadline = new SlaEvaluator(new FixedTimeProvider(createdAt.AddHours(12).AddTicks(1)))
            .Evaluate(ticket, SupportTier.Standard);

        Assert.Equal(SlaState.AtRisk, atDeadline.State);
        Assert.Equal(SlaState.Breached, afterDeadline.State);
        Assert.Equal(TimeSpan.FromTicks(1), afterDeadline.Lateness);
        Assert.Null(afterDeadline.Remaining);
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public void Evaluator_StopsAtTerminalTicketStatus(TicketStatus status)
    {
        var createdAt = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
        var ticket = CreateTicket(createdAt, TicketPriority.High, status);
        var evaluator = new SlaEvaluator(new FixedTimeProvider(createdAt.AddDays(10)));

        var result = evaluator.Evaluate(ticket, SupportTier.Premium);

        Assert.Equal(SlaState.Completed, result.State);
        Assert.Null(result.Remaining);
        Assert.Null(result.Lateness);
    }

    [Fact]
    public void Evaluator_RejectsFutureTicketCreationTime()
    {
        var now = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
        var ticket = CreateTicket(now.AddMinutes(1), TicketPriority.Low);
        var evaluator = new SlaEvaluator(new FixedTimeProvider(now));

        Assert.Throws<ArgumentException>(() => evaluator.Evaluate(ticket, SupportTier.Standard));
    }

    private static Ticket CreateTicket(
        DateTimeOffset createdAt,
        TicketPriority priority,
        TicketStatus status = TicketStatus.Open) =>
        new()
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Priority = priority,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
