namespace SupportHub.Tickets.Api.Features.Tickets;

public static class SlaPolicy
{
    public static TimeSpan GetTarget(TicketPriority priority, SupportTier supportTier)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unsupported ticket priority.");
        }

        if (!Enum.IsDefined(supportTier))
        {
            throw new ArgumentOutOfRangeException(nameof(supportTier), supportTier, "Unsupported customer support tier.");
        }

        return (supportTier, priority) switch
        {
            (SupportTier.Standard, TicketPriority.Low) => TimeSpan.FromHours(24),
            (SupportTier.Standard, TicketPriority.Medium) => TimeSpan.FromHours(12),
            (SupportTier.Standard, TicketPriority.High) => TimeSpan.FromHours(2),
            (SupportTier.Premium, TicketPriority.Low) => TimeSpan.FromHours(24),
            (SupportTier.Premium, TicketPriority.Medium) => TimeSpan.FromHours(12),
            (SupportTier.Premium, TicketPriority.High) => TimeSpan.FromHours(2),
            (SupportTier.Enterprise, TicketPriority.Low) => TimeSpan.FromHours(24),
            (SupportTier.Enterprise, TicketPriority.Medium) => TimeSpan.FromHours(12),
            (SupportTier.Enterprise, TicketPriority.High) => TimeSpan.FromHours(2),
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unsupported SLA policy input.")
        };
    }
}
