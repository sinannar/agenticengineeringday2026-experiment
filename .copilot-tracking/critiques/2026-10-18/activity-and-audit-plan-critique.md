<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub Activity and Audit

## Metadata

* Task ID: `supporthub-activity-and-audit`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: explicit user request for standard critique
* Critique type: initial
* Earlier critique: not applicable; no earlier critique supplied

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the complete Activity and Audit implementation plan for the `feature/activity` stack layer, without changing the plan or conducting open-ended research.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/activity-and-audit-research.md`; `.github/copilot-instructions.md`; `notes/prompts/4-feature-activity.md`; `notes/stacks.md`.
* Decisions, dependencies, task Goals, and task Requirements considered: The plan's D1-D5 decisions, P01-P06 phases, P01-T01 through P06-T03 tasks, acceptance criteria, dependency table, risk table, test ownership, implementation gates, and open questions.
* Assessment boundary: The supplied plan and artifacts support an architecture and implementation-readiness assessment across requirements, research recommendations, phases/tasks, dependencies, risks, ownership, tests, Aspire runtime validation, and unresolved API gates. This critique does not verify repository code or SDK behavior because no implementation-time research or runtime execution was authorized.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| Confirmed feature scope in `notes/prompts/4-feature-activity.md` | Covered | Four lifecycle event categories, queue, Cosmos audit, backend history, Blazor timeline, and end-to-end testing are represented across P01-P06. |
| Research recommendations C1-C11, W1-W5 | Covered | Outbox, event identity, partitioning, peek-lock settlement, ownership boundary, emulator limitation, and Aspire test seams are carried into D1-D4 and the phase details. |
| FR-001 through FR-011 and NFR-001 through NFR-011 | Partial | The plan references these IDs throughout, but neither the plan nor supplied research defines a requirements catalog. Traceability and completeness cannot be independently checked from the supplied evidence. |
| P01 event contracts and atomic outbox | Partial | The outbox and event trigger design is credible, but `ticket.updated` payload semantics remain open and the atomic rollback proof is described rather than tied to a concrete failure-injection evidence path. |
| P02 publication, retries, retention | Partial | At-least-once publication and retry state are planned; concrete claim/lease, retry, terminal-recovery, and cleanup evidence remains implementation-time work. |
| P03 consumer, Cosmos persistence, idempotency | Covered | Event-keyed IDs, `/ticketId` partitioning, conflict handling, settlement outcomes, and duplicate tests are explicitly planned. |
| P04-P05 history API and Web timeline | Partial | Ownership and service-discovery direction are sound, but the Worker hybrid HTTP hosting shape and empty-versus-lag contract are unresolved gates that can alter the implementation path. |
| P06 tests and runtime validation | Partial | Vertical-flow, replay, health, logs, and traces are required; deterministic failure-path injection and exact runtime evidence criteria are not fully specified. |
| Ownership and scope boundaries | Partial | Project ownership is stated for test concerns and data, but phase/task and risk-mitigation ownership is not consistently assigned. |

## Verdict

* Verdict: Revise
* Rationale: The selected architecture is consistent with the research and repository boundaries, and most functional, reliability, and test coverage is strong. Revision is required before implementation because requirements traceability is not auditable, the Worker HTTP-hosting gate can invalidate P04-P05, and several operational/test decisions lack explicit resolving evidence and owners.

## Findings

<!-- rpi:critique id=PC-001 -->
### PC-001 [High]: FR/NFR traceability is not grounded by a supplied requirements catalog

* Related IDs: `FR-001`-`FR-011`, `NFR-001`-`NFR-011`, P01-P06
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` references FR/NFR IDs in each task but defines no FR/NFR catalog; `notes/prompts/4-feature-activity.md` supplies requirements as bullets without those identifiers.
* Concern: The plan claims full requirement-level traceability, but the meaning and completeness of the referenced IDs cannot be checked from the supplied artifacts. A missed requirement could therefore appear covered merely because an undefined ID is repeated.
* Impact: Implementation and review cannot reliably prove that all confirmed requirements and non-functional guarantees are covered, especially around delivery, observability, and failure handling.
* Smallest useful change: Add a compact requirements table to the plan defining each FR/NFR from the prompt and research, then map each definition to its acceptance criterion and owning task.
* Action owner: Planning parent
* Exact resolving evidence: A plan-local FR/NFR catalog whose definitions are traceable to `notes/prompts/4-feature-activity.md` and the research findings, with every ID mapped to at least one task and acceptance criterion.
* Decision route: Direct planner correction

