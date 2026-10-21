---
title: 'Story 6.4 owner prerequisite: content-free rebuild completion proof'
type: 'feature'
created: '2026-10-21'
updated: '2026-10-21'
status: 'in-progress'
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

- [ ] Trace existing SDK terminal/current-proof APIs and document the exact safe completion boundary. Implement SDK-owned content-free evidence only if no existing seam can satisfy it.
- [ ] Publish/verify exact completion evidence from actual successful SDK compaction/recovery, preserving crash safety, immutable scope/fingerprint/owner binding, legacy constructors/JSON and ordinary behavior. Pending, absent, historical or malformed evidence refuses before rich retrieval.
- [ ] Consume the supported completion in Conversation native evidence without removing its outer generation/native/current-authority gates or adding a query write.
- [ ] Execute matrix coverage through actual SDK storage and dispatch/delegated adapter boundaries: success, each relevant fault/compaction boundary, retry/restart, altered/missing proof, legacy/direct-writer refusal and zero writes/rich reads. Do not replace production execution with hand-seeded successful receipts.
- [ ] Run focused SDK source tests and full Conversation Server source suite, retaining every command, exit code, logs and native XML under `/tmp/hexalith-content-free-rebuild-20261021-root/implementation/`. Record exact broad blockers separately and use the required focused fallback; no nested initialization or dependency changes.
- [ ] Parent independently inspect incremental diff, verify tests, obtain independent review/corrections, capture final source-bound evidence, and update only this subtask and the parent prerequisite implementation notes. Do not mark the overall prerequisite or parent complete.

**Acceptance Criteria:**
- Given a coherent original proven rebuild has actually completed, when strict metadata/current-proof reads run, then the same original source and selected generation are returned using only content-free storage/transport.
- Given incomplete, changed, unsupported or unproven evidence, when the same read runs, then it refuses before any rich retrieval or query mutation.
- Given interruption/retry or direct-writer invalidation, when recovery or reads run, then original identity and existing failure barriers are preserved.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

This source capability does not establish installed/all-writer coverage, production native read enrollment, cold-activation zero-write, native Folder/Organization generations, committed File/Projects reference mapping, metadata mutation adoption, exact-current qualification or accepted G-4. No passing fixture supplies those authorities.

## Verification

Use individual Debug source test-project builds and built xUnit assemblies with single-dash `-class`/`-method` filters where appropriate. Bind results to unchanged final source/binary hashes; retain failures and superseded attempts. Check owner incremental diffs and `git diff --check`; compare protected files and indexes to the baseline. Evidence archives exclude raw runtime logs containing session grants or environment values.
