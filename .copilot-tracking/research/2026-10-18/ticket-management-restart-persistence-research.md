<!-- markdownlint-disable-file -->
# Task Research: ticket-management-restart-persistence

| Field              | Value |
|--------------------|-------|
| Date               | 2026-10-18 |
| Researcher / agent | rpi-research |
| Output mode        | convergence |

## Executive Summary

* Bottom line: Aspire supports the required evidence path, but it must be isolated from ordinary tests: enable a named PostgreSQL data volume, use a retained/persistent resource lifetime where the test needs the resource to outlive one AppHost instance, keep credentials stable, and run two sequential AppHost lifecycles against that same state.
* Why this matters: The Ticket Management acceptance criterion requires persistence across restarts, and the review routed this as an evidence gap rather than a demonstrated application defect.
* Research status: Complete for the scoped mechanism question.
* Confidence and uncertainty: High confidence in the supported volume/lifetime model and repository wiring. Exact CI cleanup mechanics and whether the existing package patch exposes every current lifetime API should be verified during implementation.

## What You May Not Know

The existing test fixture sets `SUPPORTHUB_TEST_MODE=true`, and `AppHost.cs` omits `postgres.WithDataVolume()` under that flag. A second test application instance therefore receives a fresh database unless the test harness deliberately enables a persistent test mode. Aspire also warns that persistent data must have stable credentials and explicit cleanup expectations.

## Findings

### Aspire supports a retained PostgreSQL test path without changing service ownership

Aspire’s official testing guidance describes AppHost disposal as cleaning up test resources, while its persistence guidance supports named data volumes and persistent resource lifetimes. The smallest credible follow-up is therefore a dedicated persistence-test mode that preserves only PostgreSQL state, starts one AppHost to create/read a ticket, disposes it, starts a second AppHost against the same named state, and reads the ticket.

* Questions: Q1, Q2
* Evidence state: evidence-backed finding
* Evidence: C1, C2, C3, C4, C5, W1, W2, W3
* Confidence and limits: The mechanism is documented by Aspire and matches the repository’s existing volume gate. Implementation must still verify the exact 13.6 API surface and CI cleanup behavior.

### Stable credentials and cleanup are mandatory for a reliable two-start test

`WithDataVolume()` preserves database files, but a reused database must be reached with the same credentials and must not contaminate ordinary tests. The persistence test should use a stable secret/parameter and a uniquely scoped or explicitly reset volume, run non-concurrently, and remove/reset retained state after the assertion.

* Questions: Q2, Q3
* Evidence state: evidence-backed finding
* Evidence: W2, W3, W4
* Confidence and limits: Stable-password guidance is official; the exact cleanup command or API is environment-dependent and should be selected during implementation without destructive global cleanup.

## Recommendation and Alternatives

* Recommendation or decision state: Add a dedicated, serialized Aspire persistence test mode rather than enabling volumes for all integration tests.
* Rationale: This directly satisfies RV-001 while preserving the current ephemeral default for the rest of the suite. The test should enable a named `ticketsdb` backing volume through the AppHost’s existing PostgreSQL resource, use stable credentials, and execute two independent AppHost lifecycles against that state. `ContainerLifetime.Persistent`/`WithPersistentLifetime()` should be used only if the test requires the database container itself to survive AppHost disposal; a data volume is the essential persistence mechanism.
* What could change this result: A focused implementation compile/runtime check could show that the installed packages cannot safely reuse the persistent resource from `DistributedApplicationTestingBuilder`; then use a separately managed isolated AppHost runtime procedure with the same volume and record the exact cleanup step.

| Option | Benefits | Costs and risks | Evidence | Disposition |
|---|---|---|---|---|
| Dedicated serialized test mode with named volume and stable credentials | Preserves ordinary test isolation while directly testing two starts | Requires scoped cleanup and package/API verification | C1, C2, W1, W2, W3 | Selected |
| Separate isolated AppHost runtime procedure with retained volume | Directly demonstrates real stop/start behavior | More expensive and less convenient for CI automation | W1, W2, W3 | Fallback |
| Treat cross-request persistence plus production volume wiring as sufficient | No new test harness complexity | Does not satisfy RV-001’s two-start evidence condition | C1, C2 | Rejected for this finding |

## Scope and Questions

* Goal: Determine how to obtain credible, deterministic evidence that a ticket survives two separate SupportHub AppHost starts using the same `ticketsdb` state.
* Audience and use: The RPI follow-up owner deciding whether to implement a test/evidence path or record an explicit acceptance decision.
* In scope: Aspire 13.6 local testing/lifecycle behavior, `SupportHub.AppHost` PostgreSQL volume gating, `SupportHub.AppHost.Tests` fixture behavior, and the review resolution condition for RV-001.
* Out of scope: Application code changes, migration changes, deployment design, later Ticket Management stack layers, and implementation of the eventual test.
* Decision and evidence criteria: Identify a supported retained-resource mechanism, its isolation/cleanup semantics, and a deterministic two-start procedure; otherwise identify the smallest missing evidence and state why planning cannot claim full acceptance.
* Requested output: Evidence-backed convergence recommendation for the follow-up implementation or explicit acceptance decision.

