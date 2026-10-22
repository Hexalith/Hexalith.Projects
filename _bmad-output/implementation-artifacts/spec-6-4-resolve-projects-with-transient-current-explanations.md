---
title: 'Story 6.4: Authorized advisory Project suggestions'
type: 'feature'
created: '2026-10-17'
updated: '2026-10-22'
status: 'done'
route: 'dispatch'
story_key: '6-4-resolve-projects-with-transient-current-explanations'
baseline_commit: '83781b31e06214ad6da788c6535f9044df06ed5e'
execution_baseline_commit: '4a4640b3a6115c6fcdfd8c46f70e6350cb34ef69'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-21-simplify-story-6-4.md'
---

<frozen-after-approval reason="User approved the concrete advisory scope reset on 2026-10-21">

## Intent

**Problem:** Exact-current prerequisites prevented shipping useful suggestions. **Approach:** Finish two supported advisory queries through the existing authenticated `/query`, using committed metadata, existing scoring, and current owner authorization. Deliver Conversation and Folder scope; File references explicitly remain unsupported/incomplete until their actual mapping exists. The user's “yes” approves this implementation and supersedes the previous frozen prerequisite gate.

## Boundaries & Constraints

**Always:** Bind Tenant, original actor, workload/delegation, action and exact resource. Authorize before metadata/scoring and recheck before disclosure. Reuse weights, deterministic ordering, archive opt-in and existing confirmation checks. Carry `EvaluatedAt` and `Incomplete`; evaluation time is no source watermark. A sole candidate is unselected and advisory. No AD-32 context or first-response authority is implied.

**Never:** Read rich Conversation detail, transcripts, file content or paths; disclose names/protected exclusions; fabricate File identity; use legacy reverse-index/journal rebuilds; persist traces, events, repairs or selection. No host migration, new platform, native-head/RTI proof requirement, dependency version update, Git staging/commit/push/branch, or deployment. Preserve existing dirty work and completed strict-owner proof implementation. Existing strict queries and context/confirmation contracts retain their meanings.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Advisory | Authorized committed Conversation/Folder observations, possibly lagging | Deterministic IDs, ranks, scores, safe reasons; no names or selection | No currentness certificate |
| Protected | Foreign/denied/missing Conversation; unknown/denied Folder | Conversation safe indistinguishable denial; Folder cannot score and behaves like no match | No existence leak |
| Order/archive | Duplicate/permuted inputs; archived candidate | Identical ordering/reasons; archived only with opt-in and authorization | Preserve engine rules |
| Bounded | Over32 raw attachments; over200 distinct candidates; slow dependencies | Reject raw oversize before dedupe; bounded deterministic scan, four concurrent calls maximum, one linked2s dependency deadline | Truncation/deadline => Incomplete; caller cancellation propagates |
| Degraded | Missing view, malformed owner metadata, unavailable owner, unsupported File | Authorized completed observations only; explicit Incomplete | Empty incomplete never exhaustive NoMatch |
| Revoked | Permission removed between read and disclosure | Remove denied candidate/input and recompute or safe-deny | Never score rejected attachment IDs |
| Effects | Either supported query and independently confirmed association | Persisted store bytes unchanged by query; confirmation reauthorizes | Suggestions grant no authority |

</frozen-after-approval>

## Code Map

Paths below are repository-relative. Inspect owning repository guidance/build configuration before edits; never load skills under `references/`.

