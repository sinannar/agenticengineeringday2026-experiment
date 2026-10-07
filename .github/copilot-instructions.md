# Copilot Instructions

## Technology and scope

- This repository is a C#-only .NET 10 solution using .NET Aspire 13.6.
- The solution is organized as a distributed SupportHub application:
  - `SupportHub.Web` is the Blazor frontend using interactive server rendering.
  - `SupportHub.Customers.Api` owns customer-related HTTP endpoints and customer domain behavior.
  - `SupportHub.Tickets.Api` owns ticket-related HTTP endpoints and ticket domain behavior.
  - `SupportHub.AppHost` defines the distributed application, resources, references, health checks, and startup ordering.
  - `SupportHub.ServiceDefaults` contains shared service discovery, HTTP resilience, health checks, metrics, tracing, and OpenTelemetry configuration.
- Keep implementation code in C#.
- Do not introduce JavaScript, TypeScript, or other application-language projects unless explicitly requested.
- Keep services independently deployable.

## Build and test

Run commands from the repository root:

```bash
dotnet restore SupportHub.slnx
dotnet build SupportHub.slnx
dotnet test SupportHub.slnx
```

All automated tests belong under `tests/` and are organized into these three projects:

- `tests/SupportHub.AppHost.Tests` — Aspire distributed application and AppHost integration tests.
- `tests/SupportHub.Customers.Api.Tests` — customer API behavior and domain tests.
- `tests/SupportHub.Tickets.Api.Tests` — ticket API behavior and domain tests.

Run an individual test project when appropriate:

```bash
dotnet test tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj
dotnet test tests/SupportHub.Customers.Api.Tests/SupportHub.Customers.Api.Tests.csproj
dotnet test tests/SupportHub.Tickets.Api.Tests/SupportHub.Tickets.Api.Tests.csproj
```

Changes are complete only when:

- The solution builds.
- Relevant automated tests pass.
- Behavior changes have appropriate tests.
- Distributed/runtime changes have been verified through Aspire where applicable.

## Architecture and service wiring

- Treat `src/SupportHub.AppHost/AppHost.cs` as the source of truth for local resource names and service relationships.
- Always use the exact resource names defined by the AppHost when referencing Aspire resources.
- Use Aspire service discovery and resource references instead of hard-coded hostnames, ports, or environment-specific URLs.
- Register dependencies in the AppHost with `WithReference(...)`.
- Express startup dependencies with `WaitFor(...)` where required.
- API and frontend projects should call `builder.AddServiceDefaults()` and map shared health endpoints with `app.MapDefaultEndpoints()`.
- Use the existing `https+http://<resource-name>` URI pattern for HTTP clients so Aspire service discovery can select an available endpoint.
- Do not put domain behavior in the AppHost.
- Avoid sharing service-owned persistence models, business logic, or database schemas across API projects.
- Communicate across service boundaries using explicit contracts or messages.
- Prefer asynchronous messaging for cross-service workflows where appropriate rather than introducing unnecessary synchronous API chains.
- Design asynchronous handlers with retries, duplicate delivery, idempotency, and eventual consistency in mind.

## Architecture evolution

Do not introduce infrastructure without a concrete application requirement.

When requirements justify durable infrastructure, prefer:

- PostgreSQL for transactional relational data.
- Cosmos DB for document-oriented, activity, audit, or workload-specific data.
- Azure Blob Storage for files and attachments.
- Azure Service Bus for asynchronous messaging and queues.
- Redis for caching.
- Worker services for continuous/background processing.
- Console applications for scheduled, administrative, maintenance, import, or SLA workloads.

Add infrastructure through Aspire integrations and consume it through Aspire-generated references and configuration.

The current AppHost includes Redis for frontend output caching. Preserve the named resource and dependency wiring unless a requirement explicitly changes the caching architecture.

## API implementation conventions

- Prefer ASP.NET Core Minimal APIs.
- Organize API implementation by feature rather than technical layer.
- Keep customer behavior within `SupportHub.Customers.Api`.
- Keep ticket behavior within `SupportHub.Tickets.Api`.
- Keep request and response contracts explicit.
- Do not expose persistence entities directly as public API contracts.
- Validate external input at the API boundary.
- Use appropriate HTTP status codes.
- Use ASP.NET Core Problem Details for API errors.
- Use asynchronous APIs for I/O.
- Propagate `CancellationToken` through asynchronous I/O operations.
- Use dependency injection rather than static or global state.
- Do not introduce repository abstractions over EF Core unless there is a concrete requirement that justifies them.
- Follow the existing OpenAPI, exception handling, health check, and ServiceDefaults conventions.
- Add or update tests whenever API behavior changes.

Prefer feature-oriented organization such as:

```text
Features/
├── Tickets/
│   ├── Create/
│   ├── Get/
│   └── Update/
└── Customers/
    ├── Create/
    ├── Get/
    └── Update/
```

## Frontend conventions

