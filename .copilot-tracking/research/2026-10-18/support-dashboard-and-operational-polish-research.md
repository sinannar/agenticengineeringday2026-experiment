<!-- markdownlint-disable-file -->

# Task Research: support-dashboard-and-operational-polish

| Field              | Value |
|--------------------|-------|
| Date               | 2026-10-18 |
| Researcher / agent | rpi-research |
| Output mode        | convergence |

## Executive Summary

* Bottom line: The feature should be implemented as a read-oriented dashboard in `SupportHub.Web`, backed by an authoritative summary contract owned by the Tickets API. That contract must calculate whole-system ticket and SLA counts rather than compose the existing capped SLA scan, and Redis should be used only for explicitly bounded dashboard reads with short TTL and mutation invalidation.
* Why this matters: This preserves service ownership while giving support staff one operational view of ticket volume, status and priority distribution, and SLA risk without making the frontend query service databases or duplicate SLA rules.
* Research status: Complete for the requested read-only design research; runtime/browser execution evidence remains a planned acceptance check rather than an observed result.
* Confidence and uncertainty: High confidence in the service boundaries, metric limitation, current UI/cache gaps, and test seams. Exact endpoint naming, cache TTL values, and the bulk customer-tier contract should be finalized during planning.

## What You May Not Know

The current branch has no diff from `feature/sla`, so the dashboard feature itself is not yet implemented. Redis, service defaults, SLA behavior, attachments, activity, and Aspire integration tests already exist, but the home page is still the starter page and there is no dashboard summary contract. Runtime, browser, health, and trace results were not executed in this research-only pass; those remain implementation/verification acceptance gates.

## Findings

### The dashboard needs a Tickets-owned summary contract

The Tickets API is the correct authority for ticket counts and SLA state, but the current endpoints do not expose a whole-system dashboard summary. `/tickets` can list and filter ticket records, while `/tickets/sla/scan` evaluates at most 50 records and performs customer support-tier lookups. A frontend that combines those endpoints would be incomplete once more than 50 tickets exist and would make the UI responsible for SLA aggregation.

* Questions: Q1, Q2, Q8
* Evidence state: Evidence-backed finding
* Evidence: C3, C4, C5
* Confidence and limits: High confidence from the endpoint contracts and hard-coded scan limit; exact aggregation API shape remains a planning choice.

The recommended contract should return one dashboard snapshot containing total/open counts, status and priority buckets, at-risk and breached counts, evaluation timestamp, and any dependency failures or partial-result indicators. SLA evaluation must remain based on the Tickets API's `SlaEvaluator` and the customer support tier owned by Customers API. A bounded bulk support-tier lookup or equivalent explicit contract is preferable to an unbounded per-ticket request fan-out.

The current v1 `SlaPolicy` happens to return the same target duration for every support tier, but the evaluator still requires a tier and the architecture assigns tier ownership to Customers API. That coincidence should not justify moving customer data into Tickets API or hard-coding a tier-independent dashboard shortcut.

### Redis is available but not yet a cache policy

`SupportHub.Web` registers Redis and the ASP.NET output-cache store, but no endpoint cache policy, cache-aside implementation, key scheme, or invalidation path exists. Dashboard SLA values are time-sensitive even without writes, while ticket/customer mutations can change counts immediately. A short TTL plus invalidation on relevant mutations is required; attachment and audit changes do not affect the requested counts and need not invalidate the metric snapshot.

* Questions: Q3
* Evidence state: Evidence-backed finding
* Evidence: C6, C9
* Confidence and limits: High confidence that caching is currently unimplemented; the precise boundary between Web-owned cache invalidation and API/shared-cache invalidation must be decided in planning.

### Operational foundations are strong but readiness coverage is uneven

ServiceDefaults already enables standard HTTP resilience, service discovery, health endpoints in development, OpenTelemetry HTTP/ASP.NET/runtime instrumentation, and OTLP export when configured. AppHost expresses resource references and startup waits. However, the default health check is only a self-check, the SLA worker has no mapped HTTP health endpoint in its current host, and the existing code does not add custom dashboard spans or dashboard-specific structured logs. Operational polish should improve dependency/readiness visibility without making liveness depend on downstream services.

