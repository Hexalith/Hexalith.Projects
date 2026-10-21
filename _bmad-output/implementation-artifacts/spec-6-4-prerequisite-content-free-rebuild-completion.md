---
title: 'Story 6.4 owner prerequisite: content-free rebuild completion proof'
type: 'feature'
created: '2026-10-21'
updated: '2026-10-21'
status: 'done'
route: 'dispatch'
artifact_kind: 'authorized-owner-source-subtask'
related_story: '6-4-resolve-projects-with-transient-current-explanations'
baseline_commit: '6e9786f0f52d3e706a1eebc5114e12106e345382'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-6-4-prerequisite-current-authorized-resolution-evidence.md'
  - '_bmad-output/implementation-artifacts/evidence/6-4-native-enrollment-20261021/README.md'
  - '_bmad-output/implementation-artifacts/evidence/6-4-native-enrollment-20261021/source-and-writer-boundaries.md'
---

<frozen-after-approval reason="bounded execution of the existing authorized owner prerequisites">

## Intent

**Problem:** A native-proven Conversation rebuild can persist correct isolated metadata, but its selection points to the coordinated rich rebuild receipt. The strict resolution adapter refuses that receipt before retrieval because an incomplete marker can contain messages and other rich data.

**Approach:** Deliver the smallest compatible EventStore-owned completion proof and Conversation consumption needed to establish an exact completed rebuild without retrieving rich receipt/manifest/content bytes. This executes the already authorized owner prerequisite; it does not authorize RTI or parent resolution handlers.

## Boundaries & Constraints

**Always:** Preserve existing user changes, selected D1–D6, frozen parent intent, native proof bound to original input, exact scope/catalog/selection/fingerprint checks, current owner authorization and SDK batch recovery. Work sequentially in the owning repositories using local Debug source references and one C# type per file. Load their tracked guidance/configuration before editing. Obtain the LLM/state baseline from the verified root-declared AI.Tools copy. Keep all earlier unchecked source/deployment/installation/G-4 tasks explicit.

**Never:** Read rich markers, manifests, events, detail/index, content or paths on a resolution query; perform query repair or read-time certification; retrospectively certify old generations; weaken pending/invalidated/unsupported refusal, input bounds or source guards; duplicate persistence/runtime in Conversations; change dependencies, gitlinks, generated code, production composition, sprint, parent Story/spec or acceptance. No Git mutations, deployment, cleanup or nested initialization. Never load skills under references.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Completed proven rebuild | Original native proof, compacted coordinated output and exact completion proof | Content-free current evidence with identical original source and selected generation | No new authority inferred |
| Incomplete/faulted rebuild | Any prepared, installed, committed, partially compacted or missing-proof boundary | Unavailable before rich retrieval | Recover through existing write/rebuild paths only |
| Corrupt/replaced evidence | Missing/malformed/wrong scope, owner, batch, fingerprint, catalog or native proof | Refuse without identity disclosure | No fallback to rich marker or fresh certification |
| Retry/restart | Completion publication interrupted, same original operation retried | Idempotent original proof or honest refusal | No changed cut, clock or generation |
| Legacy/direct writer | Unproven/old rebuild or outer invalidation barrier | Remain unavailable | No migration or historical grant |
| Query effects | Strict read after successful or failed rebuild | Persisted contents unchanged; only allowed content-free keys/transport | Zero repairs, writes or rich reads |

</frozen-after-approval>

## Current execution scope

This source slice is the next bounded task of the existing owner implementation authorization, recorded in the parent prerequisite's Current execution scope. Earlier native enrollment/provenance edits are pre-existing work to preserve. Overall prerequisite remains in-progress and parent Story 6.4 blocked. A new proof must be emitted by the SDK's actual completion path, not manufactured by a domain reader or fixture. An unsupported historical rebuild stays unproven.

The parent has captured baseline patches, original dirty bytes, full revisions/index hashes and protected-file hashes under `/tmp/hexalith-content-free-rebuild-20261021-root/baseline/`. Concurrent external Git activity is observable; record it without performing any Git mutation. The parent already started the exact Projects AppHost with source references, observed security Unhealthy/dependents Waiting, and stopped that exact AppHost normally before edits; no healthy runtime claim. Continue from this baseline without starting other AppHosts.

## Code Map

