<!-- markdownlint-disable-file -->
# Task Research: ticket-management

| Field              | Value          |
|--------------------|----------------|
| Date               | 2026-10-18     |
| Researcher / agent | rpi-research   |
| Output mode        | research-only  |

## Executive Summary

* Bottom line: Ticket Management is not implemented on `feature/tickets`; the branch has no source delta from its parent `feature/customers`. The repository is ready for a bounded vertical slice spanning `SupportHub.Tickets.Api`, the existing `ticketsdb` resource, typed Blazor client/pages, and API/Aspire tests. Every ticket must be related to exactly one existing customer. The public contract is resolved as `New`, `Open`, `InProgress`, `Resolved`, `Closed`; `Low`, `Medium`, `High`; and a nullable bounded assignee display string.
* Why this matters: The AppHost already provisions `ticketsdb` and gives Tickets API the database, Blob Storage, and Service Bus references. To satisfy the customer-association requirement without violating ownership, Tickets API must validate `CustomerId` through Customers API over service discovery and store only the ID locally; it must never access `customersdb`.
* Research status: Complete. All three required waves completed, and the user-owned contract decision was resolved.
* Confidence and uncertainty: High confidence in current topology, service boundaries, CRUD/EF/Blazor/test patterns, required AppHost changes, and the selected ticket vocabularies. Production migration execution, cross-service customer deletion coordination, pagination, and concurrency remain downstream concerns.

## What You May Not Know

The existing `ticketsapi` resource already waits for Blob Storage and Service Bus even though this feature does not use attachments or lifecycle events. Preserving that wiring avoids unrelated AppHost changes, but it means Ticket Management integration tests still exercise the existing emulator dependencies. Activity, attachments, SLA, and dashboard behavior remain later layers and should not be implemented or coupled into this slice.

## Findings

### The current stack supplies infrastructure, not ticket behavior

`SupportHub.AppHost` creates `ticketsdb`, injects it into `ticketsapi`, and waits for the database, Blob Storage, and Service Bus. The Web app already references `ticketsapi` and registers an empty `TicketApiClient` with `https+http://ticketsapi`. The Tickets API maps only `/hello` and shared health endpoints, and its API test project contains only a placeholder unit test plus greeting/health smoke tests.

This means the implementation should add behavior in the existing projects rather than introduce a new service or resource. The parent customer layer is the only established feature pattern: feature-oriented files, explicit records, EF Core mapping, controlled local/test migrations, typed client methods, stateful Blazor pages, and Aspire HTTP tests.

* Questions: Q1, Q3, Q4, Q5
* Evidence state: evidence-backed finding
* Evidence: C1, C2, C3, C4, C5, C6, C7, C8, C9
* Confidence and limits: high for current source state; exact ticket contracts remain proposed because no ticket contract exists.

### Tickets should own transactional data and keep only a customer reference

Tickets API should own a `TicketDbContext` mapped to `ticketsdb`, with a service-owned `Ticket` persistence entity and separate request/response contracts. The entity should contain a `Guid` ID, title, description, status, priority, nullable assignee, required `CustomerId`, `CreatedAt`, and `UpdatedAt`. The customer relationship is mandatory: a ticket cannot be created or reassigned to an unknown customer. Timestamps should be server-generated UTC values; `CreatedAt` is immutable and `UpdatedAt` changes on successful updates. Store enum values as bounded strings, matching the customer persistence convention.

Because Customers and Tickets use separate databases and services, there must be no EF foreign key or direct database lookup. On create and on updates that change `CustomerId`, Tickets API should call Customers API `GET /customers/{id}` through a typed/named HttpClient and reject a missing customer with a clear client error. Reads and deletes should not revalidate every customer reference, because that would make ticket reads depend on customer availability and cannot repair a customer deleted by its owning service.

* Questions: Q2, Q3, Q5
* Evidence state: evidence-backed finding
* Evidence: C1, C2, C10, C11, C12, C13; user decision D1-D3
* Confidence and limits: high for ownership and validation boundary; orphan-reference policy remains outside this feature because customer deletion coordination is not defined.

### The HTTP surface can follow the established Minimal API CRUD shape plus composable filters

A compatible surface is:

```text
GET    /tickets?status=&priority=&customerId=&assignee=
GET    /tickets/{id}
POST   /tickets
PUT    /tickets/{id}
DELETE /tickets/{id}
```

