using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed class DashboardSummaryService(
    TicketDbContext db,
    ICustomerLookupClient customers,
    SlaEvaluator evaluator,
    TimeProvider timeProvider,
    ILogger<DashboardSummaryService> logger)
{
    public const int MaximumTicketPopulation = 10_000;
    public const int CustomerBatchSize = 500;
    public const int MaximumConcurrentCustomerBatches = 4;
    public static readonly TimeSpan CustomerBatchTimeout = TimeSpan.FromSeconds(2);

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        using var activity = DashboardTelemetry.ActivitySource.StartActivity("dashboard.summary");
        var startedAt = Stopwatch.GetTimestamp();
        var tickets = await db.Tickets
            .AsNoTracking()
            .OrderBy(ticket => ticket.Id)
            .Take(MaximumTicketPopulation + 1)
            .ToListAsync(cancellationToken);
        if (tickets.Count > MaximumTicketPopulation)
        {
            throw new DashboardSummaryUnavailableException(
                "dashboard-population-limit-exceeded",
                $"The dashboard supports up to {MaximumTicketPopulation} tickets.");
        }

        var ticketCount = tickets.Count;
        var evaluatedAt = timeProvider.GetUtcNow();
        var statusCounts = Enum.GetValues<TicketStatus>()
            .Select(status => new DashboardStatusBreakdown(
                status,
                tickets.Count(ticket => ticket.Status == status)))
            .ToArray();
        var priorityCounts = Enum.GetValues<TicketPriority>()
            .Select(priority => new DashboardPriorityBreakdown(
                priority,
                tickets.Count(ticket => ticket.Priority == priority)))
            .ToArray();

        var supportTiers = await GetSupportTiersAsync(
            tickets.Select(ticket => ticket.CustomerId).Distinct().ToArray(),
            cancellationToken);

        var atRiskCount = 0;
        var breachedCount = 0;
        foreach (var ticket in tickets)
        {
            if (!supportTiers.TryGetValue(ticket.CustomerId, out var supportTier))
            {
                throw new DashboardSummaryUnavailableException(
                    "customer-support-tier-missing",
                    "A ticket customer support tier was not available.");
            }

            var status = evaluator.Evaluate(ticket, supportTier, evaluatedAt);
            if (status.State == SlaState.AtRisk)
            {
                atRiskCount++;
            }
            else if (status.State == SlaState.Breached)
            {
                breachedCount++;
            }
        }

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        logger.LogInformation(
            "Dashboard summary evaluated at {EvaluatedAtUtc}: {TotalTickets} tickets, {OpenTickets} open, {AtRiskTickets} at risk, {BreachedTickets} breached in {DurationMs} ms.",
            evaluatedAt,
            ticketCount,
            tickets.Count(ticket => ticket.Status is TicketStatus.New or TicketStatus.Open or TicketStatus.InProgress),
            atRiskCount,
            breachedCount,
            elapsed.TotalMilliseconds);
        activity?.SetTag("supporthub.dashboard.ticket_count", ticketCount);
        activity?.SetTag("supporthub.dashboard.at_risk_count", atRiskCount);
        activity?.SetTag("supporthub.dashboard.breached_count", breachedCount);

        return new(
            evaluatedAt,
            ticketCount,
            tickets.Count(ticket => ticket.Status is TicketStatus.New or TicketStatus.Open or TicketStatus.InProgress),
            statusCounts,
            priorityCounts,
            atRiskCount,
            breachedCount,
            DashboardDataQuality.Complete);
    }

    private async Task<IReadOnlyDictionary<Guid, SupportTier>> GetSupportTiersAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        if (customerIds.Count == 0)
        {
            return new Dictionary<Guid, SupportTier>();
        }

        var batches = customerIds
            .Chunk(CustomerBatchSize)
            .ToArray();
        var tiers = new Dictionary<Guid, SupportTier>(customerIds.Count);
        for (var offset = 0; offset < batches.Length; offset += MaximumConcurrentCustomerBatches)
        {
            var batchTasks = batches
                .Skip(offset)
                .Take(MaximumConcurrentCustomerBatches)
                .Select(batch => GetSupportTierBatchAsync(batch, cancellationToken))
                .ToArray();

            CustomerTierBatchLookupResult?[] results;
            try
            {
                results = await Task.WhenAll(batchTasks);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new DashboardSummaryUnavailableException(
                    "customer-support-tier-timeout",
                    "The customer service did not provide support tiers within the dashboard timeout.");
            }

            foreach (var result in results)
            {
                if (result is null || result.Status != CustomerLookupResult.Exists)
                {
                    throw new DashboardSummaryUnavailableException(
                        "customer-support-tier-unavailable",
                        "The customer service did not provide the required support tiers.");
                }

                foreach (var tier in result.Tiers)
                {
                    tiers[tier.CustomerId] = tier.SupportTier;
                }
            }
        }

        return tiers;
    }

    private async Task<CustomerTierBatchLookupResult?> GetSupportTierBatchAsync(
        IReadOnlyCollection<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CustomerBatchTimeout);
        try
        {
            return await customers.GetSupportTiersAsync(customerIds, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}

public static class DashboardTelemetry
{
    public static readonly ActivitySource ActivitySource = new("SupportHub.Tickets.Api");
}
