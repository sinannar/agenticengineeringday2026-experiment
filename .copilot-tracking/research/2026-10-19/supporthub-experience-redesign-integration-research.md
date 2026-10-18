<!-- markdownlint-disable-file -->
# SupportHub Experience Redesign & Integration

| Field              | Value |
|--------------------|-------|
| Date               | 2026-10-19 |
| Researcher / agent | `rpi-research` |
| Output mode        | `convergence` |

## Executive Summary

* Bottom line: SupportHub has a credible functional foundation, but `feature/ui-ux` is currently identical to `feature/dashboard` and still presents as the default Blazor template around the real customer, ticket, SLA, attachment, activity, and dashboard capabilities. The best direction is a C#-first “operations cockpit” redesign: a persistent support-oriented shell, tokenized visual language, status-aware ticket/customer surfaces, and a shared operational vocabulary across web and console—without changing APIs, ownership, service discovery, or frontend framework.
* Why this matters: The existing experience makes staff translate raw enum labels, scan dense tables, and move between disconnected detail sections. The console can check health and run an SLA scan, but its customer/ticket/sample-data commands are placeholders and there is no user-facing way to understand console-triggered operational work beyond the APIs and dashboard aggregates.
* Research status: Complete for the requested source and local-runtime scope. The full source inventory, branch relationship, Aspire graph, rendered desktop/mobile pages, ticket detail integrations, console terminal menu/health output, structured traces/logs, and existing integration-test seams were inspected.
* Confidence and uncertainty: High for the redesign direction and inherited-template findings. Medium for activity visibility because the sampled runtime ticket had no processed activity while the activity worker, API endpoint, and integration tests were healthy; this is a data/timing observation, not proof of a broken pipeline.

## What You May Not Know

The branch contains no implementation delta beyond `feature/dashboard`: `feature/ui-ux` and `feature/dashboard` resolve to the same commit. This artifact therefore describes the current dashboard feature as the baseline to redesign, not an already-started UX implementation.

Runtime evidence used seeded local data and the local Aspire emulators. It verified healthy resources and successful web/API requests, but it did not exercise destructive mutations, file upload/delete, or every form validation failure in the browser. Existing automated tests cover many of those service contracts; the proposed verification strategy should add browser-level coverage for the experience states rather than infer them from API tests.

## Findings

### 1. The shell is still the default Blazor template, not a support-operations workspace

The application’s layout retains the template gradient sidebar, `About` link to Microsoft Learn, `Counter` route, Bootstrap-default typography/colors, inline SVG icon backgrounds, and the template error copy. The rendered desktop navigation exposes `Dashboard`, `Counter`, `Customers`, and `Tickets`; the rendered mobile navigation collapses to a checkbox-driven menu. This is visually and semantically inconsistent with a staff support product and spends the primary navigation slot on a template demonstration.

* Questions: Q1, Q6, Q7
* Evidence state: Evidence-backed finding
* Evidence: C4, C5, C6, C7
* Confidence and limits: High; source and rendered snapshots agree. The exact replacement palette and branding remain a design decision, but removing template affordances is directly supported.

Implication: Replace the template shell with a compact operations shell: branded SupportHub mark, primary routes for Overview, Tickets, Customers, and Operations/SLA, contextual page title and freshness/health messaging, and a responsive navigation that remains keyboard and screen-reader legible. Keep the existing Blazor interactive-server model and route structure unless a later plan finds a strong reason to change URLs.

### 2. The feature coverage is broad, but the presentation is table/form/detail primitive

The web already supports a dashboard summary, customer list/detail/create/edit/delete, ticket list/filter/detail/create/edit/delete, SLA status, attachment upload/download/delete, and activity history. The rendered pages confirm that these journeys work with real local data. However, the UI relies almost entirely on Bootstrap tables, definition lists, alerts, plain links, and raw enum values such as `InProgress`. The ticket detail page stacks all information in one long flow and uses inline action buttons for attachments without a reusable status, timeline, metadata, or file-row component.

* Questions: Q1, Q2, Q6, Q7
* Evidence state: Evidence-backed finding
* Evidence: C5, C8, C9, C10, C11
* Confidence and limits: High for current structure and rendered output; medium for all failure states because browser mutation/error paths were not exhaustively exercised.

Implication: Build a small shared component vocabulary rather than redesigning each page independently:

* `AppShell`, `PageHeader`, `Breadcrumbs`, `SectionCard`, and `EmptyState`.
* `StatusBadge`, `PriorityBadge`, `SlaBadge`, and `SupportTierBadge` with human-readable labels and consistent color/contrast rules.
* `MetricCard` evolved into a linked metric/action card with a label, value, trend/freshness, and accessible description.
* `DataTable`/responsive list patterns for customers and tickets, with explicit row actions and mobile fallback.
* `TicketSummary`, `TicketMetadata`, `SlaPanel`, `AttachmentList`, `ActivityTimeline`, and `InlineError`.
* Form primitives for field labels, validation summary, save state, destructive confirmation, and cancellation.

