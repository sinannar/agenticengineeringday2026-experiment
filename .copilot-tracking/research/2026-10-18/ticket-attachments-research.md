<!-- markdownlint-disable-file -->

# Task Research: ticket-attachments

| Field              | Value |
|--------------------|-------|
| Date               | 2026-10-18 |
| Researcher / agent | rpi-research |
| Output mode        | convergence |

## Executive Summary

* Bottom line: Implement attachments inside `SupportHub.Tickets.Api` as ticket-owned metadata in `ticketsdb` plus opaque, ticket-scoped blobs in the existing Azurite-backed `blobs` resource. Use explicit multipart endpoints, server-side size/type/name validation, route-scoped ticket/attachment checks, and a compensating cleanup strategy for the unavoidable PostgreSQL/Blob Storage split.
* Why this matters: The repository already has the required Aspire storage resource, Tickets API client registration, ticket-detail UI, EF migration path, and Aspire integration-test harness. The feature is a bounded vertical slice, not new infrastructure or a new service.
* Research status: Complete — Wider, Deeper, and Contrarian waves completed and synthesized.
* Confidence and uncertainty: High confidence in ownership, wiring, API shape, persistence boundary, and test approach. Exact maximum size, allowlisted types, and future identity-based authorization values are not specified by the repository and should be fixed as explicit feature policy during planning.

## What You May Not Know

“Access” cannot currently mean user/role authorization: the repository has no authentication or identity model. The implementable boundary for this stack layer is that callers can only address an existing ticket and an attachment whose metadata belongs to that ticket; blobs are never exposed through user-controlled names or direct public URLs. A later identity layer can add authorization without changing the storage ownership model.

The current `feature/attachments` branch points at the same commit as `feature/tickets`; the attachment layer is therefore entirely unimplemented, while its infrastructure placeholders are already present.

## Findings

### The existing AppHost already supplies the correct storage boundary

`SupportHub.AppHost` creates Azure Storage named `storage`, adds the blob service resource named `blobs`, runs it as Azurite locally, gives `ticketsapi` a reference to `blobs`, and waits for it before starting the Tickets API. `SupportHub.Tickets.Api` already calls `AddAzureBlobServiceClient("blobs")`, and its project already references `Aspire.Azure.Storage.Blobs` 13.6.1. No new resource or cross-service storage path is needed.

The implementation should preserve these exact names and add only attachment behavior. The AppHost remains orchestration-only; blob operations belong in the Tickets API, which owns ticket data and attachment metadata.

* Questions: Q1, Q2, Q8
* Evidence state: evidence-backed finding
* Evidence: C1, C2, C3, C4, C5, W1, W2
* Confidence and limits: High. The external Aspire pages describe the same `AddAzureStorage`/`AddBlobs`/`RunAsEmulator` and `AddAzureBlobServiceClient` patterns; exact package APIs remain pinned by the repository.

### Attachment metadata belongs in the Tickets API database, separate from blob content

The current `TicketDbContext` contains only `Ticket`, and the initial migration has no attachment table. Add a service-owned `Attachment` entity and migration in `ticketsdb`; do not put bytes or a customer database reference in it. A practical metadata shape is:

```text
AttachmentId       Guid primary key
TicketId           Guid required indexed reference by convention
BlobName           opaque generated storage key, required and unique
OriginalFileName   display-only sanitized name
ContentType        validated media type
Size               server-observed byte count
CreatedAt          UTC timestamp
```

The public response should expose the attachment ID, display name, content type, size, and timestamp, but not the connection string, container internals, or a user-supplied path. `BlobName` should be generated from IDs, for example `tickets/{ticketId}/{attachmentId}`, never from the original filename. The existing database has no cross-service foreign keys; attachment metadata can use a local ticket relationship or explicit ticket lookup, but delete behavior must account for blob cleanup.

* Questions: Q1, Q3, Q6
* Evidence state: evidence-backed finding
* Evidence: C5, C6, C7, C12, W3, W5
* Confidence and limits: High for ownership and opaque naming. Whether the EF model uses a database FK or application-level ticket lookup is an implementation detail; either must preserve route ownership and migration clarity.

### A four-operation, ticket-scoped HTTP surface fits the current Minimal API conventions

Recommended endpoints:

```text
GET    /tickets/{ticketId}/attachments
POST   /tickets/{ticketId}/attachments        multipart/form-data, one file
GET    /tickets/{ticketId}/attachments/{attachmentId}/content
DELETE /tickets/{ticketId}/attachments/{attachmentId}
```

