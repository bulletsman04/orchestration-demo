namespace Contracts;

public record PaymentStatusUpdate
{
    public Guid PaymentId { get; init; }
    public bool IsSuccessful { get; init; }
}