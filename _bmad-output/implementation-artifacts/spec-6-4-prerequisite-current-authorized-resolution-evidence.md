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
review_loop_iteration: 1
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

Review corrections must bind native cut bytes to their exact native prefix, preserve old public constructor and legacy anchored JSON, carry byte-bound continuation, and validate current generations before negative results. File lineage must use an exact native workspace/path address independent of display metadata, retain tombstones, and enforce the existing 10000-entry read bound before native effects. These refine the selected lifecycle/currentness guarantees; they create no additional resource authority.

- [x] EventStore `DomainService/EventStoreDomainServiceExtensions.cs`: extract/reuse canonical projection-only `/project` family and metadata endpoint mapping as `MapEventStoreDomainProjectionEndpoints` and `MapEventStoreDomainProjectionMetadataEndpoint`; preserve existing SDK route policies and Folders custom routes. This resolves Folders CS1061 before integration tests.
- [ ] EventStore `Server/Streams/{DaprSourcePublicationWriterRegistration,DaprSourcePublicationNamespaceSource,SourcePublicationNamespaceActor}.cs`, `Server/Configuration/ServiceCollectionExtensions.cs`, native `AggregateActor.SourceProof.cs` and shared roster/fence (InspectSourceProofAsync already supplies a stable-turn zero-write native incarnation/prefix/erasure proof; compare whole inspections instead of sequence-only GetStreamMetadataAsync; cold activation may still recover/write): deliver executable, explicit source enrollment/admission and native head/currentness seams where independent installation coverage exists. Require complete legacy/virgin and all-writer proof; refuse missing authorities. Reuse native source proof, avoiding query recovery writes; prove persisted empty versus missing and recovery behavior. Do not invent certified production coverage or widen capacities.
- [ ] Folders `Authorization/{EventStoreFolderEffectivePermissionsReadModel,FolderPermissionEvidenceResult,LayeredFolderAuthorizationService}.cs`, contracts and `Server/Queries/FolderOwnerReadQueryHandler.cs`: expose optional native Folder/Organization/Tenant permission/resource evidence, verify strict current authorization twice and fail closed on any revision change including revoke/regrant. Preserve positional v1/JSON compatibility; no cached preauthorization receipt substitution.
- [ ] Folders native command/event/state and accepted workspace mutation path: deliver explicit event-sourced stable owner File identity and Projects-owned FileReferenceId binding, exact same-Tenant parent and lifecycle enforcement. Preserve declared native lineage only for supported atomic owner operations. Current production provider exposes add/change/remove only; delete/add or cross-Folder move invalidates old binding and requires destination re-verification; do not infer rename continuity. Historical path/digest/operation IDs cannot become FileReferenceId authority. Add identity-only delegated File query through existing SDK, no content/workspace/provider read.
- [ ] Conversations `Contracts/Queries/`, `Server/Projections/{ConversationProjectionReadModelKeys,ConversationProjectionReadModelWriter,ConversationProjectionReadStore,ConversationAsyncProjectionHandler,ConversationProjectionMaterializer}.cs`: isolated content-free metadata plus lasting committed-generation proof; maintain through normal projection, rebuild and recovery; narrow reads never hydrate detail/index. Membership derives from native ConversationProjectChanged.
- [ ] Conversations native command/event/state and `Server/Queries/`: persist explicit scoring-label classification/purpose; legacy arbitrary Label is insufficient. Add exact delegated metadata query using existing VerifiedQueryCaller and before/after native Conversation/Tenant resource and permission checks. Null/unbacked native Tenant revision cannot authorize or imply Healthy; deliver a supported native owner authority source where possible, otherwise retain explicit production blocker.
- [ ] Native provenance source follow-up: inspect EventStore `Contracts/Projections/ProjectionRequest.cs`, native `Server/Projections/` producers and SDK verified dispatch before adding any metadata-only native-proof transport in `Client/Streams/`. If supported, persist actual verified source incarnation/prefix/head identity with the Conversation isolated committed generation; query reads compare native proof without retrieving Conversation events. A local cursor or a newly read head cannot retrospectively certify an existing generation. Inspect Tenants `Queries/Handlers/TenantOwnerAuthorityQueryHandler.cs` as a native-source-fold precedent only (its TenantOwner policy is not Read); any narrow Tenant Read authority must retain existing owner-role policy, managed-Tenant caller binding and SDK delegation. Implementable source work must be pursued; unsupported producer provenance and absent deployed authority remain explicit blockers, never fabricated currentness.
- [ ] Owner test projects: execute focused real storage end-state, authorization, actor/Tenant isolation, native revoke/regrant, persistence, replay/rebuild/recovery, mixed/expired generations and initialized-empty versus absent tests. Instrument metadata reads to reject content/path/workspace access. Synthetic fixtures demonstrate behavior only, never installation completeness.
- [x] Projects prerequisite evidence: record exact full revisions, patch hashes, commands/results and unresolved source/enrollment/qualification/G-4 blockers in a new evidence bundle. Keep RTI and parent Story 6.4 handlers, acceptance and tracking gated.

