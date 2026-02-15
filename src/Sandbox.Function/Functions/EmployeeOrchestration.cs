using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Sandbox.Application.DTOs;
using System.Net;
using System.Text.Json;

namespace Sandbox.Function.Functions;

public class EmployeeOrchestration
{
    private readonly ILogger<EmployeeOrchestration> _logger;

    public EmployeeOrchestration(ILogger<EmployeeOrchestration> logger)
    {
        _logger = logger;
    }

    [Function("EmployeeHttpTrigger")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "employee/process")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Employee processing HTTP trigger received request");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body is empty");
                return badResponse;
            }

            var employeeRequest = JsonSerializer.Deserialize<EmployeeRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (employeeRequest == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request format");
                return badResponse;
            }

            // Start the orchestration
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(EmployeeProcessingOrchestrator),
                employeeRequest);

            _logger.LogInformation("Started orchestration with ID = '{instanceId}'", instanceId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                instanceId,
                statusQueryUrl = $"/runtime/webhooks/durabletask/instances/{instanceId}",
                message = "Employee processing started"
            });

            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse request body");
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync($"Invalid JSON: {ex.Message}");
            return errorResponse;
        }
    }

    [Function(nameof(EmployeeProcessingOrchestrator))]
    public async Task<OrchestrationResult> EmployeeProcessingOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger<EmployeeOrchestration>();
        var employeeRequest = context.GetInput<EmployeeRequest>()!;

        logger.LogInformation("Starting employee processing orchestration for EmployeeId: {EmployeeId}",
            employeeRequest.EmployeeId);

        var result = new OrchestrationResult
        {
            EmployeeId = employeeRequest.EmployeeId,
            StartedAt = context.CurrentUtcDateTime
        };

        try
        {
            // Activity 1: Send to Queue for DB update
            await context.CallActivityAsync(
                nameof(EmployeeActivities.SendToQueueActivity),
                employeeRequest);
            result.QueueMessageSent = true;
            logger.LogInformation("Queue message sent for EmployeeId: {EmployeeId}", employeeRequest.EmployeeId);

            // Activity 2: Generate PDF
            var pdfResult = await context.CallActivityAsync<PdfGenerationResult>(
                nameof(EmployeeActivities.GeneratePdfActivity),
                employeeRequest);
            result.PdfGenerated = true;
            result.PdfFileName = pdfResult.FileName;
            logger.LogInformation("PDF generated: {FileName}", pdfResult.FileName);

            // Activity 3: Store PDF in Blob Storage
            var blobResult = await context.CallActivityAsync<BlobUploadResult>(
                nameof(EmployeeActivities.StorePdfInBlobActivity),
                pdfResult);
            result.PdfStoredInBlob = true;
            result.BlobUrl = blobResult.BlobUrl;
            logger.LogInformation("PDF stored in blob: {BlobUrl}", blobResult.BlobUrl);

            // Activity 4: Send Email notification
            var emailSent = await context.CallActivityAsync<bool>(
                nameof(EmployeeActivities.SendEmailActivity),
                employeeRequest);
            result.EmailSent = emailSent;
            logger.LogInformation("Email sent: {EmailSent}", emailSent);

            result.CompletedAt = context.CurrentUtcDateTime;
            result.Success = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Orchestration failed for EmployeeId: {EmployeeId}", employeeRequest.EmployeeId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.CompletedAt = context.CurrentUtcDateTime;
        }

        return result;
    }
}

public class OrchestrationResult
{
    public string EmployeeId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool QueueMessageSent { get; set; }
    public bool PdfGenerated { get; set; }
    public string? PdfFileName { get; set; }
    public bool PdfStoredInBlob { get; set; }
    public string? BlobUrl { get; set; }
    public bool EmailSent { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}
