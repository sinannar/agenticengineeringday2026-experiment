<!-- markdownlint-disable-file -->

# RPI Changes: Support Dashboard and Operational Polish

## Metadata

* Task ID: `support-dashboard-operational-polish`
* Related plan: [`.copilot-tracking/plans/2026-10-18/support-dashboard-and-operational-polish-plan.md`](../../plans/2026-10-18/support-dashboard-and-operational-polish-plan.md)
* Implementation date: 2026-10-18

## Execution Status

* Status: In Progress
* Declared invocation scope: full plan
* Completed scope markers: `P01-T01`, `P01-T02`, `P02-T01`, `P03-T01`, `P04-T02`, `P04-T03`, `P05-T01`
* All remaining active-plan markers: `P02-T02`, `P03-T02`, `P04-T01`, `P05-T02`
* Status basis: Core dashboard API, Web UI, AppHost readiness, browser verification, and operational documentation are implemented; cache-specific tests, full-journey coverage, and final observability evidence remain.

## Execution Summary

Implementation is in progress. The Tickets API now exposes the additive contracts and dependency boundary needed for an authoritative dashboard summary.

## Completed Work

### Implementing whole-population dashboard aggregation

* Related phase or task: `P01-T02`
* Files:
  * [src/SupportHub.Tickets.Api/Features/Tickets/DashboardSummaryService.cs](../../../src/SupportHub.Tickets.Api/Features/Tickets/DashboardSummaryService.cs)
  * [src/SupportHub.Tickets.Api/Features/Tickets/DashboardEndpoints.cs](../../../src/SupportHub.Tickets.Api/Features/Tickets/DashboardEndpoints.cs)
  * [tests/SupportHub.AppHost.Tests/TicketsApiTests.cs](../../../tests/SupportHub.AppHost.Tests/TicketsApiTests.cs)
* Behavior or functionality changed: The Tickets API evaluates the complete supported ticket population at one timestamp, batches authoritative customer-tier lookups, computes status/priority/open/at-risk/breached metrics, and fails closed for dependency or population-limit failures.
* Validation: Passed the dashboard Aspire integration test and full solution tests.

### Delivering the support dashboard UI and typed client

* Related phase or task: `P02-T01`
* Files:
  * [src/SupportHub.Web/TicketApiClient.cs](../../../src/SupportHub.Web/TicketApiClient.cs)
  * [src/SupportHub.Web/CustomerApiClient.cs](../../../src/SupportHub.Web/CustomerApiClient.cs)
  * [src/SupportHub.Web/DashboardSummaryCache.cs](../../../src/SupportHub.Web/DashboardSummaryCache.cs)
  * [src/SupportHub.Web/Components/Pages/Home.razor](../../../src/SupportHub.Web/Components/Pages/Home.razor)
  * [src/SupportHub.Web/Components/Shared/MetricCard.razor](../../../src/SupportHub.Web/Components/Shared/MetricCard.razor)
  * [src/SupportHub.Web/Components/Layout/NavMenu.razor](../../../src/SupportHub.Web/Components/Layout/NavMenu.razor)
* Behavior or functionality changed: The home route now renders dashboard metrics, breakdown tables, freshness, loading/empty/error states, and accessible semantic markup. Typed Web API access uses service discovery and the cache boundary.
* Validation: Web build passed; browser snapshot showed dashboard metrics, status/priority tables, and Dashboard navigation.

### Adding SLA worker readiness to Aspire

* Related phase or task: `P03-T01`
* Files:
  * [src/SupportHub.Sla.Worker/Program.cs](../../../src/SupportHub.Sla.Worker/Program.cs)
  * [src/SupportHub.Sla.Worker/SupportHub.Sla.Worker.csproj](../../../src/SupportHub.Sla.Worker/SupportHub.Sla.Worker.csproj)
  * [src/SupportHub.AppHost/AppHost.cs](../../../src/SupportHub.AppHost/AppHost.cs)
