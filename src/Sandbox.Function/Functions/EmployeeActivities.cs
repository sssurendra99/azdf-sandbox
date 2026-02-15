using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Sandbox.Application.Abstractions.Services;
using Sandbox.Application.DTOs;
using Sandbox.Domain.ValueObjects;

namespace Sandbox.Function.Functions;

public class EmployeeActivities
{
    private readonly IQueueService _queueService;
    private readonly IPdfService _pdfService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmployeeActivities> _logger;

    private const string EmployeeQueueName = "employee-updates";

    public EmployeeActivities(
        IQueueService queueService,
        IPdfService pdfService,
        IBlobStorageService blobStorageService,
        IEmailService emailService,
        ILogger<EmployeeActivities> logger)
    {
        _queueService = queueService;
        _pdfService = pdfService;
        _blobStorageService = blobStorageService;
        _emailService = emailService;
        _logger = logger;
    }

    [Function(nameof(SendToQueueActivity))]
    public async Task SendToQueueActivity([ActivityTrigger] EmployeeRequest request)
    {
        _logger.LogInformation("Sending employee {EmployeeId} to queue for DB update", request.EmployeeId);

        var queueMessage = new EmployeeQueueMessage(
            ClientId: request.ClientId,
            EmployeeId: request.EmployeeId,
            EmployeeName: request.EmployeeName,
            EmployeeAge: request.EmployeeAge,
            Email: request.Email,
            Certificates: request.EmployeeCertificates
        );

        await _queueService.SendMessageAsync(EmployeeQueueName, queueMessage);

        _logger.LogInformation("Employee {EmployeeId} sent to queue successfully", request.EmployeeId);
    }

    [Function(nameof(GeneratePdfActivity))]
    public async Task<PdfGenerationResult> GeneratePdfActivity([ActivityTrigger] EmployeeRequest request)
    {
        _logger.LogInformation("Generating PDF for employee {EmployeeId}", request.EmployeeId);

        var result = await _pdfService.GenerateEmployeePdfAsync(request);

        _logger.LogInformation("PDF generated: {FileName}, Size: {Size} bytes",
            result.FileName, result.PdfContent.Length);

        return result;
    }

    [Function(nameof(StorePdfInBlobActivity))]
    public async Task<BlobUploadResult> StorePdfInBlobActivity([ActivityTrigger] PdfGenerationResult pdfResult)
    {
        _logger.LogInformation("Storing PDF {FileName} in blob storage", pdfResult.FileName);

        var result = await _blobStorageService.UploadPdfAsync(
            pdfResult.PdfContent,
            pdfResult.FileName);

        _logger.LogInformation("PDF stored at: {BlobUrl}", result.BlobUrl);

        return result;
    }

    [Function(nameof(SendEmailActivity))]
    public async Task<bool> SendEmailActivity([ActivityTrigger] EmployeeRequest request)
    {
        _logger.LogInformation("Sending email notification for employee {EmployeeId}", request.EmployeeId);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            _logger.LogWarning("No email address provided for employee {EmployeeId}", request.EmployeeId);
            return false;
        }

        var email = Email.Create(request.Email);
        var subject = $"Employee Certificate Generated - {request.EmployeeName}";
        var body = $"""
            Dear {request.EmployeeName},

            Your employee certificate has been successfully generated and stored.

            Details:
            - Employee ID: {request.EmployeeId}
            - Client ID: {request.ClientId}
            - Certificates: {request.EmployeeCertificates.Count}

            Thank you.
            """;

        var result = await _emailService.SendEmailAsync(email, subject, body);

        _logger.LogInformation("Email sent to {Email}: {Result}", request.Email, result);

        return result;
    }
}
