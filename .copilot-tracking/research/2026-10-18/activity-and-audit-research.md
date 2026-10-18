<!-- markdownlint-disable-file -->
# Task Research: activity-and-audit

| Field              | Value |
|--------------------|-------|
| Date               | 2026-10-18 |
| Researcher / agent | rpi-research |
| Output mode        | convergence |

## Executive Summary

* Bottom line: Implement Activity and Audit as an asynchronous, at-least-once vertical slice with a Tickets API outbox, the existing `ticket-events` queue, an Activity Worker that owns Cosmos activity documents, and a backend history route consumed by the existing Blazor ticket detail page.
* Why this matters: The current Tickets API commits directly to PostgreSQL and has no event abstraction; publishing directly after `SaveChangesAsync` can lose an audit event if the process fails between the two operations. An outbox makes committed ticket changes discoverable for retry without moving audit ownership into the Tickets API.
* Research status: Complete — one balanced cycle completed with Wider, Deeper, and Contrarian waves.
* Confidence and uncertainty: High confidence in service boundaries, existing resource names, event triggers, consumer idempotency key, and test seams. The exact API shape for the Activity Worker read surface and the outbox dispatcher’s concurrency policy remain implementation details to verify during planning.

## What You May Not Know

The branch `feature/activity` is currently clean and points at the attachment parent commit; the Activity Worker is only a logging loop. AppHost placeholders for Service Bus and Cosmos already exist, but they do not prove event publication or persistence behavior.

There is no authentication or actor identity model. Events can carry an optional actor field or source label, but this feature cannot provide user identity or authorization. The local Service Bus emulator is for development/testing, does not persist entities across container restarts, and does not provide production guarantees.

## Findings

### The existing topology already contains the required infrastructure, but application behavior is absent

`SupportHub.AppHost` creates Azure Service Bus resource `messaging`, queue `ticket-events`, Cosmos resource `cosmos`, and database `activitydb`. It references Service Bus from `ticketsapi`, and Service Bus plus `activitydb` from `activity-worker`, with startup waits. The Tickets API registers the `messaging` client and the Worker registers both `messaging` and `activitydb`. No new infrastructure resource is needed for the feature.

The Worker currently only logs once per second, and the Tickets API’s create/update/delete handlers only save the EF entity. This means the feature must add contracts, publication, consumption, persistence, and reads rather than adapt an existing activity implementation.

* Questions: Q1, Q2
* Evidence state: evidence-backed finding
* Evidence: C1, C2, C3, C5, C6
* Confidence and limits: High. Runtime health was not observed because no AppHost was running; source topology and package references are verified.

### Ticket lifecycle events should be emitted from the Tickets API after comparing the persisted state

The existing `Ticket` model has `Id`, `Status`, `Assignee`, customer/reference fields, and timestamps. Create has one complete initial state. Update receives the complete replacement request and currently applies it in place, so the handler can capture the old status/assignee before applying the request. Emit:

* `ticket.created` for every successful create.
* `ticket.updated` for every successful update, with a changed-field or before/after summary.
* `ticket.status-changed` only when status differs.
* `ticket.assignment-changed` only when assignee differs, including null transitions.

Use an explicit versioned envelope rather than sharing `Ticket` or `TicketResponse` as a message contract. A useful envelope contains `eventId`, `eventType`, `schemaVersion`, `ticketId`, `occurredAtUtc`, optional `actor`, and an immutable payload. Set Service Bus `MessageId` to `eventId`, `Subject` to `eventType`, `ContentType` to `application/json`, and `CorrelationId` to the ticket ID.

* Questions: Q3
* Evidence state: evidence-backed recommendation
* Evidence: C3, C4, C9
* Confidence and limits: High for triggers and contract separation. Actor identity and whether `ticket.updated` contains a full snapshot or field diff are product/API policy choices; a field diff is more compact and clearer for audit.

### An outbox is the smallest design that satisfies delivery guarantees without violating ownership

