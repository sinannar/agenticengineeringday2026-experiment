# Support Dashboard & Operational Polish — HVE Core RPI Prompts

**Stacked PR branch:** `feature/dashboard`  
**Parent branch:** `feature/sla`

Run each skill separately in GitHub Copilot CLI. Review its output before proceeding to the next stage. Follow `.github/copilot-instructions.md` and the repository's Aspire Skills. Do not submit a PR without approval.

### /rpi-research

```markdown
Research the implementation of **SupportHub Support Dashboard & Operational Polish** for the GitHub stacked PR `feature/dashboard`, based on `feature/sla`.

Read `.github/copilot-instructions.md`, inspect the current branch and completed preceding features, and examine Blazor Web, Customers API, Tickets API, workers, Redis, AppHost integration tests, and existing telemetry.

The feature must support:
- Build a support dashboard with open ticket counts, status/priority breakdowns, and at-risk/breached SLA metrics.
- Add Redis caching for justified read-heavy operations, with expiry and invalidation.
- Keep critical metrics correct after customer/ticket updates.
- Improve health checks, resilience, structured logging, and OpenTelemetry traces.
- Validate API, worker, messaging, storage, and UI interactions end to end.
- Add Aspire integration tests and important Blazor user-journey tests.
- Avoid unrelated feature expansion.

Use the repository's Aspire Skills to inspect resource wiring, client integrations, service discovery, observability, and test infrastructure.

Identify the recommended architecture, dashboard API contracts and query strategy, cache boundaries and invalidation, operational telemetry, failure modes, E2E testing, and acceptance criteria. Document risks, dependencies, and acceptance criteria.

Respect project responsibilities and data ownership. Keep the scope limited to Support Dashboard & Operational Polish.

**Research only. Do not modify application code, implement features, or create pull requests.**
```

### /rpi-plan

```markdown
Create an implementation plan for **SupportHub Support Dashboard & Operational Polish** using the completed research.

Break the work into small, testable steps covering support metrics, dashboard UI, appropriate Redis caching and invalidation, observability improvements, resilience, and automated E2E/integration tests.

Include affected files and projects, architecture boundaries, acceptance criteria, Aspire runtime verification, tests, risks, and rollback considerations.

Keep the scope strictly within `feature/dashboard`. Do not implement code or create a PR.
```

### /rpi-implement

```markdown
Implement the approved **SupportHub Support Dashboard & Operational Polish** plan on `feature/dashboard`.

Follow `.github/copilot-instructions.md`, use Aspire Skills where relevant, and implement incrementally. Include support metrics, dashboard UI, appropriate Redis caching and invalidation, observability improvements, resilience, and automated E2E/integration tests.

Run relevant builds and tests, validate affected resources through Aspire, inspect logs and traces, and resolve implementation failures.

Do not implement later features, make unrelated changes, or submit a pull request yet.
```

### /rpi-review

```markdown
Review **SupportHub Support Dashboard & Operational Polish** on `feature/dashboard` against the approved plan and acceptance criteria.

Check architecture and data ownership boundaries, metric correctness, cache invalidation and staleness, health checks, logs/traces, distributed resilience, end-to-end user journeys, and regression coverage.

Use Aspire Skills to inspect resource health, logs, traces, and distributed behavior. Run the solution build and relevant unit/integration/E2E tests; report anything that could not be verified.

Report findings by severity and clearly state whether the feature is ready for a stacked PR targeting `feature/sla`. Do not submit the PR.
```