Every operation first verifies the ticket exists and then resolves the attachment through both `ticketId` and `attachmentId`; an attachment ID from another ticket must behave as not found. Upload returns `201 Created` with the attachment metadata and a `Location` for the content endpoint. List returns deterministic metadata ordered by creation time and ID. Download returns a streamed response with the persisted content type and a safe `Content-Disposition` filename. Delete should be idempotent for a missing ticket/attachment according to the project’s selected API policy, with `204 No Content` for a successful delete.

The API should use Problem Details/validation errors for missing multipart data, unsupported type, over-limit size, invalid filename, missing ticket, missing attachment, and storage failures. It must pass `CancellationToken` through EF and blob operations.

* Questions: Q1, Q4, Q6
* Evidence state: evidence-backed recommendation
* Evidence: C6, C7, C8, C9, W3, W5
* Confidence and limits: High for route shape and status conventions; exact error type/shape should reuse the existing API’s `Results.ValidationProblem`/`Results.Problem` style.

### Upload ordering should prefer blob-first, then metadata, with compensating deletion

There is no transaction spanning PostgreSQL and Blob Storage. The recommended upload sequence is:

1. Validate the ticket, file size, allowlisted type, and filename before storage.
2. Generate the attachment ID and opaque blob name.
3. Stream the request file directly to Blob Storage with persisted content type; do not buffer the entire file in memory.
4. Insert metadata only after the blob upload succeeds.
5. If the metadata insert fails, attempt to delete the newly uploaded blob and surface the database failure explicitly.

This ordering prevents committed metadata from pointing to content that was never stored. It cannot eliminate an orphan if the compensating delete fails, so the implementation must log/report that failure and make orphan cleanup an explicit operational concern. A database-first alternative reduces one orphan shape but creates committed metadata with missing content when blob upload fails; that is worse for normal list/download behavior. A distributed transaction or outbox is disproportionate for this stack layer and would introduce infrastructure not required by the brief.

For delete, blob and metadata deletion are also non-atomic. The implementation should choose and document one order, make retries safe, and return an explicit storage problem rather than silently claiming success. A blob-first/delete-metadata-second policy preserves content until blob deletion succeeds but can leave a stale metadata row if the database delete fails; a metadata-first policy hides an orphan if blob deletion fails. The smaller feature should select one policy and add tests for both failure points rather than implying atomicity.

* Questions: Q3, Q6, Q8
* Evidence state: evidence-backed recommendation with known residual risk
* Evidence: C5, C6, C9, W3, W6
* Confidence and limits: High that compensation is required; the residual cleanup policy is an explicit implementation decision, not a solved transaction.

### Validation must be server-authoritative and must not trust filenames or client size claims

The feature needs one centralized attachment policy containing a maximum byte count and an allowlist of permitted media types/extensions. The API must enforce it independently of Blazor checks. It should reject empty files, missing content, unsupported or mismatched types, and requests beyond the configured maximum. The server should stream with the same maximum bound and use the actual bytes transferred as the persisted size; client-supplied `IBrowserFile.Size` and `Content-Type` are useful hints only.

The original filename is untrusted display data. Strip path components, reject NUL/control/path-separator input as appropriate, normalize the display value, and never use it as a blob key or filesystem path. The UI may render it because Razor encodes output, but the API must persist a safe display value and generate its own storage key. The external ASP.NET guidance also calls for approved extensions, size checks, server-side validation, and malware scanning; malware scanning is not available in this repository and should be recorded as a production risk rather than invented locally.

* Questions: Q3, Q4, Q6, Q8
* Evidence state: evidence-backed finding
* Evidence: C6, C7, W4, W5
* Confidence and limits: High for safety principles. Numeric limits, exact MIME/extension pairs, and whether content sniffing is required remain policy inputs.

### The Blazor ticket-detail page is the natural UI insertion point, with a same-origin download path

`TicketDetail.razor` already loads one ticket and shows its fields; `TicketApiClient` already owns all server calls and `Program.cs` configures it through `https+http://ticketsapi`. Add an attachment section to the detail page with:

* list/loading/empty/error states;
* `<InputFile>` selection constrained by the shared policy;
* upload state with selected filename, bytes sent/total where available, disabled controls, and a retryable error;
* download and delete actions;
* refresh after successful upload/delete.

Upload can use a typed client multipart request and a progress-reporting stream/content wrapper in C#. A direct browser link to `ticketsapi` is not safe because the browser does not use server-side Aspire service discovery. For download, the strongest repository-compatible design is a same-origin Web proxy endpoint that calls `TicketApiClient` with `ResponseHeadersRead` and returns the streamed response with safe headers; the attachment API remains the source of truth. This avoids adding JavaScript or exposing the internal Tickets API endpoint to the browser.

