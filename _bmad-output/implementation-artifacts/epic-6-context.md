# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorized Project list, open, Conversation-start, context, and advisory reads through supported EventStore read models. Expose them through authenticated FrontComposer Web and CLI read surfaces, then move read routing over in a reversible way. Chatbot and operators get current Project truth with no consequential write authority; writes belong to Epic 7, and production release is an Epic 8 decision. Stories 6.1–6.5 are done. Stories 6.6–6.8 are in backlog, and 6.6 cannot start until 6.8 lands.

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

- **Authorization.** Bind every read to the authenticated Tenant, the original actor, the workload and delegation, the action, and the target. The acting session and every referenced resource must be in the Project's Tenant. Chatbot Project User visibility comes from current Folder read authority. Tenant-role reads (Tenant Operator, Tenant Project Administrator) go through Web, CLI, and MCP. Owners reauthorize their own resources. Discovery, cached evidence, and service identity never grant actor access. Filter by authority before paging.
- **Metadata and audit.** Tenant roles get Safe Metadata by default. Project name, Setup bodies, titles, paths, and Preview bodies need separate inspection permission and a durable, metadata-only inspection audit. The quarantined-legacy inventory can be read with one inspection event per Project. Denied, cross-Tenant, and absent targets all return the same safe `404`. Errors, logs, telemetry, and evidence carry Safe Metadata only, and no rejected value is echoed back.
- **Operator reads (Web and CLI).** These cover inventory, detail, reference health, and current resolution traces only. Read permission grants neither Safe Diagnostic Export nor any mutation. Audit truth arrives with Story 8.1. Until then, an audit tab shows "not yet available", and an `audit` command is either absent or returns one stable unsupported-capability result without querying.
- **CLI output.** Commands return deterministic machine-readable JSON, Tenant-scoped and filtered by authorization. Exit codes are stable, and denial, absence, and stale or unavailable models return a stable non-zero code with a safe reason code. No meaning may depend on color, terminal width, or prose. Given the same facts, CLI output must match the Web surface on lifecycle and reference states, reason codes, timestamps, and warnings. Reject unknown, duplicate, and unsupported options deterministically.
- **Lifecycle and snapshot.** Projects are Active or Archived. Pre-activation tasks are never listed as Projects. Legacy folderless Projects stay quarantined, and no read silently repairs state. List and open return one snapshot: `responseState`, `asOf`, the disclosable `projectVersion`, components (inclusion, freshness, reason, last-verified), and `recoveryActions`. Pages default to 50 rows with a cap of 200, use deterministic order and scoped cursors, and never select a resolution candidate.
- **Context, advisory, persistence.** Context binds to the named Conversation's member Project, or to the Project opened in that session. Advisory results are unselected, show explicit incomplete coverage (never an exhaustive NoMatch), and cannot authorize context, first response, or confirmation. Reads append no domain events and persist no inference, traces, or exports. Selection Evidence never appears on a read response.

## Technical Decisions

- **Runtime.** Projects supplies contracts, projections, query handlers, and the Reference Trust Index. EventStore DomainService supplies the runtime, read models, and cursor codec through `IDomainQueryHandler`, `IReadModelStore`, and `QueryCursorScope`. Do not add a Projects-owned runtime or persistence protocol.
- **Contracts.** `Hexalith.Projects.Contracts` is dependency-light and owns the operations, wire vocabulary, and security semantics. It must not depend on FrontComposer, Fluxor, Fluent UI, `Microsoft.AspNetCore.App`, Dapr, or Aspire. The non-packable `Hexalith.Projects.UI.Contracts` holds presentation descriptors only. A CLI-only or MCP-only consumer must pull no Blazor or FrontComposer dependency, even transitively.
- **CLI ownership.** `Hexalith.McpCli` is the target Hexalith CLI and MCP adapter. Do not create a new per-module CLI or MCP server. `Hexalith.Projects.Cli` and the Projects MCP plug-in are frozen migration sources that take safety and continuity fixes only. Their inventory rows stay `defer / exclude` until an owner approves a change.
  - An enrolled read needs a decorated Contracts Query, an exact Gateway route, and authorization and behavior evidence. McpCli v1 adds only decorated Commands and Queries, with no module-specific adapters.
  - Any change in McpCli or another sibling repository needs that repository's own authorization and validation.
- **CLI confirmation.** McpCli refuses human-confirmation and UI-only actions, cannot mint Selection Evidence, and cannot be promoted to a confidential interactive client. Some older text says CLI confirmation turns on after the interactive-session claim; treat that as denied. Story 6.6 is read-only, so every confirmation-required action returns a safe denial.
- **Transport.** Authorized reads return `200` with `Complete`, `Partial`, or `Unavailable`. Denied and nonexistent targets return the same safe `404`. Invalid input returns `400`. Every adapter, CLI included, applies the same role and action matrix and can only narrow it.
- **Identity.** The server derives Tenant and actor and never trusts arguments or headers for them. The CLI is a dual-principal adapter: it carries the actor's session and can neither originate nor self-confirm actions. Allow-all authentication exists only in explicit test composition, and incomplete authentication configuration fails startup.
- **Cutover.** Story 6.7 owns it: shadow-read comparison first, reversible routing, immutable history, and a single writer.

## UX & Interaction Patterns

- **Shared vocabulary.** Chatbot, Web, CLI, and MCP use the same response, freshness, safe-reason, and recovery meanings, with no local synonyms. Show incomplete advisory coverage without preselecting a candidate.
- **CLI commands.** The read-only commands are `list`, `describe`, `inspect`, `trace` (`trace-resolution`), and `validate` (`validate-references`). `describe` gives a complete safe-metadata snapshot of one Project, in the style of `kubectl describe`. A readable table is optional, but JSON is always available for automation and assistive tools.
- **Web.** The Web surface uses FrontComposer and Fluent UI. It must stay usable by keyboard and screen reader, at 200% zoom, and with 320px reflow.

## Cross-Story Dependencies

- **Story 6.5 closure.** 6.5 closed on 2026-11-15 on its own criteria. Live multiple-Folder advisory latency moved to DW-80, which feeds Story 8.9. Under the 2 s interactive timeout, advisory traces may come back incomplete. Render that truthfully; no positive live multiple-Folder result is claimed.
- **Story 6.6 prerequisites.** 6.8 must land first; it is also a prerequisite for 8.4 and 8.5. The production-authority guard pins 6.6–6.8 at `backlog`, so moving either story forward needs an approved correction that updates the guard. Edit sprint status by hand, never with `sprint_plan.py`.
- **Story 6.6 verification lane.** The story names the common-runner lane `hexalith-module test --profile cli-reads`, and that runner is not delivered yet (6.1-P0 stages 2–7 are open). The individual Debug test-project exception covers only 6.1, 6.4, and 6.5, so 6.6's spec must settle its lane. DW-81 also applies: the McpCli gitlink targets net10.0, but `Hexalith.Commons.UniqueIds` 3.0.0 is net11.0-only, so G-6 restore fails with `NU1202`.
- **Scope boundaries.** Story 8.4 owns the full CLI contract, including mutations, admission, `refresh-context`, and option-validation evidence. Story 8.5 owns MCP, and Story 8.1 turns on the audit command. Epic 8 owns packages, production identity, resilience, deployment, and release acceptance.
- **Done rule.** A story is done when its own criteria pass at a recorded commit, with green CI or an equivalent recorded local run. Record the exact commands, totals, XML, and tested commits. Later upstream movement does not reopen a story; new gates become deferred-work entries or new stories. Unfinished critical evidence is never marked as passing.
