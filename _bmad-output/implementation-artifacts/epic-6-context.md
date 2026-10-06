# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Make current Project truth readable for Chatbot and operators: authorization-filtered list, open, Conversation-start, context, and resolution reads on incremental DomainService read models and a rebuildable Reference Trust Index, served through authenticated FrontComposer and CLI. Supported reads become authoritative only after shadow-read equivalence, with reversible routing and unchanged event history. Writes, confirmation, and command cutover stay later. This epic also sets the identity, contract, and platform boundary those slices use, and moves presentation descriptors out of packable Contracts.

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

- Reads are Tenant-, actor-, action-, and current-version-scoped. Same-Tenant is checked before authorization, and the decision binds to the original actor. A denied, cross-Tenant, or nonexistent single target is one safe `404`. A bad attachment matches an authorized no-match and leaves the request intact.
- Tenant-role reads default to Safe Metadata. Names, Setup bodies, titles, paths, and Preview bodies need a separate audited inspection permission. Project Users get that content for permitted Projects. Explanations use opaque surrogates only.
- Results share `responseState`, `asOf`, disclosable `projectVersion`, metadata-only `components`, conditional `resolutionResult`, and `recoveryActions`. `Complete` is usable. `Partial` is usable only with current Project, Folder, Setup, and authorization evidence and every omission shown. `Unavailable` blocks context use and first-response admission, which allows only `Complete` or `Partial`.
- Lists are cursor-paged (default 50, cap 200) and Chatbot shows only Folder-readable Projects. A non-current Folder makes that entry `Unavailable`. Pre-activation work is never a Project. Archived Projects stay out of default resolution unless requested. Quarantined folderless records stay out of Chatbot list, resolution, and context; authorized operators may also see the name and any pre-v1 Folder or creation receipt.
- A Conversation-named read serves its member Project, or a Project opened in that session when membership is absent. Start setup is goals, instructions, context preferences, and default source policy for one snapshot. Inclusion needs same-Tenant, lifecycle, current owner read, and freshness. Setup-marked references are required; other omissions use `ExcludedBySetupPolicy`. Refresh returns a new snapshot and writes nothing.
- Resolution uses identity and metadata, never file contents, and returns `NoMatch`, `SingleCandidate`, or `MultipleCandidates` without selecting. `ConversationLinked` is existing membership; any other sole candidate is inferred. Read-only Folder authority yields a read-only candidate, and hidden siblings must not become a certain match. The trace is request-scoped and unpersisted.
- Operator reads stay read-only, and audit stays absent or explicitly unavailable until that capability exists. Legacy identifiers stay readable and opaque. Metadata reads target p95 under 500 ms at the median shape and under one second at the maximum, within 100 reads/second per Tenant and burst 200.

## Technical Decisions

- Projects owns contracts, query policy, projections, and the Reference Trust Index. DomainService owns persistence, cursors, and runtime. Missing sibling read contracts fail closed. The index stores safe owner metadata, watermark, authorization outcome, and freshness. Conversation membership stays in Conversations, with a rebuildable Projects reverse index.
- Queries use the platform handler and an authenticated opaque cursor. Identities are platform ULIDs at `Tenant/projects/ProjectId`. Authorized reads return HTTP 200. CLI and MCP eligible reads use `Hexalith.McpCli`. Confirmation stays on confidential Chatbot.
- Packable Contracts own the wire vocabulary and stay free of FrontComposer, Fluxor, Fluent UI, ASP.NET Core, Dapr, and Aspire. Non-packable UI contracts depend inward and own presentation metadata only.
- The October 1 accepted coordinate remains published EventStore 3.110.0 (`v3.110.0`) / Builds 4.29.1 with historical 3.70.1 rollback. Archives are verified. Attempt 21 completes replay/retained-snapshot restore and the pinned 2c58ffda source comparison; seven incompatible dispositions keep current P1R unusable, including unsafe evidence handling, lost authority/watermarks/APIs and an old-writer append that drops retained floor 5. Later checkout source has no inherited package qualification. Jerome approved the October 6 correction: `6.1-P1R-remediation` is open and routed through its separate bounded spec; the completed investigation and named tuple decisions stay intact.
- G-6 is separate runtime/toolchain proof. Historical attempt-16 acceptance remains limited to its beta.910 source. The October 3 CI-readiness evidence separately qualifies the approved preview tuple at its tested source; consult `qualification_gates.G-6` and its own current applicability checks. Neither result qualifies the P1R packages, rollback or later checkout commits. FrontComposer package 4.0.0 still needs parity with checked-out 4.0.1.
- Shadow reads must match output, keys, watermarks, cursors, and Tenant isolation before reversible routing. One command writer comes later.

## UX & Interaction Patterns

- Chatbot presents the shared snapshot. A sole candidate stays unselected; `ConversationLinked` is a resumption. A retained `reevaluate` alias is read-only refresh.
- Operator Web is a FrontComposer console for inventory, detail, reference health, and the current trace. Status is never color-only. Empty, denied, stale, rebuilding, and unavailable stay distinct. Web reads meet WCAG 2.2 AA, including keyboard use, 200% zoom, and 320 CSS pixels. An unresolved critical or serious violation blocks the capability.
- CLI `list`, `describe`, `inspect`, `trace`, and `validate` return deterministic JSON and stable exit codes, with Web parity for states, reasons, timestamps, and warnings, and no color-dependent meaning. Confirmation-required CLI actions stay denied until the interactive-session claim is accepted.

## Cross-Story Dependencies

- Read stories wait on the composition runner and evidence tooling, the dual-principal query envelope with safe denial and an authoritative watermark, fail-closed production identity, same-baseline architecture conformance, and a clean-checkout gate. EventStore 3.110.0 acceptance does not close those items. Readiness stays not ready, and Story 6.1 plus dependent work stay blocked until the gate, a complete specification, and an independent result of exactly `READY`. Affected lanes need applicable current G-6 qualification; the historical accepted packet cannot qualify the changed Toolkit tuple.
- Conversations, Folders, Memories, Parties, and Tenants reads need their owner contracts, with no degraded fallback. Web needs the approved FrontComposer adapter. CLI needs its adapter and the UI-contracts split first.
- Read cutover waits on equivalence plus aligned identity and generated contracts. Later command cutover depends on that reversible switch. Audit, mutations, and release acceptance follow.

## P1R Remediation Handoff (2026-10-06)

- Approved proposal: `../planning-artifacts/sprint-change-proposal-2026-10-06.md`; implementation handoff: `spec-6-1-p1r-remediation.md`. Product Owner applies planning; EventStore/Builds and P2 owners establish repository-local scope and supply tested candidate/rollback evidence; Solution/Test Architects review the exact envelope.
- No v1 requirement or UX scope changes. Old writable downgrade remains blocked for newer retained streams; apply AD-17 mutation freeze and roll forward unless a capable rollback target is independently qualified.
- Current P1R usability stays false and Story 6.1 stays blocked. P0/P2/P3/P4, independent READY and Story 8.11 terminal acceptance remain separate.
