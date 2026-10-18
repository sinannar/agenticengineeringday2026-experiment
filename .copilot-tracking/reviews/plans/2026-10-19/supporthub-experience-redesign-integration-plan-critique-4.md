<!-- markdownlint-disable-file -->
# RPI Plan Critique: SupportHub Experience Redesign & Integration

## Metadata

* Task ID: `supporthub-experience-redesign-integration`
* Critique date: 2026-10-19
* Plan: `.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md`
* Critique execution: Complete
* Critique depth: standard
* Depth provenance: default from the RPI Plan Critique contract
* Critique type: follow-up
* Earlier critiques:
  * `.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique.md`
  * `.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique-2.md`
  * `.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique-3.md`

## Inputs and Criterion Boundary

* Task context and caller requirements: Final standard gate; verify resolution of `PC-012` and `PC-013`, implementation readiness, exact browser and terminal evidence procedures, activity semantics, canonical vocabulary ownership, original scope constraints, and complete FR/NFR/task coverage.
* Research and evidence considered:
  * `.copilot-tracking/research/2026-10-19/supporthub-experience-redesign-integration-research.md`
  * `.github/copilot-instructions.md`
  * `src/SupportHub.AppHost/AppHost.cs`
  * `src/SupportHub.Sla.Console/Program.cs`
  * `src/SupportHub.Tickets.Api/Features/Tickets/TicketEndpoints.cs`
  * `.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md`
* Decisions, dependencies, task Goals, and task Requirements considered: `D1-D6`, `FR-001` through `FR-010`, `NFR-001` through `NFR-007`, phases `P01-P06`, tasks `P01-T01` through `P06-T04`, dependencies, risks, and the prior critique disposition table.
* Assessment boundary: The supplied plan and evidence were assessed once for substantive implementation blockers, contradictions, missing dependencies, unsupported acceptance claims, and scope compliance. No new external research or application changes were performed. Planned test fixtures and tape files are assessed as implementation artifacts specified by the plan, not as already-created source files.

## Coverage Assessment

| Requirement, research, phase, or task ID | Coverage | Evidence or concern |
|---|---|---|
| `D1-D6`, confirmed scope and non-goals | Covered | The plan preserves C#-first Blazor Interactive Server, existing API ownership, service discovery, business rules, AppHost resources, Redis behavior, command compatibility where practical, and the no-new-backend/no-new-audit boundary. |
| `FR-001`–`FR-008`, `NFR-001`–`NFR-005` | Covered | Requirements are assigned across P01-P05 and page/component tasks preserve routes, contracts, semantics, accessibility, responsive behavior, and template removal. |
| `FR-009`–`FR-010`, `NFR-006`–`NFR-007` | Covered | P06-T01 through P06-T04 define build/test, browser, terminal, Aspire health, logs/traces, service-discovery, and terminal-safe evidence. |
| Activity semantics: `D5`, `FR-002`, `FR-004`, `NFR-004`, `P02-T02`, `P04-T02`, `P06-T02` | Covered | Activity states are limited to observable empty, unavailable, invalid/not-found, and request-failure outcomes. “Processing” is restricted to operation-local progress with an actual UI signal; no empty activity response is labeled as processing. |
| Canonical vocabulary: `FR-007`, `NFR-004`, `P05-T03`, `P06-T01` | Covered | `src/SupportHub.Presentation/SupportHub.Presentation.csproj` is selected as the sole presentation-only vocabulary owner and is consumed by both Web and Console, with shared conformance tests. |
| Browser evidence: `FR-001`–`FR-005`, `FR-008`, `FR-010`, `NFR-002`–`NFR-004`, `P06-T02`, `P06-T04` | Covered | The plan names both fixtures, route/state matrix, deterministic API stop and validation faults, API/unit fallback for unsafe activity faults, run/evidence paths, bounded polling, and `try/finally` cleanup with deletion assertions. |
| Terminal evidence: `FR-006`, `FR-007`, `FR-009`, `FR-010`, `NFR-006`, `NFR-007`, `P05-T01`, `P06-T03` | Covered | The plan names `tests/fixtures/supporthub-cli.tape`, the exact `vhs` invocation, fixed `100x30` dimensions, scripted inputs, per-command timeouts, explicit resource start/stop, unavailable-service restoration, output path, fallback limitation, AppHost teardown, and post-stop `aspire ps`. |
| `P06-T04` final gate and self-check | Covered | The final gate covers solution restore/build/test, diff review, template-removal checklist, browser/keyboard/viewport/terminal/Aspire evidence, and relevant logs/traces. The readiness fields correctly remain pending until this critique is recorded. |

## Verdict

