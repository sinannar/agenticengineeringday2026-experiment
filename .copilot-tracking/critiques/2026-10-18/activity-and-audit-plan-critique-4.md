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
* Earlier critique: `.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-3.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the latest revised Activity and Audit implementation plan, reconcile every earlier finding `PC-001` through `PC-010`, and add only new substantive findings. Particular attention was given to the P03/P04 dependency graph, executable outbox recovery, authoritative ticket existence, the three-project test constraint, exact event/history contracts, and implementation/runtime gates.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/activity-and-audit-research.md`; `.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md`; `.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md`; `.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-3.md`; `.github/copilot-instructions.md`; `notes/prompts/4-feature-activity.md`; `notes/stacks.md`.
* Decisions, dependencies, task Goals, and task Requirements considered: The revised plan's FR/NFR catalog, D1-D5 decisions, P01-P06 tasks, dependencies, outbox maintenance surface, P04-T00 hosting gate, P04-T02 history contract, test ownership, accountability matrix, acceptance criteria, runtime validation, and critique disposition.
* Assessment boundary: The supplied artifacts support a complete standard credibility assessment of architecture, sequencing, contracts, operational recovery, test ownership, and implementation/runtime gates. This critique does not execute the application, verify SDK signatures, or treat absent runtime evidence as fact.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| Feature prompt and stack boundary | Covered | The plan remains limited to Activity and Audit on `feature/activity`, reuses the existing queue and Cosmos database, and excludes later SLA/dashboard layers. |
| Research C1-C11 and W1-W5 | Covered | Outbox publication, stable identity, Activity Worker ownership, partition-scoped Cosmos access, peek-lock settlement, emulator limits, and Aspire testing are carried forward. |
| FR-001 through FR-011 and NFR-001 through NFR-011 | Covered | The local catalog, task mappings, and acceptance evidence make the requirement set auditable. |
| P01 event contracts and atomic outbox | Partial | Event categories and typed detail categories are named, but the canonical field catalog and serialized history item schema are not fully materialized for independent publisher/consumer/API/Web implementation. |
| P02 publication, recovery, and retention | Covered | `P02-T03` now names a maintenance service, terminal-row enumeration/replay endpoints, claim recovery, cleanup exclusions, and tests. |
| P03/P04 dependency sequencing | Covered | `P03-T03` is explicitly hosting-independent; `P04-T00` gates the HTTP shape and precedes P04/P05 HTTP work. The dependency graph is acyclic as written. |
| P04 authoritative history semantics | Covered | `P04-T02` assigns the Tickets API existence check and distinguishes `400`, authoritative `404`, valid-empty `200`, downstream `503`, and malformed `502`. |
| Three-project test ownership | Partial | The fourth-project option is removed, but the plan does not identify an executable existing-project mechanism for testing Blazor timeline state transitions, especially lag behavior. |
| P06 implementation and runtime gates | Partial | Build/test/runtime evidence is required, but a blocked Aspire runtime is only recorded as a limitation; the plan does not explicitly prohibit readiness or handoff until required distributed evidence exists or an exception is approved. |

## Verdict

* Verdict: Revise
* Rationale: All ten earlier findings are addressed at the policy and sequencing level, including the formerly circular P03/P04 path, outbox terminal discovery/replay, authoritative ticket existence, and the three-project prohibition. The plan is close to implementation-ready, but exact cross-service wire contracts, executable ownership of UI-state tests, and the stop condition for missing Aspire runtime evidence remain material credibility gaps.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| `PC-001` | resolved | FR-001 through FR-011 and NFR-001 through NFR-011 are defined in a plan-local catalog, mapped to tasks, and tied to acceptance evidence. |
| `PC-002` | resolved | `P04-T00` is a blocking hosting spike that records host shape, binding, health, service URI, AppHost reference, and test path before HTTP-shape work. |
| `PC-003` | resolved | `P01-T01` binds typed detail categories, JSON casing intent, null transitions, and required transition tests; the residual schema-materialization gap is narrower and recorded as new `PC-011`. |
| `PC-004` | resolved | `P06-T01` defines a bounded failure matrix with injectable seams, expected state/settlement outcomes, and explicit unsupported-emulator recording. |
| `PC-005` | resolved | The accountability matrix assigns phase/task groups, runtime evidence, and topology decisions to accountable owners and completion artifacts. |
| `PC-006` | resolved | `P02-T03` specifies an executable maintenance service, terminal-row enumeration, replay by original event ID, claim recovery, cleanup exclusions, and deterministic tests. |
| `PC-007` | resolved | `P04-T02`, `P05-T02`, and `P06-T02` bind backend status/body semantics and client-side lag behavior. |
| `PC-008` | resolved | `P03-T03` contains hosting-independent persistence/query logic, while `P04-T00` depends on that seam and gates all Worker HTTP-shape tasks; no circular dependency remains. |
| `PC-009` | resolved | `P04-T02` explicitly requires the Tickets API to check its authoritative ticket store before calling the Activity Worker and tests unknown versus valid-empty tickets. |
| `PC-010` | resolved | `P06-T01` and the test-ownership table prohibit a fourth project and assign Worker tests to `SupportHub.AppHost.Tests` and ticket/outbox tests to `SupportHub.Tickets.Api.Tests`. |

