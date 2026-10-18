<!-- markdownlint-disable-file -->
# RPI Changes: SupportHub Ticket Management Restart Persistence

## Metadata

* Task ID: `ticket-management-restart-persistence`
* Related plan: [`.copilot-tracking/plans/2026-10-18/ticket-management-restart-persistence-plan.md](../../plans/2026-10-18/ticket-management-restart-persistence-plan.md)
* Implementation date: 2026-10-18

## Execution Status

* Status: Complete
* Declared invocation scope: full plan
* Completed scope markers: `P01`, `P01-T01`, `P01-T02`, `P02`, `P02-T01`, `P02-T02`
* All remaining active-plan markers: none
* Status basis: The persistent test mode, serialized cleanup boundary, two-lifecycle persistence test, full solution validation, and Aspire runtime verification are complete.

## Execution Summary

Implemented the approved RV-001 follow-up by adding an opt-in named PostgreSQL volume path, a stable per-run test credential, serialized cleanup, and an Aspire integration test that proves a ticket survives two independently built AppHost lifecycles.

## Completed Work

### Establishing opt-in persistent PostgreSQL test mode

* Related phase or task: `P01-T01`
* Files:
  * [src/SupportHub.AppHost/AppHost.cs](../../../src/SupportHub.AppHost/AppHost.cs)
* Behavior or functionality changed: Persistence testing can opt in with `SUPPORTHUB_PERSISTENCE_TEST_MODE`, a caller-provided named volume, and the stable Aspire parameter `supporthub-persistence-test-postgres-password`; ordinary test mode continues to avoid data volumes.
* Validation: Passed; AppHost integration test project builds with zero errors.

### Defining serialized, scoped persistent-resource cleanup

* Related phase or task: `P01-T02`
* Files:
  * [tests/SupportHub.AppHost.Tests/PersistentResourceCollection.cs](../../../tests/SupportHub.AppHost.Tests/PersistentResourceCollection.cs)
  * [tests/SupportHub.AppHost.Tests/TicketPersistenceTests.cs](../../../tests/SupportHub.AppHost.Tests/TicketPersistenceTests.cs)
* Behavior or functionality changed: The persistence test runs in the non-parallel `SupportHub persistent resources` xUnit collection and removes only its generated PostgreSQL volume through an argument-list-based Docker command in the success and failure path.
* Validation: Passed; the focused persistence test completed and exact-volume cleanup succeeded.

### Proving ticket survival across independent AppHost lifecycles

* Related phase or task: `P02-T01`
* Files:
  * [tests/SupportHub.AppHost.Tests/TicketPersistenceTests.cs](../../../tests/SupportHub.AppHost.Tests/TicketPersistenceTests.cs)
* Behavior or functionality changed: The test creates a customer and ticket through service-discovered APIs in one AppHost lifecycle, disposes it, starts a second independently built lifecycle, and verifies the same ticket fields and timestamps by ID.
* Validation: Passed; `dotnet test tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj --no-build --filter FullyQualifiedName~TicketPersistenceTests.Ticket_SurvivesSeparateAppHostLifecycles`.

### Validating persistence evidence and runtime behavior

* Related phase or task: `P02-T02`
* Files:
  * [src/SupportHub.AppHost/AppHost.cs](../../../src/SupportHub.AppHost/AppHost.cs)
  * [tests/SupportHub.AppHost.Tests/TicketPersistenceTests.cs](../../../tests/SupportHub.AppHost.Tests/TicketPersistenceTests.cs)
  * [tests/SupportHub.AppHost.Tests/PersistentResourceCollection.cs](../../../tests/SupportHub.AppHost.Tests/PersistentResourceCollection.cs)
* Behavior or functionality changed: The approved follow-up is fully validated without changing ordinary ephemeral test behavior. The final fixture also disposes a partially started distributed application when startup or health waiting fails.
* Validation: Passed. The focused persistence test passed; the full AppHost integration test project passed 6 tests; `dotnet build SupportHub.slnx --no-restore` passed with 0 errors; `dotnet test SupportHub.slnx --no-build --no-restore` passed 16 tests; isolated Aspire start, `aspire wait customersapi`, `aspire wait ticketsapi`, `aspire describe`, Tickets API logs, structured logs, PostgreSQL logs, and exact AppHost stop all completed successfully. The only warnings were the existing `MessagePack` NU1902 advisory.

## Implementation-Time Plan Updates

None.

## Business Alignment

* None

## Validation Record

| Check | Scope | Status | Evidence or reason |
|---|---|---|---|
| Focused persistence test | `P02-T01` | Passed | One test passed; both AppHost lifecycles became healthy and the ticket was read after restart; exact-volume cleanup succeeded. |
| AppHost integration test project | `P02-T02` | Passed | 6 tests passed after the final cleanup-path fix; 2 existing `MessagePack` NU1902 warnings. |
| Solution build | `P02-T02` | Passed | `dotnet build SupportHub.slnx --no-restore`; 0 errors and 2 existing `MessagePack` NU1902 warnings. |
| Solution test suite | `P02-T02` | Passed | `dotnet test SupportHub.slnx --no-build --no-restore`; 16 tests passed. |
| Aspire runtime verification | `P02-T02` | Passed | Isolated AppHost started; `customersapi` and `ticketsapi` became healthy; migrations reported databases up to date; logs and structured logs were inspected; exact AppHost stop succeeded. |

## Pre-Review Reconciliation

* Plan markers and task-local context: All full-plan markers are checked.
* Completed-work entries and handoff prose: Entries cover the AppHost mode, cleanup boundary, two-lifecycle test, and validation evidence.
* Validation, blockers, remaining work, and follow-up items: Required focused, solution, and runtime checks passed; no blockers or follow-up items remain.
* Review readiness: Ready for optional review.

## Blockers

* None currently.

## Remaining Work

* None.

## Follow-Up Items

* Canonical plan list: [`.copilot-tracking/plans/2026-10-18/ticket-management-restart-persistence-plan.md`](../../plans/2026-10-18/ticket-management-restart-persistence-plan.md), `## Follow-Up Items`
* None.

## Return-to-Caller State

* Implementation execution status: Complete
* Declared scope and markers: Full plan; all markers complete.
* Validation coverage: Focused persistence test, full AppHost tests, full solution build/tests, and isolated Aspire runtime checks passed.
* Blockers: None.
* Current plan updates: Final implementation and validation state reconciled.
* Business alignment entries: None; no BRD, PRD, or ADR is in scope.
* Planning and critique state: Plan ready; follow-up critique passed.
* Follow-up items: None.
* Review readiness or no-handoff reason: Ready for optional `/rpi-review`.
* Continuation owner: User/manual RPI Agent.