- `src/Hexalith.Projects/Resolution/` — reuse engine, rules and both evidence mappers unchanged. Attachment mapper ignores input ReferenceState: filter denied inputs BEFORE mapping.
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs`, `ProjectReadQueryExecutor.cs` and `ProjectAuthorizationGate` — reuse verified caller and scope/read authorization, not full native-index/context assembly. Project ACL alone is insufficient: authorize actual candidate Folder owner too.
- `src/Hexalith.Projects.Server/Projections/ProjectReads/` — `ProjectReadIndex`, `ProjectReadModel`, `ProjectReadModelKeys` through SDK `IReadModelStore`. Bound detail fanout to200 IDs in ordinal order (authorized Conversation linked hint may take one slot). Index is monolithic: claim no byte bound.
- `DelegatedProjectFolderDirectory`, `DelegatedOwnerQueryClient` — reuse signed delegation and existing `FolderOwnerReadQuery` Allowed outcome; optional native evidence is not needed. No Folders implementation changes expected. File mapping provider does not exist in production.
- `references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/` — isolated `ConversationResolutionMetadataReadModel`, metadata key, SDK `GetCompactedAsync`. Refuse pending/malformed/wrong Tenant/resource/schema. Safe label requires stored purpose/classification; omit unclassified label and mark incomplete. Ordinary `IConversationTenantAccessService.CheckAccessAsync(Read,...)` before metadata and before disclosure; strict `ConversationResolutionMetadataAuthorization` uses native proof, so leave it unchanged.
- `ConversationQueryServiceCollectionExtensions`, `ConversationsJsonContext` — register explicit new advisory metadata query/result/handler with verified SDK caller. Keep strict metadata query unchanged.
- `ProjectsServerServiceCollectionExtensions`, `ProjectsServerModule` — register two handlers on existing `/query` and advertise exact query names. New DTOs, one type/file, separate from old multi-type `ProjectResolution`; omit DisplayName/exclusions. Nullable result permits empty+incomplete.
- `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs`, `GatewayFixtureLoopbackHandler.cs` — authenticated JWT→gateway→Projects→signed owner integration seam; retain actual owner handler/authorization and storage, not all-mocked composition.

## Tasks & Acceptance

- [x] Planning/story/traceability scope reset and narrow production-authority scheduling exception; guard suite29 passed. Other release gates remain closed.
- [x] `references/Hexalith.Conversations/src/` and corresponding Server tests — add narrow advisory owner query using committed isolated metadata and two ordinary authorization checks.
- [x] `src/Hexalith.Projects.Contracts/Queries/` and `src/Hexalith.Projects.Server/Queries/` — two queries, advisory response, shared bounded composition and registrations. Use request-local calls/cancellation; no retries or restarted deadline. Fresh final authorization must not reuse initial cached permission. Filter then score. Observed Conversation link suggests, never establishes confirmed membership authority.
- [x] `tests/Hexalith.Projects.Server.Tests/Queries/` and owner tests — cover every matrix row, including actual authenticated query/delegated owner/store integration and byte-equal persisted state. Local source project references are allowed if integration needs actual owner handler.
- [x] Evidence under `_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/` — exact commands, logs, XML and matrix-to-executed-tests mapping; name controlled authority/storage fixtures, File gap and deployment limitations. Do not archive credentials or runtime private logs.

**Acceptance:** Given authenticated eligible callers, when either query runs, then useful authorized advisory suggestions satisfy every matrix row. Given File attachments, when evaluated, then incomplete unsupported coverage is explicit and no full File acceptance is claimed. Given unchanged context/confirmation routes, when suggestions are consumed, then their independent authorization boundary remains intact.

### Review Findings

Code review 2026-10-21, second pass. Reviewers: Blind Hunter (BH), Edge Case Hunter (EC), Verification Gap (VG) and Acceptance Auditor (AA); all four reported. Scope: Projects `4a4640b..f7c9951` (`src`, `tests`, `tools`, `docs/supported-project-reads.md`) plus the Conversations advisory files, since committed locally as `d390692`. Of 56 findings, 30 survived and were grouped into 20 entries; 26 were rejected. The five decisions were resolved on 2026-10-21 with the recommended options and became patches.

- [x] [Review][Patch] Harden the legacy resolution REST endpoints in place (decision 2026-10-21: option 2) — check each candidate with `ProjectAuthorizationGate.AuthorizeReadAsync` and drop denied ones; if any check is retryable, return the existing read-model-unavailable response instead of NoMatch. In `/from-attachments`, check each presented Folder with `IProjectFolderDirectory.RefreshFolderReferenceAsync`, score only Accepted Folders, and stop scoring File IDs. Reuse the 32-input, four-concurrent-call and two-second bounds. No contract, client or OpenAPI change; retiring the endpoints stays a follow-up for when the CLI and UI move to `/query`. [src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs:32, src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs:58]
- [x] [Review][Patch] Return Unavailable, not safe-denial, for an existing Conversation without metadata (decision 2026-10-21: option 2) — the metadata model was added in `beacc09` and is in no release, so every pre-existing Conversation currently gets a whole-request 404. When the metadata entry is missing after the first Read check, look the ID up in the tenant summary index (`TenantIndexKey`, `Summaries`): present gives `Unavailable()`, absent keeps `safe-denial`. Never read `ConversationKey`, which holds rich detail. Add present and absent tests. [references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/GetConversationAdvisoryMetadataDomainQueryHandler.cs:52]
- [x] [Review][Patch] Lag is not incompleteness (decision 2026-10-21: option 1) — remove the Stale `omitted()` call, matching the Conversation owner and the spec's "possibly lagging" Advisory row, and extend `LagCanSuggestWhileUnusableProjectionStatesCannot` to assert `Incomplete == false` for Stale. [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:216]
- [x] [Review][Patch] Log swallowed executor exceptions (decision 2026-10-21: option 1) — add an optional trailing `ILogger<ProjectAdvisoryQueryExecutor>? logger = null` (the repo idiom, so existing constructions compile unchanged) and log one Warning per catch-all with only the exception type and correlation ID. No tenant, Project, Conversation or Folder IDs, labels or exception messages; no metrics. [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:204]
- [x] [Review][Patch] Advisory admission bypasses the 6.1 supported-read admission (BH+EC) — `ProjectReadQueryExecutor.AuthorizeScopeAsync` enforces checks that `Bound()` skips:
  - `Bound()` uses `MatchesPresented`, which admits omitted Scopes and Audience;
  - it falls back to `hexalith-eventstore` when `ExpectedAudience` is unset;
  - it skips the role gate (TenantOperator, TenantProjectAdministrator, or delegated-chatbot `ProjectUser`);
  - it skips the Fresh tenant + watermark check.

  Reuse that admission (`projects.read`) for both the initial and final scope checks, and add negative tests for a missing scope, audience and role. [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:220]
- [x] [Review][Patch] Evidence preservation revives the strict non-native local-health path (BH+AA) — allowed `ConversationTenantAccessService` decisions now carry `ProjectionVersion`/`ProjectionWatermark` (`ConversationTenantAccessService.cs:77`). As a result, the non-native branch of `ConversationResolutionMetadataAuthorization` (:105), which always failed closed before, can now authorize the strict handler from local health alone whenever native evidence is not registered. That contradicts its "local health is insufficient" contract. Make that branch fail closed explicitly and add a regression test. [references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/ConversationResolutionMetadataAuthorization.cs:105]
- [x] [Review][Patch] Fan-out spends the two-second budget on calls that cannot score (BH+EC) — every request authorizes, reads and Folder-checks up to 200 Projects, twice, even when nothing can match:
  - attachments that are empty, file-only or all denied;
  - Conversation metadata with neither ProjectId nor SafeLabel.

  In attachment mode, a candidate whose committed Folder is not an accepted input can never score, because `AttachmentResolutionEvidenceMapper` skips candidates with no signal; it still gets a Folder-owner call. When the deadline fires, every candidate is dropped.

  Fix: short-circuit the no-signal cases, still running the final Conversation and scope rechecks before returning NoMatch, and skip the owner calls for attachment candidates that cannot match. Tests that drive fan-out with `ResolveProjectFromAttachmentsQuery([])` must switch to a matching Folder input. [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:117]
- [x] [Review][Patch] No always-run test covers the advisory `/query` wiring (VG+BH) — only the gated gateway lane exercises:
  - handler registration;
  - routing attachment queries to `FromAttachmentsAsync`;
  - the status mapping (InvalidEnvelope→400, other failures→404);
  - the fallback catalog entries.

  Add advisory envelopes to `SupportedProjectReadHttpTests` (200 file-only, 400 for 33 attachments, 404 safe-denial), and add `ShouldContain` checks to `ProjectsServerModuleTests`. [src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:278]
- [x] [Review][Patch] How the first Conversation read classifies failures is untested (VG) — add first-read cases: `safe-denial` must produce a denial, and `ActorException` must produce incomplete with a null result. [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:275]
- [x] [Review][Patch] A degraded index (`UnavailableProjectIds`) is untested (VG) — seed an unavailable Project and assert the result is incomplete and not an exhaustive NoMatch. [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:125]
- [x] [Review][Patch] A retryable per-Project authorization result is untested (VG) — use `UnavailableProject` and assert incomplete with a null result. [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:237]
- [x] [Review][Patch] Tenant scope ordering, late revocation and unavailability are untested (VG) — add three cases:
  - removed membership gives safe-denial with zero owner and Folder reads;
  - revocation before disclosure gives a denial;
  - a stale or unavailable tenant projection gives incomplete.

  [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:228]
- [x] [Review][Patch] Envelope and input validation is untested beyond the 33-attachment cap (VG) — add a Theory covering an unknown `referenceKind`, unknown or duplicate fields, a non-`projects` aggregate, Paging, IsGlobalAdmin, and blank or untrimmed IDs. [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:83]
- [x] [Review][Patch] The owner's freshness-state gate is untested (VG) — add Rebuilding, Unavailable and Forbidden cases to `MalformedOrPendingMetadataWithholdsObservations`. [references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/GetConversationAdvisoryMetadataDomainQueryHandler.cs:57]
- [x] [Review][Patch] A retryable owner denial is untested (VG) — it should yield Unavailable. Add a retryable denial to `ResolutionMetadataAccess` and assert null metadata and incomplete. [references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/GetConversationAdvisoryMetadataDomainQueryHandler.cs:85]
- [x] [Review][Patch] The `IsAdvisoryHeaderValid` lifecycle check is untested (VG) — add an undefined-lifecycle case. [src/Hexalith.Projects/Aggregates/Project/ProjectPersistedDetailValidator.cs:89]
- [x] [Review][Patch] The concurrency test asserts exactly four overlapping reads (BH) — that depends on timing under load; the bound under test is `<= 4`. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:163]
- [x] [Review][Patch] The advisory consumer docs omit the admission and limits contract (BH) — document:
  - the required scope, audience and roles (after the admission patch);
  - the 16 KiB payload limit and the 400 conditions;
  - ordinal-first selection of the 200 Projects;
  - the Conversation outcomes: 404 for a missing or denied Conversation, and an incomplete result for an existing Conversation whose metadata is not projected yet.

  [docs/supported-project-reads.md:75]
- [x] [Review][Patch] Release Conversations 1.3.0 and raise the pin (decision 2026-10-21: option 1, an approved exception to "no dependency version update") — run last, after every other patch including the Conversations-side ones, so Conversations is released once. Sync Conversations `main` (2 ahead, 9 behind origin), push, wait for green CI, dispatch `release.yml` with `version=1.3.0`, then set `HexalithConversationsVersion` to 1.3.0 in `references/Hexalith.Builds/Props/Directory.Packages.props` and move the root submodule pointers. Push and release each need an explicit go-ahead at that time; the story stays `in-progress` until the pin lands. Note: `main` CI is currently red on the unrelated `test_external_dependency_may_equal_candidate_version` workflow-policy test, which skips the build job and hides this break. [src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj:35]
- [x] [Review][Defer] The delegated Conversation owner hop is verified only by the gated source lane (VG+BH) [src/Hexalith.Projects.Server/Queries/DelegatedProjectAdvisoryConversationDirectory.cs:9] — deferred (medium): `SupportedProjectGatewayHttpTests` is wrapped in `#if HEXALITH_SOURCE_FIXTURE` and requires the `STORY61_DAPR_*` variables; CI sets neither, and there is no unit harness for `DelegatedOwnerQueryClient`. Running that lane routinely belongs to the CI/G-4 lane work.

