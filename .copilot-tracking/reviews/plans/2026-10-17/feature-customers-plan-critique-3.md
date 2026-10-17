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
* Earlier critique: `.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique-2.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Perform the final standard follow-up credibility assessment, reconcile PC-001, PC-002, and PC-003, and assess whether the revised Customer Management plan is ready for implementation without changing the plan or application code.
* Research and evidence considered: `.copilot-tracking/research/2026-10-17/feature-customers-research.md`, `.github/copilot-instructions.md`, `src/SupportHub.AppHost/AppHost.cs`, `tests/SupportHub.AppHost.Tests/CustomerApiTests.cs`, `tests/SupportHub.Customers.Api.Tests/SupportHub.Customers.Api.Tests.csproj`, and `tests/SupportHub.Customers.Api.Tests/UnitTest1.cs`.
* Decisions, dependencies, task Goals, and task Requirements considered: Confirmed customer scope and support tiers, existing `customersdb`/`customersapi`/`webfrontend` topology, API ownership, P01-P03 sequencing, explicit `SUPPORTHUB_TEST_MODE`/`SUPPORTHUB_APPLY_MIGRATIONS` test gates, omission of `.WithDataVolume()` in test mode, session-scoped database state, serialized or fixture-scoped test setup, and the first-create-after-health schema assertion.
* Assessment boundary: The supplied plan and directly referenced evidence support implementation-readiness assessment of API, persistence, Web, test, and Aspire verification design. This critique does not implement or run the application, validate package APIs, inspect unreferenced files, or perform open-ended research.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| FR-001–FR-006, NFR-001–NFR-004 | Covered | P01 and P02 define service-owned EF persistence, explicit API/Web contracts, validation, status codes, cancellation, and API-only Web access consistent with the repository boundaries. |
| FR-007 | Covered | P02 defines list, detail, create, edit, and delete workflows with explicit states; P03-T03 requires browser/runtime verification or an equivalent documented checklist for the required workflows and states. |
| FR-008, NFR-005 | Covered | P03-T02 now requires the explicit test flags, omission of the normal PostgreSQL data volume, session-scoped/ephemeral database state, serialized or fixture-scoped setup, health ordering, and a first-create-after-health assertion before lifecycle checks. |
| P01-T01–P01-T03 | Covered | Persistence, migration gating, contract separation, validation, error shape, endpoint status codes, and ownership are coherently sequenced. |
| P02-T01–P02-T03 | Covered | Typed service-discovery client and API-backed UI workflows are bounded without adding infrastructure or crossing ownership boundaries. |
| P03-T01 | Covered | The task is limited to pure validation, mapping, and domain behavior; HTTP status, error-contract, and persistence assertions are explicitly assigned to P03-T02. |
| P03-T02–P03-T03 | Covered | The plan specifies the real HTTP lifecycle, required status/error assertions, fresh test database path, migration gate, schema assertion, runtime health/log inspection, and UI verification. |

## Verdict

* Verdict: Pass
* Rationale: The revised plan is credible for implementation. PC-001 and PC-003 remain explicitly resolved, and PC-002 is resolved by a concrete test-only migration and isolation contract: `SUPPORTHUB_TEST_MODE=true`, `SUPPORTHUB_APPLY_MIGRATIONS=true`, no `.WithDataVolume()` in the test path, session-scoped/ephemeral database state, serialized or fixture-scoped setup, and a first create only after healthy startup. Production migration policy remains correctly deferred rather than conflated with local/test acceptance.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| PC-001 | resolved | P03-T03 requires verification of list, detail, create, edit, delete, validation failure, not-found, loading, empty, and error states through the Playwright/runtime seam, with a documented manual fallback when browser automation is unavailable. |
| PC-002 | resolved | P01-T02 and P03-T02 select explicit `SUPPORTHUB_TEST_MODE` and `SUPPORTHUB_APPLY_MIGRATIONS` gates, omit `.WithDataVolume()` for the test graph, require session-scoped/ephemeral database state, serialize or fixture-scope process-level configuration, and assert the first create only after healthy migration-gated startup. |
| PC-003 | resolved | P03-T01 owns pure behavior tests while P03-T02 owns real HTTP status, structured error, routing, and persistence assertions; the responsibilities are no longer mixed. |

## Findings

No actionable findings. The plan's remaining implementation details are appropriately left to the implementer: exact framework/API calls for conditional AppHost configuration, migration authoring, and the selected browser/runtime command. These are bounded by explicit acceptance conditions and do not prevent credible implementation.

## Strengths and Residual Risk

* The plan preserves Customers API data ownership, keeps Web free of database dependencies, uses exact Aspire resource names, separates API contracts from EF entities, and avoids speculative infrastructure.
* The final revision converts the prior migration concern into an explicit, testable contract rather than relying on an existing developer database.
* Serialized or fixture-scoped setup appropriately addresses process-level test configuration, while omission of `.WithDataVolume()` prevents normal persistent local state from masking migration behavior.
* Production migration execution and browser-tool availability remain downstream execution risks, but both are explicitly bounded and do not undermine implementation readiness.

## Questions or Blocking Evidence Gaps

* None. No decision-critical user response is required.

## Limitations

* This standard follow-up critique did not inspect unreferenced source files, modify the plan or application, start Aspire, run tests, or perform open-ended research. Runtime feasibility and exact package API behavior remain implementation-time validation items.

## Recommended Next Action

* Highest-impact finding: none
* Action owner: none
* Smallest next action: Proceed to implementation using the explicit test-mode, migration, isolation, serialization, and schema-assertion contract in P01-T02 and P03-T02.
* User response required: no

## Next Steps

| [`.copilot-tracking/plans/2026-10-17/feature-customers-plan.md`](.copilot-tracking/plans/2026-10-17/feature-customers-plan.md) | Final implementation plan assessed |
| [`.copilot-tracking/research/2026-10-17/feature-customers-research.md`](.copilot-tracking/research/2026-10-17/feature-customers-research.md) | Supplied research baseline |
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | Repository constraints and definition of done |
| [`.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique.md`](.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique.md) | Initial critique reconciled |
| [`.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique-2.md`](.copilot-tracking/reviews/plans/2026-10-17/feature-customers-plan-critique-2.md) | Previous follow-up critique reconciled |

No user action is required; the plan is credible for implementation.
