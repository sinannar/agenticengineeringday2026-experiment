<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub Activity and Audit

## Metadata

* Task ID: `supporthub-activity-and-audit`
* Critique date: 2026-10-18
* Plan: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: explicit user request for standard follow-up critique
* Critique type: follow-up
* Earlier critique: `.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-4.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Assess the latest Activity and Audit implementation plan, reconcile every earlier finding `PC-001` through `PC-013`, and add only new substantive findings. The review specifically covers canonical wire schemas, concrete UI-state evidence ownership, the hard Aspire runtime gate, dependency acyclicity, executable outbox operations, history semantics, repository test constraints, and remaining implementation-time gates.
* Research and evidence considered: `.copilot-tracking/research/2026-10-18/activity-and-audit-research.md`; `.github/copilot-instructions.md`; `notes/prompts/4-feature-activity.md`; `notes/stacks.md`; critiques 1-4.
* Decisions, dependencies, task Goals, and task Requirements considered: The plan's FR/NFR catalog, canonical wire contract section, P01-P06 task graph, P02 outbox dispatcher/maintenance tasks, P04 hosting and history tasks, P05 UI tasks, P06 test/runtime gates, test ownership contract, accountability matrix, and readiness/disposition tables.
* Assessment boundary: The supplied documents support a complete standard credibility assessment of architecture, contracts, sequencing, ownership, testability, and readiness gates. This critique does not execute the application, inspect SDK signatures, start Aspire, or infer runtime success from plan statements.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|------------------------------------------|----------|---------------------|
| Feature prompt and stack boundary | Covered | The plan remains limited to Activity and Audit on `feature/activity`, reuses `messaging`, `ticket-events`, `cosmos`, and `activitydb`, and excludes SLA/dashboard scope. |
| Research C1-C11 and W1-W5 | Covered | Outbox publication, stable identity, Activity Worker ownership, partition-scoped Cosmos storage, peek-lock settlement, duplicate handling, emulator limits, and Aspire testing are carried forward. |
| FR-001 through FR-011 and NFR-001 through NFR-011 | Covered | The local catalog, traceability table, and task evidence make the requirement set auditable. |
| P01 event contracts and atomic outbox | Partial | The contract categories and field names are substantially bound, but placeholder values and an incomplete history-item example still leave some serialized details to implementation. |
| P02 publication, recovery, and retention | Partial | Dispatcher, claim/retry lifecycle, terminal enumeration, replay, cleanup exclusions, and tests are named. Access protection for the internal maintenance surface and exact claim/concurrency thresholds remain implementation-time gates. |
| P03 consumer, Cosmos persistence, and idempotency | Covered | Event-keyed documents, `/ticketId` partitioning, conflict handling, settlement rules, and duplicate tests are explicit. |
| P03/P04 dependency graph | Covered | `P04-T00` depends on the hosting-independent `P03-T03` seam and precedes HTTP-shape tasks; the stated graph is acyclic. |
| P04 history API and semantics | Partial | Authoritative ticket existence, empty-versus-lag, and `400`/`404`/`502`/`503` mappings are specified, but `hasMore` has no continuation mechanism. |
| P05 UI-state evidence ownership | Partial | Web owns the work and Playwright is named, but no concrete browser scenario/artifact path or deterministic way to induce each state is assigned. |
| P06 test-project constraints | Covered | The plan uses only the three existing test projects and explicitly prohibits a fourth project. |
| P06 runtime readiness gate | Partial | Failure blocks normal readiness, but the approved-exception wording still permits handoff without the required distributed evidence unless the exception semantics are tightened. |

## Verdict

* Verdict: Revise
* Rationale: The revision resolves the earlier traceability, hosting sequencing, outbox discovery, authoritative ticket existence, test-project, and broad contract findings. The plan is close to credible implementation readiness, but the canonical schema is not fully executable as written, browser-state evidence is not a durable concrete deliverable, and the runtime exception path weakens the repository-required readiness gate. A new history pagination inconsistency also needs correction.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|-----------------|--------|----------|
| `PC-001` | resolved | FR-001 through FR-011 and NFR-001 through NFR-011 are defined locally, mapped to tasks, and tied to acceptance evidence. |
| `PC-002` | resolved | `P04-T00` is a blocking hosting spike before HTTP-shape work and records the supported binding, health, service URI, AppHost reference, and test path. |
| `PC-003` | resolved | `P01-T01` binds event categories, details, casing intent, null transitions, and transition tests; remaining schema materialization is narrower and addressed below. |
| `PC-004` | resolved | `P06-T01` defines a bounded failure matrix, injectable seams, expected outcomes, and unsupported-emulator recording. |
| `PC-005` | resolved | The accountability matrix assigns phase/task groups, runtime evidence, and topology decisions to accountable owners. |
| `PC-006` | resolved | `P02-T03` names terminal-row enumeration/replay, claim recovery, cleanup exclusions, and deterministic tests. |
| `PC-007` | resolved | `P04-T02`, `P05-T02`, and `P06-T02` distinguish invalid, unknown, valid-empty, populated, downstream-failure, and UI-lag states. |
| `PC-008` | resolved | The hosting spike depends on the hosting-independent persistence/query seam; no P03/P04 cycle remains. |
| `PC-009` | resolved | `P04-T02` assigns an authoritative Tickets API existence check before the Activity Worker call. |
| `PC-010` | resolved | All automated tests remain in the three existing test projects; a fourth project is prohibited. |
| `PC-011` | still open | The new contract section is materially better, but `guid`, `timestamp`, an empty `snapshot`, unspecified `actor` type, and non-exact Problem Details values still leave independent implementers room to diverge. |
| `PC-012` | still open | Playwright is named as the tool, but the plan does not name concrete browser scenarios, evidence artifact locations, or deterministic fixtures/stubs for lag, dependency failure, and not-found states. |
| `PC-013` | still open | `P06-T03` says an approved exception can satisfy the handoff condition; it does not explicitly preserve `Readiness = Not ready` and prohibit implementation handoff when required runtime evidence is absent. |

## Findings

<!-- rpi:critique id=PC-011 -->
### PC-011 [High]: Canonical wire contracts still contain implementation placeholders

* Related IDs: `P01-T01`, `P03-T03`, `P04-T01`, `P04-T02`, `P05-T01`, `P06-T01`, `FR-001`, `FR-008`, `NFR-005`
* Evidence: `.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md` uses `"guid"` and `"timestamp"` placeholders in the created snapshot, shows `details: { "snapshot": {} }` in the history item, does not define the `actor` wire type, and names Problem Details fields/statuses without exact `type`, `title`, and `detail` values.
* Concern: The section is a canonical contract by intent but not a complete schema that independently implementing publisher, consumer, Worker endpoint, proxy, Web client, and tests can consume without making choices.
* Impact: Services can each pass local serialization tests while disagreeing on GUID/timestamp formats, actor/nullability, snapshot fields, or error-body values; cross-service compatibility and UI rendering remain at risk.
* Smallest useful change: Replace placeholders with exact JSON examples or a single versioned schema for every event, history item/response, and error body, including all field types, nullability, casing, actor representation, and exact Problem Details values.
* Action owner: Planning parent with Tickets API and Activity Worker owners
* Exact resolving evidence: A complete canonical schema/example set referenced by all owning tasks, plus contract tests asserting exact serialized fields and error bodies.
* Decision route: Direct planner correction

<!-- rpi:critique id=PC-012 -->
### PC-012 [Medium]: UI-state evidence ownership is named but not executable enough

* Related IDs: `P05-T02`, `P06-T01`, `P06-T02`, `P06-T03`, `FR-009`, `NFR-008`
* Evidence: The plan assigns browser verification to the repository-local Playwright CLI and assigns `P05-T01`-`P05-T02` to the Web owner, but it does not identify browser scenario names/artifact paths, a deterministic fixture or stub for `503`/`502`, or how the lag state is induced and bounded in the browser evidence.
* Concern: A tool and owner do not by themselves prove that loading, empty, success, lag-timeout, retryable-error, and not-found states will all be exercised and retained as reviewable evidence without adding unsupported test infrastructure.
* Impact: Implementation may validate only the happy path or rely on transient manual observation, leaving the explicit UI acceptance criteria unverifiable and making failures difficult to reproduce.
* Smallest useful change: Add a browser-evidence matrix naming one scenario per required state, its deterministic setup/injection path, expected visible result, owner, and durable artifact location in the changes/review record; keep backend contract tests in the existing AppHost test project.
* Action owner: Planning parent with Web and AppHost test owners
* Exact resolving evidence: Reproducible Playwright runs or explicitly recorded browser evidence for every required state, linked from the changes/review record, with no fourth test project.
* Decision route: Direct planner correction

<!-- rpi:critique id=PC-013 -->
### PC-013 [High]: The runtime exception path still weakens the hard readiness blocker

* Related IDs: `P06-T03`, Planning Readiness, `FR-010`, `FR-011`, `NFR-002`
* Evidence: `P06-T03` says not to hand off until the runtime gate passes “or a separately approved exception” names unverified criteria, owner, and next action. Repository instructions require Aspire verification for distributed/runtime changes and define runtime evidence as part of completion.
* Concern: The exception wording permits a plan or implementation handoff while the required distributed flow, resource health, logs, or traces remain unverified. Recording an exception is useful for escalation, but it is not equivalent to passing the gate.
* Impact: The feature can be declared ready on isolated tests despite the central acceptance path being unproven, undermining the explicit hard-runtime requirement and later stacked-PR reliability.
* Smallest useful change: State that any runtime exception leaves readiness blocked and prohibits `/rpi-implement` handoff/stack completion; require a separate user-approved scope decision only to defer the feature, with the missing runtime evidence and next gate recorded.
* Action owner: AppHost test/runtime owner with planning parent
* Exact resolving evidence: Passing Aspire health, end-to-end event-to-history, correlated diagnostics, and representative failure evidence; otherwise `Readiness = Not ready` with no handoff.
* Decision route: Direct planner correction; user response is needed only to explicitly defer or change the repository runtime definition of done.

<!-- rpi:critique id=PC-014 -->
### PC-014 [Medium]: History advertises truncation without a continuation contract

* Related IDs: `P03-T03`, `P04-T02`, `P05-T01`, `P06-T02`, `FR-008`, `NFR-006`
* Evidence: The canonical history response includes `limit` and `hasMore`, and says `hasMore` is true when later items were omitted, but defines no cursor, offset, continuation token, query parameter, or second request shape for retrieving those items.
* Concern: A client receiving `hasMore: true` cannot retrieve the omitted history and the UI cannot represent whether it is showing a complete or truncated audit trail.
* Impact: Long-lived tickets can silently expose only the first bounded slice while the response signals incompleteness without a supported resolution; this is especially material for an audit/history feature.
* Smallest useful change: Either remove `hasMore` and explicitly define the endpoint as a bounded latest/earliest view, or add a deterministic continuation contract and tests for subsequent pages. Bind the chosen behavior in the Web client and UI.
* Action owner: Planning parent with Tickets API/Web owners
* Exact resolving evidence: Canonical request/response examples and proxy/client tests proving complete retrieval semantics at and beyond the configured limit.
* Decision route: Direct planner correction unless product policy requires a new pagination experience.

## Strengths and Residual Risk

* The selected outbox → Service Bus → Activity Worker → Cosmos → Tickets API proxy → Web path remains aligned with ownership and research.
* The plan now has an acyclic phase graph, executable terminal-row enumeration/replay, authoritative ticket existence semantics, a three-project test constraint, and explicit event/history status categories.
* The remaining implementation-time gates are appropriately identified: exact Aspire 13.6.1 APIs, outbox claim/concurrency and numeric retry/retention configuration, Worker hosting shape, Service Bus processor options, and real Aspire health/log/trace evidence. They must remain gates rather than silently becoming assumptions.
* Local emulator limitations are correctly retained; emulator flow must not be presented as cloud durability proof.

## Questions or Blocking Evidence Gaps

* Canonical schema placeholders and the missing continuation decision prevent fully independent contract implementation.
* Browser evidence needs deterministic state setup and a durable artifact path.
* Runtime exceptions must not convert missing distributed evidence into readiness.
* No additional open-ended research is required; these are plan corrections and implementation gates.

## Limitations

* This was a standard, follow-up, read-only critique using only the latest plan, supplied research, critiques 1-4, repository instructions, feature prompt, and stack notes.
* No application source, SDK signatures, running AppHost, logs, traces, or tests were executed.
* The plan and research artifacts were not modified.

## Relevant Artifacts

| Artifact | Description |
|---|---|
| [.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md](.copilot-tracking/plans/2026-10-18/activity-and-audit-plan.md) | Latest implementation plan assessed |
| [.copilot-tracking/research/2026-10-18/activity-and-audit-research.md](.copilot-tracking/research/2026-10-18/activity-and-audit-research.md) | Research and selected architecture |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique.md) | Initial critique |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-2.md) | Second critique |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-3.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-3.md) | Third critique |
| [.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-4.md](.copilot-tracking/critiques/2026-10-18/activity-and-audit-plan-critique-4.md) | Immediate prior critique |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Repository architecture and definition of done |
| [notes/prompts/4-feature-activity.md](notes/prompts/4-feature-activity.md) | Feature prompt and acceptance direction |
| [notes/stacks.md](notes/stacks.md) | Stack position and scope |

## Next Steps

* Highest-impact finding: `PC-013`
* Action owner: Planning parent with AppHost test/runtime owner
* Smallest next action: Make missing Aspire runtime evidence an unconditional readiness/handoff blocker, then complete the canonical schema, UI evidence matrix, and history continuation decision.
* User response required: No for these planner corrections; yes only if the user explicitly approves deferral or a topology/product change.