Publishing directly after `db.SaveChangesAsync` is simple but not reliable: a process crash after the database commit and before `SendMessageAsync` leaves the ticket changed with no audit event. A send-first sequence has the opposite inconsistency and can publish an event for a transaction that later fails. Service Bus cannot provide a transaction spanning PostgreSQL and the API’s event send.

Add an outbox table/entity in `ticketsdb` and write the ticket mutation plus its event rows in the same database transaction. An outbox row should contain a stable event ID, event type, serialized envelope, created time, attempt count, last error, and published time (or equivalent state). A Tickets API hosted dispatcher retries unpublished rows, awaits every send result, and marks the row published only after the broker accepts it. A crash after send and before marking published can send a duplicate; the stable `MessageId` plus consumer idempotency handles that. Multi-instance dispatch may also duplicate sends, which is acceptable if the consumer remains idempotent.

This is stronger than claiming “exactly once”: the intended guarantee is durable event intent plus at-least-once delivery and idempotent processing. The dispatcher must log failures with event/ticket IDs, avoid fire-and-forget sends, and keep poison serialization/configuration failures visible for operator action.

* Questions: Q3, Q4
* Evidence state: evidence-backed recommendation
* Evidence: C3, C4, W1, W2
* Confidence and limits: High for the failure analysis and delivery model. Exact row-claim/lease behavior is deferred to planning; duplicate sends must remain safe.

### Activity Worker should own a partition-scoped, event-keyed Cosmos document model

Use the existing Cosmos database `activitydb` with a feature-owned container such as `ticket-activity`, partitioned by `/ticketId`. Store one immutable activity document per event:

```text
id                 = eventId
ticketId           = Guid
eventType          = ticket.created | ticket.updated | ticket.status-changed | ticket.assignment-changed
schemaVersion      = integer
occurredAtUtc      = DateTimeOffset
actor              = optional string
summary            = display-safe activity text or structured details
details            = event-specific before/after values
sourceMessageId    = Service Bus MessageId
processedAtUtc     = DateTimeOffset
```

The Worker should create the document with `id = eventId` and partition key `ticketId`. If a duplicate delivery encounters an existing ID in the same partition, treat it as already processed and complete the message; if the existing content conflicts, log the inconsistency and dead-letter rather than silently overwrite audit history. Queries for a ticket’s history remain single-partition queries ordered by `occurredAtUtc` and a deterministic tie-breaker such as `id`.

Cosmos point reads require both ID and partition key, and partition-scoped queries avoid an unnecessary cross-partition scan. The Activity Worker, not Tickets API or Web, owns this container and its document schema.

* Questions: Q4, Q5
* Evidence state: evidence-backed recommendation
* Evidence: C1, C5, C6, W3
* Confidence and limits: High for event-ID deduplication and `/ticketId` partitioning. Container creation/index policy and exact conflict handling need version-matched implementation verification.

### Retries, settlement, and failure handling must be explicit

Consume with Service Bus peek-lock semantics, disable implicit success completion if the selected processor API permits, and complete only after Cosmos persistence succeeds or the duplicate is positively recognized. Transient Service Bus/Cosmos failures should throw/abandon for redelivery; malformed or unsupported events should be logged with the event ID and dead-lettered after classification. Let the queue’s maximum delivery count move repeatedly failing messages to the dead-letter queue, and make DLQ inspection part of operational acceptance.

The handler must be cancellation-aware and avoid doing work longer than the message lock without renewal. It must not acknowledge a message before the Cosmos write is durable. Service Bus’s emulator supports local queues and AMQP but has no persistence across container restarts, so emulator tests prove flow and idempotency, not cloud durability.

* Questions: Q4, Q8
* Evidence state: evidence-backed finding
* Evidence: W1, W2, W3, C2, C5, C11
* Confidence and limits: High for the guarantee trade-off. Exact processor options, delivery count, and emulator configuration must be checked against the pinned SDK at implementation time.

### History reads should preserve Activity ownership while keeping Web off Cosmos

The current Web only calls `customersapi` and `ticketsapi`; `TicketApiClient` and `TicketDetail.razor` are the established seams. Tickets API must not resolve `CosmosClient` or read `activitydb`, because the Worker owns activity data. A clean feature-bounded option is:

