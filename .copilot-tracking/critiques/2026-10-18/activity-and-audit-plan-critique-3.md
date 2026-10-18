<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub Activity and Audit

## Metadata

* Task ID: `supporthub-activity-and-audit`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: explicit user request for standard follow-up critique
* Critique type: follow-up
* Earlier critique: `.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the latest revised Activity and Audit implementation plan, reconcile `PC-001` through `PC-007`, and add only new substantive findings. The assessment must cover requirements, exact event contracts, executable outbox recovery/retention, the hosting gate, history status/body/lag semantics, tests, ownership, and runtime validation without modifying the plan or research.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/activity-and-audit-research.md`; `.github/copilot-instructions.md`; `notes/prompts/4-feature-activity.md`; `notes/stacks.md`.
* Decisions, dependencies, task Goals, and task Requirements considered: The revised plan's FR/NFR catalog, P01-P06 tasks and dependencies, P04-T00 hosting spike, P02-T03 maintenance service, P04-T02/P05-T02 history contract, test ownership, accountability matrix, runtime gates, acceptance criteria, and critique disposition.
* Assessment boundary: The supplied artifacts support a standard credibility assessment of implementation sequencing, contract completeness, operational recovery, repository test constraints, ownership, and validation gates. This critique does not execute the application, verify SDK signatures, or infer runtime behavior not evidenced by the supplied artifacts.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| Feature prompt and stack scope | Covered | The plan remains limited to Activity and Audit on `feature/activity`, using the existing queue, Cosmos database, backend history path, Blazor timeline, and distributed tests. |
| Research C1-C11 and W1-W5 | Covered | The selected outbox, event identity, ownership, partitioning, peek-lock, duplicate handling, emulator limitation, and Aspire test direction are carried forward. |
| FR-001 through FR-011 and NFR-001 through NFR-011 | Covered | The revised catalog and traceability table define and map the requirement IDs to tasks and evidence. |
| P01 event contracts and atomic outbox | Covered with residual contract detail risk | Typed event categories, null transitions, serialization tests, and same-unit-of-work persistence are binding; the plan still relies on implementation to materialize the full field catalog and JSON schema. |
| P02 publication and executable recovery/retention | Partial | `P02-T03` adds a maintenance service, claim recovery, replay, terminal signaling, and cleanup exclusions, but terminal-row discovery and replay initiation depend on configuration/restart behavior rather than a clearly queryable operator surface. |
| P03 consumer, Cosmos persistence, and history query | Partial | Consumer and idempotency behavior are credible, but `P03-T03` defers endpoint shape to P04 while `P04-T00` depends on completion of P03-T03, creating a sequencing cycle. |
| P04 hosting and backend history contract | Partial | The hosting spike is correctly present, and status/body/lag semantics are substantially bound; unknown-ticket `404` requires an authoritative ticket-existence check that is not assigned in the task details. |
| P05 Web client and timeline | Covered with dependency on P04 | The client and UI distinguish empty, lag, not-found, dependency failure, and bounded retry states once the backend contract is available. |
| P06 tests and runtime validation | Partial | The failure matrix and Aspire runtime evidence are specified, but the conditional dedicated Activity Worker test project conflicts with repository test-project constraints and may make ownership ambiguous. |
| Ownership and accountability | Partial | The accountability matrix is strong for phases and runtime evidence, but outbox operator discovery and authoritative ticket existence lack explicit implementation owners/mechanisms. |

## Verdict

* Verdict: Revise
* Rationale: The revision resolves the earlier traceability, hosting-gate, payload, failure-observability, accountability, and history-contract findings at the policy level. The plan is close to implementation-ready, but its task dependency graph contains a material P03/P04 cycle, outbox recovery is not fully executable for an operator who must discover terminal rows, the promised unknown-ticket response lacks an assigned existence check, and the conditional new test-project path conflicts with repository instructions.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| `PC-001` | resolved | The plan defines FR-001 through FR-011 and NFR-001 through NFR-011 and maps each to tasks and acceptance evidence. |
| `PC-002` | resolved | `P04-T00` is a blocking hosting spike that records host shape, binding, health, service URI, AppHost reference, and test path before dependent API/UI work. |
| `PC-003` | resolved | `P01-T01` binds typed details for created, updated, status, and assignment events, including null transitions and required transition tests. |
| `PC-004` | resolved | `P06-T01` defines a bounded failure matrix, injectable seams, expected state/settlement outcomes, and explicit unsupported-emulator recording. |
| `PC-005` | resolved | The accountability matrix assigns phase/task groups, runtime evidence, and topology decisions to accountable owners and artifacts. |
| `PC-006` | still open | `P02-T03` now names an executable maintenance service and safe cleanup rules, but it does not define how an operator queries or discovers terminal rows before supplying replay IDs; logs alone are not a durable recovery/query surface. |
| `PC-007` | resolved | `P04-T02`, `P05-T02`, and `P06-T02` bind `400`, `404`, `200` empty/populated, `503`/`502`, and client-side lag semantics with corresponding tests. |

