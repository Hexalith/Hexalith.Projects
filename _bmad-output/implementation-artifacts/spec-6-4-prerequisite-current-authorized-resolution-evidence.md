---
title: 'Story 6.4 prerequisite: current authorized resolution evidence'
type: 'feature'
created: '2026-10-19'
updated: '2026-10-20'
status: 'in-progress'
route: 'dispatch'
artifact_kind: 'selected-prerequisite-design'
related_story: '6-4-resolve-projects-with-transient-current-explanations'
design_decisions: 'selected'
implementation_authorized: false
owner_prerequisite_implementation_authorized: true
baseline_commit: 'b0caf2b5c25a1f761abed685c7c9ef811732c6fb'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md'
  - '_bmad-output/implementation-artifacts/6-4-resolution-evidence-contracts.md'
---

<frozen-after-approval reason="human-selected recommendations; implementation evidence remains incomplete">

## Intent

**Problem:** Story 6.4 cannot obtain content-free Conversation metadata, authoritative opaque File evidence, or current actor-scoped Reference Trust Index (RTI) evidence through the inspected contracts.

**Approach:** Reuse EventStore transport, Folder authorization and Projects read models with narrow owner contracts and one RTI. The [contract annex](6-4-resolution-evidence-contracts.md) specifies the selected design and outstanding source evidence.

**Decision (2026-10-20):** The user instructed “apply recommended,” selecting D1–D6 in the [decision record](6-4-prerequisite-decisions-2026-10-20.md). Existing implementation and qualification gates remain.

## Boundaries & Constraints

**Always:** Preserve Story 6.4's frozen decisions, blocked tracking, scoring, unselected candidates and prerequisites-first gate. Bind Tenant, actor, workload and delegation; require current owner authority before disclosure. Keep metadata names inside authorized scoring and omit `DisplayName` from candidates and exclusions. Reuse AD-32 and supported persisted readers.

**Never:** Fetch content/messages/paths/URIs/workspaces/secrets; use inspection-auditing reads, cached grants or the legacy reverse index as authority; persist query inference or repair; change packages, gitlinks, generated artifacts, legacy routes, sprint state or acceptance. Selected designs do not establish missing source capabilities or qualification.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Current | Complete supported topology and current actor-authorized owner evidence | Eligible metadata reaches existing mappers | No automatic selection or query writes |
| Protected | Denied, absent or cross-Tenant protected target | Indistinguishable safe 404 | No protected identity or existence detail |
| Degraded | Missing/incomplete/stale/rebuilding evidence or changed authority | Minimal AD-32 `Unavailable` after authority is established | Never fabricate `NoMatch` |
| Optional | Required evidence current; omission disclosure authorized | Explicit `Partial` | No protected exclusion identity |
| Reordered/oversized | Duplicate/permuted or over-budget input | Deterministic deduplication or bounded failure | No silent truncation or unbounded fan-out |

</frozen-after-approval>

## Current execution scope (2026-10-20)

The user's resume request authorizes delivery and testing of missing **owner prerequisites**. RTI implementation and parent handlers remain gated; `implementation_authorized: false` refers to those consumers. Preserve the frozen block verbatim, all pins/gitlinks, parent story/sprint/qualification files and concurrent work. No commits, dependency updates, deployment or historical reference migration are requested. Baseline source tuple: Projects `1be617abe38773ca665cb3e44bf8f6d812f574a1`, EventStore `6c56c27bc1a8d63c83f9631f7162cf79fdde0a1e`, Conversations `0c7bfac19cafbf4cbb2f67340b06e569280ef05a`, Folders `9bacd7f62848c23222dc7309635eb65dc6e5ed9c`. Inspect current source before each edit. Root AGENTS.md and the verified workspace LLM/state baseline apply in each owner; never load skills under references.

Execute owner work sequentially; source-contract availability determines consumer readiness, never synthetic authorities or static receipts. Use Debug source references and existing SDK persistence/transport. A missing production installation, native authority or external writer inventory must remain an explicit blocker rather than be fabricated.