| ID | Question | Source | Status |
|----|----------|--------|--------|
| Q1 | What does the current repository test harness retain or discard between AppHost starts? | Repository source and prior review evidence | Answered |
| Q2 | What supported Aspire 13.6 mechanism can retain PostgreSQL state across two starts while keeping tests isolated? | Official Aspire documentation and installed package XML | Answered with implementation gate |
| Q3 | What is the smallest credible validation path if direct test-host reuse is unsupported? | Repository topology and Aspire lifecycle evidence | Answered |

## Decisions and Feedback

| Group | Decision or feedback item | Status | Owner | Rationale or input needed | Evidence | Impact of answer |
|--------|---------------------------|--------|---------------|------------------------------------|-----------|-----------------------------------|
| D1 | Preserve the existing test-mode isolation for ordinary tests | confirmed | repository constraint | Existing tests must not share mutable database state accidentally | C1 | Retained-state validation must be narrowly scoped |
| D2 | Use a dedicated serialized persistence test mode; retain a separate runtime procedure as fallback | confirmed by user | user | The user accepted the evidence-backed automated route while retaining the runtime procedure as fallback | W1, W2, W3, C4, C5 | Planning can define the bounded test mode and cleanup gates |

## Risks and Open Questions

| Priority | Type | Risk, question, or research item | Impact | Smallest action or evidence needed | Owner |
|-----------|------|----------------------------------|--------|------------------------------------|-------|
| H | risk | Persistent state can contaminate later tests or remain after failures | Flaky tests, stale customers/tickets, or local resource leakage | Serialize the test, use a dedicated volume/resource identity, and clean it explicitly after the assertion | downstream |
| M | open question | The exact `ContainerLifetime.Persistent` vs `WithPersistentLifetime()` API available in pinned 13.6 packages | Incorrect API choice could block implementation | Compile a focused test-only change against the installed package surface | downstream |
| M | risk | Stable database credentials may require AppHost parameter/user-secret setup | A second start could fail authentication despite a retained volume | Use a stable secret/parameter before first volume initialization and verify both starts | downstream |

## Planning Readiness and Next Step

| Field | Record |
|-------|--------|
| Research disposition | executed |
| Decision participation | user-owned; standalone/manual RPI research invocation |
| Planning Readiness | Ready; the user confirmed the dedicated serialized persistence-test route |
| Research depth and helpers | Cycle 1 wider, deeper, and contrarian waves completed; no helper used |
| Blockers | None currently |
| Output mode and planning support | convergence; supports planning when the retained-resource path is evidence-backed |
| Continuation owner | user/manual RPI Agent |
| Required gates or confirmations | Research waves complete; D2 confirmed; implementation must verify cleanup and execute the two-start assertion |
| Next action | `/rpi-plan` for the dedicated serialized persistence-test mode, with stable credentials, explicit cleanup, and the separate-runtime fallback |
| Primary evidence file | `.copilot-tracking/research/2026-10-18/ticket-management-restart-persistence-research.md`; date from current invocation |

## Research Record

### Method and Boundaries

| Field | Record |
|-------|--------|
| Research posture and provenance | focused; bounded internal follow-up with named failure evidence and source targets |
| Completion basis | Complete wider, deeper, and contrarian waves; stop when the supported mechanism or smallest evidence gap is clear |
| Explicit limits or deadline | Research-only; no application or test implementation |
| Codebase and external scope | `src/SupportHub.AppHost`, `tests/SupportHub.AppHost.Tests`, related tracking artifacts; official Aspire 13.6 documentation |
| Initial candidate areas | AppHost volume gating, `DistributedApplicationTestingBuilder`, Aspire test resource lifecycle, CLI start/stop/isolation |
| Evidence root | `.copilot-tracking/research/2026-10-18/` |
| Constraints and excluded sources | Read-only outside the research artifact; no secrets; no source or test edits; no destructive Aspire stop operations |
| Prior knowledge | Verified RV-001 review finding, existing AppHost volume condition, and current integration test setup before new research |

### Extensions and Participation

#### Extension Registry

