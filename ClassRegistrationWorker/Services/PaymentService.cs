using MassTransit;

namespace ClassRegistrationWorker.Services;

public class PaymentService(ILogger<PaymentService> logger) : IPaymentService
{
    public Task<PaymentRegistrationResult> RegisterPayment(Guid registrationId)
    {
        // in real app: call to payment service
        
        logger.LogInformation("Payment for registration {RegistrationId} registered", registrationId);
        
        return Task.FromResult(new PaymentRegistrationResult(NewId.NextGuid(), new Uri("https://payment.com/payment/123"), true));
    }

    public Task<PaymentStatus> GetPayment(Guid paymentId)
    {
        // in real app: call to payment service
        
        return Task.FromResult(new PaymentStatus { PaymentId = paymentId, IsCompleted = true });
    }
}

public interface IPaymentService
{
    Task<PaymentRegistrationResult> RegisterPayment(Guid registrationId);
    Task<PaymentStatus> GetPayment(Guid paymentId);
}

public record PaymentStatus
{
    public Guid PaymentId { get; set; }
    public bool IsCompleted { get; init; }
}

public record PaymentRegistrationResult(Guid PaymentId, Uri PaymentLink, bool Success);