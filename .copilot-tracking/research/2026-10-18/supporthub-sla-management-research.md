<!-- markdownlint-disable-file -->
# Task Research: supporthub-sla-management

| Field | Value |
|---|---|
| Date | 2026-10-18 |
| Researcher / agent | rpi-research |
| Output mode | convergence |

## Executive Summary

* Bottom line: The recommended design is a Tickets API-owned, on-demand SLA evaluation surface backed by one deterministic policy/evaluator. The SLA Worker schedules calls to that API, the Operations Console triggers the same API, and Blazor reads API-provided status through the existing typed Tickets client.
* Why this matters: It preserves service data ownership, avoids direct database access from worker/console, and guarantees scheduled, manual, and UI results use one evaluation path.
* Research status: Complete — architecture, affected surfaces, test strategy, risks, acceptance criteria, and the full policy contract are recorded.
* Confidence and uncertainty: High confidence in the architectural recommendation, repository fit, and confirmed policy semantics. Medium confidence in persistence/API detail because the brief does not say whether scan results must be stored; the recommendation is to calculate on demand.

## What You May Not Know

The SLA Worker and Console are present but are scaffolds; no SLA domain code, endpoint, UI, migration, or test exists. The confirmed policy uses elapsed UTC time, marks AtRisk when remaining time is less than or equal to 20% of target or one hour, and stops evaluation at Resolved/Closed with no pause/reopen semantics. Low is 24 hours, Medium 12 hours, and High 2 hours for every Standard/Premium/Enterprise tier; the tier is an explicit policy input but currently maps to the same target.

## Findings

### The branch contains SLA scaffolding, not an implemented feature

`feature/sla` points at the same commit as `feature/activity` and has no application-code delta. The solution and AppHost already include `SupportHub.Sla.Worker` as `sla-worker` and `SupportHub.Sla.Console` as `supporthub-cli`, but the worker only logs once per second and the console has no scan option. Existing PostgreSQL and Tickets API wiring are sufficient; no new infrastructure resource is needed.

* Questions: Q1, Q2
* Evidence state: evidence-backed finding
* Evidence: C2, C3, C4, C12
* Confidence and limits: High. Runtime health was not re-run because this is research-only; source topology and package versions are verified.

### Tickets API should own SLA policy, customer-tier lookup, and evaluation results

The Tickets API owns ticket data and already has priority, status, customer ID, and `CreatedAt`. Customer support tier remains owned by Customers API, so SLA evaluation must obtain it through an API call rather than reading `customersdb`. A feature-local `SlaPolicy` and `SlaEvaluator` in Tickets API should accept explicit ticket/customer inputs plus an injected `TimeProvider`, and be called by a Tickets API scan/status endpoint.

The endpoint should return explicit result contracts containing ticket identity, policy inputs, deadline, evaluation time, status (`OnTrack`, `AtRisk`, `Breached`, and a terminal `Completed`/excluded behavior), remaining time or lateness, and enough policy/version metadata to explain the result. The UI should consume this projection and never calculate deadlines locally.

* Questions: Q1, Q2, Q3
* Evidence state: evidence-backed recommendation
* Evidence: C5, C6, C7, C8, C12
* Confidence and limits: High for ownership and boundary; exact route shape and terminal-state label remain implementation choices.

### Worker and console should be thin operational API clients

The SLA Worker should use a periodic hosted-service loop and a typed Tickets API client configured with the existing `https+http://ticketsapi` service-discovery pattern. It should call the scan endpoint and emit structured counts/results. The Console should add an on-demand scan command using the same client pattern and print a readable table. Neither should access PostgreSQL, call Customers API directly, or contain policy constants.

This makes scheduled and manual execution consistent. The scan must be cancellation-aware, bounded, and explicit about Tickets API or customer lookup failures. The worker cadence and console command syntax are operational configuration choices, not domain rules.

* Questions: Q1, Q2
* Evidence state: evidence-backed finding
* Evidence: C2, C3, C4, C12, W2, W3
* Confidence and limits: High.

### Calculate status on demand; do not persist it unless a durable alert/history requirement appears

No requirement asks for SLA history, notifications, acknowledgements, or durable status transitions. Status is derived from ticket creation time, priority, customer tier, policy version, and current time. The smallest design is therefore an on-demand Tickets API projection; a mutable SLA table would add stale-state, concurrency, migration, and reconciliation concerns without serving a stated use case.