| Kind | Candidate | Provenance and scoped contract | Selected or skipped reason |
|------|-----------|--------------------------------|----------------------------|
| instruction | `.github/copilot-instructions.md` | Repository-local architecture, Aspire, testing, and read-only research boundaries | selected; applies to all repository evidence |
| skill | `aspire` | Project-local Aspire routing and CLI/runtime guidance | selected; resource lifecycle and version-specific behavior are in scope |
| skill | `aspire-orchestration` | Available project skill for lifecycle operations | selected as guidance only; no lifecycle mutation planned |
| skill | `aspire-monitoring` | Available project skill for runtime state/log evidence | selected as guidance only if runtime evidence is needed |
| skill | `rpi-research` | Current lifecycle skill | excluded from candidate activation; governs this execution |
| skill | `rpi-plan`, `rpi-implement`, `rpi-review` | RPI lifecycle entrypoints | skipped; prohibited during research |

#### Direction and Participation Log

| Checkpoint or change | Question, direction, or rationale | Answer or no-interaction reason | Result and revalidation effect |
|----------------------|-----------------------------------|---------------------------------|--------------------------------|
| intake | Research RV-001 only; preserve read-only boundary | Supplied finding and acceptance resolution are sufficient; no user question needed | Focused posture selected; no application changes |
| convergence | Choose the primary RV-001 validation route | User selected the recommended dedicated serialized persistence test | Planning route confirmed; retain the separate runtime procedure as fallback |

### Research Cycle Log

#### Cycle 1

* Active posture, controls, and limits: Focused follow-up on RV-001; no source/test edits or destructive runtime operations.

##### Wave 1: Wider

* Focus and questions: Repository test-host setup, PostgreSQL volume gating, Aspire CLI lifecycle and official testing guidance.
* Evidence: C1–C3 establish the current test-mode volume gate and RV-001 resolution condition. W1 establishes that `DistributedApplicationTestingBuilder` launches a complete AppHost and disposes resources when the test app is disposed. W2 establishes that `WithDataVolume()` persists database files outside the container and that Aspire supports persistent resource lifetimes across AppHost runs.
* Reflection: The repository’s missing evidence is addressable without changing ownership or ordinary test isolation; the investigation must now resolve credential and cleanup details.

##### Wave 2: Deeper

* Focus and questions: Retained resource identity, volume lifecycle, and two independently started hosts.
* Evidence: W2 documents `WithPersistentLifetime()` for resources reused across AppHost runs and `WithDataVolume()` for data surviving container recreation. W3 documents that test AppHosts can receive configuration arguments controlling volume enablement, and that shared AppHost fixtures are supported. C1/C2 show the repository already has a configuration gate suitable for a dedicated mode.
* Reflection: A bounded test mode is preferable to changing `SUPPORTHUB_TEST_MODE`; the test should create one state, dispose the first host, then construct a second host with the same persistence configuration.

##### Wave 3: Contrarian

* Focus and questions: Challenge whether a persistent volume is sufficient evidence, and evaluate a separate runtime procedure if test-host reuse is unsupported.
* Evidence: W2 and W4 challenge a naive “just add a volume” approach: persistent resources outlive normal test cleanup, and PostgreSQL credentials must remain stable across reuse. W3’s test guidance favors session lifetime by default, reinforcing that persistence must be opt-in and explicitly cleaned.
* Reflection: The recommendation is narrowed to a serialized, dedicated persistence test with stable credentials and cleanup. A separate isolated runtime check remains a valid fallback if the test builder cannot safely reuse the retained resource.

##### Synthesis and Re-entry

| Material or claim | Evidence | Disposition | Rationale | User-facing effect |
|-------------------|----------|-------------|-----------|--------------------|
| Current test mode discards PostgreSQL volume state | C1, C2 | accepted | Directly shown by AppHost and test fixture | Existing evidence cannot prove restart persistence |
| Aspire supports retained database state using volumes and persistent lifetimes | W1, W2, W3 | accepted | Official testing, volume, and lifetime guidance agree | A focused automated two-start test is feasible |
| Persistent credentials and cleanup are required | W2, W3, W4 | accepted | Official persistence guidance and counter-evidence identify both risks | Planning must include stable secret/parameter and explicit cleanup gates |

* Another complete three-wave cycle needed: no
* Trigger or stop basis: Wider, deeper, and contrarian waves converge on a dedicated serialized persistence test mode; remaining uncertainty is implementation-level API/cleanup verification, not a research-blocking question.
* Readiness or revalidation effect: planning is ready with explicit implementation gates

### Evidence Log

* Helpers: none.

