using Sandbox.Domain.Common;
using Sandbox.Domain.Exceptions;

namespace Sandbox.Domain.Entities;

public class Certificate: BaseEntity
{
    public string CertificateId { get; private set; }
    public string CertificateName { get; private set; }

    public string EmployeeId { get; private set; }

    private Certificate()
    {
        CertificateId = string.Empty;
        CertificateName = string.Empty;
        EmployeeId = string.Empty;
    }

    public static Certificate Create(
            string certificateId,
            string certificateName,
            string employeeId)
        {

            if (string.IsNullOrWhiteSpace(certificateName))
                throw new DomainException("Certificate name cannot be empty");

            return new Certificate
            {
                CertificateId = certificateId,
                CertificateName = certificateName,
                EmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow
            };
        }
}