If later requirements add breach notifications or state-change activity, that is a separate persistence/event decision. The current layer can log scan outcomes and expose them to the UI without adding infrastructure.

* Questions: Q1, Q2
* Evidence state: evidence-backed recommendation
* Evidence: C2, C5, C7, C12
* Confidence and limits: Medium-high; this depends on the brief's absence of alerting/history requirements.

### Injected UTC time is required for deterministic policy and scan tests

The Tickets API currently uses `DateTimeOffset.UtcNow` directly, while the SLA Worker uses `DateTimeOffset.Now` in its scaffold. SLA calculation should use UTC consistently and accept `TimeProvider`; production registers `TimeProvider.System`, while unit tests use `FakeTimeProvider` and advance time instantly. This enables exact tests at deadline, at-risk boundary, breach boundary, terminal transitions, and date/time edge cases without sleeps.

Microsoft's current .NET guidance recommends `TimeProvider`/`FakeTimeProvider` for deterministic time-dependent tests and supports injecting the provider into delayed or periodic operations. A `BackgroundService`/`PeriodicTimer` loop is appropriate for scheduling, but scheduler tests should focus on the evaluator and a narrow orchestration seam rather than real elapsed time.

* Questions: Q3
* Evidence state: evidence-backed finding
* Evidence: C3, C5, W1, W2
* Confidence and limits: High; exact package pinning should follow central package management during planning.

### Blazor should display API-provided SLA status and warnings

`TicketApiClient` is the established typed boundary and `TicketDetail.razor` already models loading, empty, success, and error states for activity and attachments. Add a typed SLA response and render deadline/status/warning information in ticket detail. Add list indicators only if the API returns a bounded projection in one response; avoid per-row SLA calls. Use text and semantic markup, not color alone, for at-risk and breached states.

The UI must handle temporary service/customer lookup failure, missing customer data, and a ticket becoming terminal between list and detail reads. It should not duplicate policy calculations.

* Questions: Q1, Q3
* Evidence state: evidence-backed recommendation
* Evidence: C8, C9, `notes/prompts/7-feature-ui-ux.md`
* Confidence and limits: Medium-high; exact list-versus-detail placement depends on the final API projection.

### Tests should combine pure policy coverage with Aspire integration

`SupportHub.Tickets.Api.Tests` is the right home for policy matrix, evaluator boundary, and contract tests. `SupportHub.AppHost.Tests` already starts the actual AppHost, waits for named resources, creates service-discovery HTTP clients, and verifies end-to-end ticket behavior. Add a distributed test that creates customers at each tier and tickets at each priority, invokes the SLA endpoint, and asserts deadlines and state transitions at a controlled time boundary. Add worker/console HTTP-client tests with fake handlers if their orchestration is not independently exposed.

Runtime acceptance should include healthy `sla-worker` startup, successful scan calls, structured scan logs, and no direct database connection registration in worker or console.

* Questions: Q2, Q3
* Evidence state: evidence-backed recommendation
* Evidence: C2, C3, C4, C10, C11
* Confidence and limits: High; exact distributed clock control remains an implementation seam.

## Recommendation and Alternatives

* Recommendation or decision state: Select a Tickets API-owned, on-demand SLA projection with one deterministic evaluator. Keep the SLA Worker and Operations Console as thin API clients; calculate rather than persist status for this feature; expose typed results to Blazor.
* Rationale: This is the smallest design satisfying ownership and shared-behavior requirements using the existing `ticketsdb`, `ticketsapi`, service discovery, worker, console, and Web seams. It avoids duplicated policy matrices and storage for a value derived from current inputs/time. Evidence: C2-C10, W1-W3.
* What could change this result: Confirmed business-hours calendars, paused SLA clocks, breach notifications, audit/history, or durable alert acknowledgements would require a revised policy model and possibly persistence/events.

| Option | Benefits | Costs and risks | Evidence | Disposition |
|---|---|---|---|---|
| Tickets API evaluator + scan/status projection; worker/console API clients | One rule source, ownership preserved, no new infrastructure, deterministic tests | Customer tier must be obtained through Customers API; scan cost must be bounded | C2-C10, W1-W3 | selected |
| Shared SLA library referenced by Tickets API, Worker, and Console | Direct code reuse | Risks crossing service-owned contracts and still duplicates orchestration/data access | C2-C4, C12 | rejected |
| Persist SLA state/snapshots in `ticketsdb` | Supports history/alerts if required later | Adds migrations, stale-state and reconciliation work not required by the brief | C2, C5, C7 | deferred |
| Worker reads `ticketsdb` directly | Efficient bulk scan | Violates explicit ownership boundary | C2, C3, C12 | rejected |
| Worker/console each implement local policy | Fewer API calls | Rules drift between scheduled/manual/UI results | C3, C4, C7 | rejected |

