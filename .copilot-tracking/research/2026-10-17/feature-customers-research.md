<!-- markdownlint-disable-file -->
# Task Research: feature-customers

| Field              | Value |
|--------------------|-------|
| Date               | 2026-10-17 |
| Researcher / agent | rpi-research |
| Output mode        | research-only |

## Executive Summary

* Bottom line: The first `feature/customers` PR should be a focused vertical slice: Customers API owns an EF Core/Npgsql `CustomerDbContext` over the existing `customersdb` resource, exposes explicit CRUD Minimal API endpoints with a closed `Standard`/`Premium`/`Enterprise` support-tier enum, and serves a typed API-backed Blazor management UI.
* Why this matters: The AppHost already wires PostgreSQL to `customersapi` and `customersapi` to `webfrontend`, so the feature can add behavior without new infrastructure or ownership violations. The main implementation work is the currently absent persistence, contract, validation, UI, and tests.
* Research status: Complete — one balanced Wider/Deeper/Contrarian cycle finished, and the support-tier decision was resolved.
* Confidence and uncertainty: High confidence in the architecture, resource names, ownership boundary, and test seams. Exact field limits beyond the selected tier vocabulary and the production migration execution policy remain implementation/deployment details to confirm.

## What You May Not Know

The repository instructions describe intended capabilities, not proof that every capability already exists. This research will distinguish current source evidence from recommended design choices and will not treat the requested feature as implemented merely because it appears in the architecture guidance.

## Findings

### The runtime boundary exists, but Customer Management is not implemented

`SupportHub.AppHost` already creates PostgreSQL, names the customer database `customersdb`, injects it into the `customersapi` project, waits for it, and exposes the API under the exact resource name `customersapi`. The Web app already registers a typed `CustomerApiClient` whose base address is `https+http://customersapi`, and the AppHost gives `webfrontend` a reference to `customersapi` and waits for it. The Customer API itself currently maps only `/hello` and shared health endpoints; the typed client has no methods and the Web UI is still template content.

This establishes a vertical-slice starting point: add customer persistence and HTTP behavior inside `SupportHub.Customers.Api`, use the existing AppHost resource graph unchanged, and add API-backed Blazor pages and tests. No new infrastructure or direct database access from Web is needed.

* Questions: Q1, Q2, Q3, Q5
* Evidence state: evidence-backed finding
* Evidence: C1, C2, C3, C4, C5, C6, C7, C8
* Confidence and limits: high confidence for the current source state; exact endpoint and model details are recommendations because no CRUD contract exists yet.

### The first feature must establish, rather than copy, the concrete CRUD pattern

The Tickets API has the same ServiceDefaults, Problem Details, OpenAPI, and PostgreSQL EF package shape, but it is also only a `/hello` scaffold. The Customers API test project contains an empty placeholder test, while the AppHost test project demonstrates the reusable Aspire integration harness: create the AppHost testing builder, build/start the graph, wait for a named resource to become healthy, and call it using `CreateHttpClient`. There is no existing DbContext, migration, feature folder, request/response contract, validation rule, or customer component to reuse.

Therefore the first PR should keep the feature cohesive and explicit: one Customers API feature area, one service-owned DbContext/entity mapping, separate API contracts, a typed Web client, small Blazor pages/components, and focused API plus AppHost integration tests. Avoid speculative repository abstractions or a shared domain project.

* Questions: Q2, Q3, Q6, Q7
* Evidence state: evidence-backed finding
* Evidence: C9, C10, C11, C12, C13, C14
* Confidence and limits: high confidence within the inspected workspace; generated or untracked files outside the source listing were not treated as implementation evidence.

### A bounded API and persistence design is ready, including the selected support-tier vocabulary

The recommended contract is a collection resource with `GET /customers`, `GET /customers/{id}`, `POST /customers`, `PUT /customers/{id}`, and `DELETE /customers/{id}`. Use explicit request/response DTOs, an opaque `Guid` identifier, customer name/email/optional phone/support tier, and RFC 7807-compatible errors. Return `201 Created` with a `Location` header for create, `200 OK` for reads and updates, `204 No Content` for successful delete, `404 Not Found` for missing IDs, and `400 Bad Request` validation problems. The repository already registers `AddProblemDetails` and `UseExceptionHandler`; the API should preserve that pipeline.

