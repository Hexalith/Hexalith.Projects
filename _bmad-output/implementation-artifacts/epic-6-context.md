# Epic 6 Context: Chatbot and Operators Retrieve Authorized Project Truth

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver authorized Project list, open, Conversation-start and context reads, advisory suggestions, authenticated operator surfaces, and reversible supported-read cutover. Projects supplies domain contracts and policy; platform modules supply runtime and persistence. This epic establishes read-side authority without admitting consequential work or accepting production release.

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

- Bind every decision to authenticated Tenant, original actor, workload/delegation, action and target. Reauthorize foreign resources with their owner; discovery and cached evidence never grant access. Chatbot's permitted Project set derives from current Folder read authority; Tenant-role access belongs to Web/CLI/MCP. Never forward human bearer credentials as service authority.
- Safe Metadata is the default. Tenant-role descriptive metadata requires distinct inspection authority and durable metadata-only inspection audit. Denied, cross-Tenant and absent Projects collapse to indistinguishable safe 404. Never expose foreign payloads, secrets, paths or protected exclusion details in responses, logs or evidence.
- Lifecycle is Active or Archived. Pre-activation work is not a Project. Archived Projects can be inspected but cannot initialize context. Legacy folderless records stay quarantined recovery evidence; never fabricate a valid Folder or silently activate them.
- Context and Conversation-start bind to the named Conversation's member Project, or, without membership, the Project explicitly opened in that session. Complete requires current required evidence. Partial is usable only with current Project, Folder, Setup and first-response authority plus explicit optional omissions. Unavailable blocks context and first response. Components identify inclusion, freshness, safe reason and last-verified evidence. Refresh recomputes read-only.
- Advisory resolution alone may use lagging committed matching metadata. Return authorized IDs, scores and safe reasons with evaluation time and explicit incomplete coverage; omit names. NoMatch covers successfully evaluated observations, never an incomplete exhaustive search. A sole suggestion remains unselected and confers neither context nor association authority. Confirmation independently rechecks current permissions, lifecycle and relationships.
- Preserve deterministic ordering/scoring, archive opt-in, authenticated cursors and page default 50/cap 200. Suggestions retain at most 32 raw attachments, initially four concurrent owner calls, cancellation, a two-second dependency deadline and bounded enumeration. Real owner-backed File identity is mandatory; missing mapping means explicitly incomplete support, never guessed identity.
- Reads perform no Project/sibling mutation, query repair or persisted inference. Required security/inspection audit remains separate. Preserve additive contracts, historical events and opaque foreign identities. Verify persisted end-state, authorization, isolation, replay/restart and leakage; missing evidence is not a pass.

## Technical Decisions

- Consume EventStore DomainService hosting, incremental projection handlers, query handlers, read-model stores/batches and authenticated cursor infrastructure. Projects must not create a parallel runtime, journal or persistence protocol. Context uses rebuildable reference trust; sibling owners retain resource lifecycle and authority. Conversations alone owns membership.
- Dependency-light Contracts owns domain/wire vocabulary and operation security. Non-packable UI.Contracts owns presentation descriptors and depends inward; it cannot redefine semantics. Generated adapters preserve canonical contracts.
- The approved October 21 exception lets advisory resolution finish existing query composition over committed views and authenticated narrow owner reads. Native-head equality, complete-global discovery, rebuild/all-writer certification, supported-host migration and common-runner rollout are not its development prerequisites. Preserve strict owner contracts and completed proof work; generic platform expansion needs separate scope.
- Shadow-read cutover compares canonical output, keys, watermarks, cursors and Tenant isolation with finite approved normalization. Routing remains reversible; no event rewrite or unsafe dual writer. Read cutover does not authorize command cutover.

## UX & Interaction Patterns

- Preserve shared response, freshness, reason and recovery meanings across surfaces. Show incomplete suggestion coverage and no preselection; read-only candidates have no accept action. Adapters cannot treat advisory output as first-response authorization.
- Operator Web uses FrontComposer/Fluent UI; CLI emits deterministic machine output and stable exit codes without color-dependent meaning. Maintain keyboard, focus, assistive-technology, 200% zoom and 320px reflow behavior. Audit views remain explicitly unavailable until their capability is accepted. Hexalith.McpCli is the target CLI/MCP adapter.

## Cross-Story Dependencies

- Shared supported reads retain accepted P0/P1/P1R/P2/P3/P4 inputs, applicable sibling contracts, current runtime qualification, same-baseline architect conformance, clean-checkout verification, specification readiness and independent READY. Historical P1 and the October 1 P1R decision do not establish current P1R usability; remediation and downstream gates remain open. Local implementation does not manufacture formal acceptance.
- Story 6.4's advisory exception advances no other story, common-runner or release gate. Its bounded acceptance uses existing individual test projects plus real authenticated query/owner/store evidence with zero domain writes.
- Web needs its approved FrontComposer adapter; CLI requires Story 6.8's Contracts split. Story 6.3 must prove one cohesive slice or receive an approved split. Story 6.7 needs equivalence and rollback across applicable reads. Epic 7 retains consequential workflow authority; Epic 8 owns terminal deployment and release acceptance.
