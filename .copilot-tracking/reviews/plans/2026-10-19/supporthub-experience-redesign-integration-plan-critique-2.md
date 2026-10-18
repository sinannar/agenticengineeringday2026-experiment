<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub Experience Redesign & Integration

## Metadata

* Task ID: `supporthub-experience-redesign-integration`
* Critique date: 2026-10-19
* Plan: `.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default from the RPI Plan Critique contract
* Critique type: follow-up
* Earlier critique: `.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Follow-up standard critique of the revised plan; verify PC-001 through PC-007 and preserve the confirmed no-new-backend-capability, no-framework-migration, API ownership, service-discovery, business-rule, concrete console scope, canonical terminology, deterministic terminal testing, and reproducible Playwright/Aspire cleanup boundaries.
* Research and evidence considered: `.copilot-tracking/research/2026-10-19/supporthub-experience-redesign-integration-research.md`, `.github/copilot-instructions.md`, `src/SupportHub.AppHost/AppHost.cs`, `src/SupportHub.Sla.Console/Program.cs`, `src/SupportHub.Customers.Api/Features/Customers/CustomerEndpoints.cs`, `src/SupportHub.Tickets.Api/Features/Tickets/TicketEndpoints.cs`, and `src/SupportHub.Web/SupportHub.Web.csproj`.
* Decisions, dependencies, task Goals, and task Requirements considered: `D1-D6`, `FR-001` through `FR-010`, `NFR-001` through `NFR-007`, phases `P01-P06`, tasks `P01-T01` through `P06-T04`, the plan dependency lists, risks, and critique-disposition table.
* Assessment boundary: The supplied revised plan and evidence were assessed once for implementation blockers, contradictions, missing dependencies, unsupported acceptance claims, and scope compliance. No new external research was performed. This critique does not determine implementation details that the plan leaves to the implementer unless that omission affects reproducibility or acceptance credibility.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|---|---|---|
| `D5`, `FR-004`, `P04-T02` | Partial | The empty activity response is correctly not labeled as processing, but `P02-T02` still requires an activity processing state. |
| `D6`, `P05-T02` | Covered | Read-only customer/ticket clients, bounded output, and explicit sample-data unavailability are specified without adding backend capability. |
| `P05-T03`, `FR-007` | Partial | A shared vocabulary is required, but the plan still permits two independently maintained mappings rather than selecting one canonical seam. |
| `P02-T03`, `P06-T02` | Partial | Semantic/component coverage and a Playwright/AppHost procedure are named, but failure-state induction and exact fixture/artifact cleanup are not reproducible enough for all claimed acceptance paths. |
| `P05-T01`, `P06-T03`, `NFR-007` | Partial | A testable console boundary is required, but explicit-start behavior, scripted terminal inputs, constrained width, bounded fault cases, and teardown are not concretely specified. |
| `P05-T02`, `P06-T01` | Covered | Console artifacts, endpoint ownership, DTOs, formatters, cancellation, and focused tests are named. |
| `P06-T02`, `P06-T04`, `NFR-006` | Partial | Cleanup ordering is present, but the plan does not identify an exact fixture file, evidence directory protocol, or deterministic AppHost/persistence reset strategy. |
| Confirmed scope | Covered | The plan preserves Blazor, existing API ownership, service discovery, business rules, AppHost resources, and the no-new-backend boundary. |

## Verdict

* Verdict: Revise
* Rationale: PC-002 and PC-006 are resolved, and the plan materially improves PC-001, PC-004, PC-005, and PC-007. However, a stale activity-processing requirement remains, the terminology option does not require a single canonical seam, and the browser/terminal acceptance procedures still contain unsupported reproducibility claims. These are implementation and verification blockers, not cosmetic concerns.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|---|---|---|
| `PC-001` | Still open | `D5`, `FR-004`, and `P04-T02` correctly reject an unsupported activity-processing label, but `P02-T02` still requires distinguishing activity “processing”; the plan contradicts itself. |
| `PC-002` | Resolved | `D6` and `P05-T02` explicitly make sample-data generation unavailable/removed and define read-only list clients using existing APIs. |
| `PC-003` | Partially resolved | `P05-T03` adds a conformance-test requirement and names a shared seam option, but it also permits duplicate mappings; no single canonical terminology owner is selected. |
| `PC-004` | Partially resolved | `P02-T03` and `P06-T02` provide a concrete Playwright direction, but the complete set of claimed error/state paths lacks deterministic induction and exact evidence/fixture procedure. |
| `PC-005` | Partially resolved | `P05-T01` specifies an interaction boundary and bounded behavior, but `P06-T03` does not define the concrete terminal harness, explicit-start sequence, constrained-width method, or teardown evidence. |
| `PC-006` | Resolved | `P05-T02` names customer/ticket clients, DTOs, formatter, vocabulary, error translation, cancellation, and focused tests with sequencing. |
| `PC-007` | Partially resolved | `P06-T02` adds isolated IDs, upload, bounded activity polling, dependency-order deletion, and cleanup intent, but fixture path, artifact cleanup, and deterministic environment reset remain unspecified. |

