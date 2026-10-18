using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using SupportHub.Tickets.Api.Features.Tickets;

namespace SupportHub.Tickets.Api.Features.Activity;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    ServiceBusClient serviceBusClient,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const string QueueName = "ticket-events";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(2);
    private const int MaxAttempts = 8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var sender = serviceBusClient.CreateSender(QueueName);
        while (!stoppingToken.IsCancellationRequested)
        {
            var published = await DispatchOneAsync(sender, stoppingToken);
            if (!published)
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> DispatchOneAsync(ServiceBusSender sender, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTimeOffset.UtcNow;

        var item = await db.OutboxEvents
            .Where(x =>
                (x.Status == OutboxEventStatus.Pending
                    && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now))
                || (x.Status == OutboxEventStatus.InProgress && x.ClaimExpiresAtUtc < now))
            .OrderBy(x => x.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return false;
        }

        item.Status = OutboxEventStatus.InProgress;
        item.ClaimExpiresAtUtc = now.Add(ClaimDuration);
        item.AttemptCount++;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var message = new ServiceBusMessage(item.Payload)
            {
                MessageId = item.EventId.ToString(),
                Subject = item.EventType,
                ContentType = "application/json",
                CorrelationId = item.TicketId.ToString()
            };
            await sender.SendMessageAsync(message, cancellationToken);

            item.Status = OutboxEventStatus.Published;
            item.PublishedAtUtc = DateTimeOffset.UtcNow;
            item.ClaimExpiresAtUtc = null;
            item.NextAttemptAtUtc = null;
            item.LastErrorCategory = null;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Published ticket activity event {EventId} for ticket {TicketId} on attempt {Attempt}.",
                item.EventId,
                item.TicketId,
                item.AttemptCount);
            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            var retryable = exception is ServiceBusException serviceBusException
                ? serviceBusException.IsTransient
                : exception is TimeoutException or IOException;
            item.LastFailedAtUtc = DateTimeOffset.UtcNow;
            item.LastErrorCategory = exception.GetType().Name;
            item.ClaimExpiresAtUtc = null;

            if (retryable && item.AttemptCount < MaxAttempts)
            {
                item.Status = OutboxEventStatus.Pending;
                item.NextAttemptAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Min(60, Math.Pow(2, item.AttemptCount)));
            }
            else
            {
                item.Status = OutboxEventStatus.TerminalFailure;
                item.NextAttemptAtUtc = null;
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogError(
                exception,
                "Failed to publish ticket activity event {EventId} for ticket {TicketId} on attempt {Attempt}; state {State}.",
                item.EventId,
                item.TicketId,
                item.AttemptCount,
                item.Status);
            return true;
        }
    }
}