#### Rejected

- BH-3 false — the final ordinary Read check governs disclosure, and advisory results accept lag. The strict `IsVerifiedScoringQuery` pins the strict query type, so it cannot be reused.
- BH-4 / AA-8 low — the owner's hard-coded `projects` workload and its tolerance of omitted scopes follow the strict sibling's `Compatible(...)` pattern. The Projects-side admission patch closes the empty-scope path.
- BH-6 false — `Bound()` requires `TryBind` against the SDK-verified caller, so the envelope tenant is authoritative. The engine logger and `PresentedInputIds` only affect logging.
- BH-7 / AA-7 / EC-11 low — the delegated Folder path ignores `projectId`. The direct fallback needs an unverified caller, which `Bound()` excludes. A Folder-only seam would be new surface.
- BH-10 low — truncation must set Incomplete, and the ordinal 200-ID bound is required by the spec. The approved proposal defers a Folder→Project index.
- BH-11 / EC-10 low — unknown nested fields are ignored and never read (see prior triage item Blind6).
- BH-12 (magic-strings part) false — the wire values match the `AttachmentResolutionEvidenceMapper` constants, and no harm was identified. The endpoint-collision part is kept as a decision above.
- BH-13 (Folders metadata endpoint and leftovers) false — Folders source no longer has an `operational-index-metadata` route, and the one-element loop and indentation cause no harm. The gated-coverage part is kept under the wiring patch and the defer.
- BH-15 / EC-7 low — `ScopeAsync` makes the same HttpContext reads sequentially before any fan-out, and the parallel workers only read.
- BH-16 (ObservedHealth, changelog, diff headers) false — the health provider is created per invocation; semantic-release owns changelogs; the header mismatch comes from how the review diff was assembled. The strict-path part is kept as a patch.
- BH-17 (CRLF) false — `.editorconfig` sets `[*] end_of_line = crlf`, so the conversion conforms. The documentation gaps are kept as a patch.
- BH-18 false — letting 6.4 progress is the approved exception. The guard tests assert that 6.1 stays blocked and readiness stays NOT_READY.
- AA-3 false — the advisory response carries no capability that any confirmation route accepts. Existing confirmation reauthorization tests cover the Effects row.
- AA-4 / EC-3 false — the spec defines SingleCandidate as one visible, unselected suggestion. Only empty incomplete results are null.
- AA-5 false — the real Folders owner returns `safe-denial` both for unknown folders and for non-retryable denials (`FolderOwnerReadQueryHandler.cs:21,29,43`); both map to Denied, i.e. no match.
- AA-9 false — CRLF is the repository baseline, and the README records the per-command whitespace check.
- VG-O2 false — the removed Folders metadata route no longer exists, and the test enrolls queries from the registered handlers, as the README documents.
- EC-4 low — the only non-retryable denial outside the ACL is the validator's `Malformed` result; Unavailable and tenant Stale/Unavailable are retryable. Integrity faults are rare, and the fix would add a branch.
- EC-5 / EC-17 false — `Bound()` guarantees the owner client's preconditions. Gateway 401/403 responses map to ActorException, which gives incomplete; only an actual owner 404 is a denial.
- EC-6 low — production dependencies honor cancellation, and any orphaned work is read-only and its results are discarded.
- EC-9 maybe-false (would be low) — this depends on the Folders File-ID grammar; Files are unsupported either way.
- EC-12 false — the spec reuses the existing mappers unchanged, and Conversation lifecycle is not a scoring input.
- EC-15 low — checking scope before validation is deliberate, so it cannot become a validation oracle. It only matters during outages and discloses nothing.