The Web layer must not access Blob Storage or `ticketsdb`; it only calls the Tickets API. The current UI has no authentication context, so it can display the API’s route-scoped access result but cannot add user authorization yet.

* Questions: Q4, Q5, Q6
* Evidence state: evidence-backed finding
* Evidence: C8, C9, C10, W4
* Confidence and limits: Medium-high. The insertion point and service boundary are clear; the exact same-origin proxy mechanics should be verified against the chosen ASP.NET response-stream API during planning.

### Integration tests can prove real Azurite behavior through the existing Aspire harness

`SupportHub.AppHost.Tests` already uses `DistributedApplicationTestingBuilder`, starts the actual AppHost, waits for named resources, and calls `app.CreateHttpClient("ticketsapi")`. The test project already references `Aspire.Hosting.Testing` 13.6.1 and the AppHost. Attachment tests should start the real graph, explicitly wait for `blobs` and `ticketsapi`, create a ticket through the API, then upload bytes through multipart HTTP and verify list, download bytes/content type, and delete behavior.

For direct cleanup/orphan assertions, Aspire’s resource-testing guidance supports retrieving a resource connection string and using the Azure Blob client in the test project; this should be pinned to the exact 13.6.1 API during implementation. API-level download assertions remain the required end-to-end proof even if direct blob inspection is deferred.

Focused unit tests belong in `SupportHub.Tickets.Api.Tests` for policy validation, filename normalization, blob-name generation, and error mapping. Aspire integration tests should cover happy-path lifecycle, ticket/attachment isolation, missing ticket/attachment, disallowed type, zero/over-limit content, upload failure compensation, missing blob on download, delete retry/failure behavior, and metadata persistence across requests.

* Questions: Q5, Q7, Q8
* Evidence state: evidence-backed finding
* Evidence: C9, C11, W6
* Confidence and limits: High for the harness and API-level scenarios; direct Azurite inspection needs one implementation-time API check.

### Acceptance criteria for SupportHub Ticket Attachments

1. An existing ticket can upload one permitted attachment through `POST /tickets/{ticketId}/attachments`; the response is `201 Created` and contains metadata but no blob URL or path supplied by the caller.
2. Attachments are listed through `GET /tickets/{ticketId}/attachments`, ordered deterministically, and each result remains associated with the correct ticket.
3. Attachment content downloads through the ticket-scoped content endpoint with the persisted content type, safe display filename, and streamed bytes matching the uploaded bytes.
4. Deleting an attachment removes its metadata and content according to the selected documented compensation policy; repeated cleanup is safe and storage failures are explicit.
5. Unknown tickets and attachments, cross-ticket attachment IDs, empty files, over-limit files, disallowed/mismatched types, missing multipart content, and unsafe filenames are rejected with structured errors and no committed metadata.
6. Blob names are generated from server-side identifiers and ticket scope; original filenames are never used as storage paths.
7. Blob content is stored in the existing Azurite-backed `blobs` resource; metadata is stored in the service-owned `ticketsdb` schema through an EF migration.
8. Failed metadata persistence triggers a compensating blob delete attempt; failed compensation is surfaced/logged as an orphan risk rather than silently reported as success.
9. The Blazor ticket detail page exposes list, upload, download, delete, progress, loading, empty, success, and retryable error states through typed C# API access, without direct database/blob access or new JavaScript.
10. Focused validation tests and Aspire integration tests exercise the lifecycle, validation, isolation, missing-content, failure-compensation, and local Azurite paths.

## Recommendation and Alternatives

* Recommendation or decision state: Proceed with a feature-oriented `TicketAttachments` implementation in `SupportHub.Tickets.Api`, using the existing `blobs` reference and `ticketsdb`, an `Attachment` metadata entity, ticket-scoped opaque blob names, multipart API endpoints, blob-first upload with compensation, and a Blazor detail-page experience backed by `TicketApiClient`. Add a same-origin Web download proxy only if the chosen browser-download mechanism cannot remain entirely server-side.
* Rationale: This preserves all documented ownership boundaries, reuses existing Aspire 13.6.1 wiring and Minimal API/EF/Blazor/test patterns, avoids unsafe user paths, and makes the unavoidable dual-store failure modes explicit (C3-C11, W1-W6).
* What could change this result: A new authentication/authorization requirement, a requirement for resumable or multi-file uploads, malware scanning infrastructure, or a measured need for an outbox/reconciliation worker would materially expand the design.