## Findings

<!-- rpi:critique id=PC-011 -->
### PC-011 [High]: Event and history wire schemas are named but not canonical enough for independent implementation

* Related IDs: `P01-T01`, `P03-T03`, `P04-T01`, `P04-T02`, `P05-T01`, `P06-T01`, `P06-T02`, `FR-001`, `FR-008`, `NFR-005`
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` names `snapshot`, `changes`, and before/after detail shapes, and gives the history envelope `{ "ticketId": "...", "items": [] }`, but it does not enumerate the snapshot fields, changed-field names/types, history item fields, nullability, casing, pagination/limit representation, or exact ProblemDetails mapping in canonical examples or a single contract artifact.
* Concern: The plan resolves the category-level ambiguity identified by `PC-003`, but publisher, consumer, Activity Worker read endpoint, Tickets API proxy, Web client, and UI can still choose incompatible serialized fields while each claims conformance to the abbreviated description.
* Impact: Cross-service compilation and semantic tests may pass locally while the end-to-end history payload loses display data, changes meaning across versions, or cannot be deserialized consistently.
* Smallest useful change: Add one canonical contract section or referenced source-of-truth artifact containing exact JSON examples/schema for all four event types and the history response/items, including field names, casing, types, nullability, deterministic ordering/limit, and `400`/`404`/`502`/`503` error bodies. Require contract tests to assert the serialized wire shape.
* Action owner: Planning parent with Tickets API and Activity Worker owners
* Exact resolving evidence: Versioned event and history JSON examples or schema consumed by all owning tasks, plus serialization/proxy tests that assert the exact fields and status/error bodies.
* Decision route: Direct planner correction; no user decision is required within the settled architecture.

<!-- rpi:critique id=PC-012 -->
### PC-012 [Medium]: Blazor state-test ownership is not executable under the three-project constraint

* Related IDs: `P05-T02`, `P06-T01`, `P06-T02`, Test Ownership and Validation Contract, `FR-009`, `NFR-008`
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` requires tests for loading, empty, success, lag, retryable-error, and not-found UI states, but assigns Blazor tests to "`SupportHub.AppHost.Tests` or existing supported Web test surface" without identifying a current Web test project, rendering harness, or concrete test seam. Repository instructions permit only the three existing test projects.
* Concern: The plan has removed the prohibited fourth-project path but has not shown how the UI-specific acceptance evidence will be produced. AppHost HTTP tests can prove backend responses and cannot by themselves prove `TicketDetail.razor` state transitions or the bounded lag retry behavior.
* Impact: The implementation may silently omit UI-state coverage or add unsupported test infrastructure late, weakening the explicit eventual-consistency acceptance criteria.
* Smallest useful change: Assign UI-state verification to one existing project and name the supported mechanism (for example, a component-rendering test seam in an existing test project), or explicitly redefine P06 as backend/distributed evidence plus a documented non-automated UI gate with an owner and required artifact.
* Action owner: Planning parent with Web/test owner
* Exact resolving evidence: A named existing test project, concrete component/client seam, and passing tests or an explicitly approved documented verification artifact for all required UI states, including lag timeout and manual refresh.
* Decision route: Direct planner correction; user response is required only if satisfying the UI gate would require a new test project contrary to repository instructions.