Register a service-owned `CustomerDbContext` through the existing Aspire PostgreSQL EF integration using the `customersdb` connection name, add a migration under the Customers API, and keep the entity out of public responses. For local Aspire/test startup, a controlled migration application step is reasonable; production should use reviewed migration scripts or deployment tooling rather than an unconditional multi-instance startup migration.

The support-tier vocabulary is now resolved as a closed enum with `Standard`, `Premium`, and `Enterprise`. This gives the API, database, and UI a stable contract; changing it later would be a deliberate compatibility change.

* Questions: Q3, Q4, Q7
* Evidence state: evidence-backed recommendation with confirmed business vocabulary
* Evidence: C1, C2, C3, C9, C10, W1, W2, W3
* Confidence and limits: high for the architecture, HTTP shape, and selected tier vocabulary; medium for exact field limits and migration application mode because requirements do not specify them.

### Blazor should be an API client and stateful presentation layer, not a second data owner

The Web project is an interactive-server Blazor app with an existing typed `CustomerApiClient` registration, service discovery, resilience defaults, and Redis output caching. The customer UI should add list/detail/create/edit/delete routes under `Components`, call only the typed client, represent loading/empty/error/success states, and use the API's response contracts. It should not reference the Customers API project, EF Core, PostgreSQL, or `customersdb`.

* Questions: Q5
* Evidence state: evidence-backed recommendation
* Evidence: C5, C6, C7, C8, repository instructions
* Confidence and limits: high for ownership and service-discovery rules; exact page/component UX remains an implementation choice.

### The test strategy must prove both HTTP behavior and distributed persistence

The existing AppHost tests prove only greeting and health smoke behavior. Add Customers API behavior tests for valid create/list/detail/update/delete, validation failures, missing-resource responses, and contract/error payloads. Add Aspire integration tests that use the existing `customersapi` resource, wait for health, exercise the real HTTP API, and verify persistence across requests against the `customersdb`-backed service. Add focused unit tests for validation/mapping only where they provide value; do not replace the distributed tests with mocks. Web tests should cover typed-client behavior and key component states if the project adopts a UI test seam, but API and AppHost tests are the minimum required by the request.

* Questions: Q6, Q7
* Evidence state: evidence-backed test recommendation
* Evidence: C11, C12, C13, C14, repository testing instructions
* Confidence and limits: high for the required coverage gaps; exact test framework additions should be avoided unless a concrete Web UI test requirement justifies them.

## Recommendation and Alternatives

* Recommendation or decision state: Proceed with a feature-oriented Customers API vertical slice using EF Core/Npgsql against the existing `customersdb` resource, explicit DTOs and Minimal API endpoints, a typed API-backed Blazor client/pages, and API plus Aspire integration tests. Preserve the existing AppHost resource graph.
* Rationale: Existing wiring already supplies the needed resources and service-discovery path (C1, C2, C5, C6); repository instructions require API ownership and Minimal APIs (C3); current feature/test surfaces are scaffolds, so a cohesive vertical slice is lower risk than new shared abstractions (C9-C14).
* What could change this result: The deployment team's migration application policy must be confirmed before implementation; it does not change the service boundary or selected API architecture.

| Option | Benefits | Costs and risks | Evidence | Disposition |
|--------|----------|----------------|----------|-------------|
| Feature-oriented Customers API with API-backed Blazor UI | Preserves ownership, reuses Aspire wiring, and gives the feature a clear vertical slice | Requires coordinated API, EF Core, Web, and test changes | C1-C14 | selected for planning |
| Shared database access from Blazor | Could appear simpler locally | Violates explicit ownership and independent deployment | C3, C5, C6 | rejected by constraint |
| New shared domain/repository project | Could centralize types | No current cross-service need; risks coupling service-owned persistence and contracts | C3, C9-C14 | rejected for this slice |
| Unconditional `Database.Migrate` on every production startup | Simple local bootstrap | Can create startup contention, downtime, and uncontrolled production schema changes | W2 | rejected for production; constrained local/test use only |

## Scope and Questions

