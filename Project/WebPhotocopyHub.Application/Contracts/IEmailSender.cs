namespace WebPhotocopyHub.Application.Contracts;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}
