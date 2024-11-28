using ClassRegistrationWorker.Services;
using MassTransit;

namespace ClassRegistrationWorker.StateMachines.RequestConsumers;

public class BookSpotConsumer(
    IAvailabilityService availabilityService,
    ILogger<BookSpotConsumer> logger)
    : IConsumer<BookSpot>
{
    public async Task Consume(ConsumeContext<BookSpot> context)
    {
        var spotReservationResult = await availabilityService.ReserveSpot(context.Message.ClassId, context.Message.StudentId);

        if (!spotReservationResult.IsSuccessful)
        {
            throw new Exception($"Class {context.Message.ClassId} is full");
        }
        await context.RespondAsync(new SpotTempBooked(context.Message.RegistrationId, spotReservationResult.ReservationId));
    }
}