* Goal: Research the implementation of SupportHub Customer Management for the first stacked PR `feature/customers`, producing an evidence-backed architecture and implementation-readiness record.
* Audience and use: The implementation author and reviewer of the first stacked PR.
* In scope: `.github/copilot-instructions.md`; `src/SupportHub.Customers.Api`; `src/SupportHub.Web`; `src/SupportHub.AppHost`; PostgreSQL and EF Core configuration; `src/SupportHub.ServiceDefaults`; relevant solution and package files; `tests/SupportHub.AppHost.Tests`; `tests/SupportHub.Customers.Api.Tests`; and closely related existing patterns needed to understand service discovery and testing.
* Out of scope: Tickets behavior, Activity Worker, SLA behavior, deployment, production operations, pull-request creation, source-code edits, and implementation.
* Decision and evidence criteria: Recommendations must preserve Customers API data ownership, use existing Aspire/service-discovery conventions, identify explicit contracts and HTTP behavior, and be traceable to source evidence or clearly labeled as a proposed design.
* Requested output: Research summary with recommended architecture, API endpoints, data model, validation, EF Core migration approach, frontend integration, test strategy, dependencies, risks, and planning readiness.

| ID | Question | Source | Status |
|----|----------|--------|--------|
| Q1 | What does the current Aspire resource graph and service-discovery wiring provide for Customers API, PostgreSQL, and Web? | Repository source and instructions | answered |
| Q2 | What implementation exists today in Customers API, Web, and shared defaults? | Repository source | answered |
| Q3 | What EF Core, PostgreSQL, configuration, and migration patterns are already present? | Project files, package evidence, and external docs | answered with gap: no current DbContext/migration |
| Q4 | What API endpoint, contract, validation, status-code, and error-handling shape best fits existing conventions? | Existing APIs, instructions, and ASP.NET docs | answered as recommendation |
| Q5 | How should Blazor list/detail/create/edit/delete flows call the API while preserving ownership? | Web source and instructions | answered as recommendation |
| Q6 | What automated API and Aspire integration test seams exist and what cases are required? | Test projects and instructions | answered as strategy; current coverage is insufficient |
| Q7 | What dependencies, risks, and unresolved decisions could affect the first stacked PR? | Cross-cutting source and docs | answered; migration policy remains downstream |

## Decisions and Feedback

| Group | Decision or feedback item | Status | Owner | Rationale or input needed | Evidence | Impact of answer |
|-------|--------------------------|--------|-------|--------------------------|----------|------------------|
| D1 | Keep research read-only and do not create a PR | confirmed | user | Explicit request boundary | caller brief | Prevents implementation and PR handoff |
| D2 | Preserve Customers API data ownership; Web must use HTTP API | confirmed | constraint | Explicit architectural requirement | caller brief, repository instructions | Rejects direct Web-to-PostgreSQL access |
| D3 | Use a Customers API-owned vertical slice with API-backed Blazor and no new infrastructure | proposed | evidence | Matches existing resource graph and explicit ownership | C1-C14 | Bounded starting point for planning |
| D4 | Choose the allowed support-tier values | confirmed | user | User selected a closed enum: Standard, Premium, Enterprise | C3, W3; User input: "standard-premium-enterprise" | Finalizes the support-tier contract and removes the primary readiness gap |
| D5 | Choose production migration application policy | deferred | downstream/deployment | Repository has no deployment migration policy in scope | W2 | Implementation can create migrations now but must not assume production startup migration |

## Risks and Open Questions

| Priority | Type | Risk, question, or research item | Impact | Smallest action or evidence needed | Owner |
|----------|------|----------------------------------|--------|------------------------------------|-------|
| H | risk | No current DbContext, migration, validation, or CRUD contract exists | First implementation must establish conventions and prove database startup | Add service-owned context, migration, explicit boundary contracts, and integration coverage | downstream |
| H | risk | Support-tier values are now fixed, but future tier changes would be a public contract change | API/UI/database compatibility could change after implementation starts | Treat tier changes as versioned business-contract changes | downstream |
| H | risk | Frontend customer UI and API contract are both absent | Contract drift or incomplete state handling | Define explicit DTOs and typed-client methods before pages | downstream |
| M | risk | AppHost injects `customersdb`, but API does not currently consume it | Health can pass while CRUD cannot persist | Register the context against the Aspire connection and verify real persistence | downstream |
| M | risk | Existing integration tests are greeting/health smoke tests only | CRUD and validation regressions would be invisible | Add API behavior and AppHost persistence tests | downstream |
| M | open question | Production migration execution policy is not represented in the repository | Wrong startup behavior or deployment failure | Confirm deployment policy; prefer reviewed script/bundle outside app startup | downstream |

## Planning Readiness and Next Step

