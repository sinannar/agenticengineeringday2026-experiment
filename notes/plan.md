# Initial Experiment Plan

## Goal

Build SupportHub end-to-end using an agentic engineering workflow and record what works, what fails, and what should eventually become part of the Agentic Engineering workshop.

The experiment uses:

- .NET 10
- .NET Aspire 13.6
- GitHub Copilot CLI
- Aspire Skills
- HVE Core
- C# end-to-end
- Blazor
- ASP.NET Core

## HVE RPI Workflow

Substantial increments use the HVE Core RPI loop:

1. `/rpi-research`
2. `/rpi-plan`
3. `/rpi-implement`
4. `/rpi-review`

Aspire skills should be used throughout the loop when application topology, orchestration, runtime behavior, testing, monitoring, or diagnostics are relevant.

## Planned RPI Loops

### Loop 1 — Customer and Ticket Core

Build the initial SupportHub product behavior.

Scope:

- Customer domain and API behavior
- Ticket domain and API behavior
- Explicit API contracts
- Customer/ticket relationship
- Basic CRUD flows
- Basic Blazor UI
- Typed API clients
- Aspire integration tests

Expected outcome:

SupportHub has a usable customer and ticket workflow running through the Aspire application.

### Loop 2 — PostgreSQL Persistence

Introduce durable transactional persistence.

Scope:

- PostgreSQL through Aspire
- EF Core
- Customer persistence
- Ticket persistence
- Migrations/schema management
- Aspire resource references
- Integration test updates

Expected outcome:

Customer and ticket state survives application restarts and transactional data is owned by the appropriate services.

### Loop 3 — Ticket Attachments

Add file attachments to tickets.

Scope:

- Azure Blob Storage through Aspire
- Upload attachments
- Download attachments
- Attachment metadata
- Ticket attachment UI
- Integration verification

Expected outcome:

Support staff can attach files to tickets without storing file content in transactional ticket data.

### Loop 4 — Async Activity and Audit

Introduce event-driven processing and ticket history.

Scope:

- Azure Service Bus through Aspire
- Ticket lifecycle events
- Worker service
- Cosmos DB for activity/audit data
- Idempotent event processing
- Ticket activity timeline
- Distributed tracing across the workflow

Expected outcome:

Ticket changes produce asynchronous activity records that can be viewed as a ticket timeline.

### Loop 5 — SLA Management

Add operational SLA behavior.

Scope:

- SLA rules
- Priority/customer-tier based deadlines
- At-risk detection
- Breach detection
- Background SLA processing
- Console application for on-demand/admin SLA scans
- Aspire console/terminal capabilities
- Dashboard SLA indicators

Expected outcome:

Support staff can identify tickets approaching or exceeding their SLA, and SLA processing can run both automatically and on demand.

### Loop 6 — Operational Polish and E2E Review

Review and harden the complete distributed application.

Scope:

- Redis caching where justified
- Support dashboard improvements
- Health checks
- Logs
- Metrics
- Distributed traces
- Cross-service correlation
- Failure diagnosis
- End-to-end Aspire integration tests
- Browser-level verification
- Architecture and code review
- Cleanup of unnecessary complexity

Expected outcome:

A complete reference implementation that demonstrates the full agentic engineering feedback loop.

## Target Architecture

Infrastructure should emerge from requirements rather than being created upfront.

| Capability | Technology |
| --- | --- |
| Transactional data | PostgreSQL |
| Activity/audit/document data | Cosmos DB |
| Attachments | Azure Blob Storage |
| Async messaging | Azure Service Bus |
| Caching | Redis |
| Background processing | .NET Worker Service |
| Scheduled/admin/SLA jobs | .NET Console Application |
| Frontend | Blazor |
| APIs | ASP.NET Core |
| Orchestration | .NET Aspire |
| Observability | OpenTelemetry + Aspire Dashboard |

## Experiment Notes

For each RPI loop, capture:

- Requirement/prompt
- Research findings
- Proposed plan
- Architecture decisions
- Aspire skills used
- Copilot/HVE behavior
- Implementation friction
- Failures and recovery
- Runtime diagnostics used
- Test strategy
- Time taken
- What should be prebuilt for the workshop
- What should be demonstrated live
- What should be omitted from the workshop
