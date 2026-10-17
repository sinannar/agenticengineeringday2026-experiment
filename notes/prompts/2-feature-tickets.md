# Ticket Management — HVE Core RPI Prompts

**Stacked PR branch:** `feature/tickets`  
**Parent branch:** `feature/customers`

Run each skill separately in GitHub Copilot CLI. Review its output before proceeding to the next stage. Follow `.github/copilot-instructions.md` and the repository's Aspire Skills. Do not submit a PR without approval.

### /rpi-research

```markdown
Research the implementation of **SupportHub Ticket Management** for the GitHub stacked PR `feature/tickets`, based on `feature/customers`.

Read `.github/copilot-instructions.md`, inspect the current branch and completed preceding features, and examine Tickets API, Customers API contract, Blazor Web, PostgreSQL `ticketsdb`, service discovery, and test projects.

The feature must support:
- Ticket creation, retrieval, updates, and deletion.
- Title, description, status, priority, assignee, and timestamps.
- Customer association and validation through Customers API (never its database).
- Ticket filtering by status, priority, customer, and assignee.
- PostgreSQL persistence with EF Core and `ticketsdb`.
- Blazor ticket listing, details, creation, and editing.
- Automated API and Aspire integration tests.

Use the repository's Aspire Skills to inspect resource wiring, client integrations, service discovery, observability, and test infrastructure.

Identify the recommended architecture, API contracts, ticket data model, customer validation strategy, EF Core migrations, filtering, frontend integration, and test strategy. Document risks, dependencies, and acceptance criteria.

Respect project responsibilities and data ownership. Keep the scope limited to Ticket Management.

**Research only. Do not modify application code, implement features, or create pull requests.**
```

### /rpi-plan

```markdown
Create an implementation plan for **SupportHub Ticket Management** using the completed research.

Break the work into small, testable steps covering Tickets API CRUD and filtering, customer validation through HTTP, PostgreSQL EF Core persistence and migrations, Blazor ticket pages, and automated tests.

Include affected files and projects, architecture boundaries, acceptance criteria, Aspire runtime verification, tests, risks, and rollback considerations.

Keep the scope strictly within `feature/tickets`. Do not implement code or create a PR.
```

### /rpi-implement

```markdown
Implement the approved **SupportHub Ticket Management** plan on `feature/tickets`.

Follow `.github/copilot-instructions.md`, use Aspire Skills where relevant, and implement incrementally. Include Tickets API CRUD and filtering, customer validation through HTTP, PostgreSQL EF Core persistence and migrations, Blazor ticket pages, and automated tests.

Run relevant builds and tests, validate affected resources through Aspire, inspect logs and traces, and resolve implementation failures.

Do not implement later features, make unrelated changes, or submit a pull request yet.
```

### /rpi-review

```markdown
Review **SupportHub Ticket Management** on `feature/tickets` against the approved plan and acceptance criteria.

Check architecture and data ownership boundaries, ticket lifecycle behavior, customer validation, filters, EF Core migrations, persistence, UI, API contracts, and error handling.

Use Aspire Skills to inspect resource health, logs, traces, and distributed behavior. Run the solution build and relevant unit/integration/E2E tests; report anything that could not be verified.

Report findings by severity and clearly state whether the feature is ready for a stacked PR targeting `feature/customers`. Do not submit the PR.
```