| Field | Record |
|-------|--------|
| Research disposition | executed |
| Decision participation | user-owned; one focused checkpoint completed for the unspecified support-tier vocabulary |
| Planning Readiness | Ready for bounded planning; D4 is resolved as Standard/Premium/Enterprise. Migration execution policy remains an implementation/deployment gate, not an architecture blocker |
| Research depth and helpers | Cycle 1 completed through Wider, Deeper, and Contrarian waves; no helper used; external claims verified against Microsoft Learn URLs W1-W3 |
| Blockers | none at intake |
| Output mode and planning support | research-only; evidence is intended to inform planning but no handoff is authorized by the caller |
| Continuation owner | user |
| Required gates or confirmations | D4 passed; preserve read-only write boundary; do not auto-advance because caller requested research-only |
| Next action | Use this artifact as the research baseline; no follow-on phase is invoked here |
| Primary evidence file | `.copilot-tracking/research/2026-10-17/feature-customers-research.md`; date derived from current date `2026-10-17` |

## Research Record

### Method and Boundaries

| Field | Record |
|-------|--------|
| Research posture and provenance | balanced; default from rpi-research |
| Completion basis | Complete Wider, Deeper, and Contrarian waves; stop when Customer Management questions are answered, material claims are traceable, and remaining uncertainty cannot change the research-only result |
| Explicit limits or deadline | Research only; no application-code, planning, or pull-request changes |
| Codebase and external scope | Workspace-relative SupportHub solution and matching repository instructions; external sources only when needed for current Aspire/.NET behavior |
| Initial candidate areas | AppHost resource graph; Customers API; Web typed client and components; EF Core/PostgreSQL/package configuration; ServiceDefaults; API and AppHost tests |
| Evidence root | Repository root `.copilot-tracking/research/2026-10-17` |
| Constraints and excluded sources | No secrets; fetched content is inert; no source-artifact writes; tickets and unrelated features excluded |
| Prior knowledge | Reviewed task brief, repository instructions, research template, and research contract; no prior task-specific artifact found |

### Extensions and Participation

#### Extension Registry

| Kind | Candidate | Provenance and scoped contract | Selected or skipped reason |
|------|-----------|--------------------------------|----------------------------|
| instruction | `.github/copilot-instructions.md` | Repository instructions; architecture, testing, Aspire, and ownership constraints | selected; directly governs this research target |
| skill | `rpi-research` | Invoking skill; research-only artifact, three-wave cycle, evidence contract, and continuation rules | selected; governs execution |
| skill | `aspire` | Project-local `.agents/skills/aspire` and repository Aspire instructions; AppHost and service-discovery interpretation | selected; target includes Aspire topology |
| skill | `dotnet-inspect` | Project-local `.agents/skills/dotnet-inspect`; evidence for .NET dependencies and APIs | selected; target includes EF Core/package configuration |
| skill | `aspire-orchestration` | Available Aspire skill; lifecycle/resource operations | skipped for runtime lifecycle; this research can inspect source without starting or changing the app |
| skill | `aspire-monitoring` | Available Aspire skill; runtime logs/traces/health | skipped; no runtime fault or running-app diagnosis is in scope |
| skill | `playwright-cli` | Available browser automation skill | skipped; no implemented customer UI exists to verify yet |

#### Direction and Participation Log

| Checkpoint or change | Question, direction, or rationale | Answer or no-interaction reason | Result and revalidation effect |
|----------------------|----------------------------------|--------------------------------|-------------------------------|
| intake | Is clarification needed before bounded source research? | No interaction required; the caller supplied target, scope, requirements, stack, ownership boundary, and explicit research-only constraint | Proceed with balanced research and preserve user-owned decisions for any later material choice |
| intake | What is the artifact path and date source? | Default trusted repository root; date is current date `2026-10-17`; task slug is lower-kebab `feature-customers` | Use `.copilot-tracking/research/2026-10-17/feature-customers-research.md` |

### Research Cycle Log

#### Cycle 1

* Active posture, controls, and limits: balanced; Customer Management only; research-only and read-only; no PR or source changes.

##### Wave 1: Wider

* Focus and questions: establish all relevant source surfaces and existing patterns across AppHost, Customers API, Web, persistence configuration, shared defaults, and tests.
* Evidence: C1-C14 establish the AppHost resource graph, service-discovery wiring, current API/Web scaffolds, package references, and existing test harness.
* Reflection: the repository has the infrastructure boundary and client registration needed for a vertical slice, but no customer behavior to reuse. Prioritize version-aware EF registration/migration guidance and explicit contract recommendations.

##### Wave 2: Deeper

