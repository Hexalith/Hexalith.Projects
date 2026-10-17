---
title: 'Story 6.1: Supported authenticated Project list and open'
type: 'feature'
created: '2026-10-13'
updated: '2026-10-17'
status: 'done'
route: 'dispatch'
baseline_commit: '2b7edae27175fa5921de5768d9580e16d615db34'
review_loop_iteration: 1
context:
  - '/home/administrator/projects/hexalith/projects/AGENTS.md'
  - '/home/administrator/projects/hexalith/projects/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/projects/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore/AGENTS.md'
  - '/home/administrator/projects/hexalith/projects/references/Hexalith.Folders/AGENTS.md'
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
implementation_gate: 'local-complete-production-blocked'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Authenticated callers lack supported DomainService list/open reads with authorization-filtered Project truth and the shared admission snapshot.

**Approach:** After the entry gate, add list/open contracts, watermarked models, authenticated handlers, and shadow comparison beside legacy REST. Queries make no Project domain writes.

**Decision:** Use the full Epic 6.1 scope: Chatbot Folder-read filtering, inspection-gated names with durable FR-21 audit, and shadow equivalence.

## Boundaries & Constraints

**Always:** Require P0, usable current P1R, P2, P3, same-baseline architect sign-off, P4 clean-checkout acceptance, complete approved spec, and independent `READY` before runtime work. P4 pins Folder authority, inspection/audit, adapter, and finite normalization contracts. Bind Tenant, original actor, workload, delegation, audience, and action from the authenticated envelope. Authorize before protected validation; reuse platform store/cursor/denial seams; one C# type per file.

**Never:** While `blocked` or `NOT_READY`, do not edit runtime/tests. Do not switch `/api/v1/projects`, hand-roll persistence/cursors, rewrite events, expose unaudited names/protected detail, select candidates, mutate Project state, move pins, or trust caller Tenant headers.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Gate open | Missing prerequisite | No runtime/test/pin edit | Remain blocked |
| Authorized | Current evidence | AD-32 snapshot; default 50/cap 200 list and stable cursor; role-specific visibility | Safe `400` after authorization for structural errors |
| Denied/absent | Denied, cross-Tenant, nonexistent | No protected data | Indistinguishable `404` |
| Degraded | Stale/rebuilding/unknown event | Honest `Partial`/`Unavailable`; no write | `Unavailable` blocks context |
| Shadow | Same authorized query on both paths | Approved canonical result matches | Unexplained delta fails |

**Prerequisite decision (2026-10-03):** Start separate P1R compatibility, replay, and rollback verification for accepted EventStore 3.110.0 / Builds 4.29.1. Story 6.1 remains pending its entry gate.

**Decision (2026-10-13):** The user selected separate P1R prerequisite work. Story 6.1 remains blocked.

**Decision (2026-10-17):** The user approved “Proceed with local implementation”. This supersedes only the pre-implementation prerequisite gate above for local Story 6.1 runtime/tests and fresh validation on current source dependencies. Implement the full feature; preserve all feature acceptance criteria, historical P1R evidence, package pins and release restrictions. P0/P1R/P2/P3/P4 and production acceptance remain blocked. Local implementation completion is not production acceptance.

**Dependency exception (2026-10-17):** The user answered “yes” to implementing the remaining local EventStore/Folders fixes: interrupted conditional-batch recovery and verified caller authorization through EventStore/Folders. This explicitly supersedes the earlier dependency read-only restriction for those fixes and their tests. Preserve all prior work, concurrent differences and historical evidence; no commits, pin changes, publishing, release dispatch or acceptance changes. Complete and verify the local feature.

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md` (Story 6.1) and the base Story 6.1 artifact provide the full behavioral detail. The approved local-work exception above supersedes their pre-implementation gate only.

- `src/Hexalith.Projects.Contracts/Queries/`: reuse AD-32; add separate list paging with default 50/cap 200, preserving existing 25/100 consumers.
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs` and `Authorization/ProjectAuthorizationGate.cs`: reuse authenticated binding and list/read authorization.
- `src/Hexalith.Projects/Projections/{ProjectList,ProjectDetail}/`: reuse pure folds. Persist supported models and a tenant list index with exact global position; aggregate sequence is not the watermark.
- `src/Hexalith.Projects.Server/Projections/ConversationStartSetup/`: existing `IReadModelStore`/`ReadModelWritePolicy` pattern. Server registration already installs platform storage and `/query`; `/project` still calls the legacy projection directly.
- `src/Hexalith.Projects.Testing/Reads/ProjectContextShadowComparator.cs`: reuse comparator structure for list/open.

