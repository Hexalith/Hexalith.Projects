# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give Chatbot and operators authorized Project reads and advisory suggestions. Context and first-response admission retain current-evidence guarantees; suggestions use observed committed metadata with request-time owner authorization and honest coverage. Authenticated presentation and reversible read cutover establish the later workflow boundary without consequential writes or event-history changes.

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

- Authorization binds Tenant, original actor, workload/delegation, action, and exact resource access. Check same-Tenant before owner authorization. Cached views never grant permission. Denied/foreign/nonexistent Conversation inputs are indistinguishable; unauthorized or unknown attachments behave as an authorized input with no match.
- Responses and logs remain metadata-only. Suggestions disclose authorized IDs, scores, and safe reasons without names or protected exclusion details. Other descriptive metadata retains inspection authorization and audit. Never expose transcripts, file contents, secrets, or foreign payloads.
- Context and Conversation-start retain their snapshot/recovery contract. Usable `Complete`/`Partial` requires current Project, Folder, Setup, and first-response authority; required references pass owner checks. `Unavailable` blocks context and first-response admission. Refresh stays read-only.
- The approved October 21 reset applies only to Story 6.4: matching metadata may lag. Suggestions carry evaluation time and explicit incompleteness; time is not a source watermark. `NoMatch` covers successfully evaluated observations; `SingleCandidate` means one visible suggestion, never exclusivity or selection. Incomplete evaluation cannot claim exhaustive emptiness.
- Preserve deterministic scoring, ordering, reasons, and the archive option. Bound requests to 32 raw attachments, initially four concurrent dependency calls, cancellation, a two-second dependency deadline, and bounded candidate enumeration. Known denied/deleted inputs never score.
- Folder/File identity must be real and owner-backed. Missing File mapping or safe owner reads remain capability gaps. Name partial Folder/Conversation scope and incomplete/unsupported coverage. Never guess File identity from paths or unrelated IDs or claim unsupported attachment coverage.
- Suggestions grant no context or association authority, select nothing, persist no inference history, and perform no query repair or Project writes. Confirmation independently rechecks permissions, lifecycle, and relationships. Existing confirmed Conversation membership remains distinct from inference.
- Preserve opaque identifiers, historical events, pagination default 50/cap 200, latency objectives, and accessibility. Runtime, deployment, and release acceptance retain separate evidence boundaries.

## Technical Decisions

- Projects owns domain query policy and contracts; platform infrastructure owns runtime/persistence. Conversations owns membership; resource owners retain lifecycle and permission authority. Context retains rebuildable reference trust and current-evidence requirements.
- Story 6.4 may finish existing query composition over committed views and authenticated owner reads, adding only a missing narrow metadata-only owner seam. Native-head equality, global discovery, rebuild certification, all-writer enrollment, and supported-host migration are not advisory prerequisites. Preserve strict owner semantics and completed proof work; generic platform expansion requires separate scope.
- Packable Contracts stays free of presentation/runtime dependencies. Presentation descriptors belong in the non-packable UI.Contracts layer. Supported-route cutover still requires semantic equivalence and rollback evidence; command cutover remains separate.

## UX & Interaction Patterns

- Show incomplete coverage and unselected suggestions. A sole inferred candidate still needs confirmation; read-only candidates have no accept action. Adapters cannot reinterpret suggestions as context or first-response authorization.
- Preserve context freshness/recovery meanings, accessible status text, safe denial, and deterministic machine output. Hexalith.McpCli remains the eligible CLI/MCP target; human confirmation remains on approved confidential interactive surfaces.

## Cross-Story Dependencies

- Story 6.1 retains baseline remediation, prerequisite acceptance, architecture sign-off, clean-checkout/specification readiness, and independent readiness. The Story 6.4 exception advances no other gate or release decision.
- Advisory acceptance uses the existing test-project runner, focused regressions, and one real authenticated query/owner/store integration case. It accepts neither the missing common runner nor deployment; owner authorization and context/confirmation boundary checks remain mandatory.
- Presentation still needs approved adapters; CLI needs the Contracts split. Read cutover requires equivalence/rollback. Epic 7 retains consequential workflow authority; Epic 8 retains terminal release acceptance.