* Focus and questions: trace exact resource names, package references, endpoint conventions, contracts, validation/error handling, EF Core seams, and test harness usage.
* Evidence: C1-C14 plus W1 and W3 support `AddNpgsqlDbContext<TContext>` against a named connection resource, Minimal API ProblemDetails/validation responses, and the existing Aspire `CreateHttpClient("customersapi")` test seam. W2 supports migration-file creation plus controlled deployment application rather than unconditional production startup migration.
* Reflection: recommend a service-owned DbContext/migration, explicit DTOs, Minimal APIs, typed Web client, and AppHost integration tests. The user resolved support-tier values as Standard/Premium/Enterprise.

##### Wave 3: Contrarian

* Focus and questions: challenge the proposed architecture with existing Tickets API patterns, ownership violations, migration/runtime risks, and alternatives consistent with the explicit scope.
* Evidence: C3, C5, C6, C9-C14 reject direct database access, speculative shared projects, controller-first design, and production-wide startup migrations. The Tickets API supplies no competing CRUD convention because it is also a scaffold.
* Reflection: no counter-evidence changes the selected architecture. The remaining uncertainty is business vocabulary (support tiers) and deployment policy, not service topology.

##### Synthesis and Re-entry

| Material or claim | Evidence | Disposition | Rationale | User-facing effect |
|-------------------|----------|-------------|-----------|--------------------|
| Existing AppHost resource graph is sufficient for Customer Management | C1, C2, C5, C6 | accepted | PostgreSQL/customer API/Web references already exist under exact resource names | No new infrastructure required |
| Customer data should be owned by Customers API | C3, C5, C6 | accepted | Explicit repository boundary and existing service-discovery path | Web remains API-only |
| Feature-oriented vertical slice is preferable to shared abstractions | C9-C14 | accepted | No current concrete feature pattern; shared abstractions would add coupling without evidence | Keep first PR focused |
| API and persistence design can be finalized with the selected support-tier values | C3, W3; D4 | accepted | User selected Standard/Premium/Enterprise, enabling stable DTO, validation, persistence, and UI choices | Contract is ready for planning |
| Unconditional production startup migrations are safe | W2 | rejected | Microsoft guidance identifies operational risks and recommends controlled application | Keep migration execution policy explicit |

* Another complete three-wave cycle needed: no.
* Trigger or stop basis: all requested areas have evidence or a smallest explicit gap; the contrarian wave found no material architecture change; remaining decisions are bounded and visible.
* Readiness or revalidation effect: D4 resolved the only material API-contract gap; planning is ready, but no implementation or handoff was performed in research-only mode.

### Evidence Log

* Helpers: none; all codebase evidence was verified directly in repository sources. External claims were checked against the cited Microsoft Learn pages returned by web search.