| Option | Benefits | Costs and risks | Evidence | Disposition |
|--------|----------|----------------|----------|-------------|
| Ticket API owns metadata; opaque blob names; blob-first upload with compensation | Fits ownership, prevents path traversal, avoids metadata pointing to absent content in the common failure case, and requires no new infrastructure | Orphans remain possible if compensation fails; cleanup policy must be explicit | C3-C11, W1-W6 | selected |
| Metadata-first then blob upload | Simple DB-first flow | Leaves committed rows pointing to missing content when storage fails; worsens normal list/download consistency | C6, C7, W3 | rejected |
| Store bytes in PostgreSQL | Single transactional store | Violates explicit Blob Storage requirement and increases database pressure | caller brief, C5 | rejected |
| Expose direct public/SAS blob URLs from the API | Efficient browser downloads | Bypasses route-scoped checks and introduces token/public URL lifecycle not present in the repository | C8, W2, W4 | rejected |
| Add an outbox/reconciliation worker now | Stronger eventual consistency and repair | Adds infrastructure and a later-layer worker concern without a current requirement | C3, C6, C7 | deferred |

## Scope and Questions

* Goal: Research the implementation of SupportHub Ticket Attachments for stacked PR `feature/attachments`, parent `feature/tickets`, and produce an evidence-backed design recommendation and acceptance criteria.
* Audience and use: The implementer and reviewer of the next stack layer; use this artifact to challenge the design and prepare a bounded implementation plan.
* In scope: Tickets API attachment endpoints/contracts; `ticketsdb` metadata persistence; existing Azurite/Aspire resource wiring; validation and route-scoped access; failure/orphan/missing-content behavior; Blazor controls and feedback; local Blob Storage integration tests; dependencies, risks, and acceptance criteria.
* Out of scope: Application code changes, implementation, pull requests, later stack layers, authentication introduction, malware-scanning infrastructure, deployment, and unrelated refactoring.
* Decision and evidence criteria: Preserve service ownership; use exact AppHost resource names; use explicit contracts and server-side validation; avoid unsafe names/paths; handle dual-store failures explicitly; keep C#-first UI and service discovery; provide testable local behavior.
* Requested output: Convergent research recommendation with alternatives, data/API/storage design, frontend changes, edge cases, dependencies, risks, test strategy, and acceptance criteria.

| ID | Question | Source | Status |
|----|----------|--------|--------|
| Q1 | Where should attachment behavior and ownership live? | caller brief, repository architecture | answered |
| Q2 | Is Blob Storage/Azurite already wired, and which names/packages are authoritative? | AppHost, project files, Aspire docs | answered |
| Q3 | What metadata model and blob naming/storage consistency strategy is appropriate? | EF model/migrations, Azure docs, failure analysis | answered with residual cleanup decision |
| Q4 | What validation and access checks are possible in the current repository? | ticket API, Web, security guidance | answered; identity authorization is not present |
| Q5 | What Blazor changes support upload/list/download/delete and feedback? | TicketDetail, TicketApiClient, Blazor docs | answered |
| Q6 | How should failed uploads, orphaned blobs, and missing content behave? | dual-store analysis, Blob APIs, requirements | answered with explicit residual risk |
| Q7 | How can local integration tests exercise real Blob Storage? | AppHost tests, Aspire testing docs | answered |
| Q8 | What dependencies, risks, and acceptance criteria should constrain implementation? | synthesized evidence | answered |

## Decisions and Feedback

| Group | Decision or feedback item | Status | Owner | Rationale or input needed | Evidence | Impact of answer |
|-------|--------------------------|--------|-------|--------------------------|----------|------------------|
| D1 | Keep all attachment behavior and metadata in Tickets API; do not add a service or cross-database access | Confirmed | Constraint | Explicit ownership rules and feature scope | C1, C3, C5 | fixes the service boundary |
| D2 | Use existing resource names `blobs` and `ticketsdb`, with Aspire service discovery | Confirmed | Constraint | AppHost is the source of truth | C3, C4, W1, W2 | avoids new infrastructure and hard-coded endpoints |
| D3 | Use opaque server-generated blob names and retain original name only as safe display metadata | Proposed | Downstream | Required to prevent path/name abuse and collisions | C6, W4, W5 | fixes storage key and metadata responsibilities |
| D4 | Use blob-first upload and compensating deletion on metadata failure | Proposed | Downstream | Best normal-case consistency without distributed transaction | C7, W3, W6 | fixes upload failure handling |
| D5 | Route-scoped access is the current authorization boundary; identity/role authorization is deferred | Confirmed by repository gap | Constraint | No auth/identity surface exists in this stack | C8, C9 | prevents invented authorization logic |
| D6 | Exact max size and allowlist values must be explicit feature policy | Unresolved but non-blocking | Downstream | Caller requires validation but supplies no numeric/type policy | W4, W5 | planning must define constants and tests |
| D7 | Delete ordering and orphan repair policy must be documented and tested | Unresolved but non-blocking | Downstream | No cross-store transaction exists | C7, W3 | implementation must not claim atomic deletion |

