namespace Sandbox.Application.DTOs;

public record EmployeeRequest(
    string ClientId,
    string EmployeeId,
    string EmployeeName,
    int EmployeeAge,
    string Email,
    List<CertificateDto> EmployeeCertificates
);
