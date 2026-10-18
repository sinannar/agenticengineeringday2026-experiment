<!-- markdownlint-disable-file -->
# Task Research: supporthub-experience-redesign-integration-rv-003

| Field              | Value |
|--------------------|-------|
| Date               | 2026-10-19 |
| Researcher / agent | `rpi-research` |
| Output mode        | `analysis` |

## Executive Summary

* Bottom line: RV-003 cannot be closed as a pure evidence gap. Bounded verification is reproducible, but it found two implementation-level acceptance failures: the customer and ticket tables overflow at `390px`, and induced API outages render the ASP.NET Developer Exception Page rather than the SupportHub error state.
* Why this matters: The branch cannot be called stacked-PR ready until the remaining browser matrix and AppHost regression gate are either completed or explicitly bounded with credible evidence.
* Research status: Completed for the RV-003 evidence question; implementation remediation is required for the two browser findings, while the full solution test gate has now completed successfully.
* Confidence and uncertainty: High confidence in the browser findings and in the conclusion that AppHost integration tests are slow rather than intrinsically deadlocked. Mutation journeys were intentionally not run in this read-only research cycle, so those specific browser states remain unverified.

## What You May Not Know

The prior implementation and review records already contain representative browser/Aspire evidence and an accepted `vhs`/`hex1binterop` terminal dependency failure. This research is specifically about RV-003 and does not reopen those accepted decisions or inspect external sources. The new browser evidence used an isolated AppHost and existing seeded data; no customer, ticket, or attachment was created or deleted.

## Findings

### F1 — ✅ Evidence-backed: AppHost integration tests are reproducible but operationally slow

The earlier unfiltered AppHost run did not prove a deadlock. A single health test passed in 48.0 seconds; the complete `TicketsApiTests` class passed all 10 tests in 540.2 seconds, `CustomerApiTests` passed 3 tests in 166.8 seconds, and `TicketPersistenceTests` passed in 105.1 seconds. During startup, the Service Bus emulator was initially unhealthy and gated `ticketsapi`; after the emulator became healthy, the test graph proceeded. The practical implication is that integration verification needs a generous bounded timeout and sequential execution, not an assumption that silence after a few minutes means failure.

* Questions answered: Q2.
* Evidence state: ✅ Evidence-backed finding.
* Evidence: `C5`, `C6`, `C7`, `C8`, `C9`.
* Confidence basis: Direct command results and Aspire resource state/logs from the same repository revision.

### F2 — ⚠️ Weakened: the 390px no-overflow criterion is not met for collection pages

At a `390px` viewport, `/tickets` reported `document.documentElement.scrollWidth = 532` and `/customers` reported `scrollWidth = 589`. Element inspection identified the rendered data tables as the overflowing elements (`552px` and `609px` wide respectively). Ticket detail stayed within the viewport at `390px`. The responsive `max-width` rules in `app.css:666-700` are nested inside the `forced-colors` media block, so the intended small-screen grid rules are not active under normal browsing; the collection tables also remain fixed-width. This is a concrete failure against P06-T02's no-horizontal-overflow acceptance criterion, not merely missing evidence.

* Questions answered: Q1 and Q3.
* Evidence state: ⚠️ Weakened/disproved claim that representative mobile verification was sufficient.
* Evidence: `C10`, `C11`, `C12`, with source context in `Customers.razor:31-57`, `Tickets.razor:75-115`, and `app.css:666-700`.
* Confidence basis: Direct Playwright viewport and DOM geometry measurements on the isolated Aspire run.

### F3 — ⚠️ Weakened: induced API failures do not reach the intended SupportHub error component

Stopping `customersapi` and loading `/customers` produced HTTP 500 with the ASP.NET Developer Exception Page, including `TaskCanceledException` and `TimeoutRejectedException` details. Stopping `ticketsapi` and loading `/tickets` produced the same class of 500 response. The pages' local `catch` blocks handle `CustomerApiException`/`TicketApiException`, but the service-discovery/resilience timeout surfaced as a different exception during prerendering. `Program.cs:31-34` activates `UseExceptionHandler("/Error")` only outside Development, so the local induced-failure acceptance path is not a SupportHub-owned error state.

* Questions answered: Q1 and Q3.
* Evidence state: ⚠️ Weakened/disproved claim that local request-failure UX was verified as a SupportHub state.
* Evidence: `C13`, `C14`, `C15`, `C16`, with source context in `Customers.razor:76-98`, `Tickets.razor:142-181`, and `Program.cs:30-34`.
* Confidence basis: Direct isolated Aspire resource stop/restore and Playwright snapshots showing HTTP 500 and framework exception details.

