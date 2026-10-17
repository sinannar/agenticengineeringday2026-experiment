# SLA Management — HVE Core RPI Prompts

**Stacked PR branch:** `feature/sla`  
**Parent branch:** `feature/activity`

Run each skill separately in GitHub Copilot CLI. Review its output before proceeding to the next stage. Follow `.github/copilot-instructions.md` and the repository's Aspire Skills. Do not submit a PR without approval.

### /rpi-research

```markdown
Research the implementation of **SupportHub SLA Management** for the GitHub stacked PR `feature/sla`, based on `feature/activity`.

Read `.github/copilot-instructions.md`, inspect the current branch and completed preceding features, and examine Tickets API, Customers API support-tier contract, SLA Worker, SLA Console, Blazor Web, and test projects.

The feature must support:
- Define SLA deadlines based on ticket priority and customer support tier.
- Calculate at-risk and breached SLA states with testable time handling.
- Run scheduled SLA evaluations in `SupportHub.Sla.Worker`.
- Keep Tickets API authoritative for ticket state; the worker must not access `ticketsdb` directly.
- Expose supported SLA operations through Tickets API.
- Trigger on-demand scans from `SupportHub.Sla.Console` using Aspire service discovery.
- Reuse SLA rules between scheduled and on-demand workflows.
- Display SLA indicators and warnings in Blazor.
- Add unit tests for deadlines/boundaries and integration tests.

Use the repository's Aspire Skills to inspect resource wiring, client integrations, service discovery, observability, and test infrastructure.

Identify the recommended architecture, SLA policy model, time abstraction, API contracts, scan scheduling, shared rule placement, idempotent updates, console interaction, UI, and testing. Document risks, dependencies, and acceptance criteria.

Respect project responsibilities and data ownership. Keep the scope limited to SLA Management.

**Research only. Do not modify application code, implement features, or create pull requests.**
```

### /rpi-plan

```markdown
Create an implementation plan for **SupportHub SLA Management** using the completed research.

Break the work into small, testable steps covering SLA rules and deadline evaluation, Tickets API endpoints, scheduled SLA Worker, console on-demand scans, Blazor SLA indicators, and automated tests.

Include affected files and projects, architecture boundaries, acceptance criteria, Aspire runtime verification, tests, risks, and rollback considerations.

Keep the scope strictly within `feature/sla`. Do not implement code or create a PR.
```

### /rpi-implement

```markdown
Implement the approved **SupportHub SLA Management** plan on `feature/sla`.

Follow `.github/copilot-instructions.md`, use Aspire Skills where relevant, and implement incrementally. Include SLA rules and deadline evaluation, Tickets API endpoints, scheduled SLA Worker, console on-demand scans, Blazor SLA indicators, and automated tests.

Run relevant builds and tests, validate affected resources through Aspire, inspect logs and traces, and resolve implementation failures.

Do not implement later features, make unrelated changes, or submit a pull request yet.
```

### /rpi-review

```markdown
Review **SupportHub SLA Management** on `feature/sla` against the approved plan and acceptance criteria.

Check architecture and data ownership boundaries, deadline calculations, boundary/time-zone cases, at-risk and breached classifications, scheduled/on-demand consistency, worker resiliency, console interaction, UI, and tests.

Use Aspire Skills to inspect resource health, logs, traces, and distributed behavior. Run the solution build and relevant unit/integration/E2E tests; report anything that could not be verified.

Report findings by severity and clearly state whether the feature is ready for a stacked PR targeting `feature/activity`. Do not submit the PR.
```
