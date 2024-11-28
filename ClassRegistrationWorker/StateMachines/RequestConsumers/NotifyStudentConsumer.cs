using ClassRegistrationWorker.Services;
using MassTransit;

namespace ClassRegistrationWorker.StateMachines.RequestConsumers;

public class NotifyStudentConsumer(
    IEmailService emailService,
    ILogger<NotifyStudentConsumer> logger)
    : IConsumer<NotifyStudent>
{
    public async Task Consume(ConsumeContext<NotifyStudent> context)
    {
        await emailService.SendEmail(
            "student@email.com", // ToDo: we should take it from student
            context.Message.Subject,
            context.Message.Body);
    }
}