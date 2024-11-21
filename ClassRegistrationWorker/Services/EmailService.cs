namespace ClassRegistrationWorker.Services;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendEmail(string email, string subject, string body)
    {
        logger.LogInformation("Sending email to {Email} with subject {Subject}", email, subject);
        return Task.CompletedTask;
    }
}

public interface IEmailService
{
    Task SendEmail(string email, string subject, string body);
}