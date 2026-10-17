var builder = DistributedApplication.CreateBuilder(args);

// ============================================================
// Infrastructure
// ============================================================

// Redis - frontend caching
var cache = builder.AddRedis("cache");

// PostgreSQL - transactional databases
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

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

// Customers API
var customersapi = builder
    .AddProject<Projects.SupportHub_Customers_Api>("customersapi")
    .WithReference(customersDb)
    .WaitFor(customersDb)
    .WithHttpHealthCheck("/health");

// Tickets API
var ticketsapi = builder
    .AddProject<Projects.SupportHub_Tickets_Api>("ticketsapi")
    .WithReference(ticketsDb)
    .WithReference(blobs)
    .WithReference(serviceBus)
    .WaitFor(ticketsDb)
    .WaitFor(blobs)
    .WaitFor(serviceBus)
    .WithHttpHealthCheck("/health");

// ============================================================
// Background Workers
// ============================================================

// Activity Worker - consumes ticket events and stores audit data
builder.AddProject<Projects.SupportHub_Activity_Worker>("activity-worker")
    .WithReference(serviceBus)
    .WithReference(activityDb)
    .WaitFor(serviceBus)
    .WaitFor(activityDb);

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