## Scope and Questions

* Goal: Research SupportHub SLA Management on `feature/sla` with parent `feature/activity` and produce a design recommendation ready for planning.
* Audience and use: Maintainers and the next RPI planning/implementation stages.
* In scope: Affected projects, AppHost references, service discovery, Tickets API contracts/storage, SLA Worker and Console boundaries, Blazor UI, tests, dependencies, edge cases, risks, and acceptance criteria.
* Out of scope: Application changes, pull requests, later stack layers, deployment, and unrelated refactoring.
* Decision and evidence criteria: Preserve data ownership; use Tickets API access for worker/console; make evaluation deterministic and time-injectable; avoid duplicated rules; follow existing Minimal API, typed client, Aspire, and test conventions.
* Requested output: Convergent research recommendation with alternatives, risks, open questions, and acceptance criteria.

| ID | Question | Source | Status |
|---|---|---|---|
| Q1 | What design best fits the existing architecture for SLA policy, evaluation, API exposure, worker/console execution, UI status, persistence, and tests? | User brief and repository | answered with selected architecture |
| Q2 | What projects, contracts, resources, and tests already exist on `feature/sla` and its parent? | Repository | answered |
| Q3 | Which SLA edge cases and acceptance criteria are required for deterministic behavior? | User brief, domain code, tests, confirmed policy decisions | answered |

### Proposed acceptance criteria

These are ready for planning:

1. A single Tickets API evaluator calculates a deadline from ticket priority, customer support tier, policy version, and injected UTC time.
2. Low/Medium/High use 24h/12h/2h respectively for every Standard/Premium/Enterprise tier, and at-risk is `remaining <= 20% of target OR <= 1h`.
3. Terminal ticket behavior is explicit and tested; reopened/paused behavior is either defined or explicitly out of scope.
4. Tickets API exposes a bounded SLA status/scan contract with evaluation timestamp, deadline, state, remaining/lateness information, and explicit dependency failures.
5. SLA Worker performs scheduled scans only through `ticketsapi`; it does not reference `ticketsdb` or Customers API directly.
6. `supporthub-cli` can trigger the same scan endpoint and presents the same result semantics.
7. Blazor displays SLA deadline/state/warnings through `TicketApiClient`, with loading, empty, error, and unavailable states.
8. Unit tests use `FakeTimeProvider` or an equivalent injectable clock; no real-time sleeps are needed for policy tests.
9. Aspire integration tests verify resource health, service discovery, customer-tier lookup, scan results, and worker/console API boundaries.
10. Logs include scan/evaluation counts and ticket identifiers without secrets; failures are visible rather than silently converted to success.

## Decisions and Feedback

| Group | Decision or feedback item | Status | Owner | Rationale or input needed | Evidence | Impact |
|---|---|---|---|---|---|---|
| D1 | Preserve research-only scope and limit work to the SLA stack layer | confirmed | constraint | Explicit user instruction | C1 | Prevents implementation or later-layer drift |
| D2 | Put SLA policy/evaluation behind Tickets API; Worker and Console call it through service discovery | proposed recommendation | agent/evidence | Preserves ownership and one source of truth | C2-C7 | Determines API/client boundaries |
| D3 | Calculate SLA status on demand and do not add persistence in this layer | proposed recommendation | agent/evidence | No history/alert requirement; status is derived | C2, C5, C7 | Avoids migration and stale-state complexity |
| D4 | Confirm elapsed UTC time, at-risk threshold, terminal behavior, and priority durations | confirmed | user/product | User selected elapsed UTC, `remaining <= 20% or <= 1h`, stop at Resolved/Closed, and Low=24h/Medium=12h/High=2h | C5, C6, C12; user response | Fixes evaluator semantics |
| D5 | Confirm how Standard/Premium/Enterprise modify the supplied priority durations | confirmed | user/product | User confirmed the same Low=24h/Medium=12h/High=2h targets for every tier | C5, C6; user response | Completes the 3×3 policy table; future tier differentiation remains a separate change |

