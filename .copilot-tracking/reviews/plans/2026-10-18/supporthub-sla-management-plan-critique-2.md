<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub SLA Management

## Metadata

* Task ID: `supporthub-sla-management`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/supporthub-sla-management-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: follow-up
* Earlier critique: `.copilot-tracking/reviews/plans/2026-10-18/supporthub-sla-management-plan-critique.md`
* Severity counts: Critical 0, High 0, Medium 1, Low 0

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the revised SupportHub SLA Management plan, reconcile PC-001 through PC-003, identify new substantive gaps, and write only this follow-up critique. Confirmed policy decisions are authoritative; no open-ended research or implementation is permitted.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/supporthub-sla-management-research.md`, `.github/copilot-instructions.md`, the revised plan, and the earlier critique.
* Decisions, dependencies, task Goals, and task Requirements considered: Confirmed UTC clock, 3x3 priority/tier policy, at-risk thresholds, terminal behavior, Tickets API ownership, bounded scan limits, P01-P05 dependencies, FR-001 through FR-009, and NFR-001 through NFR-006.
* Assessment boundary: The supplied plan and artifacts were assessed across architecture, contracts, bounded scan behavior, failure handling, testability, UI coverage, operational clients, and Aspire validation. No application, build, runtime, browser, or additional repository research was performed.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|---|---|---|
| FR-001–FR-004, NFR-002 | Covered | P01-T01/P01-T02 and P05-T01 define the confirmed policy, UTC/injectable time, exact boundaries, terminal behavior, invalid-input handling, and deterministic matrix tests. |
| FR-005, NFR-001, NFR-004 | Partial | The plan now fixes a 50-ticket cap, four concurrent customer lookups, stable failure categories, and all-failure behavior. The distributed test task still does not define how an unavailable Customers API dependency is deterministically induced. |
| FR-006–FR-007, NFR-006 | Covered | P03-T01/P03-T02 and P05-T02/P05-T03 provide a shared API path, bounded non-overlapping scheduling, fake-handler client tests, and explicit scheduled/manual runtime evidence. |
| FR-008, NFR-005 | Covered | P04-T01/P04-T02 and P05-T03 now require browser-level verification of server-provided state, deadline text, semantic labels, and error/unavailable handling. |
| FR-009 | Partial | Core policy, API, operational-client, UI, and runtime coverage is assigned, but the customer-dependency outage branch remains without a reproducible distributed-test seam. |
| P01–P05 sequencing | Covered | Dependencies are coherent and the three original critique gaps are addressed in the revised phase/task details. |

## Verdict

* Verdict: Revise
* Rationale: PC-001, PC-002, and PC-003 are substantively resolved. The revised plan is otherwise credible, but it requires integration evidence for Customers API unavailability without specifying a deterministic fault/configuration seam, so one material failure-path acceptance claim remains implementation-dependent.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|---|---|---|
| PC-001: customer-tier scan strategy was not decided | resolved | P01-T03 and P02-T02 select per-ticket customer-by-ID lookup, cap scans at 50 tickets, limit concurrency to 4, define timeout/cancellation/missing-customer categories, and specify all-failure behavior. P05-T02 assigns success and failure coverage. |
| PC-002: Blazor acceptance lacked executable validation | resolved | P05-T03 now requires a browser-level Aspire check for `TicketDetail.razor`, including successful server-provided state/deadline text and an unavailable/error scenario when the harness can induce it; the limitation must be recorded otherwise. |
| PC-003: scheduled/manual equivalence lacked a deterministic seam | resolved | P05-T03 requires fake-handler Worker/Console client tests, exact request assertions, failure/cancellation coverage, and a short test interval or explicit trigger for runtime evidence. P05-T02 also requires matching scheduled/manual semantics. |

## Findings

<!-- rpi:critique id=PC-004 -->
### PC-004 [Medium]: Customers API outage coverage lacks an executable distributed-test seam

* Related IDs: `FR-005`, `FR-009`, `NFR-004`, `P01-T03`, `P02-T02`, `P05-T02`
* Evidence: `.copilot-tracking/plans/2026-10-18/supporthub-sla-management-plan.md` (`P01-T03` failure categories and `P05-T02` requirement to verify missing/unavailable customer data). The plan defines the expected `CustomerLookupUnavailable` result but does not specify how the AppHost test graph will make Customers API timeout/unavailable while the Tickets API remains reachable.
* Concern: A missing customer can be created as ordinary test data, but dependency timeout/unavailability is not reproducible from the described distributed harness. Without a named test configuration, controllable handler seam, or deliberately scoped unit test for the Tickets API orchestration, implementation may validate only the happy path and missing-customer branch while claiming coverage of transient dependency failures.
* Impact: The bounded scan's partial-failure contract, cancellation behavior, and all-failure response can regress in the highest-risk cross-service condition without detection. Ad hoc fault injection could also introduce an unsafe production-visible mechanism.
* Smallest useful change: Assign the unavailable-dependency case to a concrete executable seam: either add a focused Tickets API orchestration test with an injected customer client/fake handler and reserve Aspire tests for reachable/missing-customer behavior, or define a test-only AppHost configuration/endpoint replacement that deterministically returns timeout/unavailable responses without exposing an operational fault surface. Require assertions for partial and all-failure responses.
* Action owner: planning parent
* Exact resolving evidence: The plan names the test project and DI/configuration path that produces a Customers API timeout/unavailable response, plus assertions for `CustomerLookupUnavailable`, partial results, all-failure status, cancellation, and bounded concurrency.
* Decision route: direct planner correction

## Strengths and Residual Risk

* The revision preserves service ownership, the confirmed policy, on-demand evaluation, explicit contracts, cancellation, exact Aspire resource names, and no direct Worker/Console database access.
* PC-001 is resolved by an explicit bounded per-ticket lookup trade-off; PC-002 and PC-003 are resolved by concrete browser and fake-handler/runtime validation requirements.
* Residual operational risk from bounded N+1 lookups is explicitly accepted and testable. Durable history, notifications, and tier differentiation remain correctly out of scope.

## Questions or Blocking Evidence Gaps

* No user decision is required. The remaining issue is planner-owned: choose the executable test seam for Customers API timeout/unavailability and the scan's partial/all-failure assertions.

## Limitations

* This standard follow-up critique used only the supplied plan, research, repository instructions, and earlier critique. It did not inspect additional source, run builds/tests/Aspire/browser checks, or modify any plan or application file.
* The critique cannot establish whether an existing injectable customer-client seam is already available; the plan must name it or assign the failure cases to a test scope that can execute them.

## Recommended Next Action

* Highest-impact finding: `PC-004`
* Action owner: planning parent
* Smallest next action: Revise P05-T02 (and, if needed, P01-T03/P02-T02) to select the concrete Customers API outage/fault seam and require partial/all-failure assertions; then rerun the standard follow-up critique if the plan changes.
* User response required: no

| [.copilot-tracking/plans/2026-10-18/supporthub-sla-management-plan.md](.copilot-tracking/plans/2026-10-18/supporthub-sla-management-plan.md) | Revised implementation plan assessed |
| [.copilot-tracking/research/2026-10-18/supporthub-sla-management-research.md](.copilot-tracking/research/2026-10-18/supporthub-sla-management-research.md) | Completed SLA research and confirmed policy |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture and validation constraints |
| [.copilot-tracking/reviews/plans/2026-10-18/supporthub-sla-management-plan-critique.md](.copilot-tracking/reviews/plans/2026-10-18/supporthub-sla-management-plan-critique.md) | Initial critique reconciled above |
| [.copilot-tracking/reviews/plans/2026-10-18/supporthub-sla-management-plan-critique-2.md](.copilot-tracking/reviews/plans/2026-10-18/supporthub-sla-management-plan-critique-2.md) | This follow-up critique |

## Next Steps

Revise the plan through the active planning parent to close PC-004; no user action is required.
