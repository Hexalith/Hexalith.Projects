# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorization-filtered Project list, open, Conversation-start, assembled context, and resolution reads through named incremental EventStore DomainService read models and a rebuildable Reference Trust Index. Authenticated Chatbot, FrontComposer Web, and CLI consumers receive the same metadata-only truth. A shadow-read-first, reversible cutover then makes those models authoritative, and packable Contracts stop carrying FrontComposer/UI dependencies. This is the read side of the vertical slice; consequential writes remain in Epic 7.

## Stories

- Story 6.1: List and open Projects through supported authenticated paths
- Story 6.2: Retrieve Conversation-start setup with admission truth
- Story 6.3: Retrieve assembled Project Context through supported read models
- Story 6.4: Resolve Projects with transient current explanations
- Story 6.5: Inspect Projects through an authenticated FrontComposer read surface
- Story 6.6: Inspect Projects through an authenticated CLI read surface
- Story 6.7: Cut over supported reads while preserving compatibility and rollback
- Story 6.8: Split Hexalith.Projects.UI.Contracts from the packable Contracts package

## Requirements & Constraints

- Every read is scoped by server-derived Tenant, original actor, delegated workload, action, target, and current version. Queries and resource owners reauthorize. Denied, cross-Tenant, and nonexistent targets are indistinguishable safe 404 responses. Outputs, errors, logs, telemetry, and evidence remain metadata-only.
- List, open, resolution, context, and Conversation-start results share one snapshot: `responseState`, `asOf`, authorized `projectVersion` when disclosable, conditional `resolutionResult`, metadata-only `components`, and `recoveryActions`. `Complete` requires all required evidence current. `Partial` is usable only when Project, Folder, Setup, and first-response authorization evidence are current and every optional omission is explicit. `Unavailable` blocks use; `Denied` discloses no protected detail. Chatbot admits a first response only for `Complete` or `Partial`. Recovery actions are exactly `None`, `Retry`, `RefreshContext`, `RequestPreview`, `RenewPreview`, `PollTask`, `ResolveNeedsAttention`, `SelectAlternative`, and `ContactAdministrator`.
- A Project is usable as context or a default resolution candidate only when it is Active, read-model-confirmed, and bound to exactly one authorized Folder. Pre-activation work never appears in list or open. Archived Projects stay listable for history, cannot silently become Conversation context, and are excluded from resolution unless explicitly requested. List rows carry identity, name, lifecycle, version, Folder availability, and snapshot fields needed to choose a Project without loading full context.
- Reads use deterministic ordering and authenticated opaque cursors (default 50, maximum 200). They must support 10,000 Projects per Tenant and 5,000 references per Project, with metadata-read p95 under 500 ms at 1,000 Projects / 500 references. Stale, rebuilding, missing, or unavailable evidence is reported honestly. Refresh is a read-only recompute that returns a new snapshot and creates no confirmation, task, mutation, or maintenance audit. A retained `reevaluate` alias maps exactly to that refresh.
- Context assembly is allowlist-based: include a reference only after Tenant, project, lifecycle, authorization, and freshness all pass; exclusions carry a shared-vocabulary state and reason. Conversation-start returns only goals, user-facing instructions, context preferences, and default linked-source policy for Active Projects.
- Resolution is compute-on-demand and never silently attaches. Outcomes are `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with reasons `ConversationLinked`, `ProjectFolderMatched`, `FileReferenceMatched`, `MemoryMatched`, and `MetadataMatched`. Traces are request-scoped and not persisted. Matching uses authorized identity and metadata, never file contents. Confirmation and proposal writes belong to Epic 7.
- Operator read permission grants neither Safe Diagnostic Export nor mutation. Until later audit capability is accepted, audit views and commands must show an explicit unsupported/not-available capability and must not query or imply audit data.
- Contracts stay additive and serialization-tolerant. Historical v1 data and unversioned name-only creation remain readable. No event-history rewrite and no unsafe dual writer. The canonical metadata-classification create contract is aligned at read cutover so Epic 7 can consume it.

## Technical Decisions

- Projects owns versioned contracts, domain policy, query handlers, incremental projections, the Reference Trust Index, and presentation descriptors. EventStore DomainService/platform owns persistence, identity admission, topology, telemetry, generated adapters, cursor codecs, safe denial, and runtime plumbing. Projects must not recreate those layers or use direct Dapr state access. Chatbot owns end-user candidate and proposal presentation. Conversations, Folders, and Memories retain resource authority.
- Queries use `IDomainQueryHandler` with authenticated `QueryCursorScope`. Incremental projections use `IAsyncDomainProjectionHandler` over `IReadModelStore`/`IReadModelBatchStore` with explicit `ReadModelWritePolicy`. The rebuildable Tenant-scoped Reference Trust Index stores only safe owner metadata, owner version/watermark, authorization outcome, and freshness. Normal context uses `Current` evidence; explicit refresh uses owner batch APIs with bounded concurrency.
- Use the platform's immutable dual-principal query envelope. Tenant and actor are never taken from body or header substitutes. Authorization precedes protected lookup. Owner reauthorization can narrow but never widen authority. Delegated Chatbot or service callers inherit the original actor and never gain end-user confirmation authority.
- Identifiers for governed work are platform-generated ULIDs. Foreign owner IDs stay opaque and are never GUID-parsed. Persisted legacy IDs remain readable, including historical `ProjectFolderCreationPending` replay.
- Cutover is shadow-read first: compare legacy and supported models for values, keys, watermarks, cursors, and Tenant isolation, then switch read routes independently with reversible routing. Legacy routes stay until Epic 8 release acceptance. Command and writer cutover is Epic 7.
- Packable `Hexalith.Projects.Contracts` owns commands, events, identifiers, DTOs, enums, admission classification, and domain/wire vocabulary. It must not depend on FrontComposer, Fluxor, Fluent UI, ASP.NET Core, Dapr, or Aspire. Non-packable `Hexalith.Projects.UI.Contracts` depends inward on Contracts and owns presentation metadata only; it cannot redefine operations, vocabulary, or security.
- Authorized reads return `200` with `Complete`, `Partial`, or `Unavailable`. Denied and nonexistent collapse to safe `404`. Invalid input is `400`. Adapters must not invent local state or recovery synonyms.

## UX & Interaction Patterns

- FrontComposer Web is an authenticated, read-only operational surface over generated inventory, detail, reference-health, and current-resolution views. Credentials come from the platform identity provider, never the client. Views distinguish absence, denial, stale, rebuilding, and unavailable states without exposing sibling payloads or implying unavailable capabilities, including audit. Status is never color-only. WCAG 2.2 AA applies: full keyboard use, visible and restored focus, accessible names, live-region announcements, 200% zoom, and 320 CSS-pixel reflow.
- CLI read commands are `list`, `describe`, `inspect`, `trace`, and `validate`. Output is deterministic JSON with stable exit codes and no color-dependent meaning. Denied or absent targets use a safe reason and a non-zero stable exit code.
- Web, CLI, MCP, and Chatbot adapters preserve the same states, reason codes, timestamps, recovery actions, authorization, and redaction rules. Presentation layout may differ; semantics may not.

## Cross-Story Dependencies

- Production implementation of every read story remains blocked until the Story 6.1 prerequisite chain is accepted: historical P1 evidence, current P1R pin, P0 supported runner and evidence, P2 query and security capabilities, P3 production identity contract, same-baseline Solution Architect conformance, P4 exact clean-checkout gate, Story 6.1 specification readiness, and an independent readiness result of exactly READY.
- Stories that consume Conversations, Folders, Memories, Parties, or Tenants additionally require those owners' approved read contracts. Story 6.5 additionally requires the approved FrontComposer contract. Story 6.6 requires the CLI adapter contract and Story 6.8 first.
- Story 6.7 is the only read-cutover story and depends on shadow-read equivalence across all Epic 6 reads plus aligned ULID, OpenAPI, and generated-consumer contracts. Epic 7 depends on this read boundary and the canonical create-contract alignment. Epic 8 supplies audit, export, mutating operator surfaces, MCP mutation containment, and release evidence.
