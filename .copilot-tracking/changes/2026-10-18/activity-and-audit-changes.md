<!-- markdownlint-disable-file -->
# RPI Changes: SupportHub Activity and Audit

## Metadata

* Task ID: `supporthub-activity-and-audit`
* Related plan: [`.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md`](../../plans/2026-10-18/activity-and-audit-plan.md)
* Implementation date: 2026-10-18

## Execution Status

* Status: `Complete`
* Declared invocation scope: `full_plan`
* Completed scope markers: `P01`-`P06` and all `Pxx-Txx` tasks
* All remaining active-plan markers: `none`
* Status basis: The approved full plan is implemented and validated through build, unit, distributed, runtime, and browser checks.

## Execution Summary

Implemented the approved outbox, Service Bus, Activity Worker, Cosmos, backend proxy, Blazor timeline, and Aspire-validation sequence. Delivery is at-least-once with stable event identity and conflict-safe Cosmos persistence; the UI reads activity only through Tickets API.

## Completed Work

### `P01-T01` — Versioned activity event contracts

Added the shared contracts-only project `src/SupportHub.Activity.Contracts` with lower-camel-case envelopes, schema version `1`, typed ticket snapshot/change details, and the four required event types. `ActivityEventFactory` emits create, update, status, and assignment events with explicit null transitions.

### `P01-T02` and `P01-T03` — Transactional outbox

Added `OutboxEvent` persistence, EF configuration, migration, and atomic ticket mutation enlistment. Create and update transactions now commit the ticket and serialized event payload together; update events preserve before/after values before the tracked entity is changed.

### `P02-T01` through `P02-T03` — Publication, recovery, and retention

Added the Tickets API hosted dispatcher targeting `ticket-events`, setting `MessageId`, `Subject`, `ContentType`, and `CorrelationId`. Pending/in-progress/published/terminal states, bounded retry backoff, claim recovery, published retention cleanup, terminal enumeration, and identity-preserving replay are implemented.

### `P03-T01` through `P03-T03` — Activity Worker and Cosmos read model

Converted the Worker to a hybrid HTTP/background host, created the `ticket-activity` Cosmos container in `activitydb`, and implemented peek-lock message processing. Invalid events are dead-lettered, transient persistence failures are abandoned, and duplicate equivalent event IDs complete successfully without creating another document. The Cosmos document uses explicit lower-case JSON field mappings required by the Linux emulator.

### `P04-T00` through `P04-T02` — Backend history boundary

Validated hybrid hosting with Aspire 13.6.1, exposed the Worker history read endpoint, and added the Tickets API proxy with service discovery, ticket existence checks, and explicit dependency/contract failure mapping. AppHost wiring uses the exact `activity-worker` resource and HTTP health endpoint.

### `P05-T01` and `P05-T02` — Typed client and timeline

Added activity DTOs and retrieval to `TicketApiClient`. Ticket details now render loading, empty, error, and populated activity states without direct Cosmos access.

### `P06-T01` through `P06-T03` — Validation evidence

Added serialized event-contract tests and an Aspire integration test covering create/update/status/assignment events plus replayed duplicate delivery. Full build and targeted tests passed. Aspire runtime verification showed healthy `ticketsapi`, `activity-worker`, `messaging`, and `cosmos` resources, successful event-to-history processing, and correlated publisher/consumer logs. Playwright rendered the populated Activity history timeline on Ticket details.

## Implementation-Time Plan Updates

* `P04-T00` resolved in favor of upgrading the existing Worker project to `Microsoft.NET.Sdk.Web` and retaining its hosted consumer; no new deployed service was introduced.
* The Linux Cosmos emulator required explicit Newtonsoft JSON property names (`id`, `ticketId`, etc.) and string-normalized persisted values. This is an implementation compatibility detail, not a change to the wire contract.

## Business Alignment

* None

## Validation Record

| Check | Scope | Status | Evidence or reason |
|-------|-------|--------|--------------------|
| Plan/critique readiness review | Full plan | Passed | Approved plan is Ready; final standard critique passed with no open findings. |
| Solution build | Full plan | Passed | `rtk dotnet build SupportHub.slnx --no-restore` — 0 errors. |
| Tickets API unit tests | `P01`, `P06-T01` | Passed | `rtk dotnet test tests/SupportHub.Tickets.Api.Tests/SupportHub.Tickets.Api.Tests.csproj --no-restore` — 30 passed. |
| Customers API regression tests | Regression | Passed | `rtk dotnet test tests/SupportHub.Customers.Api.Tests/SupportHub.Customers.Api.Tests.csproj --no-restore` — 5 passed. |
| Aspire event/history integration | `P06-T02` | Passed | Targeted `TicketsApiTests.TicketChanges_AreAvailableThroughActivityHistory` — passed, including duplicate replay count. |
| Aspire runtime health/logs | `P06-T03` | Passed | Healthy `ticketsapi`, `activity-worker`, `messaging`, and `cosmos`; publisher send, consumer processing, and Cosmos-backed history observed. |
| Browser timeline | `P05-T02`, `P06-T03` | Passed | Playwright snapshot shows populated `Activity history` region with `created` entry. |
| Full solution test suite | Full plan | Incomplete | `rtk dotnet test SupportHub.slnx --no-restore` exceeded the 480-second observation window without output and was stopped; targeted suites and the Activity integration test passed. |

## Pre-Review Reconciliation

* Plan markers and task-local context: `P01`-`P06` and all task markers are complete.
* Completed-work entries and handoff prose: All implementation entries above have completion evidence.
* Validation, blockers, remaining work, and follow-up items: Required validation passed; no active blocker remains.
* Review readiness: `ready_for_review`.

## Blockers

* None.

## Remaining Work

* None within the approved full-plan scope.

## Follow-Up Items

* Canonical plan list: [`.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md`](../../plans/2026-10-18/activity-and-audit-plan.md), `## Follow-Up Items`
* None outside the active plan.

## Return-to-Caller State

* Implementation execution status: `Complete`
* Declared scope and markers: Full plan; `P01`-`P06` complete.
* Validation coverage: Full solution build, Tickets API unit tests, Aspire integration, runtime health/logs, and browser timeline.
* Blockers: None.
* Current plan updates: All phase/task markers completed; hosting and Cosmos emulator compatibility discoveries recorded.
* Business alignment entries: None; no BRD, PRD, or ADR applies.
* Planning and critique state: Plan Ready; final critique passed; implementation complete.
* Follow-up items: None.
* Review readiness or no-handoff reason: Ready for `/rpi-review`.
* Continuation owner: User/standalone RPI.
