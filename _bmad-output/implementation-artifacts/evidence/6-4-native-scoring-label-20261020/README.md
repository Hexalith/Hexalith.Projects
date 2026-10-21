# Story 6.4 native scoring-label prerequisite

Status: **reviewed source slice complete; prerequisite in-progress; Story 6.4 blocked**.

Conversation resolution previously classified ordinary legacy labels from their text. The owner now requires explicit bounded scoring-label purpose and classification in native creation or metadata events. Classification participates in idempotency and survives serialized state, publication, projection and rebuild. An ordinary label edit revokes it; unrelated metadata edits preserve it. Existing constructors and absent-field JSON remain compatible. Native creation reaches the aggregate; metadata-update contracts and replay are implemented, while the existing production metadata-mutation dispatcher remains absent.

Independent review produced eleven findings. Ten were addressed in eight correction groups; the pre-existing production mutation-adoption gap is deferred. Corrections reject distinct duplicate creation facts, verify the stored wrapper classification, refuse an old implicit generation without changing its bytes, and test malformed events, recovery barriers, source-generated compatibility and actual generation times. The [prerequisite specification](../../spec-6-4-prerequisite-current-authorized-resolution-evidence.md#review-triage-log) records every verdict and disposition.

## Final verification

| Final source check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Contract behavior, serialization and source boundaries | 131 | 0 | 0 |
| Static package metadata checks | 2 | 0 | 0 |
| Full domain/core suite | 189 | 0 | 0 |
| Full server suite | 1036 | 0 | 0 |
| Total executed | 1358 | 0 | 0 |

All three Debug source builds succeeded. Contracts.Tests retains its existing MSB9008 warning about an absent nested Commons project reference; no nested dependency was initialized. The Release package-inventory execution test was excluded under the local Debug-only policy. This is no package or production qualification.

The parent independently rebuilt and ran these checks against the corrected source. Per-command source snapshots match before and after validation. [Final results](verification-summary.json), [reviewed source hashes](reviewed-source-manifest.json), and the [reviewed patch](reviewed-conversations.patch) bind the result to Conversations `f00d30b8255038058796c4780fcae08e8d6b5ee4` plus its uncommitted changes. The [reviewed archive](reviewed-verification.tar.xz) retains correction runs and final commands, environment overrides, logs, XML, source snapshots, binary hashes and exact temporary-output setup. It also retains the runner used for final validation. Test fixtures use SDK in-memory read-model persistence and serialized native events; these are not deployed owner or G-4 acceptance runs.

The initial 475-test checkpoint, earlier failures and superseded runs remain separate in [initial results](initial-verification-summary.json), [initial command records](initial-command-records.json), [initial source hashes](conversations-source-manifest.json), [initial patch](conversations-final.patch) and [initial verification archive](implementation-verification.tar.xz). No earlier result is rewritten as corrected-source evidence.

## Runtime baseline and retained gates

`aspire start --non-interactive --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --format Json` exited 2: Tenants.Contracts selected published EventStore.Contracts 3.126.0 and failed CS0246 for NativeSourceProof while other projects used source. Retrying with `UseHexalithProjectReferences=true` exited 2 with child exit 255 during the build while the filesystem was full; retaining output also raised Errno 28. The exact cause of child exit 255 was not established. No Projects AppHost remained running, and no runtime readiness or live owner pass is claimed. [Runtime observations](runtime-results.json) and retained sanitized output record these failures; no existing files were deleted to make space.

Production mutation adoption, native Folder/Organization generation binding, legacy-compatible Conversation guard enrollment, content-free post-rebuild promotion, complete installation/all-writer coverage, committed File identity and Projects FileReferenceId binding remain incomplete. RTI, parent resolution handlers, exact-current qualification and accepted Projects G-4 consumption remain gated. No owner acceptance or production authority is inferred from these tests.

[Preservation evidence](preservation.json) verifies the parent story/spec, sprint status, dependency pins, gitlinks/HEADs and frozen prerequisite text. Concurrent 6.1-P0 document changes were preserved and are outside this change. No stage, commit, push, dependency update or deployment was performed.