## Tasks & Acceptance

**Execution:**

- [x] Local-work exception approved on 2026-10-17; preserve blocked production acceptance and prerequisite records.
- [x] `src/Hexalith.Projects.Contracts/Queries/`: add `ListProjectsQuery`, `GetProjectQuery`, `ProjectListPage`, `ProjectListRow`, `GetProjectResponse`; reuse shared admission metadata.
- [x] `src/Hexalith.Projects.Server/Projections/`: persist list/detail through platform storage and wire supported projection dispatch alongside legacy projections. Handle replay, retries, unknown relevant events and exact watermarks.
- [x] `src/Hexalith.Projects.Server/Queries/` and server registration: add handlers, Folder filtering before pagination, inspection audit and scoped platform cursors. Authorized structural errors return safe 400; denied/absent stays indistinguishable 404. Preserve legacy public routes.
- [x] `src/Hexalith.Projects.Testing/Reads/ProjectReadShadowComparator.cs`: compare local authorized results, keys, watermarks, ordering and cursor boundaries with finite documented normalization. Accepted gateway/owner equivalence remains unproven.
- [x] `tests/Hexalith.Projects.Server.Tests/Queries/` and `tests/Hexalith.Projects.Integration.Tests/`: cover the frozen matrix and actual authenticated persisted adapter, replay/restart/retry, isolation and state-store end-state; use the existing Builds runner where applicable.

**Local implementation complete (2026-10-17):** The approved Projects feature and necessary EventStore/Folders dependency fixes are implemented and independently reviewed. Final verification passed 4,941/4,941 tests with zero failures, errors or skips, including genuine gateway/owner HTTP and live Dapr recovery. All confirmed review findings are resolved. This spec's `done` status applies only to the user-approved local scope. Production/OIDC bound-authority support, P1R, accepted-fixture qualification and formal acceptance remain blocked; sprint Story 6.1 stays blocked. Evidence: `/tmp/story61-dependency-fixes-20261017-jt9oasms/local-completion-summary.json`.

**Acceptance Criteria:**

- Given authorized enumeration, when listing, then Chatbot Folder filtering precedes pagination; Tenant-role responses omit names unless inspection-authorized, with one durable FR-21 audit per inspected request recording counts and field class.
- Given any read, when effects are inspected, then only required metadata security/inspection audit may persist; no Project or sibling mutation occurs.
- Given shadow/replay/restart checks, when executed on the accepted fixture, then persisted evidence proves equivalence and every unexplained delta fails.

## Implementation Notes

- Work in the Projects repository; preserve all submodule work, the 61 recorded EventStore differences and evidence/manifests/seals/cache backup. No Git mutations, pin moves, releases or recovery scans. Resume HEAD is `899a84ccd273062e93596a389215eee16e109770`; the older frontmatter baseline is historical.
- Keep fresh build/test artifacts under `/tmp/story61-local-validation-20261017-Z6iks7`; source Debug build and 135 authentication/query tests already passed. Aspire isolated Projects AppHost started successfully; `aspire wait projects --timeout 45` passed healthy in 18 seconds; resource snapshot is `aspire-ready.json`. Projects, its workers/UI, Tenants and identity were healthy; EventStore was `Finished` in the final snapshot, so full-topology health is not claimed. Parent stopped only this owned AppHost normally; other AppHosts remain untouched. Validate EventStore/persistence in the fresh integration lane.