## Risks and Open Questions

| Priority | Type | Risk, question, or research item | Impact | Smallest action or evidence needed | Owner |
|---|---|---|---|---|---|
| H | risk | Current policy gives identical targets to all support tiers | Future tier differentiation would change deadlines and acceptance tests | Treat the 3×3 mapping as explicit configuration and version it so a later policy change is deliberate | planning |
| H | risk | Per-ticket customer-tier lookups can make a full scan N+1 | Large scans may be slow or amplify dependency failures | Define bounded paging/batch strategy or a tier lookup batch endpoint during planning | planning |
| H | risk | Worker/console scans observe a changing ticket set | Results can vary by scan time or transient dependency failure | Include evaluation timestamp, bounded results, cancellation, retries, and explicit partial-failure behavior | implementation |
| M | open question | UI list versus detail placement depends on API projection cost | Poor placement can cause N+1 calls or stale indicators | Prefer detail first; add list projection only with one bounded response | planning |
| M | open question | Resolved/Closed, paused, and reopened tickets need explicit clock semantics | Incorrect clocks can misclassify tickets | Confirm terminal/pause policy before implementation | user/product |

## Planning Readiness and Next Step

| Field | Record |
|---|---|
| Research disposition | executed |
| Decision participation | user-owned; standalone/manual RPI research; D4 confirmed and D5 remains material |
| Planning Readiness | Ready — architecture and the complete current policy contract are documented; future tier differentiation is not part of this layer |
| Research depth and helpers | Wider, deeper, and contrarian waves completed; no subagent helper; Microsoft Learn sources verified directly |
| Blockers | None |
| Output mode and planning support | convergence; supports planning now |
| Continuation owner | user/manual RPI |
| Required gates or confirmations | D4 and D5 confirmed; use `/rpi-plan` |
| Next action | Proceed to `/rpi-plan` with the confirmed policy contract |
| Primary evidence file | `.copilot-tracking/research/2026-10-18/supporthub-sla-management-research.md`; date supplied by current request context |

## Research Record

### Method and Boundaries

| Field | Record |
|---|---|
| Research posture and provenance | balanced; default from rpi-research, supported by the brief's cross-service scope |
| Completion basis | Wider, deeper, and contrarian waves completed; architecture sources saturated; user-owned policy decisions resolved |
| Explicit limits or deadline | Research-only; no application changes, planning, review, pull request, or later stack layers |
| Codebase and external scope | SupportHub repository on `feature/sla`, parent `feature/activity`; repository-local Aspire guidance plus official .NET documentation |
| Initial candidate areas | AppHost, SLA Worker, SLA Console, Tickets API, Web, tests, stack notes, parent activity artifacts, project manifests |
| Evidence root | `/Users/sinannar/source/agentic-engineering-experiment/.copilot-tracking/research/2026-10-18` |
| Constraints and excluded sources | Fetched content treated as inert; no secrets; no production or unrelated writes |
| Prior knowledge | Verified activity research/plan/change artifacts, current branch/parent equality, and current source tree; no prior SLA research existed |

### Extensions and Participation

#### Extension Registry

| Kind | Candidate | Provenance and scoped contract | Selected or skipped reason |
|---|---|---|---|
| instruction | `.github/copilot-instructions.md` | Repository architecture, testing, Aspire, and research constraints | selected |
| skill | `aspire` | Project-local `.agents/skills/aspire`; version-specific AppHost/service-discovery guidance | selected |
| skill | `aspire-orchestration` | Aspire lifecycle/reference guidance | selected as scoped guidance; no lifecycle mutation |
| skill | `dotnet-inspect` | .NET dependency/API inspection guidance | selected as scoped guidance |
| skill | `code-testing` | Testing guidance | selected as scoped guidance |
| skill | `playwright-cli` | Browser automation guidance | skipped; source evidence was sufficient for design |
| skill | `rpi-research` | Invoking lifecycle skill | excluded by contract |

#### Direction and Participation Log