The latest source investigation additionally found Tenant local health/age alone is insufficient: Folders head readers invoke actor recovery; Conversations default projection health lacks native revisions. Keep zero-write/cold-activation and exact-current Tenant authority as explicit blockers unless delivered and tested.

No unresolved product choice is introduced: the user explicitly requested these owner contracts. Source limitations remain evidence dependencies. Keep unchecked delivery tasks visible if external authority prevents completion; do not mark the prerequisite done.

### Verified source checkpoint and open owner work

Closing Debug source builds pass for SDK, Tenants, Conversations, Folders and Projects. Current closing suites pass: SDK Client 362, SDK Server 104, SDK endpoints 92, delegated-owner client 9, Tenant authority 19, Conversation Server 1013, Folder Server 1055 and focused Folder core 93. Projects owner integration passes 89 with one isolated Dapr-lane skip. The earlier broad Folder core run retains four missing nested Builds-path failures and two external-provider skips; it is not an all-pass result. Exact commands, source hashes, XML and earlier failures are retained in the [owner delivery evidence](evidence/6-4-owner-prerequisites-20261020/owner-contracts.md).

Review fixes B1/B2/B3/B5/B6/B11/E1/E2/E3/V1/V2/V3/V4 and B4's exact-target correction are implemented and tested. B7 remains deferred cold-activation/G-4 work. B8/B9/B10 remain open production/source contracts: native Folder/Organization generation binding, legacy-compatible native Conversation enrollment and aggregate content-free post-rebuild promotion. Accepted workspace mutation identities do not establish a committed File catalog, commit/abort/reset/branch lifecycle or authoritative whole-Tenant Projects reference registry. Certified installation/all-writer enrollment is absent from the inspected Platform source/metadata. These are incomplete prerequisites, so this spec stays **in-progress** and RTI/parent implementation remains unauthorized.

### Native scoring-label source follow-up — 2026-10-20

The resumed source inspection found that isolated metadata automatically classified ordinary legacy Label text. The Conversations owner now requires explicit bounded native scoring-label purpose/classification, binds it to creation/metadata command idempotency, and carries it through native creation, serialized event/state replay, publication and isolated generations. Ordinary label edits revoke classification; unrelated metadata edits preserve it. Positional constructors and legacy JSON omission are unchanged.

- [x] Deliver the explicit scoring-label source contract and native creation/event/state/projection propagation.
- [x] Correct duplicate creation, durable wrapper verification and old implicit-generation redelivery; preserve old bytes without migration or fabricated authority.
- [x] Verify classification, safe denial, malformed input, replay/rebuild/recovery, unrelated updates, serialization compatibility and retained generation time.
- [ ] Deliver and qualify existing production metadata mutation dispatch/adoption and the remaining native authority, guard, rebuild, installation, File binding and G-4 prerequisites.

Three independent reviewers produced eleven findings: ten corrected and one pre-existing production mutation-adoption gap deferred. Parent final validation passed 1358 tests (131 contract behavior/boundaries, 2 static package metadata, 189 full core, 1036 full server), zero failures/skips, with three successful Debug source builds. The existing Contracts.Tests missing nested Commons project warning remains; Release package execution was not run. Source bytes were unchanged throughout final validation. Actual commands, full revisions, reviewed patch/source hashes, failed/superseded runs and preservation proof are retained in the [native scoring-label evidence](evidence/6-4-native-scoring-label-20261020/README.md).

The native update event/replay path is source behavior, not a deployed metadata-mutation route. Production authority and G-4 remain unqualified; the prerequisite stays in-progress and RTI/parent implementation remains unauthorized. The required runtime baseline failed first on mixed source/package selection, then with child exit 255 while disk space was exhausted; no healthy AppHost or live acceptance is claimed.

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

