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
* Earlier critique: `.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the revised Activity and Audit implementation plan for `feature/activity`, reconcile every earlier finding `PC-001` through `PC-006`, and identify only new substantive concerns without modifying the plan or research.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/activity-and-audit-research.md`; `.github/copilot-instructions.md`; `notes/prompts/4-feature-activity.md`; `notes/stacks.md`.
* Decisions, dependencies, task Goals, and task Requirements considered: The revised plan's D1-D5 decisions, FR-001 through FR-011, NFR-001 through NFR-011, P01-P06 tasks, dependencies, risks, acceptance criteria, test ownership, accountability matrix, implementation gates, open questions, and critique disposition.
* Assessment boundary: The supplied artifacts support a complete standard implementation-readiness assessment across architecture, requirements traceability, phase/task coverage, reliability, ownership, tests, Aspire wiring, and runtime gates. This critique does not verify application code, SDK signatures, or runtime behavior because those remain implementation-time evidence.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| Feature prompt scope and stack boundary | Covered | Four lifecycle events, queue, Activity Worker, Cosmos `activitydb`, backend history, Blazor timeline, and end-to-end testing are represented without adding later SLA/dashboard scope. |
| Research C1-C11 and W1-W5 | Covered | Ownership, outbox, event identity, partitioning, peek-lock settlement, emulator limits, and Aspire test seams are reflected in decisions and phase tasks. |
| FR-001 through FR-011 and NFR-001 through NFR-011 | Covered | The revised plan defines a local catalog, task mappings, and acceptance evidence for every identifier. |
| P01 contracts and atomic outbox | Covered | Exact typed event details, null transitions, serialization tests, same-unit-of-work persistence, and failed-update coverage are now binding. |
| P02 publication, retry, recovery, and retention | Partial | Lease expiry, retry ceiling, terminal state, replay identity, and safe cleanup are stated, but the concrete operator query/replay/cleanup mechanism and its owning implementation/test task remain unspecified. |
| P03 consumer, Cosmos persistence, and idempotency | Covered | Event-keyed documents, partition-scoped history, conflict handling, settlement outcomes, and duplicate tests are explicit. |
| P04-P05 history API and Web timeline | Partial | Hosting is now gated, but valid-ticket/no-activity versus not-found/lag behavior remains an implementation-time choice that crosses backend and UI contracts. |
| P06 tests and runtime validation | Covered with implementation limitation | The failure matrix, duplicate replay, bounded polling, diagnostics, and emulator limitations are specified; actual evidence remains for implementation. |
| Ownership and accountability | Covered | The accountability matrix assigns phase groups, runtime evidence, and topology decisions. |

## Verdict

* Verdict: Revise
* Rationale: The revision resolves the traceability, hosting-gate, payload, failure-matrix, and accountability findings. The plan is otherwise architecturally credible and aligned with the supplied research. Revision is still required because outbox recovery/retention is not operationally implementable from the task set as written, and the history endpoint's empty/not-found/lag contract is still left to implementation-time choice.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| `PC-001` | resolved | The plan now defines FR-001 through FR-011 and NFR-001 through NFR-011, maps them to owning tasks, and names acceptance evidence in `Requirement-to-Task Traceability`. |
| `PC-002` | resolved | `P04-T00` is an explicit blocking hosting spike; it records host shape, binding, health, service URI, AppHost reference, and test path before P04/P05/P06 API work. |
| `PC-003` | resolved | `P01-T01` binds exact typed details for created, updated, status, and assignment events, including null transitions and required transition tests. |
| `PC-004` | resolved | `P06-T01` supplies a bounded failure matrix with injectable seams and expected outbox, settlement, dead-letter, and diagnostic outcomes; P06-T03 requires runtime evidence where supported and records emulator limits. |
| `PC-005` | resolved | The accountability matrix assigns each phase/task group, runtime evidence, and topology-change decision to an accountable owner with a completion artifact. |
| `PC-006` | still open | `P02-T02` now states lease, retry, terminal, replay, and cleanup semantics, but no task specifies the operator-visible query/replay mechanism or implements/tests safe cleanup. The earlier concern is narrowed from policy absence to operational closure. |

## Findings

<!-- rpi:critique id=PC-006 -->
### PC-006 [Medium]: Outbox recovery and retention still lack an executable operator surface