### F4 — ✅ Evidence-backed: the remaining static and accessibility checks are substantially covered

Overview, customer/ticket collections, detail pages, create/edit forms, invalid GUID routes, validation, destructive confirmation/cancel, reduced-motion and forced-colors emulation, keyboard focus, clean restored Aspire health, and template-remnant search were exercised. The rendered overview contained no `Counter`, `About`, Microsoft Learn, or default unhandled-error copy, and the restored browser console contained only expected Blazor informational entries with zero errors.

* Questions answered: Q1 and Q3.
* Evidence state: ✅ Evidence-backed finding, bounded by F2/F3.
* Evidence: `C17` through `C25`.
* Confidence basis: Direct Playwright snapshots/console output and Aspire `describe`/logs.

## Recommendation and Alternatives

* Recommendation or decision state: The user accepted the F2/F3 residual risk for this cycle. Treat the AppHost gate as complete and reproducible with a long sequential timeout, retain the findings as explicit follow-up risk, and keep the accepted `vhs`/`hex1binterop` limitation unchanged.
* Rationale: The browser failures are deterministic and tied to changed web behavior, while the integration tests pass when isolated and given enough time. The user explicitly chose documentation/acceptance rather than remediation in this cycle.
* What could change this result: A later remediation and browser rerun could close F2/F3; the accepted decision does not change the observed measurements or make the strict P06-T02 matrix complete.

| Option | Benefits | Costs and risks | Evidence | Disposition |
|---|---|---|---|---|
| Remediate F2/F3, then complete verification | Satisfies the approved browser acceptance boundary | Requires small web/CSS changes and another runtime run | C10-C16 | Deferred |
| Accept and document F2/F3 residual risk | Preserves current code and records the known limitation | Strict responsive/error acceptance remains unmet | User decision plus C10-C16 | Selected |
| Narrow all remaining gaps to environment limits | Avoids implementation follow-up | Incorrect because F2/F3 reproduce locally | C10-C16 | Rejected |

## Scope and Questions

* Goal: Research RV-003 and determine the smallest evidence or environment action needed to resolve the remaining stacked-PR readiness gap.
* Audience and use: The implementation owner and review/PR readiness decision-maker.
* In scope: `P06-T02`, `P06-T04`, AppHost test execution behavior, existing browser evidence, changed web routes and confirmation behavior, local Aspire resource health, and relevant test harness seams.
* Out of scope: New application features, source changes, plan changes, terminal dependency installation, PR submission, and re-review of RV-001/RV-002.
* Decision and evidence criteria: Do not claim unrun validation; distinguish demonstrated defects from missing evidence; preserve the accepted terminal dependency limitation; identify a reproducible next action.
* Requested output: Evidence-based analysis and readiness recommendation.

| ID | Question | Source | Status |
|---|---|---|---|
| Q1 | What exactly remains unverified under `P06-T02` and `P06-T04`? | Approved plan and changes record | Open |
| Q2 | Why does the unfiltered AppHost test project run without completing, and can it be bounded or isolated? | Test project and test execution evidence | Open |
| Q3 | Can the remaining browser checks be completed safely with the current AppHost and Playwright harness? | P06-T02 runbook and existing evidence | Open |
| Q4 | What is the smallest credible readiness disposition for RV-003? | Review outcome and evidence synthesis | Open |

## Decisions and Feedback

| Group | Decision or feedback item | Status | Owner | Rationale or input needed | Evidence | Impact of answer |
|---|---|---|---|---|---|---|
| D1 | Route F2 and F3 to implementation remediation or accept residual risk | resolved | user | User input: "accept-residual-risk"; findings remain documented as known limitations | F2, F3 | No implementation handoff for these findings in this cycle |
| D2 | Use long sequential timeout for AppHost integration gate | resolved | research | Class-level integration selectors and the full solution gate pass; repeated startup is the dominant cost | F1, C26 | Full automated gate is considered complete |

## Risks and Open Questions

