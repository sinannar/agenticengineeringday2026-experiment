using Microsoft.EntityFrameworkCore;

namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ticket = modelBuilder.Entity<Ticket>();

        ticket.HasKey(x => x.Id);
        ticket.Property(x => x.Title).HasMaxLength(TicketValidation.TitleMaximumLength).IsRequired();
        ticket.Property(x => x.Description).HasMaxLength(TicketValidation.DescriptionMaximumLength).IsRequired();
        ticket.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        ticket.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32).IsRequired();
        ticket.Property(x => x.Assignee).HasMaxLength(TicketValidation.AssigneeMaximumLength);
        ticket.Property(x => x.CustomerId).IsRequired();
        ticket.Property(x => x.CreatedAt).IsRequired();
        ticket.Property(x => x.UpdatedAt).IsRequired();

        ticket.HasIndex(x => x.Status);
        ticket.HasIndex(x => x.Priority);
        ticket.HasIndex(x => x.CustomerId);
        ticket.HasIndex(x => x.Assignee);
    }
}
