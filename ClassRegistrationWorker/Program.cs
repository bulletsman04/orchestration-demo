using System.ComponentModel.DataAnnotations;
using ClassRegistrationWorker;
using ClassRegistrationWorker.Services;
using ClassRegistrationWorker.StateMachines;
using ClassRegistrationWorker.StateMachines.RequestConsumers;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

// ToDo: Separate project for mass tranist
builder.Services.AddMassTransit(x =>
{
    // x.AddConsumer<NewRegistrationConsumer>();
    // x.AddConsumer<PaymentStatusUpdateConsumer>();
    // x.AddConsumer<PaymentReminderConsumer>();
    // ToDo: auto register?
    x.AddConsumer<ValidateRegistrationConsumer>();
    x.AddConsumer<BookSpotConsumer>();
    x.AddConsumer<RegisterPaymentConsumer>();
    x.AddConsumer<NotifyStudentConsumer>();
    x.AddConsumer<ConfirmSpotConsumer>();
    
    x.AddSagaStateMachine<ClassRegistrationStateMachine, ClassRegistrationState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic; // or use Pessimistic, which does not require RowVersion

            r.AddDbContext<DbContext, ClassesDbContext>((provider, builder) =>
            {
                builder.UseNpgsql(connectionString, m =>
                {
                    m.MigrationsAssembly(typeof(Program).Assembly.GetName().Name);
                    m.MigrationsHistoryTable($"__{nameof(ClassesDbContext)}");
                });
            });

            //This line is added to enable PostgreSQL features
            r.UsePostgres();
        });

    x.AddConfigureEndpointsCallback((provider, name, cfg) =>
    {
        if (cfg is ISqlReceiveEndpointConfigurator sql)
        {
            sql.LockDuration = TimeSpan.FromMinutes(10);

            // Ensure messages are consumed in order within a partition
            // Prevents head-of-line blocking across customers
            sql.SetReceiveMode(SqlReceiveMode.PartitionedOrdered);
        }

        cfg.UseInMemoryOutbox(provider);
    });

    x.AddSqlMessageScheduler();

    x.UsingPostgres((context, cfg) =>
    {
        cfg.UseSqlMessageScheduler();

        // cfg.UsePublishFilter(typeof(CustomerNumberPartitionKeyFilter<>), context);
        // cfg.UseSendFilter(typeof(CustomerNumberPartitionKeyFilter<>), context);
        cfg.ConfigureEndpoints(context);
    });

    x.AddDbContext<ClassesDbContext>(x => x.UseNpgsql(connectionString));
});

// builder.AddNpgsqlDbContext<ClassesDbContext>(connectionName: "classesdb");

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
        public ClassesDbContext(DbContextOptions<ClassesDbContext> options) : base(options)
        {
        }

        public DbSet<ClassRegistration> ClassRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var configuration in Configurations)
                configuration.Configure(modelBuilder);
        }

        private IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new ClassRegistrationStateMap(); }
        }
    }

    public class ClassRegistration
    {
        [Key] public int Id { get; set; }
        public required Guid RegistrationId { get; set; } // ToDo: Index
        public required Guid StudentId { get; set; }
        public required Guid ClassId { get; set; }
        public required Guid PaymentId { get; set; }
        public required Guid ReservationId { get; set; }

        public required bool IsCompleted { get; set; }
    }

    public class ClassRegistrationStateMap :
        SagaClassMap<ClassRegistrationState>
    {
        protected override void Configure(EntityTypeBuilder<ClassRegistrationState> entity, ModelBuilder model)
        {
            entity.Property(x => x.CurrentState).HasMaxLength(64);

            // If using Optimistic concurrency, otherwise remove this property
            entity.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .IsRowVersion();
        }
    }
}