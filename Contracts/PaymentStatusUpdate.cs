namespace Contracts;

public record PaymentStatusUpdate
{
    public Guid RegistationId { get; init; }
    public bool IsSuccessful { get; init; }
}