| Priority | Type | Risk, question, or research item | Impact | Smallest action or evidence needed | Owner |
|---|---|---|---|---|---|
| High | implementation defect | Customers and tickets collection tables overflow at `390px` | Fails responsive acceptance and affects mobile usability | Replace overflow with a responsive readable presentation or constrain the intended table region without document overflow | implementation owner |
| High | implementation defect | API outage during prerender exposes Developer Exception Page | Fails error-state and information-disclosure expectations | Normalize timeout/request-failure handling into the existing SupportHub error surface and verify in Development | implementation owner |
| Medium | evidence gap | Browser mutation journeys were not run during read-only research | Prevents complete P06-T02 claim | Execute the approved fixture/cleanup matrix after remediation | verification owner |

## Planning Readiness and Next Step

| Field | Record |
|---|---|
| Research disposition | `executed` |
| Decision participation | `user-owned`; standalone research invocation |
| Planning Readiness | `Ready with accepted residual risk for F2/F3`; strict P06-T02 closure remains incomplete because mutation journeys were not executed in research-only mode |
| Research depth and helpers | Focused posture; three waves required; no helper used |
| Blockers | None at intake |
| Output mode and planning support | `analysis`; supports a bounded planning/readiness decision after synthesis |
| Continuation owner | User |
| Required gates or confirmations | Complete wider, deeper, and contrarian waves; preserve research-only write boundary |
| Next action | Carry the accepted F2/F3 risk into the review/changes decision record; no implementation handoff is required by the user's selected disposition |
| Primary evidence file | `.copilot-tracking/research/2026-10-19/supporthub-experience-redesign-integration-rv-003-research.md`; date source is the current task date `2026-10-19` |

## Research Record

### Method and Boundaries

| Field | Record |
|---|---|
| Research posture and provenance | `focused`; bounded internal RV-003 task with named source targets and supplied failure evidence |
| Completion basis | Complete wider, deeper, and contrarian waves; stop when the remaining readiness choice is evidence-backed or the smallest missing evidence is explicit |
| Explicit limits or deadline | Research-only; no source, plan, changes, or review edits |
| Codebase and external scope | Local repository, test harness, Aspire AppHost, Playwright evidence; no external scope |
| Initial candidate areas | P06 runbook, AppHost tests, AppHost resource graph, browser evidence, changed web routes, recent remediation |
| Evidence root | `.copilot-tracking/research/2026-10-19/` |
| Constraints and excluded sources | No dependency installation, no application mutation, no PR operations, no external research |
| Prior knowledge | Original redesign research, implementation changes, and completed review were read and treated as supplied evidence; no prior RV-003 research artifact existed |

### Extensions and Participation

#### Extension Registry

| Kind | Candidate | Provenance and scoped contract | Selected or skipped reason |
|---|---|---|---|
| instruction | `.github/copilot-instructions.md` | Repository architecture, build/test, Aspire, and verification boundaries | Selected; governs repository evidence interpretation |
| skill | `aspire-monitoring` | Local Aspire resource, logs, traces, and health observation | Selected; relevant to AppHost readiness evidence |
| skill | `playwright-cli` | Browser route, viewport, keyboard, and state evidence | Selected; relevant to P06-T02 |
| skill | `aspire-orchestration` | Aspire lifecycle start/stop/wait operations | Skipped for intake; use only if bounded runtime execution requires lifecycle control |
| skill | `rpi-review` | Review lifecycle | Skipped; this is a research continuation for RV-003 |
| skill | `rpi-implement` | Implementation lifecycle | Skipped; research must remain read-only |

#### Direction and Participation Log

| Checkpoint or change | Question, direction, or rationale | Answer or no-interaction reason | Result and revalidation effect |
|---|---|---|---|
| Intake | Whether to widen beyond RV-003 | No interaction requested; the supplied task is bounded and specific | Select `focused` posture and preserve the evidence boundary |
| Intake | Whether to install missing terminal dependencies | Explicitly outside this research scope | Preserve the accepted terminal dependency limitation |

### Research Cycle Log

<!-- <per_cycle> -->
#### Cycle 1

* Active posture, controls, and limits: `focused`; investigate only RV-003, no source or application mutation, and preserve the accepted terminal tooling limitation.

##### Wave 1: Wider

* Focus and questions: Map every remaining acceptance item, available evidence, test classes, AppHost resources, and browser artifacts before choosing a deeper line of investigation.
* Evidence: `C1`-`C4` established the remaining boundary; test discovery found 10 `TicketsApiTests`, 3 `CustomerApiTests`, 1 `TicketPersistenceTests`, and pure presentation/console tests. AppHost topology showed `ticketsapi` waits on the Service Bus emulator and `webfrontend` waits on `ticketsapi`.
* Reflection: The gap was broader than a missing command result: it included a potentially slow distributed test graph and a specific browser matrix with mutation and failure-state requirements.

