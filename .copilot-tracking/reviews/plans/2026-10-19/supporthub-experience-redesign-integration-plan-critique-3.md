<!-- markdownlint-disable-file -->
# Final Follow-up Standard Critique: SupportHub Experience Redesign & Integration

## Metadata

* Task: `supporthub-experience-redesign-integration`
* Critique date: 2026-10-19
* Plan: `.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md`
* Critique type: final follow-up, standard depth
* Earlier critiques: `supporthub-experience-redesign-integration-plan-critique.md` and `supporthub-experience-redesign-integration-plan-critique-2.md`
* Research: `.copilot-tracking/research/2026-10-19/supporthub-experience-redesign-integration-research.md`
* Repository instructions: `.github/copilot-instructions.md`

## Verdict

**Revise before implementation handoff.** PC-008 through PC-011 are resolved in substance, and the plan preserves the original C#-first Blazor/Aspire scope, service ownership, service discovery, business-rule, no-new-backend, no-framework-migration, and no-audit-capability constraints. Two verification details remain insufficiently executable: the browser runbook relies on a fixture that is not present and does not define deterministic induction for every retained request-failure claim; the terminal runbook names VHS without establishing the exact supported harness/script and lifecycle commands. These are narrow, planner-owned verification gaps rather than architectural blockers.

## PC-008–PC-011 verification

| Finding | Result | Evidence |
|---|---|---|
| `PC-008` activity-processing contradiction | **Resolved** | `D5`, `P02-T02`, `P04-T02`, and `P06-T02` restrict activity to observable empty, unavailable, invalid/not-found, and failed states; “processing” is explicitly limited to operation-local progress with a real signal. No activity-processing acceptance claim remains in conflict with the current contract. |
| `PC-009` canonical vocabulary ownership | **Resolved** | `P05-T03` selects `src/SupportHub.Presentation/SupportHub.Presentation.csproj` as the sole presentation-only vocabulary owner and requires both Web and Console to reference it, with shared conformance tests. |
| `PC-010` browser fixture/fault/cleanup evidence | **Partially resolved** | `P06-T02` now names a run ID convention, exact intended fixture path, evidence directory, bounded activity polling, API/unit fallback for unsafe activity faults, dependency-order cleanup, and cleanup assertions. The remaining executable details are recorded as `PC-012`. |
| `PC-011` deterministic Dashboard terminal verification | **Partially resolved** | `P06-T03` now requires explicit `supporthub-cli` start, scripted inputs including `Q`/EOF, `100x30`, per-command timeouts, unavailable-service restoration, captured output, explicit stop, and post-stop `aspire ps`. The remaining harness/command specificity is recorded as `PC-013`. |

## Coverage and scope assessment

* **Original constraints:** Covered. The plan retains Blazor Interactive Server, C#-only implementation, existing API ownership, Aspire resource names/references, typed service-discovery clients, Redis cache behavior, existing business rules, and the no-new-backend/no-new-audit/no-framework-migration boundaries.
* **Research alignment:** Covered. The plan treats empty activity as eventual-consistency/data-state evidence, keeps Activity Worker ownership intact, distinguishes real and placeholder console commands, and carries forward the browser/terminal verification limitations.
* **Functional requirements:** `FR-001` through `FR-010` are cited by phase tasks. The activity wording is now contract-safe, and sample-data generation is explicitly unsupported rather than fabricated.
* **Non-functional requirements:** `NFR-001` through `NFR-007` are cited by phase tasks. Accessibility, responsive behavior, service discovery, build/test, Aspire health, and terminal-safe output are represented.
* **Phase/task completeness:** Each phase and task has goals, requirements, details, references, and dependencies; the selected vocabulary project is explicitly added to the solution and consumed by Web and Console.
* **Residual planning inconsistency:** The plan’s own self-check and readiness section still say the final critique/readiness are pending. That is expected before the plan is reconciled with this artifact, but the handoff is not complete until the planner updates those records.

## Findings

<!-- rpi:critique id=PC-012 -->
### PC-012 [Medium/High]: Browser runbook still has an absent fixture and incomplete failure induction

