using MailKit.Net.Smtp;
using MimeKit;
using Sandbox.Application.Abstractions.Services;
using Sandbox.Domain.ValueObjects;

namespace Sandbox.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;

    public EmailService(SmtpSettings settings)
    {
        _settings = settings;
    }

    public async Task<bool> SendEmailAsync(Email to, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(string.Empty, (string)to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Failed to send email to {to}: {ex.Message}");
            return false;
        }
    }
}
