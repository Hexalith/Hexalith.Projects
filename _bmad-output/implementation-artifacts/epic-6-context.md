# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorization-filtered Project list, open, Conversation-start, assembled-context, and resolution reads through supported incremental EventStore DomainService read models and a rebuildable Reference Trust Index. Authenticated Chatbot, FrontComposer Web, and CLI consumers receive the same metadata-only truth; shadow-read comparison and reversible cutover then make supported reads authoritative. This is the read/query side of the vertical slice—consequential writes remain in Epic 7—and it also removes UI dependencies from the packable Contracts boundary needed by later adapters.

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

- Every read is scoped by server-derived Tenant, actor, action, target, and version, with current owner-system authorization rechecked. Denied, cross-Tenant, and nonexistent targets are indistinguishable safe `404` outcomes. Responses, errors, logs, telemetry, explanations, and evidence expose Safe Metadata only; protected names, paths, Setup bodies, and titles require separately authorized, audited descriptive-metadata inspection.
- List, open, context, Conversation-start, and resolution use one snapshot contract: `responseState`, `asOf`, safely disclosable `projectVersion`, metadata-only `components`, conditional `resolutionResult`, and `recoveryActions`. `Complete` is usable; `Partial` is usable only when the Project, Folder, Setup, and first-response authorization evidence are current and omissions are explicit; `Unavailable` blocks context use; `Denied` discloses no protected detail. Chatbot admits a first response only for `Complete` or `Partial`.
- List/open is authorization-filtered and paged with opaque cursors (default 50, maximum 200). Chatbot sees only Folder-derived Projects; Tenant-role entries are operator-surface only. Pre-activation work never appears as a Project. Each row carries its own state, and non-current Folder evidence makes that row `Unavailable`. Archived Projects remain historical but cannot silently become context or automatic resolution candidates.
- Context is assembled by allowlist at query time. A reference is included only after Tenant, Project, lifecycle, owner read, and freshness checks pass; exclusions retain a safe state and reason code. Conversation-start returns only the bounded goals, instructions, context preferences, and linked-source policy for the named Conversation's member Project, or an explicitly opened Project when unmembered. Refresh is read-only recomputation and creates no task, confirmation, mutation, or maintenance audit.
- Resolution is compute-on-demand from authorized identity and metadata, never payload content. It returns only `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with current reason codes, never silently attaches, and never persists its request-scoped trace. Authorization filtering must not manufacture certainty. Confirmation and proposal writes belong to Epic 7.
- Read models must report stale, rebuilding, missing, and unavailable evidence honestly. Operator surfaces remain read-only; before Epic 8 audit capability is accepted, audit UI/commands must explicitly report unsupported or unavailable and must not query or imply audit data.
- Contracts remain additive and serialization-tolerant. Historical v1 data, legacy identifiers, and unversioned name-only creation remain readable; foreign IDs remain opaque. No event-history rewrite or unsafe dual writer is allowed. Metadata-read targets are p95 under 500 ms at 1,000 Projects/500 references and under 1 second at maximum scale.

## Technical Decisions

- Projects owns contracts, query policy, incremental projections, the Reference Trust Index, and presentation descriptors. Platform DomainService capabilities own persistence, authenticated query envelopes, cursor encoding, generated adapters, and runtime plumbing. Sibling contexts retain authority over Conversations, Folders, Memories, Parties, and Tenants; Projects accesses them through fail-closed anti-corruption boundaries.
- Queries use `IDomainQueryHandler` with an authenticated opaque `QueryCursorScope`. Tenant-scoped projections are rebuildable and carry freshness, watermark, and version evidence. The Reference Trust Index stores only safe owner metadata, authorization outcomes, and freshness; normal reads require current evidence, while explicit refresh may use bounded owner batch reads.
- All surfaces share stable lifecycle, inclusion, freshness, resolution, reason-code, and recovery vocabularies. Governed identifiers are platform-generated ULIDs; foreign identifiers are never GUID-parsed.
- Cutover is shadow-read first: legacy and supported results must match deterministically for values, keys, watermarks, cursors, and Tenant isolation before routing switches. Routing remains reversible and legacy reads remain available until Epic 8 release acceptance; command/writer cutover is deferred to Epic 7.
- Packable `Hexalith.Projects.Contracts` contains domain/wire contracts and must not depend on FrontComposer, Fluxor, Fluent UI, ASP.NET Core, Dapr, or Aspire. Non-packable `Hexalith.Projects.UI.Contracts` depends inward on Contracts and owns presentation metadata only, without redefining operations, vocabulary, or security.

## UX & Interaction Patterns

- The Web surface is an authenticated FrontComposer/Fluent UI operational console with inventory, detail, reference-health, and current-resolution views. It must distinguish true absence, denial, stale, rebuilding, and unavailable states; preserve visible Tenant scope; keep status meaningful without color; and expose no sibling payloads.
- Web read journeys must meet WCAG 2.2 AA with keyboard operation, visible/restored focus, accessible status names, screen-reader-readable grids, 200% zoom, and 320 CSS-pixel reflow. Unresolved critical or serious accessibility failures block the capability.
- CLI commands `list`, `describe`, `inspect`, `trace`, and `validate` return deterministic JSON, stable safe reason codes, and stable exit codes without color-dependent meaning. Web, CLI, MCP, and Chatbot may vary presentation but not authorization, redaction, state, reason, timestamp, or recovery semantics.

## Cross-Story Dependencies

- Production implementation remains blocked until the Story 6.1 chain is accepted: current baseline revalidation, supported runner/evidence tooling, dual-principal query and safe-denial capability, production identity contract, same-baseline Solution Architect conformance, clean-checkout gate record, Story 6.1 specification readiness, and an independent result of exactly `READY`.
- Stories consuming sibling contexts require the applicable owner-approved read contracts. Story 6.5 additionally requires the approved FrontComposer contract. Story 6.6 requires its CLI adapter contract and Story 6.8 first.
- Story 6.7 depends on equivalence evidence across all Epic 6 reads plus aligned ULID, OpenAPI, and generated-consumer contracts. Epic 7 depends on this read cutover; Epic 8 supplies audit, mutating operator surfaces, release evidence, and terminal release acceptance.
