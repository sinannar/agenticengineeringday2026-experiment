# Initial Application Setup

## Purpose

This note records the step-by-step process used to create the initial SupportHub baseline.

The goal of this phase is not to build the complete application. It is to create a clean, Aspire-native distributed application that is ready for GitHub Copilot CLI, Aspire Skills, HVE Core RPI workflows, testing, and later architectural evolution.

## Starting Point

Repository:

`agentic-engineering-experiment`

The repository was created with:

- `README.md`
- `.gitignore`
- `LICENSE`
- `notes/`

The application itself is created under `src/`, while the solution file and repository-level agent configuration live at the repository root.

---

## 1. Create the Aspire Starter Application

Create a `src` directory and use the Aspire CLI starter template from inside it:

```bash
mkdir src
cd src

aspire new aspire-starter -n SupportHub -o .
```

The Aspire starter template provides the initial application shape:

```text
SupportHub.ApiService/
SupportHub.AppHost/
SupportHub.ServiceDefaults/
SupportHub.Web/
SupportHub.sln
aspire.config.json
.agents/
```

When Aspire offers to configure AI agent environments, enable them so the repository gets the Aspire skills.

The starter template gives us:

- Aspire AppHost
- ServiceDefaults
- ASP.NET Core API
- Blazor frontend
- Redis-backed frontend caching
- service discovery
- health checks
- OpenTelemetry
- Aspire agent skills

This is deliberately used instead of building the projects individually because Aspire is a first-class part of the application from the beginning.

---

## 2. Move Repository-Level Aspire Configuration to the Root

The Aspire application was generated under `src/`, but agent skills and Aspire repository configuration should apply to the whole repository.

Move:

```text
src/.agents/             -> .agents/
src/aspire.config.json   -> aspire.config.json
```

The repository should now begin to look like:

```text
agentic-engineering-experiment/
├── .agents/
│   └── skills/
├── notes/
├── src/
│   ├── SupportHub.ApiService/
│   ├── SupportHub.AppHost/
│   ├── SupportHub.ServiceDefaults/
│   └── SupportHub.Web/
├── aspire.config.json
├── README.md
├── LICENSE
└── .gitignore
```

The generated solution file is handled separately in the next step.

---

## 3. Replace the Generated Solution with a Root `.slnx`

The Aspire starter generated a traditional solution file under `src/`.

Remove the generated solution file:

```bash
rm src/SupportHub.sln
```

Create a `.slnx` at the repository root:

```bash
dotnet new slnx -n SupportHub
```

The root solution will eventually contain all application and test projects.

---

## 4. Replace the Generic API with Domain APIs

The Aspire starter creates:

```text
SupportHub.ApiService
```

SupportHub has two initial service boundaries:

```text
SupportHub.Customers.Api
SupportHub.Tickets.Api
```

Use the generated API project as the baseline for both APIs because it already contains the correct Aspire ServiceDefaults, health checks, and hosting setup.

Duplicate the generated API:

```bash
cp -R src/SupportHub.ApiService src/SupportHub.Customers.Api
cp -R src/SupportHub.ApiService src/SupportHub.Tickets.Api
```

Then remove the original:

```bash
rm -rf src/SupportHub.ApiService
```

Rename the project files:

```text
SupportHub.Customers.Api/
└── SupportHub.Customers.Api.csproj

SupportHub.Tickets.Api/
└── SupportHub.Tickets.Api.csproj
```

Update namespaces and project-specific identifiers from `SupportHub.ApiService` to the appropriate service name.

At this stage, keep the APIs functionally close to the generated template. The goal is to establish service boundaries before implementing domain features.

---

## 5. Update the Aspire AppHost

Update `SupportHub.AppHost` so the distributed application contains:

- Redis
- Customers API
- Tickets API
- Blazor frontend

The initial AppHost shape is:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var customersApi = builder
    .AddProject<Projects.SupportHub_Customers_Api>("customersapi")
    .WithHttpHealthCheck("/health");