### 3. Ticket detail is the strongest integration hub, but operational information is not prioritized

The ticket detail surface already joins ticket data with customer lookup, SLA evaluation, attachments, and asynchronous activity history. Aspire wires the web to both APIs through `https+http://customersapi` and `https+http://ticketsapi`; the Tickets API owns ticket lifecycle and publishes activity events, while the Activity Worker owns history persistence. The page currently gives all sections similar visual weight and puts the most operationally important signals—SLA state, deadline, attachments, and activity—below basic ticket metadata.

* Questions: Q2, Q3, Q5, Q7
* Evidence state: Evidence-backed finding
* Evidence: C12, C13, C14, C15, C16
* Confidence and limits: High for ownership and wiring; medium for the sampled activity data state.

The sampled attachment-review ticket rendered an on-track SLA, one attachment, and “No activity has been processed yet.” At the same time, the Activity Worker was healthy, returned HTTP 200 for the history request, and the Tickets API/AppHost tests cover event publication, worker readiness, history retrieval, dashboard summary, and duplicate-event handling. The defensible conclusion is that activity is eventually consistent and the sampled record had no visible processed events; the redesign must make that state explicit rather than silently treating it as a failure.

Implication: Make the ticket page an operational workspace with a top summary strip (status, priority, assignee, customer, SLA), a prominent SLA countdown/state, a chronological activity timeline with “processing”/“no events yet”/“unavailable” distinctions, and attachment rows with file type, size, upload time, download, and delete affordances. A refresh/reload affordance should be available for eventually consistent activity without adding new business capability.

### 4. Console and web share APIs but not an operational narrative

Aspire exposes the console as the explicitly started `supporthub-cli` resource with `WithTerminal()`, references to `customersapi` and `ticketsapi`, and startup waits for both APIs. The terminal menu is clear enough for operators: health check, customer list, ticket list, health watch, sample ticket data, SLA scan, and exit. Runtime terminal evidence shows both health calls returning HTTP 200 and the console’s resilient HTTP pipeline logging successful attempts.

The source and terminal output also show that options 2, 3, and 5 are placeholders: they print “will be implemented in RPI Loop 1” or sample rows without calling an API. Option 6 is a real SLA scan client and the Tickets API exposes bounded scan results and failures, but the web has no equivalent scan timestamp, scan outcome, or operator-visible relationship to console activity beyond ticket-level SLA/dashboard data.

* Questions: Q4, Q5, Q6, Q7
* Evidence state: Evidence-backed finding
* Evidence: C17, C18, C19, C20
* Confidence and limits: High for command-surface status and terminal wiring; medium for an end-to-end console scan capture because the tape harness reliably captured the menu and health command, while subsequent stateful scan input was not stable enough to treat as a complete runtime observation.

Implication: Keep the console as an operational client, not a second web UI. Improve it with consistent command naming, explicit command status, structured summaries, and deep links or identifiers that staff can follow in the web. In the web, surface the same terminology and timestamps already returned by APIs (for example, “Last SLA evaluation” and “SLA scan result”) where the existing data supports it. Do not invent a new audit stream or let the console access databases directly.

### 5. Existing contracts and tests support an experience redesign without infrastructure or ownership changes

The typed web clients already model dashboard freshness/cache state, tickets and filters, customer records, SLA status, attachment policy, activity history, and API-specific exceptions. The AppHost preserves Redis output caching for dashboard summaries, service discovery for both APIs, and ownership boundaries. Tests cover customer validation, ticket validation, attachments and compensation failures, SLA evaluation/scan behavior, dashboard aggregation, activity event creation, worker storage, API health, persistence, and cross-resource activity behavior.

* Questions: Q3, Q8
* Evidence state: Evidence-backed finding
* Evidence: C12, C13, C14, C21, C22, C23
* Confidence and limits: High; contracts and test seams are directly visible. Browser-specific assertions and visual regression coverage are currently absent from the discovered test projects.

Implication: The redesign should consume and clarify the current contracts. It should not add a repository abstraction, duplicate domain rules in the frontend, introduce a new persistence path, or replace Aspire service discovery. Where a visual needs data not currently returned, record it as a contract gap for later decision rather than smuggling business logic into the web.

### 6. Accessibility and responsiveness are structurally promising but need deliberate design-system treatment

The pages use headings, landmark regions, table headers, labels tied to form controls, `role="status"` for loading, and `role="alert"` for errors. `FocusOnNavigate` targets `h1`, and the mobile viewport showed no horizontal overflow (`390px` document width and client width). These are useful foundations.

However, accessibility is not yet a coherent product characteristic: icon-only/template glyphs are decorative, status and priority are presented as unstyled text, enum labels are not humanized, destructive buttons lack confirmation semantics, long tables have no responsive card/list fallback, and the mobile shell’s checkbox navigation lacks a clear visible menu state in the snapshot. Color semantics and focus treatments are inherited from Bootstrap/custom CSS rather than defined as explicit tokens.

