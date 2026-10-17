# Ticket Attachments — HVE Core RPI Prompts

**Stacked PR branch:** `feature/attachments`  
**Parent branch:** `feature/tickets`

Run each skill separately in GitHub Copilot CLI. Review its output before proceeding to the next stage. Follow `.github/copilot-instructions.md` and the repository's Aspire Skills. Do not submit a PR without approval.

### /rpi-research

```markdown
Research the implementation of **SupportHub Ticket Attachments** for the GitHub stacked PR `feature/attachments`, based on `feature/tickets`.

Read `.github/copilot-instructions.md`, inspect the current branch and completed preceding features, and examine Tickets API, Blazor Web, Azure Blob Storage/Azurite integration, `ticketsdb`, and test projects.

The feature must support:
- Upload, list, download, and delete attachments belonging to tickets.
- Store binary content in Azure Blob Storage (Azurite locally).
- Store attachment metadata in PostgreSQL `ticketsdb`.
- Validate file size, content type, and ticket/attachment ownership.
- Handle missing blobs, failed uploads, and metadata/blob consistency.
- Blazor attachment upload and management UI.
- Automated API and Azurite integration tests.

Use the repository's Aspire Skills to inspect resource wiring, client integrations, service discovery, observability, and test infrastructure.

Identify the recommended architecture, attachment API contracts, blob naming and access strategy, metadata schema, file validation, failure handling, frontend UX, and Azurite test strategy. Document risks, dependencies, and acceptance criteria.

Respect project responsibilities and data ownership. Keep the scope limited to Ticket Attachments.

**Research only. Do not modify application code, implement features, or create pull requests.**
```

### /rpi-plan

```markdown
Create an implementation plan for **SupportHub Ticket Attachments** using the completed research.

Break the work into small, testable steps covering attachment endpoints, Blob Storage integration, PostgreSQL metadata, validation, Blazor attachment UI, and automated tests.

Include affected files and projects, architecture boundaries, acceptance criteria, Aspire runtime verification, tests, risks, and rollback considerations.

Keep the scope strictly within `feature/attachments`. Do not implement code or create a PR.
```

### /rpi-implement

```markdown
Implement the approved **SupportHub Ticket Attachments** plan on `feature/attachments`.

Follow `.github/copilot-instructions.md`, use Aspire Skills where relevant, and implement incrementally. Include attachment endpoints, Blob Storage integration, PostgreSQL metadata, validation, Blazor attachment UI, and automated tests.

Run relevant builds and tests, validate affected resources through Aspire, inspect logs and traces, and resolve implementation failures.

Do not implement later features, make unrelated changes, or submit a pull request yet.
```

### /rpi-review

```markdown
Review **SupportHub Ticket Attachments** on `feature/attachments` against the approved plan and acceptance criteria.

Check architecture and data ownership boundaries, upload/download/delete behavior, access checks, file validation, blob/metadata consistency, failure handling, UI, and Azurite integration tests.

Use Aspire Skills to inspect resource health, logs, traces, and distributed behavior. Run the solution build and relevant unit/integration/E2E tests; report anything that could not be verified.

Report findings by severity and clearly state whether the feature is ready for a stacked PR targeting `feature/tickets`. Do not submit the PR.
```
