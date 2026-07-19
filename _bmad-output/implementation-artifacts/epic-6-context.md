# Epic 6 Context: Authorized Project Reads on the Supported Platform

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorization-filtered Project list, open, context, and resolution reads through supported EventStore DomainService read models and a rebuildable Reference Trust Index. Authenticated Chatbot, FrontComposer Web, and CLI consumers receive the same metadata-only truth before a shadow-read-first, reversible cutover makes those models authoritative. The epic establishes the identity, contract, and platform boundary required by later work without adding consequential writes.

## Stories

- Story 6.1: List and open Projects through supported authenticated paths
- Story 6.2: Retrieve Conversation-start setup with admission truth
- Story 6.3: Retrieve assembled Project Context through supported read models
- Story 6.4: Resolve Projects with transient current explanations
- Story 6.5: Inspect Projects through an authenticated FrontComposer read surface
- Story 6.6: Inspect Projects through an authenticated CLI read surface
- Story 6.7: Cut over supported reads while preserving compatibility and rollback

## Requirements & Constraints

- Every read is Tenant-scoped and filtered by current actor, delegated workload, action, target, and version authority. Identity is server-derived, owners and queries reauthorize, and denied, cross-Tenant, and nonexistent targets collapse to safe `404`. Outputs and observability are metadata-only and exclude foreign payloads, secrets, unrestricted paths, and raw upstream problems.
- List, open, resolution, context, and Conversation-start results share `responseState`, `asOf`, authorized `projectVersion`, metadata-only `components`, and `recoveryActions`. `Complete` requires current mandatory evidence. `Partial` requires current Project, Folder, Setup, and admission authorization with every omission explicit. `Unavailable` blocks use; denial discloses nothing. An Active or usable Project has exactly one authorized Folder confirmed by the read model.
- Lists expose visible Active and Archived Projects, exclude pre-activation tasks, and use deterministic lifecycle-filtered pages with authenticated opaque cursors (default 50, maximum 200). Context includes references only after lifecycle, authority, and freshness checks; exclusions stay explicit and refresh never mutates. Resolution returns unselected `NoMatch`, `SingleCandidate`, or `MultipleCandidates` from current authorized metadata and persists no trace.
- Reads support 10,000 Projects per Tenant and 5,000 references per Project, targeting p95 below 500 ms at 1,000 Projects/500 references and below one second at maximum shape. Contracts remain additive; historical events, data, compatibility requests, and identifiers remain readable without history rewrite.

## Technical Decisions

- Projects supplies contracts, query handlers, named incremental projections, policy, and presentation descriptors. EventStore DomainService/platform owns runtime, storage, cursors, health, telemetry, identity admission, and generated adapters; Projects does not recreate that plumbing.
- Projections use supported asynchronous read-model seams with explicit write policy; queries use the supported handler seam and authenticated, scope-bound cursors. The rebuildable Tenant Reference Trust Index stores only safe owner metadata, version or authoritative watermark, authorization outcome, and freshness. Foreign contexts retain resource authority; refresh uses bounded owner batch reads.
- Versioned .NET contracts own DTOs, identifiers, vocabularies, schemas, and security semantics; OpenAPI and consumers are generated and live-host verified. Governed identities are platform-generated ULIDs; foreign and legacy IDs stay opaque. All surfaces map authorized reads to `200` with `Complete`, `Partial`, or `Unavailable`, and denial/nonexistence to safe `404`.
- Cutover compares shadow-model output, keys, authoritative watermarks, cursors, and Tenant isolation. Routes switch only after equivalence, remain reversible through release acceptance, and roll back honestly on failure. History is never rewritten and no dual command writer is introduced.

## UX & Interaction Patterns

- Web is a FrontComposer/Fluent UI operational console, not a bespoke frontend. Read views use shared states and reasons, distinguish absence, denial, stale, and unavailable safely, and meet WCAG 2.2 AA for keyboard use, assistive technology, 200% zoom, and 320 CSS-pixel reflow.
- CLI read commands produce deterministic JSON with stable schemas and exit codes and no color-dependent meaning. Web and CLI expose equivalent states, reasons, timestamps, warnings, audit identifiers, redaction, and role visibility.

## Cross-Story Dependencies

- Story 6.1 and later consumers require accepted 6.1-P0 through P4: runner/evidence tooling, a normalized EventStore/Builds baseline, dual-principal safe-query and watermark support, fail-closed production identity, and final gate acceptance. P0/P1 may run in parallel; P2 depends on P1, P3 on P2, and P4 on P0-P3.
- Sibling-resource reads require pinned owner contracts. Web also requires the approved FrontComposer contract, CLI its adapter contract, and affected evidence lanes runtime/toolchain alignment.
- Shared read models, trust evidence, vocabulary, and authorization feed all surfaces. Cutover requires equivalence across every Epic 6 read plus aligned ULID, OpenAPI, and generated consumers. Epic 7 consumes this boundary; legacy reads remain through Epic 8 release acceptance.