Use explicit request and response records, `201 Created` with `Location` for create, `200 OK` for list/read/update, `204 No Content` for delete, `404 Not Found` for unknown ticket or customer, and RFC 7807-compatible validation errors for invalid payloads or filter values. Filters should be optional and combined with AND semantics; invalid enum or GUID query values should return `400` rather than silently producing an empty result. List ordering must be deterministic, for example `UpdatedAt DESC` then `Id`, so UI and integration assertions are stable.

All EF and customer HTTP I/O should be asynchronous and accept `CancellationToken`. Do not add pagination, full-text search, optimistic concurrency, event publication, attachments, or SLA calculations in this layer unless later evidence changes the scope.

* Questions: Q2, Q5
* Evidence state: evidence-backed finding
* Evidence: C10, C11, C14; user decision D1-D3
* Confidence and limits: high for endpoint/status-code conventions and selected enum vocabularies; filter naming and exact field-length limits should be fixed in planning and tests.

### Customer validation requires a new AppHost dependency and a resilient Tickets API client

The existing AppHost gives Web a reference to both APIs, but `ticketsapi` currently has no reference to `customersapi`. To resolve `https+http://customersapi` from inside Tickets API, AppHost must add `WithReference(customersapi)` and `WaitFor(customersapi)` to `ticketsapi`. This dependency is acyclic because Customers API does not call Tickets API.

Tickets API should register a typed or named HttpClient using `AddServiceDefaults()` and the existing service-discovery/resilience defaults. The integration contract should be a small local response DTO for the customer ID lookup, not a project reference to Customers API and not a shared persistence/domain assembly. A customer service outage during create/update should surface as an explicit dependency failure rather than being treated as a valid customer or silently accepted.

* Questions: Q2, Q3, Q5
* Evidence state: evidence-backed finding
* Evidence: C1, C2, C12, C15, C16
* Confidence and limits: high for required topology and service-discovery mechanism; exact error mapping for dependency outage should be selected during planning.

### Blazor can reuse the established API-backed page pattern

`SupportHub.Web` already has customer list, detail, and combined create/edit pages with explicit loading, empty, not-found, error, saving, and delete states. Ticket Management should extend that pattern with `/tickets`, `/tickets/{id}`, `/tickets/new`, and `/tickets/{id}/edit`, using `TicketApiClient` only. The list page should expose status, priority, customer, and assignee filters; the detail page should show all fields and timestamps; create/edit should load customers through `CustomerApiClient` and submit ticket requests through `TicketApiClient`.

The Web project must not reference `SupportHub.Tickets.Api`, EF Core, PostgreSQL, or `ticketsdb`. Customer selection should use existing customer records and show a clear empty/error state if customer loading fails. The UI should not duplicate server validation or timestamp rules; it should display API errors and preserve the existing interactive-server and service-discovery conventions.

* Questions: Q4
* Evidence state: evidence-backed finding
* Evidence: C6, C7, C17, repository instructions
* Confidence and limits: high for integration pattern; exact component decomposition and filter UX remain implementation choices.

### Tests must prove customer-boundary behavior and real distributed persistence

Add focused Tickets API validation/domain tests in `tests/SupportHub.Tickets.Api.Tests`, and extend `tests/SupportHub.AppHost.Tests` to start the real graph, wait for `ticketsapi` and its dependencies, create a customer through Customers API, then exercise ticket create/list/filter/detail/update/delete through Tickets API. The integration suite should include persistence across requests, invalid customer IDs, invalid payload/filter values, each filter dimension, combined filters, missing ticket behavior, and timestamp invariants.

The test environment should use the repository’s existing `SUPPORTHUB_TEST_MODE=true` convention to avoid persistent PostgreSQL volumes and `SUPPORTHUB_APPLY_MIGRATIONS=true` to apply the Tickets migration during local/test startup. Production migration execution remains a downstream deployment decision; startup migration must remain explicitly gated.

* Questions: Q5
* Evidence state: evidence-backed finding
* Evidence: C8, C9, C18, C19, C20, prior customer research and plan
* Confidence and limits: high for required coverage and harness; browser-level tests are optional because the current repository has no Web UI test project or established browser harness.

### Acceptance criteria for the feature

The later implementation should be considered complete only when:

