using System.Net;
using Azure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using SupportHub.Tickets.Api.Features.Tickets;

namespace SupportHub.Tickets.Api.Features.Attachments;

public static class AttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/tickets/{ticketId:guid}/attachments").WithTags("Attachments");

        group.MapGet("", ListAsync);
        group.MapPost("", UploadAsync);
        group.MapGet("/{attachmentId:guid}/content", DownloadAsync);
        group.MapDelete("/{attachmentId:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid ticketId,
        TicketDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await TicketExistsAsync(ticketId, db, cancellationToken))
        {
            return Results.NotFound();
        }

        var attachments = await db.Attachments
            .AsNoTracking()
            .Where(x => x.TicketId == ticketId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => AttachmentPolicy.ToResponse(x))
            .ToListAsync(cancellationToken);

        return Results.Ok(attachments);
    }

    private static async Task<IResult> UploadAsync(
        Guid ticketId,
        HttpRequest request,
        TicketDbContext db,
        IAttachmentBlobStore blobStore,
        CancellationToken cancellationToken)
    {
        if (!await TicketExistsAsync(ticketId, db, cancellationToken))
        {
            return Results.NotFound();
        }

        if (request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } bodySizeFeature)
        {
            bodySizeFeature.MaxRequestBodySize = AttachmentPolicy.MaximumRequestSize;
        }

        if (!AttachmentPolicy.TryGetMultipartBoundary(request.ContentType, out var boundary))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["The request must be multipart/form-data with one file."]
            });
        }

        var reader = new MultipartReader(boundary, request.Body)
        {
            HeadersCountLimit = 16,
            HeadersLengthLimit = 16 * 1024,
            BodyLengthLimit = AttachmentPolicy.MaximumRequestSize
        };

        MultipartSection? fileSection = null;
        try
        {
            while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
            {
                if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) ||
                    !string.Equals(disposition.DispositionType.Value, "form-data", StringComparison.OrdinalIgnoreCase) ||
                    disposition.FileName.Value is null && disposition.FileNameStar.Value is null)
                {
                    continue;
                }

                if (fileSection is not null)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["file"] = ["Only one file may be uploaded at a time."]
                    });
                }

                fileSection = section;
                break;
            }
        }
        catch (InvalidDataException exception)
        {
            return AttachmentPolicy.TooLarge(exception.Message);
        }

        if (fileSection is null ||
            !ContentDispositionHeaderValue.TryParse(fileSection.ContentDisposition, out var fileDisposition))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["A file is required."]
            });
        }

        var fileName = fileDisposition.FileNameStar.Value ?? fileDisposition.FileName.Value;
        var contentType = fileSection.ContentType;
        var attachmentId = Guid.NewGuid();
        var blobName = AttachmentPolicy.BlobName(ticketId, attachmentId);
        var countingStream = new CountingReadStream(fileSection.Body, AttachmentPolicy.MaximumFileSize);
        var uploaded = false;

        try
        {
            await blobStore.EnsureContainerAsync(cancellationToken);
            await blobStore.UploadAsync(blobName, countingStream, contentType ?? string.Empty, cancellationToken);
            uploaded = true;
        }
        catch (AttachmentTooLargeException exception)
        {
            await TryDeleteBlobAsync(blobStore, blobName, cancellationToken);
            return AttachmentPolicy.TooLarge(exception.Message);
        }
        catch (RequestFailedException exception)
        {
            await TryDeleteBlobAsync(blobStore, blobName, cancellationToken);
            return StorageProblem("upload", exception);
        }

        if (!AttachmentPolicy.TryValidate(
                fileName,
                contentType,
                countingStream.BytesRead,
                out var safeFileName,
                out var validationError))
        {
            if (uploaded)
            {
                await TryDeleteBlobAsync(blobStore, blobName, cancellationToken);
            }

            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = [validationError]
            });
        }

        while (await reader.ReadNextSectionAsync(cancellationToken) is { } extraSection)
        {
            if (ContentDispositionHeaderValue.TryParse(extraSection.ContentDisposition, out var extraDisposition) &&
                string.Equals(extraDisposition.DispositionType.Value, "form-data", StringComparison.OrdinalIgnoreCase) &&
                (extraDisposition.FileName.Value is not null || extraDisposition.FileNameStar.Value is not null))
            {
                await TryDeleteBlobAsync(blobStore, blobName, cancellationToken);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Only one file may be uploaded at a time."]
                });
            }
        }

        var attachment = new Attachment
        {
            Id = attachmentId,
            TicketId = ticketId,
            BlobName = blobName,
            OriginalFileName = safeFileName,
            ContentType = contentType!,
            Size = countingStream.BytesRead,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Attachments.Add(attachment);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or TimeoutException)
        {
            var cleanupError = await TryDeleteBlobAsync(blobStore, blobName, cancellationToken);
            return cleanupError is null
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
        }

        return Results.Created(
            $"/tickets/{ticketId}/attachments/{attachment.Id}/content",
            AttachmentPolicy.ToResponse(attachment));
    }

    private static async Task<IResult> DownloadAsync(
        Guid ticketId,
        Guid attachmentId,
        TicketDbContext db,
        IAttachmentBlobStore blobStore,
        CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TicketId == ticketId && x.Id == attachmentId,
                cancellationToken);
        if (attachment is null)
        {
            return Results.NotFound();
        }

        try
        {
            var content = await blobStore.OpenReadAsync(
                attachment.BlobName,
                attachment.ContentType,
                attachment.OriginalFileName,
                cancellationToken);
            return content is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://supporthub.dev/problems/attachment-content-missing",
                    title: "Attachment content is missing",
                    detail: "The attachment metadata exists but its content could not be found.")
                : Results.Stream(
                    content.Content,
                    content.ContentType,
                    content.FileName,
                    enableRangeProcessing: true);
        }
        catch (RequestFailedException exception)
        {
            return StorageProblem("download", exception);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid ticketId,
        Guid attachmentId,
        TicketDbContext db,
        IAttachmentBlobStore blobStore,
        CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments
            .SingleOrDefaultAsync(
                x => x.TicketId == ticketId && x.Id == attachmentId,
                cancellationToken);
        if (attachment is null)
        {
            return Results.NotFound();
        }

        try
        {
            await blobStore.DeleteIfExistsAsync(attachment.BlobName, cancellationToken);
        }
        catch (RequestFailedException exception)
        {
            return StorageProblem("delete", exception);
        }

        db.Attachments.Remove(attachment);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://supporthub.dev/problems/attachment-metadata-unavailable",
                title: "Attachment metadata could not be deleted",
                detail: "The attachment content was deleted, but its metadata could not be removed. Retry the operation.");
        }

        return Results.NoContent();
    }

    internal static async Task<IResult?> CleanupForTicketAsync(
        Guid ticketId,
        TicketDbContext db,
        IAttachmentBlobStore blobStore,
        CancellationToken cancellationToken)
    {
        var attachments = await db.Attachments
            .Where(x => x.TicketId == ticketId)
            .ToListAsync(cancellationToken);

        foreach (var attachment in attachments)
        {
            try
            {
                await blobStore.DeleteIfExistsAsync(attachment.BlobName, cancellationToken);
            }
            catch (RequestFailedException exception)
            {
                return StorageProblem("ticket-delete", exception);
            }
        }

        db.Attachments.RemoveRange(attachments);
        return null;
    }

    private static async Task<bool> TicketExistsAsync(
        Guid ticketId,
        TicketDbContext db,
        CancellationToken cancellationToken) =>
        await db.Tickets.AsNoTracking().AnyAsync(x => x.Id == ticketId, cancellationToken);

    private static async Task<Exception?> TryDeleteBlobAsync(
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

    private static IResult StorageProblem(string operation, RequestFailedException exception) =>
        Results.Problem(
            statusCode: exception.Status == 404
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status503ServiceUnavailable,
            type: $"https://supporthub.dev/problems/attachment-{operation}-failed",
            title: $"Attachment {operation} failed",
            detail: exception.Status == 404
                ? "The attachment content could not be found."
                : "The attachment storage service is unavailable.");

    private sealed class CountingReadStream(Stream inner, long maximumBytes) : Stream
    {
        public long BytesRead { get; private set; }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => inner.Read(buffer);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
            if (BytesRead > maximumBytes)
            {
                throw new AttachmentTooLargeException(
                    $"The file must be {maximumBytes / 1024 / 1024} MiB or smaller.");
            }

            return read;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class AttachmentTooLargeException(string message) : Exception(message);
}
