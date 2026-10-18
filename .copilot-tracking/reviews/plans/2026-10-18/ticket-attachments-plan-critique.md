<!-- markdownlint-disable-file -->

# RPI Plan Critique: SupportHub Ticket Attachments

## Metadata

* Task ID: `supporthub-ticket-attachments`
* Critique date: 2026-10-18
* Plan: .copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: initial
* Earlier critique: not_applicable

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the complete SupportHub Ticket Attachments implementation plan without editing the plan, application, research, or changes artifacts; write only this critique.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/ticket-attachments-research.md`, `.github/copilot-instructions.md`, and the directly relevant files named by the plan, including AppHost, Tickets API, Web client/page, package manifests, and existing tests.
* Decisions, dependencies, task Goals, and task Requirements considered: D1-D3; P01-P04 and their task requirements; FR-001 through FR-009; NFR-001 through NFR-003; storage/database, service-discovery, migration, Web, Azurite, and runtime-test dependencies; risks and non-goals.
* Assessment boundary: The supplied plan and named repository files only. This is a standard-depth credibility assessment, not implementation, compilation, runtime, browser, or external research validation. No supplied earlier critique was used.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| FR-001–FR-008, NFR-001–NFR-003; P01–P02 | Partial | The plan covers the core API, metadata, streaming, compensation, and named-resource boundaries, but ticket deletion cleanup, exact blob-container provisioning, and deterministic missing-resource semantics are not fully closed. |
| FR-009; P03 | Partial | Typed client, detail-page states, and a same-origin download design are planned, but no concrete Web route/response contract or automated/browser validation task proves the UI behavior. |
| P04-T01 | Covered | Focused policy and contract tests include boundary sizes, unsafe names, error mapping, and stable assertions. |
| P04-T02 | Partial | The lifecycle and failure scenarios are listed, but compensation and delete-failure tests depend on an unspecified fault-injection seam. |
| P04-T03; repository Definition of Done | Partial | Health/log/runtime verification is planned, but the Web-facing path and ticket-delete/attachment cleanup behavior are not explicitly exercised. |
| D1–D3 | Partial | D1/D2 are explicit planner defaults and D3 matches the repository constraint, but D1/D2 remain approval-gated user-owned decisions while the plan calls itself implementation-ready. |

## Verdict

* Verdict: Revise
* Rationale: The plan has a credible core architecture and strong API/storage/test coverage, but it leaves a material lifecycle orphan path (deleting an existing ticket), an underspecified blob-container contract, and incomplete proof of the required Blazor behavior. These gaps should be corrected before implementation approval.

## Findings

<!-- rpi:critique id=PC-001 -->
### PC-001 [High]: Existing ticket deletion has no attachment cleanup contract

* Related IDs: `FR-004`, `FR-007`, `NFR-003`, `P01-T02`, `P02-T02`, `P04-T02`
* Evidence: `src/SupportHub.Tickets.Api/Features/Tickets/TicketEndpoints.cs` currently deletes a ticket row only; `.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md` P01-T02 says ticket deletion must be “considered” but does not assign an implementation task or acceptance test.
* Concern: After attachments exist, the existing ticket delete endpoint can leave metadata and blob content unreachable, or fail unpredictably if a relationship is introduced. The plan explicitly recognizes this lifecycle but does not decide whether deletion cascades, blocks, or performs attachment cleanup.
* Impact: This creates permanent orphaned storage or stale metadata through an already-supported ticket operation and makes the claimed attachment lifecycle incomplete.
* Smallest useful change: Add one owned task and acceptance case for ticket deletion: select and document cascade/cleanup behavior, implement it within Tickets API, and test ticket deletion with one or more attachments plus retry/failure semantics. If intentionally deferred, explicitly make that a non-goal and define the resulting operational behavior.
* Action owner: Planning parent
* Exact resolving evidence: A plan task naming the selected ticket-delete policy, affected endpoint/entity behavior, and an Aspire test that proves metadata/blob outcomes after deleting a ticket.
* Decision route: Significant user decision if ticket deletion semantics change the existing ticket contract; otherwise direct planner correction.

<!-- rpi:critique id=PC-002 -->
### PC-002 [High]: Required Blazor behavior lacks an executable verification path

* Related IDs: `FR-009`, `NFR-002`, `NFR-003`, `P03-T02`, `P03-T03`, `P04-T03`
* Evidence: `.github/copilot-instructions.md` requires browser-level verification when meaningful frontend behavior changes; the plan adds upload/progress/download/delete UI but P04 contains no Web component test or browser test, and P03-T03 leaves the browser-facing route as “a small Web endpoint or equivalent C# response path.”
* Concern: The plan can prove the Tickets API while still shipping a broken interactive upload state, download trigger, route, or error rendering. The same-origin design is directionally sound but not implementation- or verification-complete.
* Impact: A primary user requirement can regress without detection, especially because the current `SupportHub.Web` has only component routing and typed service clients, not an existing attachment download endpoint.
* Smallest useful change: Specify the exact Web route and response contract, the component download invocation, and add a focused Web/browser verification task covering initial load, empty/list, upload progress/error, download, delete, and retry behavior. If browser automation is deliberately unavailable, record the concrete substitute and its limitation.
* Action owner: Planning parent
* Exact resolving evidence: A named Web route/handler and test or runtime evidence that exercises the route through `SupportHub.Web`, not only `ticketsapi`, with the required UI states asserted.
* Decision route: Direct planner correction unless the user explicitly declines browser/UI verification.

<!-- rpi:critique id=PC-003 -->
### PC-003 [Medium]: Blob container contract is not fixed

* Related IDs: `FR-007`, `NFR-003`, `P02-T01`, `P04-T02`
* Evidence: `src/SupportHub.AppHost/AppHost.cs` defines the blob service resource `blobs` but no attachment container; P02-T01 says “a stable feature-owned container/name convention” and asks the implementer to resolve a container or equivalent without naming the container or provisioning step.
* Concern: `blobs` identifies the service, not the container that owns attachment data. Different implementers could create different containers or rely on implicit creation, producing inconsistent startup, permissions, and cleanup behavior.
* Impact: Integration tests and local runtime behavior may not target the same storage boundary, and the plan cannot verify exact resource/container ownership.
* Smallest useful change: Name the container, state whether AppHost provisions it or Tickets API creates it idempotently, and make the integration test assert that exact container.
* Action owner: Planning parent
* Exact resolving evidence: A plan-level container constant/provisioning rule referenced by P02-T01 and P04-T02, with a test assertion against that exact name.
* Decision route: Direct planner correction.

<!-- rpi:critique id=PC-004 -->
### PC-004 [Medium]: Failure-compensation tests rely on an unspecified fault seam

* Related IDs: `FR-008`, `P02-T03`, `P04-T02`
* Evidence: P04-T02 says to use “a narrow test seam or controlled database/storage failure” if fault injection cannot be introduced, but neither the seam nor the deterministic failure mechanism is defined.
* Concern: The plan requires proving compensating blob deletion and delete-failure behavior while leaving the implementation choice open. A test that cannot reliably force metadata failure may only cover the happy path and fail to prove the core dual-store guarantee.
* Impact: The highest-risk consistency behavior can remain unverified or encourage production-only hooks that widen scope.
* Smallest useful change: Choose the smallest deterministic seam (for example, injected storage/metadata operation abstractions at the feature boundary) and name the unit/integration test that forces each failure point without changing production semantics.
* Action owner: Planning parent
* Exact resolving evidence: A task-level seam/fixture definition plus a passing test that demonstrates blob compensation and explicit residual diagnostics.
* Decision route: Direct planner correction.

<!-- rpi:critique id=PC-005 -->
### PC-005 [Medium]: Approval-gated policy defaults conflict with implementation-ready status

* Related IDs: `D1`, `D2`, `P01-T01`, `P02-T02`, `Planning Readiness and Next Step`
* Evidence: The plan selects `10 MiB` and the allowlist in D1 and blob-first deletion in D2, but labels them “Proposed planner default” and says user approval is required while also declaring “Complete; Ready for implementation.”
* Concern: The implementation and tests are bound to values and deletion semantics that the plan says are not yet approved.
* Impact: Implementation may begin against a policy or destructive-storage behavior that the user later changes, causing rework or an invalid approval gate.
* Smallest useful change: Either obtain/record explicit acceptance of D1/D2 before marking readiness complete, or change readiness to approval-pending and identify the exact implementation tasks blocked by each decision.
* Action owner: Planning parent / user for the policy decision
* Exact resolving evidence: Recorded user disposition for D1 and D2, or a readiness state that clearly prevents implementation until disposition.
* Decision route: Significant or divergent user decision.

## Strengths and Residual Risk

* The plan correctly preserves Tickets API ownership, exact `storage`/`blobs`/`ticketsdb` resource boundaries, C#-only scope, service discovery, and the existing migration/test conventions.
* The blob-first upload plus compensation strategy is explicit and appropriately acknowledges that PostgreSQL and Blob Storage cannot commit atomically.
* Opaque server-generated blob names, server-authoritative validation, cancellation propagation, cross-ticket route checks, and structured error handling are credible.
* Malware scanning, identity authorization, reconciliation infrastructure, and later stack layers are correctly treated as residual or out of scope rather than silently invented.

## Questions or Blocking Evidence Gaps

* What is the selected behavior when an existing ticket with attachments is deleted?
* What exact container name and provisioning rule does the attachment feature use?
* What concrete Web route and browser/UI verification mechanism proves the required download and interactive states?
* What deterministic test seam forces metadata persistence and storage deletion failures?

## Limitations

* No implementation, build, test, Aspire runtime, or browser execution was performed; this critique evaluates plan credibility from the supplied artifacts and named files only.
* No earlier critique artifact was supplied, so the plan’s self-reported prior `PC-001`/`PC-002` disposition was not treated as evidence and is not reconciled here.
* External documentation cited inside the research and plan was not broadened or independently re-researched.

## Recommended Next Action

* Highest-impact finding: `PC-001`
* Action owner: Planning parent
* Smallest next action: Add and decide the ticket-delete attachment cleanup contract, then add its failure-aware integration coverage; resolve PC-002 through PC-005 in the same plan revision.
* User response required: Yes for D1/D2 and any change to ticket deletion semantics; no for the remaining direct planner corrections.

| [.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md](.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md) | Supplied implementation plan |
| [.copilot-tracking/research/2026-10-18/ticket-attachments-research.md](.copilot-tracking/research/2026-10-18/ticket-attachments-research.md) | Supplied research evidence |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture and verification constraints |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md) | This critique artifact |

## Next Steps

Revise the plan through `/rpi-plan` (or the active planning parent) before `/rpi-implement`; obtain the user decisions for D1/D2 and ticket-delete semantics first.
