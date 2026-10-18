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
* Earlier critique: .copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-2.md

## Inputs and Criterion Boundary

* Task context and caller requirements: Perform the final standard follow-up critique, reconcile `PC-001` through `PC-006`, identify any remaining material blocker or contradiction, and write only this artifact.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/ticket-attachments-research.md`, `.github/copilot-instructions.md`, the current plan, both earlier critiques, and the directly relevant AppHost, Tickets API, Web, migration, package, and test files named by the plan.
* Decisions, dependencies, task Goals, and task Requirements considered: confirmed `D1`–`D4`; `P01`–`P04`; `FR-001`–`FR-009`; `NFR-001`–`NFR-003`; service ownership, storage/container, migration, service-discovery, Web, Azurite, testing, and runtime-verification dependencies.
* Assessment boundary: The supplied plan and named repository evidence only. This is a standard-depth credibility assessment, not implementation, compilation, runtime, browser, or external-documentation validation. No broadened research was performed.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| `PC-001`–`PC-005` | Covered/resolved | The revision records ticket-delete cleanup, exact container/provisioning, same-origin Web routing and browser verification, focused failure seams, and confirmed policy decisions. |
| `PC-006`, `FR-001`, `FR-005`, `FR-008`, `NFR-001` | Covered with implementation-time validation | `P02-T01`/`P02-T02` now name `MultipartReader`, an 11 MiB request ceiling, a 10 MiB counting-stream limit, `413`, and no-row integration coverage. The exact pinned API/configuration still belongs to implementation validation, but the plan is materially determinate. |
| `FR-009`, `P03-T04`, `P04-T04` | Partial | The browser scenarios include retryable delete failure, but the supplied plan does not define a reproducible browser-facing failure stimulus while runtime fault injection is explicitly excluded. |
| Planning readiness and handoff | Partial | `D1`/`D2`/`D4` are confirmed, yet the readiness table still describes planner defaults as approval-gated, creating stale gate language. |

## Verdict

* Verdict: Revise
* Rationale: The revision resolves `PC-001` through `PC-006` sufficiently for implementation planning, including the previously open distributed-test seam and bounded multipart contract. Two material handoff issues remain: the required browser delete-failure scenario has no executable stimulus, and the readiness record contradicts the confirmed decision status. These are small planner corrections, but the plan should not be handed off as final until they are corrected.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| `PC-001`: Existing ticket deletion has no attachment cleanup contract | resolved | `D4` and `P02-T04` define cleanup-before-ticket-delete, abort-on-failure behavior, retryable state, telemetry, and `P04-T02` coverage. |
| `PC-002`: Required Blazor behavior lacks an executable verification path | resolved | `P03-T03` fixes `GET /tickets/{ticketId}/attachments/{attachmentId}/download`; `P04-T04` names running-app browser checks for loading, empty, upload, rejection, download, delete, and retry states. The remaining failure-stimulus gap is recorded separately as `PC-007`. |
| `PC-003`: Blob container contract is not fixed | resolved | `P02-T01` fixes `ticket-attachments` and idempotent application provisioning; `P04-T02` requires assertions against that exact container and ticket-scoped names. |
| `PC-004`: Failure-compensation tests rely on an unspecified fault seam | resolved | `P02-T03` places injected-fake coordinator tests in `SupportHub.Tickets.Api.Tests`, whose project references the Tickets API; `P04-T02` explicitly limits the distributed suite to real-resource paths. |
| `PC-005`: Approval-gated policy defaults conflict with implementation-ready status | resolved in decisions, stale in readiness wording | `D1`, `D2`, and `D4` record confirmed user decisions. The readiness table still says planner defaults are approval-gated; see `PC-008`. |
| `PC-006`: Server-side bounded multipart streaming is not implementation-determinate | resolved | `P02-T01` selects `MultipartReader`, an 11 MiB bounded request, a 10 MiB counting stream, cancellation, and idempotent container setup; `P02-T02` and `P04-T02` define `413` and zero-metadata-row verification. |

## Findings

<!-- rpi:critique id=PC-007 -->
### PC-007 [Medium]: Browser delete-failure verification has no reproducible stimulus

* Related IDs: `FR-009`, `NFR-002`, `P03-T02`, `P04-T01`, `P04-T04`
* Evidence: `P04-T04` requires browser verification of a “retryable delete failure state,” while `P02-T03` explicitly prohibits runtime fault-injection endpoints/configuration and `P04-T01` confines injected fakes to focused Tickets API tests. No supported browser/runtime action is specified that causes the Web delete request to return the failure being asserted.
* Concern: The plan can verify successful browser deletion and API-level failure branches separately, but cannot reproducibly demonstrate the required user-visible delete failure/retry state through the running Web app.
* Impact: A meaningful UI error path may remain untested, or implementation may introduce an undocumented production fault hook solely to satisfy the browser scenario.
* Smallest useful change: Change `P04-T04` to verify a naturally reproducible missing-content/not-found retry state through the running app and retain storage-failure semantics in `P04-T01`, or define a bounded non-production browser-test stimulus and its activation path without exposing an operational fault surface.
* Action owner: Planning parent
* Exact resolving evidence: A revised browser task naming the request stimulus and expected status/state, plus either a passing running-app check or an explicit limitation that the storage-failure branch is covered only by the coordinator tests.
* Decision route: Direct planner correction; a production-visible fault-injection mechanism would require a significant user decision.

<!-- rpi:critique id=PC-008 -->
### PC-008 [Low]: Readiness record retains stale approval-gate language

* Related IDs: `D1`, `D2`, `D4`, `PC-005`, `Planning Readiness and Next Step`
* Evidence: The plan marks `D1`, `D2`, and `D4` as confirmed user decisions and records `PC-005` as resolved, but the readiness table still says “planner defaults are clearly marked for approval” and describes decision participation as user-owned approval of planner defaults.
* Concern: The handoff section gives conflicting signals about whether implementation is gated on policy approval, despite the plan’s explicit confirmation records.
* Impact: An implementer or parent workflow could incorrectly reopen settled policy decisions or begin with an unclear readiness state.
* Smallest useful change: Update the readiness participation/gate wording to state that `D1`, `D2`, and `D4` are confirmed and that only the final critique gate remains.
* Action owner: Planning parent
* Exact resolving evidence: A readiness table whose decision status and required gates agree with `D1`–`D4` and the final critique verdict.
* Decision route: Direct planner correction.

## Strengths and Residual Risk

* The plan preserves Tickets API ownership, exact `storage`/`blobs`/`ticketsdb` boundaries, C#-only scope, service discovery, and the existing migration/test conventions.
* Ticket deletion, container naming/provisioning, same-origin download routing, focused failure seams, blob-first compensation, and bounded multipart handling are now explicit.
* `P04-T01` versus `P04-T02` correctly separates deterministic coordinator failure tests from real AppHost/Azurite lifecycle tests.
* Identity authorization, malware scanning, public URLs, reconciliation infrastructure, and later stack layers remain intentionally deferred.
* Dual-store orphan/stale-row risk remains a documented operational residual because no cross-store transaction or reconciliation worker is introduced.

## Questions or Blocking Evidence Gaps

* What supported running-app stimulus will produce the browser-visible retryable delete failure required by `P04-T04`?
* The exact .NET 10 request-limit API remains an implementation-time compile check, but no further planning decision is required if `MultipartReader` and the stated limits are retained.
* Does the readiness table now state the confirmed decision status rather than the superseded approval-gated default language?

## Limitations

* No application code, plan, research, or prior critique was modified.
* No implementation, build, test, Aspire runtime, or browser execution was performed; this artifact assesses plan credibility from supplied evidence only.
* Pinned SDK/Aspire APIs were not independently compiled or re-researched because the caller requested standard depth and no broadened research.

## Recommended Next Action

* Highest-impact finding: `PC-007`
* Action owner: Planning parent
* Smallest next action: Revise `P04-T04` with a reproducible browser failure stimulus or explicitly narrow that browser assertion to a naturally reproducible missing-content failure, then correct the stale readiness wording in `PC-008`.
* User response required: No for these planner corrections; yes only if the chosen browser-test mechanism introduces a production-visible fault-injection surface.

| [.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md](.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md) | Current implementation plan |
| [.copilot-tracking/research/2026-10-18/ticket-attachments-research.md](.copilot-tracking/research/2026-10-18/ticket-attachments-research.md) | Supplied research evidence |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture and verification constraints |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md) | Initial critique reconciled above |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-2.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique-2.md) | Previous follow-up critique reconciled above |

## Next Steps

Revise the plan through the active planning parent to close `PC-007` and `PC-008`; no user action is required unless the selected browser-test mechanism changes production behavior.
