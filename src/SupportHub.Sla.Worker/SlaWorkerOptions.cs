namespace SupportHub.Sla.Worker;

public sealed class SlaWorkerOptions
{
    public int IntervalSeconds { get; init; } = 300;
    public int MaxItems { get; init; } = 50;

    public TimeSpan Interval => TimeSpan.FromSeconds(IntervalSeconds);
}
