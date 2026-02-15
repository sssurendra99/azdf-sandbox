using Sandbox.Application.DTOs;

namespace Sandbox.Application.Abstractions.Services;

public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadPdfAsync(byte[] content, string fileName, string containerName = "employee-pdfs");
    Task<byte[]?> DownloadAsync(string blobName, string containerName);
    Task<bool> DeleteAsync(string blobName, string containerName);
}