* Verdict: Pass
* Rationale: The revision resolves `PC-012` and `PC-013` in substance and is implementation-ready. Browser verification now has exact planned fixtures, routes, state matrix, deterministic fault methods or explicit API/unit fallback, evidence paths, bounded polling, and `try/finally` cleanup. Terminal verification now has a named tape artifact, exact `vhs` invocation, fixed dimensions, explicit menu/input coverage, lifecycle/resource start-stop and restoration sequence, output/evidence paths, timeouts, fallback behavior, and teardown checks. The activity-processing contradiction is removed, one canonical vocabulary project is selected for Web and Console, and the original architecture, scope, FR/NFR, phase, task, and validation coverage remains intact.

## Earlier Finding Reconciliation

| Earlier finding | Status | Evidence |
|---|---|---|
| `PC-001` | Resolved | `P02-T02`, `D5`, `FR-004`, and `P04-T02` restrict activity to contract-observable states and operation-local progress signals. |
| `PC-002` | Resolved | `D6` and `P05-T02` make sample-data generation explicitly unavailable/removed and prohibit fabricated output or unsupported mutation. |
| `PC-003` | Resolved | `P05-T03` selects one neutral presentation project rather than permitting duplicate mappings. |
| `PC-004` | Resolved | `P06-T02` provides route/state coverage, deterministic browser faults, explicit fallback evidence, and state assertions. |
| `PC-005` | Resolved | `P06-T03` defines the terminal harness, fixed dimensions, scripted inputs, command timeouts, fault restoration, and teardown. |
| `PC-006` | Resolved | `P05-T02` defines API-backed console clients, DTOs, bounded formatters, cancellation, error translation, and focused tests. |
| `PC-007` | Resolved | `P06-T02` defines unique IDs, fixtures, evidence separation, dependency-order cleanup, and cleanup assertions. |
| `PC-008` | Resolved | No activity-specific processing acceptance claim remains unsupported by the current contract. |
| `PC-009` | Resolved | Web and Console consume the canonical vocabulary project `SupportHub.Presentation`. |
| `PC-010` | Resolved | `P06-T02` names exact fixtures, route/state matrix, deterministic induction or API/unit fallback, evidence paths, and `try/finally` cleanup. |
| `PC-011` | Resolved | `P06-T03` names the tape, `vhs` command, exact lifecycle/resource sequence, scripted inputs, timeouts, restoration, output, and teardown. |
| `PC-012` | Resolved | `P06-T02` explicitly treats the attachment fixtures as planned implementation artifacts and supplies the missing reproducibility and cleanup protocol. |
| `PC-013` | Resolved | `P06-T03` explicitly names `tests/fixtures/supporthub-cli.tape`, `vhs tests/fixtures/supporthub-cli.tape`, the exact lifecycle sequence, and a documented fallback limitation. |

## Findings

No new substantive findings.

## Strengths and Residual Risk

* The plan is appropriately bounded to presentation, workflow, and verification changes; it does not smuggle in backend, audit, identity, persistence, or frontend-framework capability.
* The browser runbook distinguishes safe runtime induction from activity faults that require deterministic API/unit evidence.
* The terminal runbook acknowledges the experimental terminal surface and refuses to claim deterministic evidence when `vhs` is unavailable.
* The named fixture and tape files are implementation-phase artifacts and do not need to exist before implementation begins; their exact paths and ownership are specified.
* Residual risk is limited to ordinary implementation execution: the planned fixture/tape artifacts must be created exactly as specified and the readiness/self-check fields must be synchronized after this critique.

## Questions or Blocking Evidence Gaps

* None decision-critical. The plan explicitly records the API/unit fallback when a browser activity fault cannot be safely induced and the terminal fallback limitation when `vhs` is unavailable.

## Limitations

* This was a standard, supplied-evidence critique. No new runtime execution, external research, or application-code inspection beyond the named relevant files was performed.

## Recommended Next Action

* Highest-impact finding: None.
* Action owner: Planning parent.
* Smallest next action: Record this passing critique in the plan’s readiness, self-check, and handoff fields, then finalize the plan for implementation.
* User response required: No.

| [.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md](.copilot-tracking/plans/2026-10-19/supporthub-experience-redesign-integration-plan.md) | Implementation plan assessed |
|---|---|
| [.copilot-tracking/research/2026-10-19/supporthub-experience-redesign-integration-research.md](.copilot-tracking/research/2026-10-19/supporthub-experience-redesign-integration-research.md) | Supplied research evidence |
| [.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique-2.md](.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique-2.md) | Earlier critique and findings |
| [.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique-3.md](.copilot-tracking/reviews/plans/2026-10-19/supporthub-experience-redesign-integration-plan-critique-3.md) | Earlier critique and PC-012/PC-013 disposition |

## Next Steps

No user action is required. The planning parent may finalize readiness and hand off to implementation.