<!-- rpi:critique id=PC-013 -->
### PC-013 [High]: Runtime-gate failure does not explicitly block readiness or implementation handoff

* Related IDs: `P04-T00`, `P06-T02`, `P06-T03`, Planning Readiness, Acceptance Criteria, `FR-010`, `FR-011`, `NFR-002`
* Evidence: `P06-T03` requires the AppHost resources, event flow, logs, traces, and a representative failure path to be inspected, but says that if startup is blocked the implementation should record the blocker and preserve unit/integration evidence. The plan does not state that readiness remains blocked or that `/rpi-implement`/stack handoff is prohibited without the required distributed evidence.
* Concern: The fallback wording can turn a mandatory runtime validation gate into a report-only limitation, even though repository instructions define Aspire verification as required for distributed/runtime changes and the feature's acceptance criteria explicitly require observable end-to-end flow.
* Impact: A feature can be handed off with passing isolated tests but no proof that `ticketsapi`, `activity-worker`, Service Bus, Cosmos, service discovery, and correlated diagnostics operate together.
* Smallest useful change: Mark P06-T03 runtime failure as a blocking implementation/readiness condition: do not set the plan Ready or hand off the stacked PR until the required resources and flow are verified, unless a separately recorded and approved exception identifies the unverified criteria, owner, and follow-up gate.
* Action owner: AppHost test/runtime owner with planning parent
* Exact resolving evidence: A passing Aspire health/readiness check, end-to-end event-to-history evidence, correlated logs/traces, and representative failure evidence; or an explicit approved exception that leaves readiness blocked and names the next verification action.
* Decision route: Direct planner correction; user response is required only for an exception to the repository's runtime definition of done.

## Strengths and Residual Risk

* The selected outbox → Service Bus → Activity Worker → Cosmos → Tickets API proxy → Web path remains consistent with ownership and the research recommendation.
* The latest revision materially improves operational recovery: terminal rows are discoverable and replayable, cleanup exclusions are explicit, and the original event identity is preserved.
* The P03/P04 graph is now acyclic, and the authoritative ticket existence check prevents unknown tickets from being confused with eventual-consistency lag.
* Residual emulator risk remains correctly acknowledged: local Service Bus restart behavior cannot prove cloud durability and must remain labeled as unverified.

## Questions or Blocking Evidence Gaps

* The plan needs canonical serialized event and history contracts before independent service implementation begins.
* The three-project test boundary needs a concrete Web-state testing mechanism or an explicitly owned non-automated verification gate.
* Runtime failure must remain a readiness blocker rather than merely a recorded limitation.
* Exact Aspire 13.6.1 SDK signatures remain implementation-time validation, not an additional open-ended research request.

## Limitations

* This was a standard, follow-up, read-only critique using only the latest plan, research artifact, critiques 1-3, repository instructions, feature prompt, and stack notes.
* No application source files, SDK signatures, running AppHost, logs, traces, or tests were executed or inspected beyond the supplied artifacts.
* The plan and research artifact were not modified.

## Recommended Next Action

* Highest-impact finding: `PC-011`
* Action owner: Planning parent with Tickets API and Activity Worker owners
* Smallest next action: Add canonical event/history wire schemas and exact error examples, then assign executable UI-state evidence within the three existing test projects and make failed Aspire validation a readiness blocker.
* User response required: No for these planner corrections; yes only if the selected UI test mechanism requires a new project or if runtime verification is formally excepted.

## Relevant Artifacts

| Artifact | Description |
|----------|-------------|
| [.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md](.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md) | Latest revised implementation plan assessed by this follow-up critique |
| [.copilot-tracking/research/2026-10-18/activity-and-audit-research.md](.copilot-tracking/research/2026-10-18/activity-and-audit-research.md) | Research and selected architecture |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md) | Initial critique reconciled above |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md) | Previous follow-up critique reconciled above |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-3.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-3.md) | Latest prior follow-up critique reconciled above |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture, ownership, test, Aspire, and validation rules |
| [notes/prompts/4-feature-activity.md](notes/prompts/4-feature-activity.md) | Feature requirements and acceptance direction |
| [notes/stacks.md](notes/stacks.md) | Stack position and parent/child scope |

## Next Steps

* Revise the plan to close `PC-011` through `PC-013`, then run the eligible standard follow-up critique again before `/rpi-implement`.
