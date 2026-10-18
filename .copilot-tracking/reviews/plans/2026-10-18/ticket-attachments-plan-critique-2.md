<!-- markdownlint-disable-file -->

# RPI Plan Critique: SupportHub Ticket Attachments

## Metadata

* Task ID: `supporthub-ticket-attachments`
* Critique date: 2026-10-18
* Plan: .copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: follow_up
* Earlier critique: .copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md

## Inputs and Criterion Boundary

* Task context and caller requirements: Perform the standard follow-up critique of the revised SupportHub Ticket Attachments plan; reconcile every earlier `PC-001` through `PC-005`; do not edit the plan, application, research, or prior critique; write only this artifact.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/ticket-attachments-research.md`, `.github/copilot-instructions.md`, the revised plan, the earlier critique, and the directly relevant AppHost, Tickets API, Web, package, migration, and test files named by the plan.
* Decisions, dependencies, task Goals, and task Requirements considered: confirmed `D1`-`D4`; `P01`-`P04` and all task requirements; `FR-001` through `FR-009`; `NFR-001` through `NFR-003`; storage, migration, service-discovery, Web, Azurite, test, and runtime dependencies.
* Assessment boundary: The supplied plan and named repository evidence only. This is a standard-depth credibility assessment, not implementation, compilation, runtime, browser, or external-documentation validation. No open-ended research was performed.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|--------------------------------------------|----------|---------------------|
| `FR-001`–`FR-009`, `NFR-001`–`NFR-003` | Partial | The revised plan closes the earlier lifecycle, container, Web-route, and policy gaps, but deterministic failure injection across the real Aspire process and the server-side multipart streaming boundary remain insufficiently specified. |
| `P01`–`P02` | Partial | Metadata, route ownership, container naming, ticket-delete cleanup, and dual-store ordering are explicit; the implementation seam for forcing metadata/storage failures is not connected to the distributed test harness. |
| `P03`–`P04` | Partial | The exact same-origin route and browser scenarios are now named, but the plan still leaves the server request parser/streaming mechanism and integration fault configuration unresolved. |
| `D1`–`D4` | Covered | The revised plan records the selected policy, delete ordering, route-scoped boundary, and ticket-delete cleanup decision as confirmed direction. |

## Verdict

* Verdict: Revise
* Rationale: The revision resolves four earlier findings and records the previously approval-gated policy and cleanup decisions. However, the failure tests still cannot be executed against the actual AppHost graph from the supplied test project without a defined injection/configuration path, and the required bounded server streaming behavior is described as an outcome rather than an implementable request-parsing contract.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| `PC-001`: Existing ticket deletion has no attachment cleanup contract | Resolved | `D4` and `P02-T04` define cleanup-before-ticket-delete, abort-on-failure behavior, retryable state, telemetry, and `P04-T02` coverage. |
| `PC-002`: Required Blazor behavior lacks an executable verification path | Resolved | `P03-T03` fixes `GET /tickets/{ticketId}/attachments/{attachmentId}/download`; `P04-T04` names running-app browser checks for loading, empty, upload, rejection, download, delete, and retry states. The remaining compile-time API check is correctly implementation-time validation, not a missing route contract. |
| `PC-003`: Blob container contract is not fixed | Resolved | `P02-T01` fixes `ticket-attachments` and idempotent application provisioning; `P04-T02` requires assertions against that exact container and ticket-scoped names. |
| `PC-004`: Failure-compensation tests rely on an unspecified fault seam | Still open | `P02-T03` names `IAttachmentBlobStore` and a metadata boundary, but `P04-T02` asks an `Aspire.Hosting.Testing` process started through the existing AppHost to force those failures. The plan does not define how the test process replaces or configures those production services, and `tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj` currently references the AppHost rather than the Tickets API implementation. |
| `PC-005`: Approval-gated policy defaults conflict with implementation-ready status | Resolved | `D1`, `D2`, and `D4` are recorded as confirmed user decisions, and readiness is explicitly held only for this follow-up critique. |

## Findings

<!-- rpi:critique id=PC-004 -->
### PC-004 [High]: Distributed failure tests still lack an executable fault-injection path

* Related IDs: `FR-008`, `FR-004`, `P02-T03`, `P02-T04`, `P04-T02`, `NFR-002`
* Evidence: `.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md` P02-T03 defines internal side-effect seams, while P04-T02 requires the real AppHost test to force metadata-write and blob-delete failures. The current `tests/SupportHub.AppHost.Tests/SupportHub.AppHost.Tests.csproj` references only `SupportHub.AppHost`, and `src/SupportHub.Tickets.Api/Program.cs` has no documented test-only replacement/configuration hook.
* Concern: A distributed test cannot use an in-process mock or internal interface merely because the production feature defines one. Without a named AppHost override, test environment switch, injectable test endpoint, or a deliberate split placing these cases in a Tickets API test project, the required compensation and delete-failure tests are not reproducible.
* Impact: The highest-risk cross-store guarantees may be claimed without actually exercising the failure branches, or implementation may add an unsafe/undocumented production fault hook.
* Smallest useful change: Choose one bounded mechanism and assign it explicitly: either move seam-driven failure cases to focused Tickets API tests with a production DI composition test, or define a test-only configuration/host override that the AppHost test can activate without exposing an operational fault endpoint. Keep at least one real Azurite success path in the distributed suite.
* Action owner: Planning parent
* Exact resolving evidence: A named test project/task and concrete registration/configuration path showing how each failure is forced, plus assertions for compensation, retained metadata, ticket-not-deleted state, and retry behavior.
* Decision route: Direct planner correction; a new production fault-injection surface would require a significant user decision.

<!-- rpi:critique id=PC-006 -->
### PC-006 [Medium]: Server-side bounded multipart streaming is not implementation-determinate

* Related IDs: `FR-001`, `FR-005`, `FR-008`, `NFR-001`, `P01-T01`, `P02-T01`, `P02-T02`, `P04-T01`
* Evidence: `src/SupportHub.Tickets.Api/Program.cs` currently uses the default ASP.NET pipeline with no multipart/request-size configuration. The plan says to “stream validated multipart content,” “enforce the configured maximum during request parsing/streaming,” and “reject oversized requests before database writes,” but does not select `MultipartReader`, a bounded `IFormFile` policy, or the exact request-limit configuration and failure mapping.
* Concern: The plan's no-whole-file-buffering requirement cannot be proven from the endpoint contract alone. A default form-binding implementation may buffer multipart content before endpoint validation, and request-limit rejection may occur outside the planned structured Problem Details path.
* Impact: The implementation could violate the stated DoS/allocation boundary or produce inconsistent oversized-file behavior while all listed policy tests still pass against in-memory inputs.
* Smallest useful change: Name the server upload mechanism and enforcement points: multipart parser/form-binding choice, maximum body and per-file limits, byte-counting stream behavior, cancellation propagation, and the expected response when the framework rejects an oversized request. Add one integration assertion that an over-limit request is rejected before metadata persistence.
* Action owner: Planning parent
* Exact resolving evidence: A task-level request-pipeline contract tied to the pinned .NET 10 APIs and an integration test proving bounded transfer and zero metadata rows after an over-limit request.
* Decision route: Direct planner correction.

## Strengths and Residual Risk

* The revised plan preserves Tickets API ownership, exact `storage`/`blobs`/`ticketsdb` resource boundaries, C#-only scope, and service discovery.
* `D1`/`D2`/`D4` now remove the earlier approval ambiguity; blob-first upload, compensation, blob-first delete, and ticket-delete cleanup are explicit.
* The named `ticket-attachments` container, same-origin Web route, and browser verification task materially improve implementation and acceptance credibility.
* Identity authorization, malware scanning, public URLs, reconciliation infrastructure, and later stack layers remain correctly deferred; the residual dual-store orphan risk is documented rather than hidden.

## Questions or Blocking Evidence Gaps

* How will the real AppHost test activate the metadata-write and blob-delete failure branches without an operational fault endpoint?
* Which pinned .NET 10 multipart/request-streaming mechanism enforces the 10 MiB boundary before metadata persistence, and what response does an early request-limit failure produce?

## Limitations

* No application code, plan, research, or prior critique was modified.
* No implementation, build, test, Aspire runtime, or browser execution was performed; this artifact assesses plan credibility from supplied evidence only.
* Pinned SDK/Aspire APIs were not independently compiled or re-researched because the request specified standard depth and no broadened research.

## Recommended Next Action

* Highest-impact finding: `PC-004`
* Action owner: Planning parent
* Smallest next action: Add the executable fault-injection/test-placement contract, then specify the bounded server multipart mechanism and its over-limit integration assertion.
* User response required: No for the direct planner corrections; yes only if the selected solution introduces a production-visible fault-injection surface or changes the confirmed API failure contract.

| [.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md](.copilot-tracking/plans/2026-10-18/ticket-attachments-plan.md) | Revised implementation plan |
| [.copilot-tracking/research/2026-10-18/ticket-attachments-research.md](.copilot-tracking/research/2026-10-18/ticket-attachments-research.md) | Supplied research evidence |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture and verification constraints |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-attachments-plan-critique.md) | Earlier critique reconciled above |

## Next Steps

Revise the plan through the active planning parent to close `PC-004` and `PC-006`; no user action is required unless the chosen fault-injection approach changes production behavior or the confirmed API contract.