## Current Review Patch Status

The second-pass local review patches are implemented and verified in the [current review evidence](evidence/6-4-advisory-resolution-20261021/review-2/README.md). Supported admission is shared with the existing reads; legacy endpoints authorize each Project and accepted Folder, suppress unmapped File scoring, and share the request bounds. Lag alone is usable. Empty-signal requests skip Project fan-out while retaining final owner/scope checks. The Conversation summary-index fallback distinguishes existing unprojected metadata from absent inputs without loading rich detail. The strict non-native authorization branch explicitly withholds permission evidence. Warning logs expose only exception type and correlation ID; expanded regressions cover all requested negative controls and HTTP/catalog wiring.

Conversations `main` is already synchronized at `20e67c96427652d35abf53fd8299825f191cf02a`; the older ahead/behind and CI note in the release checklist is historical. At that local-patch checkpoint, the remaining work was Conversations publication and its consumer pin. Both are now verified as completed in the publication and consumer-pin sections below. Story status remains `in-progress` pending the workflow review.

## Publication CI Repairs

The user approved committing and pushing the Conversations fixes on 2026-10-22. The verified files were committed and rebased unchanged onto remote `6c3cb6d574364eb8c971bf8c92c5c0df967c5246`, then pushed as `591ee3b3fa09cee761f6d56b7f82fa0d16cd04ed`. Exact-source CI run `39739114750` exposed five portable-conformance failures: three stale current contract-shape checks (219 recorded versus 225 live types), a retained projection proof naming a renamed cross-Tenant test, and an in-module gateway fake. Repair this publication readiness within the already-approved query scope; publication still requires its separate explicit go-ahead. No frozen evidence, historical results, assertion inventories or release gates may be rewritten or weakened.

