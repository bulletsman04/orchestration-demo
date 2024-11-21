using AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgresserver",
        builder.CreateResourceBuilder(new ParameterResource("username", _ => "postgres")),
        builder.CreateResourceBuilder(new ParameterResource("password", _ => "Password12!")))
    .WithDataVolume()
    .WithPgAdmin();

var backEndDb = postgres.AddDatabase("classesdb", "classesdb");

var backEnd = builder.AddProject<ClassRegistrationWorker>("Worker")
    .WithReference(backEndDb)
    .WaitFor(postgres);

var api = builder.AddProject<ClassRegistrationApi>("Api")
    .WithReference(backEndDb)
    .WaitFor(postgres);


builder.Build().Run();