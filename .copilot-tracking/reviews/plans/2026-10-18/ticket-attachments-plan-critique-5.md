<!-- markdownlint-disable-file -->

# RPI Plan Critique: SupportHub Ticket Attachments

## Metadata

* Task ID: `supporthub-ticket-attachments`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: explicit user request for the final standard follow-up
* Critique type: follow_up
* Earlier critique: `.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-4.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Perform the final standard follow-up critique, reconcile `PC-001` through `PC-009`, identify any remaining material blocker or contradiction, and write only this critique artifact.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/ticket-attachments-research.md`, `.github/copilot-instructions.md`, the current plan, critiques `ticket-attachments-plan-critique.md`, `ticket-attachments-plan-critique-2.md`, `ticket-attachments-plan-critique-3.md`, and `ticket-attachments-plan-critique-4.md`, plus the directly relevant AppHost, Tickets API, Web, migration, package, test, and roadmap files named by the plan.
* Decisions, dependencies, task Goals, and task Requirements considered: confirmed `D1`–`D4`; `P01`–`P04`; `FR-001`–`FR-009`; `NFR-001`–`NFR-003`; service ownership, storage/container, migration, service-discovery, Web, Azurite, testing, and runtime-verification dependencies.
* Assessment boundary: The supplied plan, research, prior critiques, repository instructions, and named repository files only. This standard-depth assessment does not implement, compile, run, or browser-test the feature and does not broaden research or independently validate pinned APIs.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|-------------------------------------------|----------|---------------------|
| `PC-001`–`PC-009` | Covered/resolved | Ticket-delete cleanup, container naming, Web routing and browser stimulus, focused failure seams, bounded multipart handling, confirmed decisions, and critique provenance are now represented consistently in the substantive plan and handoff records. |
| `FR-001`–`FR-009`, `NFR-001`–`NFR-003`; `P01`–`P04` | Partial | The implementation and verification tasks are materially complete for planning, but `P01-T02` retains a conditional statement that treats ticket deletion as potentially outside the feature, contradicting confirmed `D4` and the explicit `P02-T04` lifecycle. |
| `D1`–`D4`; Planning Readiness; Critique Disposition; Artifact Self-Check | Covered with one stale task detail | Confirmed decisions and the prior critique chain are correctly recorded; the remaining contradiction is localized to the metadata-foundation task wording. |

## Verdict

* Verdict: Revise
* Rationale: The plan resolves `PC-001` through `PC-009` and has no remaining decision-critical architecture or test-seam gap within the supplied standard-depth boundary. However, `P01-T02` still says ticket deletion may remain outside this feature, while `D4` and `P02-T04` mandate attachment cleanup as part of the feature. Remove that stale alternative before final implementation handoff.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| `PC-001`: Existing ticket deletion has no attachment cleanup contract | resolved | `D4` and `P02-T04` define cleanup-before-ticket-delete, abort-on-failure behavior, retryable state, telemetry, and integration coverage. |
| `PC-002`: Required Blazor behavior lacks an executable verification path | resolved | `P03-T03` fixes the same-origin download route and `P04-T04` exercises the running Web app and required stable UI states. |
| `PC-003`: Blob container contract is not fixed | resolved | `P02-T01` fixes `ticket-attachments` with idempotent application provisioning; `P04-T02` asserts the exact container and ticket-scoped names. |
| `PC-004`: Failure-compensation tests rely on an unspecified fault seam | resolved | `P02-T03` and `P04-T01` place coordinator failure tests behind focused injected fakes; `P04-T02` limits distributed tests to real-resource paths. |
| `PC-005`: Approval-gated policy defaults conflict with implementation-ready status | resolved | `D1`, `D2`, and `D4` are confirmed user decisions, and readiness is gated only by the final critique. |
| `PC-006`: Server-side bounded multipart streaming is not implementation-determinate | resolved | `P02-T01` selects `MultipartReader`, an 11 MiB request ceiling, a 10 MiB counting-stream limit, cancellation, and idempotent container setup; `P02-T02`/`P04-T02` define `413` and zero-row coverage. |
| `PC-007`: Browser delete-failure verification has no reproducible stimulus | resolved | `P04-T04` uses a seeded missing-blob download failure for browser retry-state verification; storage-operation failures remain in `P04-T01`. |
| `PC-008`: Readiness record retains stale approval-gate language | resolved | The readiness record states that `D1`, `D2`, and `D4` are confirmed and only the final critique gates implementation. |
| `PC-009`: Final handoff metadata contradicts the confirmed decisions and critique history | resolved | Critique Disposition and Artifact Self-Check enumerate the prior critique chain and state that no policy approval gate remains. |