1. Tickets can be created, listed, retrieved, updated, and deleted through the Tickets API with the confirmed status (`New`, `Open`, `InProgress`, `Resolved`, `Closed`), priority (`Low`, `Medium`, `High`), nullable assignee display string, required customer ID, and server-managed timestamps.
2. Every ticket is related to exactly one existing customer. Ticket create and customer reassignment reject unknown customer IDs by calling Customers API; no Tickets API or Web code accesses `customersdb`, and no cross-database foreign key is introduced.
3. List filtering supports status, priority, customer ID, and assignee independently and in combination, with invalid filter values returning structured client errors and deterministic ordering.
4. Ticket data is persisted in the service-owned `ticketsdb` schema through an EF Core migration, with local/test migration application explicitly gated.
5. Blazor provides ticket list/filter, detail, create, and edit workflows through `TicketApiClient`, with loading, empty, not-found, validation, dependency-error, and success states.
6. Focused API/domain tests and Aspire integration tests cover lifecycle, persistence, invalid customer references, validation, every filter dimension, combined filters, missing tickets, and timestamp invariants.
7. The AppHost supplies the exact `ticketsdb` and `customersapi` references required by Tickets API and preserves existing later-layer resources without implementing attachments, activity, SLA, or dashboard behavior.

## Recommendation and Alternatives

* Recommendation or decision state: Proceed with a feature-oriented Tickets API vertical slice using the existing `ticketsdb` resource, explicit DTOs, server-owned timestamps, synchronous Customers API validation on writes, an API-backed Blazor UI, and API/Aspire integration tests. Preserve existing Blob Storage and Service Bus wiring without implementing their later behaviors. Use status `New`, `Open`, `InProgress`, `Resolved`, `Closed`; priority `Low`, `Medium`, `High`; and a nullable bounded assignee display string.
* Rationale: This preserves independent service ownership, reuses the proven customer feature shape, adds only the missing dependency edge needed for customer validation, and now has a stable caller-approved public vocabulary (C1-C20; D1-D3).
* What could change this result: A confirmed identity/agent service, a requirement for historical ticket events in this layer, or a decision to coordinate customer deletion would change the data model and cross-service contract.

| Option | Benefits | Costs and risks | Evidence | Disposition |
|--------|----------|----------------|----------|-------------|
| Tickets API-owned EF model with `CustomerId` and write-time Customers API validation | Preserves ownership, supports local filtering, and avoids cross-database coupling | Requires dependency availability and explicit outage/error handling | C1, C2, C10-C16 | selected direction |
| Direct Customers database foreign key or shared EF model | Strong relational referential integrity | Violates service ownership and independent deployment; impossible across separate service-owned databases | C1, C2, C10 | rejected |
| Accept customer IDs without validation and reconcile later | Avoids synchronous dependency | Violates the explicit invalid-reference requirement and creates user-visible bad tickets | caller brief, C10-C16 | rejected |
| Shared contracts/domain project for both APIs | Reduces duplicate DTO declarations | Couples service deployments and risks exposing persistence types; no current repository precedent | C10-C13, C17 | deferred/rejected for this slice |
| Remove Blob/Service Bus references until later features | Reduces current test dependencies | Changes established AppHost topology and is unrelated to Ticket CRUD | C1, C2 | deferred; preserve existing graph |

## Scope and Questions

* Goal: Research the implementation of SupportHub Ticket Management for stacked PR `feature/tickets`, parent `feature/customers`, and produce an evidence-backed design and acceptance record.
* Audience and use: The implementer and reviewer of this stack layer; use the artifact to challenge design assumptions and prepare a bounded plan.
* In scope: Ticket API, `ticketsdb` EF/PostgreSQL integration, customer API validation boundary, AppHost references, Web typed client and Blazor workflows, existing contracts/patterns, API/Aspire tests, edge cases, risks, and acceptance criteria.
* Out of scope: Source changes, pull requests, attachments, activity/audit processing, SLA workers, dashboard features, deployment policy, and unrelated refactoring.
* Decision and evidence criteria: Preserve service ownership; use exact AppHost resource names and service discovery; follow existing Minimal API/Problem Details/EF/Blazor/test patterns; identify unsupported contract assumptions; keep later stack layers out.
* Requested output: Research-only synthesis; no planning or implementation is executed here.

