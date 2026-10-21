# Story 6.4: content-free rebuild completion source evidence

The bounded [owner source subtask](../../spec-6-4-prerequisite-content-free-rebuild-completion.md) is complete. The overall prerequisite remains in progress and Story 6.4 remains blocked. Newly prepared native-backed Conversation rebuilds can use SDK-owned content-free completion evidence; strict reads refuse incomplete/legacy/unsupported evidence before rich marker retrieval and preserve existing identity, source, authorization and outer invalidation checks.

Final independent Debug builds have zero warnings/errors. Final tests passed **1603/1603**, with zero failures, errors, skips or not-run cases:

| Run | Scope | Passed |
| --- | --- | --- |
| SDK Client | DaprReadModelBatchTests and ReadModelBatchTests | 195 |
| SDK Server | NamedProjectionDispatchCoordinatorTests, ProjectionDeliveryReconcilerTests and ProjectionDeliveryIdempotencyCoordinatorTests | 297 |
| Conversation Server | Full test assembly | 1111 |

These results bind to unchanged final source SHA-256 `6f2eb41395a9b14cb162b95e221dffcdfb3c70ea9314acbb4edcdda21649ab3d` and unchanged test-output DLL hashes. They are not a full SDK suite. The 88 implementer correction cases are included in these independent runs, not added to the total.

Three context-free reviewers filed thirteen individual claims. Every claim was corrected; original blind/edge reviewers confirmed source closure and the parent reran final verification. Initial executed review probes and all superseded or failed implementation attempts remain labeled separately from final results.

- [Verification totals](verification-summary.json), [all executed final test names](executed-tests.json) and [matrix-to-executed-test mapping](coverage-map.json).
- [Complete command records](command-records.json) and [source, revisions and test-output hashes](source-manifest.json).
- [Individual review claims/dispositions](review-claims.json), [review closure](review-confirmations.json) and [initial reproduced defects](review-reproduction.md).
- [Source and writer boundaries](source-and-writer-boundaries.md), [incremental diff against preserved dirty input](incremental.patch) and [cumulative diff against initial canonical commits](cumulative.patch).
- [Preservation and concurrent Git observations](preservation.json), [safe AppHost baseline](runtime-baseline.json) and [archive manifest](archive-manifest.json).
- [Raw verification archive](verification-artifacts.tar.gz): implementation and parent logs/JSON/native XML; original dirty baselines; reviewed, superseded and corrected source snapshots; recorders and initial review probe. The final native XML is under `independent/final-client-tests.xml`, `independent/final-server-tests.xml` and `independent/final-conversation-full.xml`.

The required exact Projects AppHost baseline observed security Unhealthy and dependent resources Waiting, then stopped normally before code work. No healthy runtime acceptance is claimed. Raw runtime logs with session grants/environment values are deliberately excluded from the archive.

Parent Story/spec/sprint and frozen intent are unchanged, as are owner dependency/build/style configuration bytes. Original dirty source bytes are retained, with intentional bounded edits expressed by the incremental patch. Concurrent external `/pushall` commits changed Projects/Conversations heads and root pins; exact observations are retained. This run made no Git mutations. Default `git diff --check` in Conversations reports existing CRLF as trailing whitespace; the explicit `git -c core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol diff --check` passes. No configuration or line endings were changed to suppress the report. Planning authority validation passes and does not establish readiness.

Remaining gates are production native-read enrollment, installed/all-writer and cold-activation zero-write acceptance, deployed native owner/Tenant authority, native Folder/Organization generations, committed File/Projects mapping, metadata mutation adoption, exact-current qualification and accepted Projects G-4. Native/current/Tenant fixture authority only demonstrates local behavior. No RTI or parent resolution handlers, deployment or production composition changes were performed. The earlier absent nested Tenants broad-SDK guardrail blocker remains recorded in the parent prerequisite; no nested initialization or guard weakening occurred.
