# Story 6.4 advisory resolution evidence — 2026-10-21

The approved Conversation/Folder advisory scope is implemented through the existing authenticated `/query`. Exact query names are `Hexalith.Projects.Queries.ResolveProjectFromConversation.v1` and `Hexalith.Projects.Queries.ResolveProjectFromAttachments.v1`. Both target the Projects envelope aggregate `projects`; payloads contain `conversationId` or `attachments`, plus optional `includeArchived`. Attachments contain `referenceKind` (`folder` or `file`) and opaque `referenceId`.

The response contains authorized IDs, deterministic ranks/scores/reason codes, `evaluatedAt`, `incomplete`, and `unsupportedInputKinds`. Evaluation time is not a watermark. Empty incomplete evaluation has a null result; a single suggestion remains unselected. The engine, mappers, strict owner queries, context contracts and confirmation route were preserved.

| Executed lane | Passed | Failed / skipped | Evidence |
| --- | ---: | --- | --- |
| Projects advisory matrix | 39 | 0 / 0 | `projects-matrix.xml` |
| Conversation advisory owner | 17 | 0 / 0 | `conversations-matrix.xml` |
| Authenticated gateway composition | 1 | 0 / 0 | `gateway.xml`; also executed in final Projects suite |
| Full Projects.Server | 977 | 0 / 0 | `projects-full.xml` |
| Full Conversations.Server | 1129 | 0 / 0 | `conversations-full.xml` |
| Existing resolution engine/mappers | 107 | 0 / 0 | `engine.xml` |
| Production-authority guard | 29 | 0 / 0 | `planning-guard.log` |

**Final unique test count: 2,242** (977 Projects.Server + 1,129 Conversations.Server + 107 engine/mapper + 29 planning guard). Focused runs are subsets; owner authorization/parity/advisory also passed 98/98 and strict owner tests 55/55.

`commands.json` records exact successful commands and working directories. `matrix.json` maps every spec row to executed passing tests, including final Folder re-anchoring, lifecycle changes, Project permission revocation, rejected attachment filtering, the shared two-second deadline, four concurrent calls, raw attachment limits and 200 distinct candidate identities. `executed-tests.json` lists the XML test cases. `source-manifest.json` binds implementation/test files by SHA-256. `preservation.json` confirms all pre-existing dirty Conversations and EventStore files remain byte-identical and Folders has no source changes.

The authenticated case uses real JWT authentication, EventStore gateway/SDK dispatch and signed bound-owner transport, the real Folder owner handler/ACL evaluator, the real Conversation advisory handler/ordinary `ConversationTenantAccessService`, and SDK `IReadModelStore` over Dapr/Redis. `GatewayFixtureLoopbackHandler` replaces service discovery only. `GatewayFixtureOwnerHeads` supplies controlled Folder source heads; `GatewayFixtureConversationTenants` supplies controlled healthy permission projection evidence over real persisted tenant state. Keys use a unique test tenant inside the isolated Dapr app namespace `story64-advisory-20261021`. The test compares exact Dapr-returned persisted bytes for Projects index/details, Conversation isolated metadata and Conversation tenant state before/after both queries. In-memory guarded tests additionally reject writes/rich reads and compare store count and bytes.

The current Folders composition has no legacy operational-index-metadata route. The test enrolls query names from its actual registered handlers, then executes the real owner route. Projects enrollment still uses its actual metadata route. Build discovery for the Folders sibling required explicit EventStore/Commons/Tenants source-root properties; the final Debug builds passed with no warnings or errors, without package-version changes or nested submodule initialization.

Raw `git diff --check` passes in Projects. In Conversations its default whitespace interpretation flags CRLF on added serializer lines and pre-existing dirty `ResolutionRawReadStore` lines. The repository baseline requires CRLF; the per-command `core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol` check passes in both repositories without rewriting existing dirty bytes. See `diff-check.log`. Production-authority Story 6.4 and index validation both pass.

File identity mapping remains unsupported: presented Files set incomplete coverage and `unsupportedInputKinds: ["file"]`; no File matching or full File acceptance is claimed. Candidate detail fanout is bounded to 200 distinct IDs, with final rereads of retained IDs; the existing monolithic index has no claimed byte bound. Matching observations may lag and cannot authorize context, first response or association; existing confirmation/context regression cases passed in the full Projects suite.

This is source acceptance under controlled local authority/storage fixtures, not deployment or G-4 runner acceptance. A real deployment must configure the existing signed-owner route to permit `Hexalith.Conversations.Queries.GetConversationAdvisoryMetadata.v1`, register owner query catalogs and provide its actual workload/permission authority. No host migration, dependency-version update, deployment, Git staging/commit/push/branch, inference history or query repair was performed. Test runner/build logs and XML are retained; runtime/application logs and generated credentials are excluded.

Review corrections validate consumed Project fields and accepted Folder bindings, retain successful peers when one final read fails, bound local ID selection, and cover changed observations/fences and known erasure. Three independent reviewers were triaged; all accepted patches and documentation corrections are resolved.

The first corrected-tree gateway run failed because the shared evaluator discarded healthy permission evidence. The existing Conversation facade now preserves the exact evaluated observation per invocation without a second read or shared cache. A regression checks overlapping callers. The next full owner run exposed an older assertion of that discarded-evidence behavior; its replacement verifies preserved health, absent default evidence, and continued strict native-proof enforcement. Historical failures are retained as `projects-review-failed.xml/.log` and `conversations-review-failed.xml/.log`; the final full runs above supersede them.

Deployment must supply usable permission projection version/watermark evidence through the existing Conversation access service. The default nonsignal store supplies none and therefore withholds advisory metadata with incomplete coverage. No replacement authorization platform was added.
