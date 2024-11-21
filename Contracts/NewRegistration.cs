namespace Contracts;

public record NewRegistration
{
    public Guid RegistrationId { get; init; }
    public Guid ClassId { get; init; }
    public Guid StudentId { get; init; }
}