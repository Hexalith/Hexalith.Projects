# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorized Project list, open, Conversation-start, context, and advisory reads through supported EventStore read models; expose them through authenticated operator surfaces and cut over read routing reversibly. This gives Chatbot and operators current Project truth without granting consequential write authority. Production release remains an Epic 8 decision.

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

- Bind each read to authenticated Tenant, original actor, workload/delegation, action, and target. Chatbot Project User visibility derives from current Folder read authority; Tenant-role reads belong to Web, CLI, and MCP. Owner systems reauthorize their resources. Discovery, cached evidence, and service identity never grant actor access. Filter authority before paging.
- Safe Metadata is the Tenant-role default. Project names and other Descriptive Metadata require separate inspection permission and a durable metadata-only inspection audit. Denied, cross-Tenant, and absent Projects collapse to indistinguishable safe `404`; errors, logs, telemetry, and evidence cannot disclose protected detail.
- Project lifecycle is Active or Archived. Pre-activation tasks are never listed as Projects. Legacy Active records without a valid Folder remain quarantined; authorized Archived recovery inspection cannot admit context. No read silently repairs state.
- List and open carry one truthful response snapshot: response state, `asOf`, disclosable Project version, components with inclusion/freshness/reason/last-verified evidence, and recovery actions. Current authorized list enumeration is `Complete` even when individual entries are `Unavailable`; missing required open evidence is `Unavailable`. Default page size is 50, capped at 200, with deterministic ordering and authenticated scoped cursors. No read selects a resolution candidate.
- Conversation-start and context reads bind to the named Conversation's member Project, or, without membership, one explicitly opened in that session. Context includes only same-Tenant authorized references allowed by Setup policy and never owner payloads. `Partial` requires current Project, Folder, Setup, and first-response authority plus explicit optional omissions; `Unavailable` blocks first response. Refresh recomputes without mutation.
- Advisory resolution may match lagging committed metadata, but every disclosed candidate needs current owner authorization. Return unselected IDs, scores, safe reasons, evaluation time, and explicit incomplete coverage; never claim exhaustive NoMatch for incomplete evidence. Suggestions and traces cannot authorize context, first response, or confirmation. Real File identity is required; a missing mapping remains incomplete rather than guessed.
- Reads append no domain events or persisted inference and preserve additive contracts, history, and opaque foreign IDs. Verify persisted end-state, authorization and Tenant isolation, replay/restart, cursor scope/tampering, and payload leakage. Required security and inspection audit is a separate durable effect.

## Technical Decisions

- Projects supplies domain contracts, incremental projections, query handlers, and rebuildable Reference Trust Index; EventStore DomainService supplies runtime, persisted read models, batches, and cursor codec. Use `IAsyncDomainProjectionHandler`, `IReadModelStore`/`IReadModelBatchStore`, `IDomainQueryHandler`, and `QueryCursorScope`. Do not add a Projects-owned runtime or persistence protocol.
- Dependency-light `Hexalith.Projects.Contracts` owns canonical operation and wire semantics. Non-packable `Hexalith.Projects.UI.Contracts` owns presentation descriptors without redefining them. `Hexalith.McpCli` is the target CLI/MCP adapter; legacy module adapters are migration sources.
- Shadow-read-first cutover compares canonical output, keys, watermarks, cursor behavior, and Tenant isolation with finite approved normalization. Read routing remains reversible, event history immutable, and command writing single-owner. Story 6.7 owns public read-route equivalence and rollback; Epic 7 owns consequential work.

## UX & Interaction Patterns

- Use shared response, freshness, safe-reason, and recovery meanings across Chatbot, Web, CLI, and MCP. Show incomplete advisory coverage without preselection. Web uses FrontComposer and Fluent UI; CLI uses deterministic JSON, stable exit codes, and no color-dependent meaning. Keep keyboard, screen-reader, 200% zoom, and 320px reflow usable. Audit views state capability unavailable until accepted.

## Cross-Story Dependencies

- Story 6.1 needs usable authenticated SDK query/projection/store/cursor behavior and a real fail-closed gateway-to-Folders actor authority path. Focused readiness and recorded feature evidence suffice; package publication, global READY, production enrollment, historical writer rollback, and the superseded P4 ceremony are separate obligations. Its own acceptance still requires current Folder filtering before paging, AD-32 truth, inspection audit, and persisted no-domain-write/replay/restart/cursor/authority-change proof.
- Stories 6.2–6.4 are recorded done. Story 6.5 requires accepted 6.1–6.4 contracts and handlers, usable FrontComposer server authentication/current-Tenant/per-circuit credentials, and DW-1 completion; it remains blocked. Story 6.6 requires the 6.8 Contracts split. Story 6.3's single-slice design must be proven or split by an approved correction.
- A story is done when its own criteria pass at a recorded commit with green CI or an equivalent recorded local run. Record exact commands, totals, XML, and tested source/dependency commits. Later upstream movement does not reopen it; new gates become separate work. Epic 8 owns package, production identity, resilience, deployment, and terminal release acceptance; unfinished critical evidence cannot be marked passing.
