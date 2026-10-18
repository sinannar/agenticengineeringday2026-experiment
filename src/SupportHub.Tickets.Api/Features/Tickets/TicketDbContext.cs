using Microsoft.EntityFrameworkCore;
using SupportHub.Tickets.Api.Features.Activity;
using SupportHub.Tickets.Api.Features.Attachments;

namespace SupportHub.Tickets.Api.Features.Tickets;

public sealed class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

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

        var attachment = modelBuilder.Entity<Attachment>();
        attachment.HasKey(x => x.Id);
        attachment.Property(x => x.BlobName).HasMaxLength(512).IsRequired();
        attachment.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        attachment.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        attachment.Property(x => x.Size).IsRequired();
        attachment.Property(x => x.CreatedAt).IsRequired();
        attachment.HasIndex(x => x.BlobName).IsUnique();
        attachment.HasIndex(x => x.TicketId);

        var outbox = modelBuilder.Entity<OutboxEvent>();
        outbox.HasKey(x => x.EventId);
        outbox.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        outbox.Property(x => x.Payload).IsRequired();
        outbox.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        outbox.Property(x => x.LastErrorCategory).HasMaxLength(256);
        outbox.HasIndex(x => new { x.Status, x.NextAttemptAtUtc, x.OccurredAtUtc });
    }
}