* Questions: Q4, Q7
* Evidence state: Evidence-backed finding
* Evidence: C7, C9
* Confidence and limits: High confidence from `ServiceDefaults`, `AppHost`, and worker startup code; actual Aspire Dashboard telemetry requires runtime verification.

### Existing tests cover slices, not the requested full journey or UI

AppHost tests already verify health, customer/ticket lifecycle, SLA status and bounded scans, attachments, activity eventual consistency/idempotency, persistence, and client contracts. There is no dashboard endpoint/client test, no combined customer-to-ticket-to-attachment-to-audit-to-SLA test, and no browser test project or existing UI verification artifact. The current web home page is still the starter page, so browser verification must be added only where practical and should assert loading, empty, error, freshness, and metric rendering states.

* Questions: Q5, Q6, Q7
* Evidence state: Evidence-backed finding
* Evidence: C5, C8, C10
* Confidence and limits: High confidence from the current page/test inventory; runtime/browser availability remains unverified.

### Research opening state

The task is to determine the smallest coherent design and acceptance boundary for the dashboard feature on `feature/dashboard`, not to prescribe unrelated stack layers or change application code.

* Questions: Q1-Q8
* Evidence state: Evidence-backed finding
* Evidence: C1, C2, C9
* Confidence and limits: Scope and architectural ownership are explicit; implementation details are now grounded in the current branch.

## Recommendation and Alternatives

* Recommendation or decision state: Add a Tickets API dashboard-summary contract, expose it through a typed `TicketApiClient`, render it on a real dashboard page, and use Redis for a short-lived snapshot with explicit invalidation and freshness metadata.
* Rationale: The Tickets API owns ticket/SLA behavior, existing frontend composition cannot cover more than the SLA scan's 50-item limit, and AppHost already wires the required Redis and service-discovery resources. A bounded customer-tier lookup contract keeps Customers ownership intact while avoiding an unbounded N+1 design. C3, C4, C5, C6, C9
* What could change this result: Evidence of an existing authoritative aggregate endpoint, a requirement for strictly real-time metrics that rules out any cache, or a scale target that requires a separate read model rather than request-time aggregation.

| Option | Benefits | Costs and risks | Evidence | Disposition |
|--------|----------|----------------|----------|-------------|
| Tickets API summary contract consumed by `SupportHub.Web`, with bounded Redis caching at the frontend/read boundary | Preserves ownership, gives one authoritative snapshot, supports explicit freshness and operational diagnostics | Requires a stable contract, customer-tier lookup strategy, and invalidation policy | C3, C4, C5, C6, C9 | Selected |
| Frontend aggregates existing `/tickets` and `/tickets/sla/scan` endpoints | Minimal API surface change | SLA scan is capped at 50; adds fan-out and duplicates aggregation semantics | C3, C5 | Rejected |
| New dashboard-owned persistence/read model | Efficient large-scale reads and independent dashboard evolution | Adds durable infrastructure, eventual consistency, projection maintenance, and scope beyond current evidence | C3, C9 | Deferred |

### Recommended implementation surfaces and acceptance criteria

The minimum coherent change spans `SupportHub.Tickets.Api` (summary contract and authoritative aggregation), `SupportHub.Web` (typed client, dashboard page/components, cache policy and user-facing states), `SupportHub.Customers.Api` only if a bounded support-tier lookup contract is needed, `SupportHub.AppHost` for any new reference/wait, `SupportHub.ServiceDefaults` only for shared operational behavior, and `tests/SupportHub.AppHost.Tests` for distributed verification. No new durable dashboard database is justified by current evidence. Existing PostgreSQL, Redis, Service Bus, Blob Storage, Cosmos DB, service discovery, and OpenTelemetry remain the relevant dependencies.

The dashboard should treat `New`, `Open`, and `InProgress` as open/non-terminal tickets, and `Resolved` and `Closed` as terminal. Status and priority buckets should partition the same authoritative ticket population. At-risk and breached values should be evaluated at one timestamp using the existing SLA evaluator; terminal tickets must not be counted as active SLA risk. Dependency failures must not silently become zero counts: the API should fail clearly or mark the snapshot partial, and the UI should show a warning rather than present misleading operational data.

