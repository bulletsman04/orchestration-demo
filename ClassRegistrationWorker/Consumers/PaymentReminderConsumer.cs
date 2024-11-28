using ClassRegistrationWorker.Services;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClassRegistrationWorker.Consumers;

public class PaymentReminderConsumer(
    IPaymentService paymentService,
    IEmailService emailService,
    ClassesDbContext dbContext,
    ILogger<PaymentReminderConsumer> logger) : IConsumer<PaymentReminder>
{
    public async Task Consume(ConsumeContext<PaymentReminder> context)
    {
        logger.LogInformation("Processing payment reminder for registration {RegistrationId}", context.Message.RegistrationId);

        var classRegistration = await dbContext.ClassRegistrations.FirstOrDefaultAsync(x => x.RegistrationId == context.Message.RegistrationId, context.CancellationToken);

        if (classRegistration is null || classRegistration.IsCompleted)
        {
            return;
        }
        
        var paymentStatus = await paymentService.GetPayment(classRegistration.PaymentId);

        if (paymentStatus.IsCompleted)
        {
            return;
        }

        await emailService.SendEmail(
            "student@email.com",
            "Payment reminder",
            $"Payment for class {classRegistration.ClassId} is due");

        // Schedule next message that would terminate process if not paid
    }
}