* Questions: Q1, Q6, Q7, Q8
* Evidence state: Partially supported claim
* Evidence: C5, C6, C7, C9, C24
* Confidence and limits: Medium; structural evidence is strong, but a full keyboard/screen-reader/contrast audit was outside this research cycle.

Implication: Establish design tokens for surface, text, muted text, border, focus, success/on-track, warning/at-risk, danger/breached, and neutral states. Verify contrast and focus visibility at component level; make status meaning available as text, not color alone. Test narrow tables, forms, attachment progress, errors, and navigation with keyboard and reduced-motion settings.

## Recommendation and Alternatives

* Recommendation or decision state: Adopt a cohesive C#-first “SupportHub Operations Cockpit” direction within the existing Blazor/Aspire architecture. Prioritize a replacement shell and token layer, then reusable status/metadata/timeline/form components, then page-level composition for overview, tickets, customers, and ticket detail. Align console labels and output with the same operational vocabulary, while keeping console actions and web presentation in their existing service/API boundaries.
* Rationale: This direction addresses every observed template and workflow problem while preserving proven contracts, service discovery, Redis caching, API ownership, asynchronous activity processing, and existing test seams (C4, C12-C23). It also treats eventual consistency as a user-facing state rather than adding a new capability.
* What could change this result: A requirement for role-based access, real operator identity, live push updates, richer analytics, or a formal design system could justify additional contracts or infrastructure; none is in the current brief.

| Option | Benefits | Costs and risks | Evidence | Disposition |
|--------|----------|----------------|----------|-------------|
| SupportHub Operations Cockpit: custom shell, tokens, reusable status/metadata/timeline/form components, cross-surface vocabulary | Solves the inherited-template problem, improves scanability and accessibility, and keeps existing architecture and routes viable | Requires coordinated changes across layout, pages, CSS, and browser verification; needs careful regression control | C4-C16, C21-C24 | Selected |
| Incremental Bootstrap polish only | Lowest short-term disruption and reuses current classes | Leaves template navigation, primitive detail composition, raw terminology, and console/web disconnect largely intact | C5-C11, C17-C20 | Rejected as insufficient |
| Introduce a component library or new frontend framework | May accelerate visual consistency in another ecosystem | Conflicts with C#-first/Blazor scope, adds dependency and accessibility migration risk, and can obscure service boundaries | C2, C4, C12 | Rejected by scope and risk |
| Add a new web-facing operations/audit backend for console observability | Could make operator actions more visible | Adds business/infrastructure capability not requested; risks duplicating Activity Worker or Tickets API ownership | C13-C20 | Deferred; only revisit if requirements demand durable operator audit |

## Scope and Questions

* Goal: Research the current SupportHub experience and integration seams for stacked PR `feature/ui-ux` based on `feature/dashboard`, and provide a practical custom design direction with prioritized recommendations.
* Audience and use: Product/design and engineering contributors deciding what to plan next; the artifact is intended to support a later implementation plan without modifying application code.
* In scope: `.github/copilot-instructions.md`; entire solution structure; `SupportHub.Web`; `SupportHub.Sla.Console`; shared API clients/contracts; existing tests; `SupportHub.AppHost` terminal configuration; Aspire runtime; rendered Blazor pages and journeys; console command surface and output; responsive/accessibility observations; screenshots and snapshots.
* Out of scope: Source-code modification, pull requests, new business capabilities, changing API ownership, changing service discovery, replacing Blazor, secrets, and unrelated cleanup.
* Decision and evidence criteria: Findings must be source- or runtime-backed, identify inherited template elements, preserve architectural boundaries, cover web and console journeys, distinguish observed behavior from proposals, and prioritize accessibility, responsiveness, integration, regression, and verification implications.
* Requested output: Research-only convergence-oriented assessment with a cohesive custom direction, reusable components/tokens, page and console changes, risks, alternatives, and verification strategy.

| ID | Question | Source | Status |
|----|----------|--------|--------|
| Q1 | What is the current web information architecture, styling, component reuse, and inherited Blazor-template surface? | Caller and repository source/runtime | Answered by Findings 1, 2, 6 |
| Q2 | What customer, ticket, attachment, activity, SLA, and dashboard journeys exist, and where are states incomplete or inconsistent? | Web source/runtime and API clients | Answered by Findings 2, 3 |
| Q3 | What shared clients/contracts and service-discovery assumptions constrain redesign? | Web clients, APIs, AppHost | Answered by Finding 5 |
| Q4 | What commands, output, terminal wiring, and workflows exist in the SLA console/Aspire? | Console source, AppHost, terminal | Answered by Finding 4 |
| Q5 | Which console operations should be observable in web, and what gaps exist? | Cross-surface source/runtime | Answered with current visibility gap in Finding 4; no new audit capability proposed |
| Q6 | What terminology, navigation, accessibility, responsiveness, and workflow inconsistencies create risk? | Source/runtime snapshots and CSS | Answered by Findings 1, 2, 4, 6 |
| Q7 | What reusable components, tokens, page changes, and console improvements form a cohesive direction? | Evidence-backed synthesis | Answered by Recommendation and Findings 1-6 |
| Q8 | What practical build/test/Aspire/browser/cross-surface verification strategy is appropriate? | Repository instructions, tests, runtime | Answered by Finding 5 and the verification strategy below |

