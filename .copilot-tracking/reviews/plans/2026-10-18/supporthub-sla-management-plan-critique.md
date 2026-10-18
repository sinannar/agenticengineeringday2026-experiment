<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub SLA Management

## Metadata

* Task ID: `supporthub-sla-management`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/supporthub-sla-management-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: initial
* Earlier critique: not applicable

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the implementation-ready SupportHub SLA Management plan once for substantive blockers, contradictions, unsupported architecture or scope, missing dependencies, acceptance/test/runtime gaps, and material risks; do not edit the plan or application code.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/supporthub-sla-management-research.md`; `.github/copilot-instructions.md`; `.copilot-tracking/plans/2026-10-18/supporthub-sla-management-plan.md`; prior Activity research/plan/changes paths referenced by the plan.
* Decisions, dependencies, task Goals, and task Requirements considered: Confirmed UTC clock, 3x3 priority/tier policy, at-risk thresholds, terminal behavior, ownership boundaries, on-demand evaluation, P01-P05 task dependencies, FR-001 through FR-009, and NFR-001 through NFR-006.
* Assessment boundary: The supplied plan and completed research were assessed across architecture, requirements, phase/task sequencing, dependencies, contracts, failure handling, tests, and Aspire/runtime validation. This critique does not infer unrecorded implementation details or perform new repository/runtime research.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|---|---|---|
| FR-001–FR-004, NFR-002 | Covered | P01-T01/P01-T02 define the policy inputs, UTC `TimeProvider`, thresholds, terminal state, invalid-input behavior, and deterministic unit coverage in P05-T01. |
| FR-005, NFR-003–NFR-004 | Partial | P02-T01/P02-T02 define status and bounded scan contracts, but the concrete customer-tier lookup/scan strategy remains conditional and is not locked before implementation. |
| FR-006–FR-007, NFR-006 | Partial | P03 tasks define thin clients and logging, but P05-T02/P05-T03 do not establish a deterministic, executable way to trigger and distinguish scheduled Worker behavior from manual Console behavior in Aspire tests/runtime evidence. |
| FR-008, NFR-005 | Partial | P04-T01/P04-T02 describe detail-page rendering and accessibility semantics, but the validation lock makes UI coverage conditional and no P05 task explicitly verifies the changed Blazor behavior. |
| FR-009 | Partial | Policy and distributed test scope is detailed, but the scan dependency-failure, operational-client, and UI acceptance evidence are not fully locked to concrete test seams or runtime steps. |
| P01–P05 sequencing | Covered with material gaps | Dependencies are generally ordered correctly; the unresolved scan strategy and omitted explicit UI verification weaken implementation readiness. |

## Verdict

* Verdict: Revise
* Rationale: The plan has a credible ownership-preserving architecture and complete core policy semantics, but it leaves the highest-load cross-service scan behavior as an implementation-time choice and does not lock executable UI and operational-client acceptance evidence. These gaps can materially change API shape, runtime behavior, and test scope.

## Findings

<!-- rpi:critique id=PC-001 -->
### PC-001 [High]: Customer-tier scan strategy is not decided before the scan contract

* Related IDs: `FR-005`, `FR-006`, `NFR-001`, `NFR-004`, `P01-T03`, `P02-T02`, `P05-T02`, risk `N+1 customer-tier lookup`
* Evidence: `.copilot-tracking/plans/2026-10-18/supporthub-sla-management-plan.md` (`P01-T03` and `P02-T02` Details/Requirements; Risks table)
* Concern: The plan requires a bounded scan with explicit partial failures, but permits either per-ticket customer calls with a bounded batch or a new batch contract “only if” evidence demonstrates need. That evidence cannot be produced without selecting the scan shape, and the choice affects endpoint contracts, failure aggregation, resilience behavior, latency, and integration fixtures. The plan therefore defers a decision-critical architecture dependency into implementation.
* Impact: A scan can satisfy the nominal API shape while producing unacceptable N+1 traffic or inconsistent partial-failure semantics. Worker and Console behavior, timeout/retry characteristics, and P05-T02 assertions may need redesign after coding.
* Smallest useful change: Lock one bounded strategy in P01-T03/P02-T02 before implementation: define the maximum ticket page, concurrency/lookup limit, timeout behavior, and per-ticket versus aggregate failure representation. If retaining per-ticket lookup for this layer, explicitly accept that bounded trade-off and make it the tested contract; otherwise add the justified customer batch endpoint as a prerequisite task.
* Action owner: planning parent
* Exact resolving evidence: The plan states one selected lookup strategy and numeric bounds, and P05-T02 contains tests that exercise the selected strategy's success, timeout/unavailable, and missing-customer cases without relying on an implementation-time alternative.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-002 -->
### PC-002 [Medium]: Blazor acceptance is specified but not assigned executable validation

* Related IDs: `FR-008`, `NFR-005`, `P04-T01`, `P04-T02`, `P05-T03`, Test Ownership and Validation Lock
* Evidence: `.copilot-tracking/plans/2026-10-18/supporthub-sla-management-plan.md` (`P04-T02` and Test Ownership and Validation Lock)
* Concern: The plan requires loading, terminal, unavailable, error, text-semantic state, deadline, and remaining/lateness behavior in `TicketDetail.razor`, but the locked validation says Web behavior may be verified through API contracts and Aspire/browser evidence “if present” and does not create a concrete UI test or explicit browser verification task. P05-T03 lists API/runtime checks but does not require exercising the changed page.
* Impact: The feature can pass all planned unit/API tests while the support-staff surface fails to load, renders the wrong state, duplicates calls, or violates the non-color-only requirement. This is a direct acceptance gap for an in-scope requirement.
* Smallest useful change: Add a concrete UI validation task under P04 or P05 that names the existing Web test surface if available, or mandates one browser-level Aspire check for detail-page success, terminal/unavailable state, and semantic text. Make P05-T03 depend on and record that evidence rather than leaving it conditional.
* Action owner: planning parent
* Exact resolving evidence: A named test or browser scenario against `TicketDetail.razor` asserts the server-provided state/deadline and visible semantic labels, plus the unavailable/error state, and is included in the final validation record.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-003 -->
### PC-003 [Medium]: Scheduled/manual operational equivalence lacks a deterministic test seam

* Related IDs: `FR-006`, `FR-007`, `FR-009`, `NFR-004`, `NFR-006`, `P03-T01`, `P03-T02`, `P05-T02`, `P05-T03`
* Evidence: `.copilot-tracking/plans/2026-10-18/supporthub-sla-management-plan.md` (`P03-T01`, `P03-T02`, `P05-T02` Details, and `P05-T03`)
* Concern: The plan asks Aspire tests/runtime validation to prove that Worker and Console use the same scan semantics and to observe a scheduled scan, but supplies no controllable cadence, trigger, clock, or client seam for those processes. The Worker is a long-running periodic service and the Console is interactive; “observable HTTP/log evidence where the harness supports it” is not an acceptance method.
* Impact: Validation may become timing-dependent or be reduced to source inspection, leaving failures in configuration binding, cancellation, response handling, or command wiring undetected. It also risks conflating API correctness with proof that both operational clients actually invoke it.
* Smallest useful change: Define an executable seam: expose the scan request/orchestration as an injectable client/service with fake-handler unit tests for Worker and Console, and configure a short test-only interval/explicit trigger for one Aspire runtime check. Specify the exact log or HTTP evidence and cancellation/failed-response assertions.
* Action owner: planning parent
* Exact resolving evidence: P05-T02 names deterministic Worker and Console client tests or a test configuration/trigger, and P05-T03 records one successful scheduled invocation and one manual invocation with matching request/result semantics plus failure/cancellation evidence.
* Decision route: direct planner correction

## Strengths and Residual Risk

* The plan correctly keeps SLA ownership in Tickets API, preserves Customers API ownership of support tier, avoids unsupported persistence, and carries the confirmed policy into explicit contracts and matrix tests.
* The dependency graph, service-discovery resource names, cancellation requirements, and no-direct-database boundary are consistently represented.
* Residual risk remains around scan cost and transient cross-service dependency failures until PC-001 is resolved; future notification/history requirements would still require a separate design.

## Questions or Blocking Evidence Gaps

* No user decision is required by the supplied evidence. The open items are planner-owned implementation choices, but the selected bounded lookup and operational test seams must be recorded before implementation.

## Limitations

* This was a standard/default critique limited to the supplied plan, completed research, repository instructions, and referenced artifacts. No open-ended repository research, build, Aspire run, browser run, or application modification was performed.
* The critique cannot determine whether an existing Web test surface or console test seam already exists beyond what the supplied plan states; the plan should name and validate the chosen seam rather than relying on that uncertainty.

## Recommended Next Action

* Highest-impact finding: `PC-001`
* Action owner: planning parent
* Smallest next action: Revise P01-T03/P02-T02 to select and bound the customer-tier lookup strategy, then add explicit UI and Worker/Console validation tasks/evidence as described in PC-002 and PC-003.
* User response required: no
