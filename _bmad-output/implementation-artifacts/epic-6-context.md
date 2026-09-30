# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorization-filtered list, open, Conversation-start, assembled-context, and resolution reads over named incremental DomainService read models and a rebuildable Reference Trust Index. Chatbot and operators get current, fail-closed Project truth through authenticated FrontComposer and CLI surfaces, then supported reads become authoritative by shadow-read comparison and reversible routing. Consequential writes, confirmation, and command cutover stay in Epic 7. Presentation descriptors also leave packable Contracts so later non-Web adapters do not inherit UI dependencies.

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

- Reads are Tenant-, actor-, action-, and current-version-scoped from the authenticated server context. Same-Tenant is evaluated before authorization. A denied, cross-Tenant, or nonexistent single target is one safe `404`. On attachment resolution, a foreign, unauthorized, or unknown input matches an authorized input with no match and does not deny the request.
- Safe Metadata is the default for Tenant-role callers. Project name, Setup bodies, titles, paths, and Preview bodies need a separate inspection permission, and each such read is audited. Project Users receive that content for permitted Projects. Explanations use opaque surrogates only.
- List, open, context, Conversation-start, and resolution share `responseState`, `asOf`, disclosable `projectVersion`, metadata-only `components`, conditional `resolutionResult`, and `recoveryActions`. `Complete` is usable. `Partial` is usable only when Project, Folder, Setup, and first-response authorization are current and every omission is represented. `Unavailable` blocks context and Chatbot first-response admission, which allows only `Complete` or `Partial`. Neither state selects a candidate.
- List and open are authorization-filtered and cursor-paged (default 50, cap 200). Chatbot lists only Folder-readable Projects; Tenant-role entries appear only on operator surfaces. List state is `Complete` when the enumeration is current; a non-current Folder makes that entry `Unavailable`. Pre-activation work never appears as a Project. Archived Projects stay out of default resolution unless requested. Quarantined folderless Projects stay out of Chatbot list, resolution, and context; operators may inspect Safe Metadata plus, when authorized, the name and any pre-v1 Folder or creation receipt.
- Conversation-start and context serve the named Conversation's member Project, or, with no membership, only a Project opened in that session. Start setup is goals, instructions, context preferences, and default linked-source policy for one `projectVersion` and `asOf`. Inclusion requires same-Tenant, lifecycle, current owner read, and freshness; titles appear only when that read is current. Setup-marked references are required; optional omissions use `ExcludedBySetupPolicy`. Refresh returns a new snapshot and creates no task, mutation, or maintenance audit.
- Resolution uses authorized identity and metadata, never file contents, and returns only `NoMatch`, `SingleCandidate`, or `MultipleCandidates`. It selects nothing. Confirmed membership short-circuits to `ConversationLinked` and is not an inference episode. Any other sole candidate is inferred; confirmation is Epic 7. Folder-read-only Projects are read-only candidates. Hiding unauthorized siblings must not create a certain match. The trace is request-scoped and is not persisted.
- Stale or missing evidence is honest `Partial` or `Unavailable`. Operator surfaces stay read-only. Until audit capability is accepted, an audit affordance is absent or unsupported and does not imply audit data. Legacy identifiers stay readable and opaque. Metadata reads target p95 under 500 ms at 1,000 Projects and 500 references, and under one second at the supported maximum. Per-Tenant read admission is 100 per second with burst 200.

## Technical Decisions

- Projects owns contracts, query policy, incremental projections, and the Reference Trust Index. DomainService owns persistence, cursors, generated adapters, and runtime plumbing. Sibling contexts stay authoritative; a missing owner read contract fails closed.
- Queries use the platform query handler and an authenticated opaque cursor. Projections own list, detail, reference, and reverse-membership shape. The Tenant-scoped index stores safe owner metadata, watermark, authorization outcome, and freshness only. Normal context uses current evidence; refresh uses bounded owner batch reads and rechecks authorization.
- Governed identities are platform ULIDs (`Tenant/projects/ProjectId`). Dual-principal context carries the original actor and workload identity; workload identity never widens authority. Production identity, the interactive-session claim, and inspection permission are consumed platform capabilities.
- Eligible CLI and MCP access is `Hexalith.McpCli` over versioned Contracts. Do not add a proprietary module CLI or MCP server. Confirmation and selection minting stay off that surface.
- Packable Contracts must not depend on FrontComposer, Fluxor, Fluent UI, ASP.NET Core, Dapr, or Aspire. Non-packable UI contracts depend inward and own presentation metadata only. Generated OpenAPI, clients, and schemas stay generator-owned.
- Shadow reads must match on output, keys, watermarks, cursors, and Tenant isolation before routing switches. Routing stays reversible. Authorized reads return `200` with `Complete`, `Partial`, or `Unavailable`. Command cutover is Epic 7.

## UX & Interaction Patterns

- Chatbot owns end-user presentation. Projects returns the shared snapshot and recovery vocabulary. A sole candidate is not a selection; existing membership is a resumption.
- Operator Web is a FrontComposer console for inventory, detail, reference health, and the current resolution trace. Status is never color-only. Empty, denied, stale, rebuilding, and unavailable stay distinct, and the trace does not preselect a candidate. An early audit affordance states that audit is not yet available.
- Web read journeys meet WCAG 2.2 AA through keyboard use, accessible names, 200% zoom, and 320 CSS-pixel reflow. Unresolved critical or serious violations block the capability.
- CLI reads `list`, `describe`, `inspect`, `trace`, and `validate` return deterministic JSON with stable exit codes and no color-dependent meaning. A retained `reevaluate` alias is read-only refresh and matches Web state, reason codes, timestamps, and warnings. Confirmation-required CLI actions stay denied until the interactive-session claim is accepted.

## Cross-Story Dependencies

- Read stories wait on an accepted entry ledger with rollback: composition and evidence tooling, a revalidated package baseline, a dual-principal query envelope with safe denial and an authoritative watermark, fail-closed production identity, architecture conformance on that baseline, and a clean-checkout gate. Story 6.1 is ready for development only after that gate, a complete specification, and an independent result of exactly `READY`. Toolchain alignment must hold before a build or evidence lane is claimed passing.
- Reads of Conversations, Folders, Memories, Parties, or Tenants need the applicable owner contracts. Story 6.5 needs the approved FrontComposer adapter. Story 6.6 needs its CLI adapter and Story 6.8 first.
- Story 6.7 waits on read equivalence plus aligned identity and generated-consumer contracts. Epic 7 command cutover depends on that reversible read cutover. Audit, operator mutations, and release acceptance are later epics.
