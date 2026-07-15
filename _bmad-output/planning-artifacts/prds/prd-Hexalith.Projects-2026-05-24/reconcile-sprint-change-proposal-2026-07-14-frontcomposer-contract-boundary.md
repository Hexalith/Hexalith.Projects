# Final Reconciliation: FrontComposer Contract Boundary

## Input

- Approved source: `sprint-change-proposal-2026-07-14-frontcomposer-contract-boundary.md` (approved 2026-07-14).
- Compared with: `prd.md`, `addendum.md`, and `.memlog.md` in the current PRD workspace (audited 2026-07-15).
- Verdict: **Fully reconciled — no current gap remains.**

## Extracted requirements and decisions

### Product requirement content

- Preserve the approved v1 scope, users, journeys, roles, functional requirements, UX, accessibility, tenant isolation, and observable Web/CLI/MCP behavior; this correction introduces no product feature or MVP-scope change.
- Preserve additive, serialization-tolerant compatibility unless a breaking change is explicitly approved, and require release evidence that unexplained critical failures or skips cannot be represented as passing.
- Preserve equivalent cross-surface semantics and supported-consumer behavior after the boundary correction, including descriptor discovery, routes/navigation, views, state names, reason codes, fields, contract versions, accessibility, tenant isolation, and payload-leakage protection.

### Implementation and technical how

- Move application-specific FrontComposer projection descriptors to a non-packable `Hexalith.Projects.UI.Contracts` assembly owned by the Projects repository; keep reusable DTOs, semantic enums, constants, lifecycle vocabulary, and lightweight `[ProjectionBadge]` metadata in packable `Hexalith.Projects.Contracts`.
- Enforce dependency direction `UI.Contracts` → `Contracts`; prohibit `Contracts` from depending on FrontComposer Shell, Fluxor, Fluent UI, or `Microsoft.AspNetCore.App`; exclude `UI.Contracts` from the NuGet package inventory; and keep MCP/CLI independent of the descriptor host.
- Preserve logical namespaces and FrontComposer contract versions by default. Audit supported consumers and require an explicit migration/package-versioning decision if assembly or public-surface impact is found.
- Retarget descriptor discovery, FrontComposer inspection, UI-lane descriptor tests, package-dependency/non-packability gates, documentation, and sprint evidence to the corrected owner.
- Treat the proposal's Story 5.13 route as provenance. The current implementation-readiness rebaseline supersedes its delivery vehicle with Story 6.2 without reversing the approved boundary decision.

## Current coverage

- `prd.md` correctly remains capability-oriented and unchanged by this technical correction. NFR-10 retains the compatibility rule; NFR-11 retains the release-evidence gate.
- Addendum §4.2 names the approved non-packable `Hexalith.Projects.UI.Contracts` boundary, Projects ownership, dependency direction, prohibited Contracts dependencies, package exclusion, MCP/CLI independence, namespace/version preservation default, consumer-audit requirement, and Story 6.2 delivery route.
- Addendum §7.1 binds release readiness to the supported-consumer audit, equivalent generated and cross-surface behavior, retargeted inspection and UI tests, dependency/non-packability gates, tenant isolation, accessibility, and `NoPayloadLeakage` evidence.
- Addendum §8 E-5 preserves the approved proposal as decision provenance and explicitly records that E-1/E-4 supersede only the original Story 5.13 delivery route with Story 6.2.

## Gap summary

**None remain.** The current PRD/addendum set preserves all product outcomes and routes the assembly layout, type ownership, dependency graph, package mechanics, migration decision, test gates, and delivery tracking to implementation-facing artifacts at the appropriate level of detail.

## Conflict summary

- **No unresolved conflict with `.memlog.md` decisions.** The memory log preserves the UI.Contracts boundary and Contracts package release-readiness gate while keeping technical detail in the addendum.
- The memory log's reference to Story 5.13 as evidence is historical provenance, not a conflicting current delivery instruction. Addendum §4.2 and §8 E-5 explicitly record the later Story 6.2 route; the boundary decision itself is unchanged.
- The proposal's “no PRD edits” decision remains satisfied: PRD NFR-10/NFR-11 already express the relevant product-level compatibility and evidence obligations, while the physical assembly split and gates remain implementation/how in the addendum.

## Disposition

- Leave `prd.md`, `addendum.md`, and `.memlog.md` unchanged.
- Mark this input **accepted and fully reconciled**.