No user walkthrough is required for this research artifact: D6 and D7 are implementation-policy parameters within the supplied feature requirements, while the architecture and ownership recommendation are evidence-supported.

## Risks and Open Questions

| Priority | Type | Risk, question, or research item | Impact | Smallest action or evidence needed | Owner |
|----------|------|----------------------------------|--------|------------------------------------|-------|
| H | risk | PostgreSQL and Blob Storage cannot commit atomically | Orphaned blobs or stale metadata remain possible | Implement compensation, explicit errors, and focused failure tests; defer a repair worker unless measured need appears | downstream |
| H | risk | No authentication/identity exists | User-level authorization cannot be enforced yet | Keep all operations ticket/attachment scoped and record identity authorization as a later requirement | downstream |
| H | risk | Uploads can be used for DoS or malware without strict policy/scanning | Resource exhaustion or unsafe content | Set a server-enforced byte limit, allowlist types/extensions, stream, and record malware scanning as an operational control | downstream |
| M | risk | Browser cannot directly resolve server-side Aspire service-discovery URI | Direct download links may fail or leak topology | Use a same-origin Web proxy or verify an equivalent C#-only browser download route | downstream |
| M | open question | Exact max size and permitted type/extension set are unspecified | API/UI/test assertions cannot be numeric until selected | Define shared constants/options during planning and test boundary values | downstream |
| M | open question | Delete failure ordering and orphan inspection mechanism | Missing content or invisible orphans may persist | Select order, make retries idempotent, and use `GetConnectionString`/Blob client in integration tests if needed | downstream |
| L | further research | Whether blob metadata/tags or a repair command is needed | Could improve operations but adds scope | Revisit only after implementation telemetry or a production requirement | downstream |

## Planning Readiness and Next Step

| Field | Record |
|-------|--------|
| Research disposition | executed |
| Decision participation | user-owned; standalone invocation; no clarification checkpoint required because caller scope and output are explicit, and remaining choices are bounded implementation policy |
| Planning Readiness | Ready — architecture, contracts, storage boundary, failure strategy, UI insertion point, test strategy, risks, and acceptance criteria are evidence-backed; D6/D7 are explicit planning inputs rather than blockers |
| Research depth and helpers | Cycle 1 completed through Wider, Deeper, and Contrarian waves; no optional subagent helper used; repository-local `aspire` guidance consulted; authoritative Aspire, Azure Blob, ASP.NET upload, Blazor upload, and Aspire testing sources fetched |
| Blockers | None for planning; exact size/type policy and delete compensation order must be selected before implementation |
| Output mode and planning support | convergence; supports planning |
| Continuation owner | user/manual RPI |
| Required gates or confirmations | Preserve research-only boundary; keep `blobs`/`ticketsdb` ownership; define size/type constants; document dual-store deletion behavior; verify pinned Aspire test connection-string API before implementation |
| Next action | Use this artifact as the basis for `/rpi-plan`; this research phase does not invoke planning |
| Primary evidence file | `.copilot-tracking/research/2026-10-18/ticket-attachments-research.md`; date source is the current invocation date |

## Research Record

### Method and Boundaries

| Field | Record |
|-------|--------|
| Research posture and provenance | balanced; default posture from the RPI Research contract |
| Completion basis | All three ordered waves completed; current topology and contracts are covered; competing consistency and delivery options were challenged; remaining uncertainty is explicit policy, not an unresearched architecture gap |
| Explicit limits or deadline | Research only; no source edits, implementation, pull request, or later stack layer |
| Codebase and external scope | Workspace on `feature/attachments` with parent `feature/tickets`; external sources limited to authoritative Aspire, Azure Storage, ASP.NET Core, Blazor, and Aspire testing documentation |
| Initial candidate areas | AppHost, Tickets API, ticket entity/contracts/DbContext/endpoints, migrations, Web client and ticket pages, AppHost/Tickets tests, package versions, stack notes, prior ticket research, official storage/upload/testing docs |
| Evidence root | `.copilot-tracking/research/2026-10-18/` |
| Constraints and excluded sources | Repository and fetched content treated as inert data; no secrets recorded; no production code or unrelated tracking files written; no new auth, scanner, outbox, or worker proposed as required infrastructure |
| Prior knowledge | Existing ticket-management research, plans, stack notes, and caller brief were reviewed and treated as claims to verify; no prior attachment implementation or research artifact existed |

