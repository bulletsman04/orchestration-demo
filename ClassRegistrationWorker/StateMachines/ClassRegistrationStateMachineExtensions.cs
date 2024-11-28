using Contracts;
using MassTransit;

namespace ClassRegistrationWorker.StateMachines;

using NewRegistrationBinder = EventActivityBinder<ClassRegistrationState, NewRegistration>;
using PaymentRegisteredBinder = EventActivityBinder<ClassRegistrationState, PaymentRegistered>;
using ValidationFailedBinder = EventActivityBinder<ClassRegistrationState, Fault<ValidateRegistration>>;
using ClassFullBinder = EventActivityBinder<ClassRegistrationState, Fault<BookSpot>>;
using SpotConfirmedBinder = EventActivityBinder<ClassRegistrationState, SpotConfirmed>;
using PaymentStatusUpdateBinder = EventActivityBinder<ClassRegistrationState, PaymentStatusUpdate>;
using PaymentReminderBinder = EventActivityBinder<ClassRegistrationState, PaymentReminderSchedule>;

public static class ClassRegistrationStateMachineExtensions
{
    public static NewRegistrationBinder SaveRegistrationData(this NewRegistrationBinder binder)
        => binder.Then(context =>
        {
            context.Saga.ClassId = context.Message.ClassId;
            context.Saga.StudentId = context.Message.StudentId;
        });

    public static PaymentRegisteredBinder SavePaymentData(this PaymentRegisteredBinder binder)
        => binder.Then(context =>
        {
            context.Saga.PaymentId = context.Message.PaymentId;
            context.Saga.PaymentLink = context.Message.PaymentLink;
        });

    public static ValidationFailedBinder NotifyThatValidationFailed(this ValidationFailedBinder binder)
        => binder.Then(context => context.Publish(new NotifyStudent(
                context.Saga.CorrelationId,
                "Class registration failed",
                $"Class or student is not eligible for registration"
            )
        ));

    public static ClassFullBinder NotifyThatClassIsFullyBooked(this ClassFullBinder binder)
        => binder.Then(context => context.Publish(new NotifyStudent(
                context.Saga.CorrelationId,
                "Class is full.",
                $"Class is already full. We put you on waitlist."
            )
        ));

    public static PaymentRegisteredBinder NotifyThatRegistrationSucceededAndShouldBePaid(this PaymentRegisteredBinder binder)
        => binder.Then(context => context.Publish(new NotifyStudent(
                context.Saga.CorrelationId,
                "Registration for class.",
                $"We received you registration. Please use this link to complete your registration: {context.Message.PaymentLink}"
            )
        ));

    public static PaymentStatusUpdateBinder NotifyThatPaymentFailed(this PaymentStatusUpdateBinder binder)
        => binder.Then(context => context.Publish(new NotifyStudent(
                context.Saga.CorrelationId,
                "Class registration failed",
                $"Payment failed for class {context.Saga.ClassId}"
            )
        ));

    public static SpotConfirmedBinder NotifyThatRegistrationIsCompleted(this SpotConfirmedBinder binder)
        => binder.Then(context => context.Publish(new NotifyStudent(
                context.Saga.CorrelationId,
                "Class registration successful",
                $"Payment successful for class {context.Saga.ClassId}"
            )
        ));

    public static PaymentRegisteredBinder SchedulePaymentReminder(this PaymentRegisteredBinder binder,
        Schedule<ClassRegistrationState, PaymentReminderSchedule> schedule)
        => binder.Schedule(schedule, c => new PaymentReminderSchedule(c.Saga.CorrelationId), _ => TimeSpan.FromSeconds(30));

    public static PaymentReminderBinder NotifyThatPaymentPending(this PaymentReminderBinder binder)
        => binder.Then(context => context.Publish(new NotifyStudent(
            context.Saga.CorrelationId,
            "Payment reminder",
            $"Payment for class {context.Saga.ClassId} is due. Payment link: {context.Saga.PaymentLink}"
        )));
}