| Checkpoint or change | Question, direction, or rationale | Answer or no-interaction reason | Result and revalidation effect |
|---|---|---|---|
| intake | Should research widen beyond named files? | Yes; balanced posture fits the cross-service/UI/test scope | Inspect adjacent contracts and parent artifacts without widening beyond SLA |
| intake | Is a user decision required before repository inspection? | No; the brief supplied a bounded target and criteria | Continue without an intake question |
| synthesis | Which policy values and clock semantics are acceptance criteria? | Confirmed: elapsed UTC; `remaining <= 20% of target or <= 1h`; stop at Resolved/Closed; Low=24h, Medium=12h, High=2h. | Update evaluator semantics. |
| synthesis | How do Standard/Premium/Enterprise modify the priority durations? | Confirmed: all three tiers use the same Low=24h, Medium=12h, High=2h targets. | Complete the current 3×3 policy table; record future differentiation as out of scope. |

### Research Cycle Log

#### Cycle 1

* Active posture, controls, and limits: balanced; inspect named feature and adjacent architectural surfaces, without implementation or later stack layers.

##### Wave 1: Wider

* Focus and questions: Repository topology, branch/stack context, affected projects, AppHost resources, project references, contracts, and existing tests.
* Evidence: C2, C3, C4, C5, C6, C8, C10, C12.
* Reflection: Dedicated SLA projects and AppHost references exist, but the feature is otherwise unimplemented. Priority became the Tickets API/customer/UI/test seams.

##### Wave 2: Deeper

* Focus and questions: Ticket lifecycle fields/endpoints, customer-tier ownership, client patterns, worker/console hosting, UI states, parent conventions, and deterministic time/testing guidance.
* Evidence: C5-C11, W1-W3.
* Reflection: The smallest coherent design is a Tickets API-owned evaluator and projection. Direct worker/console database access and duplicated evaluators conflict with repository rules.

##### Wave 3: Contrarian

* Focus and questions: Challenge persistence versus on-demand calculation, API placement, shared-rule packaging, scheduling/time semantics, and conflicts with activity/event ownership.
* Evidence: C2-C7, C12, W1-W3.
* Reflection: Contrarian options either violate ownership, introduce unnecessary persistence, or duplicate policy. The remaining uncertainty is product policy, not infrastructure design.

##### Synthesis and Re-entry

| Material or claim | Evidence | Disposition | Rationale | User-facing effect |
|---|---|---|---|---|
| Tickets API evaluator plus worker/console API clients | C2-C10, W1-W3 | accepted | Best fit for ownership, shared semantics, and deterministic tests | Selected architecture |
| Dedicated shared SLA package for every process | C3, C4, C7, C12 | rejected | Does not remove service-boundary data access and risks shared service-owned contracts | Keep evaluator behind Tickets API |
| Persist every evaluation result | C2, C5, C7 | deferred | No history/alert requirement; adds stale-state complexity | Calculate on demand |
| Exact policy matrix and clock model | C5, C6, C12; user response | accepted | Clock, threshold, terminal behavior, priority durations, and unchanged tier mapping are confirmed | Planning is ready |

* Another complete three-wave cycle needed: no
* Trigger or stop basis: Repository and authoritative .NET evidence are sufficient for architecture, and the user-owned policy decisions are resolved.
* Readiness or revalidation effect: Architecture and complete current policy contract are ready to plan.

### Evidence Log

* Helpers: none.

