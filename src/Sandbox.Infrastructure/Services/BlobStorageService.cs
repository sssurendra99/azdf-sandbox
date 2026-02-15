using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Sandbox.Application.Abstractions.Services;
using Sandbox.Application.DTOs;

namespace Sandbox.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<BlobUploadResult> UploadPdfAsync(byte[] content, string fileName, string containerName = "employee-pdfs")
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(fileName);

        using var stream = new MemoryStream(content);
        await blobClient.UploadAsync(stream, new BlobHttpHeaders
        {
            ContentType = "application/pdf"
        });

        return new BlobUploadResult(
            BlobUrl: blobClient.Uri.ToString(),
            BlobName: fileName,
            ContainerName: containerName
        );
    }

    public async Task<byte[]?> DownloadAsync(string blobName, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
            return null;

        var response = await blobClient.DownloadAsync();
        using var memoryStream = new MemoryStream();
        await response.Value.Content.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    public async Task<bool> DeleteAsync(string blobName, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DeleteIfExistsAsync();
        return response.Value;
    }
}