## Findings

<!-- rpi:critique id=PC-010 -->
### PC-010 [Medium]: Metadata-foundation task retains a contradictory ticket-delete alternative

* Related IDs: `D4`, `FR-004`, `FR-007`, `FR-008`, `NFR-003`, `P01-T02`, `P02-T04`, `P04-T02`
* Evidence: In `.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md`, `D4` confirms that existing ticket deletion cleans every attachment before deleting the ticket, and `P02-T04` assigns that implementation. But `P01-T02` still states: “if ticket deletion remains outside this feature, record the required integration behavior in the affected task rather than adding a second lifecycle.”
* Concern: The plan presents an obsolete alternative after selecting ticket-delete cleanup as an in-scope requirement. An implementer could interpret the conditional as permission to leave the existing delete endpoint unchanged, despite the explicit decision and phase task.
* Impact: Ticket deletion could still strand attachment metadata or blobs, or implementation sequencing could omit the required endpoint change and its migration/lifecycle tests.
* Smallest useful change: Replace the conditional sentence in `P01-T02` with a direct dependency on the confirmed `D4` contract, stating that the metadata model/migration must support the `P02-T04` ticket-delete cleanup path and its `P04-T02` coverage.
* Action owner: Planning parent
* Exact resolving evidence: `P01-T02`, `D4`, `P02-T04`, and `P04-T02` all state the same in-scope ticket-delete cleanup policy without an alternative or deferral clause.
* Decision route: Direct planner correction.

## Strengths and Residual Risk

* The plan preserves Tickets API ownership, exact `storage`/`blobs`/`ticketsdb` boundaries, C#-only scope, service discovery, and existing migration/test conventions.
* Ticket deletion, container naming/provisioning, same-origin download routing, focused failure seams, blob-first compensation, bounded multipart handling, browser missing-content verification, and critique lineage are explicit.
* The plan correctly separates deterministic coordinator failure tests from real AppHost/Azurite lifecycle tests and avoids a production-visible fault-injection surface.
* Identity authorization, malware scanning, public/SAS URLs, reconciliation infrastructure, and later stack layers remain intentionally deferred.
* Dual-store orphan/stale-row risk remains an explicit operational residual because no cross-store transaction or reconciliation worker is introduced.

## Questions or Blocking Evidence Gaps

* No decision-critical research or architecture gap remains within the supplied standard-depth boundary.
* The stale `P01-T02` conditional must be removed so the final handoff does not contradict `D4` and `P02-T04`.

## Limitations

* No application code, plan, research, or prior critique was modified.
* No implementation, build, test, Aspire runtime, or browser execution was performed; this artifact assesses plan credibility from supplied evidence only.
* Pinned .NET/Aspire APIs were not independently compiled or re-researched because the caller requested standard depth and no broadened research.

## Recommended Next Action

* Highest-impact finding: `PC-010`
* Action owner: Planning parent
* Smallest next action: Remove the obsolete ticket-delete deferral from `P01-T02`, then rerun or record the final handoff as passing.
* User response required: No.

| [.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md](.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md) | Current implementation plan |
| [.copilot-tracking/research/2026-10-18/ticket-attachments-research.md](.copilot-tracking/research/2026-10-18/ticket-attachments-research.md) | Supplied research evidence |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture and verification constraints |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md) | Initial critique reconciled above |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-2.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-2.md) | Second critique reconciled above |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-3.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-3.md) | Third critique reconciled above |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-4.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-4.md) | Fourth critique reconciled above |

## Next Steps

Revise the plan through the active planning parent to close `PC-010`; no user action is required.