- EventStore `src/Hexalith.EventStore.Client/Projections/ReadModelBatchProtocol.cs`, `ReadModelBatchMarker.cs`, `ReadModelBatchKeys.cs`, `DaprReadModelStore.OwnerProof.cs`: SDK owns coordinated persisted completion, compact receipts and recovery. Incomplete receipt rows may carry rich operation/manifests, so checking their content after fetching is insufficient. Inspect existing related SDK proof/recovery APIs and reuse an adequate content-free seam if present; otherwise implement the smallest compatible SDK capability.
- EventStore `DomainService/DomainProjectionOwnerProof.cs` and existing signed projection-current transport: preserve exact enrolled owner inventory/catalog, source currentness and authorization. Ordinary proof/legacy behavior stays compatible.
- Conversations `Server/Projections/ConversationAsyncProjectionHandler.cs:289`: rebuild prepares isolated proof/metadata/fence/selection alongside rich outputs. `ConversationResolutionNativeSelection.From(..., rebuild: true)` binds ReceiptProjectionName=rebuild and original native proof.
- Conversations `Server/Queries/ConversationResolutionNativeEvidence.cs:45`: currently rejects every ReceiptProjectionName before delegated current-proof transport. Permit only the narrowly supported content-free completion after SDK capability is proven; keep arbitrary receipt names denied.
- Conversations `ConversationProjectionReadStore.ReadResolutionMetadataAsync`: retain initial and final outer proof gates, ETag/generation/ledger coherence, native evidence and direct-writer invalidation.
- EventStore `tests/Hexalith.EventStore.Client.Tests/Projections/DaprReadModelBatchTests.CoordinatedOwnerProof.cs` and batch fault/recovery tests; Conversations `tests/Hexalith.Conversations.Server.Tests/ResolutionMetadata/` and native enrollment/provenance/recovery tests: reuse real SDK storage/dispatch/delegated transport fixtures, assert actual persisted end-state and strict read instrumentation.

## Tasks & Acceptance

- [x] Trace existing SDK terminal/current-proof APIs and document the exact safe completion boundary. Implement SDK-owned content-free evidence only if no existing seam can satisfy it.
- [x] Publish/verify exact completion evidence from actual successful SDK compaction/recovery, preserving crash safety, immutable scope/fingerprint/owner binding, legacy constructors/JSON and ordinary behavior. Pending, absent, historical or malformed evidence refuses before rich retrieval.
- [x] Consume the supported completion in Conversation native evidence without removing its outer generation/native/current-authority gates or adding a query write.
- [x] Execute matrix coverage through actual SDK storage and dispatch/delegated adapter boundaries: success, each relevant fault/compaction boundary, retry/restart, altered/missing proof, legacy/direct-writer refusal and zero writes/rich reads. Do not replace production execution with hand-seeded successful receipts.
- [x] Run focused SDK source tests and full Conversation Server source suite, retaining every command, exit code, logs and native XML under `/tmp/hexalith-content-free-rebuild-20261021-root/implementation/`. Record exact broad blockers separately and use the required focused fallback; no nested initialization or dependency changes.
- [x] Parent independently inspect incremental diff, verify tests, obtain independent review/corrections, capture final source-bound evidence, and update only this subtask and the parent prerequisite implementation notes. Do not mark the overall prerequisite or parent complete.

**Acceptance Criteria:**
- Given a coherent original proven rebuild has actually completed, when strict metadata/current-proof reads run, then the same original source and selected generation are returned using only content-free storage/transport.
- Given incomplete, changed, unsupported or unproven evidence, when the same read runs, then it refuses before any rich retrieval or query mutation.
- Given interruption/retry or direct-writer invalidation, when recovery or reads run, then original identity and existing failure barriers are preserved.

## Implementation Notes