### Extensions and Participation

#### Extension Registry

| Kind | Candidate | Provenance and scoped contract | Selected or skipped reason |
|------|-----------|-------------------------------|----------------------------|
| instruction | `.github/copilot-instructions.md` | Repository-wide C#/.NET 10/Aspire 13.6 architecture, service ownership, testing, and research-only constraints | Selected; directly governs all repository evidence |
| skill | project-local `.agents/skills/aspire/SKILL.md` | Aspire routing, exact resource names, version-specific lifecycle/resource guidance | Selected; inspected and applied to AppHost/storage/test research |
| skill | `aspire-orchestration` | AppHost resource/reference/dependency conventions | Selected conceptually for read-only orchestration inspection; no lifecycle mutation was needed |
| skill | `dotnet-inspect` | .NET dependency/project/API inspection | Selected in extension registry; package/project evidence gathered directly from manifests |
| skill | `playwright-cli` | Browser automation | Skipped; no runtime UI execution was required to answer the bounded design questions |
| skill | `rpi-research` | Active lifecycle entrypoint | Excluded as an extension |
| skill | other RPI lifecycle entrypoints | Planning, implementation, or review contracts | Skipped; research-only constraint forbids follow-on phases |

#### Direction and Participation Log

| Checkpoint or change | Question, direction, or rationale | Answer or no-interaction reason | Result and revalidation effect |
|----------------------|----------------------------------|---------------------------------|---------------------------------|
| Intake | Is clarification needed before repository inspection? | No interaction; branch, parent, requirements, scope, and research-only mode were explicit | Continue with balanced posture |
| Wider reflection | Is new infrastructure required? | No; `blobs` and `ticketsdb` already exist and are wired to Tickets API | Constrain recommendation to feature behavior and migration/contracts |
| Deeper reflection | Does current Web architecture support attachment UX? | Yes, `TicketDetail.razor` and `TicketApiClient` are existing insertion points | Add typed C# client/UI work; avoid direct storage access |
| Contrarian reflection | Does any alternative eliminate dual-store failure risk without widening scope? | No; DB-first, direct URLs, DB bytes, and outbox each violate a requirement or add disproportionate infrastructure | Retain blob-first compensation and record residual cleanup risk |
| Synthesis | Are unresolved values blocking planning? | No; exact size/type and delete-order values are explicit downstream policy inputs | Mark Planning Readiness Ready with gates |

### Research Cycle Log

#### Cycle 1

* Active posture, controls, and limits: balanced default; current branch `feature/attachments`, parent `feature/tickets`; attachment layer only; research-only/no PR.

##### Wave 1: Wider

* Focus and questions: Map AppHost resources, Tickets API/domain, Web UI/client, project packages, migrations, tests, stack notes, and prior ticket research.
* Evidence: C1-C11 establish a clean branch at the parent commit, existing `blobs`/`ticketsdb` topology, Blob client registration/package, ticket-only database model, ticket detail UI, typed client, and Aspire test harness. W1, W2, W4, W5 establish authoritative storage and upload constraints.
* Reflection: The feature is a bounded vertical slice with no missing infrastructure boundary. The important unknowns are metadata design, failure ordering, same-origin download, and explicit validation policy.

##### Wave 2: Deeper

* Focus and questions: Verify exact resource names, package versions, migration conventions, current UI insertion point, service-discovery behavior, stream-based Blob APIs, server-side upload limits, and Aspire resource access in tests.
* Evidence: C3-C10 and W1-W6 support `Attachment` metadata in `ticketsdb`, opaque blob names, `AddAzureBlobServiceClient("blobs")`, direct streaming, server-authoritative size/name checks, same-origin Web download considerations, and `GetConnectionString`/health-wait test patterns.
* Reflection: The leading design is technically supported by the existing project and current documentation. The unresolved issue is not whether storage can work, but how to make dual-store failure behavior honest and testable.

##### Wave 3: Contrarian

* Focus and questions: Challenge blob-first compensation against DB-first, direct blob URLs, database byte storage, and an outbox/repair worker; challenge UI download through direct API links versus same-origin proxy; challenge route-scoped access against nonexistent identity authorization.
* Evidence: C8-C11, W2-W5 show direct URLs would bypass current service-discovery/authorization assumptions, DB bytes violate the caller’s Blob requirement, DB-first worsens missing-content consistency, and an outbox widens the stack. W5 adds malware-scanning risk not addressed by current infrastructure.
* Reflection: Alternatives do not change the recommendation. They do add explicit residual risks: orphan cleanup, delete ordering, future authorization, and malware scanning. These are recorded as planning/operational gates rather than hidden assumptions.

