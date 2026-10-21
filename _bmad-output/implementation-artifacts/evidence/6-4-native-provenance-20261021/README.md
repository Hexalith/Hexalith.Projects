# Story 6.4 native Conversation generation provenance

Status: **reviewed source slice complete; prerequisite in progress; Story 6.4 blocked**.

EventStore carries a producer-frozen native incarnation, complete prefix and head with the original full-prefix projection input. SDK dispatch verifies that original cut before evolution/materialization. Conversation metadata, lasting generation, digest/fingerprint and selected owner batch retain exactly the admitted proof. Existing SDK atomic writes and recovery remain authoritative. Conversation metadata queries retrieve no Conversation events, detail or index and compare complete current native evidence; query reads never certify or repair older generations.

Original proof or absence is frozen in existing durable retry work and retained across ordinary, compatible, recovery and reconciliation requests. An append can verify the original supplied cut within an authenticated complete prefix; its newer head never replaces the cut. Legacy generations, old retry records, missing verification authority, unsupported protected/evolved provenance and incomplete rebuild/recovery remain unavailable for resolution metadata. Ordinary protected/legacy projection delivery is preserved. The authenticated native event path preserves exact actor metadata; ordinary stream normalization and head-only inspection stay intact.

## Independent verification

| Final Debug source check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| SDK projection/native/delegation | 86 | 0 | 0 |
| Producer/scheduler/native/controller | 393 | 0 | 0 |
| Full Conversation Server | 1055 | 0 | 0 |
| Total | 1534 | 0 | 0 |

Independent test-project builds succeeded with zero warnings/errors. Every command retains its actual argv, working directory, timing and before/after source hashes in [command records](command-records.json). [Native result counts](verification-summary.json), [full source revisions and binary hashes](source-manifest.json), and the [reviewed owner patch](reviewed-owner.patch) bind these results to the uncommitted owner sources. Final native XML, complete logs, executed test names, recorder, original/review snapshots and superseded failures are in [verification artifacts](verification-artifacts.tar.xz).

The earlier full SDK run was 1090/1091: `DomainModuleAuthoringGuardrailTests.TenantsDomainService_DoesNotReferenceGeneratedApiHostOrDeclarePerMessageControllers` requires an absent nested `EventStore/references/Hexalith.Tenants` project. Nested initialization is unauthorized. The guard and dependency layout remain unchanged; focused SDK results above are separate. Earlier full Conversation runs failed two stale legacy expectations and then 28 cases sharing an unconfigured rebuild fixture. The corrected fixture supplies the exact supported native gateway and an untrusted JSON request; the final full suite above supersedes those runs. No failed or filtered run is promoted as a pass.

Coverage includes tampering, incarnation/replacement, source advance, frozen retry proof and absence, persisted scheduler scope/replacement checks, original proof across replay/rebuild/recovery, real rebuild/reconciliation admission, actual raw-envelope codec interoperability, 101-event paging, production signed delegated adapter success/refusal, and content-free persisted reads. Mutation hashing measured 8,720 allocated bytes for five checks over an 8 MiB payload; this does not qualify production performance.

## Review and preservation

Three context-free reviewers filed fifteen separate claims. Parent execution added three findings. [Individual verdicts and follow-up confirmations](review-results.json) retain all claims; NP-P2 is the existing layout blocker above. All in-scope corrections passed parent verification. The [prerequisite review log](../../spec-6-4-prerequisite-current-authorized-resolution-evidence.md#native-provenance-source-review--2026-10-21) records every disposition.

[Preservation checks](preservation.json) bind parent story/spec, sprint, frozen intent, D1-D6, dependencies/configuration and existing native-head work. Concurrent external commits changed workspace/owner heads and gitlink state during inspection; their revisions and snapshots are retained. This run performed no staging, commit, push, branch, dependency, gitlink, submodule or deployment mutation. The prior evidence bundle is unchanged.

## Remaining blockers

Production native read enrollment and legacy-compatible guarded Conversation composition, content-free post-rebuild promotion, installation/all-writer coverage, cold-activation zero-write evidence, deployed owner authority, native Folder/Organization generation binding, committed File/Projects reference mapping, production metadata mutation adoption, exact-current qualification and accepted Projects G-4 evidence remain incomplete. Protected/evolved or retained-away source inputs without supported exact native admission stay unproven. Legacy generations receive no migration or retrospective certificate.

The [runtime baseline](runtime-baseline.json) started the exact Projects AppHost with source references, observed security Unhealthy and dependents Waiting, and stopped it normally before edits. No healthy live topology, production acceptance or installation-wide zero-write result is claimed. RTI and parent resolution handlers remain gated; parent Story 6.4 and sprint tracking remain blocked.