- [x] Inspect the member-level diff against `references/Hexalith.Conversations/docs/release-evidence/public-contract-shape-story-9.2-v2.json`. For the six additive query DTOs and four optional ResolutionScoringLabel command/event properties already authorized by the recorded Story 6.4/owner scope, produce a reviewed additive current successor and point the live checks in `PublicContractShapeSnapshotGenerationTest.cs` and `ReleaseBaselineValidationTest.cs` to it. Preserve both prior snapshot files byte-for-byte; prove all retained types/members unchanged except those four approved additive properties; preserve positional constructors and existing deconstruction signatures. Record the actual scope approval source, without fabricating a new owner/Quality decision. If the diff contains an unapproved change or a genuinely separate approval is mandatory, prepare the complete candidate diff and report that exact decision boundary.
- [x] Ensure all six new query DTOs have samples in `references/Hexalith.Conversations/tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` and run the full Contracts suite: CI also found `ContractSerializationTest.EveryPublicContractRecordShouldHaveSerializationFixtureCoverage` missing a fixture for `ConversationAdvisoryMetadataResult`. Preserve the exhaustive fixture guard and exercise round-trip serialization of all six additions.
- [x] Reconcile `ProjectionReadStorePopulationProofValidationTest.cs` with `ConversationAsyncProjectionHandlerTest.CrossTenantEventShouldFailWithOnlyAnAcceptedRoutePositionBarrier`: the current behavior rejects the cross-Tenant event and permits only one accepted-route position barrier, with no Conversation views written. Preserve the old zero-write proof and machine-result bytes as historical evidence. Bind truthful current execution and its actual successor test without pretending the old zero-write assertion passed at HEAD, adding no-op aliases, or excluding the guard.
- [x] Replace `ResolutionMetadata/ResolutionProjectionGateway.cs`'s in-module `IEventStoreGatewayClient` implementation with the existing SDK Testing seam, updating its fixture/recovery consumers. Do not disguise a duplicate, add a new platform, update packages, or alter production semantics.
- [x] Build affected test projects individually in Debug using the recorded explicit sibling source roots; run the five failed checks, all current portable/internal conformance checks with the existing CI historical exclusions, and the full Conversations.Server suite. Retain exact commands, source hashes, passing XML and honest superseded failures under the publication handoff evidence. Pin the new byte-hashed cross-Tenant XML receipt to LF through an exact-path .gitattributes rule, following existing evidence conventions, so Windows checkouts preserve its hash. The parent retains commit/push and release coordination; implement sequentially without further delegation.

The five publication CI repair items passed 2,190 distinct final tests, zero failures/skips, in the [separate repair evidence](evidence/6-4-advisory-resolution-20261021/publication-handoff/ci-repairs-20261022/README.md). Parent independently audited the XML test IDs, the full additive contract delta, frozen-byte preservation and all 14 source hashes. The repair was committed with pinned commit validation, rebased byte-identically over the unrelated Story 12.1 documentation, and pushed as `17dfc440c9d4e208879f3c121cb97ffe3460a2c7`. Exact-source CI run `39744196218` is pending. Publication and consumer pin remain unexecuted.

## Repository Tooling CI Repair

Exact-source CI run `39744196218` passes current conformance, build/unit/integration, Aspire, admin Web, release tooling and Windows cleanup. The repository lane independently reports 2,218 passing tests and one failure: `test_python_tooling_lane_has_no_ambient_skip_constructs` detects `_bmad/scripts/tests/test_authoring_outputs.py` line 261, whose POSIX FIFO regression uses a prohibited `pytest.mark.skipif`. The unrelated fixture compatibility job hit a generated .deps.json sharing violation and its accepted retry remains separate; do not change that workflow or its gates for a single transient lock.