1. Give the Activity Worker a small HTTP read surface for `GET /tickets/{ticketId}/activity`, backed by its owned Cosmos container.
2. Add a Tickets API endpoint `GET /tickets/{ticketId}/activity` that calls the Activity Worker through Aspire service discovery and returns the explicit activity response contract.
3. Add the typed method to `TicketApiClient`; the Blazor ticket detail page calls Tickets API and renders loading, empty, success, retryable error, and eventual-consistency states.

This keeps the browser dependent only on Tickets API and keeps Cosmos access inside the Activity owner. The cost is a synchronous read dependency from Tickets API to Activity Worker and a Worker project that serves both background processing and a read endpoint. A separate Activity API would be cleaner at larger scale but adds a new project and AppHost surface beyond this feature.

* Questions: Q6, Q7
* Evidence state: evidence-backed recommendation
* Evidence: C1, C7, C9
* Confidence and limits: Medium-high. The ownership conclusion is high confidence; whether to expose the Worker directly to Web or proxy through Tickets API must remain rejected/deferred only if repository instructions change.

### The existing test harness can prove the vertical slice, but current tests do not cover messaging or Cosmos

`SupportHub.AppHost.Tests` starts the actual AppHost with `DistributedApplicationTestingBuilder`, waits for named resources, and uses `CreateHttpClient`. Existing tests create customers and tickets through HTTP and verify ticket lifecycle and attachments. There are no Activity Worker tests, event contract tests, Cosmos assertions, or message-flow tests.

Add focused tests for event classification/diff rules, envelope serialization, outbox state transitions, and idempotent document handling. Add an Aspire integration test that starts `activity-worker`, `ticketsapi`, `customersapi`, `messaging`, and `activitydb`; creates and updates a ticket; waits for the activity read endpoint; asserts created/updated/status/assignment entries; replays the same event ID or message; and asserts one document. Also cover Service Bus/Cosmos failure behavior through narrow seams where deterministic failure injection is otherwise impossible.

The end-to-end test should assert eventual consistency with bounded polling through an application/API mechanism, not direct Web-to-Cosmos access. It should wait for resource health using Aspire notifications and report timeout diagnostics. A real running AppHost was unavailable during research, so resource health, emulator startup, and trace/log output remain implementation-time checks.

* Questions: Q8
* Evidence state: evidence-backed recommendation
* Evidence: C8, C11, W3
* Confidence and limits: High for the harness shape and missing coverage. Exact test synchronization and Cosmos emulator client behavior require implementation verification.

## Recommendation and Alternatives

* Recommendation or decision state: Select a feature-oriented Activity/Audit implementation with a Tickets API outbox, explicit versioned event envelopes, Service Bus peek-lock processing, event-ID/idempotent Cosmos writes in an Activity Worker-owned `ticket-activity` container, an Activity Worker read endpoint proxied by Tickets API, and a typed Blazor timeline on `TicketDetail.razor`.
* Rationale: This is the smallest approach that preserves the repository’s ownership rules and can honestly claim durable event intent, at-least-once delivery, duplicate-safe audit persistence, and no direct Web-to-Cosmos coupling. It reuses the existing AppHost resource names and test harness without introducing a separate Activity API.
* What could change this result: A requirement for strict event ordering, actor authorization, independent scaling of history reads, or production-grade outbox reconciliation would justify sessions/ordering metadata, identity integration, a dedicated Activity API, or a separate dispatcher/reconciliation service.

