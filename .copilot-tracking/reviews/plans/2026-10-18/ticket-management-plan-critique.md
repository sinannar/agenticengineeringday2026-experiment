<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub Ticket Management

## Metadata

* Task ID: `ticket-management`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/ticket-management-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: initial
* Earlier critique: not applicable

## Inputs and Criterion Boundary

* Task context and caller requirements: Independently assess the complete Ticket Management implementation boundary, using the supplied plan, research, repository instructions, and directly relevant source; do not modify those inputs or any other file.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/ticket-management-research.md`, `.github/copilot-instructions.md`, `src/SupportHub.AppHost/AppHost.cs`, `src/SupportHub.Tickets.Api/Program.cs`, `src/SupportHub.Tickets.Api/SupportHub.Tickets.Api.csproj`, `src/SupportHub.Customers.Api/Program.cs`, `src/SupportHub.Customers.Api/Features/Customers/CustomerEndpoints.cs`, `src/SupportHub.Customers.Api/Features/Customers/CustomerContracts.cs`, `src/SupportHub.Customers.Api/Features/Customers/CustomerDbContext.cs`, `src/SupportHub.Web/Program.cs`, `src/SupportHub.Web/CustomerApiClient.cs`, `src/SupportHub.Web/TicketApiClient.cs`, `tests/SupportHub.AppHost.Tests/CustomerApiTests.cs`, and `tests/SupportHub.AppHost.Tests/TicketsApiTests.cs`.
* Decisions, dependencies, task Goals, and task Requirements considered: Confirmed status/priority/assignee/customer-relationship decisions; P01-P04 and P01-T01 through P04-T03; FR-001 through FR-008; NFR-001 through NFR-005; AppHost, EF migration, service-discovery, Web, and test dependencies.
* Assessment boundary: The supplied plan and evidence were assessed once for implementation blockers, contradictions, missing dependencies or acceptance coverage, unsupported scope, and material risks. No external research or runtime execution was performed, so this critique does not establish behavior that only an implementation or running Aspire graph can prove.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| FR-001–FR-008, NFR-001, NFR-003–NFR-005 | Covered | The plan identifies the service boundary, CRUD/filter contract, explicit Web client, AppHost dependency, and API/Aspire test surfaces. |
| NFR-002, P01-T02, P02-T03 | Partial | The plan defines a migration gate and test setup, but does not explicitly wire the gate for normal Development AppHost execution even though the existing Customers API does so. See PC-001. |
| P01-T01, P01-T02, P03-T01, P03-T02, P04-T01 | Partial | “Bounded” title, description, and assignee limits are required but no exact limits are selected, making validation, EF mapping, contracts, and tests under-specified. See PC-002. |
| P02-T01, P02-T02, P04-T01, P04-T02 | Partial | Customer-not-found behavior is described, but dependency-outage status/error semantics and a proving test are not fixed. See PC-003. |
| FR-008, P04-T02, P04-T03 | Partial | Persistence across requests is covered; restart persistence is qualified as “where the test harness permits” without a concrete acceptance path or explicit limitation. See PC-004. |
| Non-goals and confirmed user direction | Covered | Attachments, activity, SLA, dashboard, identity, deployment policy, pagination, and concurrency remain outside the slice. |

## Verdict

* Verdict: Revise
* Rationale: The architecture and feature boundary are credible and align with the repository, but four planner-owned gaps leave migration behavior, public field constraints, dependency failure behavior, and persistence evidence insufficiently deterministic for implementation and review.

## Findings

<!-- rpi:critique id=PC-001 -->
### PC-001 [High]: Development migration gate is not connected to Tickets API startup

* Related IDs: `NFR-002`, `P01-T02`, `P02-T03`, `P04-T03`
* Evidence: `src/SupportHub.AppHost/AppHost.cs` sets `SUPPORTHUB_APPLY_MIGRATIONS=true` for `customersapi` only inside `builder.Environment.IsDevelopment()`. The plan requires local/test gated migration behavior and says Tickets API should become healthy only after its schema is available, but only the test task explicitly sets the variable.
* Concern: A normal Development Aspire run can start Tickets API without applying the new migration, leaving `ticketsdb` uninitialized while the plan claims local migration readiness. This diverges from the established Customers API pattern and can make runtime verification pass health while ticket persistence is not usable.
* Impact: Local implementation and Aspire verification may exercise a healthy-but-unmigrated Tickets API, producing false confidence and avoidable first-request failures.
* Smallest useful change: Add an explicit P02-T03 detail to mirror the existing Development-only `WithEnvironment("SUPPORTHUB_APPLY_MIGRATIONS", "true")` registration for `ticketsapi`, while retaining the test environment gate and production opt-in behavior; add a Development/AppHost verification criterion.
* Action owner: planning parent
* Exact resolving evidence: `src/SupportHub.AppHost/AppHost.cs` contains the Development-only migration environment assignment for both APIs, and an Aspire run or integration test confirms `ticketsdb` migration is applied before Tickets API health is accepted.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-002 -->
### PC-002 [Medium]: Bounded field lengths are not resolved into an implementable contract

* Related IDs: `FR-001`, `FR-003`, `FR-006`, `P01-T01`, `P01-T02`, `P03-T01`, `P04-T01`
* Evidence: The plan requires bounded validation and says validation and EF mapping must use identical limits, but it specifies no maximum for title, description, or nullable assignee. The research explicitly says exact field-length limits should be fixed in planning and tests.
* Concern: Different implementers can choose different limits, resulting in inconsistent API validation, database schema, JSON/UI behavior, and test expectations. “Bounded assignee display string” is especially not an executable contract.
* Impact: Contract compatibility and migration shape become implementation guesses, and invalid-input coverage cannot prove the stated requirement.
* Smallest useful change: Record concrete maximum constants for title, description, and assignee in P01-T01, require the same values in EF mapping and Web annotations, and name boundary tests in P04-T01.
* Action owner: planning parent
* Exact resolving evidence: The plan contains the selected numeric limits, and implementation tests demonstrate rejection at limit+1 while accepting the limit in API and persistence paths.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-003 -->
### PC-003 [Medium]: Customer dependency-outage semantics are not a testable contract

* Related IDs: `FR-006`, `NFR-003`, `P02-T01`, `P02-T02`, `P04-T01`, `P04-T02`
* Evidence: P02-T01 requires distinguishing a customer `404` from Customers API unavailability and returning an “explicit dependency failure,” but it does not select a status code, Problem Details shape, or client exception mapping. P04 tests enumerate invalid customer references but not an outage result.
* Concern: A transport failure, timeout, or exhausted resilience policy could be mapped inconsistently as validation, not-found, or a generic 500. The Web requirement to preserve dependency distinctions cannot be implemented or verified from the current contract.
* Impact: Operators and users may receive misleading errors, and retries could create ambiguous write outcomes if the caller cannot distinguish dependency failure from invalid input.
* Smallest useful change: Define the dependency-failure response (normally `503 Service Unavailable` with a stable Problem Details title/type) and add a focused test of the mapping; specify the corresponding `TicketApiClient` exception behavior. If the real graph cannot induce the outage deterministically, use a focused handler/client test rather than omitting the contract.
* Action owner: planning parent
* Exact resolving evidence: The plan names the status/problem contract and P04 tests assert it for a simulated Customers API transport failure while `404` remains a client/not-found outcome.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-004 -->
### PC-004 [Medium]: Restart persistence acceptance is non-committal

* Related IDs: `FR-008`, `NFR-001`, `NFR-002`, `P04-T02`, `P04-T03`
* Evidence: P04-T02 asks for “persistence across requests/restarts where the test harness permits,” while the supplied `tests/SupportHub.AppHost.Tests/CustomerApiTests.cs` starts one ephemeral graph per test and current Tickets tests have no migration/test-environment setup. No restart procedure or fallback acceptance is defined.
* Concern: The phrase leaves a material persistence requirement dependent on an unspecified harness capability. A single-process request sequence proves only that the DbContext can round-trip data, not that the migration-backed `ticketsdb` survives resource/application restart.
* Impact: A broken migration, volume configuration, or startup reapplication path could remain undetected while the plan reports persistence coverage.
* Smallest useful change: Choose one explicit proof: either add a two-start Aspire test using the same test PostgreSQL resource/volume semantics and document its isolation, or narrow the criterion to migration application plus cross-request persistence and state restart persistence as unverified downstream evidence.
* Action owner: planning parent
* Exact resolving evidence: P04-T02 specifies the selected restart/test isolation procedure and P04-T03 records the resulting persistence evidence or explicitly records the accepted limitation.
* Decision route: direct planner correction unless the test-environment limitation requires a user decision

## Strengths and Residual Risk

* The plan preserves service ownership, keeps `CustomerId` local to Tickets API, adds only the required `customersapi` dependency, and reuses established Minimal API, EF, typed-client, Blazor, and Aspire test seams.
* The feature boundary is appropriately narrow; later-layer Blob, Service Bus, activity, SLA, identity, deployment, pagination, and concurrency work is explicitly excluded.
* Residual risk remains for stale customer references after customer deletion and production migration policy; both are explicitly deferred and should remain visible as downstream concerns.

## Questions or Blocking Evidence Gaps

* No decision-critical external evidence gap blocks the critique. The four findings are plan corrections; PC-004 may require a user decision only if the harness cannot support the selected restart proof and the limitation is not acceptable.

## Limitations

* This was a standard, supplied-evidence-only critique. No source or plan edits, build, test, Aspire startup, browser verification, or external research was performed.

## Recommended Next Action

* Highest-impact finding: `PC-001`
* Action owner: planning parent
* Smallest next action: Revise P02-T03/AppHost wiring to make Development and test migration behavior explicit, then resolve PC-002 through PC-004 in the plan and acceptance matrix.
* User response required: No for the direct corrections; yes only if the planner cannot provide restart persistence evidence and must accept a divergent test boundary.

## Relevant Artifacts

| Artifact | Description |
|----------|-------------|
| [.copilot-tracking/plans/2026-10-18/ticket-management-plan.md](.copilot-tracking/plans/2026-10-18/ticket-management-plan.md) | Supplied implementation plan |
| [.copilot-tracking/research/2026-10-18/ticket-management-research.md](.copilot-tracking/research/2026-10-18/ticket-management-research.md) | Supplied research and evidence record |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture and validation constraints |

## Next Steps

Revise the plan through `/rpi-plan` (or have the active planning parent apply the four corrections), then rerun the standard critique if the parent requires a fresh verdict.