var ticketsApi = builder
    .AddProject<Projects.SupportHub_Tickets_Api>("ticketsapi")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.SupportHub_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(customersApi)
    .WaitFor(customersApi)
    .WithReference(ticketsApi)
    .WaitFor(ticketsApi);

builder.Build().Run();
```

The AppHost becomes the source of truth for resource names and service relationships.

The initial topology is:

```text
                    SupportHub.Web
                         |
              +----------+----------+
              |          |          |
              v          v          v
       Customers API  Tickets API  Redis
```

---

## 6. Add Application Projects to the Root Solution

Add the application projects to `SupportHub.slnx`:

```bash
dotnet sln SupportHub.slnx add src/SupportHub.AppHost/SupportHub.AppHost.csproj
dotnet sln SupportHub.slnx add src/SupportHub.ServiceDefaults/SupportHub.ServiceDefaults.csproj
dotnet sln SupportHub.slnx add src/SupportHub.Web/SupportHub.Web.csproj
dotnet sln SupportHub.slnx add src/SupportHub.Customers.Api/SupportHub.Customers.Api.csproj
dotnet sln SupportHub.slnx add src/SupportHub.Tickets.Api/SupportHub.Tickets.Api.csproj
```

Build the solution:

```bash
dotnet restore SupportHub.slnx
dotnet build SupportHub.slnx
```

---

## 7. Create Test Projects Separately

Tests are created under a root `tests/` directory rather than inside `src/`.

Create the directory:

```bash
mkdir tests
```

### Aspire AppHost integration tests

Use the Aspire xUnit template:

```bash
dotnet new aspire-xunit \
  -n SupportHub.AppHost.Tests \
  -o tests/SupportHub.AppHost.Tests
```

Reference the AppHost:

```bash
dotnet add tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj \
  reference src/SupportHub.AppHost/SupportHub.AppHost.csproj
```

### Customers API tests

```bash
dotnet new xunit \
  -n SupportHub.Customers.Api.Tests \
  -o tests/SupportHub.Customers.Api.Tests

dotnet add tests/SupportHub.Customers.Api.Tests/SupportHub.Customers.Api.Tests.csproj \
  reference src/SupportHub.Customers.Api/SupportHub.Customers.Api.csproj
```

### Tickets API tests

```bash
dotnet new xunit \
  -n SupportHub.Tickets.Api.Tests \
  -o tests/SupportHub.Tickets.Api.Tests

dotnet add tests/SupportHub.Tickets.Api.Tests/SupportHub.Tickets.Api.Tests.csproj \
  reference src/SupportHub.Tickets.Api/SupportHub.Tickets.Api.csproj
```

Add all test projects to the solution:

```bash
dotnet sln SupportHub.slnx add tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj
dotnet sln SupportHub.slnx add tests/SupportHub.Customers.Api.Tests/SupportHub.Customers.Api.Tests.csproj
dotnet sln SupportHub.slnx add tests/SupportHub.Tickets.Api.Tests/SupportHub.Tickets.Api.Tests.csproj
```

The AppHost tests are the primary place for distributed integration verification. The API test projects are available for focused service-level tests where they are useful.

---

## 8. Use GitHub Copilot CLI to Repair and Complete the Refactor

The template duplication and renaming intentionally leave some mechanical work to the coding agent.

Start GitHub Copilot CLI from the repository root:

```bash
copilot
```

Use Copilot to inspect the repository and resolve issues introduced by the refactor.

Example request:

```text
Inspect this repository after splitting the generated Aspire ApiService into
Customers.Api and Tickets.Api.

Fix project names, namespaces, references, AppHost project references,
frontend service references, and any build errors caused by the rename.

Do not introduce new architecture or features yet. Preserve the existing
Aspire starter behavior and get the complete solution building and running.
```

Then ask Copilot to verify the result:

```text
Build the complete SupportHub.slnx solution, run the tests, and fix any
remaining issues caused by the project restructuring.

