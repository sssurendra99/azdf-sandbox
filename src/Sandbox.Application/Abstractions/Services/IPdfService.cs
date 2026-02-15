using Sandbox.Application.DTOs;

namespace Sandbox.Application.Abstractions.Services;

public interface IPdfService
{
    Task<PdfGenerationResult> GenerateEmployeePdfAsync(EmployeeRequest employee);
}