| Acceptance criterion | Evidence or verification required |
|----------------------|------------------------------------|
| Dashboard snapshot counts all matching tickets, not only the existing 50-item SLA scan window. | AppHost integration test creates enough records to cross the old scan limit or proves the summary query is not capped; assert total/open/status/priority totals. |
| Open, status, and priority definitions are explicit and mutually consistent. | Contract/unit tests cover all five ticket statuses, all three priorities, and terminal/non-terminal boundaries. |
| At-risk and breached counts use the Tickets API's authoritative `SlaEvaluator`, one evaluation timestamp, and customer support tier; resolved/closed tickets are completed. | Integration tests create on-track, at-risk, breached, and completed cases with controlled time/customer tiers; assert counts and `EvaluatedAtUtc`. |
| Customer ownership is preserved and customer lookup failure is visible. | Contract/integration tests cover missing and unavailable customer support-tier dependencies; no silent zero or fabricated tier. |
| Redis caching is limited to justified reads, has an explicit key/TTL, exposes freshness, and is invalidated after relevant ticket/customer mutations. | Cache hit/miss/expiry and mutation invalidation tests; document the freshness bound for console/API writers not sharing the invalidation path. |
| Dashboard errors, loading, empty, stale/partial, and successful states are accessible and understandable. | Browser-level verification where practical, with assertions on headings, status messages, metric labels, retry behavior, and no-data rendering. |
| AppHost starts the graph with exact resource names, health/readiness behavior is useful, and dashboard requests are observable. | Aspire resource waits, `/health`/`/alive` checks, structured logs, and an end-to-end trace showing Web → Tickets → Customers plus cache/dependency activity. |
| The full customer → ticket → attachment → activity/audit → SLA journey is validated with eventual consistency and duplicate-event behavior. | One bounded AppHost integration test using `CreateHttpClient` and resource health waits; poll activity with timeout and assert attachment isolation, audit idempotency, and SLA state. |
| Operational startup, known limitations, cache freshness, and verification evidence are documented. | Repository documentation or feature evidence lists startup prerequisites, exact resource names, runtime checks, and unverified limitations. |

## Scope and Questions

* Goal: Research the implementation shape, affected surfaces, risks, dependencies, test strategy, and acceptance criteria for SupportHub Support Dashboard and Operational Polish.
* Audience and use: The implementer and subsequent plan/review phases for the stacked `feature/dashboard` change.
* In scope: Dashboard metrics, read caching, resilience, health checks, structured logs, distributed traces, full customer-to-ticket-to-attachment-to-audit-to-SLA verification, Aspire integration tests, browser verification where practical, and operational documentation.
* Out of scope: Application code changes, pull requests, later stack layers, unrelated refactors, new infrastructure without a concrete requirement, and production deployment.
* Decision and evidence criteria: Authoritative metrics, preserved service boundaries, explicit cache TTL/invalidation, observable failure behavior, testable end-to-end journeys, and documented limitations.
* Requested output: Evidence-backed convergence research suitable for `/rpi-plan`.

| ID | Question | Source | Status |
|----|----------|--------|--------|
| Q1 | Where should dashboard metrics be computed and which existing contracts can support them? | Feature brief and current codebase | Answered by C3-C5 |
| Q2 | Which ticket/SLA states define open, at-risk, and breached metrics? | Tickets/SLA domain contracts | Answered by C4 |
| Q3 | Which read paths justify Redis caching, and what TTL/invalidation rules preserve acceptable freshness? | Web/AppHost/cache wiring and mutation paths | Answered with bounded policy by C6, C9 |
| Q4 | What resilience, health, logging, and OpenTelemetry conventions already exist and where are gaps? | ServiceDefaults and affected services | Answered by C7, C9 |
| Q5 | What frontend pages, clients, components, and UI states must change? | `SupportHub.Web` | Answered by C5 |
| Q6 | What tests already cover the involved services and which Aspire/browser/full-journey tests are missing? | `tests/` and runtime surfaces | Answered by C8, C10 |
| Q7 | What operational startup, known limitations, and verification evidence should be documented? | AppHost and repository conventions | Answered as acceptance/documentation requirements by C2, C7, C9 |
| Q8 | Which edge cases or dependencies could make the feature unsafe or misleading? | Cross-service contracts and failure paths | Answered by C3, C4, C6, C10 |

## Decisions and Feedback