## Findings

<!-- rpi:critique id=PC-008 -->
### PC-008 [High]: Activity processing requirements contradict the confirmed contract boundary

* Related IDs: `D5`, `FR-002`, `FR-004`, `NFR-004`, `P02-T02`, `P04-T02`
* Evidence: `P02-T02` still requires an activity “processing” state, while `D5`, `FR-004`, and `P04-T02` explicitly state that the current activity contract cannot identify processing and that an empty successful response must not be labeled that way. `src/SupportHub.Tickets.Api/Features/Tickets/TicketEndpoints.cs` exposes empty success, unavailable, invalid response, and not-found distinctions only.
* Concern: An implementer can satisfy `P02-T02` by inventing a processing inference or an unsupported UI state, violating the no-new-backend-capability boundary and making acceptance contradictory.
* Impact: Activity semantics may regress into the exact fabricated state the first critique rejected.
* Smallest useful change: Remove “processing” from the activity-specific requirement in `P02-T02`; state that processing is covered only for UI operations with an observable local signal (such as upload/save), or remove it from the global requirements if no such signal is in scope. Align the task mapping and risk text with `D5`.
* Action owner: Planner
* Exact resolving evidence: `P02-T02`, `FR-002`, `FR-004`, and `NFR-004` contain no activity-processing acceptance claim unsupported by the current API contract.
* Decision route: Direct planner correction.

<!-- rpi:critique id=PC-009 -->
### PC-009 [High]: Canonical terminology ownership remains optional rather than selected

* Related IDs: `FR-007`, `NFR-004`, `P05-T03`, `P06-T01`
* Evidence: `P05-T03` permits either “a small neutral shared presentation-contract project/file” or “two explicitly named presentation mappings” covered by a conformance test. The latter leaves two sources of truth and does not establish the requested canonical terminology seam. The task also defers selection to a changes record owned by the later implementation phase.
* Concern: Cross-surface agreement can pass initially while web and console mappings later diverge; a conformance test detects drift but does not provide canonical ownership.
* Impact: Status, timestamp, and unavailable/error wording can remain duplicated and implementation-critical terminology decisions are not made before coding.
* Smallest useful change: Select one dependency-safe canonical artifact in the plan before implementation, name its project/file and allowed dependencies, and make both renderers/formatters consume it. Do not defer this selection to the implementation changes record.
* Action owner: Planner
* Exact resolving evidence: `P05-T03` names one canonical artifact, its concrete path/dependency direction, and a test that proves both web and console consume the same mapping.
* Decision route: Direct planner correction unless the user rejects adding a neutral presentation-only shared seam.

<!-- rpi:critique id=PC-010 -->
### PC-010 [High]: Browser acceptance claims are not fully reproducible for failure states and cleanup

* Related IDs: `FR-004`, `FR-005`, `FR-010`, `NFR-003`, `NFR-004`, `P06-T02`, `P06-T04`
* Evidence: `P06-T02` claims verification of unavailable, invalid-response, not-found, and request-failed activity states, plus attachment and destructive cleanup, but only specifies creating a customer/ticket and polling activity. It does not specify how each dependency failure is induced deterministically, the exact repository-local upload fixture path, the exact evidence directory/procedure, or an AppHost/persistence isolation/reset mode. `AppHost.cs` shows `supporthub-cli` is explicitly started and the application uses persistent resources unless test settings are supplied.
* Concern: An implementer may be unable to reproduce several acceptance paths without ad hoc service manipulation, and cleanup may leave database/blob artifacts or delete unrelated local data.
* Impact: Browser evidence can be incomplete or non-repeatable while still being reported as satisfying `FR-010` and the definition of done.
* Smallest useful change: Add a fixture protocol naming the exact local file, unique-ID convention, AppHost test/persistence settings, route list, evidence directory, bounded activity timeout, and cleanup assertions. Define deterministic ways to exercise each claimed error state (or explicitly downgrade those to source/unit/API evidence when the runtime cannot induce them safely).
* Action owner: Planner
* Exact resolving evidence: A bounded Playwright/AppHost runbook in `P06-T02` proves each retained browser claim and leaves no created customer, ticket, blob, or evidence fixture behind except declared evidence artifacts.
* Decision route: Direct planner correction; a user decision is required only if a claimed fault state cannot be safely induced within the confirmed scope.