- Initial 2026-10-17 investigation, before implementation: list/open handlers and persisted list index were absent. P0 runner and P2/P3 runtime capabilities already exist; older “not implemented” notes are stale. Their formal acceptance remains separate. Runtime/tests were unchanged at that initial observation; subsequent authorized implementation is recorded above.
- Reuse this spec. Stop recovery searches; preserve `/tmp/p1r-limit-triage-20261017-7v02juq2/recovery-summary.json` and the unchanged retention manifest. New validation outputs live outside historical evidence.

## Spec Change Log

- 2026-10-17 review loop 1: DB04/DB05 and DE03 exposed missing crash-completion/cleanup and failed-sequence recovery requirements. Added the correction plan outside frozen intent. Known-bad states avoided: orphan retained manifests and stale replay falsely returning Current. KEEP all approved working paths and preserve code/evidence; the user’s preservation instruction overrides workflow reversion.

- 2026-10-17 review: preserve the SDK store/batch/cursor, authorization-first behavior, legacy routes and blocked production gates. Apply the confirmed local corrections incrementally; no reversion of existing work or new dependency edits is authorized.
- 2026-10-17 B09 verification: the original plan assumed resumable conditional batches were available through the existing SDK seam. That assumption is disproved. Keep the working local contracts/handlers/projection consistency and the failing recovery test. Amend the integration plan to require platform-owned conditional-batch recovery; dependency work awaits the explicit user exception. Existing code is preserved as instructed, without a reset or acceptance change.

## Review Triage Log

Independent review on 2026-10-17; all three layers completed before triage. Each finding is retained below. Fixes preserve the approved intent and existing work.

| Finding | Verdict | Route | Evidence and resolution |
| --- | --- | --- | --- |
| B01 | high | patch | Canonical identifiers with dots or length 129–256 pass the platform boundary but fail SafeIdentity; reuse the platform validator. |
| B02 | high | patch | A delayed existing-aggregate update below the tenant maximum changes detail without changing SameIndex or cursor scope. Bind the SDK index ETag separately from exact GlobalPosition. |
| B03 | high | patch | Scope omits EventStore authority freshness and the filtered Folder visibility view; a grant before the continuation boundary can be skipped. Bind the observed visibility view and authority evidence. |
| B04 | false | reject | No index is missing enumeration evidence, explicitly Unavailable under the approved matrix. Tenant membership alone cannot establish a known-empty Project history. Do not synthesize empty/current evidence; a future authoritative empty-history initialization seam remains a platform integration limitation. |
| B05 | medium | patch | References copies stored Included after current Folder verification fails. Return current Unavailable with a safe reason. |
| B06 | high | patch | Invalid persisted lifecycle reaches list as an unknown enum and open as invented Active. Reject corrupt singleton evidence safely and return unavailable enumeration without fabricated lifecycle metadata. |
| B07 | medium | patch | Page-level degraded state always has recoveryActions None. Aggregate actionable row recovery and component evidence. |
| B08 | high | patch | Shadow observations reuse hard-coded keys, scope and boundary and do not execute list parity. Improve independent local list/open observations and machine-readable comparison output; accepted full gateway/owner fixture remains blocked. |
| B09 | high | bad_spec | Added optimistic-conflict and interrupted-batch regressions. The latter exposes a real unresolved recovery failure: prior values remain visible but retry never completes. Existing SDK APIs cannot rediscover the original conditional manifest. Local implementation is incomplete; preserve the regression and existing work while the dependency-change exception is pending. No custom recovery framework or concurrency bypass. |
| B10 | high | defer | Pre-existing gateway workload/principal incompatibility is confirmed by the actual workload HTTP test. EventStore and delegated Folder integration require changes outside the approved Projects-only/read-only dependency boundary; Story remains incomplete. |
| E01 | high | patch | Same verified delayed-update defect as B02; preserve exact GlobalPosition and use the SDK index ETag for revision consistency. |
| E02 | high | patch | FolderCurrentAsync collapses Stale/Unavailable into false; Chatbot then skips those rows and reports Complete. Preserve owner outcomes and make unverifiable enumeration Unavailable. |
| E03 | high | patch | Same verified canonical-identifier regression as B01. |
| E04 | medium | patch | Same verified stored Folder Included contradiction as B05. |
| E05 | high | patch | Same verified changed Folder visibility gap as B03. |
| E06 | medium | patch | Ordinary SDK dispatch validates non-null event objects, but Projects registration has no evolution registry and does not validate EventTypeName. Null reaches dictionary lookup and leaves old evidence Current. Classify the event as unavailable. |
| E07 | high | patch | SDK rebuild validates contiguous events but does not compare a supplied RebuildSourceHeadSequence to the last event. Handler can clear unavailable state from a prefix. Reject a supplied head mismatch. |
| E08 | high | defer | Same confirmed upstream runtime authority incompatibility as B10; preserve failing-closed behavior and incomplete status. |
| V01 | medium | patch | Reviewer verified no issued-cursor test advances authority/projection versions. Add version-change and delayed-update regressions. |
| V02 | high | patch | Reviewer verified descriptive list audit persistence/failure is untested, while singleton is covered. Assert count, field class, retry idempotence and failure preventing disclosure. |
| V03 | high | patch | Reviewer verified archive/restore tests bypass the new event catalog. Test supported persisted lifecycle, filters, singleton and replay. |
| V04 | medium | patch | Reviewer verified HTTP routing/status checks skip in normal runs. Add an always-run authenticated in-memory HTTP boundary fixture; retain opt-in real persistence tests without changing gated CI. |
| P01 | high | patch | Parent follow-through: ProjectAuthorizationGate explicitly returns retryable Unavailable for project-specific validator/storage failures, but the executor drops that typed distinction. Preserve it so list cannot report Complete after an authorization outage. |

