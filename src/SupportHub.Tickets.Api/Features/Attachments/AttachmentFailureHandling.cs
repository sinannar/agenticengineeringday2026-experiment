using Azure;
using Microsoft.EntityFrameworkCore;

namespace SupportHub.Tickets.Api.Features.Attachments;

internal static class AttachmentFailureHandling
{
    public static async Task<Exception?> TryDeleteBlobAsync(
        IAttachmentBlobStore blobStore,
        string blobName,
        CancellationToken cancellationToken)
    {
        try
        {
            await blobStore.DeleteIfExistsAsync(blobName, cancellationToken);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public static IResult MetadataSaveProblem(Exception? cleanupError) =>
        cleanupError is null
            ? Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://supporthub.dev/problems/attachment-metadata-unavailable",
                title: "Attachment metadata could not be saved",
                detail: "The attachment upload could not be completed.")
            : Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://supporthub.dev/problems/attachment-compensation-failed",
                title: "Attachment upload could not be completed",
                detail: "The uploaded content could not be reconciled safely.");

    public static IResult MetadataDeleteProblem() =>
        Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            type: "https://supporthub.dev/problems/attachment-metadata-unavailable",
            title: "Attachment metadata could not be deleted",
            detail: "The attachment content was deleted, but its metadata could not be removed. Retry the operation.");

    public static async Task<IResult?> SaveMetadataWithCompensationAsync(
        Func<Task> saveMetadata,
        IAttachmentBlobStore blobStore,
        string blobName,
        CancellationToken cancellationToken)
    {
        try
        {
            await saveMetadata();
            return null;
        }
        catch (Exception exception) when (exception is DbUpdateException or TimeoutException)
        {
            var cleanupError = await TryDeleteBlobAsync(blobStore, blobName, cancellationToken);
            return MetadataSaveProblem(cleanupError);
        }
    }

    public static async Task<IResult?> DeleteContentThenMetadataAsync(
        Func<Task> deleteBlob,
        Func<Task> deleteMetadata)
    {
        try
        {
            await deleteBlob();
        }
        catch (RequestFailedException exception)
        {
            return StorageProblem("delete", exception);
        }

        try
        {
            await deleteMetadata();
            return null;
        }
        catch (DbUpdateException)
        {
            return MetadataDeleteProblem();
        }
    }

    public static IResult StorageProblem(string operation, RequestFailedException exception) =>
        Results.Problem(
            statusCode: exception.Status == 404
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status503ServiceUnavailable,
            type: $"https://supporthub.dev/problems/attachment-{operation}-failed",
            title: $"Attachment {operation} failed",
            detail: exception.Status == 404
                ? "The attachment content could not be found."
                : "The attachment storage service is unavailable.");
}
