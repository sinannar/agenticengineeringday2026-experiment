<!-- markdownlint-disable-file -->
# RPI Plan Critique: ticket-management-restart-persistence

## Metadata

* Task ID: `ticket-management-restart-persistence`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/ticket-management-restart-persistence-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: initial
* Earlier critique: not applicable

## Inputs and Criterion Boundary

* Task context and caller requirements: Independently assess the supplied follow-up plan for RV-001, limited to opt-in PostgreSQL data volume/lifetime, stable credentials, a serialized two-start Aspire test, scoped cleanup, and validation/fallback. Do not modify the plan, source, tests, research, or other review records.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/ticket-management-restart-persistence-research.md`, `.github/copilot-instructions.md`, `src/SupportHub.AppHost/AppHost.cs`, and `tests/SupportHub.AppHost.Tests/TicketsApiTests.cs`.
* Decisions, dependencies, task Goals, and task Requirements considered: Confirmed dedicated serialized persistence-test route; P01-T01 through P01-T02 and P02-T01 through P02-T02; FR-001 through FR-002; NFR-001 through NFR-003; stable credentials, volume identity, lifecycle reuse, serialization, cleanup, and fallback requirements.
* Assessment boundary: The supplied plan and evidence were assessed once for implementation credibility within the stated follow-up boundary. No external research, implementation, build, test, Aspire run, or runtime cleanup was performed; this critique cannot establish the exact pinned-package API or environment behavior.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| `FR-001`–`FR-002`, `NFR-001`–`NFR-002`, `P02-T01` | Covered | The plan requires two independently built AppHost lifecycles, exact resource names, health waits, service-discovered HTTP clients, and persisted field assertions. |
| `NFR-003`, `P01-T01` | Partial | Opt-in mode and a dedicated named volume are required, but the concrete configuration key, volume identity, and stable credential mechanism are deferred to implementation judgment. See `PC-001`. |
| `NFR-003`, `P01-T02` | Partial | Serialization and cleanup boundaries are required, but no exact xUnit isolation construct or supported cleanup/reset operation is selected. See `PC-002`. |
| `NFR-002`–`NFR-003`, `P02-T02` | Partial | Validation and fallback are acknowledged, but the fallback procedure and its exact cleanup/evidence commands are not defined sufficiently to execute or audit if the automated route is unavailable. See `PC-003`. |
| Confirmed user direction and non-goals | Covered | The plan preserves ordinary ephemeral tests and excludes unrelated Ticket Management and later-stack changes. |

## Verdict

* Verdict: Revise
* Rationale: The plan selects the correct narrow architecture and test shape, but stable credentials, serialized isolation/cleanup, and the fallback evidence path remain implementation-dependent at decision-critical points. These gaps could produce a false two-start result or leave retained state contaminating later runs.

## Findings

<!-- rpi:critique id=PC-001 -->
### PC-001 [High]: Stable credentials and persistence-test identity are not an executable contract

* Related IDs: `NFR-003`, `P01-T01`, `P02-T01`
* Evidence: `src/SupportHub.AppHost/AppHost.cs` currently calls `builder.AddPostgres("postgres")` and only gates `WithDataVolume()` on `SUPPORTHUB_TEST_MODE`; the plan says to resolve credentials through a supported mechanism or another non-secret configuration but does not select the mechanism, exact configuration key, or stable volume identity.
* Concern: Two AppHost builders may initialize or address the retained PostgreSQL state with different credentials, or multiple runs may target the same broad identity. The test could fail for authentication rather than persistence, or pass against contaminated state.
* Impact: The central acceptance assertion becomes nondeterministic and the retained resource may collide with unrelated local/CI runs.
* Smallest useful change: Specify the exact opt-in configuration key, dedicated volume name/resource identity, and non-secret stable credential source in P01-T01; require both builders to receive the same values before the first volume initialization and require the test to assert that the second APIs become healthy before reading.
* Action owner: planning parent
* Exact resolving evidence: The plan contains concrete key, volume identity, and credential-source names; implementation/build evidence shows both lifecycle builders use those same values; the focused test proves both `customersapi` and `ticketsapi` become healthy and the second read returns the first ticket.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-002 -->
### PC-002 [High]: Serialized cleanup is required but no supported mechanism is chosen

* Related IDs: `NFR-003`, `P01-T02`, `P02-T01`
* Evidence: P01-T02 says to “select the narrowest supported xUnit serialization mechanism” and to “prefer” reset or exact cleanup, while also allowing implementation to record a command if programmatic cleanup is unavailable. The plan does not identify the collection/assembly boundary, cleanup owner, or exact reset/removal operation.
* Concern: A fixed named volume with persistent lifetime can be accessed concurrently or survive failures without deterministic cleanup. A test that merely disposes both applications does not prove the volume is isolated or reset for the next run.
* Impact: Parallel tests or stale state can make the two-start assertion flaky, mask startup/authentication defects, or leak developer/CI resources. Broad cleanup would violate the supplied boundary.
* Smallest useful change: Commit P01-T02 to a concrete xUnit collection/serialization boundary and a scoped cleanup strategy (including failure-path execution), with the exact supported API/command selected during implementation and a precondition that no unrelated resource is touched.
* Action owner: planning parent
* Exact resolving evidence: The plan names the xUnit construct and dedicated resource cleanup/reset operation; focused test execution demonstrates serialized access and leaves no dedicated state, or records a clearly bounded retained-state limitation with an explicit failure result.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-003 -->
### PC-003 [Medium]: Fallback validation is not reproducible enough to close the evidence gap

* Related IDs: `NFR-001`, `NFR-002`, `P02-T02`, `D2`
* Evidence: P02-T02 requires an “exact limitation” and a “separately managed isolated AppHost procedure” if the test host cannot retain the resource, but it does not define the AppHost invocation, state identity, health/read steps, cleanup command, or artifact that records the result.
* Concern: If the automated path is unavailable, implementers can document a one-off manual check without proving that it used the same volume, credentials, two independent starts, and scoped cleanup. The plan would then preserve the unresolved RV-001 gap while appearing validated.
* Impact: Review closure cannot distinguish a real two-start persistence result from an unrepeatable manual observation or an unsafe environment workaround.
* Smallest useful change: Define the fallback as a bounded, repeatable procedure with exact AppHost start/stop or test commands, dedicated state identity, service-discovered ticket create/read checks, health/log evidence, cleanup/reset, and an explicit implementation-changes record format; require the fallback to report RV-001 as unproven when any step is unavailable.
* Action owner: planning parent
* Exact resolving evidence: P02-T02 names the procedure and evidence artifact, and implementation records two separate starts, the persisted ticket identity/fields, relevant health/log output, and scoped cleanup or an explicit failure/limitation.
* Decision route: direct planner correction

## Strengths and Residual Risk

* The plan correctly preserves the existing `SUPPORTHUB_TEST_MODE=true` ephemeral behavior, uses the authoritative AppHost graph, and avoids direct PostgreSQL access from the integration test.
* The two-start scenario is materially better than cross-request persistence and correctly keeps the fallback subordinate to the selected automated route.
* Residual risk remains until the pinned Aspire package surface and local/CI cleanup behavior are verified; the research explicitly identifies those as implementation gates rather than established facts.

## Questions or Blocking Evidence Gaps

* No decision-critical evidence gap blocks this critique. The three findings are planner-owned corrections. If the supported environment cannot provide stable credentials or scoped cleanup without a divergent persistence-test design, that result must be returned as a user decision rather than silently weakening RV-001.

## Limitations

* This was a standard, supplied-evidence-only critique. No external research, source/test edits, build, focused test, Aspire startup, or cleanup operation was performed.

## Recommended Next Action

* Highest-impact finding: `PC-001`
* Action owner: planning parent
* Smallest next action: Make the persistence-test key, dedicated volume identity, and stable credential source concrete, then specify the xUnit serialization/cleanup mechanism and reproducible fallback evidence path.
* User response required: No for direct planner corrections; yes only if implementation evidence shows the selected credential or cleanup design cannot be supported without changing the confirmed route.

## Relevant Artifacts

| Artifact | Description |
|----------|-------------|
| [.copilot-tracking/plans/2026-10-18/ticket-management-restart-persistence-plan.md](.copilot-tracking/plans/2026-10-18/ticket-management-restart-persistence-plan.md) | Supplied follow-up implementation plan |
| [.copilot-tracking/research/2026-10-18/ticket-management-restart-persistence-research.md](.copilot-tracking/research/2026-10-18/ticket-management-restart-persistence-research.md) | Supplied research and evidence record |
| [src/SupportHub.AppHost/AppHost.cs](src/SupportHub.AppHost/AppHost.cs) | Authoritative PostgreSQL resource and current volume gate |
| [tests/SupportHub.AppHost.Tests/TicketsApiTests.cs](tests/SupportHub.AppHost.Tests/TicketsApiTests.cs) | Current test-mode and AppHost lifecycle setup |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture and validation constraints |

## Next Steps

Revise the plan through `/rpi-plan` (or have the active planning parent apply `PC-001` through `PC-003`), then rerun the standard critique if the parent requires a fresh verdict.