| DB01 | high | patch | Verified: System.Text.Json supplies enum zero Allowed for a missing owner outcome. Require explicit, unique, valid owner outcome; malformed success is Unavailable. |
| DB02 | high | patch | Verified: recovery validates only the discovery key, while Install/Compact trust another matching envelope's candidate/delete fields. Validate every installed participating envelope before mutation. |
| DB03 | high | patch | Verified: IsEnvelope is documented as a substring prescreen; normal metadata containing "$hxrmb" is rejected by recovery and its previous/final checks. Use structural top-level discrimination and reject malformed actual envelopes. |
| DB04 | medium | bad_spec | Verified: BeforeReceipt runs after all keys compact; pending-key discovery loses the original batch and its retained manifest. Require recoverable completion across this phase without an attempt registry; preserve committed visibility and newer writers. |
| DB05 | medium | bad_spec | Verified: CompensateAsync persists Aborted with OriginalManifest/OriginalManifestKey, then Projects retries a new ETag identity. Reclaim retained payload only after proven compensation, preserving guarded-owner semantics. |
| DB06 | medium | patch | Verified: QueryEnvelope compares CallerAuthority through generated record equality, whose list fields use reference equality. Equivalent authority must have ordinal sequence equality and consistent hashing. |
| DB07 | medium | patch | Verified: QueryCallerAuthority retains caller-owned mutable lists; VerifiedQueryCaller snapshots principal claims separately from later consumers of the envelope. Defensively capture authority and prevent mutations through exposed lists while preserving wire compatibility. |
| DB08 | medium | patch | Verified: bounded claim capture throws InvalidOperationException outside controller error mapping, producing 500 for a validated token with oversized claims. Translate to a bounded authorization refusal. |
| DB09 | low | patch | Verified: handler resolution and duplicate-route checks occur after feature installation but before finally. Extend cleanup scope so all exits clear request authority. |
| DB10 | low | patch | The index inherits the SDK's explicit 1 MiB canonical-manifest boundary; large tenants can exceed it. Keep the bounded implementation, document its capacity limit, and test fail-before-write behavior rather than introducing a partitioning framework. |
| DE01 | high | patch | Same verified normal-data discriminator defect as DB03; all affected recovery/protocol comparisons must preserve ordinary values. |
| DE02 | high | patch | Same verified cross-operation corruption defect as DB02; test corrupt non-discovery candidate/delete/ordinal evidence with zero recovery writes. |
| DE03 | high | bad_spec | Verified: failure retains the last successful Sequence, and Fold accepts replay ending at that sequence; an older prefix clears Freshness and the unavailable ID without covering the failed event. Persist the recovery floor and require incremental/full rebuild coverage through it. |
| DE04 | medium | patch | Verified: the protected cursor embeds the entire raw canonical scope, including unrestricted authenticated scope/audience strings; Projects rejects incoming tokens over 4096. Hash the complete canonical scope before SDK protection and preserve all bindings. |
| DE05 | high | patch | Same verified malformed owner outcome defect as DB01; no affirmative authorization may be inferred from serializer defaults. |
| DE06 | high | patch | Verified: missing owner signing setup or null issuer output returns safe-denial, which Projects treats as a normal filtered denial. Keep invalid caller evidence denied, but represent inability to obtain owner authority as Unavailable. |
| DV01 | medium | patch | Pre-verified gap: production Folders host tests cover only process/project while the live fixture registers query/metadata policy separately. Extend the real host policy matrix with both new callbacks. |
| DV02 | medium | patch | Pre-verified gap: unexpected remote 400, malformed JSON and transport exceptions lack sentinel assertions for closed error mapping. Add focused invoker regressions. |
| DV03 | high | patch | Independently verified the DB01/DE05 serializer-default authorization defect. |
| P02 | medium | patch | Full SDK rerun exposed shared durable identity in PendingBatchRecoveryLiveTests: later uses fixed aggregate other and batch later across distinct random key sets. Include the random test scope in that aggregate and verify repeatability. |

