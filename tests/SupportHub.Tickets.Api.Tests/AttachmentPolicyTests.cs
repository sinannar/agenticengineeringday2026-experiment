using SupportHub.Tickets.Api.Features.Attachments;

public class AttachmentPolicyTests
{
    [Theory]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("notes.txt", "text/plain")]
    [InlineData("image.png", "image/png")]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.jpeg", "image/jpeg")]
    public void AcceptsPermittedFile(string fileName, string contentType)
    {
        var valid = AttachmentPolicy.TryValidate(
            fileName,
            contentType,
            128,
            out var safeFileName,
            out var error);

        Assert.True(valid);
        Assert.Equal(fileName, safeFileName);
        Assert.Empty(error);
    }

    [Theory]
    [InlineData("document.exe", "application/octet-stream")]
    [InlineData("document.pdf", "text/plain")]
    [InlineData("../document.pdf", "application/pdf")]
    [InlineData("folder/document.pdf", "application/pdf")]
    [InlineData("", "application/pdf")]
    public void RejectsUnsafeOrDisallowedFile(string fileName, string contentType)
    {
        var valid = AttachmentPolicy.TryValidate(
            fileName,
            contentType,
            128,
            out _,
            out var error);

        Assert.False(valid);
        Assert.NotEmpty(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10 * 1024 * 1024 + 1)]
    public void RejectsInvalidSize(long size)
    {
        var valid = AttachmentPolicy.TryValidate(
            "document.pdf",
            "application/pdf",
            size,
            out _,
            out var error);

        Assert.False(valid);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void GeneratesOpaqueTicketScopedBlobName()
    {
        var ticketId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();

        var blobName = AttachmentPolicy.BlobName(ticketId, attachmentId);

        Assert.Equal($"tickets/{ticketId:D}/{attachmentId:D}", blobName);
        Assert.DoesNotContain("document", blobName, StringComparison.OrdinalIgnoreCase);
    }
}
