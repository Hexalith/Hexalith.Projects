# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give Chatbot and operators current, authorization-filtered Project list, open, Conversation-start, context, and resolution reads through supported DomainService read models. Expose the same safe truth through authenticated FrontComposer and CLI surfaces, then switch read routing after shadow-read equivalence with a reversible cutover. This establishes the read-side contract and package boundary needed by later work while preserving event history.

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

- Read authorization binds the server-derived Tenant, original actor, workload, action, and current owner-system evidence. Chatbot lists only Projects whose Folder the actor can read; Tenant-role operator reads require their own permission. Denied, cross-Tenant, and nonexistent targets have indistinguishable safe responses. A foreign or unauthorized attachment behaves as no match.
- Operator results contain Safe Metadata by default. Project names, Setup bodies, titles, paths, and other descriptive fields require separate inspection authorization and a metadata-only audit. Context and diagnostics never copy foreign payloads, secrets, transcripts, or file contents.
- Applicable results share one snapshot: `responseState`, `asOf`, disclosable `projectVersion`, component freshness and safe reasons, conditional resolution result, and recovery actions. `Partial` is usable only with current Project, Folder, Setup, and first-response authority evidence, with every optional omission shown. `Unavailable` blocks context and Chatbot first-response use; only `Complete` or `Partial` can admit that response.
- List cursors default to 50 and cap at 200. Pre-activation tasks are not Projects. Active/context-usable Projects have exactly one authorized Folder; quarantined folderless legacy records cannot enter Chatbot list, resolution, or context. Conversation-named reads use its member Project, or a Project explicitly opened in that session if membership is absent.
- Context includes only current authorized reference metadata; owner reads are checked at assembly. Setup-marked references are required, other omissions are explained, and refresh is read-only. Resolution returns `NoMatch`, `SingleCandidate`, or `MultipleCandidates` with a transient trace; an inferred candidate never selects or attaches a Project.
- Read contracts remain compatible with historical events and opaque foreign identifiers. Supported read routing needs deterministic equivalence and rollback evidence; no event history rewrite or dual command writer. Web reads require WCAG 2.2 AA evidence. Metadata-read targets are p95 below 500 ms at 1,000 Projects/500 references and below one second at supported maximum size.

## Technical Decisions

- Projects owns query policy, contracts, incremental projections, and a rebuildable Tenant-scoped Reference Trust Index. DomainService supplies runtime, persistence, and scoped opaque cursors. Conversations owns Conversation membership; Folders and Memories retain resource authority. Queries reauthorize against the owner instead of treating cached index evidence as permission.
- Packable `Hexalith.Projects.Contracts` owns wire vocabulary and excludes FrontComposer, Fluxor, Fluent UI, ASP.NET Core, Dapr, and Aspire dependencies. Non-packable `Hexalith.Projects.UI.Contracts` depends inward and owns presentation descriptors only. Generated OpenAPI, clients, and adapter schemas derive from the versioned contracts.
- Shadow reads compare results, keys, watermarks, cursors, and Tenant isolation before a reversible read-route switch. Legacy read routing remains available through cutover; command cutover belongs to Epic 7.

## UX & Interaction Patterns

- Chatbot presents the shared snapshot and visible omission/recovery states. A sole inferred candidate stays unselected; `ConversationLinked` reflects existing membership.
- FrontComposer provides read-only inventory, detail, reference health, and current resolution trace views. Status and recovery are conveyed in text as well as visual cues; denied and unavailable states do not expose protected detail.
- CLI read commands return deterministic machine-readable output and stable exit codes. Web and CLI use the same lifecycle, freshness, reason, timestamp, and warning meanings.

## Cross-Story Dependencies

- Story 6.1 remains blocked: the accepted October 1 EventStore 3.110.0 / Builds 4.29.1 decision does not make current P1R usable; the October 6 remediation is open. It still needs accepted P0/P1R/P2/P3 capabilities, same-baseline Solution Architect sign-off, P4 clean-checkout acceptance, specification readiness, and an independent result of exactly `READY`. G-6 runtime qualification applies separately to each affected source/runtime tuple.
- Each read story needs its applicable sibling-owner read contracts; Web needs the approved FrontComposer adapter, and CLI needs its approved adapter plus Story 6.8. Story 6.7 waits for equivalence across Epic 6 reads and aligned identity/generated contracts. Epic 7 durable decisions depend on the supported read boundary; operator audit and mutation journeys come later.