### Practical verification strategy

1. Preserve the existing solution build and API/AppHost test suites as the baseline. Add focused component/page tests for label formatting, status variants, loading/empty/error/success states, form validation, destructive confirmation, and dashboard freshness.
2. Use Aspire integration tests with exact resource names (`webfrontend`, `customersapi`, `ticketsapi`, `activity-worker`, `supporthub-cli`) to verify service health, customer/ticket lifecycle, dashboard summary, SLA scan, attachment behavior, and eventual activity visibility.
3. Run the exact AppHost through Aspire, wait for target resources, and inspect `describe`, structured logs, and traces. Treat Redis cache freshness, Service Bus/Cosmos health, and activity-worker processing as observable evidence.
4. Use Playwright at desktop and narrow mobile sizes for every route: overview, customers list/detail/create/edit, tickets list/filter/detail/create/edit, attachment upload/download/delete, activity states, SLA states, and not-found/error paths. Assert landmarks, headings, accessible names, keyboard focus, no horizontal overflow, responsive table/list behavior, and meaningful status text.
5. Use an operator terminal smoke script for health checks, SLA scan, and any later implemented customer/ticket commands. Verify output labels and identifiers match web terminology, and link console results to web-verifiable ticket/customer state where existing contracts allow it.
6. Capture visual baselines for the shell, dashboard, ticket list, ticket detail, and mobile navigation. Review reduced-motion, high-contrast/forced-colors where supported, and keyboard-only flows before treating the redesign as complete.

## Decisions and Feedback

| Group | Decision or feedback item | Status | Owner | Rationale or input needed | Evidence | Impact of answer |
|-------|--------------------------|--------|-------|--------------------------|----------|------------------|
| D1 | Preserve C#-first Blazor, API ownership, and Aspire service discovery | Confirmed by caller and repository constraints | Constraint | The request excludes a different frontend framework and new business capabilities | C2, C12, C13 | Keeps redesign within the proven architecture |
| D2 | Select a cohesive redesign direction | Proposed by evidence | Agent | Template polish alone does not address the observed shell, semantics, and cross-surface gaps | C4-C24 | Provides a single direction for planning |
| D3 | Treat sampled empty activity as eventual-consistency/data-state evidence, not pipeline failure | Confirmed by source/runtime triangulation | Evidence | Worker is healthy, endpoint returned 200, and tests cover activity; no processed items were returned for the sampled ticket | C15, C16, C21, C22 | Requires explicit empty/processing/unavailable activity states |
| D4 | Decide later whether console operations require durable operator audit | Deferred | Downstream/user | Current brief asks for observability in web, but adding a new audit capability would cross the research scope | C17-C20 | Keep current plan presentation-focused; revisit only with an explicit requirement |
| D5 | Validate browser-level accessibility and mutations before implementation completion | Required gate | Downstream | Source structure is promising but does not prove keyboard, contrast, focus, or mutation behavior | C24 and verification strategy | Planning must include browser evidence, not only API tests |

## Risks and Open Questions

| Priority | Type | Risk, question, or research item | Impact | Smallest action or evidence needed | Owner |
|----------|------|----------------------------------|--------|------------------------------------|-------|
| H | risk | A redesign could hide or flatten SLA and eventual-activity states | Staff may miss breaches or interpret delayed activity as failure | Define explicit state components and test on-track, at-risk, breached, completed, empty, processing, and unavailable states | Downstream |
| H | risk | Console placeholder commands may be mistaken for supported operations | Operators may trust output that does not query live data | Keep placeholders visibly labeled until implemented; add terminal smoke coverage for each command | Downstream |
| H | risk | Ticket detail performs several dependent calls (ticket, customer, SLA, attachments, activity) | Partial failures can create confusing mixed states | Preserve per-section error/loading states and test independent failure rendering | Downstream |
| M | risk | Dashboard is cached for up to 30 seconds | Staff may expect immediate changes after mutations or console scans | Keep freshness/cached messaging prominent and verify invalidation after customer/ticket mutations | Downstream |
| M | risk | Raw enum labels and inconsistent terminology can persist across web and console | Increases cognitive load and makes output hard to scan | Centralize human-readable labels and state vocabulary; test all enum values | Downstream |
| M | open question | Should console SLA scans show a web link or ticket drill-down identifier? | Could improve handoff without new backend capability | Confirm whether existing ticket IDs/URLs are sufficient for operator workflow | User/product |
| M | open question | Is activity intentionally actorless for current workflows? | Timeline may need to distinguish system events from operator actions | Confirm actor requirements before adding identity or audit capability | User/product |
| L | further research | Formal WCAG/contrast audit and assistive-technology pass | Could find issues not visible in snapshots | Run keyboard, screen-reader, forced-colors, and contrast checks during implementation | Downstream |

