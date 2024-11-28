using ClassRegistrationWorker.Services;
using Contracts;
using MassTransit;

namespace ClassRegistrationWorker.Consumers;

public class NewRegistrationConsumer(
    IClassesService classesService,
    IStudentService studentService,
    IAvailabilityService availabilityService,
    IEmailService emailService,
    IPaymentService paymentService,
    ClassesDbContext dbContext,
    ILogger<NewRegistrationConsumer> logger) : IConsumer<NewRegistration>
{
    public async Task Consume(ConsumeContext<NewRegistration> context)
    {
        logger.LogInformation("Processing registration for class {ClassId,-20} by student {OrderId}", context.Message.ClassId, context.Message.StudentId);

        var isClassActive = await classesService.IsClassActiveForRegistration(context.Message.ClassId);

        if (!isClassActive)
        {
            await emailService.SendEmail(
                "student@email.com",
                "Class registration failed",
                $"Class {context.Message.ClassId} is not active for registration");
            
            logger.LogInformation("Class {ClassId} is not active for registration", context.Message.ClassId);
        }
        
        var isStudentActive = await studentService.IsStudentActive(context.Message.StudentId);
        if (!isStudentActive)
        {
            await emailService.SendEmail(
                "student@email.com",
                "Class registration failed",
                $"Student {context.Message.StudentId} is not active");

            logger.LogInformation("Student {StudentId} is not active", context.Message.StudentId);
        }
        
        var spotReservationResult = await availabilityService.ReserveSpot(context.Message.ClassId, context.Message.StudentId);

        if (!spotReservationResult.IsSuccessful)
        {
            await emailService.SendEmail(
                "student@email.com",
                "Class registration failed",
                $"Class {context.Message.ClassId} is full");
            logger.LogInformation("Class {ClassId} is full", context.Message.ClassId);
        }
        
        var paymentResult = await paymentService.RegisterPayment(context.Message.RegistrationId);

        if (paymentResult.Success is false)
        {
            logger.LogInformation("Payment for registration {RegistrationId} failed", context.Message.RegistrationId);
            return;
        }

        var classRegistration = new ClassRegistration
        {
            RegistrationId = context.Message.RegistrationId,
            ClassId = context.Message.ClassId,
            StudentId = context.Message.StudentId,
            PaymentId = paymentResult.PaymentId,
            ReservationId = spotReservationResult.ReservationId,
            IsCompleted = false
        };
        
        dbContext.ClassRegistrations.Add(classRegistration);
        
        await dbContext.SaveChangesAsync(context.CancellationToken);
        
        // sent email to student with payment link
        await emailService.SendEmail(
            "student@email.com",
            "Registration for class.",
            $"We received you registration. Please use this link to complete your registration: {paymentResult.PaymentLink}");
        
        logger.LogInformation("Registration {RegistrationId} processed until payment", context.Message.RegistrationId);

        // ToDo: use nameof?
        await context.ScheduleSend(new Uri("queue:PaymentReminder"), TimeSpan.FromDays(1), new PaymentReminder
        {
            RegistrationId = classRegistration.RegistrationId
        }, context.CancellationToken);
    }
}