- [x] In `references/Hexalith.Conversations/_bmad/scripts/tests/test_authoring_outputs.py`, remove only the ambient skip decorator from `test_review_reuse_rejects_a_fifo_without_opening_it` and require FIFO support explicitly before creating the actual FIFO, following the existing fail-closed POSIX tooling fixtures. Preserve the real named-pipe timeout, blocked-result and no-input/no-output-mutation assertions; do not weaken the anti-skip guard or add exclusions, substitute a regular file, expand platform scope, or alter production code. Reproduce the static guard failure, then run the full authoring-output test file plus the static anti-skip test with locked tooling and retain exact commands, passing JUnit results, file hashes and the CI failure summary in a distinct follow-up evidence directory. Parent retains commit/push/CI/release coordination.

The [focused tooling repair](evidence/6-4-advisory-resolution-20261021/publication-handoff/repository-tooling-repair-20261022/README.md) passed 77 tests with zero skips. Parent audited the real FIFO and static-guard cases, all six source/config hashes and the exact one-file diff. Pinned commit validation and hooks passed. Pushed as `feddda1f6a26aef1ebdffb92c9cd81037eeac300`. Upstream planning/gitlink updates cancelled its run after the Contracts/Client/Domain/Server suites passed. Parent fast-forwarded nonrecursively to `71fab44b7dd664699cedbbfc17ed0d7822e8bdb9` and confirmed all 15 reviewed files remain byte-identical. Current exact-source CI is `39759753386`; the separate release go-ahead remains pending.

## Green CI and Observed Publication

Current candidate `71fab44b7dd664699cedbbfc17ed0d7822e8bdb9` passed exact-source CI run `39759753386`, CodeQL and commit policy. All required CI jobs passed; Performance remains skipped by the existing workflow policy. The final read-only source verifier proved live main, latest successful exact-source push CI and unused 1.3.0 destinations; the semantic dry-run independently confirmed exactly 1.3.0. The frozen scope remains byte-identical to Projects baseline `06a232629b38e955a0fa3927cc04fcd344504875` (SHA-256 `78e8a29557f949b064db4111f8d487ff4c4d15f3f7d57284e72905f797b9a7df`).

An external `workflow_dispatch` by GitHub actor `jpiquot` started release run `39764388610` for exactly version 1.3.0 at that same source. The release and official package/symbol/tag/source verification jobs passed. Independent local `verify-release-state.py present 1.3.0 --source-sha 71fab44b7dd664699cedbbfc17ed0d7822e8bdb9` also passed. This session did not dispatch publication or duplicate that action, and claims no new chat release approval. Evidence: [verified publication](evidence/6-4-advisory-resolution-20261021/publication-handoff/verified-publication.json).

## Consumer Pin Landed

After publication verification, Builds was fast-forwarded nonrecursively from `359fee24aeca4d9eabed4b425d4cfa2520b2adda` to `5e54e892e69c31f346d69cb79f19f04bce8b79cc`; this includes existing upstream EventStore 3.126.1 and Tenants 5.10.0 selections. The only new configuration edit is `HexalithConversationsVersion` 1.2.0 to 1.3.0 in `references/Hexalith.Builds/Props/Directory.Packages.props`. XML comparison proves no other semantic edit; BOM/CRLF remain intact. Projects.Server built in Release using packages, with zero warnings/errors. Its effective assets select Conversations Client/Contracts 1.3.0 and EventStore Client/Contracts 3.126.1 as packages. Pinned Builds commitlint and raw whitespace checks pass.

The user explicitly replied “approve” to the Builds commit/push request. While that approval was pending, the identical one-line pin landed externally as `0d8b910e0ee4df31198838f35242d0e6be9c80c4`. Its working-file SHA-256 matches the reviewed candidate exactly; it is an ancestor of fetched Builds main `2d653b611c8efcffb4d27bd8715d5dbf24eaebfd`. Projects HEAD `a4f617d94807a678e8f14ae8434f61681eb2341c` already records that Builds commit and Conversations `71fab44b7dd664699cedbbfc17ed0d7822e8bdb9`. Both submodule trees are clean. This session created no duplicate commit or push. The release/pin task is complete. See the [one-line pin](evidence/6-4-advisory-resolution-20261021/publication-handoff/consumer-pin.patch), [build readiness](evidence/6-4-advisory-resolution-20261021/publication-handoff/consumer-pin-readiness.json) and [landed-pin receipt](evidence/6-4-advisory-resolution-20261021/publication-handoff/consumer-pin-landed.json).

## Implementation Notes

Completed bounded Conversation/Folder advisory source implementation. Final authorization rereads retained Project Folder bindings and lifecycle before disclosure. Parent audited the passing XML: Projects977, Conversations1129, engine107; planning guard29 (2242 unique passing tests, zero failures/skips). The focused matrix39/17, owner-access98, strict-owner55 and gateway1 are subsets. Evidence and explicit File/deployment limits: [verification](evidence/6-4-advisory-resolution-20261021/README.md). Original dirty Conversations/EventStore bytes are preserved.