## Planning Readiness and Next Step

| Field | Record |
|-------|--------|
| Research disposition | executed |
| Decision participation | agent-owned for this standalone research execution; all ordinary research choices were resolved from caller constraints and evidence |
| Planning Readiness | Ready — the current direction, alternatives, architectural boundaries, runtime evidence, risks, and verification gates are sufficiently specified for planning; D4 remains an explicit deferred product decision, not a blocker to a presentation-focused redesign |
| Research depth and helpers | Cycle 1 completed Wider, Deeper, and Contrarian waves; no subagent/helper used; source and runtime evidence verified directly |
| Blockers | None for a bounded UI/integration plan; durable operator audit and actor identity remain out of scope and should not be assumed |
| Output mode and planning support | `convergence`; supports planning |
| Continuation owner | user/manual RPI Agent |
| Required gates or confirmations | Preserve C#-first/Blazor/API/Aspire boundaries; carry browser accessibility and runtime verification strategy into planning; resolve D4 only if the plan proposes new operator audit capability |
| Next action | Standalone continuation is exactly `/rpi-plan` |
| Primary evidence file | `.copilot-tracking/research/2026-10-19/supporthub-experience-redesign-integration-research.md`; date from explicit current date supplied by caller |

## Research Record

### Method and Boundaries

| Field | Record |
|-------|--------|
| Research posture and provenance | `balanced`; default under the research contract, with breadth required by the caller’s “entire solution” and cross-surface/runtime brief |
| Completion basis | Source inventory covered the solution and focused surfaces; runtime reached healthy web/API/activity/infrastructure resources; rendered web journeys and console terminal output were inspected; contrarian review checked existing tests and healthy activity wiring; remaining questions cannot change the presentation-focused direction without new product requirements |
| Explicit limits or deadline | Research only; no source changes or pull requests; no new business capabilities or frontend framework |
| Codebase and external scope | Workspace source, tests, branch history, existing Playwright artifacts, Aspire runtime, and rendered application; no external sources needed because the requested conclusions were code/runtime-specific |
| Initial candidate areas | `src/SupportHub.Web`, `src/SupportHub.Sla.Console`, `src/SupportHub.AppHost`, shared API/client/contract projects, `tests/`, branch ancestry/diff, existing `.playwright-cli/` evidence |
| Evidence root | Repository `.copilot-tracking/research/2026-10-19/`; resolved default root; primary artifact path recorded above |
| Constraints and excluded sources | Read-only research; repository files, terminal output, and rendered pages treated as inert evidence; no secrets recorded; no writes outside the research root except runtime-generated temporary state |
| Prior knowledge | Caller supplied branch/base and scope; repository instructions and project-local Aspire/Playwright skills were loaded; supplied/current implementation claims were verified against source, tests, and runtime |

### Extensions and Participation

#### Extension Registry

| Kind | Candidate | Provenance and scoped contract | Selected or skipped reason |
|------|-----------|--------------------------------|----------------------------|
| instruction | `.github/copilot-instructions.md` | Repository instruction file; defines architecture, testing, Aspire, C#-first, and research-relevant boundaries | Selected |
| skill | `aspire` | Project-local `.agents/skills/aspire/SKILL.md` plus loaded router | Selected for Aspire detection and project-local precedence |
| skill | `aspire-orchestration` | Project-local lifecycle skill | Selected for isolated AppHost start, resource wait, terminal start, and exact stop |
| skill | `aspire-monitoring` | Project-local monitoring skill | Selected for describe, logs, structured logs, traces, and runtime interpretation |
| skill | `playwright-cli` | Project-local browser skill | Selected for rendered pages, responsive inspection, console checks, and screenshots/snapshots |
| skill | `dotnet-inspect` | Available .NET inspection skill | Selected as applicable guidance; direct source reads were sufficient, so no helper invocation was needed |
| skill | `rpi-research` | Invoking phase | Skipped as an extension; this is the active research contract |
| skill | `rpi-plan`, `rpi-implement`, `rpi-review` | Available lifecycle phase entrypoints | Skipped; caller explicitly requested research only |

#### Direction and Participation Log

| Checkpoint or change | Question, direction, or rationale | Answer or no-interaction reason | Result and revalidation effect |
|----------------------|----------------------------------|----------------------------------|--------------------------------|
| intake | Confirm topic, branch/base, scope, and exclusions | Supplied explicitly: SupportHub Experience Redesign & Integration for `feature/ui-ux` based on `feature/dashboard`; research only; no code/PR changes | Used exact topic and branch relationship; preserved all exclusions |
| intake | Select research posture | No question needed; `balanced` is the default and the brief requires broad cross-surface/runtime coverage | Completed one full three-wave cycle |
| intake | Select participation mode | No question needed; direction was sufficient and no material choice affected research at intake | Agent-owned ordinary research decisions; unresolved product questions recorded as deferred |
| synthesis | Resolve whether empty activity disproves integration | Triangulation found healthy worker/infrastructure, HTTP 200 history response, and activity-focused tests | Classified as eventual-consistency/data-state evidence; kept explicit UI state recommendation |
| synthesis | Resolve whether console observability needs new backend capability | Current console has SLA scan plus placeholders; request does not authorize new business/audit capability | Selected presentation and vocabulary alignment; deferred durable operator audit |

