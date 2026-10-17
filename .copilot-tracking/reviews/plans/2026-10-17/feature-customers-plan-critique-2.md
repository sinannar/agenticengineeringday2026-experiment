<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub Customer Management

## Metadata

* Task ID: `feature-customers`
* Critique date: 2026-10-17
* Plan: `.copilot-tracking/plans/2026-10-17/feature-customers-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: follow-up
* Earlier critique: `.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique.md`
* Severity counts: Critical 0, High 0, Medium 1, Low 0

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess whether the revised Customer Management plan is credible for implementation, reconcile PC-001 through PC-003, and identify any newly introduced substantive gaps without editing the plan or application code.
* Research and evidence considered: `.copilot-tracking/research/2026-10-17/feature-customers-research.md`, `.github/copilot-instructions.md`, `src/SupportHub.AppHost/AppHost.cs`, `src/SupportHub.Customers.Api/Program.cs`, `src/SupportHub.Web/Program.cs`, `src/SupportHub.Web/CustomerApiClient.cs`, `tests/SupportHub.AppHost.Tests/CustomerApiTests.cs`, `tests/SupportHub.Customers.Api.Tests/SupportHub.Customers.Api.Tests.csproj`, and `tests/SupportHub.Customers.Api.Tests/UnitTest1.cs`.
* Decisions, dependencies, task Goals, and task Requirements considered: Confirmed `Standard`/`Premium`/`Enterprise` tiers, existing `customersdb`/`customersapi`/`webfrontend` topology, API ownership, P01-P03 sequencing, the local/test migration gate, browser/runtime verification decision D5, and the revised P03-T01/P03-T02 test boundary.
* Assessment boundary: The supplied plan and referenced evidence support an implementation-readiness assessment of API, persistence, Web, test, and Aspire verification design. This critique does not implement or run the application, validate package APIs, or choose a production migration policy outside the supplied scope.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| FR-001–FR-006, NFR-001–NFR-004 | Covered | P01 and P02 define service-owned EF persistence, explicit DTOs, CRUD routes, validation, status codes, cancellation, and API-only Web access consistent with the supplied repository boundaries. |
| FR-007 | Covered | P02 defines list/detail/create/edit/delete behavior and explicit UI states; revised P03-T03 requires Playwright/runtime verification or a documented manual checklist for the workflows and states. |
| FR-008, NFR-005 | Partial | API and Aspire coverage is concrete, but the claimed clean isolated database state and migration-gate configuration are not operationally specified against the existing persistent AppHost PostgreSQL volume. |
| P01-T01–P01-T03 | Covered | Persistence, contract separation, validation, error shape, endpoint status codes, and ownership are coherently sequenced. |
| P02-T01–P02-T03 | Covered | Typed service-discovery client and API-backed UI workflows are bounded without adding infrastructure or crossing ownership boundaries. |
| P03-T01 | Covered | The revision narrows this task to pure validation/mapping/domain tests and explicitly assigns HTTP boundary assertions to P03-T02. The Customers API test project already references the API project. |
| P03-T02–P03-T03 | Partial | Required HTTP lifecycle, status/error contracts, runtime health, logs, and UI verification are named, but migration execution and clean-state isolation lack a single concrete mechanism and proof. |

## Verdict

* Verdict: Revise
* Rationale: The revised plan resolves the UI verification gap and the unit-versus-distributed test ambiguity. It does not yet make the migration-backed integration test deterministic: the existing AppHost uses a persistent PostgreSQL data volume, while the plan only requires a “clean isolated” state and offers alternative migration gates without specifying their configuration or reset/isolation mechanism.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| PC-001 | resolved | P03-T03 now requires verification of list, detail, create, edit, delete, validation failure, not-found, loading, empty, and error states through the Playwright/runtime seam, with a documented manual fallback. |
| PC-002 | still open | P01-T02/P03-T02 now require a gated `Database.MigrateAsync` path and a first-create assertion, but they do not identify how the explicit gate is configured in the Aspire test graph or how the existing `postgres.WithDataVolume()` state is made clean and isolated. A first create proves only that a usable schema exists; it does not prove the intended migration ran on a fresh database. |
| PC-003 | resolved | P03-T01 is limited to pure behavior tests, while P03-T02 explicitly owns HTTP status, structured error, routing, and persistence assertions. The supplied Customers API test project has the API project reference needed for the narrowed unit scope. |

## Findings

<!-- rpi:critique id=PC-002 -->
### PC-002 [Medium]: Migration test isolation and gate configuration remain non-deterministic

* Related IDs: FR-006, FR-008, NFR-005, P01-T02, P03-T02, P03-T03
* Evidence: `src/SupportHub.AppHost/AppHost.cs` configures PostgreSQL with `.WithDataVolume()`. The revised plan says the test graph must use an explicit local/test migration gate and a “clean isolated database/resource state,” but P01-T02 permits either an explicit flag or an environment-based condition and does not specify how the test builder sets that condition, resets/isolate the persistent database, or proves the migration history on a fresh schema.
* Concern: The plan still leaves two decision-critical implementation choices open: the exact migration-gate configuration and the mechanism that makes the integration test database clean and isolated. Reusing the existing persistent volume can make tests depend on prior runs, and the first-create assertion can pass against an already-created schema without exercising migration application.
* Impact: CRUD/persistence tests may be order-dependent or falsely validate a pre-existing database, while local startup and test startup can diverge. This weakens FR-008/NFR-005 evidence and can conceal migration failures until a clean environment.
* Smallest useful change: Choose one explicit test mechanism and state it in P01-T02/P03-T02: configure the AppHost test graph to use a uniquely isolated disposable database/resource (or an explicit reset/cleanup step despite the data volume), set the migration flag through that test configuration, and assert the migration/schema state before the first create. Keep production migration policy deferred.
* Action owner: planning parent
* Exact resolving evidence: The plan names the exact flag/configuration path, clean-state isolation or reset ordering, and schema/migration-history assertion; implementation evidence shows the integration test passes from a fresh state without relying on prior volume contents.
* Decision route: direct planner correction

## Strengths and Residual Risk

* The revised plan correctly preserves Customers API ownership, keeps Web free of EF/PostgreSQL dependencies, uses exact Aspire resource names, separates DTOs from persistence entities, and avoids speculative infrastructure.
* PC-001 and PC-003 are substantively resolved: UI completion now has an explicit verification path, and unit versus real HTTP coverage has a clear boundary.
* Production migration execution remains an explicitly deferred downstream risk and is not itself a readiness blocker for this bounded local/test implementation.

## Questions or Blocking Evidence Gaps

* No user question is required. The remaining issue is planner-owned: specify the deterministic migration-gate and clean database isolation mechanism for the Aspire integration test graph.

## Limitations

* This standard follow-up critique did not inspect unreferenced source files, modify the plan or application, start Aspire, run tests, or perform open-ended research. Runtime feasibility beyond the supplied source/test seams remains unverified.

## Recommended Next Action

* Highest-impact finding: PC-002
* Action owner: planning parent
* Smallest next action: Revise P01-T02 and P03-T02 to select and configure one migration gate plus a fresh isolated/reset database strategy, and require a migration/schema assertion before CRUD.
* User response required: no

## Next Steps

| [`.copilot-tracking/plans/2026-10-17/feature-customers-plan.md`](.copilot-tracking/plans/2026-10-17/feature-customers-plan.md) | Revised implementation plan assessed |
| [`.copilot-tracking/research/2026-10-17/feature-customers-research.md`](.copilot-tracking/research/2026-10-17/feature-customers-research.md) | Supplied research baseline |
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | Repository constraints and definition of done |
| [`.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique.md`](.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique.md) | Earlier critique reconciled |
| [`.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique-2.md`](.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique-2.md) | This follow-up critique |
