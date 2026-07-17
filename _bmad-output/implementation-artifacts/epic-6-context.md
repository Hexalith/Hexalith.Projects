# Epic 6 Context: Authorized Project Reads on the Supported Platform

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver the read/query side of Hexalith.Projects through supported EventStore DomainService read models and a rebuildable Reference Trust Index. Authenticated Chatbot, FrontComposer Web, and CLI paths must expose the same authorization-filtered, metadata-only truth before a shadow-read-first, reversible cutover makes it authoritative. This establishes the identity, contract, and platform boundary required by later epics without introducing consequential writes.

## Stories

- Story 6.1: List and open Projects through supported authenticated paths
- Story 6.2: Retrieve Conversation-start setup with admission truth
- Story 6.3: Retrieve assembled Project Context through supported read models
- Story 6.4: Resolve Projects with transient current explanations
- Story 6.5: Inspect Projects through an authenticated FrontComposer read surface
- Story 6.6: Inspect Projects through an authenticated CLI read surface
- Story 6.7: Cut over supported reads while preserving compatibility and rollback

## Story 6.1 Entry Gate

Story 6.1 is `blocked`, not `ready-for-dev`. Platform enablement is external to the value story and
is tracked through five prerequisite work packages approved by the 2026-07-17 Sprint Change
Proposal:

- **6.1-P0 — G-4 runner/evidence:** Builds/platform tooling must supply the supported persisted runner
  and machine-checkable evidence producer.
- **6.1-P1 — version/source normalization:** EventStore, Builds, and the Solution Architect must
  approve one compatible source, architecture, central-package, API, and runner baseline.
- **6.1-P2 — query safety/projection capability:** EventStore/platform must supply complete immutable
  dual-principal identity, indistinguishable safe denial, and an authoritative global-position
  watermark.
- **6.1-P3 — production identity contract:** identity/security owners must approve mandatory
  fail-closed production authentication and supported verification fixtures.
- **6.1-P4 — Projects gate acceptance:** Product Owner, Solution Architect, Test Architect, and all
  P0-P3 owners must accept exact pins, evidence, normalization, rollback, and the Story 6.1 readiness
  rerun.

P0 and P1 may proceed in parallel; P2 depends on P1, P3 depends on P2, and P4 depends on P0-P3.
Targets remain uncommitted until the repository owners accept their local work. No Story 6.1 task
may enter progress before P4 is accepted and its specification passes ready-for-development.

## Requirements & Constraints

- List and open expose authorized metadata, lifecycle, setup, and reference summaries only. Lists include Active and Archived Projects, support lifecycle filtering, page deterministically with an authenticated opaque cursor (default 50, cap 200), and never expose pre-activation tasks as Projects.
- Conversation-start setup is limited to goals, user-facing instructions, context preferences, and default source policy. Project Context is assembled at query time through allowlist checks for Tenant, Project, lifecycle, authorization, and freshness. Every omission is represented with a safe state and reason; refresh is read-only.
- Resolution recomputes from current authorized Conversation, Folder, and File metadata and returns `NoMatch`, `SingleCandidate`, or `MultipleCandidates`. Archived Projects are excluded by default, raw content is never inspected, explanations are request-scoped and nonpersistent, and no read selects, attaches, confirms, or creates anything.
- Applicable responses share `responseState`, `asOf`, authorized `projectVersion`, component inclusion/freshness evidence, optional resolution outcome, and recovery actions. `Partial` is usable only when required Project, Folder, Setup, and authorization evidence are current and all omissions are explicit; `Unavailable` blocks context use.
- Every read is Tenant-, actor-, action-, target-, and version-scoped. Server-derived Tenant and actor identity, delegated actor authority, owner reauthorization, and query-side filtering are mandatory. Denied, cross-Tenant, and nonexistent targets collapse to a non-leaking `404`. Responses, logs, errors, traces, and all surfaces remain metadata-only.
- Metadata reads target p95 below 500 ms at 1,000 Projects and 500 references and below 1 second at the supported maximum. Contracts stay additive and tolerant; historical data, events, legacy identifiers, and name-only creation compatibility remain readable. Cutover never rewrites history or introduces a dual writer.

## Technical Decisions

- Projects owns stable contracts, query handlers, incremental projections, context/resolution policy, and presentation descriptors. EventStore DomainService owns read-model storage, cursors, hosting, health, and telemetry; platform hosts own authenticated Web and CLI composition. Domain code must not recreate platform runtime plumbing.
- Incremental projections and queries use the supported DomainService projection, read-model store, query-handler, and cursor seams. Cursors are opaque and bound to authenticated query scope. The Tenant-scoped Reference Trust Index stores only safe owner metadata, owner version/watermark, authorization outcome, and freshness; Conversations, Folders, and Memories retain resource authority.
- Versioned .NET contracts are the editable authority for DTOs, identifiers, shared states, reasons, recovery codes, schemas, and security semantics. OpenAPI and consumer artifacts are generated and mechanically verified. Governed identities are platform-generated ULIDs; foreign and legacy identifiers remain opaque and are never GUID-parsed.
- All surfaces preserve one transport mapping and vocabulary: authorized reads return `200` with `Complete`, `Partial`, or `Unavailable`; denial and nonexistence return safe `404`. Structured errors and recovery guidance never echo protected data.
- Cutover replays immutable streams into shadow supported models, compares output, keys, watermarks, cursors, and Tenant isolation, then switches read routes independently. Routing stays reversible and legacy reads remain available until later release acceptance; any failed equivalence gate or regression rolls back and records a blocker.

## UX & Interaction Patterns

- Web is a FrontComposer/Fluent UI operational console, not a bespoke frontend. Read-only inventory, detail, reference-health, current-resolution, and audit views use shared status/reason contracts, explicitly show stale or unavailable evidence, leak nothing through absence/denial, and meet WCAG 2.2 AA for keyboard, focus, assistive technology, 200% zoom, and 320-pixel reflow.
- CLI uses predictable `list`, `describe`, `inspect`, `trace`, `validate`, and `audit` reads with deterministic JSON, stable schemas and exit codes, and no color-dependent meaning. For identical facts, Web and CLI preserve equivalent lifecycle/reference states, reasons, timestamps, warnings, and audit identifiers.

## Cross-Story Dependencies

- Story 6.1 and later consumers of its shared read baseline require accepted 6.1-P0 through 6.1-P4. Stories consuming sibling reads additionally require their G-2 owner contracts; Story 6.5 additionally requires the G-3 FrontComposer contract; Story 6.6 additionally requires its CLI adapter contract; affected build/evidence lanes require G-6. Missing external capabilities require separately authorized repository-local work.
- Shared read models, trust evidence, snapshot vocabulary, and authorization semantics feed both operator surfaces. Final cutover requires deterministic equivalence across every Epic 6 read plus aligned ULID, OpenAPI, and generated-consumer contracts. Epic 7 consumes this boundary; legacy read routing remains until Epic 8 release acceptance.

## Course-Correction Authority

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md` — approved Major correction and handoff route.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — live P0-P4 action ledger and Story 6.1 blocked state.
- `_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml` — canonical FR/NFR implementation-readiness evidence state.
