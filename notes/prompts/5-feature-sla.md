# SLA Management — HVE Core RPI Prompts

**Branch:** `feature/sla`  
**PR target:** `feature/activity`  
**Usage:** Invoke each skill separately in GitHub Copilot CLI. Review each stage's output before proceeding.

## /rpi-research

```markdown
Research the implementation of **SupportHub SLA Management** for the stacked PR `feature/sla` (parent `feature/activity`).

Feature: **SLA Management**. Current branch: `feature/sla`; PR parent: `feature/activity`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Feature requirements:
- Define SLA policy based on ticket priority and customer support tier, including deadlines and at-risk/breached states.
- Implement deterministic, testable SLA calculations with injectable time.
- SLA Worker performs scheduled scans through Tickets API, not direct database access.
- Operations Console (`SupportHub.Sla.Console`, resource `supporthub-cli`) triggers on-demand SLA scans through application APIs.
- Share evaluation behavior without duplicating domain rules in worker and console.
- Show SLA status and warnings in Blazor; add unit and integration tests.

Inspect affected projects, current Aspire AppHost references, client integrations, existing tests and relevant domain contracts. Identify recommended design, APIs, data model/storage, frontend changes, edge cases, dependencies, implementation risks and test strategy. Capture clear acceptance criteria.

Research only. Do not modify application code, implement features, or create pull requests.
```

## /rpi-plan

```markdown
Create a concrete, testable implementation plan for **SupportHub SLA Management** using the completed research artifacts.

Feature: **SLA Management**. Current branch: `feature/sla`; PR parent: `feature/activity`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Planned scope:
- Define SLA policy based on ticket priority and customer support tier, including deadlines and at-risk/breached states.
- Implement deterministic, testable SLA calculations with injectable time.
- SLA Worker performs scheduled scans through Tickets API, not direct database access.
- Operations Console (`SupportHub.Sla.Console`, resource `supporthub-cli`) triggers on-demand SLA scans through application APIs.
- Share evaluation behavior without duplicating domain rules in worker and console.
- Show SLA status and warnings in Blazor; add unit and integration tests.

Break work into small ordered steps. Identify affected files/projects, contracts, infrastructure client registration, tests, Aspire runtime verification, failure cases, and migration or compatibility considerations where applicable.

Acceptance criteria:
- SLA calculations are correct for boundaries and time-dependent scenarios.
- Scheduled and manual scans behave consistently.
- Worker and CLI operate through service discovery and are observable in Aspire.

Plan only. Do not change application code or create a pull request. Present the plan for approval before implementation.
```

## /rpi-implement

```markdown
Implement the approved plan for **SupportHub SLA Management** on `feature/sla`.

Feature: **SLA Management**. Current branch: `feature/sla`; PR parent: `feature/activity`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Required scope:
- Define SLA policy based on ticket priority and customer support tier, including deadlines and at-risk/breached states.
- Implement deterministic, testable SLA calculations with injectable time.
- SLA Worker performs scheduled scans through Tickets API, not direct database access.
- Operations Console (`SupportHub.Sla.Console`, resource `supporthub-cli`) triggers on-demand SLA scans through application APIs.
- Share evaluation behavior without duplicating domain rules in worker and console.
- Show SLA status and warnings in Blazor; add unit and integration tests.

Follow the approved research and plan, implement incrementally, and add meaningful automated tests. Use Aspire Skills for service wiring, integration, health checks and runtime diagnosis. Run solution build, relevant tests and Aspire integration verification where available; fix failures within this feature's scope.

Do not implement later features, rename unrelated projects, or submit a pull request. Report changes, tests run and any unverified acceptance criteria.
```

## /rpi-review

```markdown
Review **SupportHub SLA Management** on `feature/sla` against the approved research, plan and acceptance criteria.

Feature: **SLA Management**. Current branch: `feature/sla`; PR parent: `feature/activity`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Acceptance criteria:
- SLA calculations are correct for boundaries and time-dependent scenarios.
- Scheduled and manual scans behave consistently.
- Worker and CLI operate through service discovery and are observable in Aspire.

Inspect changes for correctness, service boundaries, validation, security, resilience, observability, and test quality. Run `dotnet build SupportHub.slnx`, relevant `dotnet test` commands, and Aspire runtime/integration checks where available. Examine health, logs and traces; report any checks that cannot be performed.

Report findings ordered by severity, missing coverage, remaining risks, and whether the feature is ready for its stacked PR. Do not submit the PR.
```
