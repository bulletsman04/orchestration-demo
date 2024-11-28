using ClassRegistrationApi.Models;
using Contracts;
using MassTransit;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("classesdb");

builder.Services.AddOptions<SqlTransportOptions>()
    .Configure(options => { options.ConnectionString = connectionString; });

builder.Services.AddMassTransit(x =>
{
    x.AddSqlMessageScheduler();

    x.UsingPostgres((context, cfg) =>
    {
        cfg.UseSqlMessageScheduler();

        // cfg.UsePublishFilter(typeof(CustomerNumberPartitionKeyFilter<>), context);
        // cfg.UseSendFilter(typeof(CustomerNumberPartitionKeyFilter<>), context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();

app.MapPost("/classes/registrations", async (ClassRegistrationRequest classRegistrationRequest, IPublishEndpoint publishEndpoint, CancellationToken ct) =>
    {
        var registrationId = NewId.NextGuid();

        await publishEndpoint.Publish(new NewRegistration
        {
            RegistrationId = registrationId,
            ClassId = classRegistrationRequest.ClassId,
            StudentId = classRegistrationRequest.StudentId
        }, ct);
        
        return Results.Ok(new ClassRegistrationResponse(registrationId, DateTime.UtcNow));
    })
    .WithName("RegisterForClass");


// Just to simulate payment
app.MapPost("/payments/{registrationId:guid}", async (IPublishEndpoint publishEndpoint, Guid registrationId, CancellationToken ct) =>
    {
        await publishEndpoint.Publish(new PaymentStatusUpdate
        {
            RegistationId = registrationId,
            IsSuccessful = true
        }, ct);

    })
    .WithName("ProcessPayment");

app.Run();