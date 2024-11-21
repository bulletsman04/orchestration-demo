using MassTransit;

namespace ClassRegistrationWorker.Services;

public class AvailabilityService(ILogger<AvailabilityService> logger) : IAvailabilityService
{
    public Task<ReservationResult> ReserveSpot(Guid classId, Guid studentId)
    {
        // in real app: call to availability service

        logger.LogInformation("Reserving spot for class {ClassId} for student {StudentId}", classId, studentId);

        return Task.FromResult(new ReservationResult(NewId.NextGuid(), true));
    }

    public Task<bool> ConfirmSpotReservation(Guid reservationId)
    {
        // in real app: call to availability service

        logger.LogInformation("Confirming spot reservation: {ReservationId}", reservationId);

        return Task.FromResult(true);
    }
}

public interface IAvailabilityService
{
    Task<ReservationResult> ReserveSpot(Guid classId, Guid studentId);
    Task<bool> ConfirmSpotReservation(Guid reservationId);
}

public record ReservationResult(Guid ReservationId, bool IsSuccessful);