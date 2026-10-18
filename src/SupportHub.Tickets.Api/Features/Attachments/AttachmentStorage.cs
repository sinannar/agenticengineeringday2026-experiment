using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SupportHub.Tickets.Api.Features.Attachments;

public interface IAttachmentBlobStore
{
    Task EnsureContainerAsync(CancellationToken cancellationToken);
    Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken);
    Task<AttachmentBlobDownload?> OpenReadAsync(
        string blobName,
        string contentType,
        string fileName,
        CancellationToken cancellationToken);
    Task<bool> DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken);
}

public sealed class AzureAttachmentBlobStore(BlobServiceClient blobServiceClient) : IAttachmentBlobStore
{
    public async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        var container = blobServiceClient.GetBlobContainerClient(AttachmentPolicy.ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var container = blobServiceClient.GetBlobContainerClient(AttachmentPolicy.ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            cancellationToken);
    }

    public async Task<AttachmentBlobDownload?> OpenReadAsync(
        string blobName,
        string contentType,
        string fileName,
        CancellationToken cancellationToken)
    {
        var container = blobServiceClient.GetBlobContainerClient(AttachmentPolicy.ContainerName);
        var blob = container.GetBlobClient(blobName);
        try
        {
            var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return new AttachmentBlobDownload(response.Value.Content, contentType, fileName);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken)
    {
        var container = blobServiceClient.GetBlobContainerClient(AttachmentPolicy.ContainerName);
        var blob = container.GetBlobClient(blobName);
        var response = await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        return response.Value;
    }
}

public sealed class AttachmentStorageException(string operation, Exception innerException)
    : Exception($"Attachment blob storage operation '{operation}' failed.", innerException)
{
    public string Operation { get; } = operation;
}
