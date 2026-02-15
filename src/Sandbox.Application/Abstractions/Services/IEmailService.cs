using Sandbox.Domain.ValueObjects;

namespace Sandbox.Application.Abstractions.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(Email to, string subject, string body);
}
