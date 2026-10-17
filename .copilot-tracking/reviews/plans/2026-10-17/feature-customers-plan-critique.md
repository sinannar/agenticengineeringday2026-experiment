<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub Customer Management

## Metadata

* Task ID: `feature-customers`
* Critique date: 2026-10-17
* Plan: `.copilot-tracking/plans/2026-10-17/feature-customers-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: initial
* Earlier critique: not applicable

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the complete Customer Management plan for Customers API CRUD, EF Core/PostgreSQL/`customersdb`, validation and status codes, API-backed Blazor list/detail/create/edit/delete, API and Aspire integration tests, runtime verification, ownership boundaries, dependencies, risks, and scope.
* Research and evidence considered: `.copilot-tracking/research/2026-10-17/feature-customers-research.md`, `.github/copilot-instructions.md`, and the directly referenced AppHost, Customers API, Web, and test files.
* Decisions, dependencies, task Goals, and task Requirements considered: Confirmed `Standard`/`Premium`/`Enterprise` tiers, existing `customersdb`/`customersapi`/`webfrontend` graph, API ownership, three implementation phases P01-P03, task dependencies, and stated migration/browser-test deferrals.
* Assessment boundary: The supplied plan and evidence support architectural and implementation-readiness assessment only. No application code, deployment policy, or browser test infrastructure was assumed beyond the supplied files.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| FR-001–FR-006, NFR-001–NFR-004 | Covered | P01 and P02 define service-owned EF persistence, explicit DTOs, CRUD routes, validation, status codes, cancellation, and API-only Web access; this matches the existing AppHost and repository boundaries. |
| FR-007 | Partial | P02 specifies all required Blazor flows and UI states, but P03-T03 makes browser-level verification optional. Health checks and backend tests cannot establish that list/detail/create/edit/delete interactions actually work. |
| FR-008, NFR-005 | Partial | P03 defines API and Aspire coverage, but the migration/test-database setup is left to implementation (“select ... during implementation”), so deterministic schema creation is not yet an acceptance-ready dependency. |
| P01-T01–P01-T03 | Covered | Persistence, contract separation, validation, error shape, endpoint status codes, and ownership are coherently sequenced. |
| P02-T01–P02-T03 | Covered | Typed service-discovery client and API-backed UI workflows are bounded without adding infrastructure or crossing ownership boundaries. |
| P03-T01 | Partial | Its requirements promise API-boundary status/contract behavior, while its details constrain the Customers API test project to validation/mapping/domain tests without a running host; the real endpoint assertions are deferred to P03-T02. |
| P03-T02–P03-T03 | Partial | Runtime topology and HTTP persistence checks are specified, but migration application and UI interaction evidence are not concretely required. |

## Verdict

* Verdict: Revise
* Rationale: The plan is architecturally credible and preserves the requested scope and ownership boundaries, but it is not implementation-ready until deterministic migration setup and acceptance evidence for the Blazor workflows are made explicit. One test-task boundary is also internally inconsistent.

## Findings

<!-- rpi:critique id=PC-001 -->
### PC-001 [High]: Distributed verification does not prove the required Blazor workflows

* Related IDs: FR-007, FR-008, P02-T02, P02-T03, P03-T03, D5
* Evidence: `.copilot-tracking/plans/2026-10-17/feature-customers-plan.md` P02-T02/P02-T03 specifies list, detail, create, edit, and delete behavior, while P03-T03 says browser-level verification is used “only if” behavior cannot be proven through existing seams. The supplied Web files show only an empty `CustomerApiClient` and template UI; `.github/copilot-instructions.md` requires frontend behavior verification when applicable.
* Concern: The planned API and Aspire checks can prove backend CRUD and resource health but cannot prove that Blazor routes, forms, event handling, API error rendering, navigation, and delete confirmation work end to end. Treating browser verification as optional leaves FR-007 without an objective completion signal.
* Impact: A broken or unwired customer UI could be handed off while all planned tests pass.
* Smallest useful change: Make P03-T03 require one explicit UI verification path after implementation: either add an existing-compatible browser/component test seam covering the five workflows and key states, or record a concrete manual/browser verification checklist with evidence for list, detail, create, edit, delete, validation failure, not-found, loading, empty, and error states. Keep D5 as a tooling decision only if the chosen seam is genuinely unavailable.
* Action owner: planning parent
* Exact resolving evidence: P03-T03 names the selected UI verification method and its cases; the implementation changes record contains successful evidence for each required workflow and state.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-002 -->
### PC-002 [Medium]: Migration application is underspecified for deterministic integration tests

* Related IDs: FR-006, FR-008, NFR-005, P01-T02, P03-T02, P03-T03
* Evidence: P01-T02 says to “select a controlled local/test migration application mechanism during implementation,” and P03-T02 says “if migrations are applied explicitly” without making schema initialization mandatory. Research C9/C11-C14 confirms there is no existing DbContext, migration, or database setup pattern.
* Concern: The real Aspire CRUD test graph cannot reliably exercise persistence until the customer schema exists. The plan does not identify who applies the migration, when it runs, how the test database is isolated/reset, or what proves the migration was applied.
* Impact: Integration tests may fail on a clean environment, depend on a mutated developer database, or silently validate only an already-existing schema.
* Smallest useful change: Make P01-T02 and P03-T02 specify one deterministic local/test mechanism (for example, an explicitly scoped test setup that applies the service-owned migration against the test graph) plus cleanup/isolation and a schema-applied assertion; keep production migration policy deferred.
* Action owner: planning parent
* Exact resolving evidence: The plan identifies the mechanism, scope, ordering, isolation strategy, and success assertion; P03-T02 passes from a clean database/resource state.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-003 -->
### PC-003 [Medium]: P03-T01 mixes incompatible unit-test and API-boundary objectives

* Related IDs: FR-004, FR-005, FR-008, P03-T01, P03-T02
* Evidence: P03-T01 requires status codes and meaningful API response/error contracts, but its Details require replacing `UnitTest1` with tests that “do not require a running distributed graph.” The current `tests/SupportHub.Customers.Api.Tests` project has no reference to the Customers API project, while the existing HTTP seam is in `tests/SupportHub.AppHost.Tests/CustomerApiTests.cs`.
* Concern: The task does not establish a viable host/test seam for endpoint behavior and simultaneously defers real HTTP assertions to P03-T02. This makes its stated API-boundary coverage ambiguous and risks either brittle mocks or untested endpoint mapping.
* Impact: Validation/status-code regressions could be attributed to P03-T01 without being testable there, or endpoint tests could be added ad hoc outside the planned dependency and project boundaries.
* Smallest useful change: Narrow P03-T01 to pure validation/mapping/domain tests and move all HTTP status/error-contract acceptance explicitly to P03-T02, or add the required Customers API project/host reference and a documented endpoint test seam. Do not count the same cases as both unit and distributed coverage without a reason.
* Action owner: planning parent
* Exact resolving evidence: P03-T01 has a single feasible test scope, and P03-T02 explicitly lists every endpoint status and structured-error assertion that remains the API-boundary proof.
* Decision route: direct planner correction

## Strengths and Residual Risk

* The plan correctly preserves Customers API ownership, keeps Web free of EF/PostgreSQL dependencies, reuses exact Aspire resource names, separates DTOs from persistence entities, and avoids speculative shared projects or infrastructure.
* CRUD endpoint shape, selected support tiers, asynchronous I/O, status codes, and cross-service dependency order are concrete and traceable to the supplied requirements and research.
* Production migration execution remains a legitimate downstream risk rather than an architecture blocker, provided local/test schema setup is made deterministic as above.

## Questions or Blocking Evidence Gaps

* No decision-critical user question is required. The remaining gaps are planner-owned: select the test migration mechanism and define UI verification evidence.

## Limitations

* This standard critique did not inspect unreferenced source files, add implementation, start Aspire, or perform open-ended research. It cannot assess runtime feasibility beyond the supplied source/test seams.

## Recommended Next Action

* Highest-impact finding: PC-001
* Action owner: planning parent
* Smallest next action: Revise P03-T03 to require concrete Blazor workflow verification, specify the deterministic migration/test setup in P01-T02/P03-T02, and reconcile P03-T01 with P03-T02 before implementation handoff.
* User response required: no

## Next Steps

| [`.copilot-tracking/plans/2026-10-17/feature-customers-plan.md`](.copilot-tracking/plans/2026-10-17/feature-customers-plan.md) | Assessed implementation plan |
| [`.copilot-tracking/research/2026-10-17/feature-customers-research.md`](.copilot-tracking/research/2026-10-17/feature-customers-research.md) | Supplied research baseline |
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | Repository constraints and definition of done |
| [`.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique.md`](.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique.md) | Completed critique artifact |

No user action is required; the planning parent should revise the plan before implementation.
