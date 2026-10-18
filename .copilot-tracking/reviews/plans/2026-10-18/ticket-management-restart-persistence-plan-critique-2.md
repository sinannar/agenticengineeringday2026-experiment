<!-- markdownlint-disable-file -->
# RPI Plan Critique: ticket-management-restart-persistence

## Metadata

* Task ID: `ticket-management-restart-persistence`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/ticket-management-restart-persistence-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: follow-up
* Earlier critique: `.copilot-tracking/reviews/plans/2026-10-18/ticket-management-restart-persistence-plan-critique.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the revised RV-001 follow-up plan for opt-in PostgreSQL data-volume retention, stable credentials, serialized two-start Aspire testing, scoped cleanup, and fallback evidence.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/ticket-management-restart-persistence-research.md`, `.github/copilot-instructions.md`, `src/SupportHub.AppHost/AppHost.cs`, `tests/SupportHub.AppHost.Tests/TicketsApiTests.cs`, and the earlier critique.
* Decisions, dependencies, task Goals, and task Requirements considered: The confirmed dedicated serialized test route; P01-T01 through P01-T02; P02-T01 through P02-T02; FR-001 through FR-002; NFR-001 through NFR-003.
* Assessment boundary: Assess plan credibility only. No implementation, build, test, Aspire run, or runtime cleanup was performed.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| `FR-001`–`FR-002`, `NFR-001`–`NFR-002`, `P02-T01` | Covered | The plan requires two independently built/disposed AppHost lifecycles, health waits for `customersapi` and `ticketsapi`, service-discovered HTTP clients, and comparison of the persisted ticket fields. |
| `NFR-003`, `P01-T01` | Covered | The plan now names `SUPPORTHUB_PERSISTENCE_TEST_MODE`, `SUPPORTHUB_PERSISTENCE_TEST_VOLUME_NAME`, and `Parameters__supporthub-persistence-test-postgres-password`, requires one per-run volume/password identity across both builders, and preserves the existing ephemeral path. |
| `NFR-003`, `P01-T02` | Covered | The plan selects the `SupportHub persistent resources` xUnit collection with `DisableParallelization = true`, disposes both hosts before cleanup, and scopes cleanup to the generated volume through `ProcessStartInfo.ArgumentList`. |
| `NFR-001`–`NFR-003`, `P02-T02` | Covered | The plan defines the focused command/evidence requirements and a bounded fallback with exact AppHost start/stop commands, generated state identity, health/log evidence, cleanup, and an explicit unproven result on failure. |
| Confirmed user direction and non-goals | Covered | The revised plan remains limited to RV-001 and does not change Ticket Management contracts or later stack layers. |

## Earlier Finding Reconciliation

| Finding | Status | Evidence in revised plan |
|---------|--------|--------------------------|
| `PC-001` | Resolved | P01-T01 names the exact mode key, volume-name key and format, parameter key, per-run stable password source, and same-value requirement for both builders. |
| `PC-002` | Resolved | P01-T02 names the xUnit collection, cleanup owner, exact volume-removal operation, failure-path requirement, and prohibition on broad cleanup. |
| `PC-003` | Resolved | P02-T02 defines the fallback start/stop sequence, health waits, service endpoint discovery, ticket create/read evidence, logs, exact generated-volume cleanup, and changes-record disposition. |

## Verdict

* Verdict: Pass
* Rationale: The revised plan is implementation-ready for the confirmed dedicated serialized persistence-test route. The previously decision-critical identity, serialization, cleanup, and fallback gaps are resolved without changing scope or requiring a new user decision.

## Findings

No new actionable `PC-xxx` findings were identified within the supplied standard-critique boundary.

## Strengths and Residual Risk

* The plan keeps ordinary `SUPPORTHUB_TEST_MODE=true` tests ephemeral and makes persistence opt-in.
* The per-run volume and password identity prevent cross-run ambiguity while the xUnit collection prevents concurrent access to the dedicated resource.
* Cleanup is explicit and narrowly scoped; a missing Docker cleanup capability fails clearly instead of silently claiming clean state.
* Residual implementation risk remains in verifying the pinned Aspire overloads and the local container runtime, which is appropriately assigned to implementation validation rather than treated as established by this critique.

## Questions or Blocking Evidence Gaps

* None within the supplied evidence boundary.

## Limitations

* This was a standard follow-up critique. It did not verify the pinned package API, execute the focused test, start Aspire, or perform volume cleanup.

## Recommended Next Action

* Highest-impact finding: None.
* Action owner: implementation stage.
* Smallest next action: Implement P01 and P02, then run the focused persistence test and required build/test checks.
* User response required: No.

## Relevant Artifacts

| Artifact | Description |
|----------|-------------|
| [.copilot-tracking/plans/2026-10-18/ticket-management-restart-persistence-plan.md](.copilot-tracking/plans/2026-10-18/ticket-management-restart-persistence-plan.md) | Revised follow-up implementation plan |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-management-restart-persistence-plan-critique-2.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-management-restart-persistence-plan-critique-2.md) | Follow-up standard critique |
| [.copilot-tracking/reviews/plans/2026-10-18/ticket-management-restart-persistence-plan-critique.md](.copilot-tracking/reviews/plans/2026-10-18/ticket-management-restart-persistence-plan-critique.md) | Earlier critique with reconciled findings |
| [.copilot-tracking/research/2026-10-18/ticket-management-restart-persistence-research.md](.copilot-tracking/research/2026-10-18/ticket-management-restart-persistence-research.md) | Persistence research and selected route |
| [src/SupportHub.AppHost/AppHost.cs](src/SupportHub.AppHost/AppHost.cs) | Authoritative AppHost resource graph |
| [tests/SupportHub.AppHost.Tests/TicketsApiTests.cs](tests/SupportHub.AppHost.Tests/TicketsApiTests.cs) | Existing Aspire test lifecycle patterns |

## Next Steps

The plan is ready for `/rpi-implement` for `ticket-management-restart-persistence`.