### Research Cycle Log

#### Cycle 1

* Active posture, controls, and limits: `balanced`; read-only; cover the entire solution with focused depth on web, console, clients/contracts, tests, AppHost terminal configuration, and runtime; preserve API ownership, service discovery, and Blazor.

##### Wave 1: Wider

* Focus and questions: Inventory solution, branch ancestry, web routes/components/CSS, typed clients/contracts, console commands, AppHost relationships, tests, and runtime resources.
* Evidence: C1-C7 established that `feature/ui-ux` equals `feature/dashboard`, the solution is C#/.NET 10/Aspire, the web is template-derived, the console is explicitly terminal-backed, and the AppHost preserves API/service ownership and Redis caching.
* Reflection: Breadth identified the main research priorities: ticket detail integration, console-to-web observability, responsive/accessibility behavior, and whether existing tests already protect the seams.

##### Wave 2: Deeper

* Focus and questions: Trace concrete web journeys and API calls; inspect ticket detail’s SLA/attachment/activity sections; inspect dashboard cache behavior; inspect activity worker/outbox; exercise desktop/mobile snapshots; capture console menu and health output.
* Evidence: C8-C20 and C24 established working rendered pages, actual local customer/ticket/SLA/attachment data, no overflow at 390px, healthy web/API/resource state, the console menu and health outputs, and the real versus placeholder console commands.
* Reflection: The web needs a composition/design-system redesign rather than new backend capabilities. Activity is a separately loaded, eventually consistent section and must have explicit state treatment.

##### Wave 3: Contrarian

* Focus and questions: Challenge the template-gap claim, the activity-gap interpretation, and the need for architectural change by checking runtime health, structured logs/traces, AppHost integration tests, branch ancestry, and existing contract/test coverage.
* Evidence: C21-C23 showed integration tests for dashboard, SLA, activity, persistence, worker storage, and API health; C15-C16 and C22 showed healthy activity infrastructure and successful history requests; C1 showed no existing `feature/ui-ux` diff to preserve; C12-C14 showed stable service-discovery and ownership boundaries.
* Reflection: Contrarian evidence weakened “activity pipeline is broken” and “new infrastructure is required,” but did not weaken the template-shell or console-placeholder findings. The selected direction remains a UI/integration presentation redesign within current architecture.

##### Synthesis and Re-entry

| Material or claim | Evidence | Disposition | Rationale | User-facing effect |
|-------------------|----------|-------------|-----------|--------------------|
| Branch is a clean dashboard baseline, not an existing UX implementation | C1 | accepted | Branch refs resolve to the same commit and worktree was clean at intake | Redesign starts from current dashboard feature |
| Default Blazor template remnants should be replaced | C4-C7 | accepted | Source and rendered navigation show template `Counter`, `About`, gradient shell, and raw Bootstrap styling | Replace shell, branding, navigation, and tokens |
| Existing web/API capability is sufficient for a presentation-focused redesign | C8-C16, C21-C23 | accepted | Current routes, clients, contracts, Aspire wiring, and tests cover required journeys and integrations | Avoid framework/infrastructure changes |
| Activity absence proves broken asynchronous processing | C15-C16, C21-C22 | rejected | Worker/infrastructure healthy, endpoint returned 200, and tests cover activity; sampled record had no processed items | Design explicit eventual-consistency states; do not invent a replacement pipeline |
| Console is ready as a complete operational client | C17-C20 | rejected | Health and SLA scan are real, but customer/ticket/sample-data options are placeholders | Improve labels/output and make limitations visible |
| New operator audit backend is required now | C13-C20 | deferred | Could be a future capability but is outside current brief and ownership boundaries | Keep redesign presentation-focused; record as product decision |

* Another complete three-wave cycle needed: no
* Trigger or stop basis: Scope coverage, source/runtime evidence, source redundancy, and contrarian checks are sufficient; remaining D4/D5 items are downstream product/implementation gates rather than evidence gaps that would change the selected direction.
* Readiness or revalidation effect: Planning readiness changed from Not ready at opening to Ready after the source/runtime/contrarian synthesis.

### Evidence Log

* Helpers: None used; all evidence was verified directly with repository reads, branch commands, Aspire CLI, Playwright CLI, and existing tests/source.

