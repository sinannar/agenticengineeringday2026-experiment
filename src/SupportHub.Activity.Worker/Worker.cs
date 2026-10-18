using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using SupportHub.Activity.Contracts;

namespace SupportHub.Activity.Worker;

public sealed class Worker(
    ServiceBusClient serviceBusClient,
    ActivityStore activityStore,
    ILogger<Worker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private ServiceBusProcessor? processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await activityStore.EnsureCreatedAsync(stoppingToken);
        processor = serviceBusClient.CreateProcessor(
            "ticket-events",
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 4,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
            });
        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;
        await processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
            await processor.DisposeAsync();
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        ActivityEvent? activity;
        try
        {
            activity = JsonSerializer.Deserialize<ActivityEvent>(args.Message.Body, JsonOptions);
            if (activity is null || activity.EventId == Guid.Empty || activity.TicketId == Guid.Empty
                || activity.SchemaVersion != 1 || string.IsNullOrWhiteSpace(activity.EventType))
            {
                throw new InvalidDataException("The activity event envelope is invalid.");
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            await args.DeadLetterMessageAsync(args.Message, "invalid-event", exception.Message);
            logger.LogError(exception, "Dead-lettered malformed ticket activity message {MessageId}.", args.Message.MessageId);
            return;
        }

        try
        {
            var inserted = await activityStore.SaveAsync(activity, args.CancellationToken);
            await args.CompleteMessageAsync(args.Message);
            logger.LogInformation(
                "Processed ticket activity event {EventId} for ticket {TicketId}; inserted {Inserted}.",
                activity.EventId,
                activity.TicketId,
                inserted);
        }
        catch (InvalidDataException exception)
        {
            await args.DeadLetterMessageAsync(args.Message, "conflicting-event", exception.Message);
            logger.LogError(exception, "Dead-lettered conflicting ticket activity event {EventId}.", activity.EventId);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Transient failure processing ticket activity event {EventId} for ticket {TicketId}; message will be redelivered.",
                activity.EventId,
                activity.TicketId);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Service Bus processor error for entity {EntityPath} in namespace {FullyQualifiedNamespace}.",
            args.EntityPath,
            args.FullyQualifiedNamespace);
        return Task.CompletedTask;
    }
}
