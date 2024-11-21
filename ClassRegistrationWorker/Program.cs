using System.ComponentModel.DataAnnotations;
using ClassRegistrationWorker;
using ClassRegistrationWorker.Consumers;
using ClassRegistrationWorker.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);


builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("classesdb");

builder.Services.AddOptions<SqlTransportOptions>()
    .Configure(options => { options.ConnectionString = connectionString; });

builder.Services.AddPostgresMigrationHostedService(options =>
{
    // default, but shown for completeness
    options.CreateDatabase = true;
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<NewRegistrationConsumer>();
    x.AddConsumer<PaymentStatusUpdateConsumer>();
    x.AddConsumer<PaymentReminderConsumer>();

    x.AddConfigureEndpointsCallback((provider, name, cfg) =>
    {
        if (cfg is ISqlReceiveEndpointConfigurator sql)
        {
            sql.LockDuration = TimeSpan.FromMinutes(10);

            // Ensure messages are consumed in order within a partition
            // Prevents head-of-line blocking across customers
            sql.SetReceiveMode(SqlReceiveMode.PartitionedOrdered);
        }
    });

    x.AddSqlMessageScheduler();

    x.UsingPostgres((context, cfg) =>
    {
        cfg.UseSqlMessageScheduler();

        // cfg.UsePublishFilter(typeof(CustomerNumberPartitionKeyFilter<>), context);
        // cfg.UseSendFilter(typeof(CustomerNumberPartitionKeyFilter<>), context);
        cfg.ConfigureEndpoints(context);
    });
});

builder.AddNpgsqlDbContext<ClassesDbContext>(connectionName: "classesdb");

builder.Services.AddTransient<IPaymentService, PaymentService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IClassesService, ClassesService>();
builder.Services.AddTransient<IStudentService, StudentService>();
builder.Services.AddTransient<IAvailabilityService, AvailabilityService>();


builder.Services.AddHostedService<ClassRegistrationWorker.Worker>();

var host = builder.Build();

// Apply pending migrations
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ClassesDbContext>();
    dbContext.Database.Migrate();
}

host.Run();

namespace ClassRegistrationWorker
{
    public class ClassesDbContext : DbContext
    {
        public ClassesDbContext(DbContextOptions<ClassesDbContext> options) : base(options) {}
        public DbSet<ClassRegistration> ClassRegistrations { get; set; }
    }

    public class ClassRegistration
    {
        [Key] public int Id { get; set; }
        public required Guid StudentId { get; set; }
        public required Guid ClassId { get; set; }
        public required Guid PaymentId { get; set; }
        public required Guid ReservationId { get; set; }
        
        public required bool IsCompleted { get; set; }
    }
}