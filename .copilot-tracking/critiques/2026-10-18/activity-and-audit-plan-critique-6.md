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
* Earlier critique: `.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-5.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the latest SupportHub Activity and Audit implementation plan without modifying the plan or research; reconcile `PC-001` through `PC-014`; add only new substantive findings. Particular attention is given to executable canonical contracts, reproducible UI evidence, the unconditional Aspire runtime blocker, honest bounded-window history semantics, and complete dependencies, ownership, and test constraints.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/activity-and-audit-research.md`; `.github/copilot-instructions.md`; `notes/prompts/4-feature-activity.md`; `notes/stacks.md`; critiques 1-5.
* Decisions, dependencies, task Goals, and task Requirements considered: The plan's FR/NFR catalog, canonical wire contracts, D1-D5 decisions, P01-P06 tasks and dependency graph, outbox maintenance/replay design, P04-T00 hosting gate, P04-T02 history contract, P05-T02 browser matrix, P06 test ownership/runtime gate, accountability matrix, acceptance criteria, and readiness/disposition records.
* Assessment boundary: The supplied artifacts support a complete standard credibility assessment of contract executability, sequencing, ownership, testability, bounded history semantics, and readiness gating. This critique does not execute application code, verify SDK signatures, run Aspire, or treat implementation-time validation as already completed.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| Feature prompt and stack boundary | Covered | The plan remains limited to Activity and Audit on `feature/activity`, reuses the existing `messaging`, `ticket-events`, `cosmos`, and `activitydb` resources, and excludes later SLA/dashboard layers. |
| Research C1-C11 and W1-W5 | Covered | Ownership, transactional outbox, stable event identity, partitioned Cosmos storage, peek-lock settlement, duplicate handling, emulator limits, and Aspire testing are carried through to tasks and acceptance evidence. |
| FR-001 through FR-011 and NFR-001 through NFR-011 | Covered | The plan-local catalog, traceability table, task Requirements blocks, and acceptance evidence make the requirement set auditable. |
| P01 event contracts and atomic outbox | Covered | Canonical examples define exact event fields, casing, types, nullability, timestamps, event details, and required transition cases; outbox intent is committed with ticket mutations. |
| P02 publication, recovery, and retention | Covered | Dispatcher metadata, claim expiry, retry/terminal lifecycle, terminal-row enumeration/replay, identity preservation, cleanup exclusions, and tests are assigned to explicit tasks. |
| P03/P04 dependency sequencing and hosting gate | Covered | P03-T03 is hosting-independent; P04-T00 precedes HTTP-shape work and gates P04/P05/P06 API tasks. The dependency graph is acyclic. |
| P04 history semantics and bounded window | Covered | Tickets API performs authoritative existence checking; `400`, `404`, valid-empty/populated `200`, `502`, and `503` are exact; the response returns the latest finite `limit` entries and intentionally makes no pagination or completeness claim. |
| P05 UI behavior and evidence | Covered | UI-01 through UI-06 define deterministic setup/resource state, expected text/ARIA state, screenshot/trace artifact, and owner requirements in the changes record; lag, loading, empty, success, not-found, and dependency-error states are distinct. |
| P06 test-project and ownership constraints | Covered | Automated tests remain in the three existing test projects, browser verification is separately owned, and the accountability matrix assigns phase groups, runtime evidence, and topology decisions. |
| P06 runtime readiness gate | Covered | Runtime startup and the required event-to-history flow are unconditional gates; missing evidence keeps readiness `Not ready` and prohibits handoff. An exception can defer/change scope but cannot mark the feature ready. |

## Verdict