* Related IDs: `P02-T01`, `P02-T02`, `P06-T01`, `P06-T03`, `FR-007`, `FR-011`, `NFR-009`, `NFR-011`
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` requires terminal rows to be queryable and replayable with the original `eventId`, and requires safe cleanup, but the phase tasks only say the implementer must provide those behaviors. No endpoint, console command, maintenance operation, scheduled cleanup task, or explicit test target is assigned.
* Concern: The plan has converted the lifecycle into policy language without identifying how an operator discovers terminal rows, initiates replay, or verifies that published-row cleanup is safe.
* Impact: A permanently failed event can remain invisible or unrecoverable, while retention can remain unbounded or be implemented unsafely; this undermines the promised durable-event and diagnosable-recovery behavior.
* Smallest useful change: Add one explicit recovery/retention task under P02 or P06 that selects the minimal supported operator mechanism for this stack (for example, a bounded internal maintenance operation plus a scheduled cleanup routine), names its owner, and defines tests for terminal visibility, replay preserving `eventId`, claim expiry, and cleanup exclusion rules.
* Action owner: Planning parent with Tickets API/runtime owner
* Exact resolving evidence: A named implementation surface and task, configuration for retry/retention thresholds, tests proving terminal query/replay and claim recovery, and cleanup tests proving pending, in-progress, and terminal-unresolved rows are never deleted.
* Decision route: Direct planner correction; user response is required only if the mechanism adds a new externally exposed operational component beyond the current stack scope.

<!-- rpi:critique id=PC-007 -->
### PC-007 [Medium]: History consistency and not-found semantics remain an unbound cross-service contract

* Related IDs: `P03-T03`, `P04-T02`, `P05-T01`, `P05-T02`, `P06-T02`, `FR-008`, `FR-009`, `NFR-006`
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` requires explicit empty/not-found/error responses, but `P04-T02` says the implementation may validate ticket existence or pass through an empty list and instructs the implementer to choose the behavior; the same choice is repeated in the Open Questions table. `P05-T02` separately defines a lag state without a binding response/status contract.
* Concern: The proxy, typed client, UI, and end-to-end test can independently interpret an empty history response as a valid ticket with asynchronous lag, an unknown ticket, or a dependency failure. The plan therefore does not yet bind the observable contract that the timeline and bounded polling must implement.
* Impact: A missing ticket can render as a misleading empty timeline, or a valid newly created ticket can render as not-found; retries and Aspire assertions may become inconsistent across services.
* Smallest useful change: Choose and document one contract before P04-T02/P05-T01: status/body semantics for unknown ticket, valid ticket with no activity, activity lag, and downstream failure, including the bounded polling trigger and terminal UI state. Add one proxy contract test and one AppHost assertion for each meaningful state.
* Action owner: Planning parent with Tickets API/Web owners
* Exact resolving evidence: Versioned endpoint/client response examples, explicit status mapping, UI state mapping, and distributed tests distinguishing not-found, empty, lag/timeout, and dependency failure.
* Decision route: Direct planner correction unless product policy requires a new user-visible distinction; no new infrastructure is otherwise required.

## Strengths and Residual Risk

* The revised plan now has auditable requirement traceability, a genuine hosting gate, deterministic event payloads, a bounded failure matrix, and explicit accountability.
* The selected outbox → Service Bus → Activity Worker → Cosmos → Tickets API proxy → Web path remains consistent with ownership and research evidence.
* Residual emulator risk is correctly retained: local Service Bus restart behavior cannot prove cloud durability, so implementation evidence must label that limitation rather than overclaiming.

## Questions or Blocking Evidence Gaps

* The plan must identify an executable operator mechanism for outbox terminal-row query/replay and safe cleanup before implementation can claim operational recovery.
* The backend/UI history contract must be fixed before dependent API and UI tasks are implemented; no additional open-ended research is required, but the selected behavior must be recorded and tested.
* Exact Aspire 13.6.1 SDK signatures and runtime health remain implementation-time validation, not blockers to this critique.

## Limitations

* This was a standard, follow-up, read-only critique using only the supplied revised plan, original research artifact, earlier critique, repository instructions, feature prompt, and stack notes.
* No application source files, SDK signatures, running AppHost, logs, traces, or tests were inspected or executed.
* The critique does not modify the plan, research artifact, earlier critique, or application code.

## Recommended Next Action

* Highest-impact finding: `PC-006`
* Action owner: Planning parent with Tickets API/runtime owner
* Smallest next action: Add one explicit outbox recovery/retention implementation task with an operator surface and tests, then bind the history endpoint's empty/not-found/lag/error contract before implementation handoff.
* User response required: No for these planner corrections; yes only if the chosen recovery surface or history policy expands the settled stack topology or product scope.

## Relevant Artifacts

| Artifact | Description |
|----------|-------------|
| [.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md](.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md) | Revised implementation plan assessed by this follow-up critique |
| [.copilot-tracking/research/2026-10-18/activity-and-audit-research.md](.copilot-tracking/research/2026-10-18/activity-and-audit-research.md) | Original research and selected architecture |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md) | Earlier critique reconciled above |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture, ownership, Aspire, and validation rules |
| [notes/prompts/4-feature-activity.md](notes/prompts/4-feature-activity.md) | Feature requirements and acceptance direction |
| [notes/stacks.md](notes/stacks.md) | Stack position and parent/child scope |

## Next Steps

* Revise the plan to close `PC-006` and `PC-007`, then run the eligible standard follow-up critique again before `/rpi-implement`.