| CB01 | high | patch | Verified: dispatcher skips binding verification when CallerAuthority is removed even if the authenticated principal carries DigestClaim. Verify when either proof or body authority exists; preserve unbound legacy dispatch. |
| CB02 | high | patch | Verified: the newly added persisted projection/rebuild endpoints accept ordinary authenticated bearer callers in a non-runtime composition. Remove that fallback; only map these mutation endpoints with existing workload security and require its Project policy. Legacy routes remain unchanged. |
| CB03 | medium | patch | Verified: Projects accepts projects:role, but gateway capture omits it with stock JWT RoleClaimType; the live fixture overrides that default. Preserve the authenticated query-domain role namespace explicitly and test standard configuration, without promoting scopes or foreign-domain roles. |
| CB04 | high | patch | Verified: owner gateway 401/403 are authentication/provenance failures before owner ACL evaluation, yet the client reports safe-denial and Projects filters them as Complete. Map those failures to Unavailable; preserve actual safe owner denial. |
| CB05 | medium | patch | Verified: an inspected request reused after the observed revision changes either refuses changed counts or silently reuses the old observation audit. Bind inspection idempotency to the stable observed revision as well as the request; unchanged retries still deduplicate. Keep the stored metadata-only contract and public surface. |
| CB06 | high | patch | Verified: ReadProof requires v but missing st becomes enum-zero Prepared, including after removing Aborting status. Require explicit status and reject malformed evidence before recovery mutation. |
| CB07 | high | patch | Verified: a CreateOnly installed envelope can carry fabricated non-null previous JSON and pass current recovery checks; compensation would persist it. Validate prior absence/presence against original concurrency before any mutation. |
| CB08 | medium | patch | Verified: execution accepts configured limits above fixed recovery proof/codec ceilings. For the new opted-in mode enforce the existing immutable ceilings before state access, while retaining lower configured limits and existing non-opted behavior. No higher scale contract or new options surface. |
| CB09 | medium | patch | Verified: the raw literal prescreen misses a JSON-escaped top-level discriminator even though the structural decoder recognizes it. Make discrimination structurally correct for escaped names and preserve ordinary/nested metadata. |
| CB10 | medium | patch | Verified: undefined freshness can reach response components, and Current/default observation timestamps are accepted in model/index metadata. Normalize corrupt persisted evidence to bounded Unavailable; add list/open cases. |
| CE01 | high | patch | Same verified owner 401/403 classification defect as CB04. |
| CE02 | high | patch | Same verified omitted marker status defect as CB06. |
| CV01 | medium | patch | Pre-verified gap: the real delegated Folder ACL behavior is only exercised in the opt-in live source fixture. Add deterministic dispatcher-driven Folders tests for allowed/revoked/missing permission and stale/unavailable evidence; do not change CI or package pins. The new stale test exposed existing layered evidence with Retryable=false; preserve its typed Stale outcome before the retryability fallback. |
| CV02 | medium | patch | Pre-verified gap: singleton final model/tenant-scope checks lack concurrent-change assertions. Add tests using the existing Folder read hook and assert safe-denial with no payload. |

