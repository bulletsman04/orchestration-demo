using ClassRegistrationWorker.Services;
using MassTransit;

namespace ClassRegistrationWorker.StateMachines.RequestConsumers;

public class ValidateRegistrationConsumer(
    IClassesService classesService,
    IStudentService studentService,
    IEmailService emailService, // ToDo: email is sent in separate consumer as triggered by state machine publishing message
    ILogger<ValidateRegistrationConsumer> logger)
    : IConsumer<ValidateRegistration>
{
    public async Task Consume(ConsumeContext<ValidateRegistration> context)
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
            
            throw new Exception("Class registration failed. No such class.");
        }

        var isStudentActive = await studentService.IsStudentActive(context.Message.StudentId);
        
        if (!isStudentActive)
        {
            await emailService.SendEmail(
                "student@email.com",
                "Class registration failed",
                $"Student {context.Message.StudentId} is not active");

            logger.LogInformation("Student {StudentId} is not active", context.Message.StudentId);
            throw new Exception("Class registration failed. No such student.");
        }

        await context.RespondAsync(new RegistrationValidated(context.Message.RegistrationId));
    }
}