### Owner execution plan

- [ ] EventStore `DomainService/EventStoreDomainServiceExtensions.cs`: extract/reuse canonical projection-only `/project` family and metadata endpoint mapping as `MapEventStoreDomainProjectionEndpoints` and `MapEventStoreDomainProjectionMetadataEndpoint`; preserve existing SDK route policies and Folders custom routes. This resolves Folders CS1061 before integration tests.
- [ ] EventStore `Server/Streams/{DaprSourcePublicationWriterRegistration,DaprSourcePublicationNamespaceSource,SourcePublicationNamespaceActor}.cs`, `Server/Configuration/ServiceCollectionExtensions.cs`, native `AggregateActor.SourceProof.cs` and shared roster/fence (InspectSourceProofAsync already supplies a stable-turn zero-write native incarnation/prefix/erasure proof; compare whole inspections instead of sequence-only GetStreamMetadataAsync; cold activation may still recover/write): deliver executable, explicit source enrollment/admission and native head/currentness seams where independent installation coverage exists. Require complete legacy/virgin and all-writer proof; refuse missing authorities. Reuse native source proof, avoiding query recovery writes; prove persisted empty versus missing and recovery behavior. Do not invent certified production coverage or widen capacities.
- [ ] Folders `Authorization/{EventStoreFolderEffectivePermissionsReadModel,FolderPermissionEvidenceResult,LayeredFolderAuthorizationService}.cs`, contracts and `Server/Queries/FolderOwnerReadQueryHandler.cs`: expose optional native Folder/Organization/Tenant permission/resource evidence, verify strict current authorization twice and fail closed on any revision change including revoke/regrant. Preserve positional v1/JSON compatibility; no cached preauthorization receipt substitution.
- [ ] Folders native command/event/state and accepted workspace mutation path: deliver explicit event-sourced stable owner File identity and Projects-owned FileReferenceId binding, exact same-Tenant parent and lifecycle enforcement. Preserve declared native lineage only for supported atomic owner operations. Current production provider exposes add/change/remove only; delete/add or cross-Folder move invalidates old binding and requires destination re-verification; do not infer rename continuity. Historical path/digest/operation IDs cannot become FileReferenceId authority. Add identity-only delegated File query through existing SDK, no content/workspace/provider read.
- [ ] Conversations `Contracts/Queries/`, `Server/Projections/{ConversationProjectionReadModelKeys,ConversationProjectionReadModelWriter,ConversationProjectionReadStore,ConversationAsyncProjectionHandler,ConversationProjectionMaterializer}.cs`: isolated content-free metadata plus lasting committed-generation proof; maintain through normal projection, rebuild and recovery; narrow reads never hydrate detail/index. Membership derives from native ConversationProjectChanged.
- [ ] Conversations native command/event/state and `Server/Queries/`: persist explicit scoring-label classification/purpose; legacy arbitrary Label is insufficient. Add exact delegated metadata query using existing VerifiedQueryCaller and before/after native Conversation/Tenant resource and permission checks. Null/unbacked native Tenant revision cannot authorize or imply Healthy; deliver a supported native owner authority source where possible, otherwise retain explicit production blocker.
- [ ] Owner test projects: execute focused real storage end-state, authorization, actor/Tenant isolation, native revoke/regrant, persistence, replay/rebuild/recovery, mixed/expired generations and initialized-empty versus absent tests. Instrument metadata reads to reject content/path/workspace access. Synthetic fixtures demonstrate behavior only, never installation completeness.
- [ ] Projects prerequisite evidence: record exact full revisions, patch hashes, commands/results and unresolved source/enrollment/qualification/G-4 blockers in a new evidence bundle. Keep RTI and parent Story 6.4 handlers, acceptance and tracking gated.

The latest source investigation additionally found Tenant local health/age alone is insufficient: Folders head readers invoke actor recovery; Conversations default projection health lacks native revisions. Keep zero-write/cold-activation and exact-current Tenant authority as explicit blockers unless delivered and tested.