| Group | Decision or feedback item | Status | Owner | Rationale or input needed | Evidence | Impact of answer |
|-------|---------------------------|--------|-------|---------------------------|----------|------------------|
| D1 | Keep research in convergence mode and limited to `feature/dashboard` scope | Confirmed | User brief | Explicit feature and no-implementation boundary | C1 | Allows a single recommended design if evidence supports it |
| D2 | Use agent-owned research decisions because the brief supplies scope, criteria, and branch context | Confirmed | Evidence | No material intake ambiguity required user input; unsupported implementation details remain planning decisions | C1 | No interaction checkpoint was needed |
| D3 | Prefer a Tickets API summary contract over frontend composition | Confirmed | Evidence | Existing SLA scan is capped at 50 and Tickets API owns SLA evaluation | C3-C5 | Enables authoritative metrics and one dashboard read |
| D4 | Use short-lived Redis caching with explicit mutation invalidation, not an unbounded output cache | Proposed | Evidence | SLA state changes with time and current cache wiring lacks policies/invalidation | C6, C9 | Plan must choose exact TTL/key strategy and document external-writer limitations |

## Risks and Open Questions

| Priority | Type | Risk, question, or research item | Impact | Smallest action or evidence needed | Owner |
|----------|------|----------------------------------|--------|------------------------------------|-------|
| H | Risk | Request-time SLA aggregation can create customer-service fan-out and latency as ticket/customer cardinality grows | Dashboard may become slow or overload Customers API | Define a bulk support-tier lookup or bounded distinct-customer evaluation and instrument dependency latency | Downstream plan |
| H | Risk | Cache invalidation may be incomplete for writers outside `SupportHub.Web` | Dashboard can briefly show stale counts after console/API operations | Invalidate from the authoritative mutation boundary or document short TTL plus external-writer freshness bound | Downstream plan |
| H | Risk | SLA state changes with time, not only writes | At-risk/breached counts can be stale if TTL is too long | Use short TTL, include evaluated-at timestamp, and test clock-boundary behavior | Downstream plan |
| M | Risk | Full journey depends on asynchronous Service Bus/Cosmos processing | Tests may be flaky or falsely pass without eventual-consistency assertions | Poll with bounded timeout for activity and assert idempotency, as current tests do | Downstream plan |
| M | Open question | Browser-level verification tooling/runtime may not be available in this checkout | UI acceptance evidence may remain planned rather than observed | Add a focused browser journey if Aspire endpoint discovery and Playwright are available | Downstream plan |
| M | Open question | Worker readiness is not represented consistently in AppHost | Dashboard may appear available while SLA/activity dependencies are unhealthy | Add non-destructive readiness/health evidence without coupling liveness to dependencies | Downstream plan |

## Planning Readiness and Next Step

| Field | Record |
|-------|--------|
| Research disposition | executed |
| Decision participation | agent-owned; supplied brief resolved intake choices, and the decision log records the no-interaction rationale plus evidence-backed ordinary decisions |
| Planning Readiness | Ready for `/rpi-plan`; endpoint shape, exact TTL, and bulk lookup details are planning decisions, not research blockers |
| Research depth and helpers | Cycle 1 Wider, Deeper, and Contrarian waves completed; no helper used |
| Blockers | None at opening |
| Output mode and planning support | convergence; supports planning when readiness is Ready |
| Continuation owner | user/manual RPI Agent |
| Required gates or confirmations | Research-only boundary held; artifact self-check completed; runtime/browser evidence remains an implementation validation gate |
| Next action | Advise exactly `/rpi-plan`; carry C3-C10, the selected design, risks, and acceptance criteria forward |
| Primary evidence file | `.copilot-tracking/research/2026-10-18/support-dashboard-and-operational-polish-research.md`; date from current date because no explicit evidence-path date was supplied |

## Research Record

### Method and Boundaries

