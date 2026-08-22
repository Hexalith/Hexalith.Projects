# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorization-filtered Project list, open, Conversation-start, context, and resolution reads through supported EventStore DomainService read models and a rebuildable Reference Trust Index. Authenticated Chatbot, FrontComposer Web, and CLI consumers receive the same metadata-only truth before a shadow-read-first, reversible cutover makes the supported models authoritative. This is the read side of the vertical slice; consequential writes remain in Epic 7.

## Stories

- Story 6.1: List and open Projects through supported authenticated paths
- Story 6.2: Retrieve Conversation-start setup with admission truth
- Story 6.3: Retrieve assembled Project Context through supported read models
- Story 6.4: Resolve Projects with transient current explanations
- Story 6.5: Inspect Projects through an authenticated FrontComposer read surface
- Story 6.6: Inspect Projects through an authenticated CLI read surface
- Story 6.7: Cut over supported reads while preserving compatibility and rollback

## Requirements & Constraints

- Every read is scoped by server-derived Tenant, original actor, delegated workload, action, target, and current version authority; queries and resource owners reauthorize. Denied, cross-Tenant, and nonexistent targets are boundary-indistinguishable safe `404` responses. Outputs, errors, logs, and evidence remain metadata-only.
- List, open, resolution, context, and Conversation-start results share `responseState`, `asOf`, authorized `projectVersion` when disclosable, metadata-only `components`, and `recoveryActions`. `Partial` is usable only when Project, Folder, Setup, and first-response authorization evidence are current and every omission is explicit; `Unavailable` blocks use; `Denied` discloses nothing protected.
- A context-usable Project has exactly one authorized Folder confirmed by the read model. References pass Tenant, Project, lifecycle, authorization, and freshness checks; exclusions stay explicit. Refresh never mutates. Resolution returns an unselected `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with a non-persisted request-scoped explanation.
- Queries use deterministic ordering and scope-bound opaque cursors (default 50, maximum 200). Metadata reads support 10,000 Projects per Tenant and 5,000 references per Project, targeting p95 below 500 ms at the median declared shape and below one second at the supported maximum. Contracts remain additive; historical data and identifiers stay readable without history rewrite or an unsafe dual writer.

## Technical Decisions

- Projects owns stable contracts, domain policy, query handlers, incremental projections, the Reference Trust Index, and presentation descriptors. EventStore DomainService/platform owns persistence, cursors, identity admission, topology, telemetry, and generated adapters; Projects does not recreate those layers.
- The rebuildable Trust Index contains only safe owner metadata, authoritative version or watermark, authorization outcome, and freshness. Sibling contexts retain resource authority. Governed identities are platform-generated ULIDs; foreign identifiers stay opaque. OpenAPI and consumers are generated from versioned contracts and live-host verified.
- Read routing switches only after shadow equivalence proves output, keys, watermarks, cursors, and Tenant isolation across all Epic 6 reads. The legacy route remains reversible through release acceptance and is restored when equivalence or post-cutover validation fails.

## UX & Interaction Patterns

- Web is a FrontComposer/Fluent UI operational console for Project inventory, detail, reference health, and current resolution traces. It distinguishes absence, denial, stale, rebuilding, and unavailable states without exposing sibling payloads or implying unavailable later capabilities.
- Web journeys meet WCAG 2.2 AA for keyboard and assistive-technology use, visible focus, non-color-only meaning, 200% zoom, and 320 CSS-pixel reflow. CLI reads emit deterministic JSON with stable exit codes and no color-dependent meaning; both surfaces preserve the same facts and safe vocabulary.

## Cross-Story Dependencies

- Story 6.1 and later shared-read consumers remain blocked until P0, historical P1, current P1R, P2, P3, Solution Architect conformance, and P4 are accepted on one reproducible baseline. P1R precedes P0 and P2; P3 follows P2; P4 records immutable pins, approvals, clean-checkout evidence, and executable rollback. An independent readiness result must then be exactly `READY`.
- Historical P1 is accepted on EventStore/Builds/Architecture `3.70.1`, but it does not authorize the drifted candidate. P1R is open: the source-mode candidate is `7854f8e51ce9b852bb6c3cac6012670122e93792` (`v3.89.0-9-g7854f8e5`), while the package candidate is tag `v3.89.0` at `c590590bc581a3f72ef6e67148eda988ba4b8fe6`; the Builds catalog selects `3.89.0` from `10af541e7b2a5a4664be37c9495930844e0954a8`. These coordinates are intentionally distinct and remain pending owner selection, qualification, atomic alignment, and four-owner acceptance.
- G-4 is open. Runner source remains the superseded, unaccepted `3.88.0` candidate observed at Builds `7bdbd293991985d150dfca62f77709e61152de76`; published `4.23.0` tools from `7ac2849d79e603b88c7cb76e178cd2ba106eaf00` embed EventStore `3.70.1` and are not the supported candidate consumer. The Architecture Spine and rollback remain `3.70.1`; Projects has no accepted tool/module manifest or persisted P0 qualification. Do not rebind architecture or pass G-4 evidence before P1R and P0 acceptance.
- Sibling-resource reads additionally require their applicable owner contracts. Web requires the approved FrontComposer boundary, CLI its adapter contract, and affected runner/evidence lanes G-6 toolchain alignment. Epic 7 consumes the read boundary; Epic 8 supplies later audit/maintenance/release capabilities and introduces no backward dependency into Epic 6.