* Verdict: Pass
* Rationale: The latest plan is credible for implementation within the supplied boundary. Earlier findings are closed: the wire contracts are executable across publisher, consumer, proxy, Web client, and tests; UI scenarios have reproducible evidence requirements and durable artifact ownership; Aspire runtime validation is an unconditional readiness blocker; bounded history semantics do not advertise unsupported continuation; and dependencies, task ownership, and three-project test constraints are explicit. Remaining SDK, numeric-threshold, hosting, emulator, and runtime checks are correctly retained as implementation gates rather than hidden assumptions.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| `PC-001` | resolved | The plan defines FR-001 through FR-011 and NFR-001 through NFR-011, maps them to tasks, and specifies acceptance evidence. |
| `PC-002` | resolved | `P04-T00` is a blocking hosting spike that records host shape, binding, health, service URI, AppHost reference, and test path before dependent HTTP work. |
| `PC-003` | resolved | `P01-T01` and the canonical wire-contract section bind exact event-specific detail shapes, casing, nullability, and transition coverage. |
| `PC-004` | resolved | `P06-T01` provides a bounded failure matrix with injectable seams, expected settlement/state outcomes, and explicit unsupported-emulator recording. |
| `PC-005` | resolved | The accountability matrix assigns phase/task groups, runtime evidence, and topology decisions to owners and completion artifacts. |
| `PC-006` | resolved | `P02-T03` provides terminal-row enumeration, replay preserving the original payload and `eventId`, claim recovery, cleanup exclusions, and deterministic tests. |
| `PC-007` | resolved | `P04-T02`, `P05-T02`, and `P06-T02` bind authoritative existence, empty history, lag, dependency failure, and UI response semantics. |
| `PC-008` | resolved | The hosting-independent P03-T03 seam precedes P04-T00, which gates all Worker HTTP-shape tasks; no dependency cycle remains. |
| `PC-009` | resolved | `P04-T02` explicitly checks the authoritative Tickets API store before calling Activity Worker and tests unknown versus valid-empty tickets. |
| `PC-010` | resolved | The plan prohibits a fourth test project and assigns automated coverage to the three repository-approved projects. |
| `PC-011` | resolved | The canonical wire-contract section supplies concrete GUIDs, timestamps, snapshot/item fields, actor type, casing/nullability, ordering/limit semantics, and exact Problem Details bodies; P06-T01 requires serialized assertions. |
| `PC-012` | resolved | P05-T02 defines UI-01 through UI-06 with setup/resource state, expected text/ARIA state, screenshot/trace path, and owner in the changes record; backend 502 proof remains contract-tested. |
| `PC-013` | resolved | P06-T03 makes runtime startup and end-to-end flow unconditional gates, preserves `Not ready` on failure, and prohibits implementation handoff without required evidence. |
| `PC-014` | resolved | The canonical response removes `hasMore` and pagination claims, explicitly defines a bounded latest-window response, and assigns the UI no completeness or continuation claim. |

## Findings

No new substantive findings were identified. The latest revision closes the prior contract, UI-evidence, runtime-gate, bounded-history, dependency, ownership, and test-constraint concerns without introducing a new material credibility gap.

## Strengths and Residual Risk

* The plan now has an executable cross-service contract: exact event and history JSON, error bodies, ordering, limits, nullability, and serialization assertions.
* The UI evidence matrix is reproducible in intent and bounded in scope without adding a fourth test project or a new UI framework.
* Runtime validation is correctly treated as a hard readiness condition rather than a report-only limitation.
* The bounded latest-history window is honest because it omits `hasMore` and makes no unsupported pagination/completeness claim.
* Residual risks are appropriately implementation-time gates: exact Aspire 13.6.1 APIs, chosen numeric retry/retention thresholds, hybrid Worker hosting validation, emulator limitations, and actual health/log/trace evidence. They are not reasons to revise the plan before implementation.

## Questions or Blocking Evidence Gaps

* None decision-critical within the supplied planning boundary.
* Implementation must still produce the stated runtime, browser, contract, failure, and emulator-limitation evidence before readiness is marked complete.

## Limitations

* This was a standard, follow-up, read-only critique using only the latest plan, supplied research, critiques 1-5, repository instructions, feature prompt, and stack notes.
* No application source, SDK signatures, running AppHost, logs, traces, or tests were executed.
* The plan and research artifacts were not modified.

## Recommended Next Action

* Highest-impact finding: None
* Action owner: Planning parent / implementation owners
* Smallest next action: Preserve the current plan and proceed to implementation only after the plan's existing P04-T00 and P06-T03 gates are followed; produce the required contract, browser, failure, and Aspire runtime evidence during implementation.
* User response required: No

## Relevant Artifacts

| Artifact | Description |
|----------|-------------|
| [.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md](.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md) | Latest implementation plan assessed |
| [.copilot-tracking/research/2026-10-18/activity-and-audit-research.md](.copilot-tracking/research/2026-10-18/activity-and-audit-research.md) | Research and selected architecture |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md) | Initial critique |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md) | Second critique |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-3.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-3.md) | Third critique |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-4.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-4.md) | Fourth critique |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-5.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-5.md) | Immediate prior critique |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture, ownership, test, Aspire, and readiness rules |
| [notes/prompts/4-feature-activity.md](notes/prompts/4-feature-activity.md) | Feature requirements and acceptance direction |
| [notes/stacks.md](notes/stacks.md) | Stack position and scope |

## Next Steps

* The standard follow-up critique is complete and passes. No plan revision or user decision is required. The active parent may proceed to `/rpi-implement`, while preserving the plan's unconditional runtime and evidence gates.