<!-- rpi:critique id=PC-011 -->
### PC-011 [High]: Terminal verification is described as bounded but not operationally deterministic

* Related IDs: `FR-006`, `FR-007`, `FR-009`, `FR-010`, `NFR-007`, `P05-T01`, `P06-T03`
* Evidence: `src/SupportHub.AppHost/AppHost.cs` configures `supporthub-cli` with `.WithExplicitStart().WithTerminal()`. `P06-T03` says to wait for `supporthub-cli` “as applicable,” use a bounded terminal script/harness, and test a narrow width or equivalent condition, but does not state how to explicitly start the resource, inject menu/health-watch/exit input, constrain width, induce an API error, set timeouts, or prove resource/AppHost teardown.
* Concern: The acceptance procedure still depends on interactive Dashboard behavior and an unspecified harness; the real `Console.ReadLine`, `Console.KeyAvailable`, and `Console.ReadKey` paths cannot be proven deterministic from the plan alone.
* Impact: Console compatibility, cancellation, error, narrow-width, and cleanup claims may be unverifiable or hang in CI/manual execution.
* Smallest useful change: Define one concrete terminal protocol (for example, the repository’s supported terminal automation) with explicit `supporthub-cli` start, scripted inputs including EOF/Q/exit, fixed terminal dimensions, bounded command/watch timeouts, a deterministic unavailable/error case, captured output path, and exact stop/health verification for the AppHost and explicit resource.
* Action owner: Planner
* Exact resolving evidence: `P06-T03` contains a runnable, bounded terminal sequence that exercises the required commands and records clean teardown and output artifacts without relying on undocumented Dashboard interaction.
* Decision route: Direct planner correction.

## Strengths and Residual Risk

* The revision credibly preserves the confirmed architecture: no new backend capability, no frontend-framework migration, unchanged API ownership and service discovery, no fabricated sample data, and no activity-processing inference.
* Console command scope is now concrete for read-only customer/ticket lists and explicit sample-data unavailability.
* Residual risk remains around implementer-selected visual decomposition and the availability of browser fault injection; those are acceptable only after the procedures above are made explicit.

## Questions or Blocking Evidence Gaps

* Can the planned browser/runtime harness safely induce activity unavailable and invalid-response states without modifying application code or AppHost ownership? If not, those claims must be reassigned to deterministic API/unit evidence rather than asserted as Playwright acceptance.
* What single canonical presentation artifact will both `SupportHub.Web` and `SupportHub.Sla.Console` reference?

## Limitations

* No new research or runtime execution was performed, per the standard critique boundary. Findings are limited to supplied artifacts and directly relevant source files.

## Recommended Next Action

* Highest-impact finding: `PC-010` and `PC-011` jointly block credible reproducible verification; `PC-008` is the highest contract contradiction.
* Action owner: Planner.
* Smallest next action: Revise `P02-T02`, select the canonical vocabulary seam in `P05-T03`, and add concrete Playwright and terminal runbooks with deterministic fault/fixture/teardown evidence.
* User response required: No for the planner-owned corrections; yes only if safe fault injection or a shared presentation seam would materially expand the confirmed scope.

| [.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md](.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md) | Revised implementation plan assessed by this follow-up |
| [.copilot-tracking/research/2026-10-19/supporthub-experience-redesign-integration-research.md](.copilot-tracking/research/2026-10-19/supporthub-experience-redesign-integration-research.md) | Supplied research evidence |
| [.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique.md](.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique.md) | Earlier critique reconciled above |

## Next Steps

Revise the plan for `PC-008` through `PC-011` and rerun the follow-up gate; no application code was modified.
