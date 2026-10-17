# Ticket Management — HVE Core RPI Prompts

**Branch:** `feature/tickets`  
**PR target:** `feature/customers`  
**Usage:** Invoke each skill separately in GitHub Copilot CLI. Review each stage's output before proceeding.

## /rpi-research

```markdown
Research the implementation of **SupportHub Ticket Management** for the stacked PR `feature/tickets` (parent `feature/customers`).

Feature: **Ticket Management**. Current branch: `feature/tickets`; PR parent: `feature/customers`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Feature requirements:
- Ticket CRUD with title, description, status, priority, assignee and timestamps.
- Associate tickets with existing customers; validate IDs through Customers API, never `customersdb`.
- Filter and list tickets by status, priority, customer and assignee.
- EF Core persistence and migrations in PostgreSQL `ticketsdb`.
- Blazor ticket list, detail, create and edit workflows.
- Automated API and Aspire integration tests.

Inspect affected projects, current Aspire AppHost references, client integrations, existing tests and relevant domain contracts. Identify recommended design, APIs, data model/storage, frontend changes, edge cases, dependencies, implementation risks and test strategy. Capture clear acceptance criteria.

Research only. Do not modify application code, implement features, or create pull requests.
```

## /rpi-plan

```markdown
Create a concrete, testable implementation plan for **SupportHub Ticket Management** using the completed research artifacts.

Feature: **Ticket Management**. Current branch: `feature/tickets`; PR parent: `feature/customers`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Planned scope:
- Ticket CRUD with title, description, status, priority, assignee and timestamps.
- Associate tickets with existing customers; validate IDs through Customers API, never `customersdb`.
- Filter and list tickets by status, priority, customer and assignee.
- EF Core persistence and migrations in PostgreSQL `ticketsdb`.
- Blazor ticket list, detail, create and edit workflows.
- Automated API and Aspire integration tests.

Break work into small ordered steps. Identify affected files/projects, contracts, infrastructure client registration, tests, Aspire runtime verification, failure cases, and migration or compatibility considerations where applicable.

Acceptance criteria:
- Ticket lifecycle works end-to-end and persists across restarts.
- Invalid customer references are rejected.
- Filtering, validation and integration tests pass.

Plan only. Do not change application code or create a pull request. Present the plan for approval before implementation.
```

## /rpi-implement

```markdown
Implement the approved plan for **SupportHub Ticket Management** on `feature/tickets`.

Feature: **Ticket Management**. Current branch: `feature/tickets`; PR parent: `feature/customers`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Required scope:
- Ticket CRUD with title, description, status, priority, assignee and timestamps.
- Associate tickets with existing customers; validate IDs through Customers API, never `customersdb`.
- Filter and list tickets by status, priority, customer and assignee.
- EF Core persistence and migrations in PostgreSQL `ticketsdb`.
- Blazor ticket list, detail, create and edit workflows.
- Automated API and Aspire integration tests.

Follow the approved research and plan, implement incrementally, and add meaningful automated tests. Use Aspire Skills for service wiring, integration, health checks and runtime diagnosis. Run solution build, relevant tests and Aspire integration verification where available; fix failures within this feature's scope.

Do not implement later features, rename unrelated projects, or submit a pull request. Report changes, tests run and any unverified acceptance criteria.
```

## /rpi-review

```markdown
Review **SupportHub Ticket Management** on `feature/tickets` against the approved research, plan and acceptance criteria.

Feature: **Ticket Management**. Current branch: `feature/tickets`; PR parent: `feature/customers`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Acceptance criteria:
- Ticket lifecycle works end-to-end and persists across restarts.
- Invalid customer references are rejected.
- Filtering, validation and integration tests pass.

Inspect changes for correctness, service boundaries, validation, security, resilience, observability, and test quality. Run `dotnet build SupportHub.slnx`, relevant `dotnet test` commands, and Aspire runtime/integration checks where available. Examine health, logs and traces; report any checks that cannot be performed.

Report findings ordered by severity, missing coverage, remaining risks, and whether the feature is ready for its stacked PR. Do not submit the PR.
```
