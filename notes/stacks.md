# SupportHub — Feature Implementation Roadmap

## Overview

SupportHub is a distributed customer support and ticket management application built using .NET 10, .NET Aspire 13.6, ASP.NET Core Minimal APIs, Blazor Interactive Server, and Azure-compatible local infrastructure.

The application will be implemented through **six GitHub stacked pull requests**, with each PR representing one feature and one complete **HVE Core Research → Plan → Implement → Review** loop.

GitHub Copilot CLI is the primary engineering agent. Aspire Skills provide application orchestration, infrastructure integration, runtime diagnostics, and testing support. The GitHub `gh-stack` skill manages the stacked PR workflow.

## Prerequisites — Aspire Foundation

Before starting feature development, the `main` branch must contain:

- All SupportHub application projects.
- PostgreSQL with `customersdb` and `ticketsdb`.
- Azure Blob Storage using Azurite.
- Azure Service Bus emulator with the `ticket-events` queue.
- Azure Cosmos DB Linux emulator with `activitydb`.
- Redis caching infrastructure.
- Aspire service discovery, health checks, and OpenTelemetry.
- Required Aspire client integration NuGet packages.
- Repository-level GitHub Copilot instructions.
- Passing solution builds and baseline tests.

Infrastructure configuration belongs to the foundation. Business functionality will be implemented in the feature PRs.

## Feature Stack

| PR | Branch | Feature | Parent branch |
|---|---|---|---|
| 1 | `feature/customers` | Customer Management | `main` |
| 2 | `feature/tickets` | Ticket Management | `feature/customers` |
| 3 | `feature/attachments` | Ticket Attachments | `feature/tickets` |
| 4 | `feature/activity` | Activity & Audit | `feature/attachments` |
| 5 | `feature/sla` | SLA Management | `feature/activity` |
| 6 | `feature/dashboard` | Support Dashboard & Operational Polish | `feature/sla` |

The branches form a dependency chain:

```text
main
  └── feature/customers
        └── feature/tickets
              └── feature/attachments
                    └── feature/activity
                          └── feature/sla
                                └── feature/dashboard
```

## PR 1 — Customer Management

**Branch:** `feature/customers`  
**Parent:** `main`

### Scope

Implement customer management through the Customers API and Blazor frontend.

Features:

- Create, retrieve, update, and delete customers.
- Store customer name, contact information, and support tier.
- Validate customer input.
- Implement PostgreSQL persistence using EF Core.
- Create and apply database migrations.
- Expose customer CRUD endpoints using ASP.NET Core Minimal APIs.
- Implement customer list, detail, create, and edit pages in Blazor.
- Add API and integration tests.

### Primary projects

- `SupportHub.Customers.Api`
- `SupportHub.Web`
- `SupportHub.Customers.Api.Tests`

### Infrastructure

- PostgreSQL (`customersdb`)

### Acceptance criteria

- Customer CRUD works through the API and frontend.
- Validation rejects invalid input.
- Customer records persist across application restarts.
- EF Core migrations apply successfully.
- API and integration tests pass.
- Customer operations are observable through Aspire.

---

## PR 2 — Ticket Management

**Branch:** `feature/tickets`  
**Parent:** `feature/customers`

### Scope

Implement the core ticket lifecycle and customer association.

Features:

- Create, retrieve, update, and delete tickets.
- Associate tickets with existing customers.
- Manage ticket title, description, status, and priority.
- Support ticket assignment.
- Track creation and modification timestamps.
- Filter tickets by status, priority, customer, and assignee.
- Implement PostgreSQL persistence using EF Core.
- Create and apply database migrations.
- Expose ticket endpoints using ASP.NET Core Minimal APIs.
- Implement ticket management pages in Blazor.
- Validate customer references through the Customers API.
- Add API and integration tests.

### Primary projects

- `SupportHub.Tickets.Api`
- `SupportHub.Customers.Api`
- `SupportHub.Web`
- `SupportHub.Tickets.Api.Tests`

### Infrastructure

- PostgreSQL (`ticketsdb`)
- Aspire service discovery between APIs

