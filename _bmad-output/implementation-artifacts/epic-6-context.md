# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorized Project list, open, Conversation-start, context, and advisory reads through supported EventStore read models, expose them through authenticated FrontComposer Web and `Hexalith.McpCli` read surfaces, then move read routing over reversibly. Chatbot and operators get current Project truth with no consequential write authority; writes belong to Epic 7, and production release is an Epic 8 decision. Stories 6.1–6.5 are done. Under a guarded exception, 6.8 may be scheduled now, 6.6 follows once 6.8 and DW-55 are done, and 6.7 stays in backlog.

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

- **Authorization.** Bind every read to the authenticated Tenant, original actor, workload and delegation, action, and target; the session and every referenced resource must be in the Project's Tenant. Chatbot Project User visibility comes from current Folder read authority. Tenant Operators and Tenant Project Administrators read through Web and eligible McpCli operations. Owners reauthorize their own resources; discovery, cached evidence, and service identity never grant access. Filter by authority before paging.
- **Metadata and audit.** Tenant roles get Safe Metadata by default. Project name, Setup bodies, titles, paths, and Preview bodies need `projects:inspect_descriptive_metadata` and a durable, metadata-only inspection audit; an audit write failure denies the disclosure. The quarantined-legacy inventory is readable with one inspection event per Project. Errors, logs, telemetry, and evidence carry Safe Metadata only and echo no rejected value.
- **Operator reads (Web and CLI).** Inventory, detail, reference health, and current resolution traces only; read permission grants neither Safe Diagnostic Export nor mutation. Until Story 8.1 brings audit truth, the Web audit tab shows "not yet available" and McpCli has no Projects audit operation.
- **CLI output.** Deterministic, Tenant-scoped, authorization-filtered JSON; no meaning depends on color, terminal width, or prose. A missing credential reaches the Gateway, which returns 401 before any Projects query executes (6.6 D8). McpCli exit codes stay unchanged: `0` for a result, including `Partial` or `Unavailable` reported truthfully with snapshot, components, freshness, and reason codes; `2` for an error, including the safe `404`, with no payload or remote detail. With the same facts, CLI and Web must agree on lifecycle and reference states, reason codes, `asOf`/`lastVerifiedAt`, and warnings.
- **Lifecycle and snapshot.** Projects are Active or Archived; pre-activation tasks are never listed. Legacy folderless Projects stay quarantined, and no read silently repairs state. List and open return one snapshot: `responseState`, `asOf`, disclosable `projectVersion`, components (inclusion, freshness, reason, last-verified), and `recoveryActions`. Pages default to 50 rows (cap 200), use deterministic order and scoped cursors, and never select a resolution candidate.
- **Context, advisory, persistence.** Context binds to the named Conversation's member Project, or to the Project opened in that session. Advisory results are unselected, show explicit incomplete coverage (never an exhaustive NoMatch), and cannot authorize context, first response, or confirmation. Apart from the inspection audit, reads write no domain events or read models and persist no inference, traces, or exports. Selection Evidence never appears on a read.

## Technical Decisions

- **Runtime.** Projects supplies contracts, projections, query handlers, and the Reference Trust Index. EventStore DomainService supplies runtime, read models, and cursors (`IDomainQueryHandler`, `IReadModelStore`, `QueryCursorScope`). Add no Projects-owned runtime or persistence protocol.
- **Contracts.** `Hexalith.Projects.Contracts` is dependency-light and owns operations, wire vocabulary, and security semantics, with no FrontComposer, Fluxor, Fluent UI, `Microsoft.AspNetCore.App`, Dapr, or Aspire dependency. The non-packable `Hexalith.Projects.UI.Contracts` depends inward on it and holds presentation descriptors only. Types used by Contracts commands, events, models, or queries (such as `ReferenceState`, `ProjectLifecycle`, `ProjectReasonCode`) stay in Contracts with namespaces and wire names unchanged. CLI-only and MCP-only consumers pull no Blazor or FrontComposer dependency, even transitively.
- **CLI host.** `Hexalith.McpCli` is the CLI and MCP adapter; add no per-module CLI, MCP server, or module-specific McpCli adapter. McpCli enrolls `Hexalith.Projects.Contracts` (`HexalithContracts="true"`) and sends decorated queries through the EventStore Gateway, the route 6.5 Web uses. `Hexalith.Projects.Cli` and the Projects MCP plug-in are frozen migration sources (safety and continuity fixes only), with inventory rows kept `defer / exclude`.
  - Operations via `[HexalithQuery(Name=…)]`: `projects.list` (`ListProjectsQuery`), `projects.describe` (`GetProjectQuery`), `projects.trace-conversation` (`ResolveProjectFromConversationQuery`), `projects.trace-attachments` (`ResolveProjectFromAttachmentsQuery`, at most 32 attachments). Conversation-start setup and context queries are not enrolled.
  - Story 6.6 D9 (Jerome, 2026-11-15 session "authorize"): computed queries omit both projection selectors, as Web does; generic McpCli catalog validation now permits absence while preserving explicit selector validation and Gateway committed-target/authorization guards. This scoped correction resolves the original required HALT; no module adapter or Gateway bypass.
  - McpCli consumes Contracts only as a package, and published 1.2.4 is undecorated, so enrollment is proven against a locally packed Contracts with an isolated `NUGET_PACKAGES`.
  - McpCli edits are authorized for 6.6 only and pass McpCli's own gates; merging to `main`, pushing, publishing, and moving gitlinks are not.