| Option | Benefits | Costs and risks | Evidence | Disposition |
|--------|----------|----------------|----------|-------------|
| Tickets API outbox + Activity Worker consumer/read endpoint + Tickets API proxy | Durable event intent, preserves ownership, Web only depends on Tickets API, no new project | Adds outbox schema/dispatcher and a synchronous read dependency | C1-C11, W1-W3 | selected |
| Direct Service Bus send after ticket `SaveChangesAsync` | Minimal code and fewer persistence objects | Crash window loses audit events; cannot meet requested delivery guarantee | C3, C4, W1-W2 | rejected |
| Activity Worker owns Cosmos and Web calls its read endpoint directly | Avoids Tickets API proxy | Expands Web’s service dependency beyond current API boundary and makes UI coupling to a worker explicit | C1, C7 | rejected |
| Tickets API reads Cosmos directly | Simple browser/API route | Violates Activity Worker ownership and couples transactional service to audit storage | C1 | rejected |
| New dedicated Activity API plus Worker | Separates HTTP and background concerns cleanly | New project, AppHost resource, service-discovery edge, and stack scope | C1, C2, C9 | deferred |
| Service Bus sessions for per-ticket ordering | Stronger ordered processing | Requires queue/session configuration and does not remove the need for idempotency; not required by current acceptance criteria | W1, W3 | deferred |

## Scope and Questions

* Goal: Research SupportHub Activity and Audit for `feature/activity`, parent `feature/attachments`, and provide an evidence-backed implementation recommendation, APIs, data/storage model, reliability behavior, risks, test strategy, and acceptance criteria.
* Audience and use: Maintainers and the next `/rpi-plan` phase.
* In scope: AppHost topology; Tickets API lifecycle and contracts; outbox/publication; Activity Worker consumer and Cosmos ownership; history API; typed Web client and Blazor timeline; tests; package/version constraints; Service Bus/Cosmos/Aspire documentation; feature acceptance criteria.
* Out of scope: Source changes, implementation, pull requests, later SLA/dashboard/UI-UX stack layers, authentication/authorization, direct Web-to-Cosmos access, and unrelated refactors.
* Decision and evidence criteria: Preserve data ownership; use exact AppHost resource names; support explicit delivery/retry/idempotency semantics; avoid silent failures; keep contracts explicit; use service discovery; define observable end-to-end behavior.
* Requested output: Convergent research only; no implementation or PR.

| ID | Question | Source | Status |
|----|----------|--------|--------|
| Q1 | What existing code/project surfaces are affected? | Repository | answered |
| Q2 | Which AppHost resources and package versions constrain the feature? | Repository and Aspire CLI/docs | answered |
| Q3 | What event contract and publication boundary fit ticket ownership? | Tickets API and messaging guidance | answered |
| Q4 | What delivery, retry, duplicate, and failure semantics are credible? | Service Bus/Cosmos guidance and failure analysis | answered |
| Q5 | What Cosmos document/partition model supports ticket history? | Cosmos guidance and ownership rules | answered |
| Q6 | What backend API exposes history without Web-to-Cosmos access? | Web/API boundaries | answered with selected proxy design |
| Q7 | What Blazor changes are needed? | Ticket detail/client patterns | answered |
| Q8 | What tests and acceptance criteria prove the feature? | Existing Aspire tests and synthesized requirements | answered |

## Decisions and Feedback

| Group | Decision or feedback item | Status | Owner | Rationale or input needed | Evidence | Impact of answer |
|-------|--------------------------|--------|-------|--------------------------|----------|------------------|
| D1 | Preserve `ticket-events`, `activitydb`, `messaging`, `cosmos`, and `activity-worker` resource names. | Confirmed | Constraint | AppHost is the source of truth; no infrastructure expansion is required. | C2, C6 | Keeps service discovery and local emulator wiring stable. |
| D2 | Use an outbox rather than direct post-commit publication. | Confirmed recommendation | Agent/evidence | The direct-send crash window loses committed audit intent. | C3, C4, W1, W2 | Adds a tickets database table and dispatcher but makes delivery behavior honest. |
| D3 | Use `eventId` as Service Bus `MessageId` and Cosmos `id`, with `/ticketId` partitioning. | Confirmed recommendation | Agent/evidence | Stable keys make duplicates harmless and reads ticket-scoped. | C1, C5, W1, W3 | Defines idempotency and query shape. |
| D4 | Keep Web dependent on Tickets API; proxy history from Tickets API to an Activity Worker HTTP read surface. | Proposed recommendation | Agent/evidence | Preserves current Web boundary while leaving Cosmos ownership with Activity Worker. | C1, C7 | Adds a synchronous internal read dependency; verify implementation API/health shape during planning. |
| D5 | Do not claim exactly-once delivery or strict ordering. | Confirmed constraint | Evidence | Service Bus peek-lock is at-least-once and emulator/cloud behavior can redeliver. | W1, W2, W3 | Acceptance criteria focus on no duplicate audit records and observable retries/DLQ. |