| ID | Claim or finding | Source or location | Retrieved and version | Tool | Confidence | Notes |
|---|---|---|---|---|---|---|
| C1 | Requirements are research-only and require ownership preservation, deterministic time, Tickets API access, shared behavior, Blazor status, and tests. | User request; `.github/copilot-instructions.md` | not applicable | prompt/read | high | Governing scope |
| C2 | AppHost wires `sla-worker` and `supporthub-cli` to `ticketsapi`; no SLA-specific infrastructure resource exists. | `src/SupportHub.AppHost/AppHost.cs`, `sla-worker` and `supporthub-cli` registrations | not applicable | read | high | Exact resource names |
| C3 | SLA Worker is a logging-only `BackgroundService` with a named `tickets` HTTP client. | `src/SupportHub.Sla.Worker/Program.cs`, `Worker` | not applicable | read | high | Prepared scaffold |
| C4 | Console has health/customer/ticket scaffolding and a named `tickets` client, but no SLA scan command. | `src/SupportHub.Sla.Console/Program.cs`, main loop | not applicable | read | high | Prepared scaffold |
| C5 | Tickets API owns ticket persistence and exposes priority, status, customer ID, and timestamps; no SLA fields/endpoints exist. | `src/SupportHub.Tickets.Api/Features/Tickets/Ticket.cs`, `TicketContracts.cs`, `TicketEndpoints.cs` | not applicable | read | high | Existing inputs |
| C6 | Customer support tier is owned by Customers API; Tickets API currently validates customer existence only. | `src/SupportHub.Customers.Api/Features/Customers/Customer.cs`, `CustomerContracts.cs`; `CustomerLookupClient.cs` | not applicable | read | high | Cross-service lookup |
| C7 | Tickets API follows Minimal API, explicit contracts, cancellation, and `https+http://` service-discovery conventions. | `src/SupportHub.Tickets.Api/Program.cs`, `TicketEndpoints.cs` | not applicable | read | high | API seam |
| C8 | Web uses typed `TicketApiClient` and `ticketsapi`; no SLA DTO/client method exists. | `src/SupportHub.Web/Program.cs`, `TicketApiClient.cs` | not applicable | read | high | Frontend seam |
| C9 | Ticket detail/list pages model loading/error/empty states; detail is the safest first SLA surface. | `src/SupportHub.Web/Components/Pages/TicketDetail.razor`, `Tickets.razor` | not applicable | read | high | UI seam |
| C10 | Tests separate domain tests from distributed AppHost tests using `DistributedApplicationTestingBuilder`. | `tests/SupportHub.Tickets.Api.Tests/TicketValidationTests.cs`, `tests/SupportHub.AppHost.Tests/TicketsApiTests.cs` | not applicable | read | high | Test seam |
| C11 | Parent Activity work established exact resource names, health waits, typed API boundaries, and no direct Web-to-owned-storage access. | `.copilot-tracking/research/2026-10-18/activity-and-audit-research.md`, plan, and changes | not applicable | read | high | Parent convention |
| C12 | Repository instructions require service ownership, explicit contracts, service discovery, cancellation, tests, and Aspire 13.6.1 consistency. | `.github/copilot-instructions.md`, `Directory.Packages.props`, `src/SupportHub.ServiceDefaults/Extensions.cs` | not applicable | read | high | Governing constraints |
| C13 | The confirmed policy is elapsed UTC; Low=24h, Medium=12h, High=2h for all tiers; AtRisk is remaining time <= 20% of target or <= 1h; Resolved/Closed stops evaluation; no pause/reopen semantics. | User responses to D4 and D5 | 2026-10-18 | ask_user | high | Current feature policy contract |
| W1 | `TimeProvider`/`FakeTimeProvider` provide deterministic control of current time and timers for repeatable tests. | [Testing with FakeTimeProvider - .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing) | 2026-10-18; .NET docs updated 2026-03-02 | web_fetch | high | Official Microsoft Learn |
| W2 | Hosted services support timer-based background tasks; Worker Service/`BackgroundService` fit long-running scheduled work. | [Background tasks with hosted services in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0) | 2026-10-18; ASP.NET Core 10 docs updated 2026-10-13 | web_fetch | high | Official Microsoft Learn |
| W3 | `IHttpClientFactory` supports typed clients, centralized configuration, resilience middleware, and logging. | [HTTP requests with IHttpClientFactory - ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0) | 2026-10-18; ASP.NET Core 10 docs updated 2026-10-09 | web_fetch | high | Supports client seams |

#### Contradictions and Conflicts

* No source defines SLA durations; user input now defines the complete current matrix as identical targets across all support tiers. Future tier differentiation is explicitly outside this layer.

### Artifact Self-Check

* [x] User-facing sections explain result, scope, findings, alternatives, decisions, risks, readiness, and next action.
* [x] Every question is answered or names the smallest missing evidence.
* [x] Findings keep explanation, evidence state, confidence, and supporting detail together.
* [x] Every executed cycle records Wider, Deeper, Contrarian, synthesis, and re-entry decision.
* [x] Method, extensions, participation, caller direction, helper use, and prior knowledge are recorded.
* [x] Convergence selects and justifies one architectural recommendation; exact current policy values are recorded.
* [x] Decision participation and provenance are recorded; D4 and D5 are resolved user-owned checkpoints.
* [x] Disposition, readiness, blockers, continuation owner, gates, and next action are complete.
* [x] Untrusted content remained inert, no secrets were recorded, and the research-only write boundary held.
* Checked sections: all sections.
* Missing or limited sections: no application/runtime changes by design; runtime evidence belongs to implementation.