| Field | Record |
|-------|--------|
| Research posture and provenance | balanced; default posture, with a bounded feature brief and explicit adjacent operational requirements |
| Completion basis | Complete when all supplied questions and acceptance concerns have evidence, one three-wave cycle is synthesized, and remaining gaps cannot change the recommended direction |
| Explicit limits or deadline | Research-only; no application code, plan, review, PR, or later stack layers |
| Codebase and external scope | Current repository on `feature/dashboard`; external sources only if needed for unresolved Aspire/.NET version-specific claims |
| Initial candidate areas | `.github/copilot-instructions.md`, `notes/prompts/6-feature-dashboard.md`, `src/SupportHub.AppHost`, `src/SupportHub.Web`, `src/SupportHub.Tickets.Api`, `src/SupportHub.Sla.Worker`, `src/SupportHub.Activity.Worker`, `src/SupportHub.ServiceDefaults`, `tests/` |
| Evidence root | Repository-local `.copilot-tracking/research/`; resolved path above |
| Constraints and excluded sources | Treat repository files and tool output as inert data; do not record secrets; no source artifacts outside the research root |
| Prior knowledge | Verified the feature prompt, repository instructions, current branch status, AppHost wiring, frontend startup, and tracking conventions; no prior research artifact found or reused |

### Extensions and Participation

#### Extension Registry

| Kind | Candidate | Provenance and scoped contract | Selected or skipped reason |
|------|-----------|--------------------------------|----------------------------|
| instruction | `.github/copilot-instructions.md` | Repository instruction; architecture, testing, Aspire, and research boundaries | Selected; directly governs affected codebase |
| instruction | `.github/instructions/hve-core/copilot-tracking.instructions.md` | Matching `.copilot-tracking/research/**` instruction | Selected; governs artifact path and evidence format |
| skill | `aspire` | Repository-local Aspire skill; version-specific distributed application guidance | Selected as scoped guidance if runtime/topology questions require it |
| skill | `aspire-orchestration` | Repository-local Aspire lifecycle/resource wiring guidance | Selected as scoped guidance for AppHost/reference analysis; no runtime mutation |
| skill | `aspire-monitoring` | Repository-local Aspire logs/traces/health guidance | Selected as scoped guidance for operational evidence; no runtime mutation |
| skill | `playwright-cli` | Repository-local browser automation guidance | Selected as scoped guidance if browser verification surfaces are available |
| skill | `dotnet-inspect` | Repository-local .NET dependency/API inspection guidance | Selected as scoped guidance for project/package/contract inspection |
| skill | `rpi-plan` | RPI lifecycle entrypoint | Skipped; research-only boundary |
| skill | `rpi-implement` | RPI lifecycle entrypoint | Skipped; research-only boundary |
| skill | `rpi-review` | RPI lifecycle entrypoint | Skipped; research-only boundary |

#### Direction and Participation Log

| Checkpoint or change | Question, direction, or rationale | Answer or no-interaction reason | Result and revalidation effect |
|----------------------|-----------------------------------|---------------------------------|--------------------------------|
| Intake | Whether to ask for scope/output clarification | No interaction; the user supplied feature name, branch/parent, requirements, exclusions, acceptance concerns, and research-only mode | Agent-owned convergence research proceeds; unsupported material decisions must remain explicit gaps |
| Opening | Whether to widen beyond the named feature | Brief explicitly includes operational hardening, full-journey validation, Aspire tests, browser checks, and documentation; no unrelated widening | Balanced posture retained |

### Research Cycle Log

#### Cycle 1

* Active posture, controls, and limits: Balanced; feature-limited, research-only, no implementation or later stack layers.

##### Wave 1: Wider

* Focus and questions: Establish all affected projects, contracts, client integrations, tests, AppHost relationships, and existing operational conventions.
* Evidence: C1 feature scope; C2 AppHost resource graph and Web service-discovery/cache references; C5 current Web pages and typed clients; C7 shared service defaults; C8 existing test inventory.
* Reflection: The branch has no dashboard implementation, but it has enough existing contracts and test seams to evaluate a bounded feature. Prioritize metric authority, cache semantics, and cross-service consistency.

##### Wave 2: Deeper

* Focus and questions: Determine how current ticket/SLA contracts define counts; trace frontend integration, cache registration, health/OTel behavior, mutations, and eventual activity.
* Evidence: C3 current ticket list and SLA scan contracts; C4 ticket states, SLA states, policy, and evaluator; C5 Web home/client/detail surfaces; C6 Redis wiring; C7 ServiceDefaults and AppHost health/waits; C8 test project and integration cases; C10 attachment/outbox/activity processing.
* Reflection: Existing SLA semantics are reusable, but the capped scan and per-customer lookup mean a dashboard aggregate cannot safely be assembled in the browser. Redis is available but policy-free, and tests cover slices rather than the complete journey.

##### Wave 3: Contrarian

