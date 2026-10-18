using Microsoft.Extensions.Options;

namespace SupportHub.Sla.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    SlaScanClient scanClient,
    IOptions<SlaWorkerOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.Interval;
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunScanAsync(options.Value.MaxItems, stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task RunScanAsync(int maxItems, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting scheduled SLA scan with max items {MaxItems}.", maxItems);
            var result = await scanClient.ScanAsync(new(MaxItems: maxItems), cancellationToken);
            logger.LogInformation(
                "Scheduled SLA scan completed at {EvaluatedAtUtc}: evaluated {EvaluatedCount} tickets {EvaluatedTicketIds}, failed {FailedCount}, at risk {AtRiskCount}, breached {BreachedCount}.",
                result.EvaluatedAtUtc,
                result.EvaluatedCount,
                result.Items.Select(item => item.TicketId).ToArray(),
                result.FailedCount,
                result.Items.Count(item => item.State == SlaState.AtRisk),
                result.Items.Count(item => item.State == SlaState.Breached));

            if (result.FailedCount > 0)
            {
                logger.LogWarning(
                    "Scheduled SLA scan recorded failure codes {FailureCodes}.",
                    result.Failures.Select(failure => failure.Code).Distinct().ToArray());
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Scheduled SLA scan cancelled during worker shutdown.");
        }
        catch (SlaScanHttpException exception)
        {
            logger.LogError(exception, "Scheduled SLA scan failed with HTTP status {StatusCode}.", exception.StatusCode);
        }
        catch (SlaScanContractException exception)
        {
            logger.LogError(exception, "Scheduled SLA scan returned an invalid response.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Scheduled SLA scan could not reach the Tickets API.");
        }
    }
}