### Acceptance criteria

- Support staff can manage tickets end to end.
- Tickets reference valid customers.
- Invalid customer associations are rejected.
- Ticket data persists across restarts.
- Ticket filtering works correctly.
- Tests pass and API communication is observable through Aspire.

---

## PR 3 — Ticket Attachments

**Branch:** `feature/attachments`  
**Parent:** `feature/tickets`

### Scope

Add attachment management to support tickets.

Features:

- Upload files to tickets.
- Download ticket attachments.
- List attachments associated with a ticket.
- Delete attachments.
- Store binary content in Azure Blob Storage.
- Store attachment metadata in PostgreSQL.
- Validate file sizes and allowed file types.
- Validate ticket ownership and attachment access.
- Add attachment controls to the Blazor frontend.
- Handle upload failures and missing files.
- Add integration tests using Azurite.

### Primary projects

- `SupportHub.Tickets.Api`
- `SupportHub.Web`
- `SupportHub.Tickets.Api.Tests`

### Infrastructure

- Azure Blob Storage (Azurite)
- PostgreSQL (`ticketsdb`)

### Acceptance criteria

- Attachments can be uploaded, listed, downloaded, and deleted.
- Attachment metadata remains associated with the correct ticket.
- Invalid files are rejected.
- Failed uploads are handled safely.
- Integration tests pass against local Blob Storage.

---

## PR 4 — Activity & Audit

**Branch:** `feature/activity`  
**Parent:** `feature/attachments`

### Scope

Introduce asynchronous ticket lifecycle events and audit history.

Features:

- Publish ticket lifecycle events from Tickets API.
- Support ticket-created, ticket-updated, status-changed, and assignment-changed events.
- Send events through the `ticket-events` Service Bus queue.
- Implement event consumption in Activity Worker.
- Persist audit documents in Cosmos DB.
- Handle duplicate events through idempotent processing.
- Implement appropriate retry and failure handling.
- Expose ticket activity history through an application API.
- Display ticket activity history in Blazor.
- Add message processing and integration tests.

### Primary projects

- `SupportHub.Tickets.Api`
- `SupportHub.Activity.Worker`
- `SupportHub.Web`
- `SupportHub.Tickets.Api.Tests`
- `SupportHub.AppHost.Tests`

### Infrastructure

- Azure Service Bus emulator (`ticket-events`)
- Azure Cosmos DB Linux emulator (`activitydb`)

### Acceptance criteria

- Ticket operations publish the expected events.
- Activity Worker consumes and processes events.
- Audit documents are stored in Cosmos DB.
- Duplicate events do not create duplicate audit records.
- Ticket history is visible through the application.
- Event processing and failures are observable through Aspire logs and traces.

---

## PR 5 — SLA Management

**Branch:** `feature/sla`  
**Parent:** `feature/activity`

### Scope

Implement service-level agreement evaluation and monitoring.

Features:

- Define SLA rules based on ticket priority and customer support tier.
- Calculate SLA deadlines.
- Identify tickets approaching their SLA deadlines.
- Detect breached SLAs.
- Implement scheduled evaluations in SLA Worker.
- Expose appropriate SLA operations through Tickets API.
- Add on-demand SLA scanning to the interactive Operations Console.
- Reuse SLA evaluation logic between scheduled and on-demand operations.
- Display SLA status and warnings in Blazor.
- Add unit and integration tests for SLA calculations.

### Primary projects

- `SupportHub.Tickets.Api`
- `SupportHub.Sla.Worker`
- `SupportHub.Sla.Console`
- `SupportHub.Web`
- `SupportHub.Tickets.Api.Tests`

### Infrastructure

- PostgreSQL (`ticketsdb`)
- Aspire service discovery
- Aspire interactive terminal

### Acceptance criteria

- SLA deadlines are calculated correctly.
- At-risk and breached tickets are identified accurately.
- Scheduled SLA scans execute successfully.
- Operations Console can trigger an on-demand scan.
- Scheduled and on-demand evaluations use consistent rules.
- Time-dependent SLA calculations have automated tests.
- SLA activity is observable through Aspire.

