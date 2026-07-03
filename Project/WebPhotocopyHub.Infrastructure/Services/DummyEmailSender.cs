using Microsoft.Extensions.Logging;
using WebPhotocopyHub.Application.Contracts;

namespace WebPhotocopyHub.Infrastructure.Services;

public class DummyEmailSender : IEmailSender
{
    private readonly ILogger<DummyEmailSender> _logger;

    public DummyEmailSender(ILogger<DummyEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("--- DUMMY EMAIL SENDER ---");
        _logger.LogInformation("To: {Email}", email);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Message: {HtmlMessage}", htmlMessage);
        _logger.LogInformation("--------------------------");
        return Task.CompletedTask;
    }
}