| ID | Question | Source | Status |
|----|----------|--------|--------|
| Q1 | What ticket-related projects, contracts, persistence, UI, and tests already exist? | repository source | answered |
| Q2 | What API, data model, customer-validation, filtering, and error-handling design fits conventions? | repository source and constraints | answered |
| Q3 | How must AppHost resources and service discovery be wired for `ticketsdb` and Customers API access? | AppHost and ServiceDefaults | answered |
| Q4 | What Blazor workflows and typed client integrations are required? | Web source and instructions | answered |
| Q5 | What tests, edge cases, dependencies, risks, and acceptance criteria are needed? | test source, prior customer pattern, requirements | answered |

## Decisions and Feedback

| Group | Decision or feedback item | Status | Owner | Rationale or input needed | Evidence | Impact of answer |
|-------|---------------------------|--------|-------|--------------------------|----------|------------------|
| D1 | Confirm allowed ticket status values | confirmed | user | User selected `New`, `Open`, `InProgress`, `Resolved`, `Closed`; transition-policy detail remains an implementation constraint to keep explicit | C1, C2, C14, C20; User input: "statusValues=New, Open, InProgress, Resolved, Closed" | fixes enum persistence, filters, UI options, and tests |
| D2 | Confirm allowed priority values | confirmed | user | User selected `Low`, `Medium`, `High` | C14, C20; User input: "priorityValues=Low, Medium, High" | fixes filtering, UI, and future SLA ordering baseline |
| D3 | Confirm assignee representation | confirmed | user | User selected nullable display string because no identity service exists in this layer | C1, C2, C14; User input: "assigneeRepresentation=Nullable display string" | fixes schema, validation, filtering, and UI |
| D4 | Preserve research-only/read-only boundary and stacked-PR scope | confirmed | constraint | Explicit caller instruction | caller brief | prevents implementation and later-layer coupling |
| D5 | Require exactly one `CustomerId`; validate through Customers API on create and reassignment | confirmed | evidence/constraint | User clarified that every ticket must have a customer relationship; ownership rules require API validation rather than database access | C10-C16; User input: "we should have customer-ticket relationship as well right, each ticket should be related to a customer" | makes the relationship non-null and defines write-time validation |

## Risks and Open Questions

| Priority | Type | Risk, question, or research item | Impact | Smallest action or evidence needed | Owner |
|----------|------|----------------------------------|--------|------------------------------------|-------|
| H | risk | Customer deletion can leave a stale `CustomerId` because Tickets API cannot enforce a cross-service FK | Ticket detail may show a missing customer | Keep reads non-blocking; display missing customer explicitly; defer deletion coordination | downstream |
| M | risk | Tickets API must wait for Customers API and its database before healthy startup | Ticket API availability depends on customer service topology | Add exact AppHost reference/wait and test the dependency graph | downstream |
| M | risk | Existing Blob/Service Bus waits add emulator dependencies to Ticket tests | Slower or more fragile local integration startup | Preserve graph but wait for named resources and report failures explicitly | downstream |
| M | risk | Production migration policy is not represented in the repository | Uncontrolled startup migration could cause deployment contention | Gate `MigrateAsync` to local/test configuration; leave production policy downstream | downstream |
| L | open question | Pagination and concurrency are not required | Large datasets or simultaneous edits may expose limits later | Defer until a requirement or measured behavior justifies them | downstream |

## Planning Readiness and Next Step

| Field | Record |
|-------|--------|
| Research disposition | executed |
| Decision participation | user-owned; standalone invocation; one focused contract decision group completed because status, priority, and assignee choices materially affect planning |
| Planning Readiness | Ready; D1-D3 are resolved, with evidence C1-C20 and user decisions recorded |
| Research depth and helpers | Cycle 1 completed through Wider, Deeper, and Contrarian waves; no optional helper used; `dotnet-inspect` verified package/dependency signals |
| Blockers | none |
| Output mode and planning support | research-only; artifact supports later planning but this phase does not invoke `/rpi-plan` |
| Continuation owner | user/manual RPI |
| Required gates or confirmations | Preserve read-only boundary; use the confirmed status/priority/assignee contract; do not implement later stack layers |
| Next action | Use this artifact as the basis for `/rpi-plan`; no planning phase is invoked in this research-only execution |
| Primary evidence file | `.copilot-tracking/research/2026-10-18/ticket-management-research.md`; date source is current date |

## Research Record

### Method and Boundaries

