# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorization-filtered Project list, open, Conversation-start, context, and resolution reads through named incremental EventStore DomainService read models and a rebuildable Reference Trust Index. Authenticated Chatbot, FrontComposer Web, and CLI consumers receive the same metadata-only truth before a shadow-read-first, reversible cutover makes supported models authoritative. This is the read side of the vertical slice; consequential writes remain in Epic 7.

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

- Every read is scoped by server-derived Tenant, original actor, delegated workload, action, target, and current version authority. Queries and resource owners reauthorize. Denied, cross-Tenant, and nonexistent targets are boundary-indistinguishable safe 404 responses; outputs, errors, logs, telemetry, and evidence remain metadata-only.
- List, open, resolution, context, and Conversation-start results share the AD-32 snapshot: `responseState`, `asOf`, authorized `projectVersion` when disclosable, metadata-only `components`, and `recoveryActions`. `Complete` requires all required evidence current. `Partial` is usable only when Project, Folder, Setup, and first-response authorization evidence are current and every optional omission is explicit. `Unavailable` blocks use; `Denied` discloses no protected detail. Chatbot admits a first response only for `Complete|Partial`.
- Reads use deterministic ordering and scope-bound opaque cursors (default 50, maximum 200), support the declared Project/reference scale, and expose stale, rebuilding, missing, or unavailable evidence honestly. Refresh and resolution are read-only; resolution explanations are request-scoped and not persisted. No event-history rewrite, unsafe dual writer, sibling payload copy, raw content, secrets, or unrestricted paths.
- Story 6.8 removes the `Contracts` package's UI dependency boundary before the CLI surface can be accepted. Web uses the approved FrontComposer boundary; all surfaces preserve the same server semantics while presentation formatting may differ.

## Technical Decisions

- Projects owns stable versioned contracts, domain policy, query handlers, incremental projections, the Reference Trust Index, and presentation descriptors. EventStore DomainService/platform owns persistence, identity admission, topology, telemetry, generated adapters, cursor codecs, safe denial, and runtime plumbing. Projects must not recreate those layers or use direct Dapr state access.
- Use the platform's immutable dual-principal query envelope for Tenant, actor, workload, delegation, scopes, audience, action, target, and version authority. Authorization precedes protected validation or lookup, and owner reauthorization can narrow but never widen authority.
- Use rebuildable persisted read models with deterministic replay, optimistic write policy, truthful watermark/provenance, and no custom journal or parallel trust store. Preserve legacy routes for shadow comparison and rollback until Story 6.7 proves equivalence of values, keys, watermarks, cursors, and Tenant isolation.
- Use the shared AD-32 state, reason, component, freshness, version, and recovery vocabulary. Conversation-start returns only goals, instructions, context preferences, and default linked-source policy for Active Projects; archived or unauthorized targets are safe-denied.

## UX & Interaction Patterns

- FrontComposer Web is an authenticated, read-only operational surface. It distinguishes absence, denial, stale, rebuilding, and unavailable states without exposing sibling payloads or implying unavailable capabilities. It must meet WCAG 2.2 AA, including keyboard and assistive technology use, visible focus, non-color-only meaning, 200% zoom, and 320 CSS-pixel reflow.
- CLI output is deterministic JSON with stable exit codes and no color-dependent meaning. Web, CLI, MCP, and Chatbot adapters preserve the same states, reason codes, timestamps, recovery actions, authorization, and redaction rules.

## Cross-Story Dependencies

- All read stories require the applicable accepted Story 6.1 prerequisite chain: P0 supported runner/evidence, historical P1, current P1R baseline, P2 query/security capabilities, P3 production identity contract, and P4 exact clean-checkout gate. Production implementation remains blocked until the chain, Solution Architect conformance, Story 6.1 readiness, and an independent readiness rerun are accepted.
- Stories that consume Conversations, Folders, Memories, Parties, or Tenants require their owner-approved read contracts. Story 6.5 additionally requires the FrontComposer contract; Story 6.6 requires the CLI adapter contract and Story 6.8 first. Story 6.7 is the only cutover story; Epic 7 depends on this read boundary, while Epic 8 supplies later operational and release capabilities.
