# Activity & Audit — HVE Core RPI Prompts

**Stacked PR branch:** `feature/activity`  
**Parent branch:** `feature/attachments`

Run each skill separately in GitHub Copilot CLI. Review its output before proceeding to the next stage. Follow `.github/copilot-instructions.md` and the repository's Aspire Skills. Do not submit a PR without approval.

### /rpi-research

```markdown
Research the implementation of **SupportHub Activity & Audit** for the GitHub stacked PR `feature/activity`, based on `feature/attachments`.

Read `.github/copilot-instructions.md`, inspect the current branch and completed preceding features, and examine Tickets API, Activity Worker, Blazor Web, Service Bus emulator, Cosmos DB Linux emulator, and test projects.

The feature must support:
- Publish ticket-created, ticket-updated, status-changed, and assignment-changed events from Tickets API.
- Use the `ticket-events` Azure Service Bus queue (local emulator).
- Consume events in `SupportHub.Activity.Worker`.
- Persist ticket activity/audit documents in Cosmos DB `activitydb` using the Linux emulator.
- Handle duplicate messages, retries, poison messages, and processing failures.
- Expose ticket history through an appropriate backend read endpoint, not direct Blazor-to-Cosmos access.
- Show ticket activity history in Blazor.
- Add messaging, persistence, and Aspire integration tests.

Use the repository's Aspire Skills to inspect resource wiring, client integrations, service discovery, observability, and test infrastructure.

Identify the recommended architecture, event contracts, publication reliability, idempotency, Cosmos partition keys and document model, failure handling, read API design, UI, and integration testing. Document risks, dependencies, and acceptance criteria.

Respect project responsibilities and data ownership. Keep the scope limited to Activity & Audit.

**Research only. Do not modify application code, implement features, or create pull requests.**
```

### /rpi-plan

```markdown
Create an implementation plan for **SupportHub Activity & Audit** using the completed research.

Break the work into small, testable steps covering ticket event publication, Service Bus consumption, idempotent Cosmos audit persistence, ticket history read API, Blazor history UI, and automated tests.

Include affected files and projects, architecture boundaries, acceptance criteria, Aspire runtime verification, tests, risks, and rollback considerations.

Keep the scope strictly within `feature/activity`. Do not implement code or create a PR.
```

### /rpi-implement

```markdown
Implement the approved **SupportHub Activity & Audit** plan on `feature/activity`.

Follow `.github/copilot-instructions.md`, use Aspire Skills where relevant, and implement incrementally. Include ticket event publication, Service Bus consumption, idempotent Cosmos audit persistence, ticket history read API, Blazor history UI, and automated tests.

Run relevant builds and tests, validate affected resources through Aspire, inspect logs and traces, and resolve implementation failures.

Do not implement later features, make unrelated changes, or submit a pull request yet.
```

### /rpi-review

```markdown
Review **SupportHub Activity & Audit** on `feature/activity` against the approved plan and acceptance criteria.

Check architecture and data ownership boundaries, event correctness, publication reliability, idempotency, retry/dead-letter handling, Cosmos persistence, ticket history, distributed traces, and integration tests.

Use Aspire Skills to inspect resource health, logs, traces, and distributed behavior. Run the solution build and relevant unit/integration/E2E tests; report anything that could not be verified.

Report findings by severity and clearly state whether the feature is ready for a stacked PR targeting `feature/attachments`. Do not submit the PR.
```
