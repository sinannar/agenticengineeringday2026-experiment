using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================
// Infrastructure
// ============================================================

// Redis - frontend caching
var cache = builder.AddRedis("cache");

// PostgreSQL - transactional databases
var persistenceTestMode = builder.Configuration.GetValue<bool>("SUPPORTHUB_PERSISTENCE_TEST_MODE");
var postgresPassword = persistenceTestMode
    ? builder.AddParameter("supporthub-persistence-test-postgres-password", secret: true)
    : null;
var postgres = postgresPassword is null
    ? builder.AddPostgres("postgres")
    : builder.AddPostgres("postgres", password: postgresPassword);

if (persistenceTestMode)
{
    var volumeName = builder.Configuration["SUPPORTHUB_PERSISTENCE_TEST_VOLUME_NAME"]
        ?? throw new InvalidOperationException(
            "SUPPORTHUB_PERSISTENCE_TEST_VOLUME_NAME is required when persistence test mode is enabled.");
    postgres.WithDataVolume(volumeName);
}
else if (!builder.Configuration.GetValue<bool>("SUPPORTHUB_TEST_MODE"))
{
    postgres.WithDataVolume();
}

var customersDb = postgres.AddDatabase("customersdb");
var ticketsDb = postgres.AddDatabase("ticketsdb");

// Azure Storage - Azurite emulator for ticket attachments
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var blobs = storage.AddBlobs("blobs");

// Azure Service Bus - local emulator for ticket events
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

var ticketEvents = serviceBus.AddServiceBusQueue("ticket-events");

// Azure Cosmos DB - Linux-based emulator for activity/audit
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator();

var activityDb = cosmos.AddCosmosDatabase("activitydb");

// ============================================================
// APIs
// ============================================================

// Activity Worker - consumes ticket events and stores audit data
var activityWorker = builder.AddProject<Projects.SupportHub_Activity_Worker>("activity-worker")
    .WithReference(serviceBus)
    .WithReference(activityDb)
    .WaitFor(serviceBus)
    .WaitFor(activityDb)
    .WithHttpEndpoint()
    .WithHttpHealthCheck("/health");

// Customers API
var customersapi = builder
    .AddProject<Projects.SupportHub_Customers_Api>("customersapi")
    .WithReference(customersDb)
    .WaitFor(customersDb)
    .WithHttpHealthCheck("/health");

if (builder.Environment.IsDevelopment())
{
    customersapi.WithEnvironment("SUPPORTHUB_APPLY_MIGRATIONS", "true");
}

// Tickets API
var ticketsapi = builder
    .AddProject<Projects.SupportHub_Tickets_Api>("ticketsapi")
    .WithReference(ticketsDb)
    .WithReference(customersapi)
    .WithReference(blobs)
    .WithReference(serviceBus)
    .WithReference(activityWorker)
    .WaitFor(ticketsDb)
    .WaitFor(customersapi)
    .WaitFor(blobs)
    .WaitFor(serviceBus)
    .WithHttpHealthCheck("/health");

if (builder.Environment.IsDevelopment())
{
    ticketsapi.WithEnvironment("SUPPORTHUB_APPLY_MIGRATIONS", "true");
}

// ============================================================
// Background Workers
// ============================================================

// SLA Worker - monitors ticket SLA deadlines
builder.AddProject<Projects.SupportHub_Sla_Worker>("sla-worker")
    .WithReference(ticketsapi)
    .WaitFor(ticketsapi);

// ============================================================
// Interactive Operations CLI
// ============================================================

#pragma warning disable ASPIRETERMINAL001

builder.AddProject<Projects.SupportHub_Sla_Console>("supporthub-cli")
    .WithReference(customersapi)
    .WithReference(ticketsapi)
    .WaitFor(customersapi)
    .WaitFor(ticketsapi)
    .WithExplicitStart()
    .WithTerminal();

#pragma warning restore ASPIRETERMINAL001

// ============================================================
// Blazor Frontend
// ============================================================

builder.AddProject<Projects.SupportHub_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(customersapi)
    .WaitFor(customersapi)
    .WithReference(ticketsapi)
    .WaitFor(ticketsapi);

// ============================================================
// Run
// ============================================================

builder.Build().Run();