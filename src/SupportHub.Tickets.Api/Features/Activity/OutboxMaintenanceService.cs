using Microsoft.EntityFrameworkCore;
using SupportHub.Tickets.Api.Features.Tickets;

namespace SupportHub.Tickets.Api.Features.Activity;

public sealed class OutboxMaintenanceService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxMaintenanceService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTimeOffset.UtcNow;
        var expired = await db.OutboxEvents
            .Where(x => x.Status == OutboxEventStatus.InProgress && x.ClaimExpiresAtUtc < now)
            .ToListAsync(cancellationToken);
        foreach (var item in expired)
        {
            item.Status = OutboxEventStatus.Pending;
            item.ClaimExpiresAtUtc = null;
            item.NextAttemptAtUtc = now;
        }

        var cutoff = now.Subtract(Retention);
        var removed = await db.OutboxEvents
            .Where(x => x.Status == OutboxEventStatus.Published && x.PublishedAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
        if (expired.Count > 0 || removed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Outbox maintenance recovered {RecoveredCount} claims and removed {RemovedCount} published rows.",
                expired.Count,
                removed);
        }
    }
}

public static class OutboxMaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapOutboxMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/internal/outbox").WithTags("Internal Outbox");
        group.MapGet("/terminal", async (TicketDbContext db, CancellationToken cancellationToken) =>
            await db.OutboxEvents
                .AsNoTracking()
                .Where(x => x.Status == OutboxEventStatus.TerminalFailure)
                .OrderBy(x => x.OccurredAtUtc)
                .Select(x => new OutboxSummary(
                    x.EventId,
                    x.TicketId,
                    x.EventType,
                    x.Status,
                    x.AttemptCount,
                    x.LastErrorCategory,
                    x.OccurredAtUtc,
                    x.LastFailedAtUtc))
                .ToListAsync(cancellationToken));

        group.MapPost("/{eventId:guid}/replay", async (
            Guid eventId,
            TicketDbContext db,
            CancellationToken cancellationToken) =>
        {
            var item = await db.OutboxEvents.SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
            if (item is null)
            {
                return Results.NotFound();
            }

            item.Status = OutboxEventStatus.Pending;
            item.NextAttemptAtUtc = DateTimeOffset.UtcNow;
            item.ClaimExpiresAtUtc = null;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Accepted();
        });

        return endpoints;
    }
}

public sealed record OutboxSummary(
    Guid EventId,
    Guid TicketId,
    string EventType,
    OutboxEventStatus Status,
    int AttemptCount,
    string? LastErrorCategory,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? LastFailedAtUtc);
