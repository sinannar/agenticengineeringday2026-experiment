<!-- markdownlint-disable-file -->

# RPI Plan Critique: SupportHub Ticket Attachments

## Metadata

* Task ID: `supporthub-ticket-attachments`
* Critique date: 2026-10-18
* Plan: .copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: explicit user request for the final standard follow-up
* Critique type: follow_up
* Earlier critique: .copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-3.md

## Inputs and Criterion Boundary

* Task context and caller requirements: Perform the final standard follow-up critique for SupportHub Ticket Attachments; reconcile `PC-001` through `PC-008`; identify any remaining material blocker or contradiction; do not edit the plan, application, research, or earlier critiques; write only this artifact.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/ticket-attachments-research.md`, `.github/copilot-instructions.md`, the current plan, critiques `ticket-attachments-plan-critique.md`, `ticket-attachments-plan-critique-2.md`, and `ticket-attachments-plan-critique-3.md`, plus the directly relevant AppHost, Tickets API, Web, migration, package, and test files named by the plan.
* Decisions, dependencies, task Goals, and task Requirements considered: confirmed `D1`–`D4`; `P01`–`P04`; `FR-001`–`FR-009`; `NFR-001`–`NFR-003`; service ownership, storage/container, migration, service-discovery, Web, Azurite, testing, and runtime-verification dependencies.
* Assessment boundary: The supplied plan, research, prior critiques, repository instructions, and named repository files only. This standard-depth assessment does not implement, compile, run, or browser-test the feature and does not broaden research or independently validate pinned APIs.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|--------------------------------------------|----------|---------------------|
| `PC-001`–`PC-004`, `PC-006`–`PC-007` | Covered/resolved | The plan now defines ticket-delete cleanup, the exact `ticket-attachments` container, focused fake-based failure tests, bounded `MultipartReader` handling with `413` coverage, and a reproducible seeded missing-blob browser failure. |
| `PC-005` | Covered/resolved | `D1`, `D2`, and `D4` are recorded as confirmed user decisions; implementation remains gated only by the final critique. |
| `PC-008` | Resolved in the readiness table; stale elsewhere | The readiness table now states that the decisions are confirmed, but the artifact self-check and critique-disposition text still retain approval-gated/default language. |
| `FR-001`–`FR-009`, `NFR-001`–`NFR-003`; `P01`–`P04` | Covered with one handoff-record contradiction | The implementation and verification tasks are materially complete for planning; the remaining issue is inconsistent plan provenance/readiness metadata, not a missing feature requirement. |

## Verdict

* Verdict: Revise
* Rationale: The substantive architecture, lifecycle, streaming, failure-seam, browser-stimulus, and decision gaps from `PC-001` through `PC-008` are resolved or adequately narrowed. One remaining contradiction in the plan’s own handoff records says earlier critiques are absent and that decisions remain approval-gated, despite the recorded follow-up history and confirmed decisions. Correcting that metadata is required before calling the plan final and implementation-ready.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| `PC-001`: Existing ticket deletion has no attachment cleanup contract | resolved | `D4` and `P02-T04` define cleanup-before-ticket-delete, abort-on-failure behavior, retryable state, and integration coverage. |
| `PC-002`: Required Blazor behavior lacks an executable verification path | resolved | `P03-T03` fixes the same-origin download route and `P04-T04` exercises the running Web app and required stable UI states. |
| `PC-003`: Blob container contract is not fixed | resolved | `P02-T01` fixes `ticket-attachments` with idempotent application provisioning; `P04-T02` asserts the exact container and ticket-scoped names. |
| `PC-004`: Failure-compensation tests rely on an unspecified fault seam | resolved | `P02-T03` and `P04-T01` place coordinator failure tests behind focused injected fakes; `P04-T02` explicitly limits distributed tests to real-resource paths. |
| `PC-005`: Approval-gated policy defaults conflict with implementation-ready status | resolved in decisions | `D1`, `D2`, and `D4` are confirmed user decisions, and the readiness table no longer gates on their approval. Residual stale wording is captured in `PC-009`. |
| `PC-006`: Server-side bounded multipart streaming is not implementation-determinate | resolved | `P02-T01` selects `MultipartReader`, an 11 MiB request ceiling, a 10 MiB counting-stream limit, cancellation, and idempotent container setup; `P02-T02`/`P04-T02` define `413` and zero-row coverage. |
| `PC-007`: Browser delete-failure verification has no reproducible stimulus | resolved | `P04-T04` narrows browser failure verification to a seeded missing-content download failure using the exact Azurite resource client; storage-operation failures remain in `P04-T01`. |
| `PC-008`: Readiness record retains stale approval-gate language | resolved in the primary readiness table; stale in supporting records | The readiness table is corrected, but the self-check and critique-disposition records still describe decisions as approval-gated and do not accurately enumerate prior critiques. See `PC-009`. |

## Findings

<!-- rpi:critique id=PC-009 -->
### PC-009 [Medium]: Final handoff metadata contradicts the confirmed decisions and critique history

* Related IDs: `D1`, `D2`, `D4`, `PC-005`, `PC-008`, `Planning Readiness and Next Step`, `Critique Disposition`, `Artifact Self-Check`
* Evidence: `.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md` records `D1`, `D2`, and `D4` as confirmed and says implementation is gated only on the final critique, but its artifact self-check still says “planner defaults are explicit and approval-gated.” Its Critique Disposition also says “Earlier critiques: none” and identifies the initial critique as the latest critique, despite recording `PC-001` through `PC-008` and citing the follow-up critiques.
* Concern: The plan’s substantive tasks are aligned, but its final provenance and readiness records give mutually inconsistent instructions about whether policy approval remains open and which critique is authoritative.
* Impact: The implementation handoff can reopen settled user decisions, omit the actual follow-up chain, or treat a superseded initial verdict as current. This undermines the plan’s final-gate credibility even though it does not require an application-design change.
* Smallest useful change: Update the self-check and Critique Disposition to state that `D1`, `D2`, and `D4` are confirmed, remove approval-gated wording, enumerate the three earlier critique paths, identify critique-3 as the latest prior critique, and mark this critique-4 as the final gate pending its verdict.
* Action owner: Planning parent
* Exact resolving evidence: A plan whose readiness, self-check, and critique-disposition records agree on confirmed decision status, the complete prior critique chain, and the final follow-up artifact path.
* Decision route: Direct planner correction.

## Strengths and Residual Risk

* The plan preserves Tickets API ownership, exact `storage`/`blobs`/`ticketsdb` boundaries, C#-only scope, service discovery, and existing migration/test conventions.
* Ticket deletion, container naming/provisioning, same-origin download routing, focused failure seams, blob-first compensation, bounded multipart handling, and browser missing-content verification are now explicit.
* The plan correctly separates deterministic coordinator failure tests from real AppHost/Azurite lifecycle tests and avoids a production-visible fault-injection surface.
* Identity authorization, malware scanning, public/SAS URLs, reconciliation infrastructure, and later stack layers remain intentionally deferred.
* Dual-store orphan/stale-row risk remains an explicit operational residual because no cross-store transaction or reconciliation worker is introduced.

## Questions or Blocking Evidence Gaps

* No decision-critical implementation or research gap remains within the supplied standard-depth boundary.
* The plan metadata must be corrected before final handoff so the confirmed decisions and critique lineage are not contradicted.

## Limitations

* No application code, plan, research, or prior critique was modified.
* No implementation, build, test, Aspire runtime, or browser execution was performed; this artifact assesses plan credibility from supplied evidence only.
* Pinned .NET/Aspire APIs were not independently compiled or re-researched because the caller requested standard depth and no broadened research.

## Recommended Next Action

* Highest-impact finding: `PC-009`
* Action owner: Planning parent
* Smallest next action: Correct the stale self-check and critique-disposition metadata, then treat this artifact as the final planning gate.
* User response required: No.

| [.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md](.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md) | Current implementation plan |
| [.copilot-tracking/research/2026-10-18/ticket-attachments-research.md](.copilot-tracking/research/2026-10-18/ticket-attachments-research.md) | Supplied research evidence |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture and verification constraints |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md) | Initial critique reconciled above |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-2.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-2.md) | Second critique reconciled above |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-3.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-3.md) | Third critique reconciled above |

## Next Steps

Revise the plan through the active planning parent to close `PC-009`; no user action is required.