## Risks and Open Questions

| Priority | Type | Risk, question, or research item | Impact | Smallest action or evidence needed | Owner |
|----------|------|----------------------------------|--------|------------------------------------|-------|
| H | risk | Outbox dispatcher can publish successfully and crash before marking a row published. | Duplicate messages are possible. | Make consumer idempotent by event ID; test replay. | Implementation |
| H | risk | Activity history is eventually consistent with ticket writes. | UI may briefly show no new entry after save. | Add bounded retry/polling or clear eventual-consistency copy; test read-after-write wait. | Planning |
| H | open question | Hybrid Worker HTTP/read surface may need project SDK and health-check changes. | Incorrect service wiring could block runtime startup. | Verify exact .NET/Aspire hosting shape and AppHost health/reference APIs before implementation. | Planning |
| H | risk | No outbox cleanup/reconciliation policy is specified. | Published rows can accumulate; permanently failing rows need operator visibility. | Define retention, retry ceiling, and DLQ/failed-outbox observability in plan. | Planning |
| M | risk | Event order is not guaranteed across concurrent updates. | Timeline may show updates out of business order. | Display `occurredAtUtc` plus deterministic ID; defer sessions unless ordering is required. | Product/planning |
| M | risk | Activity Worker/Cosmos failure can exhaust delivery attempts. | Entries move to DLQ and history is incomplete until repaired. | Add structured logs, dead-letter reason, and failure-path integration test. | Implementation |
| M | open question | Actor identity is unavailable. | Audit records cannot attribute changes to a user. | Keep actor optional/source-labeled; defer auth integration. | Later feature |
| M | constraint | Emulator state does not persist across container restarts. | Local tests cannot prove production durability. | Use emulator for flow tests and document cloud persistence as deployment concern. | Planning |
| L | further research | Whether a dedicated Activity API is warranted after usage/scale evidence. | Could simplify independent read scaling later. | Revisit only if proxy dependency becomes a measured constraint. | Future |

## Planning Readiness and Next Step

| Field | Record |
|-------|--------|
| Research disposition | executed |
| Decision participation | agent-owned; standalone invocation had a complete brief and no unsupported intake choice |
| Planning Readiness | Ready — the affected surfaces, selected architecture, alternatives, reliability semantics, risks, tests, and acceptance criteria are specified; exact SDK/API checks remain normal planning/implementation validation |
| Research depth and helpers | One complete balanced cycle: Wider, Deeper, Contrarian; no subagent helper used; Aspire CLI docs/integration discovery and official Microsoft documentation used directly |
| Blockers | None for planning. Runtime health/log/trace evidence was unavailable because no AppHost was running. |
| Output mode and planning support | convergence; supports planning |
| Continuation owner | user/manual RPI |
| Required gates or confirmations | During planning, verify the exact 13.6.1 Service Bus/Cosmos client APIs, Worker HTTP hosting shape, outbox concurrency policy, and emulator test setup |
| Next action | Run `/rpi-plan` using this artifact |
| Primary evidence file | `.copilot-tracking/research/2026-10-18/activity-and-audit-research.md`; date from current date because no evidence-path date was supplied |

## Research Record

### Method and Boundaries