## Verification

- Final local verification: 4,941/4,941 passed, zero failures/errors/skips. Projects Server 938; Integration 58; EventStore Client 1,648; DomainService 994; relevant gateway/controller/invoker 147; query contracts 155; QueryRouting 14; Folders Server 987. Affected Debug source-reference builds passed with zero warnings/errors. Exact commands, XML and logs are in `final-verified-parent-commands.json` and `final-verified-parent-tests.json` under `/tmp/story61-dependency-fixes-20261017-jt9oasms`. Local list/open shadow comparison has zero divergences and `AcceptedGatewayOwnerFixture: false`. Earlier red results below remain historical evidence.
- Final corrected-tree validation: Debug Server.Tests and Integration.Tests builds passed with zero warnings/errors. Full Server.Tests: 922 total, 921 passed, one failed, zero skipped. The only failure is `SupportedProjectProjectionTests.InterruptedBatchRetainsBothPriorValuesUntilRetryCompletes` (line 199): expected Completed, actual Retryable. All 58 Integration.Tests passed. Exact commands, XML, local shadow result and resource cleanup are recorded in `/tmp/story61-local-validation-20261017-Z6iks7/local-implementation-summary.json`.
- The always-run JWT HTTP fixture and both opt-in Dapr HTTP fixtures executed. The workload callback fixture proves safe denial until upstream authority exists; it does not prove successful production reads. The local shadow report is equivalent with zero divergences and `AcceptedGatewayOwnerFixture: false`.
- Before review corrections: local Debug build passed with zero warnings/errors; 112 targeted and all 890 Server.Tests passed with no skips. These earlier results do not supersede the final red recovery regression.

- 2026-10-17: `python3 -B tools/planning/validate_production_authority.py --story-id 6.1` passed (scheduling only). Entry/owner approvals remain absent; no acceptance status changed.
- Fresh baseline: `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 --artifacts-path /tmp/story61-local-validation-20261017-Z6iks7/artifacts`; passed with zero warnings/errors; `server-build.log` beside artifacts. Focused built-assembly authentication, principal binding and query composition tests: 135 passed, zero failed/skipped (`auth-query-tests.xml`).
- Run individual built xUnit assemblies with single-dash `-class`/`-method` filters, then relevant complete test projects. Builds runner rejects `--filter`; use a declared native-test profile instead of the stale Story 6.1 command.
- `git diff --check` passed; approved local-work exception is the only change to frozen intent.

## Approved Dependency Implementation — 2026-10-17

This continuation implements the two remaining capability groups, without expanding into release or P1R work. Keep the existing local implementation, its truthful failure regression, SDK persistence/cursor reuse and auth-first behavior. Do not revert code or create a replacement framework. The parent owns this spec/status and final review. The implementation agent owns runtime/tests and `docs/supported-project-reads.md`, including necessary EventStore/Folders source and documentation changes; work sequentially and inspect the owning repository guidance/config/tests before editing. No skills under `references/` may be loaded.