The initial implementation and parent independent checks both passed 1567 tests (Client 167, Server 294, full Conversation 1106) on source hash `85580258e90828f4bbe339b93ef0b607b63520b328b607b6f8d6f341ec97677a`. Three independent review layers returned thirteen individual claims, retained below before grouping. Corrections remain within the existing private protocol, public wire compatibility, source boundaries and test matrix; no frozen intent or parent gate changes are needed. The intermediate envelope-version approach was superseded by an explicit compacted-only SDK read and is not delivered.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence / required correction |
| --- | --- | --- | --- |
| RC-B1 | medium | patch | Per-route enrollment admits mixed receipt names; ReceiptContains rejects both owners after a successful write. Confirmed by normal binding and independent executed probe; preserve each exact owner and support the admitted mixture. |
| RC-B2 | medium | patch | Nested ProjectionOwnerSelection fields are skipped during deserialization, so appending owners[0].messages leaves the typed digest unchanged. Independent probe accepted the changed proof. Validate complete nested encoded evidence locally without changing the public owner JSON contract. |
| RC-B3 | medium | patch | Completion size is first checked after terminal mutation. Prepared control and terminal proof have different overhead, so reject an over-budget opted-in completion before effects and execute the boundary in both storage profiles. |
| RC-B4 | medium | patch | Identity-mutation tests leave stale digests and therefore do not execute the exact scope/batch/selection checks. Add valid-digest mismatches for independently checked identities; fingerprint currentness remains bound by the existing source-guard evidence, not a newly invented reader authority. |
| RC-B5 | medium | patch | Existing partial-transaction test fails in the earlier general verifier. Exercise the publication read-back guard against persisted output loss after initial verification, retaining actual SDK receipt/storage execution. |
| RC-B6 | medium | patch | The new delegated adapter fixture constructs current proof from the actual owner endpoint; the separate Server production harness executes native current inspection. Add the missing new-receipt positive/refusal through the production Conversation metadata query handler, preserving the explicit fixture authority and G-4 limitation. |
| RC-B7 | medium | patch | The Conversation interruption matrix redispatches JSON but keeps its host/provider. Recreate the host over retained storage and clock for retry; preserve original proof, terminal time and persisted generation. |
| RC-B8 | low | patch | The legacy-reader namespace assertion uses a new batch that was not executed. Execute a separate new batch and read its actual stored selection before simulating the old lookup; no older package or dependency update. |
| RC-B9 | medium | patch | The strict getter adds an expiry-wrapper entry path without focused Dapr/in-memory boundary cases. Execute live, inclusive-expired and malformed wrappers with marker refusal and no writes. |
| RC-B10 | medium | patch | Publication interruption coverage uses only resumable InvalidOperationException. Add lost-response and cancellation reconciliation in both existing profiles with exact original proof/time/end-state assertions. |
| RC-E1 | medium | patch | Same verified mixed-owner defect as RC-B1; independent reflection probe rejects both owners for an admitted mixed receipt. |
| RC-E2 | medium | patch | Same verified nested-field integrity defect as RC-B2; independent reflection probe accepts the appended rich property. |
| RC-V1 | medium | patch | Normal BindRebuildBatch and publication admit mixed owners, while ReceiptContains rejects both. Existing tests uniformly opt all routes in or keep all legacy; preserve the existing positive cases and add partial adoption. |

## Design Notes

This source capability does not establish installed/all-writer coverage, production native read enrollment, cold-activation zero-write, native Folder/Organization generations, committed File/Projects reference mapping, metadata mutation adoption, exact-current qualification or accepted G-4. No passing fixture supplies those authorities.

## Verification

Use individual Debug source test-project builds and built xUnit assemblies with single-dash `-class`/`-method` filters where appropriate. Bind results to unchanged final source/binary hashes; retain failures and superseded attempts. Check owner incremental diffs and `git diff --check`; compare protected files and indexes to the baseline. Evidence archives exclude raw runtime logs containing session grants or environment values.

## Final source checkpoint — 2026-10-21

This bounded source subtask is done. All thirteen review claims were individually triaged, corrected and independently verified. The original blind and edge reviewers confirmed corrected source closure. Final parent Debug builds passed with zero warnings/errors; the exact final unchanged source passed Client batch **195/195**, Server dispatch/reconciliation/idempotency **297/297**, and full Conversation Server **1111/1111**, with zero failures/skips/errors/not-run. These are **1603** tests, not a full SDK suite. Implementer correction checks (88) are additional evidence included within those final runs. Native XML, complete commands/results, source/binary hashes, failed/superseded attempts, baseline dirty bytes, source snapshots and review records are retained in [the completion evidence](evidence/6-4-content-free-rebuild-completion-20261021/README.md).

Final source SHA-256: `6f2eb41395a9b14cb162b95e221dffcdfb3c70ea9314acbb4edcdda21649ab3d`. Private nested proof decoding is strict, mixed-route receipt membership is exact, and proof size is checked before effects. Recovery tests recreate the host over retained storage; the production metadata handler is exercised across success and final completion/source/catalog/permission changes. Fixtures are not deployed authority. The final matrix audit binds every row to executed passing names in coverage-map.json.

Parent protected files, its frozen block, owner dependency/build/style configuration and original dirty bytes are preserved. Concurrent external `/pushall` committed Projects and Conversations while this run remained uncommitted; exact revisions, root pins and index observations are recorded without attributing those Git mutations to this run. Default Conversation whitespace checking reports preserved CRLF as trailing whitespace; the explicit CRLF-aware check passes without changing configuration or line endings. Planning authority validation passes; it does not grant readiness.

The overall prerequisite stays in-progress and Story 6.4 stays blocked. Native-read/installed all-writer authority, cold-activation zero-write, other native owner generations, File/Projects mapping, metadata mutation adoption, exact-current qualification and accepted G-4 remain separate gates. No RTI/parent handler, production composition, deployment or Git mutation was performed by this run. The bounded subtask has no sprint key to synchronize, and its explicit no-Git scope overrides the workflow's generic commit step.