The parent completed required Aspire start/describe/stop baseline before changes. Projects/security/workers were healthy; EventStore app exited, so this is not deployed acceptance. Initial dirty-byte snapshots and execution baseline are in `/tmp/story64-advisory-20261021/baseline/` and `baseline.json`. Implement sequentially; do not delegate further. If source details differ, adapt within approved boundaries and document it, without expanding prerequisites.

## Spec Change Log

2026-10-21: User explicitly renegotiated the former frozen strict-current intent by approving the linked scope-reset proposal. Prior exact-current prerequisites are historical for this advisory scope; authorization, real identity and prior completed proof work remain.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind1: ReadOnlyCandidate capability | medium | The retained PRD text incorrectly assigns confirmation capability to the new suggestion contract. No consumer accepts this DTO as permission; approved output is IDs/scores/reasons and confirmation independently authorizes. Clarify PRD/consumer guidance that the confirmation surface obtains manage capability and suppresses accept for readers. Do not add a capability protocol to advisory matching. |
| Blind2: owner health absent | high | DefaultConversationTenantProjectionSignal returns Healthy with no version/watermark for a nonsignal store. AllowedAsync currently accepts that missing evidence. Require usable existing decision version/watermark and test default registration; no native-head equality or new health service. Patch. |
| Blind3: explicitly erased metadata | high | Optional NativeSourceProof.Erased is ignored in the advisory read even though the committed model can carry it. Reject known erasure while permitting absent native proofs. Patch and regression. |
| Blind4: malformed/unconfirmed Folder binding | high | DetailAsync validates lifecycle/sequence but ignores the binding ReferenceState and scoring-name validity. Pending/Conflict/undefined states can be promoted to Included. Validate the actual scoring fields and committed binding with existing validators; no context Setup gate. Patch. |
| Blind5: one final read fault loses all | medium | The final Parallel loop exception reaches the outer empty-incomplete response, dropping independently checked candidates. Mirror per-candidate fault handling from discovery. Patch with surviving-candidate test. |
| Blind6: nested duplicate/unknown JSON fields | low | Deserialization uses one canonical identity for both authorization and matching; the signed envelope binds exact original bytes. No authorization bypass or data disclosure occurs. Closed nested-schema rejection is extra validation rather than the approved behavioral promise; reject added branches for this cosmetic parser preference. |
| Blind7: unbounded local selection allocation | medium | All index IDs are copied, deduplicated and sorted before the200-ID fanout cap, with no cancellation checks in that local loop. Keep only the bounded ordinal selection and observe cancellation while scanning the existing monolithic view. Patch, retaining the explicit index-byte limitation. |
| Blind8: integration cannot detect every new key | low | The real-store case verifies specified persisted values; guarded unit stores reject writes and compare key count, and both new query call graphs contain no persistence operations. A complete namespace recorder would add a new fixture mechanism without demonstrating a missing writer in this change. Existing complementary evidence satisfies scoped zero-write acceptance; reject broader harness work. |
| Blind9: no new captured-log sentinel suite | low | New executor/owner handler expose only typed IDs/enums/numbers and have no logger; raw failures are caught without logging. Engine uses NullLogger and its existing leakage regressions passed. Authenticated host logs contain types/status, not query bodies. No introduced leak path was identified. Reject a new logging harness; retain source and existing leakage evidence. |
| Blind10: originating audience owner check | false | Projects Bound verifies the authenticated original audience against deployment configuration before invoking DelegatedOwnerQueryClient. SDK/gateway signatures bind that original envelope and owner target; the caller cannot change its audience after this check. A wrong-audience original request cannot reach this owner call through either new handler. Strict owner's hardcoded audience is not the new deployed audience policy. |
| Edges1: reader/manage marker | medium | Same documentation mismatch as Blind1; confirmation capability is independently evaluated at its owning surface. Apply the same PRD/consumer clarification. |
| Verification1: successful final metadata changes | medium | Existing tests only vary final owner denial; a successful withdrawal of link/label lacks a regression. Add a test proving final authorized observations replace initial scoring evidence and mark incompleteness. Patch. |
| Verification2: fence changes during read | medium | Existing pending-fence test rejects at the first check, leaving the second check unexercised. Use existing AfterRead hook to establish a new fence during metadata read and assert withheld metadata. Patch. |

### Final workflow review — 2026-10-22