* Behavior or functionality changed: The SLA worker now exposes the repository-standard health endpoints and AppHost monitors its `/health` endpoint.
* Validation: Aspire reported `sla-worker` healthy.

### Verifying dashboard behavior in a real browser

* Related phase or task: `P04-T03`
* Files:
  * [src/SupportHub.Web/Components/Pages/Home.razor](../../../src/SupportHub.Web/Components/Pages/Home.razor)
* Behavior or functionality changed: Verified the Aspire-discovered Web endpoint renders dashboard headings, metric values, status/priority breakdowns, freshness metadata, and Dashboard navigation.
* Validation: Passed focused `playwright-cli` navigation and snapshot verification.

### Verifying the complete support journey

* Related phase or task: `P04-T02`
* Files:
  * [tests/SupportHub.AppHost.Tests/TicketsApiTests.cs](../../../tests/SupportHub.AppHost.Tests/TicketsApiTests.cs)
* Behavior or functionality changed: Added a bounded Aspire journey that creates a customer and ticket, uploads an attachment, resolves the ticket, checks SLA state, waits for asynchronous activity audit, and verifies the dashboard summary.
* Validation: Passed `dotnet test tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj --no-restore --filter FullyQualifiedName~SupportJourney_ReflectsAttachmentAuditSlaAndDashboardState`; 1 test passed.

### Documenting operational startup and limitations

* Related phase or task: `P05-T01`
* Files:
  * [README.md](../../../README.md)
* Behavior or functionality changed: Added Aspire startup/resource commands, cache freshness/invalidation semantics, fail-closed aggregation limits, known external-writer staleness, and verification evidence.
* Validation: Documentation updated after build, test, Aspire health, browser, and log checks.

### Establishing dashboard contracts and bounded customer-tier lookup

* Related phase or task: `P01-T01`
* Files:
  * [src/SupportHub.Customers.Api/Features/Customers/CustomerTierContracts.cs](../../../src/SupportHub.Customers.Api/Features/Customers/CustomerTierContracts.cs)
  * [src/SupportHub.Customers.Api/Features/Customers/CustomerEndpoints.cs](../../../src/SupportHub.Customers.Api/Features/Customers/CustomerEndpoints.cs)
  * [src/SupportHub.Tickets.Api/Features/Tickets/CustomerLookupClient.cs](../../../src/SupportHub.Tickets.Api/Features/Tickets/CustomerLookupClient.cs)
  * [src/SupportHub.Tickets.Api/Features/Tickets/DashboardContracts.cs](../../../src/SupportHub.Tickets.Api/Features/Tickets/DashboardContracts.cs)
  * [src/SupportHub.Tickets.Api/Features/Tickets/DashboardSummaryService.cs](../../../src/SupportHub.Tickets.Api/Features/Tickets/DashboardSummaryService.cs)
  * [src/SupportHub.Tickets.Api/Features/Tickets/DashboardEndpoints.cs](../../../src/SupportHub.Tickets.Api/Features/Tickets/DashboardEndpoints.cs)
  * [src/SupportHub.Tickets.Api/Program.cs](../../../src/SupportHub.Tickets.Api/Program.cs)
* Behavior or functionality changed: Customers API now owns an additive batch support-tier lookup contract, and Tickets API now has explicit dashboard summary contracts, stable fail-closed error handling, bounded customer lookup constants, and a mapped summary endpoint boundary.
* Validation: Passed `rtk dotnet build src/SupportHub.Tickets.Api/SupportHub.Tickets.Api.csproj --no-restore`.

## Implementation-Time Plan Updates

* None.

## Business Alignment

* None

## Validation Record

