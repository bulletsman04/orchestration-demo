using ClassRegistrationWorker.Services;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClassRegistrationWorker.Consumers;

public class PaymentStatusUpdateConsumer(
    IAvailabilityService availabilityService,
    IEmailService emailService,
    ClassesDbContext dbContext,
    ILogger<PaymentStatusUpdateConsumer> logger) : IConsumer<PaymentStatusUpdate>
{
    public async Task Consume(ConsumeContext<PaymentStatusUpdate> context)
    {
        var classRegistration =
            await dbContext.ClassRegistrations.FirstOrDefaultAsync(x => x.RegistrationId == context.Message.RegistationId, context.CancellationToken);

        if (classRegistration is null)
        {
            logger.LogInformation("Class registration {PaymentId} not found", context.Message.RegistationId);
            return;
        }

        var classId = classRegistration.ClassId;
        var studentId = classRegistration.StudentId;

        logger.LogInformation("Processing payment status change of registration for class {ClassId,-20} by student {OrderId}", classId, studentId);

        if (context.Message.IsSuccessful is false)
        {
            await emailService.SendEmail(
                "student@email.com",
                "Class registration failed",
                $"Payment failed for class {classId}");

            return;
        }

        bool isSpotConfirmed = await availabilityService.ConfirmSpotReservation(classRegistration.ReservationId);

        if (isSpotConfirmed is false)
        {
            logger.LogError("Previously reserved spot for class {ClassId} by student {StudentId} could not be confirmed", classId, studentId);
            return;
        }

        await emailService.SendEmail(
            "student@email.com",
            "Class registration successful",
            $"Payment successful for class {classId}");

        classRegistration.IsCompleted = true;
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}

// ToDo: Extract to Contracts project