namespace Sandbox.Application.DTOs;

public record BlobUploadResult(
    string BlobUrl,
    string BlobName,
    string ContainerName
);