##### Wave 2: Deeper

* Focus and questions: Isolate the AppHost test execution behavior and assess the reproducibility/safety of the missing browser journeys.
* Evidence: `C5`-`C9` show the integration tests pass when isolated; `C10`-`C25` cover routes, forms, validation, focus/media modes, template remnants, request failures, and restored Aspire health. The 390px measurements and failure snapshots are direct runtime evidence.
* Reflection: The initial “hang” explanation was replaced by a slow-but-progressing startup hypothesis. The browser evidence then exposed two implementation defects, so RV-003 cannot be closed by documentation alone.

##### Wave 3: Contrarian

* Focus and questions: Seek counter-evidence that representative checks are sufficient, that the AppHost run is merely slow, or that browser mutation/failure states can be safely omitted.
* Evidence: `C5`-`C9` contradict an intrinsic AppHost deadlock; `C10`-`C16` contradict the claim that the current representative evidence proves responsive/error acceptance; `C17`-`C25` confirm the accepted static/accessibility checks and show that the failures are bounded rather than universal.
* Reflection: The strongest contrary explanation is environmental Service Bus warm-up, but it explains only the runtime duration, not the deterministic browser failures. Mutation journeys were not executed because this research cycle remained read-only; the approved fixtures and cleanup procedure still need a post-remediation run.

##### Synthesis and Re-entry

| Material or claim | Evidence | Disposition | Rationale | User-facing effect |
|---|---|---|---|---|
| AppHost integration run is deadlocked | C5-C9 | rejected | Isolated classes pass with long startup budgets | Use a long sequential gate, not an environment waiver |
| Full P06 browser acceptance is already met | C10-C16 | rejected | Mobile overflow and framework error page reproduce | Requires implementation remediation |
| Static/template/accessibility evidence is absent | C17-C25 | rejected | Routes, forms, focus/media modes, remnants, and restored health were observed | Preserve existing evidence and focus follow-up on the two defects |
| RV-003 can close now | F2, F3, C26, D1 | accepted with caveat | User accepted F2/F3 residual risk; strict P06-T02 remains incomplete for unrun mutation journeys | Branch may proceed only with explicit residual-risk documentation |

* Another complete three-wave cycle needed: No. The material questions are answered; re-entry is optional if the accepted risks are later remediated or the mutation matrix is explicitly required.
* Trigger or stop basis: Stop after one complete focused cycle; new evidence cannot make the deterministic F2/F3 failures disappear.
* Readiness or revalidation effect: No implementation remediation is required under the selected disposition; strict browser-matrix closure remains a documented residual gap.
<!-- </per_cycle> -->

### Evidence Log

* Helpers: None.

