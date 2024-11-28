using ClassRegistrationWorker.Services;
using MassTransit;

namespace ClassRegistrationWorker.StateMachines.RequestConsumers;

public class ConfirmSpotConsumer(
    IAvailabilityService availabilityService,
    ILogger<ConfirmSpotConsumer> logger)
    : IConsumer<ConfirmSpot>
{
    public async Task Consume(ConsumeContext<ConfirmSpot> context)
    {
        var isSpotConfirmed = await availabilityService.ConfirmSpotReservation(context.Message.ReservationId);

        if (!isSpotConfirmed)
        {
            logger.LogError("Previously reserved spot with ReservationId {ReservationId} could not be confirmed", context.Message.ReservationId);
            throw new Exception($"Error confirming reserved spot with ReservationId {context.Message.ReservationId}");
        }
        await context.RespondAsync(new SpotConfirmed(context.Message.RegistrationId));
    }
}