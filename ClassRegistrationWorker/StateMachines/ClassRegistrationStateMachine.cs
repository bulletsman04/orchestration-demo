using Contracts;
using MassTransit;

namespace ClassRegistrationWorker.StateMachines;

// ToDo: NEXT STEP: schedule payment notification => ToDos => Refactor

// ToDo: To graph - visualize
public class ClassRegistrationStateMachine : MassTransitStateMachine<ClassRegistrationState>
{
    private Request<ClassRegistrationState, ValidateRegistration, RegistrationValidated> ValidateRegistration { get; set; } = null!;
    private Request<ClassRegistrationState, BookSpot, SpotTempBooked> BookSpot { get; set; } = null!;
    private Request<ClassRegistrationState, RegisterPayment, PaymentRegistered> RegisterPayment { get; set; } = null!;
    private Request<ClassRegistrationState, ConfirmSpot, SpotConfirmed> ConfirmSpot { get; set; } = null!;

    public ClassRegistrationStateMachine()
    {
        InstanceState(x => x.CurrentState);
        Event(() => NewRegistration, x => x.CorrelateById(context => context.Message.RegistrationId));
        Event(() => PaymentStatusUpdate, x => x.CorrelateById(context => context.Message.RegistationId));
        Request(() => ValidateRegistration, o => o.Timeout = TimeSpan.Zero);
        Request(() => BookSpot, o => o.Timeout = TimeSpan.Zero);
        Request(() => RegisterPayment, o => o.Timeout = TimeSpan.Zero);
        Request(() => ConfirmSpot, o => o.Timeout = TimeSpan.Zero);

        During(Initial, When(NewRegistration).SaveRegistrationData().TransitionTo(Submitted));
        WhenEnter(Submitted, x => x
            .Request(ValidateRegistration, context => new ValidateRegistration(context.Saga.CorrelationId, context.Saga.ClassId, context.Saga.StudentId))
            .TransitionTo(ValidateRegistration.Pending));

        During(ValidateRegistration.Pending,
            When(ValidateRegistration.Completed)
                .TransitionTo(Validated),
            When(ValidateRegistration.Faulted)
                .NotifyThatValidationFailed()
                .TransitionTo(Faulted)
        );

        WhenEnter(Validated,
            x => x
                .Request(BookSpot, context => new BookSpot(context.Saga.CorrelationId, context.Saga.ClassId, context.Saga.StudentId))
                .TransitionTo(BookSpot.Pending));

        During(BookSpot.Pending,
            When(BookSpot.Completed)
                .Then(context => context.Saga.ReservationId = context.Message.ReservationId)
                .TransitionTo(SpotTempReserved),
            When(BookSpot.Faulted)
                .NotifyThatClassIsFullyBooked()
                .TransitionTo(Faulted)
        );

        WhenEnter(SpotTempReserved,
            x => x
                .Request(RegisterPayment, context => new RegisterPayment(context.Saga.CorrelationId))
                .TransitionTo(RegisterPayment.Pending)); // Publishing command to PaymentService?

        During(RegisterPayment.Pending,
            When(RegisterPayment.Completed)
                .SavePaymentData()
                .NotifyThatRegistrationSucceededAndShouldBePaid()
                .TransitionTo(AwaitingPayment), // ToDo: Schedule notification
            When(RegisterPayment.Faulted) // Unlikely to happen
                .TransitionTo(Faulted)
        );


        During(AwaitingPayment,
            When(PaymentStatusUpdate)
                // ToDo: Extract extension
                .IfElse(context => context.Message.IsSuccessful,
                    then => then
                        .Request(ConfirmSpot, context => new ConfirmSpot(context.Saga.CorrelationId, context.Saga.ReservationId))
                        .TransitionTo(ConfirmSpot.Pending),
                    @else => @else.NotifyThatPaymentFailed())
        );

        During(ConfirmSpot.Pending,
            When(ConfirmSpot.Completed)
                .NotifyThatRegistrationIsCompleted()
                .Then(c => c.GetServiceOrCreateInstance<ILogger<ClassRegistrationStateMachine>>().LogInformation("LOG: Student registration for class succeeded."))
                .TransitionTo(Completed),
            When(ConfirmSpot.Faulted)
                // We took money but couldn't complete registration. Let student decide (refund, another class, ...)
                .TransitionTo(Faulted)
        );

        WhenEnter(Faulted,
            binder => binder.Then(
                c => c.GetServiceOrCreateInstance<ILogger<ClassRegistrationStateMachine>>().LogError("Student registration for class failed.")));
    }

    public State Submitted { get; private set; } = null!;
    public State Validated { get; private set; } = null!;
    public State SpotTempReserved { get; private set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;
    public State Completed { get; private set; } = null!; // ToDo: Or initial?

    public Event<NewRegistration> NewRegistration { get; private set; } = null!;
    public Event<PaymentStatusUpdate> PaymentStatusUpdate { get; private set; } = null!;
}

public record SpotConfirmed(Guid RegistrationId);

public record ConfirmSpot(Guid RegistrationId, Guid ReservationId);

public record NotifyStudent(Guid RegistrationId, string Subject, string Body);

public record RegisterPayment(Guid RegistrationId);

public record PaymentRegistered(Guid RegistrationId, Guid PaymentId, Uri PaymentLink);

public record SpotTempBooked(Guid RegistrationId, Guid ReservationId);

public record BookSpot(Guid RegistrationId, Guid ClassId, Guid StudentId);

public record RegistrationValidated(Guid RegistrationId);

public record ValidateRegistration(Guid RegistrationId, Guid ClassId, Guid StudentId);

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
    public required Uri? PaymentLink { get; set; }
    public required Guid ReservationId { get; set; }

    // If using Optimistic concurrency, this property is required
    public uint RowVersion { get; set; }
}