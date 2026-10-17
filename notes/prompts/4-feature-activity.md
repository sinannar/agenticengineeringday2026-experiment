# Activity and Audit — HVE Core RPI Prompts

**Branch:** `feature/activity`  
**PR target:** `feature/attachments`  
**Usage:** Invoke each skill separately in GitHub Copilot CLI. Review each stage's output before proceeding.

## /rpi-research

```markdown
Research the implementation of **SupportHub Activity and Audit** for the stacked PR `feature/activity` (parent `feature/attachments`).

Feature: **Activity and Audit**. Current branch: `feature/activity`; PR parent: `feature/attachments`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Feature requirements:
- Publish ticket-created, ticket-updated, status-changed and assignment-changed events from Tickets API.
- Use Service Bus `ticket-events` queue; implement Activity Worker consumer.
- Persist activity/audit documents in Cosmos DB Linux emulator `activitydb`.
- Define event contracts, delivery guarantees, retries, idempotency and failure handling.
- Expose ticket history through an appropriate backend read endpoint, not direct Web-to-Cosmos access.
- Show activity timeline in Blazor and test end-to-end event processing.

Inspect affected projects, current Aspire AppHost references, client integrations, existing tests and relevant domain contracts. Identify recommended design, APIs, data model/storage, frontend changes, edge cases, dependencies, implementation risks and test strategy. Capture clear acceptance criteria.

Research only. Do not modify application code, implement features, or create pull requests.
```

## /rpi-plan

```markdown
Create a concrete, testable implementation plan for **SupportHub Activity and Audit** using the completed research artifacts.

Feature: **Activity and Audit**. Current branch: `feature/activity`; PR parent: `feature/attachments`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Planned scope:
- Publish ticket-created, ticket-updated, status-changed and assignment-changed events from Tickets API.
- Use Service Bus `ticket-events` queue; implement Activity Worker consumer.
- Persist activity/audit documents in Cosmos DB Linux emulator `activitydb`.
- Define event contracts, delivery guarantees, retries, idempotency and failure handling.
- Expose ticket history through an appropriate backend read endpoint, not direct Web-to-Cosmos access.
- Show activity timeline in Blazor and test end-to-end event processing.

Break work into small ordered steps. Identify affected files/projects, contracts, infrastructure client registration, tests, Aspire runtime verification, failure cases, and migration or compatibility considerations where applicable.

Acceptance criteria:
- Ticket changes create retrievable audit entries.
- Duplicate deliveries do not create duplicate activity records.
- Failures and message flow are observable through Aspire logs/traces.

Plan only. Do not change application code or create a pull request. Present the plan for approval before implementation.
```

## /rpi-implement

```markdown
Implement the approved plan for **SupportHub Activity and Audit** on `feature/activity`.

Feature: **Activity and Audit**. Current branch: `feature/activity`; PR parent: `feature/attachments`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Required scope:
- Publish ticket-created, ticket-updated, status-changed and assignment-changed events from Tickets API.
- Use Service Bus `ticket-events` queue; implement Activity Worker consumer.
- Persist activity/audit documents in Cosmos DB Linux emulator `activitydb`.
- Define event contracts, delivery guarantees, retries, idempotency and failure handling.
- Expose ticket history through an appropriate backend read endpoint, not direct Web-to-Cosmos access.
- Show activity timeline in Blazor and test end-to-end event processing.

Follow the approved research and plan, implement incrementally, and add meaningful automated tests. Use Aspire Skills for service wiring, integration, health checks and runtime diagnosis. Run solution build, relevant tests and Aspire integration verification where available; fix failures within this feature's scope.

Do not implement later features, rename unrelated projects, or submit a pull request. Report changes, tests run and any unverified acceptance criteria.
```

## /rpi-review

```markdown
Review **SupportHub Activity and Audit** on `feature/activity` against the approved research, plan and acceptance criteria.

Feature: **Activity and Audit**. Current branch: `feature/activity`; PR parent: `feature/attachments`.

Read `.github/copilot-instructions.md` and relevant existing code before making decisions. Respect service data ownership and use repository-local Aspire Skills for version-specific infrastructure, service discovery and runtime diagnostics. Keep work limited to this feature and do not implement later stack layers.

Acceptance criteria:
- Ticket changes create retrievable audit entries.
- Duplicate deliveries do not create duplicate activity records.
- Failures and message flow are observable through Aspire logs/traces.

Inspect changes for correctness, service boundaries, validation, security, resilience, observability, and test quality. Run `dotnet build SupportHub.slnx`, relevant `dotnet test` commands, and Aspire runtime/integration checks where available. Examine health, logs and traces; report any checks that cannot be performed.

Report findings ordered by severity, missing coverage, remaining risks, and whether the feature is ready for its stacked PR. Do not submit the PR.
```