- **No CLI confirmation.** McpCli refuses every confirmation-required and UI-only action, cannot mint Selection Evidence, and cannot become a confidential interactive client; neither A-7 nor Story 8.11 changes that. Confirmation stays on confidential interactive Web and Chatbot UI. Epic 8 text enabling CLI confirmation after A-7 is superseded (DW-84).
- **Transport and identity.** Authorized reads return `200` with `Complete`, `Partial`, or `Unavailable`; denied, cross-Tenant, and nonexistent targets one identical safe `404`; invalid input `400`. Every adapter applies the same role and action matrix and can only narrow it. The server derives Tenant and actor, never from arguments or headers. Allow-all authentication exists only in explicit test composition; incomplete authentication configuration fails startup.
- **Cutover.** Story 6.7 owns it: shadow-read comparison first, reversible routing, immutable history, and a single writer.

## UX & Interaction Patterns

- **Shared vocabulary.** Chatbot, Web, CLI, and MCP share response, freshness, safe-reason, and recovery meanings with no local synonyms. Show incomplete advisory coverage without preselecting a candidate.
- **CLI commands.** `projects.describe` covers `describe`, `inspect`, and `validate` (`validate-references`) through its reference states, in the style of `kubectl describe`; the two trace operations cover `trace` (`trace-resolution`).
- **Web.** FrontComposer and Fluent UI; usable by keyboard, screen reader, 200% zoom, and 320px reflow.

## Cross-Story Dependencies

- **Story 6.5 closure.** Done on its own criteria (2026-11-15). Live multiple-Folder advisory latency is DW-80 (feeds 8.9); traces cut by the 2 s timeout render truthfully as incomplete.
- **Scheduling guard.** 6.8 may take any status. 6.6 may leave `backlog`, even for `blocked`, only when 6.8 is `done` and DW-55 is `status: done` (one word, date on a separate `closed:` line); closing 6.8 closes DW-55. 6.7 stays pinned at `backlog` and readiness stays `NOT_READY`. 6.8 is also a prerequisite for 8.4 and 8.5. Edit sprint status by hand, never with `sprint_plan.py`.
- **Story 6.6 verification lane.** The individual Debug test-project lane now covers 6.6; no common runner. Build each Projects test project the spec names with `--configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers` and run the built xUnit v3 assembly with native XML. In McpCli, run `DependencyPolicyTests`, `EnrolledModuleCoverageTests`, and `tools/conformance-vectors/v1/check_enrolled.py` in Release. Record totals and tested Projects and McpCli revisions in `evidence/epic6/6.6-cli-read-surface.{trx,json}`. G-6 is not a 6.6 prerequisite.
- **Deferred work (none gates 6.8 or 6.6).** DW-83: main `build-and-test` is red from FrontComposer 6.0.0-vs-5.0.0 manifest drift, and G-6 fails again; 6.8 must not hide it, and records against it if its manifest regeneration clears the drift. DW-82: publish the decorated Contracts and land the enrollment on McpCli `main`. DW-81 is closed.
- **Scope boundaries.** Story 8.4 owns the mutating CLI, admission, option validation, and `refresh-context`; 8.5 owns MCP agent flows; 8.1 turns on audit. Epic 8 owns packages, production identity, resilience, deployment, and release acceptance.
- **Done rule.** Done means the story's own criteria pass at a recorded commit (green CI or an equivalent recorded local run, with exact commands, totals, XML, and tested commits). Upstream movement never reopens it; new gates become deferred-work entries or stories. Unfinished critical evidence never counts as passing.
