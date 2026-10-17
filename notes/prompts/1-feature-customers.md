
### /rpi-research

```markdown
Research the implementation of **SupportHub Customer Management** for the first GitHub stacked PR, `feature/customers`.

Read `.github/copilot-instructions.md` and inspect the existing solution, Customers API, Blazor Web, PostgreSQL configuration, Aspire AppHost, and test projects.

The feature must support:
- Customer creation, retrieval, updates, and deletion.
- Customer name, email, phone number, and support tier.
- Customer listing and detail views.
- Input validation and appropriate HTTP responses.
- PostgreSQL persistence using EF Core and `customersdb`.
- Blazor UI for managing customers.
- Automated API and integration tests.

Use the repository's Aspire Skills to understand existing infrastructure, service discovery, and testing patterns.

Identify the recommended architecture, API endpoints, data model, validation approach, EF Core migrations, frontend integration, test strategy, dependencies, and implementation risks.

Respect Customers API data ownership. Blazor must access customer data through the API, not PostgreSQL.

**Research only. Do not modify application code, implement features, or create pull requests.** Keep the scope limited to Customer Management.
```

### /rpi-plan

```markdown
Create an implementation plan for SupportHub Customer Management using the completed research.

Break the work into clear, testable steps covering Customers API, PostgreSQL EF Core persistence and migrations, validation, Blazor customer management pages, and automated tests.

Include acceptance criteria, affected files/projects, Aspire runtime verification, and potential risks.

Keep the scope strictly within `feature/customers`. Do not implement code or create a PR.
```

### /rpi-implement

```markdown
Implement the approved SupportHub Customer Management plan on `feature/customers`.

Follow `.github/copilot-instructions.md`, use Aspire Skills where relevant, and implement the feature incrementally.

Include Customers API CRUD endpoints, EF Core PostgreSQL persistence, migrations, validation, Blazor customer management UI, and automated tests.

Run relevant builds and tests, verify the application through Aspire, and resolve implementation failures.

Do not implement ticket management or other future features. Do not submit a pull request yet.
```

### /rpi-review

```markdown
Review the Customer Management implementation on `feature/customers` against the approved plan and acceptance criteria.

Check architecture boundaries, API behavior, validation, PostgreSQL persistence, EF Core migrations, Blazor functionality, test coverage, and error handling.

Use Aspire Skills to inspect runtime health, logs, traces, and integration behavior where available.

Run solution build and relevant tests. Identify defects, missing coverage, security issues, and remaining risks.

Report findings by severity and clearly state whether the feature is ready for a stacked PR. Do not submit the PR.
```