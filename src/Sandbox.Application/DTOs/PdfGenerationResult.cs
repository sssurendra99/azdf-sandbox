namespace Sandbox.Application.DTOs;

public record PdfGenerationResult(
    byte[] PdfContent,
    string FileName
);