<!-- rpi:critique id=PC-002 -->
### PC-002 [High]: Worker HTTP hosting is an architectural gate but is not an explicit pre-implementation stop

* Related IDs: D3, P04-T01, P04-T02, P05-T01, P06-T02
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` says the Worker is currently a generic host, requires hybrid-host validation, and otherwise requires a smallest alternative, while P04-T02 already specifies `https+http://activity-worker` and a proxy dependency.
* Concern: The plan allows implementation to proceed with a route and service-discovery shape that may be impossible for the current Worker project. The fallback is only described as something to record, not as a gated decision that must occur before P04/P05 contracts and AppHost wiring are finalized.
* Impact: A failed hosting validation can force a new project or different API boundary, invalidating task dependencies, test ownership, route contracts, and runtime topology after earlier work has begun.
* Smallest useful change: Add a blocking “P04 hosting spike” before P04-T01/P04-T02 that records the supported host shape, endpoint/health binding, AppHost reference, and selected fallback before any proxy or Web contract is implemented.
* Action owner: Planning parent
* Exact resolving evidence: A buildable hosting/API decision recorded in the plan or changes record, plus an AppHost test proving `activity-worker` reaches health and accepts the history request through the selected service-discovery URI; if hybrid hosting fails, an explicitly approved ownership-preserving alternative.
* Decision route: Direct planner correction unless the fallback changes project topology, in which case obtain a significant user decision before implementation.

<!-- rpi:critique id=PC-003 -->
### PC-003 [Medium]: Event payload semantics are not deterministic enough for a binding contract

* Related IDs: P01-T01, P01-T03, P05-T02, `FR-001`, `FR-002`, `NFR-005`
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` calls the envelope binding but says `ticket.updated` “should identify the changed representation needed by the timeline,” while the research records full snapshot versus field diff as an unresolved policy choice.
* Concern: The consumer, Cosmos document, API DTO, UI labels, and semantic tests can diverge if the exact `ticket.updated` details and status/assignment representations are chosen independently during implementation.
* Impact: Equivalent updates may produce incompatible documents or an incomplete timeline, and contract tests cannot prove compatibility across publisher, consumer, API, and Web.
* Smallest useful change: Select and document one exact typed details shape per event, preferably a field-diff/before-after contract, including null transitions, serialization names, and the rendering rule for unknown future details.
* Action owner: Planning parent
* Exact resolving evidence: Versioned contract examples and tests asserting the exact JSON/details shape for create, ordinary update, status-only, assignment-only, combined, and null-assignment transitions.
* Decision route: Direct planner correction; user input is only needed if product policy requires a different audit representation.

<!-- rpi:critique id=PC-004 -->
### PC-004 [Medium]: Failure observability lacks a deterministic validation path

* Related IDs: P02-T02, P03-T02, P06-T01, P06-T02, P06-T03, `FR-007`, `FR-011`, `NFR-002`, `NFR-004`
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` requires a representative failure/retry path in P06-T03 and failure handling in P06-T02, but provides no controlled fault-injection seam or exact evidence for forcing publisher, Cosmos, malformed-message, and dead-letter outcomes. The research explicitly notes that runtime health/log/trace evidence was unavailable and that narrow seams may be needed.
* Concern: A green happy-path Aspire test and passive log inspection may leave retry, abandonment, dead-letter, and terminal outbox behavior unproven, while the acceptance criteria require observable failure handling.
* Impact: The implementation could claim the requested reliability semantics without demonstrating them, especially against emulator limitations and transient dependency failures.
* Smallest useful change: Define a bounded failure-test matrix with the narrowest injectable seams (or explicitly mark which failures are unit-only), expected settlement/state transition, and required log/trace fields; make P06-T03 collect those artifacts where runtime injection is supported.
* Action owner: Planning parent with implementation test owner
* Exact resolving evidence: Tests or documented runtime checks that force each supported failure class and assert retry/abandon/dead-letter or terminal-outbox state plus event/ticket correlation in logs/traces; unsupported emulator cases are explicitly recorded as unverified rather than implied.
* Decision route: Direct planner correction

