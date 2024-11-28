using ClassRegistrationWorker.Services;
using MassTransit;

namespace ClassRegistrationWorker.StateMachines.RequestConsumers;

public class RegisterPaymentConsumer(
    IPaymentService paymentService,
    IEmailService emailService, // ToDo: email is sent in separate consumer as triggered by state machine publishing message
    ILogger<BookSpotConsumer> logger)
    : IConsumer<RegisterPayment>
{
    public async Task Consume(ConsumeContext<RegisterPayment> context)
    {
        var paymentResult = await paymentService.RegisterPayment(context.Message.RegistrationId);

        if (paymentResult.Success is false)
        {
            logger.LogInformation("Payment for registration {RegistrationId} failed", context.Message.RegistrationId);
            return;
        }

        await context.RespondAsync(new PaymentRegistered(context.Message.RegistrationId, paymentResult.PaymentId, paymentResult.PaymentLink));
    }
}