No unresolved product choice is introduced: the user explicitly requested these owner contracts. Source limitations remain evidence dependencies. Keep unchecked delivery tasks visible if external authority prevents completion; do not mark the prerequisite done.

## Implementation Evidence Required

D1–D6 directions are selected. Readiness still needs named File/feed APIs, native revision mappings, production binding/enrollment, full-result/performance/compatibility proof, and exact-current G-4 and shared prerequisite acceptance. Missing source contracts remain blockers; no provider or passing result is inferred.

## Code Map

- `src/Hexalith.Projects.Server/Projections/ProjectReads/` — reuse persisted detail/index, ETags, atomic projection/rebuild and keys; `Current` alone proves no owner authority.
- `src/Hexalith.Projects.Server/Queries/ProjectContextQueryExecutor.cs` — reuse two-pass authorization pattern; its pre-RTI fallback is insufficient.
- `src/Hexalith.Projects.Server/Queries/ProjectReadQueryExecutor.cs` — reuse validation ideas only; its inspection audit disqualifies invocation.
- `src/Hexalith.Projects/Resolution/` — retain existing engine, mappers and weights.
- Annex §§2–9 — owner paths, selected directions and missing implementation evidence.

## Tasks & Acceptance

**Execution after source contracts are ready:**

- [ ] EventStore/deployment owners — deliver §2's production binding and verified-origin contract; name exact enrollment configuration.
- [ ] Folders owner — establish §4's actual stable File source/mapping and native receipt evidence; verify compatibility.
- [ ] Conversations owner — implement the narrow metadata key/read and delegated handler in annex §3; no detail hydration.
- [ ] Projects owner — deliver §5's sole RTI, complete inverse/name lookup and actor-scoped reader; no resolution handlers yet.
- [ ] Owner test suites — execute annex §8's contract, isolation, freshness, leakage and projection controls; retain exact revisions and results.
- [ ] Existing `6.1-P0` work — independently deliver G-4 qualification; do not create a substitute runner here.

**Acceptance Criteria:**

- Given approved owner contracts, when metadata reads execute, then only the annex allowlist is retrieved and every admitted row has current resource and actor authority.
- Given actor, Tenant, workload, delegation, permission or owner revision changes, when final disclosure is checked, then previous grants cannot authorize the result.
- Given complete empty versus uninitialized RTI evidence, when read, then only the former can establish absence of matches.
- Given owner faults, malformed responses or bounds violations, when normalized, then the matrix holds without protected identities or invented freshness.
- Given either eventual resolution query, when persisted state is compared through G-4, then domain, read-model, RTI, trace, task, repair and inspection-audit contents are unchanged.

## Implementation Notes

2026-10-19: Initial draft and annex; no implementation.

2026-10-20: Applied selected recommendations to this draft and annex; added the decision record. Historical preflight evidence is unchanged. Parent implementation remains gated.

## Spec Change Log

- 2026-10-20 — Recorded D1–D6 selections; corrected transport/raw-count assumptions and simplified receipt/ingestion scope. Preserve parent gates and scoring.

## Review Triage Log

## Design Notes

One evidence prerequisite spans owner repositories. The annex carries detailed contracts so the execution summary stays short. Unknown storage/identity authority is a blocking decision, never a permanent unavailable stub presented as delivery.

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` — planning authority only; no implementation readiness claim.
- `git diff --check` and document checks — validate this documentation change.
- Compare tracked-file hashes and repository state with the pre-write snapshot; Story 6.4, sprint, pins, owners and legacy routes must remain unchanged.
- Application tests and G-4 runs are future prerequisite evidence, not claimed by this draft. See annex §§8–9.

2026-10-20 owner-delivery resume: the user explicitly authorized prerequisite implementation in owning repositories, preserving consumer gates. Source investigations identified executable native-proof/SDK endpoint fixes, isolated metadata and owner File identity work; independent installation coverage and native Tenant authority remain evidence dependencies. The required Aspire baseline started; security awaited missing client-credential parameters and remained unhealthy, while all dependent services waited. The exact Projects AppHost was stopped without force before source tests.
