# PR 7 — SupportHub Experience Redesign & Integration

**Branch:** `feature/ui-ux`  
**Parent:** `feature/dashboard`  
**Primary projects:** `SupportHub.Web`, `SupportHub.Sla.Console`  
**Workflow:** HVE Core Research → Plan → Implement → Review

## Objective

Replace the default Blazor template look and feel with a **purpose-built SupportHub interface**, and deliver a coherent, accessible experience across the Blazor web application and the interactive operations console. Make the existing customer, ticket, attachment, activity, SLA, dashboard, and operational workflows feel like one product. Preserve Blazor Interactive Server, existing service ownership, APIs, business rules, and runtime behavior.

This is an experience redesign and integration pass—not a new backend feature or frontend framework migration.

## Scope

### 1. Complete Blazor UI replacement

- Remove or replace the default template's visible branding, navigation, layout, home page, demo/sample pages, and template styling. Remove unused template assets and components when safe.
- Create a distinctive, consistent SupportHub visual identity: design tokens, typography, spacing, colors, iconography, responsive grid, reusable controls, and clear information hierarchy.
- Implement a custom application shell with sidebar or equivalent navigation, header, active-page indication, page titles, breadcrumbs where helpful, and responsive navigation.
- Preserve Blazor Interactive Server and existing API-based access; do not add a separate SPA framework or direct database access.
- Choose UI libraries only when justified by maintainability, accessibility, and the current dependency stack. Avoid unnecessary dependencies.

### 2. Cohesive web journeys

- **Customers:** searchable lists, customer detail/edit forms, support-tier visibility, validation, empty states, and safe deletion confirmation.
- **Tickets:** efficient list and detail views, priority/status/assignment controls, filtering, and clear customer association.
- **Attachments:** understandable upload/download/delete actions, progress and file validation feedback.
- **Activity:** readable, chronological audit timeline with event types and timestamps.
- **SLA:** accessible deadline, at-risk, and breached states with explanatory text, not color alone.
- **Dashboard:** purposeful KPIs, charts or summaries, useful navigation to underlying work, and consistent loading/error states.
- Standardize loading, success, empty, validation, authorization (if already present), error, and confirmation patterns across all pages.

### 3. Operations console experience

- Audit and refine `SupportHub.Sla.Console` commands for health checks, customer/ticket listing, sample-data generation, SLA scans, and any existing watch operations.
- Provide discoverable help, clear command names and usage examples, consistent terminology with the web UI, legible tables, aligned columns, meaningful timestamps, and clear success/warning/error feedback.
- Handle invalid input, unavailable services, cancellation, and long-running operations predictably; avoid noisy output or ambiguous errors.
- Ensure output is usable in Aspire Dashboard's interactive terminal, including narrow widths and non-color terminals. Do not depend on ANSI color alone.
- Preserve existing commands and behavior where possible; document any deliberate compatibility changes.

### 4. Cross-surface consistency and integration

- Ensure web and console use consistent names, statuses, priorities, support tiers, SLA terminology, and date/time conventions.
- Both surfaces must use existing APIs and service discovery rather than directly accessing service-owned databases.
- Validate workflows where a console operation affects data or status later viewed in the web application, including sample-data generation and on-demand SLA scanning where supported.
- Make failures from dependent services understandable in both surfaces without exposing secrets or sensitive diagnostics to end users.
- Treat cross-surface work as presentation and orchestration refinement; avoid adding new business capabilities or changing service boundaries.

### 5. Responsive design, accessibility, and quality

- Validate representative desktop, tablet, and mobile widths; ensure no clipped navigation, unusable tables, or inaccessible dialogs.
- Support keyboard navigation, visible focus, accessible labels, semantic structure, sufficient contrast, and reduced-motion preferences where applicable.
- Use `playwright-cli` and repository Aspire Skills to inspect the real running application and test key journeys.
- Preserve observability, resilience, API contracts, and existing automated tests.

## Explicit Non-Goals

- Replacing Blazor Interactive Server with React, Angular, or another frontend framework.
- Rebuilding or merging the APIs, workers, databases, or event infrastructure.
- Introducing new ticket, customer, attachment, or SLA business rules.
- Adding authentication or authorization features that do not already exist.
- Rewriting the console into a different application type.
- Submitting a stacked PR before explicit approval.

## Acceptance Criteria

- No visible default Blazor template branding, demo navigation, sample pages, or template presentation remains in the product UI.
- A coherent SupportHub design system and custom app shell are used throughout the web application.
- All existing customer, ticket, attachment, activity, SLA, and dashboard workflows still function.
- The operations console has consistent command help, readable output, robust errors, and usable interaction in Aspire Dashboard's terminal.
- Shared concepts and labels agree between console and web; existing console-to-web workflows are demonstrated and verified.
- Key web journeys work at representative desktop, tablet, and mobile viewports.
- Status and SLA indicators are understandable without color; key journeys support keyboard use and visible focus.
- Loading, empty, validation, success, and failure states are implemented consistently.
- Existing builds/tests pass; browser checks and console command checks are recorded with evidence.
- No unapproved business logic, service ownership, or API contract changes occur. Any exceptions are explicitly justified and reviewed.

