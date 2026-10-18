using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace SupportHub.Tickets.Api.Features.Attachments;

public sealed record AttachmentResponse(
    Guid Id,
    Guid TicketId,
    string OriginalFileName,
    string ContentType,
    long Size,
    DateTimeOffset CreatedAt);

public sealed record AttachmentBlobDownload(Stream Content, string ContentType, string FileName);

public static class AttachmentPolicy
{
    public const long MaximumFileSize = 10 * 1024 * 1024;
    public const long MaximumRequestSize = 11 * 1024 * 1024;
    public const string ContainerName = "ticket-attachments";

    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg"
        };

    public static bool TryValidate(
        string? fileName,
        string? contentType,
        long size,
        out string safeFileName,
        out string error)
    {
        safeFileName = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            error = "A file name is required.";
            return false;
        }

        var trimmedFileName = fileName.Trim();
        if (trimmedFileName.Any(char.IsControl) ||
            trimmedFileName.Contains('/') ||
            trimmedFileName.Contains('\\') ||
            trimmedFileName.Contains('\0'))
        {
            error = "The file name is not safe.";
            return false;
        }
        safeFileName = Path.GetFileName(trimmedFileName);

        if (size <= 0)
        {
            error = "The file must not be empty.";
            return false;
        }

        if (size > MaximumFileSize)
        {
            error = $"The file must be {MaximumFileSize / 1024 / 1024} MiB or smaller.";
            return false;
        }

        var extension = Path.GetExtension(safeFileName);
        if (!AllowedTypes.TryGetValue(extension, out var expectedType) ||
            !string.Equals(contentType, expectedType, StringComparison.OrdinalIgnoreCase))
        {
            error = "The file type is not permitted.";
            return false;
        }

        return true;
    }

    public static bool TryGetMultipartBoundary(string? contentType, out string boundary)
    {
        boundary = string.Empty;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var parsed) ||
            !string.Equals(parsed.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var boundaryValue = parsed.Parameters
            .FirstOrDefault(parameter => string.Equals(parameter.Name.Value, "boundary", StringComparison.OrdinalIgnoreCase))
            ?.Value;
        if (boundaryValue is null)
        {
            return false;
        }

        boundary = HeaderUtilities.RemoveQuotes(boundaryValue.Value).Value ?? string.Empty;
        return boundary.Length > 0;
    }

    public static string BlobName(Guid ticketId, Guid attachmentId) =>
        $"tickets/{ticketId:D}/{attachmentId:D}";

    public static AttachmentResponse ToResponse(Attachment attachment) =>
        new(
            attachment.Id,
            attachment.TicketId,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.Size,
            attachment.CreatedAt);

    public static IResult TooLarge(string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status413PayloadTooLarge,
            type: "https://supporthub.dev/problems/attachment-too-large",
            title: "Attachment is too large",
            detail: detail);
}
