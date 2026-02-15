using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
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

        // Generate a SAS URL valid for 24 hours
        var sasUrl = GenerateSasUrl(blobClient, TimeSpan.FromHours(24));

        return new BlobUploadResult(
            BlobUrl: sasUrl,
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

    private string GenerateSasUrl(BlobClient blobClient, TimeSpan validFor)
    {
        if (!blobClient.CanGenerateSasUri)
            return blobClient.Uri.ToString();

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(validFor)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }
}
