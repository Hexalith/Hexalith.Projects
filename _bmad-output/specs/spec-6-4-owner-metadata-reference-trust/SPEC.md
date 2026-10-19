---
id: SPEC-6-4-owner-metadata-reference-trust
companions:
  - brownfield.md
  - owner-contracts.md
  - tasks-and-acceptance.md
  - validation.md
  - ../../implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md
  - ../../project-context.md
  - ../../planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md
  - ../../../docs/payload-taxonomy.md
  - ../../../docs/resolution-scoring-heuristic.md
sources: []
---

> Canonical prerequisite proposal, derived from `.memlog.md` and the inspected sources. All companions are required. New API names, fields and limits below are proposals for repository-owner review; this document records no owner approval or implementation readiness.

# Story 6.4 prerequisite: owner metadata and Reference Trust Index evidence

## Why

Story 6.4 cannot safely resolve Projects until Conversations and Folders expose bounded metadata evidence, EventStore preserves the originating caller through owner dispatch, and Projects supplies current actor-scoped Reference Trust Index (RTI) evidence. Existing detail reads, path-based file reads and the legacy reverse index do not meet that contract. This prerequisite defines the missing owner work without starting the resolution handlers.

## Capabilities

- **CAP-1**
  - **intent:** Projects can read current Conversation membership and matching metadata without retrieving Conversation content.
  - **success:** The Conversations contract returns only authorized allowlisted metadata with verifiable freshness, or safe denial/degradation; content access and read-triggered writes remain zero.
- **CAP-2**
  - **intent:** Projects can verify Folder and File references through bounded owner reads using opaque identities.
  - **success:** Folders proves each exact resource binding, current permission and evidence revision; missing legacy file bindings cannot score, and batches obey the limits in `owner-contracts.md`.
- **CAP-3**
  - **intent:** Owners can reauthorize the authenticated Tenant, actor, originating workload and delegation across the supported EventStore query route.
  - **success:** The signed caller-binding matrix rejects substitutions, stale authority, wrong audiences, missing permission and unapproved workloads while preserving existing wire compatibility.
- **CAP-4**
  - **intent:** Projects can read complete actor-scoped RTI evidence and current Project associations without treating cached authorization as authority.
  - **success:** Only scope-matched, revision-matched evidence confirmed by current owner reads is eligible; incomplete, stale, rebuilding or unavailable evidence yields honest degradation and never false `NoMatch`.
- **CAP-5**
  - **intent:** Repository owners can qualify this prerequisite separately from Story 6.4 and G-4 runner delivery.
  - **success:** Each gap has an owning repository, concrete task, acceptance case, validation command and required acceptance record; no missing approval or unavailable test is represented as passed.

## Constraints

- The adopted frozen Story 6.4 spec governs resolution behavior. Preserve the engine, both mappers, weights `50/45/35/30/20`, minimum `20`, distinct reasons, ordering, archive opt-in and unselected candidates.
- Supported resolution responses omit `DisplayName` on candidates and exclusions. Authorized names/labels may reach existing matching code internally; no inspection-auditing read is invoked. Shadow comparison ignores names.
- Reuse `QueryEnvelope`, `QueryCallerAuthority`, `VerifiedQueryCaller`, `DelegatedOwnerQueryClient`, `IDomainQueryHandler`, supported read stores/projections and `AdmissionSnapshot`. Make owner changes additive in the owning repository; no parallel query transport, identity authority, trust vocabulary for Projects responses, or custom Dapr journal.
- Validate Tenant equality and authenticated authority before protected validation or disclosure. OAuth scopes do not grant permissions. Safe denial and protected absence are indistinguishable; required non-current evidence cannot masquerade as no match.
- Reads perform zero domain, read-model, owner, inspection-audit, task, trace, repair or selection writes. Only existing platform-owned metadata authorization-denial audit is excepted, separately identified and measured.
- RTI is rebuildable evidence, never an ACL authority. The legacy Projects reverse index is a shadow comparator only. No path, URI, workspace, provider detail, message, prompt, payload, secret or token enters these contracts.
- G-4 tool/manifest/reads-profile delivery remains a separate prerequisite. Exact-current P1R, G-2, G-6 and Story 6.4 readiness remain governed by their existing gates; this proposal accepts none of them.

## Non-goals

- Implementing either Story 6.4 handler, query registration, response composer or routing cutover; changing sprint status or existing frozen artifacts.
- Implementing owner APIs, RTI storage/publication, file-binding enrollment, dependency upgrades or generated artifacts in this spec-writing change.
- Delivering G-4, enabling legacy file paths, adding Memory-owner reads, altering scoring, persisting inference, or claiming Story 8.9 release performance.

## Success signal

Owners can assess the exact deltas and run the acceptance lanes in the companions against pinned implementations. Story 6.4 stays gated until those owner records and the independent prerequisites exist; successful qualified reads disclose only permitted metadata and leave persisted state unchanged.

## Open Questions

- Will Conversations, Folders, EventStore and Projects maintainers accept the proposed field sets, bounds, exact authorization actions and evidence revision semantics, and supply named dated acceptance with source/package hashes and rollback?
- Will Folders and Projects accept the proposed owner-issued file identity and a separately authorized provenance/enrollment plan for existing Projects-owned file-reference IDs?
- Will EventStore accept the additive originating-workload/delegation provenance design with the specified compatibility behavior?
