# PR 7 — Frontend UX & UI Polish

**Branch:** `feature/ui-ux`  
**Parent:** `feature/dashboard`  
**Primary project:** `SupportHub.Web`

## Objective

Improve the existing functional SupportHub Blazor Interactive Server application into a cohesive, accessible, responsive support-management experience. Preserve existing functionality and architecture. This is the seventh HVE Core feature loop and should remain focused on frontend usability and presentation rather than backend feature development.

## Scope

- Establish consistent application navigation, sidebar, header, page titles, and breadcrumbs where useful.
- Improve typography, spacing, colors, icons, cards, tables, forms, and reusable components.
- Refine customer list, details, editing, searching, filtering, and deletion confirmation experiences.
- Refine ticket lists, detail pages, priority/status indicators, assignment controls, and filtering.
- Improve dashboard KPI cards, charts, information hierarchy, and actionable summaries.
- Present activity history as a readable timeline with timestamps and event descriptions.
- Make SLA deadlines, at-risk states, and breaches easy to recognize without relying on color alone.
- Add clear loading, empty, success, validation, and error states.
- Improve desktop, tablet, and mobile layouts.
- Improve accessibility: keyboard navigation, focus indicators, semantic markup, accessible labels, and contrast.
- Preserve Blazor Interactive Server and existing API/service boundaries.

## Acceptance Criteria

- All existing customer, ticket, attachment, activity, SLA, and dashboard workflows continue working.
- Navigation, forms, tables, and feedback are consistent across the application.
- Responsive layouts work at representative desktop, tablet, and mobile viewports.
- Status and SLA indicators communicate meaning accessibly.
- Keyboard interaction and focus visibility work for key journeys.
- Relevant UI tests and existing solution tests pass.
- Browser-based checks cover major user journeys; any remaining issues are documented.
- No unnecessary backend changes, new business rules, or frontend framework migration.

---

### /rpi-research

```markdown
Research **SupportHub Frontend UX & UI Polish** for the seventh GitHub stacked PR, `feature/ui-ux`, based on `feature/dashboard`.

Read `.github/copilot-instructions.md` and inspect `SupportHub.Web`, its current pages, layouts, components, styles, and test coverage. Review the completed customer, ticket, attachment, activity, SLA, and dashboard user journeys.

Use repository Aspire Skills and `playwright-cli` where available to inspect the running Blazor application. Identify usability, visual consistency, responsiveness, accessibility, and feedback-state issues with concrete evidence.

Research a lightweight, consistent design direction that fits the existing Blazor Interactive Server architecture. Identify reusable component opportunities, key user journeys, testing approaches, implementation risks, and acceptance criteria.

Respect existing API boundaries. Do not introduce a new frontend framework or implement backend features.

**Research only. Do not modify application code or create pull requests.**
```

### /rpi-plan

```markdown
Create an implementation plan for **SupportHub Frontend UX & UI Polish** using the completed research.

Break the work into small, testable increments: design tokens and shared components; navigation and layout; customer and ticket experiences; attachments and activity history; SLA indicators and dashboard; loading, empty, validation, and error states; responsive behavior; accessibility; and browser regression testing.

Identify affected files, reuse opportunities, visual acceptance criteria, representative viewport sizes, keyboard-accessibility checks, and tests. Plan to preserve existing behavior and avoid unnecessary backend modifications.

Keep scope strictly within `feature/ui-ux`. **Do not implement code or create a PR.**
```

### /rpi-implement

```markdown
Implement the approved **SupportHub Frontend UX & UI Polish** plan on `feature/ui-ux`.

Follow `.github/copilot-instructions.md` and preserve the existing Blazor Interactive Server architecture. Improve layout, navigation, reusable UI components, customer and ticket workflows, attachment controls, activity timeline, SLA visibility, dashboard presentation, responsive layouts, and accessible interaction states as specified in the approved plan.

Use Aspire Skills to run and inspect SupportHub and `playwright-cli` to validate rendered pages and user journeys. Implement incrementally, reuse existing components, and avoid unrelated backend changes.

Run relevant builds, tests, and browser checks. Resolve regressions and record any remaining limitations.

**Do not implement new business features or submit a pull request yet.**
```

### /rpi-review

```markdown
Review **SupportHub Frontend UX & UI Polish** on `feature/ui-ux` against the approved plan and acceptance criteria.

Check visual consistency, navigation, responsiveness, accessibility, keyboard interaction, focus states, validation feedback, empty/loading/error states, and all major customer, ticket, attachment, activity, SLA, and dashboard journeys.

Use `playwright-cli` for browser-based verification and Aspire Skills for application health, logs, and runtime diagnostics. Run solution builds and relevant tests. Check for unintended backend changes, functional regressions, and unnecessary UI dependencies.

Report findings by severity, distinguish verified results from unverified areas, and state whether the feature is ready for a stacked PR.

**Do not submit the PR.**
```

## Stacked PR Submission

After implementation and review are complete, use the repository's `gh-stack` skill to prepare the `feature/ui-ux` PR targeting `feature/dashboard`. Submit only after explicit approval.