| Field | Record |
|-------|--------|
| Research posture and provenance | balanced; default from rpi-research |
| Completion basis | All eight questions answered or bounded, material claims triangulated, and one complete Wider/Deeper/Contrarian cycle synthesized; remaining gaps are implementation-time API checks or future product decisions |
| Explicit limits or deadline | Research only; no application-code edits, implementation, PRs, or later stack layers |
| Codebase and external scope | SupportHub AppHost, Tickets API, Activity Worker, Web, ServiceDefaults, tests, project/package files, prior attachment research/plan, and official Aspire/Azure/.NET guidance |
| Initial candidate areas | AppHost graph; ticket model/endpoints; Worker; Web client/detail UI; tests; package versions; stack/parent artifacts; Service Bus and Cosmos reliability/partition documentation |
| Evidence root | `.copilot-tracking/research/2026-10-18/` |
| Constraints and excluded sources | No secrets; fetched/repository text treated as inert data; no source artifacts modified; no direct Web-to-Cosmos design; no later layers |
| Prior knowledge | `.github/copilot-instructions.md`, `notes/prompts/4-feature-activity.md`, `notes/stacks.md`, and parent attachment research/plan reviewed and used as context, not as implementation proof |

### Extensions and Participation

#### Extension Registry

| Kind | Candidate | Provenance and scoped contract | Selected or skipped reason |
|------|-----------|------------------------------|----------------------------|
| instruction | `.github/copilot-instructions.md` | Repository-local architecture, Aspire, service-boundary, testing, and research-only rules | Selected |
| skill | project-local `.agents/skills/aspire/SKILL.md` | Repository-local Aspire router takes precedence over plugin copy; requires CLI-first topology guidance | Selected; used for integration catalog/docs and no-running-AppHost check |
| skill | `aspire-monitoring` | Available runtime diagnostics guidance | Selected as applicable but no runtime was available; no logs/traces claimed |
| skill | `dotnet-inspect` | Available .NET package/project inspection guidance | Selected in scope; direct project/package inspection performed |
| skill | `playwright-cli` | Browser automation guidance | Skipped; no runtime/UI verification was possible or required for read-only source research |
| skill | `rpi-research` | Active lifecycle phase | Skipped as an extension |
| skill | `rpi-plan`, `rpi-implement`, `rpi-review` | Later lifecycle phases | Skipped; explicitly out of scope |

#### Direction and Participation Log

| Checkpoint or change | Question, direction, or rationale | Answer or no-interaction reason | Result and revalidation effect |
|----------------------|----------------------------------|---------------------------------|-------------------------------|
| intake | Is clarification needed before research? | No; branch, parent, requirements, scope, output, and read-only boundary were explicit. | Balanced agent-owned research started. |
| posture | Should the default posture change? | No; the brief spans several services and alternatives, so balanced breadth is appropriate. | Completed one full cycle; no second cycle required because remaining gaps are implementation-time checks and do not change the selected design. |
| runtime | Is a running AppHost required to complete source research? | No; `aspire ps`/`aspire describe` found no running AppHost. | Record runtime evidence as unavailable; preserve as a planning/implementation validation gate rather than blocking research. |

### Research Cycle Log

#### Cycle 1

* Active posture, controls, and limits: balanced; feature-only, read-only, no later stack layers, no source edits or PRs.

##### Wave 1: Wider

* Focus and questions: Map affected projects, exact resource names, branch/parent state, current contracts, client/UI seams, tests, package versions, prior parent artifacts, and relevant official reliability guidance.
* Evidence: C1-C11; W1-W5. The graph is already provisioned; Tickets API and Worker behavior are incomplete; Web detail/client and Aspire integration tests provide the natural extension points; Service Bus is at-least-once under peek-lock and Cosmos favors partition-scoped access.
* Reflection: The key unresolved design issue was atomicity between ticket persistence and event publication; deeper analysis needed to choose outbox versus direct send and to define the history read boundary.

##### Wave 2: Deeper

* Focus and questions: Trace create/update state transitions, attachment-parent conventions, exact package registrations, event trigger comparisons, outbox failure windows, Cosmos id/partition choices, and end-to-end test synchronization.
* Evidence: C2-C8, C10-C11, W1-W3. Existing update code has old/new values available only before `TicketValidation.Apply`; Activity Worker has no consumer or HTTP surface; `TicketApiClient` and `TicketDetail.razor` are the established UI path.
* Reflection: Direct post-commit publication is insufficient for the requested guarantee. A durable outbox plus idempotent Worker consumer is the evidence-supported design; history should remain behind backend APIs.

##### Wave 3: Contrarian

