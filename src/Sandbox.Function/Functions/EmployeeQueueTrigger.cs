using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Sandbox.Application.Abstractions.Persistence;
using Sandbox.Application.DTOs;
using Sandbox.Domain.Entities;
using Sandbox.Infrastructure.Persistence;

namespace Sandbox.Function.Functions;

public class EmployeeQueueTrigger
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly ILogger<EmployeeQueueTrigger> _logger;

    public EmployeeQueueTrigger(
        IUnitOfWorkFactory unitOfWorkFactory,
        ILogger<EmployeeQueueTrigger> logger)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _logger = logger;
    }

    [Function(nameof(ProcessEmployeeQueue))]
    public async Task ProcessEmployeeQueue(
        [QueueTrigger("employee-updates", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation("Processing employee queue message");

        try
        {
            var message = JsonSerializer.Deserialize<EmployeeQueueMessage>(queueMessage, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message == null)
            {
                _logger.LogError("Failed to deserialize queue message");
                return;
            }

            _logger.LogInformation("Processing employee: {EmployeeId}, Name: {EmployeeName}",
                message.EmployeeId, message.EmployeeName);

            using var unitOfWork = _unitOfWorkFactory.Create();

            var employeeRepo = new GenericRepository<Employee>(unitOfWork);
            var certificateRepo = new GenericRepository<Certificate>(unitOfWork);

            // Check if employee exists
            var existingEmployee = await employeeRepo.FirstOrDefaultAsync(
                "EmployeeId = @EmployeeId OR ClientId = @ClientId",
                new { message.EmployeeId, message.ClientId });

            if (existingEmployee != null)
            {
                _logger.LogInformation("Employee {EmployeeId} already exists, updating...", message.EmployeeId);

                await employeeRepo.ExecuteAsync(
                    @"UPDATE Employees
                      SET EmployeeName = @EmployeeName,
                          EmployeeAge = @EmployeeAge,
                          Email = @Email,
                          UpdatedAt = @UpdatedAt
                      WHERE Id = @Id",
                    new
                    {
                        existingEmployee.Id,
                        message.EmployeeName,
                        message.EmployeeAge,
                        Email = message.Email,
                        UpdatedAt = DateTime.UtcNow
                    });

                await certificateRepo.ExecuteAsync(
                    "DELETE FROM Certificates WHERE EmployeeId = @EmployeeId",
                    new { EmployeeId = message.EmployeeId });
            }
            else
            {
                _logger.LogInformation("Creating new employee: {EmployeeId}", message.EmployeeId);

                var employee = Employee.Create(
                    employeeId: message.EmployeeId,
                    clientId: message.ClientId,
                    employeeName: message.EmployeeName,
                    employeeAge: message.EmployeeAge,
                    email: message.Email);

                await employeeRepo.InsertAsync(employee);
            }

            // Insert certificates
            if (message.Certificates.Count > 0)
            {
                var certificates = message.Certificates
                    .Select(c => Certificate.Create(
                        certificateId: c.CertificateId,
                        certificateName: c.CertificateName,
                        employeeId: message.EmployeeId))
                    .ToList();

                await certificateRepo.InsertRangeAsync(certificates);

                _logger.LogInformation("Inserted {CertCount} certificates for employee {EmployeeId}",
                    certificates.Count, message.EmployeeId);
            }

            await unitOfWork.CompleteAsync();

            _logger.LogInformation("Successfully processed employee {EmployeeId} with {CertCount} certificates",
                message.EmployeeId, message.Certificates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing employee queue message: {Message}", queueMessage);
            throw; // Rethrow to trigger retry/dead-letter
        }
    }
}