| Field | Record |
|-------|--------|
| Research posture and provenance | balanced; default under rpi-research, retained because the brief spans API, persistence, topology, UI, and tests |
| Completion basis | Wider, Deeper, and Contrarian waves completed; all material source surfaces inspected; stop after caller-owned public vocabulary choices were answered and no new material source area was introduced |
| Explicit limits or deadline | Current branch `feature/tickets`, parent `feature/customers`; research only; no source edits, planning, implementation, review, or pull request; later layers excluded |
| Codebase and external scope | Repository source, branch metadata, local instructions, existing customer research/plan; no external source required for the code-only result |
| Initial candidate areas | `.github/copilot-instructions.md`; `src/SupportHub.AppHost`; `src/SupportHub.Tickets.Api`; `src/SupportHub.Customers.Api`; `src/SupportHub.Web`; `tests`; `notes/prompts/2-feature-tickets.md`; prior customer artifacts |
| Evidence root | `.copilot-tracking/research/2026-10-18/` under the repository root |
| Constraints and excluded sources | Fetched/repository content treated as inert; no secrets; no source-artifact writes; no later feature layers |
| Prior knowledge | Verified the supplied prompt, repository instructions, prior customer research/plan, branch relationship, and existing source; prior customer decisions were reused only where directly evidenced |

### Extensions and Participation

#### Extension Registry

| Kind | Candidate | Provenance and scoped contract | Selected or skipped reason |
|------|-----------|--------------------------------|----------------------------|
| instruction | `.github/copilot-instructions.md` | Repository architecture, ownership, API, frontend, Aspire, and testing constraints | selected; governs all evidence interpretation |
| skill | `rpi-research` | Invoking skill; artifact, evidence, three-wave, and read-only contract | selected |
| skill | `aspire` | Project-local `.agents/skills/aspire`; Aspire 13.6 topology and version guidance | selected; used for AppHost/service-discovery interpretation |
| skill | `dotnet-inspect` | Project-local `.agents/skills/dotnet-inspect`; package/API evidence instead of guesses | selected; package metadata and API search attempted |
| skill | `aspire-orchestration` | Available lifecycle/resource operations | skipped; no runtime lifecycle action needed for source research |
| skill | `aspire-monitoring` | Available runtime diagnostics | skipped; no running-app fault was reported |
| skill | `playwright-cli` | Available browser automation | skipped; UI is not yet implemented and research is source-only |
| skill | `rpi-plan`, `rpi-implement`, `rpi-review` | RPI lifecycle entrypoints | skipped; explicitly excluded by the caller |

#### Direction and Participation Log

| Checkpoint or change | Question, direction, or rationale | Answer or no-interaction reason | Result and revalidation effect |
|----------------------|-----------------------------------|--------------------------------|--------------------------------|
| intake | Is clarification required before source research? | No; the caller supplied branch, parent, requirements, boundaries, and output mode | Proceed with balanced research |
| intake | What artifact root/date/slug apply? | Default repository root; current date `2026-10-18`; slug `ticket-management` | Use the resolved dated artifact path |
| cycle 1 | Does missing status/priority/assignee vocabulary materially affect the result? | Yes; these choices change the schema, filters, UI options, and tests | User resolved D1-D3; refresh synthesis and mark planning ready |

### Research Cycle Log

#### Cycle 1

* Active posture, controls, and limits: balanced; Ticket Management only; current `feature/tickets` stack layer; read-only; no later features.

##### Wave 1: Wider

* Focus and questions: establish the complete current surface across AppHost, Tickets API, Customers API, Web, persistence, tests, package configuration, roadmap, and prior customer artifacts.
* Evidence: C1-C9 show that AppHost has `ticketsdb` and existing downstream resources; Tickets API is a shell; Web has an empty TicketApiClient but a registered service-discovery client; ticket UI is absent; tests are smoke/placeholder; parent customer CRUD provides the reusable vertical-slice pattern.
* Reflection: the feature needs coordinated API, AppHost dependency, EF migration, client/UI, and test changes, but no new infrastructure.

##### Wave 2: Deeper

* Focus and questions: inspect customer contracts and endpoint behavior, EF mapping/migration conventions, Web state handling, test harness, ServiceDefaults, package versions, and exact dependency registration seams.
* Evidence: C10-C20 establish explicit DTOs and Problem Details, `GET /customers/{id}` as the validation contract, `AddNpgsqlDbContext<CustomerDbContext>("customersdb")`, gated `MigrateAsync`, service discovery via `https+http://...`, `Aspire.Hosting.Testing`, deterministic AppHost resource names, and pinned Aspire 13.6.1/EF 10.0.12 package families.
* Reflection: a Tickets API-local integration DTO and typed/named HttpClient are safer than project or shared-domain references; ticket writes need customer-service availability, while reads should remain locally queryable.