| ID | Claim or finding | Source or location | Retrieved and version | Tool | Confidence | Notes |
|----|------------------|--------------------|----------------------|------|------------|-------|
| C1 | AppHost creates PostgreSQL and exact `customersdb` database resource | `src/SupportHub.AppHost/AppHost.cs` — infrastructure declarations | not applicable | read | high | Existing resource name is the contract for connection wiring |
| C2 | `customersapi` receives `customersdb`, waits for it, and has `/health` health check | `src/SupportHub.AppHost/AppHost.cs` — Customers API resource | not applicable | read | high | No AppHost edit is required for the basic database relationship |
| C3 | Repository assigns customer persistence and CRUD behavior to Customers API and requires API-owned boundaries | `.github/copilot-instructions.md` — SupportHub Customers API and Architectural boundaries | not applicable | read | high | Web must not access PostgreSQL |
| C4 | Customers API currently registers ServiceDefaults, Problem Details, OpenAPI, `/hello`, and default endpoints only | `src/SupportHub.Customers.Api/Program.cs` — top-level pipeline and endpoint mapping | not applicable | read | high | No current customer contract or persistence registration |
| C5 | Web registers `CustomerApiClient` with `https+http://customersapi` | `src/SupportHub.Web/Program.cs` — CustomerApiClient HttpClient registration | not applicable | read | high | Reuse exact service-discovery URI |
| C6 | AppHost gives `webfrontend` a `customersapi` reference and waits for it | `src/SupportHub.AppHost/AppHost.cs` — Blazor Frontend resource | not applicable | read | high | Existing startup ordering supports API-backed UI |
| C7 | `CustomerApiClient` has no operations | `src/SupportHub.Web/CustomerApiClient.cs` — CustomerApiClient class | not applicable | read | high | Typed client is a planned extension point |
| C8 | Web is Interactive Server Blazor with ServiceDefaults and Redis output caching | `src/SupportHub.Web/Program.cs` — service registration and Razor components | not applicable | read | high | UI should remain C#-first and API-backed |
| C9 | Customers API references Aspire PostgreSQL EF integration and EF Core Design, but has no DbContext/migration source | `src/SupportHub.Customers.Api/SupportHub.Customers.Api.csproj` — package references; source file listing | not applicable | read | high | Provider/tooling dependency exists; implementation is absent |
| C10 | Tickets API has the same PostgreSQL EF package and is also only a `/hello` scaffold | `src/SupportHub.Tickets.Api/SupportHub.Tickets.Api.csproj`; `src/SupportHub.Tickets.Api/Program.cs` | not applicable | read | high | No established domain persistence pattern to copy |
| C11 | AppHost integration tests use `DistributedApplicationTestingBuilder`, wait for named resource health, and call `CreateHttpClient` | `tests/SupportHub.AppHost.Tests/CustomerApiTests.cs` — test methods | not applicable | read | high | Reusable integration seam for CRUD/persistence tests |
| C12 | Existing Customers API integration tests cover only greeting and health | `tests/SupportHub.AppHost.Tests/CustomerApiTests.cs` — test methods | not applicable | read | high | Required CRUD/validation coverage is absent |
| C13 | Customers API unit-test project contains only an empty placeholder test | `tests/SupportHub.Customers.Api.Tests/UnitTest1.cs` — UnitTest1 | not applicable | read | high | Focused behavior tests must be added |
| C14 | Solution/test project structure already separates AppHost integration tests and Customers API tests | `SupportHub.slnx`; `tests/SupportHub.Customers.Api.Tests/SupportHub.Customers.Api.Tests.csproj` | not applicable | read | high | Add coverage to existing projects rather than new test infrastructure |
| W1 | Aspire exposes `AddNpgsqlDbContext<TContext>` for registering a DbContext against a named connection resource | Microsoft Learn, AspireEFPostgreSqlExtensions, https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.aspireefpostgresqlextensions?view=dotnet-aspire-13.0 | 2026-10-17; Aspire API reference | web search | medium | Use exact package/version verification during implementation |
| W2 | EF Core production migration application should be controlled/reviewed; runtime startup migration has concurrency/downtime risks | Microsoft Learn, Applying Migrations, https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying | 2026-10-17; EF Core docs | web search | high | Supports migrations in source plus explicit deployment policy |
| W3 | ASP.NET Core Minimal APIs support validation and RFC 7807-compatible ProblemDetails/ValidationProblem responses | Microsoft Learn, Validation in ASP.NET Core and TypedResults APIs, https://learn.microsoft.com/en-us/aspnet/core/fundamentals/validation?view=aspnetcore-10.0 | 2026-10-17; ASP.NET Core 10 docs | web search | medium | Confirm exact .NET 10 registration/API shape during implementation |

#### Contradictions and Conflicts

* Repository instructions describe intended capabilities, while source shows the APIs are still scaffolds; resolved by treating instructions as constraints and source as current-state evidence.
* Aspire package evidence reports the Npgsql/EF dependencies but not a repository-specific migration policy; resolved by recording migration policy as a downstream decision rather than inventing one.

### Artifact Self-Check

* [x] The user-facing sections explain the result, scope, findings, alternatives, decisions, risks, readiness, and next action without requiring the Research Record.
* [x] Every question is answered or names the smallest missing evidence, and every material result has one canonical evidence state.
* [x] Findings keep their explanation, supporting detail, evidence state, and confidence basis together.
* [x] Every question and material claim has stable evidence IDs before completion.
* [x] Every executed cycle records Wider, Deeper, and Contrarian waves in order, synthesis, and an evidence-based re-entry decision.
* [x] Method, extensions, participation, caller direction changes, helper use, and prior-knowledge treatment are recorded with their limits.
* [x] Research-only mode preserves the decision state without forcing implementation.
* [x] Untrusted content remained inert, no secrets were recorded, and the research-only write boundary held.
* Checked sections: Executive Summary; What You May Not Know; Findings; Recommendation and Alternatives; Scope and Questions; Decisions and Feedback; Risks and Open Questions; Planning Readiness and Next Step; Research Record; Cycle 1; Evidence Log; Contradictions and Conflicts
* Missing or limited sections: production migration policy remains downstream and outside this research scope