- `SupportHub.Web` is a Blazor Web App using interactive server components.
- Keep the application C#-first.
- Put UI pages, components, and layouts under the existing `Components` structure.
- Prefer small reusable components over large pages.
- Pages and components should coordinate presentation and interaction; backend domain behavior belongs in the owning API.
- Keep API access behind typed `HttpClient` wrappers such as `CustomerApiClient` and `TicketApiClient`.
- Configure backend addresses through Aspire service discovery rather than direct URLs.
- Keep API contracts strongly typed.
- Represent loading, empty, error, and success states explicitly where applicable.
- Do not duplicate backend business rules in the frontend.
- Preserve output-cache integration with the Aspire `cache` resource and standard middleware ordering unless a requirement explicitly changes it.

## ServiceDefaults

- Use `SupportHub.ServiceDefaults` only for cross-cutting hosting concerns shared by services.
- Do not add domain-specific dependencies or business logic to ServiceDefaults.
- Preserve service discovery, standard HTTP resilience, health checks, metrics, distributed tracing, and OpenTelemetry conventions.
- Use OTLP configuration provided by Aspire and `OTEL_EXPORTER_OTLP_ENDPOINT` rather than creating custom telemetry pipelines without a concrete requirement.

## Aspire

Aspire is the default mechanism for composing, running, observing, testing, and diagnosing the distributed application.

- Define infrastructure resources and project relationships in the AppHost.
- Prefer official Aspire integrations when introducing infrastructure.
- Preserve health checks and OpenTelemetry instrumentation.
- Use Aspire service discovery instead of fixed local URLs.
- Use `*.dev.localhost` endpoints where appropriate.
- Verify distributed changes through the running Aspire application rather than relying only on isolated builds or source-code inspection.
- Treat resource health, logs, traces, and runtime state as engineering feedback.

## Aspire skills

Use the repository's available Aspire skills whenever they apply to the current task.

In particular:

- Use `aspire` for general Aspire guidance and application understanding.
- Use `aspire-orchestration` when modifying AppHost resources, references, dependencies, or orchestration.
- Use `aspire-monitoring` when diagnosing or reviewing runtime behavior, logs, traces, metrics, or resource health.
- Use `aspireify` when introducing Aspire patterns to projects or resources.
- Use `dotnet-inspect` when inspecting .NET project configuration, dependencies, or solution structure.
- Use `playwright-cli` when browser-level verification of meaningful frontend behavior is appropriate.
- Use `aspire-deployment` only for deployment-related work.

Follow the applicable skill's workflow and validation guidance rather than hand-authoring integration steps when a skill covers the scenario.

Prefer runtime evidence from Aspire over assumptions derived only from source code.

## Aspire integration tests and verification

- Use `Aspire.Hosting.Testing` for distributed integration tests.
- Start the actual `SupportHub.AppHost` graph when verifying cross-resource behavior.
- Create the testing builder, build and start the application, wait for target resources to become healthy, and then interact with them through Aspire.
- Use `app.CreateHttpClient("<resource-name>")` with the exact resource name defined by the AppHost.
- Assert functional behavior as well as health where appropriate.
- Use Aspire integration tests for behavior spanning services or infrastructure.
- Add focused unit tests when domain logic benefits from isolated verification.
- Keep health and liveness endpoints consistent with ServiceDefaults.
- Do not introduce ad hoc readiness mechanisms that bypass the AppHost resource model.

## Agentic engineering workflow

For substantial engineering changes, prefer the HVE Core RPI workflow:

1. `/rpi-research` — understand the requirement, repository, existing implementation, architecture, service relationships, and relevant runtime topology.
2. `/rpi-plan` — create a concrete implementation plan based on the research before modifying the application.
3. `/rpi-implement` — implement the approved plan and continuously validate the change.
4. `/rpi-review` — review the implementation, tests, architecture, runtime behavior, and observability evidence.

RPI work must respect these repository instructions.

Use the available Aspire skills as part of RPI whenever they are relevant rather than treating Aspire and RPI as separate workflows.

### During research

- Inspect the existing solution before proposing architectural changes.
- Understand the AppHost topology and existing resource relationships.
- Use Aspire and .NET inspection capabilities where they provide useful evidence.

### During planning

- Account for Aspire orchestration and service relationships.
- Identify required tests and runtime verification.
- Avoid introducing infrastructure unless the requirement justifies it.

### During implementation

- Use Aspire skills when modifying orchestration or infrastructure.
- Build and test incrementally.
- Run the distributed application when runtime behavior is affected.
- Inspect resource state and health rather than assuming startup succeeded.
- Use logs and distributed traces to diagnose runtime failures.

### During review

- Review the final code and Git diff.
- Run relevant automated tests.
- Verify affected Aspire resources are healthy.
- Inspect logs and traces for unexpected failures.
- Verify cross-service behavior end-to-end.
- Use browser-level verification when meaningful frontend behavior changed.
- Prefer runtime evidence over source-code reasoning alone.

## Definition of done

Before considering substantial implementation complete:

1. Restore and build the solution successfully.
2. Run and pass the relevant automated tests.
3. Add or update tests for changed behavior.
4. Start or inspect the distributed application with Aspire when runtime behavior changed.
5. Verify affected resources reach the expected health state.
6. Inspect relevant logs and traces for unexpected failures.
7. Verify affected frontend behavior when applicable.
8. Review the final diff for unnecessary, unrelated, or accidental changes.
9. Ensure the implementation follows the architecture and conventions documented here.