Use the available Aspire skills where relevant.
```

This step is useful for the experiment because it demonstrates an early agentic engineering pattern:

```text
Human establishes architecture
        |
        v
Agent inspects repository
        |
        v
Agent performs mechanical refactoring
        |
        v
Build / test feedback
        |
        v
Agent repairs remaining issues
```

---

## 9. Establish Aspire Integration Tests

Replace the generated `IntegrationTest1.cs` with tests organized around the SupportHub resources.

For example:

```text
SupportHub.AppHost.Tests/
├── CustomerApiTests.cs
├── TicketsApiTests.cs
└── WebTests.cs
```

Aspire integration tests should:

1. Create a `DistributedApplicationTestingBuilder`.
2. Start the actual `SupportHub.AppHost`.
3. Wait for the target resource to become healthy.
4. Use `app.CreateHttpClient("<resource-name>")`.
5. Verify actual distributed application behavior.

The exact resource names must match the names in `AppHost.cs`.

---

## 10. Add Repository-Level Copilot Instructions

Create:

```text
.github/copilot-instructions.md
```

The instructions document:

- .NET 10 and Aspire 13.6
- C#-only implementation
- Blazor frontend conventions
- API boundaries
- Minimal API conventions
- Aspire service discovery
- AppHost orchestration rules
- ServiceDefaults responsibilities
- Aspire testing
- Aspire skills
- target infrastructure preferences
- HVE Core RPI workflow
- definition of done

The purpose is to make the repository self-describing so later HVE RPI prompts can focus on the requirement rather than repeatedly explaining the architecture.

---

## 11. Verify the Initial Distributed Application

From the repository root:

```bash
dotnet restore SupportHub.slnx
dotnet build SupportHub.slnx
dotnet test SupportHub.slnx
```

Run the application with Aspire:

```bash
aspire run
```

Verify in the Aspire Dashboard that:

- `cache` is running.
- `customersapi` is healthy.
- `ticketsapi` is healthy.
- `webfrontend` is healthy.
- Web can resolve the APIs through Aspire service discovery.
- Logs and traces are available.
- Integration tests can start the AppHost independently.

---

## 12. Initial Repository Shape

The resulting baseline is:

```text
agentic-engineering-experiment/
├── .agents/
│   └── skills/
│       ├── aspire/
│       ├── aspire-deployment/
│       ├── aspire-init/
│       ├── aspire-monitoring/
│       ├── aspire-orchestration/
│       ├── aspire-project-v2-migration/
│       ├── aspireify/
│       ├── dotnet-inspect/
│       └── playwright-cli/
├── .github/
│   └── copilot-instructions.md
├── notes/
│   ├── init.md
│   └── initial-plan.md
├── src/
│   ├── SupportHub.AppHost/
│   ├── SupportHub.Customers.Api/
│   ├── SupportHub.ServiceDefaults/
│   ├── SupportHub.Tickets.Api/
│   └── SupportHub.Web/
├── tests/
│   ├── SupportHub.AppHost.Tests/
│   ├── SupportHub.Customers.Api.Tests/
│   └── SupportHub.Tickets.Api.Tests/
├── .gitignore
├── aspire.config.json
├── LICENSE
├── README.md
└── SupportHub.slnx
```

---

## What Is Intentionally Missing

The baseline does not create the complete target architecture.

These capabilities will be introduced through HVE RPI feature loops:

- Customer and ticket domain functionality
- PostgreSQL
- Cosmos DB
- Azure Blob Storage
- Azure Service Bus
- background workers
- console workloads
- SLA processing
- activity/audit processing
- richer Redis scenarios
- operational dashboard features

The architectural principle is:

> Infrastructure should emerge from application requirements.

---

## Next Step

With the baseline complete, begin the first HVE Core RPI loop:

```text
/rpi-research
    |
/rpi-plan
    |
/rpi-implement
    |
/rpi-review
```

The first loop introduces the initial Customer and Ticket product functionality while using Aspire as the runtime and verification environment.
