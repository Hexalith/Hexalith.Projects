# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Serve authorized Project list, open, Conversation-start, context, and advisory reads from supported EventStore read models, expose them through authenticated FrontComposer Web and `Hexalith.McpCli` read surfaces, then cut read routing over reversibly. Chatbot and operators get current Project truth with no write authority (writes are Epic 7; release is Epic 8). Stories 6.1–6.6 and 6.8 are done. Story 6.7 stays in `backlog`, pinned by the production-authority guard with readiness `NOT_READY`; no planning artifact records a condition that releases it.

## Stories

- Story 6.1: List and open Projects through supported authenticated paths
- Story 6.2: Retrieve Conversation-start setup with admission truth
- Story 6.3: Retrieve assembled Project Context through supported read models
- Story 6.4: Resolve authorized advisory Project suggestions
- Story 6.5: Inspect Projects through an authenticated FrontComposer read surface
- Story 6.6: Inspect Projects through an authenticated CLI read surface
- Story 6.7: Cut over supported reads while preserving compatibility and rollback
- Story 6.8: Split Hexalith.Projects.UI.Contracts from the packable Contracts package

## Requirements & Constraints

- **Authorization.** Bind every read to the authenticated Tenant, original actor, workload, delegation, action, and target, all in one Tenant. Chatbot visibility follows current Folder read authority. Discovery, cached evidence, and service identity never grant access. Filter before paging.
- **Disclosure.** Safe Metadata by default. Names, Setup bodies, titles, paths, and Previews need `projects:inspect_descriptive_metadata` and a durable metadata-only inspection audit; an audit failure denies disclosure. Errors, logs, and telemetry echo no rejected value. Selection Evidence never appears on a read.
- **Operator surfaces.** Inventory, detail, reference health, and resolution traces only; no export, mutation, or audit read before Story 8.1. CLI output is deterministic JSON independent of color or width. The Gateway rejects a call without a token with `401`, exit `2` and no payload before any Projects query executes. Exit `0` for any result, including truthful `Partial`/`Unavailable`; `2` for errors, including the safe `404`. CLI and Web agree on states, reason codes, timestamps, and warnings.
- **Snapshot.** Active or Archived only; quarantined legacy Projects are never repaired by a read. Responses carry `responseState`, `asOf`, `projectVersion`, components, and `recoveryActions`. Pages default to 50, cap 200.
- **Context and advisory.** Context binds to the named Conversation's member Project or the session's opened Project. Advisory results are unselected, show incomplete coverage, and never authorize context or confirmation. Reads write nothing but the inspection audit.
- **Compatibility.** Contracts stay additive. Historical data, legacy IDs (opaque, never GUID-parsed), and `ProjectFolderCreationPending` stay readable; no history rewrite.

## Technical Decisions

- **Runtime.** Projects supplies contracts, projections, handlers, and the Reference Trust Index; EventStore DomainService supplies runtime, read models, and cursors. No Projects-owned runtime.
- **Contracts.** `Hexalith.Projects.Contracts` stays free of FrontComposer, Fluxor, Fluent UI, ASP.NET Core, Dapr, and Aspire; non-packable `Hexalith.Projects.UI.Contracts` holds presentation descriptors.
- **CLI.** `Hexalith.McpCli` is the only CLI/MCP adapter, sending `projects.list`, `projects.describe`, `projects.trace-conversation`, and `projects.trace-attachments` through the EventStore Gateway; computed queries omit projection selectors. The module CLI and MCP plug-in are frozen. McpCli never confirms.
- **Transport.** `200` for authorized reads; one safe `404` for denied, cross-Tenant, or absent targets; `400` for invalid input. The server derives Tenant and actor; adapters may only narrow the shared role matrix.
- **Cutover (6.7).** Inventory events, keys, read models, cursors, routes, clients, and partial records first. Exhaustive legacy/supported comparison consumes 6.1 output and `ProjectReadShadowComparator` over output, keys, exact watermarks, cursors, and Tenant isolation, with finite owner-approved normalization. ULID, OpenAPI, and generated-consumer contracts align before routing switches. Routing stays reversible, legacy stays until Epic 8 release acceptance, and failures roll back deterministically. One writer; command cutover is Epic 7. Old runtime removal also needs the common runner and package parity.

## UX & Interaction Patterns

- All surfaces share response, freshness, reason, and recovery vocabulary. Web must work by keyboard, screen reader, 200% zoom, and 320px reflow.

## Cross-Story Dependencies

- **6.7 pin.** `tools/planning/validate_production_authority.py` pins 6.7 at `backlog` and requires `NOT_READY`; the 2026-11-15 correction grants no 6.7 progress. Earlier exceptions each needed an approved correction plus a guard change. Edit sprint status by hand, never with `sprint_plan.py`.
- **6.7 verification** still names the undelivered common runner (`--profile read-cutover`, row `nfr-10-reads`); no individual-lane exception covers it.
- **Inherited by 6.7.** 6.5 kept legacy routes and claims no equivalence. Open entries gated on 6.7: DW-57, DW-59, DW-60, DW-63, DW-66; DW-77 gates its release. DW-58/DW-62 legacy-baseline repairs need normalization approval.
- **Downstream.** Epic 7 needs the 6.7 read cutover; Story 7.1 needs 6.7 accepted with executable rollback, and 7.1-P1 waits on its contract cutover.
- **Open deferred work.** DW-80 (advisory latency, 8.9), DW-82 (publish Contracts; McpCli main restore fails until then), DW-83 (restore green main CI and G-6), DW-84 (Epic 8 confirmation wording).
- **Done rule.** A story is done when its own criteria pass at a recorded commit. Upstream movement never reopens it; new gates become deferred work or stories.