## Findings

<!-- rpi:critique id=PC-006 -->
### PC-006 [Medium]: Outbox recovery still lacks a durable terminal-row discovery surface

* Related IDs: `P02-T02`, `P02-T03`, `P06-T01`, `P06-T03`, `FR-007`, `FR-011`, `NFR-011`
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` describes `OutboxMaintenanceService`, configuration-driven replay IDs, and operator-visible logs/health signals, but does not assign a query/report mechanism that lets an operator enumerate terminal rows and their failure state.
* Concern: Replay is only executable when the operator already knows the event ID and can change configuration or restart/refresh the service. A log signal is not a durable query surface and may be unavailable after retention, restart, or log aggregation gaps.
* Impact: Terminal event intent can remain technically retained but operationally undiscoverable, weakening the plan's claim of recoverable and diagnosable outbox behavior.
* Smallest useful change: Extend `P02-T03` with one bounded, owner-assigned terminal-row discovery mechanism compatible with the existing stack (for example, a protected internal maintenance query or a deterministic operator report/log projection), and define how its output feeds replay without changing `eventId`.
* Action owner: Planning parent with Tickets API/runtime owner
* Exact resolving evidence: A named discovery surface, retention/configuration behavior, and tests proving terminal rows can be enumerated, selected for replay, replayed with the original payload/identity, and excluded from cleanup until resolved.
* Decision route: Direct planner correction; user response is required only if the chosen surface expands the current topology or introduces a new externally exposed operational API.

<!-- rpi:critique id=PC-008 -->
### PC-008 [High]: P03/P04 task dependencies form a circular hosting-gate sequence

* Related IDs: `P03-T03`, `P04-T00`, `P04-T01`, `P04-T02`, `P06-T02`
* Evidence: `P03-T03` says the HTTP read contract and endpoint shape are finalized in P04 after Worker hosting verification, while `P04-T00` declares a blocking hosting spike but depends on `P03-T03`. The plan also makes `P04-T01` depend on both `P04-T00` and `P03-T03`.
* Concern: The plan cannot reach its required hosting decision without first completing a task that defers part of its work to that decision. This undermines the gate and permits endpoint/read-surface assumptions to be made before the host shape is resolved.
* Impact: Implementation may either deadlock at the planning sequence or prematurely implement a read surface that must be redesigned after hosting validation.
* Smallest useful change: Split `P03-T03` into hosting-independent Cosmos persistence/history-query logic and a later HTTP contract task, or move the hosting spike before the HTTP portion of P03-T03; make `P04-T00` depend only on the worker's persistence seam and package/project evidence.
* Action owner: Planning parent
* Exact resolving evidence: An acyclic dependency graph in which `P04-T00` completes before any task that requires the Worker HTTP shape, followed by explicit host/read endpoint and proxy tasks.
* Decision route: Direct planner correction unless the acyclic plan reveals that hybrid hosting is not viable and a new topology is required.

<!-- rpi:critique id=PC-009 -->
### PC-009 [Medium]: The promised unknown-ticket 404 has no authoritative existence-check task

* Related IDs: `P04-T02`, `P05-T01`, `P06-T02`, `FR-008`, `FR-009`, `NFR-001`
* Evidence: The binding contract in `P04-T02` promises `404 ProblemDetails` for an unknown ticket and `200` empty history for an existing ticket with no processed activity, but the Activity Worker owns only activity documents and the task does not state that Tickets API checks its ticket store before proxying.
* Concern: Activity storage alone cannot distinguish an unknown ticket from a valid ticket whose asynchronous activity has not arrived. Without an explicit Tickets API existence check, the implementation cannot reliably satisfy both the `404` and valid-empty/lag responses.
* Impact: Unknown tickets may be rendered as empty histories, while valid new tickets may be incorrectly reported as not found; backend and UI tests can pass against an ambiguous implementation.
* Smallest useful change: Add to `P04-T02` an explicit Tickets API ticket-existence lookup and failure mapping before the Activity Worker call, including cancellation and a test proving unknown, valid-empty, and downstream-failure states remain distinct.
* Action owner: Planning parent with Tickets API owner
* Exact resolving evidence: A task-level sequence and proxy test showing ticket existence is authoritative in Tickets API, activity absence returns `200` for a known ticket, and Worker/Cosmos failures return `503`/`502` without being converted to empty success.
* Decision route: Direct planner correction; no user decision is needed within the settled ownership model.

<!-- rpi:critique id=PC-010 -->
### PC-010 [Medium]: Conditional creation of an Activity Worker test project conflicts with repository test constraints

* Related IDs: `P06-T01`, `P06-T02`, Test Ownership and Validation Contract, `NFR-005`, `NFR-008`
* Evidence: `.github/copilot-instructions.md` states that all automated tests belong under the three existing projects in `tests/`, while `P06-T01` and its ownership table allow “a dedicated Activity Worker test project only if needed.”
* Concern: The plan leaves an implementation path that would create a fourth test project contrary to repository instructions and makes the canonical ownership of consumer/Cosmos tests conditional.
* Impact: Implementation can drift into an unsupported project layout or duplicate test setup, and approval/readiness cannot verify the intended test boundary.
* Smallest useful change: Remove the dedicated-project option from the plan and assign Activity Worker unit/consumer tests to the existing `SupportHub.AppHost.Tests` or `SupportHub.Tickets.Api.Tests` project according to the actual seam; if a new test project is genuinely unavoidable, record a separate user-approved repository-structure decision before implementation.
* Action owner: Planning parent with test owner
* Exact resolving evidence: An explicit existing test-project assignment for each consumer/Cosmos test category, with no unapproved fourth project in the maximum-additions contract.
* Decision route: Direct planner correction; user response is required only for an exception to the repository's three-project test constraint.

## Strengths and Residual Risk

* The selected outbox → Service Bus → Activity Worker → Cosmos → Tickets API proxy → Web path remains aligned with ownership and the supplied research.
* The revision materially improves requirements traceability, event details, failure tests, accountability, hosting gating, and history status/body/lag semantics.
* Runtime validation is appropriately retained as an implementation gate: actual health, logs, traces, SDK signatures, emulator behavior, and cloud-versus-emulator limitations are not claimed as proven by this critique.
* Residual risk remains around full field-level event schema materialization; the implementation must turn the typed contract descriptions into canonical serializable types and exact contract assertions.

## Questions or Blocking Evidence Gaps

* The P03/P04 dependency cycle must be removed before the hosting gate can function as written.
* The plan needs a durable terminal-row discovery path and an explicit Tickets API existence check to make its recovery and history contracts executable.
* The test-project exception must be removed or escalated as a repository-structure decision.
* Exact Aspire 13.6.1 SDK signatures and runtime health remain implementation-time validation gates, not additional research requests.

## Limitations

* This was a standard, follow-up, read-only critique using only the latest plan, research artifact, critiques 1 and 2, repository instructions, feature prompt, and stack notes.
* No application source files, SDK signatures, running AppHost, logs, traces, or tests were executed or inspected beyond the supplied artifacts.
* The plan and research artifact were not modified.

## Recommended Next Action

* Highest-impact finding: `PC-008`
* Action owner: Planning parent
* Smallest next action: Break the P03/P04 dependency cycle so the hosting spike precedes all Worker HTTP-shape assumptions; then close `PC-006`, `PC-009`, and `PC-010` in the same plan revision.
* User response required: No for these planner corrections; yes only if resolving the hosting or test constraints requires a new service/topology or an exception to the repository's test-project rule.

## Relevant Artifacts

| Artifact | Description |
|----------|-------------|
| [.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md](.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md) | Latest revised implementation plan assessed by this follow-up critique |
| [.copilot-tracking/research/2026-10-18/activity-and-audit-research.md](.copilot-tracking/research/2026-10-18/activity-and-audit-research.md) | Research and selected architecture |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md) | Initial critique reconciled above |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md) | Previous follow-up critique reconciled above |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture, ownership, test, Aspire, and validation rules |
| [notes/prompts/4-feature-activity.md](notes/prompts/4-feature-activity.md) | Feature requirements and acceptance direction |
| [notes/stacks.md](notes/stacks.md) | Stack position and parent/child scope |

## Next Steps

* Revise the plan to remove the P03/P04 dependency cycle, close the remaining executable outbox recovery gap, bind the ticket-existence check, and conform test ownership to the repository's three existing test projects. Then run the eligible standard follow-up critique before `/rpi-implement`.
