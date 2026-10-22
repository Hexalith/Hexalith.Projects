# Story 6.4 second-pass review patches — 2026-10-21

All 18 local review patch groups are implemented. The Conversations 1.3.0 release and consumer pin are the only open patch; they require the explicit push and release go-aheads recorded in the spec. Story status remains `in-progress`. No stage, commit, branch, push, dependency-version edit or deployment was performed.

| Executed lane | Passed | Failed / skipped | Evidence |
| --- | ---: | --- | --- |
| Projects focused advisory/legacy/HTTP/catalog/gateway | 136 | 0 / 0 | `projects-focused.xml` |
| Conversations advisory and strict owner controls | 78 | 0 / 0 | `conversations-focused.xml` |
| Full Projects.Server | 1012 | 0 / 0 | `projects-full.xml` |
| Full Conversations.Server | 1135 | 0 / 0 | `conversations-full.xml` |
| Existing resolution engine and mappers | 107 | 0 / 0 | `engine.xml` |
| Production-authority guard | 29 | 0 / 0 | `planning-guard.log` |

The final suites contain **2,283 unique passing tests** (1012 + 1135 + 107 + 29). Focused runs are subsets. Builds for all three projects passed with zero warnings or errors. The Projects focused lane includes 65 advisory cases, 51 legacy endpoint cases, 16 module cases, three HTTP cases and one real gateway composition case. The Conversation lane includes 22 advisory cases and 56 strict cases.

`commands.json` records exact working directories and commands. `matrix.json` maps all seven spec matrix rows and every local review patch to individual passing tests in the full-suite XML. `executed-tests.json` lists the executed cases and counts, including superseded failures. `source-manifest.json` binds the changed source, tests, consumer guidance and spec by SHA-256. Frozen scope text is unchanged.

The real gateway case executed with authenticated JWT caller authority, signed delegated owner requests, actual Conversation and Folder handlers/authorization and SDK read-model storage over isolated Dapr/Redis. The existing test compares exact persisted bytes before and after both advisory queries. `GatewayFixtureLoopbackHandler` replaces discovery; `GatewayFixtureOwnerHeads` and `GatewayFixtureConversationTenants` provide controlled source-head and tenant-health authority observations. This is local source acceptance under controlled fixtures, not deployment, production OIDC or G-4 acceptance. `fixture.json` records the fresh app namespace, loopback ports, setup commands, outbound health and successful cleanup. No shared resources were changed. Raw application/runtime logs and generated credentials are excluded; retained runner logs contain only runner discovery/summary lines.

The required current-run Aspire baseline was completed by the parent before edits: Projects/UI/workers/Tenants/security and sidecars were healthy; EventStore finished without becoming healthy. The exact AppHost was stopped normally. No deployed-acceptance claim follows from that baseline.

The shared admission checks require `projects.read`, deployment audience, verified original authority, eligible operator/delegated Chatbot role and fresh Tenant evidence with a watermark. They run before protected reads and before disclosure. Native permission proof remains mandatory for the strict Conversation query; its legacy local-health branch now explicitly withholds permission evidence. Strict tests now supply an explicit native transport adapter while retaining all prior Read/health/decision fault hooks. The separate `nativeAuthorization:false` regression demonstrates that usable preserved local evidence still cannot authorize strict metadata.

Superseded failures are retained. `projects-focused-failed.xml` contains one logging-test assertion failure: source-generated state had the correct safe keys in a different order; the corrected assertion checks the exact key set. `projects-mixed-graph-failed.xml` contains one unrelated workers failure: Folders used packaged Commons.UniqueIds/2.30.3 while EventStore expected the source assembly/3.126.0. `workers-minver-only-failed.xml` records that a MinVer override alone did not resolve it. The final Projects build uses explicit EventStore, Commons and Tenants source roots, matching the prior accepted source lane; `workers-source-roots.xml` passes 2/2 and the full final suite passes 1012/1012. No dependency or build-gate source was changed. Earlier compile failures (mixed Folders SDK graph, temporary route argument, test ConfigureAwait/null validation) are retained in named build logs; final builds supersede them.

Raw `git diff --check` passes in Projects. In Conversations it flags CRLF endings on added lines, matching that repository's pre-existing CRLF files and the shared baseline requirement. The retained raw result is exit 2; the exact per-command CRLF-compatible check in `commands.json` passes with exit 0, without rewriting unrelated files. Both production-authority validators pass; Story 6.1 and other gates remain unchanged.

Files remain explicitly unsupported/incomplete; no File matching is fabricated. Matching metadata may lag; lag alone is not incomplete. Candidate selection is ordinal-first and bounded to 200 details (an observed linked hint may reserve one slot); the monolithic index is not byte-bounded. Empty/File-only/fully denied inputs and linkless/labelless Conversation observations skip Project fan-out while final owner/scope checks remain. Queries persist no inference, event, repair or selection; confirmation and context keep their independent authority.