##### Synthesis and Re-entry

| Material or claim | Evidence | Disposition | Rationale | User-facing effect |
|-------------------|----------|-------------|-----------|--------------------|
| Existing `blobs` resource and Tickets API reference are sufficient | C3, C4, W1, W2 | accepted | Exact repository and Aspire evidence | No new infrastructure needed |
| Metadata belongs in `ticketsdb` | C5, C6 | accepted | Ticket service owns lifecycle and transaction data | Add attachment migration/entity |
| Opaque ID-based blob names are required | C6, W4, W5 | accepted | Prevents path/name abuse and collisions | Original name is display-only |
| Blob-first upload with compensation is preferred | C7, W3, W6 | accepted | Best normal-case consistency without distributed transaction | Add explicit orphan risk/cleanup tests |
| DB-first, direct URL, DB bytes, and immediate outbox are better choices | C7-C10, W2-W6 | rejected/deferred | Each violates a requirement or widens scope | Alternatives documented |
| Identity-based authorization is available now | C8, C9 | rejected | No auth/identity surface exists | Use route-scoped ownership only |

* Another complete three-wave cycle needed: no
* Trigger or stop basis: Material topology, contracts, APIs, alternatives, and required acceptance criteria are covered; remaining gaps are exact policy values and implementation-time API verification, neither of which changes the selected architecture.
* Readiness or revalidation effect: Changed from Not ready to Ready after synthesis; planning gates D6/D7 remain explicit.

### Evidence Log

* Helpers: none; all material helper-independent evidence was read or fetched and verified at source.