---

### /rpi-research

```markdown
Research **SupportHub Experience Redesign & Integration** for stacked PR `feature/ui-ux` based on `feature/dashboard`.

Read `.github/copilot-instructions.md` and inspect the entire solution, focusing on `SupportHub.Web`, `SupportHub.Sla.Console`, shared API clients/contracts, existing tests, and Aspire AppHost terminal configuration.

Run the application using repository Aspire Skills. Use `playwright-cli` where available to inspect rendered Blazor pages and the existing navigation, branding, styling, components, responsiveness, accessibility, and all customer, ticket, attachment, activity, SLA, and dashboard journeys. Inspect the console's existing command surface and output in Aspire Dashboard's terminal.

Identify everything inherited from the default Blazor template that should be replaced, and propose a cohesive custom SupportHub design direction. Identify reusable web components, design tokens, page-level changes, console UX improvements, cross-surface terminology and workflow inconsistencies, regression risks, and a practical verification strategy.

Pay special attention to operations performed in the console that should be observable in the web application. Preserve API ownership and service discovery; do not introduce new business capabilities or a different frontend framework.

Record concrete findings, screenshots or other evidence where practical, and prioritized recommendations.

**Research only. Do not modify application code or create pull requests.**
```

### /rpi-plan

```markdown
Create an implementation plan for **SupportHub Experience Redesign & Integration** using the completed research on `feature/ui-ux`.

Plan incremental, testable work covering:
1. Removing/replacing the default Blazor template UI and unused demo assets.
2. Defining a custom SupportHub visual identity, design tokens, accessible shared components, and responsive application shell.
3. Redesigning customer, ticket, attachment, activity, SLA, and dashboard experiences without changing their business behavior.
4. Improving `SupportHub.Sla.Console` command discoverability, help, tabular output, feedback, error handling, and terminal compatibility.
5. Aligning web/console terminology, status presentation, and API-backed operational journeys.
6. Browser, keyboard, viewport, console, Aspire runtime, and automated regression verification.

For each increment, identify affected projects/files, acceptance criteria, dependencies, tests, and risks. Include a concrete checklist proving the default Blazor template presentation is gone and both surfaces remain functional. Distinguish required changes from optional visual enhancements.

Preserve Blazor Interactive Server, API/service boundaries, existing command compatibility where practical, and all existing business rules. Do not add unnecessary UI libraries or backend features.

**Plan only. Do not implement code or create a pull request.**
```

### /rpi-implement

```markdown
Implement the approved **SupportHub Experience Redesign & Integration** plan on `feature/ui-ux`.

Follow `.github/copilot-instructions.md` and the approved plan. Completely replace the visible default Blazor template UI with the custom SupportHub design system and app shell; remove unused template assets safely. Redesign all existing web journeys using accessible, responsive, reusable components.

Refine `SupportHub.Sla.Console` so its existing commands are discoverable, consistent, readable, and robust within the Aspire Dashboard terminal. Align terminology and presentation across web and console, and validate API-backed workflows that connect them.

Keep Blazor Interactive Server, existing API calls/service discovery, service data ownership, business rules, and existing console capabilities intact. Avoid unrelated backend changes and unnecessary dependencies.

Work incrementally. Use repository Aspire Skills to run and diagnose the application, `playwright-cli` to inspect actual rendered UI and verify representative viewports and keyboard journeys, and terminal checks for console commands. Run relevant builds, tests, and end-to-end checks; fix regressions and document any limitations.

**Do not implement new business features or submit a pull request yet.**
```

### /rpi-review

```markdown
Review **SupportHub Experience Redesign & Integration** on `feature/ui-ux` against the approved plan and acceptance criteria.

Verify that the default Blazor template presentation has been fully replaced, that the custom SupportHub design is consistent across all pages, and that all customer, ticket, attachment, activity, SLA, and dashboard workflows remain functional.

Review `SupportHub.Sla.Console` help, command compatibility, formatting, cancellation/error behavior, and Aspire terminal usability. Verify shared terminology and at least one supported console-to-web workflow through existing APIs.

Use `playwright-cli` for browser journeys, representative viewport sizes, keyboard/focus behavior, and visual consistency. Use Aspire Skills for health, logs, traces, service integration, and terminal behavior. Run solution builds and relevant automated tests.

Inspect for unintended API/business changes, service-boundary violations, unnecessary dependencies, inaccessible status indicators, incomplete loading/empty/error states, and unremoved template artifacts.

Report findings by severity, cite concrete evidence, distinguish verified from unverified behavior, and state whether the branch is ready for a stacked PR.

**Do not submit the PR.**
```

## Stacked PR Submission

After implementation and review, use the repository's `gh-stack` skill to prepare `feature/ui-ux` targeting `feature/dashboard`. Inspect the diff, document UI/console changes and validation evidence, and submit only after explicit approval.
