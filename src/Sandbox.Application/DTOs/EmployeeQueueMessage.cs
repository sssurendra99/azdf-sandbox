namespace Sandbox.Application.DTOs;

public record EmployeeQueueMessage(
    string ClientId,
    string EmployeeId,
    string EmployeeName,
    int EmployeeAge,
    string Email,
    List<CertificateDto> Certificates
);