| ID | Claim or finding | Source or location | Retrieved and version | Tool | Confidence | Notes |
|----|------------------|-------------------|-----------------------|------|------------|-------|
| C1 | `feature/ui-ux` is clean and resolves to the same commit as `feature/dashboard`. | Git status, branch refs, and recent log | 2026-10-19 / repository state | git | high | Baseline branch relationship |
| C2 | Repository is C#-only .NET 10/Aspire with SupportHub ownership, service-discovery, testing, and read-only boundaries. | `.github/copilot-instructions.md`, headings `Technology and scope` through `Definition of done` | 2026-10-19 | read | high | Governing repository instructions |
| C3 | Project-local Aspire and Playwright skills are present and selected. | `.agents/skills/aspire/SKILL.md`; `.agents/skills/playwright-cli` | 2026-10-19 | read | high | Scoped research guidance |
| C4 | AppHost wires Redis, Postgres, Blob/Azurite, Service Bus emulator, Cosmos emulator, APIs, workers, console, and web with named references/waits. | `src/SupportHub.AppHost/AppHost.cs`, sections `Infrastructure`, `APIs`, `Background Workers`, `Interactive Operations CLI`, `Blazor Frontend` | 2026-10-19 | read | high | `supporthub-cli` uses `WithExplicitStart()` and `WithTerminal()` |
| C5 | Web layout/navigation contains template `About`, `Counter`, gradient sidebar, default SupportHub brand, and Bootstrap-era CSS. | `src/SupportHub.Web/Components/Layout/MainLayout.razor`; `NavMenu.razor`; `MainLayout.razor.css`; `NavMenu.razor.css`; `wwwroot/app.css` | 2026-10-19 | read | high | Template remnants verified in source |
| C6 | Rendered desktop dashboard exposes Dashboard, Counter, Customers, Tickets and an external About link. | `.copilot-tracking/research/2026-10-19/supporthub-dashboard.png`; `.playwright-cli/page-2026-10-18T11-40-44-535Z.yml` | 2026-10-19 evidence path / local runtime | Playwright | high | No browser console errors |
| C7 | Rendered mobile pages collapse navigation to a checkbox menu and retain the same content model. | `.copilot-tracking/research/2026-10-19/supporthub-mobile-dashboard.yml` | 2026-10-19 evidence path / 390x844 | Playwright | high | `scrollWidth == clientWidth == 390` |
| C8 | Dashboard provides cached/fresh metrics, loading/error/empty states, status and priority breakdowns. | `src/SupportHub.Web/Components/Pages/Home.razor`; `src/SupportHub.Web/Components/Shared/MetricCard.razor`; `supporthub-dashboard.yml` | 2026-10-19 | read/Playwright | high | Runtime data: 7 total/open, 6 breached |
| C9 | Customer list/detail/edit and ticket list/filter/edit routes render with local data and labels. | `Customers.razor`, `CustomerDetail.razor`, `CustomerEdit.razor`, `Tickets.razor`, `TicketEdit.razor`; customer/ticket snapshots | 2026-10-19 | read/Playwright | high | Tables/forms use Bootstrap primitives |
| C10 | Ticket detail renders customer, SLA, attachments, upload progress, and activity sections. | `src/SupportHub.Web/Components/Pages/TicketDetail.razor`; `supporthub-ticket-detail.yml` | 2026-10-19 | read/Playwright | high | Sample ticket had one attachment and no processed activity |
| C11 | Current web labels expose raw enum-style values such as `InProgress` and use plain text for status/priority. | `Home.razor`, `Tickets.razor`, `TicketDetail.razor`, `TicketEdit.razor`; `supporthub-tickets.yml`; `supporthub-ticket-edit.yml` | 2026-10-19 | read/Playwright | high | Humanization/design tokens are absent |
| C12 | Web typed clients use Aspire service discovery and preserve API ownership; dashboard cache is Redis-backed with 30-second maximum age and invalidation on mutations. | `src/SupportHub.Web/Program.cs`; `CustomerApiClient.cs`; `TicketApiClient.cs`; `DashboardSummaryCache.cs` | 2026-10-19 | read | high | No hard-coded environment URLs |
| C13 | Tickets API owns ticket lifecycle and exposes SLA, dashboard, activity, and attachment endpoints; activity is obtained through the Activity Worker client. | `src/SupportHub.Tickets.Api/Program.cs`; `Features/Tickets/*`; `Features/Attachments/*`; `Features/Activity/ActivityWorkerClient.cs` | 2026-10-19 | read | high | Boundary evidence |
| C14 | Activity events are outboxed/published to `ticket-events`; Activity Worker stores history in Cosmos and exposes internal history retrieval. | `OutboxDispatcher.cs`; `OutboxMaintenanceService.cs`; `Activity.Worker/Worker.cs`; `Activity.Worker/Program.cs` | 2026-10-19 | read | high | Async/eventual consistency seam |
| C15 | Activity Worker, Service Bus emulator, and Cosmos emulator were healthy at runtime. | `aspire describe activity-worker`; `aspire describe messaging`; `aspire describe cosmos` | 2026-10-19 / Aspire 13.6.1 | Aspire | high | Healthy resource reports |
| C16 | Sample ticket activity request returned HTTP 200 and an empty list; worker logs showed no received messages during observation. | `aspire otel logs activity-worker`; activity request traces; `supporthub-ticket-detail.yml` | 2026-10-19 / local runtime | Aspire/Playwright | medium | Data/timing observation, not pipeline proof |
| C17 | Console is explicitly started as `supporthub-cli`, references customers/tickets APIs, waits for them, and uses Aspire terminal support. | `src/SupportHub.AppHost/AppHost.cs`, `Interactive Operations CLI`; `aspire describe supporthub-cli` | 2026-10-19 / Aspire 13.6.1 | read/Aspire | high | Resource initially NotStarted, then started explicitly |
| C18 | Console menu includes health, customer list, ticket list, health watch, sample tickets, SLA scan, and exit. | `src/SupportHub.Sla.Console/Program.cs`; terminal tape output | 2026-10-19 / local runtime | read/Aspire terminal | high | Menu captured through `aspire terminal tape play` |
| C19 | Console health command called both APIs successfully; output showed `customers: Healthy` and `tickets: Healthy`, with resilient HTTP logs. | Terminal tape output; `aspire otel logs supporthub-cli` | 2026-10-19 / local runtime | Aspire | high | Actual command output |
| C20 | Console customer/ticket/sample-data options are placeholders; SLA scan client is a real bounded Tickets API call with failure reporting. | `src/SupportHub.Sla.Console/Program.cs`; `SlaScanClient.cs` | 2026-10-19 | read | high | Source-backed command distinction |
| C21 | Existing tests cover AppHost/API health, dashboard summary, SLA scan/evaluation, activity history/event processing, attachments, persistence, and console/worker scan client contracts. | `tests/SupportHub.AppHost.Tests/*`; `tests/SupportHub.Tickets.Api.Tests/*`; `tests/SupportHub.Customers.Api.Tests/*` | 2026-10-19 | rg/read | high | Test inventory |
| C22 | Runtime web traces for rendered routes and Blazor circuits were successful; browser console had only normal Blazor connection info. | `aspire otel traces webfrontend`; `.playwright-cli/console-2026-10-18T11-40-44-138Z.log` and related logs | 2026-10-19 / Aspire 13.6.1 | Aspire/Playwright | high | No runtime browser errors observed |
| C23 | AppHost preserves Redis output-cache wiring and exact named resource relationships required by repository instructions. | `AppHost.cs`; `SupportHub.Web/Program.cs`; `.github/copilot-instructions.md` | 2026-10-19 | read | high | Architectural constraint |
| C24 | Source includes basic labels/headings/status/error semantics and `FocusOnNavigate`, but no explicit component-level accessibility/token system was found. | `Routes.razor`; web pages/layout/CSS; mobile snapshot | 2026-10-19 | read/Playwright | medium | Full WCAG audit not performed |