Fresh verification outputs: `/tmp/story61-dependency-fixes-20261017-jt9oasms`. Its `source-before.json` records pre-change hashes and Git boundaries for all three repositories. Prior validation `/tmp/story61-local-validation-20261017-Z6iks7` and P1R reports stay unchanged. Existing isolated Dapr/Redis resources were cleaned up; fresh owned resources may be started for local tests. Debug source references only; use explicit source properties when building EventStore/Folders independently. Reuse the already completed Projects Aspire baseline; no host/resource configuration changes are required by this work.

### Implementation and tests

- [x] Add the smallest SDK-owned recovery capability that can discover/reconcile the original pending conditional manifest from existing batch metadata, then safely retry a compensated optimistic conflict with fresh ETags. Reuse the existing protocol and preserve identity/fingerprint and scope isolation. No domain-owned attempt registry, last-write bypass, raw Dapr in Projects, Redis profile switch, or fabricated completion. Test partial installation plus failed reconciliation, retry convergence, concurrent writers on the tenant index, mismatched/corrupt metadata and scope refusal. Consume the seam from Projects; make `InterruptedBatchRetainsBothPriorValuesUntilRetryCompletes` pass without weakening it. Include real Dapr persistence/restart evidence where feasible.
- [x] Carry authenticated originating actor/role/distinct inspection authority through the EventStore query invocation using existing signed workload assertion bindings. Bind the exact immutable query (tenant, actor, originating workload, delegation, audience, query/action/target and payload), with verification at the receiving SDK boundary. Derive evidence only from authenticated gateway authority; do not trust ordinary payload/header fields or promote OAuth scopes to permissions. Preserve FR28's prohibition on forwarding human bearer credentials, unique-claim validation, TTL/audience/operation checks and bearer/workload conflict rejection. Do not relax runtime workload policy.
- [x] Give Folders a supported, narrowly scoped delegated owner-read path that consumes verified authority and checks its current lifecycle/permissions for the originating actor. Reuse owner query/authorization semantics and platform transport. Projects consumes it for Folder filtering and labels. A command-only human-authority receipt is not query authority. No owner mutation or workspace-path workaround. Preserve legacy public routes and existing Folder behavior.
- [x] Exercise a successful genuine workload callback through the actual gateway authority producer, receiver and Folder owner authorization path, plus absent/denied indistinguishable 404, changed tenant/actor/payload/target/audience, forged/duplicate/expired authority, inspection without the distinct permission, revoked Folder access and outages. Test the full local list/open/pagination path and persisted end-state with real SDK implementations. Deterministic external identity/Tenant fixtures are allowed for local validation; do not label them formal P4/production acceptance.
- [x] Run focused tests for changed EventStore, Folders and Projects code; report exact commands/results and any remaining limitations. Parent will run wider relevant suites and independent review after implementation. Source metadata/proof/cursor or batch details must not leak through denial/logging surfaces.

Existing source investigation and exact producer/consumer paths are in `docs/supported-project-reads.md`. The current regression baseline is 921/922 Server tests and 58/58 Integration tests; the only failing test is interrupted recovery. The known runtime workload query denial is a real incomplete feature even though its fail-closed regression passes. Implement these capabilities to completion within this authorization; ordinary design choices need no further permission.

## Review Correction Plan — 2026-10-17

Review loop 1 addresses the individually recorded DB/DE/DV/P02 findings above. The user requires preservation and pragmatic local completion: preserve existing code rather than reverting/re-deriving it wholesale. KEEP the passing supported contracts, authorization ordering, explicit RequiredQueries compatibility opt-in, exact workload proofs, guarded-owner semantics, current Folder ACL checks, conditional writes, real gateway fixture, source-only dependency boundaries, and all historical evidence. No new approval is needed for these local corrections. Production/sprint/release restrictions remain unchanged.

Implement the smallest corrections for every patch row. For DB04/DB05, refine only the SDK's opted-in recovery mode: retain a discoverable original identity until the terminal transition can be recovered, including interruption just before receipt; reclaim retained manifests only after proven terminal completion or compensation. Do not add a Projects attempt registry, scans, provider-profile switches, raw state access, or weaken concurrency/guarded-owner semantics. Exercise all new crash boundaries and inspect stored marker/manifest cleanup, including repeated optimistic conflicts. The old non-opted and guarded modes must retain their behavior.