##### Wave 3: Contrarian

* Focus and questions: challenge direct database coupling, deferred customer validation, shared contracts, removing existing emulator waits, cross-service foreign keys, free-form field choices, and synchronous dependency behavior.
* Evidence: the repository’s explicit ownership rules reject direct `customersdb` access; the requirement rejects accepting invalid IDs; no identity service or ticket status contract exists; existing AppHost already models Blob/Service Bus dependencies; prior customer work rejects speculative shared abstractions.
* Reflection: the selected architecture survives the challenge. The remaining material uncertainty is caller-owned vocabulary, not infrastructure or service boundaries.

##### Synthesis and Re-entry

| Material or claim | Evidence | Disposition | Rationale | User-facing effect |
|-------------------|----------|-------------|-----------|--------------------|
| Existing AppHost topology is sufficient for `ticketsdb` | C1, C2 | accepted | Resource already exists and is injected | No new database resource |
| Tickets API must validate customer IDs through Customers API | C10-C16 | accepted | Explicit requirement and ownership boundary | Add `ticketsapi -> customersapi` reference/wait |
| Direct cross-database FK/shared persistence model | C10-C13 | rejected | Violates service ownership | Store only `CustomerId` |
| Feature-oriented API/EF/Blazor/test vertical slice | C3-C9, C17-C20 | accepted | Matches established customer pattern | Coordinates existing project surfaces |
| Exact status/priority/assignee public contract | C14, C20; D1-D3 | accepted | User selected stable values and nullable display-string assignee | Planning can use a fixed contract |

* Another complete three-wave cycle needed: no; the answers confirm the existing architecture and introduce no new source area or material contradiction.
* Trigger or stop basis: all repository evidence is saturated and the only material contract gap was resolved without changing scope.
* Readiness or revalidation effect: Planning Readiness moved to Ready.

### Evidence Log

* Helpers: none. `dotnet-inspect` was used directly; its package metadata corroborated the pinned Aspire/EF dependency family, while the exact extension method was already evidenced by the customer API source.