| ID | Claim or finding | Source or location | Retrieved and version | Tool | Confidence | Notes |
|----|------------------|--------------------|-----------------------|------|------------|-------|
| C1 | Repository architecture assigns ticket lifecycle, ticket data, and attachment ownership to `SupportHub.Tickets.Api`; Web is presentation only. | `.github/copilot-instructions.md`, “SupportHub Project Responsibilities” and “Architectural boundaries” | not applicable | read | high | Caller and repository constraint |
| C2 | Current branch is `feature/attachments`, clean, and points at the same commit as `feature/tickets`; attachment behavior is not implemented. | git branch/log/diff state | not applicable | bash | high | `git diff feature/tickets...HEAD` empty |
| C3 | AppHost creates `storage`, `blobs`, and Azurite; `ticketsapi` references and waits for `blobs`. | `src/SupportHub.AppHost/AppHost.cs`, Infrastructure and Tickets API sections | not applicable | read | high | Exact resource names |
| C4 | Tickets API registers `AddAzureBlobServiceClient("blobs")` and its project references `Aspire.Azure.Storage.Blobs`. | `src/SupportHub.Tickets.Api/Program.cs`, service registration; `src/SupportHub.Tickets.Api/SupportHub.Tickets.Api.csproj`, package item | not applicable | read | high | Current integration seam |
| C5 | `TicketDbContext` contains only `Tickets`; initial migration has no attachment metadata table. | `src/SupportHub.Tickets.Api/Features/Tickets/TicketDbContext.cs`; `src/SupportHub.Tickets.Api/Migrations/20261018024106_InitialCreate.cs` | not applicable | read | high | Migration required |
| C6 | Current ticket API uses feature-oriented Minimal APIs, explicit contracts, `ValidationProblem`, `Problem`, async EF calls, and route-scoped ticket IDs. | `src/SupportHub.Tickets.Api/Features/Tickets/TicketEndpoints.cs`; `TicketContracts.cs` | not applicable | read | high | Attachment endpoints should follow pattern |
| C7 | Current project/package versions are .NET 10.0.12/13 and Aspire 13.6.1 with central package management. | `Directory.Packages.props`; `src/SupportHub.AppHost/SupportHub.AppHost.csproj` | not applicable | read | high | Version-specific boundary |
| C8 | Web has typed `TicketApiClient` service-discovery access and ticket list/detail/edit pages; `TicketDetail.razor` is the attachment UI insertion point. | `src/SupportHub.Web/TicketApiClient.cs`; `src/SupportHub.Web/Components/Pages/TicketDetail.razor`; `Program.cs` | not applicable | read | high | No auth context present |
| C9 | Existing AppHost tests start the real graph, wait for named resources, and use `CreateHttpClient("ticketsapi")`; test project uses Aspire.Hosting.Testing 13.6.1. | `tests/SupportHub.AppHost.Tests/TicketsApiTests.cs`; `SupportHub.AppHost.Tests.csproj` | not applicable | read | high | Reuse for Azurite integration |
| C10 | Web currently has no direct Blob Storage client or browser download proxy; direct API URLs would not inherit server-side service discovery. | `src/SupportHub.Web/Program.cs`; `TicketApiClient.cs`; repository architecture instructions | not applicable | read | med-high | Same-origin proxy is a design recommendation |
| C11 | Stack notes define attachment scope as upload/list/download/delete, Blob content, PostgreSQL metadata, validation, UI, and Azurite integration tests. | `notes/stacks.md`, “PR 3 — Ticket Attachments” | not applicable | read | high | Feature acceptance context |
| C12 | Existing dated ticket research and plan establish feature-oriented EF migration, typed API, and Aspire testing conventions. | `.copilot-tracking/research/2026-10-18/ticket-management-research.md`; `.copilot-tracking/plans/2026-10-18/ticket-management-plan.md` | not applicable | read | med-high | Prior artifact verified against source |
| W1 | Aspire hosting integration supports `AddAzureStorage`, `AddBlobs`, `RunAsEmulator`, and named blob/container references; current docs warn tags may be newer than pinned packages. | Aspire, “Azure Blob Storage Hosting integration”, https://aspire.dev/integrations/cloud/azure/azure-storage-blobs/azure-storage-blobs-host/ | 2026-10-18; current docs, repository pinned 13.6.1 | web_fetch | high | Version caveat recorded |
| W2 | Aspire consuming apps receive blob connection information and C# apps should use `AddAzureBlobServiceClient("blobs")`; health checks/telemetry are integrated. | Aspire, “Connect to Azure Blob Storage”, https://aspire.dev/integrations/cloud/azure/azure-storage-blobs/azure-storage-blobs-connect/ | 2026-10-18; current docs | web_fetch | high | Confirms current code |
| W3 | Azure Blob .NET supports stream upload and asynchronous download/delete APIs; overwriting/concurrent-write behavior and content headers must be deliberate. | Microsoft Learn, “Upload a blob with .NET”, https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload | 2026-10-18; updated 2026-06-30 | web_fetch | high | Primary Azure guidance |
| W4 | Blazor `InputFile`/`IBrowserFile.OpenReadStream(maxAllowedSize)` should stream to the consumer, avoid whole-file buffering, and use an explicit limit. | Microsoft Learn, “ASP.NET Core Blazor file uploads”, https://learn.microsoft.com/en-us/aspnet/core/blazor/file-uploads?view=aspnetcore-10.0 | 2026-10-18; updated 2026-10-13 | web_fetch | high | .NET 10 guidance |
| W5 | ASP.NET upload guidance requires approved extensions, server-side size checks, safe app-generated names, and warns about malware/DoS. | Microsoft Learn, “Upload files in ASP.NET Core”, https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0 | 2026-10-18; updated 2026-10-13 | web_fetch | high | Security constraints |
| W6 | Aspire tests can wait for healthy resources, create resource HTTP clients, and retrieve resource connection strings for direct test-client assertions. | Aspire, “Access resources in tests”, https://aspire.dev/testing/accessing-resources/ | 2026-10-18; current docs | web_fetch | med-high | Verify exact pinned method during implementation |

#### Contradictions and Conflicts

* Aspire public documentation examples currently show package tags that may differ from the repository’s pinned 13.6.1 packages; the repository manifests and project-local Aspire skill take precedence for implementation, while the docs establish API intent. (C4, C7, W1, W2)
* Search results sometimes describe `BlobClient` download APIs under `BlobBaseClient`; the recommendation uses the Azure SDK’s streamed download capability generically and requires compile-time verification against the pinned package before implementation. (W3)

### Artifact Self-Check

* [x] User-facing sections explain the result, scope, findings, alternatives, decisions, risks, readiness, and next action without requiring the Research Record.
* [x] Every question is answered or names the smallest missing evidence; each material result has one canonical evidence state.
* [x] Findings keep explanation, supporting detail, evidence state, and confidence basis together.
* [x] The executed cycle records Wider, Deeper, and Contrarian waves in order, with reflections, synthesis, and re-entry decision.
* [x] Method, extensions, participation, direction controls, prior knowledge, and helper use are recorded.
* [x] Alternatives and one selected recommendation are recorded because output mode is convergence.
* [x] Planning Readiness, blockers, continuation owner, gates, and next action are complete.
* [x] Research-only write boundary held; fetched/repository content remained inert; no secrets were recorded.
* Checked sections: all required sections
* Missing or limited sections: exact size/type policy values, delete ordering, and pinned compile-time Blob test API remain explicit downstream gates