For DE03, record the highest failed/observed sequence needed for recovery in the persisted Project read model without relabeling Sequence or GlobalPosition. A replay ending at the last successful sequence, or any explicit rebuild source head below that floor, cannot clear degradation. A valid complete replay through the floor can. Cover both incremental redelivery and supported rebuild paths. Preserve old serialized-model compatibility conservatively.

For DE04, hash the entire existing QueryCursorScope canonical string, preserving every authority/filter/watermark field; continue to use the platform codec for protection. Update the independent shadow expectation and prove that valid long scope/audience inputs yield a usable bounded continuation.

Keep the current tenant index and existing SDK size ceiling (DB10). Document the capacity limit and prove oversized batches fail before modifying detail/index; do not add a partitioning system or change command/domain admission rules. This is a declared local scaling limit, not a production qualification.

Parent broader validation: Projects Server 924/924 and Integration 58/58; QueryRouting 14/14; Folders Server 979/979 after placing an unchanged copy of the built assembly beneath the owning repository. EventStore full Client has five old source-root fixture lookup failures plus P02. DomainService has 11 old source-root guardrail lookup failures; all other 974 tests passed. Full Contracts includes unrelated packaging/evidence suites whose root lookup fails under /tmp, and a pre-existing package skip; the parent will use the relevant query-contract suite rather than expand into gated package evidence work. Do not change these old lookup helpers or gated evidence. The parent handles fresh ignored runner directories beneath owning repositories and preserves/deletes only its own copies.

The implementation agent owns runtime/tests/docs corrections; parent owns this plan, full verification, review and resource cleanup. Run only affected tests and preserve failed logs. Use a new correction log/XML prefix under the current evidence directory. Parent will run broader corrected-tree checks afterward.

## Corrected-tree verification and final patches — 2026-10-17

The broader corrected-tree run passed 4,767/4,767 with zero skips: Projects Server 931, Integration 58, EventStore Client 1,639, DomainService 989, query contracts 155, QueryRouting 14, and Folders Server 981. Fresh evidence is `corrected-parent-tests.json` and `corrected-parent-commands.json` under the current report directory. All three independent reviewers then completed; every finding is separately triaged above. Apply only the direct CB/CE/CV corrections, with no public API expansion or redesign, and retain the established production restrictions. The parent will rerun affected checks and preserve the initial green results.

## Local completion — 2026-10-17

All CB/CE/CV patch rows are resolved by the final changes and regressions recorded in `final-patch-results.md`; all prior confirmed B/E/V/P and DB/DE/DV findings have their local corrections applied. The final broad run above supersedes the earlier recovery failure. The independently reviewed changes preserve legacy behavior, current Folder authorization, exact caller bindings, metadata-only inspection audits and existing SDK concurrency rules. No further unresolved local implementation finding remains.

The 27,288-file preservation audit found 27,250 identical files and 38 authorized source/tracking changes, with no unexplained changes, missing files or retention-entry intersections. Of the 61 historical EventStore concurrent differences, 58 are byte-identical; three recovery source files contain the approved extensions while preserving prior guarded behavior. Historical evidence, the retention manifest and cache backup remain unchanged. Current Git heads, staged content and gitlinks remain unchanged; no commit, pin move or publication occurred.

This completion uses current source dependencies and the configured signing-key workload profile. The production OIDC/client-credentials issuer cannot supply exact query bindings and required bound reads fail closed in that profile. Accepted deployed gateway/owner equivalence is unproven. Existing single-index/full-visibility-sweep capacity bounds and omitted File/Memory labels remain documented in `docs/supported-project-reads.md`. These are explicit production/integration limits, not acceptance claims. P1R recovery results remain in `/tmp/p1r-limit-triage-20261017-7v02juq2/recovery-summary.json`; no completed recovery search was restarted.