| ID | Claim or finding | Source or location | Retrieved and version | Tool | Confidence | Notes |
|----|------------------|--------------------|----------------------|------|------------|-------|
| C1 | AppHost enables PostgreSQL data volume only when `SUPPORTHUB_TEST_MODE` is false | `src/SupportHub.AppHost/AppHost.cs` — PostgreSQL resource declaration | not applicable | read | high | Direct source evidence |
| C2 | Ticket integration tests set `SUPPORTHUB_TEST_MODE=true` before building the test AppHost | `tests/SupportHub.AppHost.Tests/TicketsApiTests.cs` — `ConfigureTestEnvironment` | not applicable | read | high | Direct source evidence |
| C3 | Review resolution requires a deterministic two-start test against retained PostgreSQL state or equivalent evidence | `.copilot-tracking/reviews/logs/2026-10-18/ticket-management-review.md` — RV-001 | not applicable | read | high | Prior review contract |
| C4 | Pinned Aspire.Hosting.PostgreSQL 13.6.1 exposes `WithDataVolume` with an optional explicit volume name | `$HOME/.nuget/packages/aspire.hosting.postgresql/13.6.1/lib/net8.0/Aspire.Hosting.PostgreSQL.xml` — `PostgresBuilderExtensions.WithDataVolume` | not applicable | rg/read | high | Installed package XML |
| C5 | Pinned Aspire.Hosting 13.6.1 exposes `ContainerLifetime.Persistent`, `WithLifetime`, and `WithPersistentLifetime`; Aspire.Hosting.Testing 13.6.0 exposes `DistributedApplicationTestingBuilder.CreateAsync` and `BuildAsync` | `$HOME/.nuget/packages/aspire.hosting/13.6.1/lib/net8.0/Aspire.Hosting.xml`; `$HOME/.nuget/packages/aspire.hosting.testing/13.6.0/lib/net8.0/Aspire.Hosting.Testing.xml` — lifetime and testing builder members | not applicable | rg/read | high | Installed package XML; compile-time surface still needs a focused implementation check |
| W1 | Aspire integration tests start a complete AppHost and dispose test resources when the test application is disposed | `Testing overview` — https://aspire.dev/testing/overview/ | 2026-10-18; Aspire testing guidance | web_fetch | high | Official testing lifecycle guidance |
| W2 | Aspire supports `WithDataVolume()` for database data persistence and persistent resource lifetimes for reuse across AppHost runs | `Configure resource lifetimes in Aspire` — https://aspire.dev/app-host/resource-lifetimes/; `Persist Aspire data with volumes and binds` — https://aspire.dev/fundamentals/persist-data-volumes/ | 2026-10-18; Aspire 13.6-era documentation | web_fetch | high | Official persistence and lifetime guidance |
| W3 | Aspire test builders accept AppHost configuration arguments and support shared AppHost fixtures; default session lifetime is the normal test isolation model | `Manage the AppHost in tests` — https://aspire.dev/testing/manage-app-host/?testing-framework=xunit | 2026-10-18; Aspire testing guidance | web_fetch | high | Official xUnit/test-host management guidance |
| W4 | Persistent volume reuse requires stable credentials and explicit cleanup/reset planning | `Persist Aspire data with volumes and binds` — https://aspire.dev/fundamentals/persist-data-volumes/; PostgreSQL persistence issue search evidence — https://github.com/microsoft/aspire/issues/11337 | 2026-10-18; Aspire documentation and issue tracker | web_search/web_fetch | medium | Issue tracker page was not directly readable; use as risk corroboration, not sole proof |

#### Contradictions and Conflicts

* W2 recommends persistence, while W3 recommends session lifetime for normal tests. Resolved by using persistence only in a dedicated test mode and retaining session lifetime elsewhere.

### Artifact Self-Check

* [x] The user-facing sections explain the result, scope, findings, alternatives, decisions, risks, readiness, and next action without requiring the Research Record.
* [x] Every question is answered or names the smallest missing evidence, and every material result has one canonical evidence state.
* [x] Findings keep their explanation, supporting detail, evidence state, and confidence basis together.
* [x] Every codebase finding has a `C#` ID and workspace-relative path with a heading or symbol.
* [x] Every executed cycle records Wider, Deeper, and Contrarian waves in order, synthesis, and an evidence-based re-entry decision.
* [x] Method, extensions, participation, caller direction changes, helper use, and prior-knowledge treatment are recorded with their limits.
* [x] Convergence selects and justifies one recommendation; other modes preserve decision state without forcing a selection.
* [x] Decision groups, participation mode, and provenance are recorded.
* [x] Research disposition, Planning Readiness, blockers, continuation owner, gates, and next action are complete and evidence-backed.
* [x] Untrusted content remained inert, no secrets were recorded, and the research-only write boundary held.
* Checked sections: all user-facing sections, Scope and Questions, Decisions and Feedback, Risks and Open Questions, Planning Readiness, extensions, Cycle 1 waves/synthesis, Evidence Log, contradictions, and self-check.
* Missing or limited sections: exact compile-time API verification and environment-specific cleanup behavior are implementation gates, not unresolved research questions.