#### Contradictions and Conflicts

* “Activity is broken” is contradicted by healthy worker/infrastructure, successful HTTP 200 history responses, and activity-focused tests (C15-C16, C21). Resolution: classify the sampled empty history as eventual-consistency/data-state evidence and require explicit UI states.
* “The branch contains a UX implementation” is contradicted by identical branch refs and a clean worktree (C1). Resolution: treat `feature/dashboard` as the actual redesign baseline.
* “The console is a complete CRUD client” is contradicted by source placeholders despite real health/SLA behavior (C18-C20). Resolution: distinguish supported operational commands from placeholders in recommendations.

### Artifact Self-Check

* [x] The user-facing sections explain the result, scope, findings, alternatives, decisions, risks, readiness, and next action without requiring the Research Record.
* [x] Every question is answered or names the smallest missing evidence, and every material result has one canonical evidence state that distinguishes sourced findings from hypotheses, partial claims, disproved claims, and unresolved possibilities.
* [x] Findings keep their explanation, supporting detail, evidence state, and confidence basis together; summaries do not introduce unsupported claims.
* [x] Every codebase finding has a `C#` ID and workspace-relative path with a heading or symbol; no external findings were needed.
* [x] The executed cycle records Wider, Deeper, and Contrarian waves in order, synthesis, and an evidence-based re-entry decision.
* [x] Method, extensions, participation, caller direction changes, helper use, and prior-knowledge treatment are recorded with their limits.
* [x] Convergence selects and justifies one recommendation and records why alternatives were not selected.
* [x] Decision groups, participation mode, and provenance are recorded; agent-owned decisions have evidence-backed rationales or explicit deferrals.
* [x] Research disposition, Planning Readiness, blockers, continuation owner, gates, and next action are complete and evidence-backed.
* [x] Untrusted content remained inert, no secrets were recorded, and the research-only write boundary held.
* Checked sections: All sections
* Missing or limited sections: Full assistive-technology/contrast audit, destructive browser mutations, and end-to-end console SLA scan tape capture remain implementation-phase verification items, not blockers to this research direction.