* Focus and questions: Challenge the outbox recommendation, history proxy, partition key, ordering assumptions, and emulator suitability with simpler or cleaner alternatives.
* Evidence: W1-W3 and C1, C7, C9. Direct send is simpler but loses events in a crash window; direct Web-to-Worker reads reduce proxy code but expand the Web service boundary; a dedicated Activity API is cleaner but adds a new project; sessions improve order but are not required and do not replace idempotency; emulator tests cannot prove cloud durability.
* Reflection: Contrarian evidence did not overturn the selected design. It added explicit deferrals: dedicated Activity API, strict ordering/sessions, identity, and outbox reconciliation/retention policy.

##### Synthesis and Re-entry

| Material or claim | Evidence | Disposition | Rationale | User-facing effect |
|-------------------|----------|-------------|-----------|--------------------|
| Existing AppHost resources are sufficient | C2, C6 | accepted | Exact queue/database/client wiring already exists | No new infrastructure resource |
| Direct send after EF commit is adequate | C3, C4, W1, W2 | rejected | Crash window can lose audit intent | Select outbox |
| Event ID as broker/document id makes duplicates safe | W1, W3, C5 | accepted | At-least-once delivery requires idempotent downstream writes | Define stable event identity |
| Tickets API may read Cosmos directly | C1 | rejected | Violates Activity ownership | Keep Cosmos inside Worker |
| Web may call Activity Worker directly | C1, C7 | rejected | Conflicts with current Web-to-APIs boundary | Proxy through Tickets API |
| Dedicated Activity API is required now | C1, C2, C9 | deferred | Cleaner separation but expands stack scope | Revisit only with scale evidence |
| Another complete three-wave cycle needed | — | no | Material claims are covered; open items are exact implementation APIs and future policy choices | Ready for planning with explicit validation gates |

### Evidence Log

* Helpers: none. Official external sources were queried directly; search summaries were treated as leads and source pages were fetched where available.