All three independent layers reported before triage. Edge Case Hunter returned no findings. Every Blind Hunter finding and the verified regression gap below has its own verdict before grouping.

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Final BH1: local MCP transport setup | false | These files are preserved user changes, not Story 6.4 implementation. The configured loopback service is listening on port 8812. Different client configuration schemas and absence of a new startup script do not establish the claimed failure. Leave user configuration untouched. |
| Final BH2: obsolete default G-6 policy | medium | The runner and runbook still select the October 3 tuple while CI explicitly selects October 20; the root SDK is 10.0.402. The same default exists at the original baseline and is independent of advisory resolution. Defer this pre-existing qualification-tooling issue. |
| Final BH3: legacy final authorization | high | The new helper checks Project access once; attachments then await Folder-owner checks, and neither REST adapter rechecks scope before disclosure. A revocation in that interval can survive. Patch by repeating the existing exact Project and scope checks before scoring/disclosure, preserving safe denial, retryable failure and the single deadline; add late-revocation controls. |
| Final BH4: legacy Conversation candidate Folder | high | AuthorizeReadAsync checks Project authority and loads detail but does not authorize its Folder owner. The Conversation adapter discards that binding and keeps a candidate even when its actual Folder is denied. Patch using the existing decision detail and Folder directory, with denied/unavailable owner controls. No new wire contract. |
| Final BH5: malformed index receipt | medium | Advisory enumeration checks ETag, tenant and collections but accepts zero/negative GlobalPosition and default LastVerifiedAt. An empty malformed observation can claim complete NoMatch. Patch these existing receipt checks and add malformed-index controls while retaining lag support. |
| Final BH6: cancellation on early owner returns | medium | A synchronously completed owner task can cancel the caller token yet complete normally; WaitAsync can then return the completed task and missing-metadata branches return before the final cancellation check. Patch cancellation propagation at those returns and test both owner passes. |
| Final BH7: Conversation HTTP dispatch control | medium | Its always-run HTTP case deliberately fails principal binding and expects 404, which cannot distinguish a missing handler. Patch with authorized known-output and malformed-payload requests through the existing HTTP fixture. |
| Final BH8: payload byte-limit control | medium | The oversize input is invalid JSON, so removing the byte guard still fails parsing. Patch the test with valid 16385-byte JSON and a valid 16384-byte boundary request. |
| Final BH9: advisory wire shape controls | medium | The five new DTOs have no exact public JSON assertions; round trips using the same types cannot catch property drift. Patch contract tests for literal property sets, enum spelling, nullable result, optional inputs and protected-field absence. |
| Final BH10: legacy unsupported Files documentation | medium | The retained endpoint now suppresses Files but its OpenAPI description still promises FileReferenceMatched and does not explain its legacy NoMatch limitation. Patch the endpoint description/generated documentation and consumer guidance without changing response schemas or engine rules. |
| Final VG1: read-only admission | medium | Pre-verified regression gap: every positive fixture grants both read and list scopes/actions, so selecting list admission would escape the tests. Patch a theory for both advisory query kinds that verifies candidate output with read-only authority and denial with list-only authority. |

## Review Resolution

The final three-layer workflow review is complete. All nine final patch groups are resolved: legacy final authorization and actual Folder checks, malformed index receipts, owner-completion cancellation, authorized Conversation HTTP routing, independent byte-limit controls, literal DTO wire assertions, read-only admission and legacy File documentation. The parent inspected the final code/generated diffs and independently verified all 70 source/config/evidence hashes. Final suites passed 2,609 unique tests with zero failures/skips; all affected Debug builds and the package-mode Release build passed with zero warnings/errors. All seven frozen matrix rows, 116 earlier references and 33 final-correction references map to current passing XML. The owned Dapr/Redis fixture was cleaned up successfully. Evidence: [final verification](evidence/6-4-advisory-resolution-20261021/review-3/README.md). The one pre-existing G-6 default-policy issue is recorded in the deferred-work ledger. No unresolved implementation finding remains in this advisory scope.

The earlier review resolution below is retained as history.

All accepted findings are resolved. The seven bounded patches passed their regressions and full suites. PRD/consumer guidance now assigns manage capability to independent confirmation. The real gateway exposed dropped permission evidence in the existing Conversation facade; it now carries the exact evaluated observation per invocation, with an overlapping-request regression. The older strict test now verifies this preservation while retaining default-missing-evidence and native-proof requirements. No strict handler checks changed. Parent independently inspected the final diffs and audited every matrix reference against passing XML. Historical failing runs remain in the evidence directory. No unresolved implementation finding remains in the approved Conversation/Folder advisory scope; unsupported Files and deployment acceptance remain explicit limitations.

## Workflow Completion

Build workflow complete on 2026-10-22; the sprint story is ready for review. The approved frozen scope excludes root Git staging/commits, and the later explicit go-aheads covered Conversations and the Builds pin. That specific scope constraint takes precedence over the workflow's generic local-commit step. Root changes remain uncommitted; no root push was performed. The externally landed publication and consumer pin are independently verified and are not duplicated by this session.

## Verification

Build individual affected test projects Debug with `-p:UseHexalithProjectReferences=true`. Run built xUnit assemblies with `-class`/`-method` and `-result-xml`, not solution tests. Run focused new matrix tests, existing Resolution engine tests, then full affected Projects.Server and Conversations.Server suites. Record exact results and genuine blockers; missing G-4 runner/deployment acceptance is independent. Parent independently audits diff and matrix before review. Run `python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py`, production-authority validator and `git diff --check` (including owning submodule).