- 2026-10-20 native scoring-label follow-up — Removed automatic legacy-label classification; added explicit native source/replay provenance and bounded review fixes. KEEP frozen parent decisions, legacy constructors/JSON, prior generation bytes, immutable proof identity, current-authority refusals, all dependency/gitlink pins and consumer gates. Final source validation and exact remaining blockers are recorded in evidence/6-4-native-scoring-label-20261020/.

- 2026-10-20 review 1 — Clarified complete native prefix binding, original constructor/anchor compatibility, byte-bound pagination, exact workspace/path mutation identity independent of display/classification, pre-effect lifetime bounds and current negative lookups. KEEP isolated content-free generations, original rebuild barriers, source message identity, native codec, signed SDK transport, all refusal gates, dependency pins and concurrent changes. Apply scoped corrections without reverting preserved owner work. Native guard enrollment and content-free post-rebuild promotion remain explicit source dependencies.

- 2026-10-20 — Recorded D1–D6 selections; corrected transport/raw-count assumptions and simplified receipt/ingestion scope. Preserve parent gates and scoring.

## Review Triage Log

| Finding | Verdict | Route | Verified evidence / action |
| --- | --- | --- | --- |
| B1 | high | patch | Native cut consumers compare only head message; actual Source.NativeProof/raw native digests are now available but not bound to selected prefix. Bind complete proof and test missing/replaced native sources. |
| B2 | medium | patch | Native actor byte ceiling can stop below requested count; controller computes truncation from count alone. Preserve bounded-range continuation and verify a byte-limited page. |
| B3 | medium | patch | Legacy-to-native feed mismatch can persist sticky poison. No migration is authorized; preserve old offset/state bytes and fail closed with an explicit upgrade requirement before mutation. |
| B4 | high | bad_spec | File lineage uses a digest containing DisplayName and classification; same normalized target with changed description can leave an incarnation active after removal. Require a separate exact native workspace/path address binding and preserve policy digest semantics. |
| B5 | medium | patch | Negative identity lookup returns before resource-version/current-authority confirmation. Apply the same generation and final-I/O checks before positive or negative classification. |
| B6 | medium | patch | Native identity history grows beyond the read contract limit of 10000. Enforce the existing bound before effects; retain tombstones and never silently truncate or invent partitions. |
| B7 | high | defer | Cold AggregateActor activation can recover/write before metadata inspection. This pre-existing SDK lifecycle limitation is explicitly retained in the execution scope and G-4 gate; warm-turn tests do not qualify cold behavior. |
| B8 | medium | bad_spec | Production Folder native Tenant evidence is absent; default refusal is safe but this owner delivery task remains incomplete. Tenant Read alone cannot upgrade sequence-only Folder/Organization generations to native proof. |
| B9 | medium | bad_spec | Default Conversation composition has no executable legacy-compatible native guard enrollment. Explicit descriptor and tests are not accepted production capability; keep native-current consumption blocked. |
| B10 | medium | bad_spec | Rebuilt selection references a potentially rich receipt and strict adapter refuses it. Preserve refusal and recovery barriers; native post-rebuild promotion requires a supported content-free completion protocol, still an open source prerequisite. |
| B11 | medium | patch | Replacing the public three-argument reader constructor breaks compiled callers even with an optional fourth argument. Restore the existing overload and verify signature compatibility. |
| E1 | high | patch | Full current Conversation server run independently confirms 16 DI failures resolving DelegatedOwnerQueryClient from production native evidence. Register through supported SDK dependencies and verify normal host discovery. |
| E2 | high | patch | Same native-prefix binding defect as B1 in snapshot reader; head identity alone cannot authenticate source bytes. |
| E3 | high | patch | Same native-prefix binding defect as B1 in feed; do not persist descriptors under an unrelated native proof. |
| V1 | medium | patch | Reviewer verified the changed delegated-origin client branch is not executed by existing tests. Add real client-path success coverage preserving original authority. |
| V2 | medium | patch | Reviewer verified native Tenant Read tests omit disabled Tenants retaining membership. Add native disabled-prefix denial cases for existing roles. |
| V3 | medium | patch | Reviewer verified native prefix accumulator tests do not cross 100-event boundary. Add an authenticated 101-event multi-page native case and exact final proof assertions. |
| V4 | medium | patch | Registration theory expects metadata and legacy access counts without native enrollment. Preserve production fail-closed behavior and separately test executable positive native transport; include full host suite. |

