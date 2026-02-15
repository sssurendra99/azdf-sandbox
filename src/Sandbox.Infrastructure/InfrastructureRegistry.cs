using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Sandbox.Application.Abstractions.Persistence;
using Sandbox.Application.Abstractions.Services;
using Sandbox.Domain.ValueObjects;
using Sandbox.Infrastructure.Persistence;
using Sandbox.Infrastructure.Persistence.TypeHandlers;
using Sandbox.Infrastructure.Services;

namespace Sandbox.Infrastructure;

public static class InfrastructureRegistry
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string sqlConnectionString, string storageConnectionString, SmtpSettings smtpSettings)
    {
        // Register Dapper type handlers
        SqlMapper.AddTypeHandler(new EmailTypeHandler());

        // Persistence
        services.AddSingleton<IUnitOfWorkFactory>(sp => new UnitOfWorkFactory(sqlConnectionString));
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Azure Storage
        services.AddSingleton(_ => new BlobServiceClient(storageConnectionString));
        services.AddSingleton(_ => new QueueServiceClient(storageConnectionString));

        // Services
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddSingleton(smtpSettings);
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
