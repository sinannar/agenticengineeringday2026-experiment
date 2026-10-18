<!-- markdownlint-disable-file -->
# RPI Changes: SupportHub Activity and Audit

## Metadata

* Task ID: `supporthub-activity-and-audit`
* Related plan: [`.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md`](../../plans/2026-10-18/activity-and-audit-plan.md)
* Implementation date: 2026-10-18

## Execution Status

* Status: `Partial`
* Declared invocation scope: `RV-001`, `RV-002`, `RV-003`
* Completed scope markers: `RV-001`, `RV-002`, and the implementable portions of `RV-003`; prior full-plan implementation remains complete.
* All remaining active-plan markers: `RV-003` transient dependency-redelivery validation remains open.
* Status basis: Operational access and duplicate-equivalence defects are fixed and validated; malformed/unsupported-event coverage and the full solution suite pass, but emulator-level transient redelivery remains unverified.

## Execution Summary

The original Activity and Audit implementation is complete. This invocation remediates the accepted review findings by protecting operational outbox routes, enforcing complete duplicate equivalence, and adding focused validation while retaining the existing service boundaries and at-least-once model.

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
* Review remediation updated `P02-T03` to require a configured internal maintenance key for outbox inspection/replay and updated `P03-T03` to require equivalence across every persisted immutable event field. The accepted `RV-003` follow-up is now limited to transient dependency-redelivery coverage because malformed/unsupported validation and full-suite evidence are complete.

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
| Full solution test suite (earlier attempt) | Full plan | Incomplete | An earlier concurrent/initial run exceeded the observation window without output and was stopped. |
| Remediation focused tests | `RV-001`-`RV-003` | Passed | `rtk dotnet test tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj --no-restore --filter "FullyQualifiedName~ActivityStoreTests\|FullyQualifiedName~ActivityEventValidatorTests\|FullyQualifiedName~TicketChanges_AreAvailableThroughActivityHistory"` — 4 passed. |
| Remediation full solution tests | `RV-003` | Passed | `rtk dotnet test SupportHub.slnx --no-restore` — 48 passed; existing MessagePack and redundant hosting-package warnings remain. |

## Review Remediation

### RV-001 — Protect operational outbox maintenance routes

* Related finding: `RV-001`
* Files:
  * [src/SupportHub.Tickets.Api/Features/Activity/OutboxMaintenanceService.cs](../../../src/SupportHub.Tickets.Api/Features/Activity/OutboxMaintenanceService.cs)
  * [src/SupportHub.AppHost/AppHost.cs](../../../src/SupportHub.AppHost/AppHost.cs)
  * [tests/SupportHub.AppHost.Tests/TicketsApiTests.cs](../../../tests/SupportHub.AppHost.Tests/TicketsApiTests.cs)
* Behavior or functionality changed: Outbox terminal enumeration and replay now require a configured internal maintenance key; absent or incorrect credentials do not reveal route or event existence. Aspire test configuration supplies the key only to the Tickets API test resource, and the distributed test verifies denied and authorized replay behavior.
* Validation: Focused authorization and Activity flow tests passed; the full solution suite passed after remediation.

### RV-002 — Enforce complete duplicate-event equivalence

* Related finding: `RV-002`
* Files:
  * [src/SupportHub.Activity.Worker/ActivityStore.cs](../../../src/SupportHub.Activity.Worker/ActivityStore.cs)
  * [tests/SupportHub.AppHost.Tests/ActivityStoreTests.cs](../../../tests/SupportHub.AppHost.Tests/ActivityStoreTests.cs)
  * [tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj](../../../tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj)
* Behavior or functionality changed: Same-ID activity deliveries are equivalent only when all persisted immutable event fields match, including schema version and actor; inconsistent envelopes remain eligible for conflicting-event dead-letter handling.
* Validation: `ActivityStoreTests.EquivalentDocumentsRequireAllImmutableFieldsToMatch` passed; the distributed replay test also passed with denied and authorized access coverage.

### RV-003 — Validation follow-up

* Related finding: `RV-003`
* Behavior or functionality changed: The plan now carries only the accepted residual requirement for deterministic transient dependency-redelivery coverage; malformed/unsupported-event validation and complete-suite execution are implemented.
* Validation: `ActivityEventValidatorTests` passed for malformed and unsupported-schema payloads; the full solution suite passed. Transient dependency-redelivery remains open because reliable emulator failure injection is unavailable.

## Pre-Review Reconciliation

* Plan markers and task-local context: `P01`-`P06` and all task markers are complete.
* Completed-work entries and handoff prose: All implementation entries above have completion evidence.
* Validation, blockers, remaining work, and follow-up items: Remediation checks pass; only transient dependency-redelivery evidence remains open as an explicit emulator/test-harness follow-up.
* Review readiness: Remediation evidence is reconciled; no second review is required by the implementation workflow, though the existing review record remains the canonical assessment.

## Blockers

* None.

## Remaining Work

* None within the approved full-plan scope.

## Follow-Up Items

* Canonical plan list: [`.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md`](../../plans/2026-10-18/activity-and-audit-plan.md), `## Follow-Up Items`
* `RV-003`: Add deterministic transient dependency-redelivery coverage when a reliable emulator/test-harness failure-injection seam is available; implementation/test owner.

## Return-to-Caller State

* Implementation execution status: `Partial`
* Declared scope and markers: Review remediation `RV-001`-`RV-003`; `RV-001` and `RV-002` complete, `RV-003` complete for malformed/unsupported validation and full-suite execution with transient redelivery still open; prior `P01`-`P06` implementation remains complete.
* Validation coverage: Prior build, targeted tests, Aspire integration/runtime, and browser evidence remain valid; remediation focused tests passed (4), full solution tests passed (48), and transient dependency-redelivery remains unverified.
* Blockers: None.
* Current plan updates: `P02-T03` requires a maintenance key; `P03-T03` requires complete immutable-field equivalence; `RV-003` now records only the transient dependency-redelivery harness gap.
* Business alignment entries: None; no BRD, PRD, or ADR applies.
* Planning and critique state: Plan Ready; final critique passed; implementation complete.
* Follow-up items: `RV-003` transient dependency-redelivery validation.
* Review readiness or no-handoff reason: Ready for optional review follow-up; no second review is required by the implementation workflow. The residual transient-redelivery gap is explicitly recorded.
* Continuation owner: User/standalone RPI.