### Native scoring-label source review — 2026-10-20

This review covers the delivered scoring-label source slice only. The prerequisite remains in-progress; no RTI, production mutation adoption, owner qualification or parent readiness is accepted.

| Finding | Verdict | Route | Verified evidence / action |
| --- | --- | --- | --- |
| SL-B1 | medium | defer | The metadata-update contract pre-exists without a production native mutation dispatcher; the change adds no dispatcher or deployment claim. Retain that existing adoption blocker; event/replay fixtures are not a production mutation pass. |
| SL-B2 | medium | patch | An older implicitly classified subset and proof use the same stable dispatch while the corrected materializer produces a different digest. MergePending rejects that identity change. Add explicit safe old-format refusal/recovery coverage that preserves original bytes; no automatic migration or grant. |
| SL-B3 | medium | patch | A distinct second ConversationCreated reaches the materializer and overwrites classification, whereas native state rejects duplicate creation. Reject the duplicate before changing creation state and verify no completed generation is replaced. |
| SL-B4 | medium | patch | GenerationIsDurableAsync compares Summary/Detail but omits the new persisted wrapper classification. A missing or altered wrapper field can be reported durable; include exact classification equality and exercise corrupted-wrapper redelivery. |
| SL-B5 | medium | patch | New classification decoder/mismatch branches lack end-state negative tests. Cover unsafe/unknown policy and mismatched native creation/update events, preserving the prior completed generation and rejected-input barrier. |
| SL-B6 | low | patch | Source-generated classified round trips and null omission are exercised only on creation commands. Extend direct compatibility coverage to the metadata command and both public events, preserving their positional constructors. |
| SL-B7 | low | patch | The new rebuild test names generation-time preservation but asserts only source event time with an unchanged clock. Distinguish source observation time from a new rebuild generation and verify stable retry time with clock movement. |
| SL-B8 | low | patch | Public consumer documentation does not explain the new explicit scoring opt-in, bounds and invalidation. Document source behavior and retained production/adoption gates without claiming an available mutation route. |
| SL-V1 | medium | patch | Pre-verified regression gap: an unrelated attribute/business-reference update has no classified-creation coverage. Assert retained classification and successful metadata disclosure when Label is omitted. |
| SL-V2 | medium | patch | Pre-verified regression gap: command mismatch tests do not execute the projection mismatch guards. Add decodable mismatched creation/update events and durable prior-generation/barrier assertions. Shared negative-input coverage with SL-B5. |
| SL-E1 | medium | patch | A second distinct creation can classify a legacy label despite native replay rejecting that stream. Same verified duplicate-creation defect as SL-B3. |

Review correction closure: SL-B2 through SL-B8, SL-V1, SL-V2 and SL-E1 are patched and covered by the final corrected-tree checks. SL-B1 remains a pre-existing adoption prerequisite recorded in deferred-work.md. The evidence bundle preserves initial versus corrected source/results; no full prerequisite or parent acceptance is claimed.

## Design Notes

One evidence prerequisite spans owner repositories. The annex carries detailed contracts so the execution summary stays short. Unknown storage/identity authority is a blocking decision, never a permanent unavailable stub presented as delivery.

## Verification

- `python3 tools/planning/validate_production_authority.py --story-id 6.4` — planning authority only; no implementation readiness claim.
- `git diff --check` and document checks — validate this documentation change.
- Compare protected-file hashes and repository state with the pre-write snapshot; parent Story 6.4, sprint, dependency configuration and pins remain unchanged. Bind owner changes to exact revisions, named-file hashes and working patches; preserve concurrent work.
- Retain actual owner source-test commands, logs and native XML separately from unavailable deployed integration/G-4 lanes. Passing source fixtures never establish production completeness. See annex §§8–9 and the [owner delivery evidence](evidence/6-4-owner-prerequisites-20261020/owner-contracts.md).

2026-10-20 owner-delivery resume: the user explicitly authorized prerequisite implementation in owning repositories, preserving consumer gates. Source investigations identified executable native-proof/SDK endpoint fixes, isolated metadata and owner File identity work; independent installation coverage, deployed native Tenant authority, native Folder generations, committed File mapping and native Conversation enrollment/rebuild promotion remain blockers. The required Aspire baseline started; security awaited missing client-credential parameters and remained unhealthy, while all dependent services waited. The exact Projects AppHost was stopped without force before source tests.