---

## PR 6 — Support Dashboard & Operational Polish

**Branch:** `feature/dashboard`  
**Parent:** `feature/sla`

### Scope

Complete the operational dashboard and validate the full distributed application.

Features:

- Implement support dashboard metrics.
- Display open ticket counts.
- Display tickets grouped by status and priority.
- Display at-risk and breached SLA counts.
- Add Redis caching for suitable read-heavy operations.
- Implement cache expiration and invalidation where required.
- Improve API error handling and resilience.
- Improve health checks and structured logging.
- Verify OpenTelemetry traces across services.
- Validate the complete application using Aspire integration tests.
- Add end-to-end UI verification for important user journeys.

### Primary projects

- `SupportHub.Web`
- `SupportHub.Customers.Api`
- `SupportHub.Tickets.Api`
- `SupportHub.Activity.Worker`
- `SupportHub.Sla.Worker`
- `SupportHub.AppHost.Tests`

### Infrastructure

- Redis
- PostgreSQL
- Azure Blob Storage
- Azure Service Bus
- Azure Cosmos DB
- Aspire Dashboard and OpenTelemetry

### Acceptance criteria

- Dashboard metrics accurately reflect application state.
- Redis caching improves suitable queries without compromising correctness.
- Relevant cache entries are invalidated after data changes.
- All services start and communicate successfully.
- Health checks, logs, and traces provide useful diagnostics.
- End-to-end tests cover the major customer and ticket workflows.
- The complete SupportHub application runs successfully through Aspire.

---

## HVE Core Workflow for Every PR

Every feature PR follows the same engineering process.

### 1. Research — `/rpi-research`

- Understand the feature requirements.
- Inspect existing implementation and architecture.
- Identify affected projects and service boundaries.
- Examine relevant Aspire resources and integrations.
- Research applicable implementation patterns.
- Identify testing requirements and risks.

### 2. Plan — `/rpi-plan`

- Define the implementation scope.
- Identify API contracts and data models.
- Plan persistence and infrastructure integration.
- Define frontend changes.
- Identify unit, integration, and end-to-end tests.
- Establish acceptance criteria.
- Keep the plan limited to the current PR.

### 3. Implement — `/rpi-implement`

- Implement the approved plan.
- Follow existing repository conventions.
- Use Aspire Skills for platform-specific implementation.
- Add automated tests alongside functionality.
- Run builds and relevant tests.
- Validate affected services using Aspire.

### 4. Review — `/rpi-review`

- Review implementation against requirements.
- Verify service ownership and architectural boundaries.
- Run relevant tests and integration checks.
- Inspect Aspire resource health, logs, and traces.
- Resolve identified issues.
- Confirm acceptance criteria before PR submission.

## GitHub Stacked PR Workflow

Use the GitHub `gh-stack` skill and CLI extension to manage the stack.

Guidelines:

1. Start the first feature branch from `main`.
2. Complete the HVE Core loop for that feature.
3. Commit and review the implementation.
4. Submit the PR against its parent branch.
5. Create the next feature branch on top of the preceding feature branch.
6. Use the `gh-stack` tooling to synchronize and update the stack.
7. Keep each PR focused on its own feature.
8. Avoid implementing functionality belonging to later PRs.
9. Ensure each PR leaves the application working relative to its parent.
10. Update the stack appropriately as earlier PRs are merged.

Do not submit PRs automatically without explicit approval.

## Final Deliverable

After all six feature PRs are implemented, SupportHub should provide:

- Complete customer management.
- Complete ticket management.
- Ticket attachments using Azure Blob Storage.
- Asynchronous ticket activity and audit history.
- SLA evaluation and monitoring.
- Interactive operational tooling.
- Support dashboard and Redis caching.
- Distributed observability through Aspire.
- Automated unit, integration, and end-to-end tests.

The completed application will serve as the reference implementation for the Agentic Engineering workshop.

The two-hour workshop will use a prepared starter template and selected feature loops rather than requiring attendees to implement the entire six-PR stack.