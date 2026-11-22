# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Serve authorized Project list, open, Conversation-start, context, and advisory reads through supported EventStore read models and authenticated Web/CLI surfaces, then switch read routing reversibly. This establishes the identity, contract, and platform boundary needed by subsequent work. Consequential writes belong to Epic 7; production qualification and release belong to Epic 8. The approved November 16 scheduling correction permits Story 6.7 to progress while historical readiness remains `NOT_READY`.

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

- Bind authorization to the authenticated Tenant, original actor, workload, delegation, action, target, and current evidence. Project, session, and references must share one Tenant. Chatbot visibility follows current Folder read authority; filter before pagination. Discovery rows, cached suggestions, and service identity confer no permission.
- Return Safe Metadata by default. Descriptive Metadata needs explicit inspection authorization and durable metadata-only inspection audit; failed audit denies disclosure. Logs, errors, telemetry, and evidence disclose no protected payload or rejected value. Denied, cross-Tenant, and nonexistent targets are indistinguishable through safe `404` responses.
- Applicable responses preserve `responseState`, `asOf`, authorized `projectVersion`, components, and recovery actions. Required non-current evidence makes context `Unavailable`; optional omissions remain explicit. List pages default to 50 and cap at 200. Historical folderless or pending Projects remain quarantined rather than repaired by reads.
- Context, explanation and Conversation-start require current native membership for the named Conversation and Project under the original actor and any delegation. With readable Project authority, missing or unavailable membership returns `Unavailable` and withholds Setup, references and first-response admission. Get and Explain use Project-owned persisted detail. Explanations are transient Safe Metadata. Refresh is a retained v1 read.
- Advisory matching may lag committed metadata. Recheck owner authorization at request time, omit names and protected exclusion details, report incomplete coverage honestly, and never preselect a candidate. Suggestions confer no context, first-response, or confirmation authority.
- Keep contracts additive and serialization-tolerant. Historical v1 data and unversioned name-only creation remain accepted/readable throughout v1. Retirement requires a major version, migration notice, usage evidence, compatibility tests, and rollback evidence. Never rewrite event history.

## Technical Decisions

- Projects owns domain contracts, projections and handlers. DW-85 covers supported read-only Refresh, the rebuildable Tenant-scoped Reference Trust Index and owner-backed Folder revocation. The delivered Get and Explain reads observe a later Folder revocation when their persisted evidence refreshes. EventStore owns runtime, storage, query/projection plumbing, and scoped opaque cursors. New reusable infrastructure belongs to the technical module.
- Contracts are the sole editable operation, schema, security, and vocabulary authority. Generated OpenAPI, clients, schemas, and runtime descriptors must align mechanically and be verified against the host. Contracts remains dependency-light; non-packable UI.Contracts contains inward-dependent presentation metadata only.
- Platform-governed identities use ULIDs. Persisted legacy IDs stay readable and owner-defined foreign IDs remain opaque; never GUID-parse them. Historical `ProjectFolderCreationPending` deserialization and replay remain supported.
- Before routing changes, inventory events, state keys, read models, cursors, routes, clients, identities, consumers, in-flight work, and partial records. Exhaustive comparison across seven delivered Epic 6 reads consumes accepted list/open output and the existing comparator, covering canonical output, keys, exact watermarks, cursor boundaries/scopes and Tenant isolation. The selector requires `story67-read-normalization-2026-11-22-v4`. The v3 pass-2 packet is historical. Requalification requires 77 distinct executed cells and seven explicit retained rollback reads bound to the new packet hash. The conditional comparison rules still apply: missing Setup never becomes empty without current-empty proof; timestamps must represent the same observation; keys/cursors retain independent checks. Supported-only admission fields need independent evidence; safety differences are not normalized away. [Current cutover rules and evidence](../../docs/read-cutover.md) · [Approved course correction](../planning-artifacts/sprint-change-proposal-2026-11-22.md)
- Switch read routes only after equivalence and identity/contract alignment pass. The approved 2026-11-16 decisions select existing Gateway typed consumption and one explicit primary-Web routing choice, preserving v1 REST/client signatures. Block activation on failed equivalence; an operator explicitly rolls back only to a security-verified legacy path, otherwise affected reads are unavailable. Retain legacy until Epic 8 acceptance and record blockers honestly. Read rollback does not authorize writer rollback. Whole-host migration DW-63 stays open under Projects/EventStore owners and gates Story 7.1 writer cutover, with host/route, authorization, projection, read-regression and AD-17 proof; current query hosting remains for 6.7.
- `Hexalith.McpCli` is the shared CLI/MCP target. Four decorated read operations use the EventStore Gateway; no proprietary per-module expansion or consequential agent confirmation. Authorized `Partial`/`Unavailable` results remain results with exit `0`; errors use exit `2`. CLI and Web preserve equivalent lifecycle, reference, reason, timestamp, and warning semantics. Audit reads wait for Story 8.1.

## UX & Interaction Patterns

All surfaces preserve shared response, component inclusion, freshness, reason, and recovery vocabulary. Incomplete empty advisory output never means exhaustive NoMatch. `Unavailable` blocks context and first response. Web uses FrontComposer and Fluent UI, with keyboard/screen-reader support, visible focus, non-color status, 200% zoom, and 320 CSS-pixel reflow.

## Cross-Story Dependencies

- The November 16 correction supersedes the previous 6.7 backlog pin. Its E1 lane builds individual Debug Projects test projects named in the spec with `--configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers`, then runs built xUnit v3 assemblies with native XML. The compatibility evidence row is `nfr-10`; retain read-cutover JSON and rollback-drill evidence.
- Focused feature acceptance does not require unrelated publication or global qualification. Actual read/security defects still block the affected path. The common runner, publication, clean-checkout/package parity, and replacement local lanes still gate removal of legacy runtime projects; the E1 exception grants no runtime retirement.
- Epic 7 needs accepted read cutover with executable rollback and retains the capable single-writer boundary. Epic 8 retains package, production identity, performance, resilience, deployment, and terminal release acceptance. No feature result changes global readiness or advances those gates.
- A story is done when its own criteria pass at a recorded commit, with green CI or an equivalent recorded local run. Later dependency movement does not reopen it. Preserve historical evidence; route newly discovered independent gates to deferred work or new stories. Edit sprint tracking by hand rather than regenerating it.
