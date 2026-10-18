namespace SupportHub.Tickets.Api.Features.Attachments;

public sealed class Attachment
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
