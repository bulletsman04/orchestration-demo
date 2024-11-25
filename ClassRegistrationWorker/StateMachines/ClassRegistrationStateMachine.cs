using Contracts;
using MassTransit;

namespace ClassRegistrationWorker.StateMachines;

public class ClassRegistrationStateMachine : MassTransitStateMachine<ClassRegistrationState>
{
    // Let's try first version without Routing Slip
    public ClassRegistrationStateMachine()
    {
        InstanceState(x => x.CurrentState);
        // Event(() => RegistrationFaulted, x => x.CorrelateById(context => context.Message.RegistrationId));
        Event(() => NewRegistration, x => x.CorrelateById(context => context.Message.RegistrationId));
        Event(() => PaymentStatusUpdate, x => x.CorrelateById(context => context.Message.PaymentId)); // ToDo: separate ID?
        // Event(() => RegistrationFinalized, x => x.CorrelateById(context => context.Message.RegistrationId)); // ToDo: separate ID?
        
        During(Initial, When(NewRegistration).Then(x => Console.WriteLine("Save registration date.")).TransitionTo(Submitted));
        WhenEnter(Submitted, x => x.Then(c => Console.WriteLine("Custom activity to validate data. If successful then")).TransitionTo(Validated));
        WhenEnter(Validated, x => x.Then(c => Console.WriteLine("Try booking spot. If successful then transition to")).TransitionTo(SpotTempReserved));
        WhenEnter(SpotTempReserved, x => x.Then(c => Console.WriteLine("Register payment and notify student")).TransitionTo(AwaitingPayment));
        During(AwaitingPayment, When(PaymentStatusUpdate).Then(c => Console.WriteLine("Payment confirmed. Book spot.")).TransitionTo(Paid)); // ToDo: Schedule notification
        WhenEnter(Paid, x => x.Then(c => Console.WriteLine("Spot booked. Notify student")).TransitionTo(Completed));
        
        // DuringAny(When(RegistrationFaulted).TransitionTo(Faulted)); // ToDo: DuringAny? when can it happen?
    }

    public State Submitted { get; private set; } = null!;
    public State Validated { get; private set; } = null!;
    public State SpotTempReserved { get; private set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;
    public State Paid { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;
    public State Completed { get; private set; } = null!; // ToDo: Or initial?

    public Event<NewRegistration> NewRegistration { get; private set; } = null!;
    public Event<PaymentStatusUpdate> PaymentStatusUpdate { get; private set; } = null!;
    // public Event<RegistrationFinalized> RegistrationFinalized { get; private set; } = null!;
    
    // public Event<RegistrationFaulted> RegistrationFaulted { get; private set; } = null!;


}

public class RegistrationFaulted
{
    public Guid RegistrationId { get; }
}

public class RegistrationFinalized
{
    public Guid RegistrationId { get; }
}

public class ClassRegistrationState :
    SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public required string CurrentState { get; set; }

    public required Guid StudentId { get; set; }
    public required Guid ClassId { get; set; }
    public required Guid PaymentId { get; set; }
    public required Guid ReservationId { get; set; }

    // If using Optimistic concurrency, this property is required
    public uint RowVersion { get; set; }
}