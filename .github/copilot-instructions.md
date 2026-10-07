# Copilot Instructions

## Technology and scope

- This repository is a C#-only .NET 10 solution using .NET Aspire 13.6.
- The solution is organized as a distributed SupportHub application:
  - `SupportHub.Web` is the Blazor frontend using interactive server rendering.
  - `SupportHub.Customers.Api` owns customer-related HTTP endpoints.
  - `SupportHub.Tickets.Api` owns ticket-related HTTP endpoints.
  - `SupportHub.AppHost` defines the local distributed application, resources, references, health checks, and startup ordering.
  - `SupportHub.ServiceDefaults` contains shared service discovery, HTTP resilience, health checks, and OpenTelemetry configuration.
- Keep implementation code in C#. Do not introduce JavaScript/TypeScript or other application-language projects unless explicitly requested.

## Build and test

Run commands from the repository root:

```bash
dotnet restore SupportHub.slnx
dotnet build SupportHub.slnx
dotnet test SupportHub.slnx
```

Run one test project:

```bash
dotnet test tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj
dotnet test tests/SupportHub.Customers.Api.Tests/SupportHub.Customers.Api.Tests.csproj
dotnet test tests/SupportHub.Tickets.Api.Tests/SupportHub.Tickets.Api.Tests.csproj
```

Run a single test by name:

```bash
dotnet test tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj \
  --filter "FullyQualifiedName~CustomerApi_ReturnsGreeting"
```

Changes are complete only when the solution builds and the relevant tests pass. Add or update tests with behavior changes.

## Architecture and service wiring

- Treat `SupportHub.AppHost/AppHost.cs` as the source of truth for local resource names and service relationships. Current resource names include `cache`, `customersapi`, `ticketsapi`, and `webfrontend`.
- Use the repository's applicable Aspire skills when adding or integrating new Aspire resources, services, hosting features, deployments, monitoring, or AppHost wiring. Follow the skill's workflow and validation guidance instead of hand-authoring integration steps when a skill covers the scenario.
- Use Aspire service discovery and resource references instead of hard-coded hostnames, ports, or environment-specific URLs. Register dependencies in `AppHost` with `WithReference(...)` and express startup dependencies with `WaitFor(...)` where needed.
- API and frontend projects should call `builder.AddServiceDefaults()` and map the shared health endpoints with `app.MapDefaultEndpoints()`.
- Use the existing `https+http://<resource-name>` URI pattern for HTTP clients so Aspire service discovery can select the available endpoint.
- Keep services independently deployable. Avoid sharing service-owned persistence models, business logic, or database schemas across API projects; use explicit contracts or messages at service boundaries.
- Prefer asynchronous messaging for cross-service workflows rather than introducing synchronous chains between APIs. Design handlers for retries, duplicate delivery, and eventual consistency.
- For new durable infrastructure, prefer PostgreSQL for relational data, Cosmos DB for document/workload-specific data, Azure Storage for blobs, and Azure Service Bus for queues. Add resources through Aspire and consume them through Aspire-generated references/configuration.
- The current AppHost includes Redis for frontend output caching. Preserve the named resource and dependency wiring when modifying that cache.

## Frontend conventions

- `SupportHub.Web` is a Blazor Web App with interactive server components. Put UI pages and layouts under the existing `Components` structure.
- Keep API access behind typed `HttpClient` wrappers such as `CustomerApiClient` and `TicketApiClient`; configure their base addresses through Aspire service discovery rather than direct URLs.
- Preserve the existing output-cache registration for the `cache` resource and the standard middleware order unless a change specifically requires otherwise.

## APIs and shared defaults

- Keep customer behavior in `SupportHub.Customers.Api` and ticket behavior in `SupportHub.Tickets.Api`; do not place domain behavior in `AppHost` or the frontend.
- APIs currently expose OpenAPI only in development and use the shared exception handling and health endpoint conventions. Follow those conventions for new endpoints.
- Extend `SupportHub.ServiceDefaults` for cross-cutting hosting concerns shared by all services, not for domain-specific dependencies.
- `ServiceDefaults` enables service discovery, standard HTTP resilience, ASP.NET Core/HTTP client/runtime metrics, distributed tracing, and OTLP export when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.

## Aspire integration tests and verification

- Use `Aspire.Hosting.Testing` to start the `SupportHub.AppHost` graph in integration tests. Create the testing builder, build/start the app, wait for the target resource to become healthy, then use `app.CreateHttpClient("<resource-name>")`.
- Assert both functional responses and health where appropriate. Use the exact AppHost resource name in test clients and health waits.
- Verify distributed behavior through the Aspire dashboard and OpenTelemetry signals (logs, metrics, traces, and resource health), not only by checking an individual process or HTTP response.
- Keep health and liveness endpoints consistent with `ServiceDefaults`; do not add ad hoc readiness checks that bypass the AppHost resource model.
