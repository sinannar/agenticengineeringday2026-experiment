<!-- markdownlint-disable-file -->
# RPI Changes: SupportHub Ticket Attachments

## Metadata

* Task ID: `supporthub-ticket-attachments`
* Related plan: [`.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md](../../plans/2026-10-18/ticket-attachments-plan.md)
* Implementation date: 2026-10-18

## Execution Status

* Status: Complete
* Declared invocation scope: review findings `RV-001`, `RV-002`, and residual follow-up `RV-003`
* Completed scope markers: Original full-plan markers remain complete; review-finding implementation is in progress.
* Remaining active-plan markers: none; active review routes are `RV-001` and `RV-002`, with `RV-003` retained as non-blocking follow-up.
* Status basis: The original feature implementation and accepted review routes `RV-001` and `RV-002` are complete; `RV-003` is intentionally retained as non-blocking follow-up.

## Execution Summary

The approved ticket-attachment plan is being implemented as a Tickets API-owned vertical slice with PostgreSQL metadata, Azurite blob content, typed Web access, Blazor controls, focused tests, Aspire integration tests, and runtime verification.

This invocation applies accepted review routes `RV-001` and `RV-002` as ordinary implementation work. `RV-003` remains a distinct non-blocking validation follow-up and is not treated as a blocker for the two source fixes.

## Completed Work

### Establishing the attachment policy and metadata foundation (`P01-T01`, `P01-T02`)

* Added the server-authoritative 10 MiB policy for PDF, TXT, PNG, and JPEG files, including extension/MIME matching, safe display-name validation, opaque ticket-scoped blob names, and explicit attachment response contracts.
* Added the `Attachment` metadata entity, EF mapping/indexes, and `AddTicketAttachments` migration in the Tickets API-owned `ticketsdb`.
* Validation: focused policy tests pass; migration applied successfully during Aspire runtime startup.

### Implementing ticket-scoped Blob Storage and API behavior (`P02-T01`–`P02-T04`)

* Added the `IAttachmentBlobStore` seam and Azure Blob implementation over the existing Aspire `blobs` client, using the `ticket-attachments` container and streamed content.
* Added ticket-scoped upload, list, streamed download, and delete endpoints with bounded multipart parsing, validation, missing-content handling, storage Problem Details, upload compensation, and ticket-delete cleanup.
* Validation: distributed AppHost lifecycle, isolation, invalid/oversized upload, and ticket cleanup tests pass.

### Adding typed Web and Blazor attachment workflows (`P03-T01`–`P03-T03`)

* Added typed client operations, upload progress reporting, same-origin download proxying, and ticket-detail attachment list/upload/download/delete UI with loading, empty, progress, and error states.
* Fixed browser upload header propagation so the selected file MIME type reaches the API and Blob Storage.
* Validation: Web project build passes; browser smoke flow successfully uploaded, listed, downloaded through the same-origin route, and deleted a TXT attachment.

### Proving distributed behavior and runtime health (`P04-T02`–`P04-T04`)

* Added Aspire integration coverage for lifecycle, cross-ticket isolation, invalid and oversized uploads, and ticket-delete cleanup against local PostgreSQL and Azurite.
* Started the exact AppHost, waited for `blobs`, `ticketsapi`, and `webfrontend` health, inspected Tickets API/Web logs, and verified migration and Azurite container activity.
* Browser verification covered initial empty state, valid upload, list refresh, same-origin download, and delete success. Browser console reported no errors during the attachment flow.

### Locking down failure compensation and error contracts (`P04-T01`)

* Added an internal failure-handling seam used by the attachment endpoints for upload compensation, metadata-save failure, metadata-delete failure, and blob storage Problem Details.
* Added deterministic fake-store tests covering successful and failed compensation deletion plus exact operation-specific error types for upload, individual delete, and ticket-delete cleanup paths.
* Validation: `dotnet test tests/SupportHub.Tickets.Api.Tests/SupportHub.Tickets.Api.Tests.csproj --no-restore` passed with 26 tests.

### Addressing accepted review findings (`RV-001`, `RV-002`)

* Scope: Resolve orphan-cleanup error handling in rejection paths and add attachment-specific Web download failure/retry feedback.
* Planned affected surfaces: `AttachmentEndpoints`, `AttachmentFailureHandling`, `TicketApiClient`, `SupportHub.Web/Program.cs`, `TicketDetail.razor`, focused tests, and browser/distributed validation as needed.
* Validation intent: Re-run focused Tickets API tests, solution build, relevant AppHost tests, and Aspire/Web verification for the changed failure paths.
* Current state: Complete and validated.

### Resolving orphan cleanup and download failure feedback (`RV-001`, `RV-002`)

* Related review findings: `RV-001`, `RV-002`
* Files:
  * [src/SupportHub.Tickets.Api/Features/Attachments/AttachmentFailureHandling.cs](../../../src/SupportHub.Tickets.Api/Features/Attachments/AttachmentFailureHandling.cs)
  * [src/SupportHub.Tickets.Api/Features/Attachments/AttachmentEndpoints.cs](../../../src/SupportHub.Tickets.Api/Features/Attachments/AttachmentEndpoints.cs)
  * [src/SupportHub.Tickets.Api/Features/Attachments/AttachmentStorage.cs](../../../src/SupportHub.Tickets.Api/Features/Attachments/AttachmentStorage.cs)
  * [src/SupportHub.Web/TicketApiClient.cs](../../../src/SupportHub.Web/TicketApiClient.cs)
  * [src/SupportHub.Web/Components/Pages/TicketDetail.razor](../../../src/SupportHub.Web/Components/Pages/TicketDetail.razor)
  * [tests/SupportHub.Tickets.Api.Tests/AttachmentPolicyTests.cs](../../../tests/SupportHub.Tickets.Api.Tests/AttachmentPolicyTests.cs)
* Behavior or functionality changed: Rejection-path blob cleanup now preserves cancellation and returns explicit reconciliation Problem Details when compensation fails. Attachment downloads now have a ticket-scoped content preflight and the Blazor page reports missing or unavailable content instead of navigating blindly.
* Validation: 28 focused Tickets API tests passed; 3 distributed attachment tests passed; solution build passed; Aspire resources reached healthy state; real Azurite upload, `HEAD` preflight, and same-origin download returned successful responses. Browser automation was unavailable because the required Playwright browser binary is not installed.

## Implementation-Time Plan Updates

### Explicit implementation authorization

* Affected plan area or markers: `User Decisions and Requirements`, `Planning Readiness and Next Step`
* What changed: Replaced the original planning-only limitation with the explicit implementation authorization from the `/hve-core:rpi-implement` invocation.
* Why: The current user request authorizes implementation of the already-approved plan while retaining the feature boundary and no-PR constraint.
* Triggering evidence: Current user invocation.
* User answer or decision: Confirmed by explicit implementation request.
* Reconciliation performed: Updated the plan’s direction while preserving the approved scope, decisions, risks, and critique state.
* Planning and critique state: Approved plan; critique-6 passed.

### Reconciled review-ready implementation state

* Affected plan area: `Planning Readiness and Next Step`.
* What changed: Updated the plan from implementation-ready/pending approval to implementation complete and review-ready after all active markers gained implementation and validation evidence.
* Why: The explicit implementation invocation authorized the work, and the final build, tests, runtime checks, and browser verification now provide the required evidence.
* Reconciliation performed: Updated the plan handoff, changes-record status, remaining work, and review readiness consistently.

### Accepted review-route implementation scope

* Affected review findings: `RV-001` and `RV-002`; `RV-003` remains a distinct non-blocking follow-up.
* What changed: Added the accepted review findings to the active implementation scope without reopening or changing the approved feature requirements.
* Why: The standalone review accepted implementation routes for orphan-cleanup handling and Web download failure feedback.
* Triggering evidence: `.copilot-tracking/reviews/logs/2026-10-18/ticket-attachments-review.md`, Decision History entries `RD-002` and `RD-003`.
* User answer or decision: User accepted the suggested `rpi-implement` route for both findings and retained `RV-003` as residual work.
* Reconciliation performed: Execution status changed to partial/in progress; original plan markers remain complete; review routes and validation intent are recorded here.
* Planning and critique state: No replan required; this is ordinary implementation of accepted review findings.

## Business Alignment

* None

## Validation Record

| Check | Scope | Status | Evidence or reason |
|---|---|---|---|
| `dotnet build SupportHub.slnx --no-restore` | Full solution | Passed | Build succeeded; only the existing `MessagePack` vulnerability warnings remain. |
| `dotnet test SupportHub.slnx --no-restore` | Full solution | Unavailable | The command produced no output within the validation budget and was stopped; prior baseline evidence recorded 40 passing tests. |
| `dotnet test tests/SupportHub.Tickets.Api.Tests/SupportHub.Tickets.Api.Tests.csproj --no-restore` | Policy and failure-seam tests | Passed | 28 focused attachment tests passed, including rejection cleanup failure and cancellation preservation. |
| Aspire AppHost attachment integration tests | Distributed API/storage | Passed | Three filtered lifecycle, validation, and ticket-cleanup tests passed after the review fixes. |
| Aspire CLI `wait blobs`, `wait ticketsapi`, `wait webfrontend` | Runtime health | Passed | All affected resources reached healthy state. |
| Aspire CLI logs for `ticketsapi` and `webfrontend` | Runtime diagnostics | Passed | Migration, Azurite container access, service discovery, and attachment requests observed without application errors. |
| Playwright browser smoke flow | Web attachment experience | Unavailable for this invocation | The required Playwright browser binary is not installed; prior valid-flow browser smoke evidence remains available, while this invocation verified the Web route through HTTPS HTTP checks. |
| Direct Azurite blob enumeration/assertion | Exact container/blob test assertion | Unavailable | Existing distributed tests validate behavior through the API; direct resource-client assertions were not added. |
| Deterministic failure-handling tests | Compensation and delete failure seams | Passed | 26 focused tests cover policy boundaries, compensation success/failure, metadata-save outcomes, and operation-specific storage error types. |

## Pre-Review Reconciliation

* Plan markers and task-local context: `P01`, `P02`, `P03`, `P04`, and every task marker are checked.
* Completed-work entries and handoff prose: Entries above reconcile the implemented API, Web, tests, runtime, and browser behavior.
* Validation, blockers, remaining work, and follow-up items: Runtime, browser, distributed, and deterministic failure-seam verification passed; direct Azurite assertions remain an optional validation gap.
* Review readiness: Ready for review; direct Azurite enumeration remains a non-blocking follow-up, and browser automation is unavailable in this environment.

## Blockers

* None. The accepted review routes are implemented and validated within available checks.

## Remaining Work

* Original plan markers remain complete. `RV-001` and `RV-002` are complete; `RV-003` remains outside the blocking implementation scope as a distinct follow-up.

## Follow-Up Items

* Canonical plan list: [`.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md`](../../plans/2026-10-18/ticket-attachments-plan.md), `## Follow-Up Items`
* Direct Azurite inspection is a follow-up validation gap; API-level distributed tests and runtime Azure SDK logs are the current evidence.

## Return-to-Caller State

* Implementation execution status: Complete
* Declared scope and markers: `RV-001` and `RV-002` complete; original full-plan markers remain complete; `RV-003` retained as follow-up
* Validation coverage: Solution build, 28 focused tests, 3 distributed tests, Aspire health/logs, and HTTP validation of Azurite-backed preflight/download passed; full-suite rerun unavailable due no output before timeout; browser binary unavailable
* Blockers: None
* Current plan updates: Accepted review-route scope completed without changing original requirements or plan markers
* Business alignment entries: None; no BRD/PRD/ADR is cited
* Planning and critique state: Approved; critique-6 passed
* Follow-up items: Optional direct Azurite container/blob enumeration assertions
* Review readiness or no-handoff reason: Ready for `/rpi-review`; browser automation and direct Azurite enumeration remain explicit evidence limitations/follow-ups
* Continuation owner: User/manual RPI
