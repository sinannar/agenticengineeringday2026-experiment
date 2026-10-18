<!-- markdownlint-disable-file -->

# RPI Plan Critique: Support Dashboard and Operational Polish

## Metadata

* Task ID: `support-dashboard-operational-polish`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/support-dashboard-and-operational-polish-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default
* Critique type: initial
* Earlier critique: not applicable

## Inputs and Criterion Boundary

* Task context and caller requirements: Standard independent critique of the complete dashboard and operational-polish plan; the plan and research must remain unchanged.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/support-dashboard-and-operational-polish-research.md`, `.github/copilot-instructions.md`, and `notes/prompts/6-feature-dashboard.md`.
* Decisions, dependencies, task Goals, and task Requirements considered: All five plan phases, task requirements and dependencies, FR-001–FR-008, NFR-001–NFR-007, planning decisions D1–D5, risks, open questions, and acceptance criteria.
* Assessment boundary: The supplied plan, research, repository instructions, and feature brief only. No code, runtime, browser, Aspire, or external research was consulted; therefore this critique assesses plan credibility and evidence coverage, not implementation correctness.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| FR-001–FR-005, NFR-001–NFR-002 | Covered | P01-T01/P01-T02 define a Tickets-owned additive summary, explicit status semantics, one evaluation timestamp, and reuse of the existing evaluator without exposing persistence entities. |
| FR-006 | Partial | P01-T01/P01-T02 require visible dependency failure, but the plan leaves “problem response versus partial snapshot” unresolved until implementation. |
| FR-007, NFR-003–NFR-004 | Partial | P02-T01/P02-T02 cover UI states, TTL, freshness metadata, and invalidation, but no enforceable numeric freshness bound or single cache/invalidation policy is selected. |
| FR-008 | Covered | P05-T01 requires startup, resource names, freshness, limitations, prerequisites, and verification evidence documentation. |
| NFR-005–NFR-007 | Partial | P03 and P04 cover health, logs, traces, resilience, and privacy constraints; worker readiness and final runtime evidence remain conditional or implicit. |
| Research C3–C10 and selected architecture | Covered | The plan correctly rejects frontend composition of the capped scan, preserves customer ownership, retains the existing `cache` resource, and uses existing Aspire/test seams. |
| P01 dependencies and aggregation risk | Partial | The plan recognizes customer fan-out, but “bulk contract or bounded lookup when scale evidence supports it” leaves the key scalability strategy and operating bound undecided. |
| P04/P05 completion evidence | Missing | No explicit final task requires the solution build, relevant/full tests, Aspire health/log/trace inspection, and recording pass/failure evidence before implementation handoff. |

## Verdict

* Verdict: Revise
* Rationale: The plan has the right architecture, boundaries, phases, and broad acceptance coverage, but implementation-critical policies remain deferred to the implementer. In particular, cache freshness/invalidation, dependency-failure semantics, and whole-population customer lookup bounds need explicit decisions, and the plan needs a concrete final validation gate.

## Findings

<!-- rpi:critique id=PC-001 -->
### PC-001 [High]: Cache freshness and invalidation are not enforceable

* Related IDs: FR-005, FR-006, NFR-003, NFR-004, P02-T02, P04-T01
* Evidence: `.copilot-tracking/plans/2026-10-18/support-dashboard-and-operational-polish-plan.md` — P02-T02 says the exact TTL is an implementation parameter, permits either centralized invalidation or documented external-writer limitations, and leaves the policy choice between output-cache and cache-aside; NFR-003/NFR-004 specify “short” freshness and relevant invalidation without a numeric bound.
* Concern: Multiple materially different implementations can satisfy the prose while producing different stale-result behavior. Web-side invalidation cannot immediately cover `supporthub-cli` or direct API writers, and a TTL described only as “short” cannot be objectively verified.
* Impact: Staff may see stale SLA-critical counts while the implementation still appears to meet the plan, undermining the required authoritative dashboard and cache-safety outcome.
* Smallest useful change: Select one cache policy, define a numeric maximum freshness/TTL and external-writer bound, name the authoritative invalidation boundary, and make P04-T01 test those exact limits and mutation paths.
* Action owner: planning parent
* Exact resolving evidence: A revised P02-T02/NFR-003/NFR-004 states the chosen key/policy, numeric TTL or maximum age, all invalidating mutations, external-writer behavior, and passing expiry/invalidation/fallback tests.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-002 -->
### PC-002 [High]: Whole-population customer-tier evaluation lacks an operating bound

* Related IDs: FR-001, FR-004, FR-006, NFR-005, P01-T02, P04-T01, P04-T02
* Evidence: `.copilot-tracking/plans/2026-10-18/support-dashboard-and-operational-polish-plan.md` — P01-T02 permits a “bounded bulk support-tier contract or bounded distinct-customer lookup strategy when scale evidence supports it,” while the risk table only says to prefer bounded concurrency or a bulk contract and measure duration.
* Concern: The authoritative summary must evaluate the complete ticket population, yet the plan does not choose the lookup contract, maximum concurrency/cardinality, timeout, or behavior when the bound is exceeded. “When scale evidence supports it” defers a decision that is required to implement the endpoint safely.
* Impact: A large dataset can create latency, overload Customers API, or produce inconsistent partial results; the central dashboard endpoint may be neither reliably authoritative nor predictably available.
* Smallest useful change: Choose the bounded lookup design before implementation, specify timeout/concurrency/cardinality limits and failure semantics, and add a representative large-population/performance assertion or an explicit documented scale limit.
* Action owner: planning parent
* Exact resolving evidence: Revised P01-T02 and P04-T01/P04-T02 identify the selected Customers API contract or lookup algorithm, hard bounds, timeout/partial behavior, and tests proving the summary is uncapped without unbounded fan-out.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-003 -->
### PC-003 [Medium]: Dependency-failure and partial-snapshot semantics remain undecided

* Related IDs: FR-006, FR-007, P01-T01, P01-T02, P02-T01, P04-T01
* Evidence: `.copilot-tracking/plans/2026-10-18/support-dashboard-and-operational-polish-plan.md` — P01-T02 allows either a standard problem response or an explicit partial-data contract; P02-T01 requires partial/error states; the risk table says the implementer must select one behavior before implementation.
* Concern: The API contract, typed client, cache behavior, and UI state machine are not guaranteed to agree on whether a dependency failure produces no snapshot, a partial snapshot, or an uncached retryable result.
* Impact: The UI could display incomplete counts as authoritative, cache a partial result accidentally, or make failure/retry behavior inconsistent across API and browser tests.
* Smallest useful change: Add a contract decision matrix covering dependency unavailable, missing customer, partial aggregation, cache backend failure, and invalid/default responses; state which responses are cacheable and which UI state each maps to.
* Action owner: planning parent
* Exact resolving evidence: P01/P02 contract details and P04-T01 assertions unambiguously map each failure category to HTTP/data-quality response, cache treatment, and accessible UI state.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-004 -->
### PC-004 [Medium]: Final build, runtime, and observability validation is not an explicit plan gate

* Related IDs: NFR-005, NFR-006, P03-T01, P03-T02, P04-T02, P04-T03, P05-T01
* Evidence: `.copilot-tracking/plans/2026-10-18/support-dashboard-and-operational-polish-plan.md` — P05-T01 documents commands and evidence, but no task explicitly executes `dotnet restore/build/test`, starts the actual Aspire graph for final verification, inspects resource health/logs/traces, or records the final result before handoff.
* Concern: Documentation of commands is not equivalent to a completion gate. The repository instructions require build, relevant tests, and runtime verification for distributed changes.
* Impact: The implementation can be handed off with unverified compilation, unhealthy resources, missing trace correlation, or failed end-to-end behavior while still satisfying all listed task checkboxes.
* Smallest useful change: Add a final verification task, dependent on P03/P04, with exact restore/build/test commands, Aspire resource waits, health/log/trace checks, browser result or limitation, and a required evidence record.
* Action owner: planning parent
* Exact resolving evidence: A new final task/checklist and P05 evidence table record executed command results, exact resource health, relevant diagnostic evidence, browser status, and any explicitly accepted limitation.
* Decision route: direct planner correction

<!-- rpi:critique id=PC-005 -->
### PC-005 [Medium]: SLA-worker readiness coverage is conditional rather than acceptance-bound

* Related IDs: NFR-005, NFR-006, P03-T01, P04-T02
* Evidence: `.copilot-tracking/plans/2026-10-18/support-dashboard-and-operational-polish-plan.md` — P03-T01 says to represent SLA-worker readiness “where Aspire supports it” and otherwise use a supported pattern or document the limitation; P04-T02 similarly waits for `sla-worker` “where health support permits.”
* Concern: The plan does not establish what health/readiness evidence is required for the worker or what constitutes an accepted limitation. This weakens the operational-polish acceptance boundary and makes resource health assertions environment-dependent.
* Impact: The dashboard may be declared operational while scheduled SLA evaluation is unhealthy or unobservable, and tests may silently skip a required dependency.
* Smallest useful change: Decide whether to add the repository-supported worker health/readiness surface or explicitly accept/document its absence; require a deterministic test/assertion for the selected behavior rather than a conditional wait.
* Action owner: planning parent
* Exact resolving evidence: P03-T01 specifies the worker health contract and P04-T02/P05-T01 record the corresponding health assertion or an explicit accepted limitation with rationale.
* Decision route: direct planner correction

## Strengths and Residual Risk

* The plan correctly keeps ticket/SLA aggregation in Tickets API, customer-tier ownership in Customers API, and presentation/cache concerns in Web.
* It explicitly avoids the known 50-item SLA scan cap, durable dashboard infrastructure, hard-coded endpoints, persistence-entity leakage, sensitive telemetry, and unbounded logs.
* Existing eventual-consistency and idempotency risks are carried into the full-journey test rather than treated as synchronous.
* Residual scale risk remains until the customer lookup bounds and a measured operating limit are made explicit.

## Questions or Blocking Evidence Gaps

* No additional research is required for this standard critique. The decision-critical gaps are plan decisions: numeric cache freshness, lookup bounds, failure contract, and worker readiness acceptance.

## Limitations

* This was a standard, supplied-input-only critique. Runtime/browser/Aspire execution evidence was not available and was not inferred.
* The supplied plan and research contain no earlier critique, so no finding reconciliation was applicable.

## Recommended Next Action

* Highest-impact finding: PC-001
* Action owner: planning parent
* Smallest next action: Revise P02-T02 and the related NFRs/tests to select a cache policy and numeric freshness/invalidation bound, then resolve PC-002–PC-005 in the plan before implementation.
* User response required: no; the supplied brief permits these planner-owned corrections.
