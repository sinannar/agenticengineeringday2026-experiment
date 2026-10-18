# Standard Plan Critique: SupportHub Experience Redesign & Integration

## Verdict

**Not ready for implementation handoff.** The plan has strong coverage of the requested web redesign and preserves the documented service boundaries, but several implementation-critical choices are deferred into execution. The largest blockers are the impossible-to-prove activity “processing” state under the current contract, the undefined console API-client/sample-data scope, and the absence of a concrete browser/component test strategy. Resolve PC-001 through PC-004 before implementation; PC-005 through PC-007 should be resolved in the plan or explicitly accepted as residual risk.

## Findings

### PC-001 — Activity processing state is not supported by the current contract

- **Severity:** High — decision/blocker
- **Evidence:** `P04-T02`/FR-004 require distinct “no activity,” “processing/not yet visible,” “unavailable,” and “request failed” states. `src/SupportHub.Tickets.Api/Features/Tickets/TicketEndpoints.cs` returns either `200` with the activity collection, `503`, `502`, or `404`; an empty `200` does not identify processing versus no events. The research explicitly classifies the observed empty response as eventual-consistency/data-state evidence, not a processing signal.
- **Owner:** Planner with product/architecture decision
- **Exact resolving action:** Choose and record one contract: (a) narrow FR-004 to empty/unavailable/error unless an existing signal exists, or (b) approve an additive API response/status field and assign the required Tickets API/Activity Worker work, tests, and ownership. Update P04-T02, FR-004, P06-T02, and the risks table so acceptance criteria are actually observable.
- **Disposition:** Requires a decision; not planner-owned alone.

### PC-002 — Console API-backed scope and sample-data behavior are unresolved

- **Severity:** High — implementation blocker
- **Evidence:** `P05-T02` says to inspect contracts during implementation and then decide whether options 2, 3, and 5 are real or unavailable. The current console has only `SlaScanClient`; `Program.cs` uses raw `IHttpClientFactory` for health and prints placeholder/sample rows. Customers API supports listing and Tickets API supports listing/creation, but sample ticket creation requires a valid customer and the plan does not define fixture/customer selection, batch limits, cleanup, or whether creation is allowed in an operational console.
- **Owner:** Planner
- **Exact resolving action:** Add a pre-implementation decision table for commands 2/3/5: endpoint, DTO/client class, input/fixture strategy, bounded output, cancellation, error states, mutation/cleanup policy, and supported/unavailable menu behavior. If sample generation is not approved, remove it from the required journey and make the command explicitly unavailable; otherwise add the required API-client and integration-test tasks.
- **Disposition:** Planner-owned unless product chooses to expand sample-data capability.

### PC-003 — Cross-surface terminology has no authoritative implementation seam

- **Severity:** High — regression risk
- **Evidence:** `P02-T01` and `P05-T03` permit “equivalent small presentation mappings,” while FR-007 requires one mapping across web and console. The console currently has no shared presentation contract, and the plan does not name a canonical mapping file/project or define how tests compare both surfaces. Separate mappings can satisfy local tests while drifting.
- **Owner:** Planner
- **Exact resolving action:** Define the canonical vocabulary artifact and ownership before implementation. Prefer a dependency-safe shared presentation-contract project/file containing wire-value-to-label mappings and timestamp/state wording, or explicitly document duplicated mappings plus a single table-driven conformance test that exercises both renderers. Add exact files and test location to P02/P05/P06.
- **Disposition:** Planner-owned.

### PC-004 — Browser and component verification is not implementation-ready

- **Severity:** High — verification blocker
- **Evidence:** `P02-T03` says to add a component test seam “if” the current harness cannot render components, and `P06-T02` says to use Playwright “or the repository’s available browser automation path.” The existing test project is xUnit/Aspire.Hosting.Testing and `SupportHub.Web.csproj` has no browser/component test dependency. P06-T02 nevertheless requires mutation, upload/download/delete, error, keyboard, forced-colors, reduced-motion, and mobile verification.
- **Owner:** Planner
- **Exact resolving action:** Select the concrete verification mechanism and add it to the plan: either introduce a justified browser/component test project and package/tool setup, or define a reproducible Playwright CLI evidence procedure with seeded data, exact routes, fixture creation, upload files, destructive-action cleanup, keyboard assertions, and artifact locations. Separate automated gates from manual/evidence checks and identify which requirements each proves.
- **Disposition:** Planner-owned.

### PC-005 — Interactive console behavior is not testable or terminal-safe by construction

- **Severity:** Medium/High — material risk
- **Evidence:** `Program.cs` calls `Console.Clear`, `Console.ReadLine`, `Console.KeyAvailable`, `Console.ReadKey`, and uses an unbounded polling loop. `P05-T01` requests terminal-safe feedback and cancellation, while `P06-T03` requires deterministic help/menu, health watch, narrow-width, and exit verification. No input/output abstraction, non-TTY behavior, timeout, or test seam is specified. Aspire Dashboard terminal support for these APIs is therefore an assumption.
- **Owner:** Planner
- **Exact resolving action:** Add a console interaction boundary (input/output/time provider or equivalent) and define behavior for non-interactive/narrow terminals, EOF, cancellation, and health-watch termination. Add focused tests for rendering and command dispatch, plus one bounded Aspire terminal run. If the existing terminal cannot support a behavior, mark it unavailable rather than relying on `Console.*` calls.
- **Disposition:** Planner-owned.

### PC-006 — Console implementation artifacts and dependency sequencing are incomplete

- **Severity:** Medium
- **Evidence:** `P05-T02` and `P05-T03` describe API-backed lists and aligned labels, but references mention only `Program.cs` and `SlaScanClient.cs`; no customer/ticket client contracts, response models, formatter, or shared vocabulary files are named. `P05-T03` depends on P05-T02, yet P06-T01 requires console rendering/command tests without a defined seam.
- **Owner:** Planner
- **Exact resolving action:** Expand P05 with explicit tasks/files for customer/ticket HTTP clients, DTOs, formatters, vocabulary mapping, cancellation/error translation, and tests. Add dependencies so implementation cannot reach P06 with an implicitly invented architecture.
- **Disposition:** Planner-owned.

### PC-007 — “All existing journeys remain available” is not fully operationalized for destructive browser flows

- **Severity:** Medium
- **Evidence:** FR-003/FR-005 and P06-T02 require customer/ticket create/edit/delete plus attachment upload/download/delete, but the plan only broadly says “safe test data and clean up.” It does not define isolation from seeded local data, ordering for ticket/customer deletion dependencies, blob cleanup, or how asynchronous activity verification is bounded.
- **Owner:** Planner
- **Exact resolving action:** Add a browser-fixture and cleanup protocol: create an isolated customer/ticket, retain IDs, upload a repository-local fixture, verify download/delete, wait/poll for activity with a bounded timeout, then delete in dependency order and verify cleanup. State which destructive checks are automated versus evidence-only.
- **Disposition:** Planner-owned.

## Positive assessment

The plan correctly keeps Blazor Interactive Server, typed service-discovery clients, AppHost resource names, API ownership, attachment policy, cache semantics, and activity ownership intact. It also explicitly rejects fabricated success states, hard-coded URLs, a new audit backend, and a frontend-framework migration. The critique findings concern unresolved execution contracts and verification seams, not a need for an unrelated redesign.