* **Related IDs:** `FR-004`, `FR-005`, `FR-010`, `NFR-003`, `NFR-004`, `P06-T02`, `P06-T04`
* **Evidence:** `P06-T02` requires `tests/fixtures/supporthub-redesign-attachment.txt`, but that path does not currently exist in the repository. The same task claims browser coverage for “locally induced request-failure states” and successful empty/not-found/failure rendering without specifying the route or controlled mechanism for each non-activity failure. The task only explicitly assigns the unsafe activity unavailable/invalid-response cases to API/unit fallback.
* **Concern:** An implementer must invent a fixture, fault-induction method, or route list. A run can therefore omit a claimed customer/ticket/attachment request-failure path while still appearing to satisfy `FR-010`.
* **Impact:** Browser evidence is not fully reproducible, and a failed or interrupted run may leave resources/data unless the fixture and cleanup protocol are operationally defined.
* **Smallest resolving change:** Add the repository-local fixture as a planned implementation artifact (or state that the implementation must create it before the run), list exact route URLs and a verification matrix, and for every retained failure claim specify the deterministic induction method. If a failure cannot be safely induced through the supported AppHost/UI, downgrade it explicitly to API/unit evidence. Add an abort/finally cleanup path, including the dedicated persistence-test mode/volume option when needed, so partial runs cannot leave created rows or blobs.
* **Owner:** Planner
* **Resolution evidence required:** `P06-T02` names the fixture creation/ownership, exact route and state matrix, deterministic fault or API/unit fallback per state, timeout, and recovery cleanup procedure.

<!-- rpi:critique id=PC-013 -->
### PC-013 [Medium]: Terminal protocol names a harness but does not make the exact supported execution reproducible

* **Related IDs:** `FR-006`, `FR-009`, `FR-010`, `NFR-006`, `NFR-007`, `P05-T01`, `P06-T03`
* **Evidence:** `P06-T03` requires “the repository-supported VHS terminal automation” and says to start the isolated AppHost, explicitly start `supporthub-cli`, stop/restore an API, and stop the AppHost, but it does not identify a checked-in tape/script, exact command sequence, or the supported Aspire lifecycle commands for those actions. Repository Aspire guidance confirms `WithTerminal()` is an experimental interactive terminal, but does not itself establish VHS as the repository’s supported automation path.
* **Concern:** “Use VHS” can conceal environmental or Dashboard-specific assumptions. The requested evidence may still depend on ad hoc manual interaction, especially for explicit-start, API stop/restore, EOF, and post-stop verification.
* **Impact:** The terminal run may not be repeatable in CI or by another engineer, and a hang or incorrectly restored resource can invalidate the health/fault evidence.
* **Smallest resolving change:** Name one existing supported terminal procedure and its exact invocation (or add a repository-local tape/script as an implementation/evidence artifact), including the explicit `supporthub-cli` start command, scripted input stream, `100x30` dimensions, per-command timeout values, API stop/restore commands, output path, and exact `aspire stop`/`aspire ps` teardown checks. If VHS is only an illustrative option, replace it with the repository-supported protocol rather than presenting it as established.
* **Owner:** Planner
* **Resolution evidence required:** `P06-T03` contains a runnable bounded sequence with no undocumented interactive step and records successful fault restoration and clean AppHost teardown.

## Final disposition

* `PC-008`: resolved.
* `PC-009`: resolved.
* `PC-010`: substantially resolved, with `PC-012` remaining.
* `PC-011`: substantially resolved, with `PC-013` remaining.
* `PC-001`–`PC-007`: resolved by the current revision; no regression was found in their previously confirmed corrections.
* No application code or plan was modified by this critique.

## Recommended next action

Resolve `PC-012` and `PC-013`, then update the plan’s final self-check/readiness/disposition fields and rerun the standard gate. No user decision is required unless the team cannot safely induce a claimed browser fault or cannot provide a supported terminal automation path; in either case, narrow the acceptance claim rather than adding backend or orchestration capability.
