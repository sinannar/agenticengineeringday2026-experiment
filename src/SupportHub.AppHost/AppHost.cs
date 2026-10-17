var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var customersapi = builder.AddProject<Projects.SupportHub_Customers_Api>("customersapi")
    .WithHttpHealthCheck("/health");
var ticketsapi = builder.AddProject<Projects.SupportHub_Tickets_Api>("ticketsapi")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.SupportHub_Activity_Worker>(
    "activity-worker");

builder.AddProject<Projects.SupportHub_Sla_Worker>(
    "sla-worker");

#pragma warning disable ASPIRETERMINAL001
builder.AddProject<Projects.SupportHub_Sla_Console>("supporthub-cli")
    .WithReference(customersapi)
    .WithReference(ticketsapi)
    .WaitFor(customersapi)
    .WaitFor(ticketsapi)
    .WithExplicitStart()
    .WithTerminal();

#pragma warning restore ASPIRETERMINAL001
     
builder.AddProject<Projects.SupportHub_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(customersapi)
    .WaitFor(customersapi)
    .WithReference(ticketsapi)
    .WaitFor(ticketsapi);

builder.Build().Run();