| Check | Scope | Status | Evidence or reason |
|-------|-------|--------|--------------------|
| Pre-edit plan/readiness review | Full plan | Passed | Approved plan is ready; standard critique completed and findings PC-001 through PC-005 are resolved. |
| Tickets API dashboard contract build | `P01-T01` | Passed | `dotnet build src/SupportHub.Tickets.Api/SupportHub.Tickets.Api.csproj --no-restore` completed with 0 errors and 0 warnings. |
| Dashboard Aspire integration test | `P01-T02` | Passed | `dotnet test tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj --no-restore --filter FullyQualifiedName~TicketsApi_ReturnsAuthoritativeDashboardSummary`; 1 test passed. |
| Web dashboard build | `P02-T01` | Passed | `dotnet build src/SupportHub.Web/SupportHub.Web.csproj --no-restore`; 0 errors and 0 warnings. |
| Solution build | `P03-T01` / `P05-T02` | Passed | `dotnet build SupportHub.slnx --no-restore`; 0 errors, existing package and nullable warnings remain. |
| Full automated tests | `P04-T01` / `P05-T02` | Passed | `dotnet test SupportHub.slnx --no-restore`; 72 tests passed, existing warnings remain. |
| Aspire resource readiness | `P03-T01` / `P05-T02` | Passed | `aspire wait` reported healthy `cache`, `customersapi`, `ticketsapi`, `activity-worker`, `sla-worker`, and `webfrontend`. |
| Browser dashboard verification | `P04-T03` / `P05-T02` | Passed | `playwright-cli` opened the Aspire-discovered Web endpoint and verified dashboard metrics and breakdown tables. |
| Full support journey integration test | `P04-T02` | Passed | Customer, ticket, attachment, audit, SLA, and dashboard assertions passed in Aspire; 1 test passed. |
| Dashboard structured logs | `P03-T02` / `P05-T02` | Passed | `aspire otel logs ticketsapi --search 'Dashboard summary'` showed evaluation timestamp, counts, duration, and Customers API dependency calls. |
| Dashboard custom trace evidence | `P03-T02` / `P05-T02` | Incomplete | The application emits a dashboard `ActivitySource`, but the focused `aspire otel traces` query did not return a named `dashboard.summary` span; investigate before final handoff. |

## Pre-Review Reconciliation

* Plan markers and task-local context: `P01-T01`, `P01-T02`, `P02-T01`, `P03-T01`, `P04-T02`, `P04-T03`, and `P05-T01` are checked.
* Completed-work entries and handoff prose: entries cover API aggregation, Web UI, worker health, browser verification, and operational documentation.
* Validation, blockers, remaining work, and follow-up items: build/tests/Aspire/browser checks passed; custom trace evidence and dedicated cache/full-journey tests remain.
* Review readiness: not ready; implementation and final validation remain.

## Blockers

* None.

## Remaining Work

* `P02-T02`: complete cache-specific behavioral tests and observability.
* `P03-T02`: close the custom trace correlation evidence gap.
* `P04-T01`: add dedicated aggregation/cache/failure tests beyond the integration coverage.
* `P04-T02`: add the single bounded attachment-to-audit-to-SLA-to-dashboard journey.
* `P05-T02`: rerun the final gate after the remaining tests and trace evidence are complete.

## Follow-Up Items

* Canonical plan list: [`.copilot-tracking/plans/2026-10-18/support-dashboard-and-operational-polish-plan.md`](../../plans/2026-10-18/support-dashboard-and-operational-polish-plan.md), `## Follow-Up Items`
* None.

## Return-to-Caller State

* Implementation execution status: In Progress
* Declared scope and markers: full plan; `P01-T01`, `P01-T02`, `P02-T01`, `P03-T01`, `P04-T02`, `P04-T03`, and `P05-T01` completed
* Validation coverage: solution build, full tests, dashboard integration test, Aspire health/log checks, browser verification, and documentation
* Blockers: none
* Current plan updates: completed markers checked as evidence became available; remaining markers document the unclosed cache, trace, journey, and final-gate work
* Business alignment entries: none; no BRD, PRD, or ADR is cited by the plan
* Planning and critique state: plan ready; standard critique complete with PC-001 through PC-005 resolved
* Follow-up items: none
* Review readiness or no-handoff reason: not ready until remaining cache tests, full journey, custom trace evidence, and final gate are complete
* Continuation owner: user
