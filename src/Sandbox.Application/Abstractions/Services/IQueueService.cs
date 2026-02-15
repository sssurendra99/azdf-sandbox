namespace Sandbox.Application.Abstractions.Services;

public interface IQueueService
{
    Task SendMessageAsync<T>(string queueName, T message) where T : class;
}