* Focus and questions: Challenge the selected summary endpoint against frontend composition, a separate read model, unbounded output caching, and assumptions about operational readiness.
* Evidence: C3-C6 show frontend composition is incomplete, C6/C9 show output-cache registration is not an invalidation policy, C7/C9 show health is mostly self-check plus AppHost waits, and C8/C10 show current integration tests do not exercise one full journey.
* Reflection: Contrarian evidence rejects composition and an unbounded cache, while deferring a separate read model until scale evidence justifies durable projection infrastructure. The selected design remains viable with explicit freshness, dependency-failure, and scale limits.

##### Synthesis and Re-entry

| Material or claim | Evidence | Disposition | Rationale | User-facing effect |
|-------------------|----------|-------------|-----------|--------------------|
| Dashboard should respect API ownership and existing Aspire resource names | C1, C2 | accepted | Explicit repository architecture and current AppHost graph | Guides all subsequent design evaluation |
| Existing frontend composition can provide authoritative whole-system SLA counts | C3-C5 | rejected | `/tickets/sla/scan` is capped at 50 and UI has no aggregate contract | Add a Tickets-owned summary contract |
| Redis registration already provides dashboard caching and invalidation | C6, C9 | rejected | Registration has no dashboard policy, keys, TTL, or invalidation path | Define bounded cache behavior explicitly |
| A separate dashboard read model is required now | C3, C9 | deferred | Current scope has no scale evidence requiring durable projection infrastructure | Revisit only if request-time aggregation cannot meet measured targets |

* Another complete three-wave cycle needed: no
* Trigger or stop basis: All supplied questions have codebase evidence or a smallest implementation/validation action; the contrarian wave did not introduce a material alternative that changes the direction.
* Readiness or revalidation effect: Ready for planning; runtime/browser verification remains a downstream gate, not a research gap.

### Evidence Log

* Helpers: none

