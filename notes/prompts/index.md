# SupportHub — HVE Core RPI Feature Prompts

## Purpose

These prompts guide development of SupportHub through seven GitHub stacked pull requests. Each file contains four copy/paste prompts for GitHub Copilot CLI. Use them **one stage at a time**, reviewing outputs before continuing.

## What is HVE Core?

**HVE Core** is the engineering workflow and skill set used in this repository to guide AI-assisted software development. GitHub Copilot CLI is the coding agent; HVE Core skills structure the work, repository-local Aspire Skills support .NET Aspire-specific tasks, and the `gh-stack` skill manages stacked pull requests.

## What is RPI?

**RPI** stands for **Research → Plan → Implement**. We add a distinct **Review** stage, including tests and Aspire runtime verification:

1. `/rpi-research` — Inspect requirements, repository state, architecture, risks and testing options. No implementation.
2. `/rpi-plan` — Turn research into a scoped, reviewable implementation plan and acceptance criteria. No implementation.
3. `/rpi-implement` — Implement the approved plan, including tests, using Aspire Skills where relevant.
4. `/rpi-review` — Check the implementation against the plan, build/test it, inspect Aspire logs/traces and report findings.

These are prompts for the **existing HVE Core skills**, not definitions of new skills. Confirm the exact skill commands available in your Copilot CLI installation.

## Prerequisites

- `main` contains the Aspire AppHost, all SupportHub projects, infrastructure emulators, client NuGet dependencies and baseline tests.
- `.github/copilot-instructions.md` describes project responsibilities and boundaries.
- Aspire runs locally and the GitHub `gh-stack` extension/skill is configured.
- Use the actual repository state as source of truth; prompts describe intended behavior, not necessarily implemented behavior.

## Feature stack

| Order | Prompt file | Feature branch | PR targets |
|---|---|---|---|
| 1 | [1-feature-customers.md](1-feature-customers.md) | `feature/customers` | `main` |
| 2 | [2-feature-tickets.md](2-feature-tickets.md) | `feature/tickets` | `feature/customers` |
| 3 | [3-feature-attachments.md](3-feature-attachments.md) | `feature/attachments` | `feature/tickets` |
| 4 | [4-feature-activity.md](4-feature-activity.md) | `feature/activity` | `feature/attachments` |
| 5 | [5-feature-sla.md](5-feature-sla.md) | `feature/sla` | `feature/activity` |
| 6 | [6-feature-dashboard.md](6-feature-dashboard.md) | `feature/dashboard` | `feature/sla` |
| 7 | [7-feature-ui-ux.md](7-feature-ui-ux.md) | `feature/ui-ux` | `feature/dashboard` |

## How to use each file

1. Check out the relevant stacked feature branch, based on its listed parent.
2. Copy the text under **`/rpi-research`** into Copilot CLI with that skill and inspect the result.
3. Run **`/rpi-plan`** using the research artifacts; approve or revise the plan.
4. Run **`/rpi-implement`** using the approved plan; test and inspect the Aspire runtime.
5. Run **`/rpi-review`**; resolve important findings and re-run verification.
6. Commit changes and use the `gh-stack` skill to submit or update the PR **only after your approval**.
7. Create the next stack layer on top of the completed feature branch.

Keep feature work within its PR. Do not treat a passing build alone as proof of functionality. If tests or emulator validation cannot run, report that explicitly.

## Feature 7: Frontend UX & UI Polish

The final feature improves the usability and visual quality of the completed SupportHub Blazor application. It covers consistent navigation and shared components, customer and ticket workflows, dashboard presentation, activity timelines, SLA indicators, responsive layouts, accessibility, loading/empty/error states, and browser-based regression checks.

Keep feature 6 focused on dashboard functionality, Redis caching, observability, and operational correctness. Reserve broad frontend redesign for feature 7. Preserve existing APIs, data ownership, and business rules; use the repository `playwright-cli` skill where available to inspect the rendered experience.
