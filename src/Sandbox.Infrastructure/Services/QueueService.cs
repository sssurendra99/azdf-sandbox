using System.Text.Json;
using Azure.Storage.Queues;
using Sandbox.Application.Abstractions.Services;

namespace Sandbox.Infrastructure.Services;

public class QueueService : IQueueService
{
    private readonly QueueServiceClient _queueServiceClient;

    public QueueService(QueueServiceClient queueServiceClient)
    {
        _queueServiceClient = queueServiceClient;
    }

    public async Task SendMessageAsync<T>(string queueName, T message) where T : class
    {
        var queueClient = _queueServiceClient.GetQueueClient(queueName);
        await queueClient.CreateIfNotExistsAsync();

        var jsonMessage = JsonSerializer.Serialize(message);
        var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonMessage));

        await queueClient.SendMessageAsync(base64Message);
    }
}