| ID | Claim or finding | Source or location | Retrieved and version | Tool | Confidence | Notes |
|---|---|---|---|---|---|---|
| C1 | RV-003 remains limited to `P06-T02` and `P06-T04` | `.copilot-tracking/reviews/logs/2026-10-19/supporthub-experience-redesign-integration-review.md`, RV-003 | not applicable | read | high | Supplied review finding |
| C2 | P06-T02 names the complete browser route/state matrix and cleanup protocol | `.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md`, P06-T02 | not applicable | read | high | Acceptance boundary |
| C3 | P06-T04 requires the unfiltered solution restore/build/test gate and final checklist | `.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md`, P06-T04 | not applicable | read | high | Acceptance boundary |
| C4 | Representative browser/runtime evidence passed while full verification remained open | `.copilot-tracking/changes/2026-10-19/supporthub-experience-redesign-integration-changes.md`, Validation Record | not applicable | read | high | Existing implementation evidence |
| C5 | One AppHost health integration test passes in 48.0 seconds | `dotnet test ... --filter FullyQualifiedName~TicketsApi_IsHealthy` | 2026-10-19 execution | bash / dotnet test | high | Direct command result |
| C6 | All 10 ticket integration tests pass in 540.2 seconds | `dotnet test ... --filter FullyQualifiedName~TicketsApiTests` | 2026-10-19 execution | bash / dotnet test | high | Direct command result |
| C7 | All 3 customer integration tests pass in 166.8 seconds | `dotnet test ... --filter FullyQualifiedName~CustomerApiTests` | 2026-10-19 execution | bash / dotnet test | high | Direct command result |
| C8 | Persistence integration test passes in 105.1 seconds | `dotnet test ... --filter FullyQualifiedName~TicketPersistenceTests` | 2026-10-19 execution | bash / dotnet test | high | Direct command result |
| C9 | Service Bus emulator initially gated `ticketsapi`, then became healthy | `aspire describe`, `aspire logs messaging`, `aspire wait messaging` | 2026-10-19 execution | Aspire CLI | high | Readiness warm-up evidence |
| C10 | `/tickets` overflows at 390px with document scroll width 532px | Playwright viewport and DOM geometry evaluation | 2026-10-19 execution | Playwright CLI | high | Table width measured at 552px |
| C11 | `/customers` overflows at 390px with document scroll width 589px | Playwright viewport and DOM geometry evaluation | 2026-10-19 execution | Playwright CLI | high | Table width measured at 609px |
| C12 | Ticket detail stays within 390px viewport | Playwright viewport evaluation | 2026-10-19 execution | Playwright CLI | high | `scrollWidth = 390` |
| C13 | Customer API outage renders HTTP 500 Developer Exception Page | Stop `customersapi`, load `/customers`, restore resource | 2026-10-19 execution | Aspire + Playwright CLI | high | Timeout and framework stack exposed |
| C14 | Ticket API outage renders HTTP 500 Developer Exception Page | Stop `ticketsapi`, load `/tickets`, restore resource | 2026-10-19 execution | Aspire + Playwright CLI | high | Timeout and framework stack exposed |
| C15 | Page-local catches target custom API exceptions only | `Customers.razor:76-98`, `Tickets.razor:142-181` | 2026-10-19 source read | view | high | Timeout exception is not normalized there |
| C16 | Development only bypasses `/Error` exception handler | `src/SupportHub.Web/Program.cs:30-34` | 2026-10-19 source read | rg/view | high | Explains framework error surface |
| C17 | Overview renders SupportHub shell and operational metrics | Playwright overview snapshot | 2026-10-19 execution | Playwright CLI | high | Actual rendered route |
| C18 | Customers/tickets/detail/forms render semantic headings and controls | Playwright snapshots for named routes | 2026-10-19 execution | Playwright CLI | high | Representative route matrix |
| C19 | Invalid customer and ticket GUIDs render not-found status state | Playwright snapshots | 2026-10-19 execution | Playwright CLI | high | No backend mutation |
| C20 | Empty customer validation renders summary and field errors | Playwright customer form submission | 2026-10-19 execution | Playwright CLI | high | Accessible invalid fields |
| C21 | Destructive confirmation exposes alertdialog Confirm/Cancel | Playwright customer edit confirmation | 2026-10-19 execution | Playwright CLI | high | Cancel path verified |
| C22 | Reduced-motion and forced-colors emulation are accepted by browser | Playwright media emulation and matchMedia | 2026-10-19 execution | Playwright CLI | medium | CSS behavior not exhaustively audited |
| C23 | Keyboard focus reaches branded overview link | Playwright Tab and active-element evaluation | 2026-10-19 execution | Playwright CLI | high | Focus is visible through rendered markup |
| C24 | Restored Aspire graph is healthy | `aspire describe` after API restoration | 2026-10-19 execution | Aspire CLI | high | APIs, workers, storage, messaging, and frontend healthy |
| C25 | Template-remnant search is clean in rendered overview and browser console has no errors after restoration | Playwright `find` and `console` | 2026-10-19 execution | Playwright CLI | high | Informational Blazor entries only |
| C26 | Full solution test completed with 91 passed and 3 warnings in 791.8 seconds | `dotnet test SupportHub.slnx --no-restore` | 2026-10-19 execution | bash / dotnet test | high | Warnings are existing nullable warnings in `TicketsApiTests.cs` |

#### Contradictions and Conflicts

* None identified at intake; the key question is whether additional evidence can close the documented gap.

### Artifact Self-Check

* [x] User-facing sections are complete after synthesis.
* [x] Every question is answered or names the smallest missing evidence.
* [x] Findings keep evidence state and confidence together.
* [x] Every codebase finding has a stable `C#` ID and workspace-relative location.
* [x] The executed cycle records Wider, Deeper, and Contrarian waves in order.
* [x] Method, extensions, participation, caller direction, helper use, and prior knowledge are recorded.
* [x] Output mode and recommendation state are internally consistent.
* [x] Planning Readiness, blockers, continuation owner, gates, and next action are evidence-backed.
* [x] Research-only write boundary held and no secrets were recorded.
* Checked sections: all sections through synthesis and readiness.