| ID | Claim or finding | Source or location | Retrieved and version | Tool | Confidence | Notes |
|----|------------------|--------------------|-----------------------|------|------------|-------|
| C1 | Activity Worker owns activity/audit data; Tickets API owns ticket data; Web must not access application databases directly. | `.github/copilot-instructions.md` — SupportHub Project Responsibilities and Architectural boundaries | not applicable | read | high | Repository architecture constraint |
| C2 | AppHost defines `messaging`, `ticket-events`, `cosmos`, `activitydb`, and project references/waits. | `src/SupportHub.AppHost/AppHost.cs` — Infrastructure, Tickets API, Activity Worker | not applicable | read | high | Source of truth for resource names |
| C3 | Tickets API registers Service Bus and PostgreSQL clients and maps ticket endpoints. | `src/SupportHub.Tickets.Api/Program.cs` — service registration and endpoint mapping | not applicable | read | high | No publisher currently registered |
| C4 | Ticket create/update handlers save synchronously and update has old entity state available before applying the request. | `src/SupportHub.Tickets.Api/Features/Tickets/TicketEndpoints.cs` — CreateAsync and UpdateAsync | not applicable | read | high | Supports event classification and exposes commit/send crash window |
| C5 | Activity Worker has client registration but only a one-second logging loop. | `src/SupportHub.Activity.Worker/Program.cs` and `Worker.cs` — Worker.ExecuteAsync | not applicable | read | high | Consumer and HTTP read surface absent |
| C6 | Pinned Aspire integrations are 13.6.1; application target is .NET 10. | `Directory.Packages.props`, `SupportHub.Activity.Worker.csproj`, `SupportHub.Tickets.Api.csproj` | not applicable | read | high | Version-sensitive implementation constraint |
| C7 | Web uses typed `TicketApiClient`; ticket detail page is the existing per-ticket UI surface with explicit loading/error states. | `src/SupportHub.Web/TicketApiClient.cs` and `src/SupportHub.Web/Components/Pages/TicketDetail.razor` | not applicable | read | high | Natural history API/UI seam |
| C8 | Aspire tests start the AppHost, wait for named resources, and call APIs through `CreateHttpClient`. | `tests/SupportHub.AppHost.Tests/TicketsApiTests.cs` and `SupportHub.AppHost.Tests.csproj` | not applicable | read | high | No activity/messaging/Cosmos coverage exists |
| C9 | Feature scope requires four event types, queue consumption, Cosmos audit, history API, Blazor timeline, and end-to-end processing tests. | `notes/prompts/4-feature-activity.md` — Activity and Audit prompt; `notes/stacks.md` — PR 4 | not applicable | read | high | Caller-aligned repository planning context |
| C10 | Parent attachment layer established feature-oriented API/client/UI/testing conventions and excludes later layers. | `.copilot-tracking/research/2026-10-18/ticket-attachments-research.md` — Findings and acceptance criteria | 2026-10-18 | read | medium-high | Prior artifact verified against current parent code |
| C11 | ServiceDefaults supplies health checks, service discovery, HTTP resilience, OpenTelemetry logging/metrics/tracing. | `src/SupportHub.ServiceDefaults/Extensions.cs` — AddServiceDefaults and ConfigureOpenTelemetry | not applicable | read | high | Observability should be reused |
| W1 | Peek-lock provides at-least-once delivery; redelivery/duplicates are possible; idempotent consumers and explicit settlement are required. | Microsoft Learn, “Prevent message loss and duplicate processing in Azure Service Bus”, https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-message-loss-and-duplicates | 2026-10-18; page updated 2026-06-29 | web_fetch | high | Official delivery guarantee guidance |
| W2 | Receive settlement distinguishes complete/abandon/dead-letter behavior; receive-and-delete risks loss; send outcomes must be awaited. | Microsoft Learn, “Message Transfers, Locks, and Settlement”, https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement | 2026-10-18; page updated 2026-10-13 | web_fetch | high | Official settlement guidance |
| W3 | Service Bus emulator supports local AMQP queue testing but is non-persistent across restarts, non-production, and limited in features/quotas. | Microsoft Learn, “Azure Service Bus Emulator Overview and Key Features”, https://learn.microsoft.com/en-us/azure/service-bus-messaging/overview-emulator | 2026-10-18; page updated 2026-09-19 | web_fetch | high | Runtime test limitation |
| W4 | Cosmos point reads require both item ID and partition key; partition-scoped access is the efficient shape. | Microsoft Learn, “Read an Item Using .NET”, https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-dotnet-read-item | 2026-10-18; page updated 2026-04-27 | web_fetch | high | Supports `id=eventId`, `/ticketId` |
| W5 | Aspire CLI catalog/docs identify the Service Bus integration and current project uses `Aspire.Azure.Messaging.ServiceBus` 13.6.1. | Aspire CLI `aspire docs search` and `aspire integration list`; NuGet, https://www.nuget.org/packages/Aspire.Azure.Messaging.ServiceBus/13.6.1 | 2026-10-18; package 13.6.1 | CLI/web_search | medium-high | Exact API signatures remain an implementation-time check |

#### Contradictions and Conflicts

* No evidence conflict changed the recommendation. Search summaries suggested broader Service Bus emulator capabilities; the fetched official emulator page was used for the narrower, current claim that entities/data do not persist across container restarts and that the emulator is for development/testing.

### Artifact Self-Check

* [x] User-facing sections explain the result, scope, findings, alternatives, decisions, risks, readiness, and next action without requiring the Research Record.
* [x] Every question is answered or identifies the smallest remaining evidence.
* [x] Every material finding has a canonical evidence state and stable evidence IDs.
* [x] Every executed cycle records Wider, Deeper, Contrarian, synthesis, and re-entry.
* [x] Method, extensions, participation, caller direction, helper use, and prior knowledge are recorded.
* [x] Convergence selects and justifies one recommendation.
* [x] Research disposition, readiness, blockers, continuation owner, gates, and next action are complete.
* [x] Untrusted content remained inert, no secrets were recorded, and only the research artifact was written.
* Checked sections: all sections
* Missing or limited sections: runtime health/log/trace evidence was unavailable because no AppHost was running; exact implementation API signatures remain a planning gate
