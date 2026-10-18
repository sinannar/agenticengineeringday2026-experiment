using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task CompensationDeleteReportsSuccessAndAttemptsTheBlobDelete()
    {
        var store = new FakeAttachmentBlobStore();

        var error = await AttachmentFailureHandling.TryDeleteBlobAsync(
            store,
            "tickets/ticket/attachment",
            CancellationToken.None);

        Assert.Null(error);
        Assert.Equal(["tickets/ticket/attachment"], store.DeletedBlobNames);
    }

    [Fact]
    public async Task CompensationDeleteReturnsFailureWithoutThrowing()
    {
        var expected = new RequestFailedException(503, "storage unavailable");
        var store = new FakeAttachmentBlobStore(expected);

        var error = await AttachmentFailureHandling.TryDeleteBlobAsync(
            store,
            "tickets/ticket/attachment",
            CancellationToken.None);

        Assert.Same(expected, error);
        Assert.Equal(["tickets/ticket/attachment"], store.DeletedBlobNames);
    }

    [Fact]
    public async Task MetadataSaveProblemsDistinguishSuccessfulAndFailedCompensation()
    {
        var metadataOnly = await ExecuteProblemAsync(
            AttachmentFailureHandling.MetadataSaveProblem(null));
        var compensationFailed = await ExecuteProblemAsync(
            AttachmentFailureHandling.MetadataSaveProblem(new InvalidOperationException()));

        Assert.Equal(503, metadataOnly.StatusCode);
        Assert.Equal(
            "https://supporthub.dev/problems/attachment-metadata-unavailable",
            metadataOnly.Type);
        Assert.Equal(503, compensationFailed.StatusCode);
        Assert.Equal(
            "https://supporthub.dev/problems/attachment-compensation-failed",
            compensationFailed.Type);
    }

    [Fact]
    public async Task MetadataSaveFailureCompensatesUploadedBlob()
    {
        var store = new FakeAttachmentBlobStore();
        var persisted = false;

        var result = await AttachmentFailureHandling.SaveMetadataWithCompensationAsync(
            () => Task.FromException(
                new DbUpdateException("metadata unavailable")),
            store,
            "tickets/ticket/attachment",
            CancellationToken.None);

        Assert.False(persisted);
        Assert.Equal(["tickets/ticket/attachment"], store.DeletedBlobNames);
        var problem = await ExecuteProblemAsync(result!);
        Assert.Equal(
            "https://supporthub.dev/problems/attachment-metadata-unavailable",
            problem.Type);
    }

    [Fact]
    public async Task CompensationFailureUsesReconciliationProblem()
    {
        var store = new FakeAttachmentBlobStore(
            new RequestFailedException(503, "storage unavailable"));

        var result = await AttachmentFailureHandling.SaveMetadataWithCompensationAsync(
            () => Task.FromException(
                new DbUpdateException("metadata unavailable")),
            store,
            "tickets/ticket/attachment",
            CancellationToken.None);

        var problem = await ExecuteProblemAsync(result!);
        Assert.Equal(
            "https://supporthub.dev/problems/attachment-compensation-failed",
            problem.Type);
    }

    [Fact]
    public async Task RejectedUploadCleanupFailureUsesReconciliationProblem()
    {
        var store = new FakeAttachmentBlobStore(
            new RequestFailedException(503, "storage unavailable"));

        var result = await AttachmentFailureHandling.CleanupRejectedUploadAsync(
            store,
            "tickets/ticket/attachment",
            CancellationToken.None);

        var problem = await ExecuteProblemAsync(result!);
        Assert.Equal(
            "https://supporthub.dev/problems/attachment-compensation-failed",
            problem.Type);
    }

    [Fact]
    public async Task RejectedUploadCleanupPreservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var store = new FakeAttachmentBlobStore(
            new OperationCanceledException(cancellation.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            AttachmentFailureHandling.CleanupRejectedUploadAsync(
                store,
                "tickets/ticket/attachment",
                cancellation.Token));
    }

    [Fact]
    public async Task AttachmentDeleteDoesNotDeleteMetadataWhenBlobDeleteFails()
    {
        var metadataDeleted = false;
        var result = await AttachmentFailureHandling.DeleteContentThenMetadataAsync(
            () => Task.FromException(
                new RequestFailedException(503, "storage unavailable")),
            () =>
            {
                metadataDeleted = true;
                return Task.CompletedTask;
            });

        Assert.False(metadataDeleted);
        var problem = await ExecuteProblemAsync(result!);
        Assert.Equal(
            "https://supporthub.dev/problems/attachment-delete-failed",
            problem.Type);
    }

    [Fact]
    public async Task AttachmentDeleteRetainsMetadataWhenMetadataDeleteFails()
    {
        var blobDeleted = false;
        var result = await AttachmentFailureHandling.DeleteContentThenMetadataAsync(
            () =>
            {
                blobDeleted = true;
                return Task.CompletedTask;
            },
            () => Task.FromException(
                new DbUpdateException("metadata unavailable")));

        Assert.True(blobDeleted);
        var problem = await ExecuteProblemAsync(result!);
        Assert.Equal(
            "https://supporthub.dev/problems/attachment-metadata-unavailable",
            problem.Type);
    }

    [Fact]
    public async Task DeleteAndTicketCleanupStorageFailuresUseDistinctOperationTypes()
    {
        var delete = await ExecuteProblemAsync(
            AttachmentFailureHandling.StorageProblem(
                "delete",
                new RequestFailedException(503, "storage unavailable")));
        var ticketDelete = await ExecuteProblemAsync(
            AttachmentFailureHandling.StorageProblem(
                "ticket-delete",
                new RequestFailedException(404, "missing")));

        Assert.Equal(503, delete.StatusCode);
        Assert.Equal(
            "https://supporthub.dev/problems/attachment-delete-failed",
            delete.Type);
        Assert.Equal(404, ticketDelete.StatusCode);
        Assert.Equal(
            "https://supporthub.dev/problems/attachment-ticket-delete-failed",
            ticketDelete.Type);
    }

    private static Task<ProblemDetailsDocument> ExecuteProblemAsync(IResult result)
    {
        var problem = Assert.IsType<ProblemHttpResult>(result);
        var type = problem.ProblemDetails.Type
            ?? throw new InvalidOperationException("The problem type was not set.");
        return Task.FromResult(new ProblemDetailsDocument(problem.StatusCode, type));
    }

    private sealed record ProblemDetailsDocument(int StatusCode, string Type);

    private sealed class FakeAttachmentBlobStore(Exception? deleteException = null)
        : IAttachmentBlobStore
    {
        public List<string> DeletedBlobNames { get; } = [];

        public Task EnsureContainerAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UploadAsync(
            string blobName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<AttachmentBlobDownload?> OpenReadAsync(
            string blobName,
            string contentType,
            string fileName,
            CancellationToken cancellationToken) =>
            Task.FromResult<AttachmentBlobDownload?>(null);

        public Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> DeleteIfExistsAsync(
            string blobName,
            CancellationToken cancellationToken)
        {
            DeletedBlobNames.Add(blobName);
            return deleteException is null
                ? Task.FromResult(true)
                : Task.FromException<bool>(deleteException);
        }
    }
}