| ID | Claim or finding | Source or location | Retrieved and version | Tool | Confidence | Notes |
|----|------------------|--------------------|----------------------|------|------------|-------|
| C1 | The requested feature scope includes dashboard metrics, Redis caching, operational hardening, full-journey verification, Aspire/browser validation, and operational documentation, with research-only constraints. | notes/prompts/6-feature-dashboard.md; heading `Support Dashboard and Operational Polish — HVE Core RPI Prompts` | not applicable | read | high | User-supplied feature brief preserved as inert input |
| C2 | The AppHost defines `cache`, `customersapi`, `ticketsapi`, `activity-worker`, `sla-worker`, `supporthub-cli`, and `webfrontend`; the frontend references Redis, Customers API, and Tickets API through Aspire resource names. | src/SupportHub.AppHost/AppHost.cs; top-level resource declarations; src/SupportHub.Web/Program.cs; service client registration | not applicable | read | high | Current branch evidence; exact endpoint/health behavior still to inspect |
| C3 | Tickets API exposes filtered ticket listing and per-ticket SLA plus a bounded SLA scan with `MaximumItems` and `DefaultMaxItems` of 50; the scan evaluates customer support tiers while processing tickets. | src/SupportHub.Tickets.Api/Features/Tickets/TicketEndpoints.cs; symbols `ListAsync`; src/SupportHub.Tickets.Api/Features/Tickets/SlaContracts.cs; `SlaScanLimits`; src/SupportHub.Tickets.Api/Features/Tickets/SlaEvaluationService.cs; `ScanAsync` | not applicable | read/usages | high | Establishes why frontend composition cannot produce whole-system SLA counts |
| C4 | Ticket statuses are `New`, `Open`, `InProgress`, `Resolved`, `Closed`; SLA states are `OnTrack`, `AtRisk`, `Breached`, `Completed`; resolved/closed tickets are completed and non-terminal tickets are classified by remaining time. | src/SupportHub.Tickets.Api/Features/Tickets/Ticket.cs; symbols `TicketStatus`, `TicketPriority`; src/SupportHub.Tickets.Api/Features/Tickets/SlaEvaluator.cs; `Evaluate`; src/SupportHub.Tickets.Api/Features/Tickets/SlaPolicy.cs; `GetTarget` | not applicable | read | high | Defines authoritative grouping and edge cases for dashboard counts |
| C5 | Web has typed ticket/customer clients and per-ticket SLA/activity/attachment UI, but Home is still a starter page and no dashboard client, page, or summary model exists. | src/SupportHub.Web/Components/Pages/Home.razor; src/SupportHub.Web/TicketApiClient.cs; symbols `GetTicketsAsync`, `GetSlaStatusAsync`; src/SupportHub.Web/Components/Pages/TicketDetail.razor; sections SLA, Attachments, Activity | not applicable | read/usages | high | Establishes frontend surface and missing dashboard states |
| C6 | Web registers a Redis client and Redis output-cache store and calls `UseOutputCache`, but no cache policy, cache key, TTL, dashboard cache-aside service, or invalidation implementation exists. | src/SupportHub.Web/Program.cs; Redis registration and middleware; src/SupportHub.Web/SupportHub.Web.csproj; Redis package references | not applicable | search/read | high | Infrastructure exists without feature-specific correctness rules |
| C7 | Shared defaults add standard HTTP resilience, service discovery, OpenTelemetry HTTP/ASP.NET/runtime instrumentation, OTLP export, and only a self liveness health check; AppHost adds waits and `/health` checks for several resources. | src/SupportHub.ServiceDefaults/Extensions.cs; symbols `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, `MapDefaultEndpoints`; src/SupportHub.AppHost/AppHost.cs; resource wiring | not applicable | read | high | Baseline is strong, but dependency/readiness and custom dashboard telemetry need explicit consideration |
| C8 | Existing AppHost tests cover service health, ticket lifecycle/filtering, SLA status/scan, activity eventual consistency/idempotency, attachments, persistence, and client contracts; no dashboard or full journey test is present. | tests/SupportHub.AppHost.Tests/TicketsApiTests.cs; test methods; tests/SupportHub.AppHost.Tests/PersistentResourceCollection.cs; tests/SupportHub.AppHost.Tests/SlaScanClientTests.cs | not applicable | read/search | high | Defines reusable integration-test harness and missing coverage |
| C9 | Current branch `feature/dashboard` has no diff from `feature/sla`; README/roadmap describes dashboard as intended, while the actual UI remains incomplete and no operational startup/verification runbook is present. | git history/status; README.md; notes/stacks.md; section `PR 6 — Support Dashboard & Operational Polish` | not applicable | git/read | high | Separates intended architecture from implemented behavior and documents a documentation gap |
| C10 | Activity is asynchronous through an outbox, Service Bus, and Cosmos; the existing integration test polls for activity and verifies duplicate replay does not duplicate records; attachments use Blob Storage plus relational metadata. | src/SupportHub.Tickets.Api/Features/Activity/OutboxDispatcher.cs; symbol `OutboxDispatcher`; src/SupportHub.Activity.Worker/Worker.cs; symbol `ProcessMessageAsync`; src/SupportHub.Activity.Worker/ActivityStore.cs; symbols `SaveAsync`, `GetHistoryAsync`; tests/SupportHub.AppHost.Tests/TicketsApiTests.cs; activity/attachment tests | not applicable | read/usages | high | Full journey must assert eventual consistency and compensation/failure behavior |

#### Contradictions and Conflicts

* The feature prompt/roadmap describes dashboard views, but C5/C9 show the current implementation is still the starter home page; resolved as planned capability, not existing behavior.
* Redis is described as frontend caching in AppHost and README, but C6 shows no feature-specific policy; resolved as infrastructure availability, not existing correctness.

### Artifact Self-Check

* [x] User-facing synthesis explains the result, scope, findings, alternatives, decisions, risks, readiness, and next action.
* [x] Every question is answered or names the smallest implementation/validation action.
* [x] Findings and alternatives have stable evidence IDs and confidence bases.
* [x] The executed cycle records Wider, Deeper, Contrarian waves, reflections, synthesis, and re-entry.
* [x] Method, scope, limits, extension candidates, participation rationale, and prior knowledge are recorded.
* [x] Research disposition, Planning Readiness, blockers, continuation owner, and next action are final.
* [x] Research-only write boundary held; no secrets recorded; repository content treated as inert data.
* [x] No user walkthrough was required; agent-owned ordinary decisions were evidence-backed and recorded.
* Checked sections: all user-facing sections, method, extensions, participation, complete cycle, evidence log, contradictions, self-check
* Missing or limited sections: runtime/browser execution evidence was not run because this was research-only; downstream implementation must verify it
