using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sandbox.Application.Abstractions.Services;
using Sandbox.Infrastructure;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Get connection strings from configuration
var sqlConnectionString = builder.Configuration["SqlConnectionString"]
    ?? "Server=localhost,1433;Database=EmployeeDb;User Id=sa;Password=mypassword;TrustServerCertificate=True;";

var storageConnectionString = builder.Configuration["AzureWebJobsStorage"]
    ?? "UseDevelopmentStorage=true";

// SMTP settings
var smtpSettings = new SmtpSettings
{
    Host = builder.Configuration["Smtp:Host"] ?? "smtp.gmail.com",
    Port = int.TryParse(builder.Configuration["Smtp:Port"], out var port) ? port : 587,
    Username = builder.Configuration["Smtp:Username"] ?? string.Empty,
    Password = builder.Configuration["Smtp:Password"] ?? string.Empty,
    FromEmail = builder.Configuration["Smtp:FromEmail"] ?? string.Empty,
    FromName = builder.Configuration["Smtp:FromName"] ?? "Employee System",
    UseSsl = !bool.TryParse(builder.Configuration["Smtp:UseSsl"], out var useSsl) || useSsl
};

// Register infrastructure services
builder.Services.AddInfrastructure(sqlConnectionString, storageConnectionString, smtpSettings);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
