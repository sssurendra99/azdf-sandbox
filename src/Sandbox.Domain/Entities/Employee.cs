using Sandbox.Domain.Common;
using Sandbox.Domain.Exceptions;
using Sandbox.Domain.ValueObjects;

namespace Sandbox.Domain.Entities;

public class Employee: BaseEntity
{
    public string EmployeeId { get; private set; }
    public string EmployeeName { get; private set; }
    public int EmployeeAge { get; private set; }
    public Email? Email { get; private set; }
    public string ClientId { get; private set; }

    private Employee()
    {
        EmployeeId = string.Empty;
        ClientId = string.Empty;
        EmployeeName = string.Empty;
    }

     public static Employee Create(
            string employeeId,
            string clientId,
            string employeeName,
            int employeeAge,
            string? email = null)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
                throw new DomainException("Employee ID cannot be empty");

            if (string.IsNullOrWhiteSpace(clientId))
                throw new DomainException("Client ID cannot be empty");

            if (string.IsNullOrWhiteSpace(employeeName))
                throw new DomainException("Employee name cannot be empty");

            if (employeeAge < 0)
                throw new DomainException("Employee age cannot be negative");

            var employee = new Employee
            {
                EmployeeId = employeeId,
                ClientId = clientId,
                EmployeeName = employeeName,
                EmployeeAge = employeeAge,
            };

            if (!string.IsNullOrWhiteSpace(email))
            {
                employee.Email = Email.Create(email);
            }

            return employee;
        }
}