| ID | Claim or finding | Source or location | Retrieved and version | Tool | Confidence | Notes |
|----|------------------|--------------------|-----------------------|------|------------|-------|
| C1 | AppHost creates `ticketsdb` and `customersdb` from the same PostgreSQL resource | `src/SupportHub.AppHost/AppHost.cs` — infrastructure declarations | not applicable | read | high | Exact resource names are source of truth |
| C2 | Tickets API receives `ticketsdb`, Blob Storage, and Service Bus references and waits for them | `src/SupportHub.AppHost/AppHost.cs` — Tickets API registration | not applicable | read | high | Existing graph includes later-layer resources |
| C3 | Tickets API is currently only `/hello`, health, OpenAPI, and ServiceDefaults | `src/SupportHub.Tickets.Api/Program.cs` — top-level pipeline | not applicable | read | high | No domain behavior exists |
| C4 | Ticket client is registered with service discovery but has no operations | `src/SupportHub.Web/Program.cs` — TicketApiClient registration; `src/SupportHub.Web/TicketApiClient.cs` | not applicable | read | high | Base address is `https+http://ticketsapi` |
| C5 | Ticket UI routes/components are absent; customer pages establish loading/error/edit patterns | `src/SupportHub.Web/Components/Pages` — Customers, CustomerDetail, CustomerEdit | not applicable | read | high | Reuse presentation conventions |
| C6 | Customers API exposes CRUD and `GET /customers/{id}` with explicit records and Problem Details validation | `src/SupportHub.Customers.Api/Features/Customers/CustomerEndpoints.cs` and `CustomerContracts.cs` | not applicable | read | high | Suitable write-time validation contract |
| C7 | Customer persistence uses service-owned EF context and string enum mapping | `src/SupportHub.Customers.Api/Features/Customers/CustomerDbContext.cs` | not applicable | read | high | Pattern to mirror for tickets |
| C8 | Customer migrations are service-local and startup application is gated by `SUPPORTHUB_APPLY_MIGRATIONS` | `src/SupportHub.Customers.Api/Program.cs` and `Migrations/` | not applicable | read | high | Tickets needs equivalent controlled gate |
| C9 | Current ticket tests are greeting/health smoke tests plus empty unit placeholder | `tests/SupportHub.AppHost.Tests/TicketsApiTests.cs`; `tests/SupportHub.Tickets.Api.Tests/UnitTest1.cs` | not applicable | read | high | Coverage gap is explicit |
| C10 | Repository forbids direct customer database access from other services | `.github/copilot-instructions.md` — Customers API ownership and architectural boundaries | not applicable | read | high | Constraint, not merely preference |
| C11 | APIs should use Minimal APIs, explicit contracts, async I/O, Problem Details, and cancellation | `.github/copilot-instructions.md` — API implementation conventions | not applicable | read | high | Governs proposed endpoint design |
| C12 | Web communicates through typed clients and must not access databases | `.github/copilot-instructions.md` — frontend conventions | not applicable | read | high | Governs Blazor design |
| C13 | Aspire integration tests start the AppHost, wait for health, and use `CreateHttpClient(resource)` | `.github/copilot-instructions.md` — integration tests; `tests/SupportHub.AppHost.Tests/CustomerApiTests.cs` | not applicable | read | high | Governs test strategy |
| C14 | Console sample uses `High`, `Medium`, and `Low` priority, but no formal enum exists | `src/SupportHub.Sla.Console/Program.cs` — `GenerateSampleTickets` | not applicable | read | medium | Suggestive only; not binding |
| C15 | ServiceDefaults enables service discovery and standard HTTP resilience | `src/SupportHub.ServiceDefaults/Extensions.cs` — `AddServiceDefaults` | not applicable | read | high | Supports Tickets-to-Customers HttpClient |
| C16 | Existing worker/console clients use `https+http://customersapi` and `https+http://ticketsapi` | `src/SupportHub.Sla.Worker/Program.cs`; `src/SupportHub.Sla.Console/Program.cs` | not applicable | read | high | Prior art for internal service clients |
| C17 | Web customer pages use explicit list/detail/create/edit/delete states | `src/SupportHub.Web/Components/Pages/Customers.razor`, `CustomerDetail.razor`, `CustomerEdit.razor` | not applicable | read | high | UI workflow pattern |
| C18 | AppHost tests use ephemeral test mode and gated migrations for customer behavior | `tests/SupportHub.AppHost.Tests/CustomerApiTests.cs` — `ConfigureTestEnvironment` | not applicable | read | high | Tickets should follow the same setup |
| C19 | Aspire and EF package family is pinned to Aspire 13.6.1 and EF Design 10.0.12 | `Directory.Packages.props` — central package versions; `src/SupportHub.AppHost/SupportHub.AppHost.csproj` | not applicable | read/dotnet-inspect | high | Version-specific infrastructure evidence |
| C20 | Current branch has no source delta from parent `feature/customers` | Git branch/history metadata; `git diff --stat feature/customers...feature/tickets` | not applicable | bash | high | Ticket work is unstarted, not partially implemented |

#### Contradictions and Conflicts

* Priority examples (`High`, `Medium`, `Low`) suggest a vocabulary, while README language mentions urgent tickets; resolved as an input request rather than silently selecting `Urgent`.
* Repository instructions describe ticket capabilities as intended architecture, while source shows the API is still a shell; source state takes precedence for implementation readiness.

### Artifact Self-Check

* [x] User-facing sections explain result, scope, findings, alternatives, decisions, risks, readiness, and next action without requiring the Research Record.
* [x] Every question is answered or names the smallest missing evidence.
* [x] Findings keep explanation, evidence state, confidence basis, and supporting detail together.
* [x] Every material codebase claim has a stable `C#` ID and workspace-relative locator.
* [x] The executed cycle records Wider, Deeper, and Contrarian waves in order, with reflections, synthesis, and re-entry decision.
* [x] Method, extensions, participation, direction, helper use, and prior-knowledge treatment are recorded.
* [x] Alternatives preserve the decision state without forcing convergence in research-only mode.
* [x] Decision groups, participation mode, blockers, and readiness are recorded; user-owned D1-D3 remain explicitly pending.
* [x] Research-only write boundary held; no source files or pull requests were changed; no secrets were recorded.
* Checked sections: all required template sections.
* Missing or limited sections: none; deployment migration policy and cross-service customer deletion remain explicitly downstream.