<!-- rpi:critique id=PC-005 -->
### PC-005 [Medium]: Operational ownership for tasks, risks, and unresolved gates is incomplete

* Related IDs: P01-P06, Dependencies and Integration Points, Risks and Mitigations, Open Questions and Follow-Up Items
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` names project ownership for test concerns, but phase/task entries, risk mitigations, and implementation-time gates do not consistently identify an accountable owner or disposition authority.
* Concern: The plan can be technically complete yet leave no accountable party for deciding retry/retention thresholds, validating SDK APIs, resolving host shape, capturing runtime evidence, or closing residual risks.
* Impact: Decision-critical gates may be deferred silently and the stacked PR can reach implementation without a clear owner for required evidence.
* Smallest useful change: Add an owner column or ownership matrix covering every phase/task, each high risk, each open question, and P06 runtime evidence, distinguishing planner, Tickets API, Activity Worker, Web, AppHost-test, and user decision ownership.
* Action owner: Planning parent
* Exact resolving evidence: Every Pxx-Txx and every high/medium risk/open question has one accountable project/role, a completion artifact, and a disposition route before `/rpi-implement`.
* Decision route: Direct planner correction

<!-- rpi:critique id=PC-006 -->
### PC-006 [Medium]: Outbox recovery and retention are stated as policies but not operationally closed

* Related IDs: P01-T02, P02-T01, P02-T02, P06-T03, `FR-004`, `FR-007`, `FR-011`, `NFR-003`, `NFR-006`
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` requires pending/in-progress/published/terminal states and bounded retention, but leaves claim/lease, retry ceiling, replay/reconciliation, cleanup execution, and operator visibility as implementation-time choices. The research identifies missing outbox cleanup/reconciliation as a high risk.
* Concern: A terminal row can remain permanently unresolved or published rows can grow indefinitely without a concrete recovery and cleanup mechanism, even though the plan promises diagnosable at-least-once behavior.
* Impact: Committed ticket changes may remain invisible after transient or configuration failures, and the database can accumulate unbounded operational state without a supported replay path.
* Smallest useful change: Specify the minimal lifecycle and operator behavior: claim expiry, retry ceiling/backoff, terminal-row alert/query or replay mechanism preserving `eventId`, and safe cleanup criteria for published rows.
* Action owner: Planning parent
* Exact resolving evidence: Configuration and tests proving claim recovery, retry-to-terminal transition, operator-visible failed rows/replay, and cleanup that never removes unpublished or unresolved intent; P06 runtime evidence shows the corresponding logs/metrics.
* Decision route: Direct planner correction; user decision is required only if recovery requires a new operational component outside the current stack scope.

## Strengths and Residual Risk

* The selected outbox → Service Bus → Activity Worker → Cosmos → Tickets API proxy → Web path follows the supplied ownership rules and research recommendation.
* The plan correctly rejects exactly-once and strict ordering claims, preserves stable event identity, treats duplicate delivery as expected, and includes explicit eventual-consistency UI behavior.
* Residual emulator risk is appropriately acknowledged: local Service Bus restart behavior cannot prove production durability. This remains acceptable if the implementation records the limitation and supplies unit/contract evidence for the untestable production semantics.

## Questions or Blocking Evidence Gaps

* The supplied artifacts do not define the FR/NFR identifiers used by the plan; the planner must add that traceability before implementation.
* The Worker hosting/API validation is decision-critical. No additional open-ended research is requested here; the implementation gate must produce the exact build/runtime evidence or route a topology change for user decision.

## Limitations

* This was a standard, initial, read-only critique using only the supplied plan, research artifact, repository instructions, feature prompt, and stack notes.
* No application files, SDK signatures, running AppHost, logs, traces, or tests were inspected or executed. Those are implementation-time validation evidence, not assumptions in this critique.
* No earlier critique was supplied, so there is no finding reconciliation section.

## Recommended Next Action

* Highest-impact finding: `PC-001`
* Action owner: Planning parent
* Smallest next action: Add the FR/NFR catalog and mappings, then add the blocking Worker-hosting spike and explicit failure/ownership evidence gates before marking the plan Ready.
* User response required: No for the listed planner corrections; yes only if hosting validation requires a new project or other topology change outside the settled scope.
