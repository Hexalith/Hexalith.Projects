---
title: 'Story 6.4: Authorized advisory Project suggestions'
type: 'feature'
created: '2026-10-17'
updated: '2026-10-27'
status: 'in-progress'
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
- `src/Hexalith.Projects.Server/Queries/ProjectQueryEnvelopePrincipalBinding.cs`, `ProjectReadQueryExecutor.cs` and `ProjectAuthorizationGate` — reuse verified caller and scope/read authorization, not full native-index/context assembly. Project ACL alone is insufficient: authorize actual candidate Folder owner too, when a Folder is bound.
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

### Review Findings — fourth pass, chunk 1 of 3 (2026-10-22)

Code review 2026-10-22, fourth pass. Reviewers: Blind Hunter (BH), Edge Case Hunter (EC), Verification Gap (VG) and Acceptance Auditor (AA); all four reported. Chunk 1 scope: Projects `83781b3..d30b955`, limited to `src/` (excluding `Hexalith.Projects.AppHost`), `docs/supported-project-reads.md`, `tools/planning/` and `tests/tools/test_production_authority_guard.py`. Chunk 2 (Projects tests) and chunk 3 (Conversations advisory slice `1f38912..71fab44`) remain. Of 49 findings, 22 survived and were grouped into 12 entries; 27 were rejected.

- [x] [Review][Patch] Projects with no bound Folder rely on their exact Project authorization (decision 2026-10-22: option 3) (BH3+AA2+AA4a+EC3+EC6) — Every new Project starts with `ProjectFolderCreationPending` (null FolderId) until `SetProjectFolder`, and folderless historical Projects exist. Today `IsAdvisoryHeaderValid` requires an Included Folder and `DetailAsync` calls `omitted()`. As a result:
  - any such Project among the 200 marks every advisory result incomplete, and a linked one is never suggested;
  - the legacy Conversation route silently drops it.

  Fix:
  - A Project whose `ProjectFolder` is null, or Pending with a null FolderId, skips the Folder-owner call and relies on the existing fresh-Tenant, exact Project read authorization and the recheck before disclosure, as 6.1 reads do. Confirmation needs no Folder.
  - A Project that carries a FolderId still requires an Included binding and an owner Accepted result.
  - Attachment mode skips folderless Projects without flagging Incomplete.
  - Apply the same rule in the legacy helper.
  - Clarify the Code Map line to "authorize actual candidate Folder owner too, when a Folder is bound", and document it in `docs/supported-project-reads.md`.
  - Add tests: suggested with zero owner calls (null and Pending), attachment mode not incomplete, legacy SingleCandidate, and a bound Folder still needing the owner.
  - Chunk 2 merge (AA1+BH14+EC10+EC12): invert `ResolveProjectFromConversationTests.MissingCommittedFolderBindingCannotBecomeACandidate`, and cover `SeedAsync(folder: false)` on the supported path.

  [src/Hexalith.Projects/Aggregates/Project/ProjectPersistedDetailValidator.cs:99, src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:226, src/Hexalith.Projects.Server/Queries/LegacyProjectResolutionAuthorization.cs:26]
- [x] [Review][Patch] Conversation-mode fan-out spends the deadline on candidates that cannot score (BH5+AA1+EC8) — In Conversation mode, every authorized and readable Project among the 200 gets a Folder-owner call and is retained. The final pass then re-authorizes it, re-reads it and checks its Folder again. The review-2 skip applies only to attachment mode. At four concurrent calls this is up to about 1,200 dependency calls inside one 2-second deadline. When the deadline fires, the catch-all returns an empty incomplete result, so mid-size tenants get no suggestions. Discarding on deadline is itself correct, because disclosure needs the final rechecks.

  Fix: after `DetailAsync`, skip the Folder-owner call and do not retain a candidate unless `ConversationResolutionEvidenceMapper` gives it a signal under the observed metadata (a linked ID, or a label equal to the name). Changed final metadata already marks the result Incomplete. [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:164]
- [x] [Review][Patch] The legacy Conversation route authorizes every tenant Project twice, with no bound (BH6+VG4+AA3+EC4) — `AuthorizeLegacyResolutionCandidatesAsync` runs over all list rows twice. Each row costs a gate call plus a Folder-owner call, all inside the 2-second deadline, and any deadline or retryable decision returns 503. Before this story the route was a single list read. Large tenants will get 503 on every request, and an unreadable Conversation (no link, no label) still pays for the whole fan-out.

  Fix: map evidence from the rows already loaded, and authorize only the Projects the mapper gives a signal. Output is unchanged, because the mapper already drops no-signal candidates before the engine. A no-signal Conversation then skips the fan-out but keeps the final `AuthorizeListAsync`. [src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs:145]
- [x] [Review][Patch] Archived or failed Folders turn the legacy routes into permanent 503s (EC1) — REST requests carry no `VerifiedQueryCaller`, so `DelegatedProjectFolderDirectory` falls back to `FoldersProjectFolderDirectory`. That directory returns `Archived` for archived, Failed, Inaccessible and Unknown-provider Folders, and both legacy helpers treat every outcome other than `Denied` as unavailable. As a result:
  - one authorized Project with such a Folder makes every legacy Conversation request in its tenant return 503;
  - presenting such a Folder fails the attachment request.

  Fix: treat `Archived` (and `ValidationFailed`) as a definitive exclusion, like `Denied`. This matches the delegated owner, which already denies inactive Folders. [src/Hexalith.Projects.Server/Queries/LegacyProjectResolutionAuthorization.cs:32]
- [x] [Review][Patch] No test covers owner metadata returned with `incomplete: true` (VG1) — Every advisory test double returns metadata with `incomplete: false`. Add a Theory with two cases:
  - metadata with no link or label and `incomplete: true`: assert `Result == null` and `Incomplete == true`;
  - metadata linked to `project-a` and `incomplete: true`: assert `project-a` is still offered and `Incomplete == true`.

  [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:117]
- [x] [Review][Patch] The live e2e specs still expect File inputs to score (VG3) — `projects-resolution.spec.ts` expects a File-only request to return SingleCandidate with `FileReferenceMatched`. It also expects a Folder+File request to return MultipleCandidates that include the File-matched Project. The legacy route now returns NoMatch for File-only requests and never scores Files, so the `liveAppHostTest` lane will fail the next time it runs. Update the expectations to the unsupported-File behavior. [tests/e2e/specs/projects-resolution.spec.ts:48]
- [x] [Review][Patch] The legacy attachment OpenAPI wording implies a coverage signal (BH12+AA7) — "returns NoMatch (200) with unsupported coverage" suggests a field that the legacy response does not have. The `fileId` parameter still reads as a scoring input, and the edited line breaks the YAML wrapping. Reword the text without changing the schema, state that File IDs never score, and regenerate the client. [src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:1597]
- [x] [Review][Patch] The final `AuthorizeListAsync` maps retryable denials differently from the first check (EC5) — The first check returns 503 only for `Retryable && Reason == Unavailable`. The final check returns 503 for any retryable result. The same authority state therefore gives 404 at the start of the request and 503 at the end. Use the first check's mapping in both routes. [src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs:154, src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs:122]
- [x] [Review][Patch] Stale `6-4: blocked` entry in the guard's expected statuses (BH14) — `EPIC6_DEVELOPMENT_STATUSES` still lists Story 6.4 as `blocked`, but `_validate_scheduling` always overrides it. A maintainer reading the table would believe 6.4 is pinned. Remove the entry. [tools/planning/validate_production_authority.py:78]
- [x] [Review][Defer] The delegated Conversation owner hop is verified only by the gated source lane (VG2) [src/Hexalith.Projects.Server/Queries/DelegatedProjectAdvisoryConversationDirectory.cs:13] — deferred: already in the deferred-work ledger since 2026-10-21. Reconfirmed here, not duplicated.
- [x] [Review][Defer] Local owner-client preconditions surface as a Conversation 404 (BH11) [src/Hexalith.Projects.Server/Queries/ProjectAdvisoryQueryExecutor.cs:286] — deferred, maybe-false (medium if true). `ProjectQueryEnvelopePrincipalBinding.TryBind` also admits a JWT principal that has no `VerifiedQueryCaller`. `DelegatedOwnerQueryClient` then returns the same `safe-denial` locally, so every Conversation query would return 404, as if the Conversation were missing. To settle it, confirm whether a direct, non-gateway `/query` composition is a supported deployment.
- [x] [Review][Defer] The legacy Conversation route turns an unavailable Conversation owner into NoMatch (AA4b) [src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs:121] — deferred: this is the retained route's pre-existing, documented fail-closed behavior (degraded reference evidence gives NoMatch 200), and its response has no coverage field. Address it when the legacy routes are retired.

#### Rejected (chunk 1)

- BH1 false — `.editorconfig` sets `[*] end_of_line = crlf`. The quoted sentence "tracked files retain their existing endings" is a 2026-10-17 evidence note, not policy (same as the earlier BH-17).
- BH2 / EC9 low — a retryable admission returns the spec's empty-incomplete shape and discloses nothing. It only happens during outages, and the prior review required this behavior ("a stale or unavailable tenant projection gives incomplete").
- BH4 false — the spec mandates the ordinal 200-ID bound and forbids the legacy reverse index. Truncation is reported as Incomplete and is documented.
- BH7 low — workers only read (the gate writes no `HttpContext.Items`), and late results are discarded. Removing `WaitAsync` would give up deadline enforcement (same as the earlier EC-6).
- BH8 low — only the engine's rare fail-closed warnings are lost, and the fix adds a constructor dependency.
- BH9 / EC11 low — the same deserialized value drives both authorization and matching, so a caller can only confuse its own request (same as the earlier Blind6 and BH-11).
- BH10 / EC7 false — accepting Stale is the 2026-10-21 decision "Lag is not incompleteness". The unchecked detail positions are not scoring inputs.
- BH13 false — the positive test fails if `_validate_scheduling` raises for any of the six statuses. Separate tests cover moving 6.1 and the downstream keys.
- BH15 false — every mapped value is compared exactly to `P0_CAPABILITY_STATUS`. The regex change comes from Story 6.1's `02ce53a`.
- BH16 false — the doc states "Missing or denied Conversations return indistinguishable HTTP 404" and points to the bound-signing configuration section above it.
- BH17 false — `ScopeAsync` already required `TryBind` and an `HttpContext` for the same query, so the unbound branch cannot be reached in the middle of a request.
- BH18 low — the legacy catch-all logs nothing, but logging there needs a new logger dependency on a route slated for retirement.
- BH19 low — an unknown owner lifecycle fails closed to Incomplete.
- BH20 low — the delegated Folder path ignores the Project ID (same as the earlier BH-7).
- AA5 low — this is a narrow revocation race on the retained route. The Revoked row governs the supported queries, which do reread. The fix adds an owner call.
- AA6 / EC10 low — those returns disclose no Project data. The owner's ordinary Read check, which includes tenant access, authorized the existence signal. The fix adds guards.
- AA8 false — names are part of the legacy route's pre-existing contract and go only to callers authorized for each Project. The 2026-10-21 decision kept that contract unchanged.
- AA9 low — the legacy `ProjectResolution` already uses the same `Contracts.Ui` enums, so Story 6.8 must untangle them regardless.
- AA10 false — the P0 guard edits come from Story 6.1's `02ce53a`; they fall inside the review range only because of the baseline.
- AA11 false — the C# tests are in chunk 2.
- EC2 false — the spec requires actual Folder-owner authorization, so an unconfigured or unavailable owner correctly fails closed.
- EC12 low — a deterministic failure mapped to 503 is rare, and classifying exceptions would add branches.
- EC13 low — the raw count matches the raw-before-dedupe bound. Only pathological, duplicate-heavy requests behave differently.

### Review Findings — fourth pass, chunk 2 of 3 (2026-10-22)

Chunk 2 scope: Projects tests `83781b3..d30b955`, `tests/` without `tests/tools`. All four layers reported. Of 52 findings, 37 survived. Four of them belong to the chunk-1 option-3 patch and were merged into it. The other 33 form 18 entries. 15 findings were rejected.

- [x] [Review][Patch] Truncation tests cannot fail on missing truncation (AA3+BH1+VG1+EC4) — the bound-scan tests index IDs that have no detail rows. `DetailAsync` already marks every slot omitted, so `Incomplete` stays true even if the scan's `Omitted()` at executor :149 were deleted.
  - Seed 201 valid, non-matching Projects and assert `Incomplete` with a null `Result`.
  - Add an exactly-200 control (199 plus the linked hint for Conversation) that asserts a complete NoMatch.
  - Assert the exact `p000`–`p199` selection.

  [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:139]
- [x] [Review][Patch] No test asserts `Result` for an incomplete response that has candidates (VG2) — simplifying executor :103 to `incomplete != 0 ? null : …` would pass every test. Assert `SingleCandidate` / `MultipleCandidates` in the incomplete tests that already produce candidates. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:316]
- [x] [Review][Patch] The read-model integrity guards are unpinned (VG3+BH8+EC8) — no test mutates any of these:
  - model or detail TenantId / ProjectId;
  - the Sequence match;
  - an undefined Freshness (including the default `0`);
  - an index with the wrong TenantId, an empty ETag or null collections.

  Dropping `detail.ProjectId != id` would emit an ID that was never authorized. Add these cases to the malformed-header and index theories, and assert the serialized body omits the foreign ID. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:282]
- [x] [Review][Patch] The legacy routes' final Folder recheck result is never shown to be used (VG4) — no test fake changes a Folder's outcome between its first and final owner reads. Discarding the second result still passes. Add one case per route where the Folder is Accepted first and Denied on its final read, and assert the Project is not disclosed. [src/Hexalith.Projects.Server/Queries/ResolveProjectFromAttachmentsEndpoint.cs:113, src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs:148]
- [x] [Review][Patch] The legacy Conversation route's 2 s deadline and four-call bound are untested (VG5) — deleting `CancelAfter` or `MaxDegreeOfParallelism` passes. Mirror the attachments deadline test (a slow Folder owner gives 503 in under 4 s) after the chunk-1 fan-out patch lands. [src/Hexalith.Projects.Server/Queries/ResolveProjectFromConversationEndpoint.cs:76]
- [x] [Review][Patch] The malformed owner-metadata theory skips explicit fail-closed branches (VG6+BH7) — no cases exist for:
  - an unknown `lifecycleState` (dropping it turns `Purged` into a complete NoMatch);
  - an over-long `safeLabel`;
  - a blank, untrimmed or invalid `projectId`;
  - a non-boolean `incomplete`.

  Add these as `InlineData`. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:204]
- [x] [Review][Patch] The envelope-validation theory covers only the attachment query and misses branches (BH5+EC3+EC5+EC6) — add:
  - Conversation-query cases: blank, untrimmed or null `conversationId`, and unknown or duplicate fields;
  - a `QueryType` mismatch;
  - an `EntityId` that differs from the aggregate;
  - `attachments: null` and `[null]`;
  - a string `includeArchived`;
  - case-variant names;
  - the inclusive 32-attachment boundary (32 accepted, 33 rejected).

  [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:467]
- [x] [Review][Patch] The test doubles ignore the arguments that matter (BH10) — `AdvisoryTestConversationDirectory` ignores `tenantId` and echoes any `conversationId`, and `ProjectReadTestFolders` ignores `projectId`. Owner-hop identity regressions therefore surface only in the gated lane. Record the arguments and assert the tenant and Conversation sent to the owner. [tests/Hexalith.Projects.Server.Tests/Queries/AdvisoryTestConversationDirectory.cs:12]
- [x] [Review][Patch] The gateway no-write snapshot can pass vacuously (BH15+EC13) — Dapr answers 204 with empty bytes for a missing key, and `beforeAdvisory` is never asserted non-empty, so a wrong key compares empty to empty. Assert every snapshot is non-empty. A full key-set comparison stays out of scope (Blind8). [tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs:169]
- [x] [Review][Patch] Mixed Folder+File inputs are untested on both paths (AA4+BH13+EC18) — the only mixed legacy case lost its `fileId`, and the supported query tests only File-only requests.
  - Supported query: a matching Folder plus a File should give a candidate with a non-null `Result`, `Incomplete`, `unsupportedInputKinds: ["file"]`, and no File-ID owner call.
  - Legacy route: a File-only matched Project stays out of the results.

  [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:61, tests/Hexalith.Projects.Server.Tests/Queries/ResolveProjectFromAttachmentsTests.cs:85]
- [x] [Review][Patch] The final scope recheck is unproven for no-signal attachment inputs (AA8+BH9+EC7) — only the Conversation case counts the owner rechecks. For the `empty`, `file` and `denied` cases, count scope-level authorization decisions and assert 2. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:492]
- [x] [Review][Patch] Denial tests don't prove that nothing downstream ran (BH6+EC16) — add an unauthorized-plus-malformed case expecting `safe-denial`, not InvalidEnvelope. In the identity-mismatch, list-only and stale/unavailable admission cases, assert zero owner and Folder calls. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:432]
- [x] [Review][Patch] The new-Folder-anchor test passes without proof (EC2) — empty candidates can come from another path. Record the Folder IDs the owner receives, assert `new-denied-folder` was checked, and assert `Incomplete == false`. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:236]
- [x] [Review][Patch] The "unknown Folder" and "archived needs owner access" cases don't test what they claim (AA7):
  - bind a Project to the unknown Folder;
  - in the archived case, accept the input and deny only the archived candidate's Folder.

  [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:71]
- [x] [Review][Patch] `EvaluatedAt` is never compared with the request clock (AA6) — assert `EvaluatedAt == f.Clock.GetUtcNow()` in `OldButValidIndexReceiptRemainsUsable`, where the index time differs. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:546]
- [x] [Review][Patch] Test doubles shared by the four-worker fan-out are not thread-safe (BH11):
  - `ProjectReadTestFolders.Reads++`;
  - the `Denied` set mutated in `OnRead`;
  - `AdvisoryTestLogger.Entries`;
  - the shared `revoked` flags.

  Use `Interlocked` or concurrent collections. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectReadTestFolders.cs:18]
- [x] [Review][Patch] The query discriminators are not pinned as wire literals (BH17) — assert both `QueryType` strings literally, and use the unused theory parameter or remove it. [tests/Hexalith.Projects.Contracts.Tests/Queries/ProjectAdvisoryContractTests.cs:15]
- [x] [Review][Patch] An always-true test predicate (BH18) — `Interlocked.Increment(ref calls) >= 1` has no effect. Use a plain predicate or assert the call count. [tests/Hexalith.Projects.Server.Tests/Queries/ProjectAdvisoryQueryTests.cs:186]

#### Rejected (chunk 2)

- AA2 low — production `/query` returns `Ok(failure)`, and `DaprDomainQueryInvoker` plus `HandlerAwareQueryRouter` turn an owner `safe-denial` into a gateway 404. So the fixture's 404 mapping matches production for every outcome Projects relies on; it differs only for an owner InvalidEnvelope, which Projects never sends.
- AA5 low — the final pass removes a revoked candidate through the same `TryRemove` path the surviving-peer fault test covers.
- BH2 / EC1 false — the spec bound is a maximum of four concurrent calls, and serial execution satisfies it; `<= 4` was the deliberate review-2 correction.
- BH3 low — production dependencies honor cancellation (EC-6), so uncooperative fan-out calls are theoretical.
- BH4 low — deterministic fake-time deadlines would need a production change to a clock-bound CTS; the 1–2 s margins are acceptable.
- BH12 low — all five catch sites share the one `LogFailure` helper, whose message shape is pinned.
- BH16 low — the executor tests cover denial indistinguishability; the HTTP lane only proves the wiring.
- BH19 low — a cancelled caller discards the response anyway.
- BH20 low — the executor's call graph has no persistence operations (same reasoning as Blind8).
- EC9 maybe-false (would be low) — the 200,000-entry in-memory scan plus 200 fake reads runs well inside 2 s; it is only flaky under extreme load.
- EC11 false — a File-only legacy NoMatch is the documented, intended behavior; the chunk-1 OpenAPI patch fixes the wording.
- EC14 low — the server always emits both lists; only a hypothetical foreign producer could omit them.
- EC15 low — the server emits names; tolerant integer reads affect only consumers.
- EC17 false — the Folders metadata route no longer exists (same as VG-O2).

### Review Findings — fourth pass, chunk 3 of 3 (2026-10-22)

Chunk 3 scope: the Conversations advisory slice `1f38912..71fab44` (advisory handler, DTOs, registration, tenant-access evidence, strict non-native branch and their tests). All four layers reported. Of 41 findings, 19 survived and were grouped into 10 entries; 22 were rejected. Every entry here changes `references/Hexalith.Conversations`. Any production change there needs a separately approved commit, push, release and consumer pin.

- [ ] [Review][Patch] Accept the ordinary allowed Tenant decision for advisory disclosure (AA1+VG-O2; decision 2026-10-22: option 2) — the shipped host uses the in-memory tenant store, whose `DefaultConversationTenantProjectionSignal` reports Healthy with no version or watermark, so the evidence requirement added under Blind2 makes every advisory read return `(null, true)`. This reverses that Blind2 patch. Remove the `ProjectionVersion`/`ProjectionWatermark` throw from `AllowedAsync`. Keep the retryable-denial throw and the exact Requirement, TenantId and CallerPrincipalId match, the same rule `ConversationProjectionReadService` applies to ordinary reads. Invert `DefaultNonsignalStorePermissionCannotDiscloseWithoutProjectionEvidence` so the default store discloses committed metadata. Delete `FinalPermissionRequiresUsableProjectionEvidence`, or narrow it to the retryable and identity-mismatch cases. Drop the version/watermark prerequisite sentence from `docs/supported-project-reads.md:83`. Ship together with the next entry as Conversations 1.3.1, then move the submodule pointer and `tools/release-packages.json` pins from 1.3.0. Each commit, push and release needs separate approval. [references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/GetConversationAdvisoryMetadataDomainQueryHandler.cs:93]
- [x] [Review][Patch] Classify a Conversation with no label and no scoring label as complete absence (AA6+EC5; decision 2026-10-22: option 1) — in `ConversationResolutionMetadataReadModel.From`, set `complete = scoring is null ? summary.Label is null : <existing check>` and persist `LabelPurpose = complete ? ConversationResolutionScoringPolicy.Purpose : null`, not `scoring!.Purpose`. `CanClassify(null, Purpose)` already returns true, so `IsWellFormed` and the handler's `classified` check accept the row unchanged. Add projection tests:
  - no label and no scoring label gives `IsLabelComplete == true` with a null `SafeLabel`;
  - a label without a scoring label stays incomplete.

  Add an advisory handler test showing a no-label Conversation returns `Incomplete == false`. Existing rows benefit only after reprojection or their next update; record that in the 1.3.1 release notes. [references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Projections/ConversationResolutionMetadataReadModel.cs:56]
- [x] [Review][Patch] The handler's catch-all hides test-double failures in tests that expect Unavailable (BH1+VG4+EC11) — Shouldly assertions inside `ResolutionMetadataAccess`, and forbidden-key throws from `ResolutionGuardedStore`, both become `Unavailable()`. Mirror the strict `DispatchAsync`: assert `Guard.ForbiddenAttempts == 0`, a read-key allow-list and the recorded access arguments in a `finally` block of `DispatchAdvisoryAsync`. [references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/ResolutionMetadata/ResolutionMetadataFixture.cs:128]
- [x] [Review][Patch] The advisory `Verified()` guards are unpinned (VG1+AA8+BH10) — only "unverified caller", which the SDK rejects first, and "tampered workload" are tested. Mirror the strict 15-case theory for:
  - `IsGlobalAdmin`;
  - scopes without `projects.read`;
  - `Paging`;
  - `EntityId`;
  - a non-empty, array, null or over-8 KiB payload;
  - a null origin context;
  - the `authenticated-caller` origin;
  - a delegation without an ID;
  - `OriginalActorId` mismatch;
  - domain case.

  Each case asserts `safe-denial`, zero access checks and zero reads. Add the missing-ETag, non-canonical DispatchId and unknown-lifecycle row cases. [references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAdvisoryMetadataQueryHandlerTest.cs:96]
- [x] [Review][Patch] The proof-fence comparison and the IsPending/Barrier checks are never decisive in a test (VG2) — both fence fixtures use `Current == null`. Make `FenceChangedDuringAdvisoryReadWithholdsMetadata` a theory with four cases: null-current, completed-replacement, pending-current and barrier. [references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAdvisoryMetadataQueryHandlerTest.cs:164]
- [x] [Review][Patch] The owner side never tests that Stale metadata is disclosed (VG3) — narrowing the freshness gate to Current passes every test, which would reverse the "lag is not incompleteness" decision. Add a Stale case asserting metadata is present and `Incomplete == false`. [references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAdvisoryMetadataQueryHandlerTest.cs:66]
- [x] [Review][Patch] The "foreign tenant" case proves nothing about isolation (BH2) — it runs on the never-projected `missing` fixture with a tenant-agnostic access fake. Run it on a projected fixture with a tenant-dependent fake, and assert that no key under the projected tenant is read. [references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAdvisoryMetadataQueryHandlerTest.cs:53]
- [x] [Review][Patch] The advisory query is never dispatched through the real SDK registration (BH3+AA9+EC13) — the discovery test dispatches only the strict query, and its `ReadKeys.ShouldAllBe` after `ShouldBeEmpty` is vacuous. Dispatch an advisory request through `app.Services` in both composition orders, and remove the dead assertion. [references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryRegistrationTest.cs:34]
- [x] [Review][Patch] Future-dated native Tenant evidence is untested on the strict path (VG5) — dropping `|| evidence.VerifiedAt > now` passes every test. Add a `+1 s` VerifiedAt case asserting Unavailable with no metadata reads. [references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/ConversationResolutionMetadataAuthorization.cs:82]
- [x] [Review][Patch] The ignored `signal` parameter is documented as gating disclosure (BH8+EC14) — `_ = signal;` discards it, while its `<param>` says absence "conservatively prevents disclosure". Correct the XML docs (and the `<param>` order) without changing the 1.3.0 signature. [references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/ConversationResolutionMetadataAuthorization.cs:18]

#### Rejected (chunk 3)

- AA2 / BH7 / EC1 low — poisoned or malformed tenant projections become `safe-denial` instead of unavailable. It is a rare integrity fault, and fixing it adds a branch (same as EC-4 earlier).
- AA3 / BH5 low — the handler validates every field that scoring consumes. A corrupt native proof is a projection fault, and reusing `IsWellFormed()` needs a validator split.
- AA4 maybe-false (would be low) — whether `Erased` means the Conversation is gone or only its content decides between denial and Incomplete. No data is disclosed either way.
- AA5 low — those returns expose existence only to a caller whose ordinary Read check passed, and Projects makes a second owner call (same as chunk-1 AA6).
- AA7 / BH4 false — Projects checks `callerToken` right after each owner call, and a remote caller aborts the HTTP request.
- BH6 low — `DefaultConversationTenantProjectionSignal` already logs the missing-signal warning, and owner logging needs a new dependency.
- BH9 low — the CRLF-to-LF churn in the registration file is real, but the whitespace-insensitive change is only 6 lines, and reverting it in a released file adds more churn.
- BH11 low — the only consumer is Projects, whose docs state the wire rules; a `V1` rename would be breaking.
- VG-O1 / EC6 low — Projects verifies the audience before the signed owner hop, so the missing owner audience check is defense in depth only.
- EC2 false — a revoke-and-regrant between the two checks still ends with Read at disclosure, which is the authorization required.
- EC3 false — accepting Stale with `Incomplete == false` is the 2026-10-21 decision.
- EC4 maybe-false (would be low) — whether metadata can precede its proof generation depends on persistence order. Advisory tolerates lag, and the owner authorized the row either way.
- EC7 low — skew and evidence-age policy belong to the strict path, which has no consumer in the advisory scope.
- EC8 low — a null proof gives the same serialized version on both checks, but both checks still returned Allowed.
- EC9 low — the allowed decision's version is used only as presence evidence on the advisory path.
- EC10 low — ASP.NET hosts always register `IConfiguration`; only bare test collections lack it.
- EC12 low — that test hangs only when a regression already fails it, and global test cancellation still bounds it.

## Fourth-pass local implementation — 2026-10-22

All local fourth-pass code, test and documentation patches are implemented. Current verification passed 2,657 distinct xUnit cases plus 29 planning-guard tests (2,686 total), with zero failures or skips. The full Projects suite includes the actual authenticated gateway, signed owner and isolated Dapr/Redis store case, with nonempty before/after snapshots. All seven frozen matrix rows map to current passing XML. Debug source builds and the package-mode Release build pass without warnings/errors; the e2e TypeScript check passes after exact-lock restore. Source hashes, commands, corrected environment failures and matrix/correction mappings are in [fourth-pass evidence](evidence/6-4-advisory-resolution-20261021/review-4/README.md).

The ordinary advisory decision and label-absence fixes are local Conversations 1.3.1 candidates. The unchecked release/pin item remains incomplete: no Conversations commit, push or publication, Builds pin, root gitlink update or `tools/release-packages.json` version change was performed. These require the separate approvals specified above. Candidate release notes record that existing unlabeled rows need reprojection or a subsequent update. Root changes remain uncommitted. The live AppHost browser lane and production deployment acceptance were not executed. The parent workflow audit/review remains outstanding.

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

### Workflow review of fourth-pass implementation — 2026-10-22

All three independent layers reported before triage. Edge Case Hunter returned no findings. Blind Hunter returned ten findings; Verification Gap returned two existing deferred gaps. Each finding has an individual verdict below before grouping.

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Fourth-pass BH1: duplicate owner response fields | low | A synthetic response can contain conflicting duplicate JSON properties, but the registered signed owner emits the source-generated ConversationAdvisoryMetadataResult shape once per property. No reachable duplicate-producing owner path was demonstrated. Extra response-schema parsing adds guards for a hypothetical malformed transport; reject. |
| Fourth-pass BH2: unsafe owner label text | false | GetConversationAdvisoryMetadataDomainQueryHandler calls ConversationResolutionScoringPolicy.CanClassify, which invokes GovernanceContractValidation.RequiredSafeText. Unsafe/blank/control text is withheld and marks the owner result incomplete before the signed response is serialized. The claimed complete unsafe-label response does not come from the registered owner. |
| Fourth-pass BH3: scoped owner client resolved from singleton root | medium | AddEventStoreDelegatedOwnerQueries registers DelegatedOwnerQueryClient scoped. When registered before AddProjectsServer, TryAddTransient preserves it, and the singleton directory captures the root provider. ValidateScopes then rejects client resolution. Patch both delegated directory factories to resolve through the active HttpContext request services and add both registration-order controls. |
| Fourth-pass BH4: metadata command can persist a poisoned classified-absence event | false | ValidateEnvelope is state-independent. The current ConversationAggregate and command dispatch have no UpdateConversationMetadata producer/handler; repository-wide production searches show only its schema/fingerprint contracts. State/projection replay correctly reject an externally manufactured event inconsistent with the existing label. The claimed accepted-command-to-durable-event path is absent; do not change preserved native replay semantics. |
| Fourth-pass BH5: obsolete G-6 default tuple | medium | carried: same location and claim as Final BH2 above. Retain the existing deferred qualification-tooling entry; no duplicate work or ledger entry. |
| Fourth-pass BH6: local MCP transport bootstrap | false | carried: same location and claim as Final BH1 above. These are preserved user configuration with the recorded listening service; do not overwrite it or invent a startup requirement. |
| Fourth-pass BH7: 1.3.1 publication/pin unfinished | medium | Correctly unfinished: the sole unchecked fourth-pass release group and current evidence explicitly retain this boundary. Source tests use local projects; the package-mode build still consumes released 1.3.0. Keep in-progress until separately approved publication and consumer verification; no new source patch. |
| Fourth-pass BH8: delegated owner not in routine CI | medium | carried: the same deferred gateway-lane gap recorded in the second/fourth-pass findings and deferred-work ledger. The fresh local signed gateway/store case ran and passed; routine CI qualification remains deferred. |
| Fourth-pass BH9: no overlapping two-caller gateway test | low | The new executor retains all invocation observations in ExecuteAsync locals, the directory keeps only a client factory, and the SDK obtains caller authority from the active HttpContext. Existing owner authorization tests cover overlapping permission decisions. No shared request state or cross-caller result was demonstrated. A new concurrent gateway harness adds complexity without a current defect; reject. |
| Fourth-pass BH10: Contracts README conflates strict and advisory metadata | low | The final scoring-label paragraph still assigns native proof and parent-resolution gates to all metadata reads, while the separate advisory query accepts ordinary owner Read authorization. Correct the paragraph to distinguish the strict query and explicit advisory coverage; direct documentation patch. |
| Fourth-pass VG1: delegated owner not in routine CI | medium | carried: pre-verified duplicate of the existing delegated gateway-lane deferral (also BH8). Preserve its current ledger entry without duplication. |
| Fourth-pass VG2: immutable Dapr selection not behaviorally pinned | medium | carried: pre-verified existing qualification-tooling gap already in deferred-work.md. It is outside the advisory source changes; retain the recorded deferral without duplicate work. |

Surviving patch groups: request-scoped delegated client resolution (BH3), and strict/advisory Contracts guidance (BH10). Publication/pin remains the existing pending approval boundary.

## Review Resolution

The final three-layer workflow review is complete. All nine final patch groups are resolved: legacy final authorization and actual Folder checks, malformed index receipts, owner-completion cancellation, authorized Conversation HTTP routing, independent byte-limit controls, literal DTO wire assertions, read-only admission and legacy File documentation. The parent inspected the final code/generated diffs and independently verified all 70 source/config/evidence hashes. Final suites passed 2,609 unique tests with zero failures/skips; all affected Debug builds and the package-mode Release build passed with zero warnings/errors. All seven frozen matrix rows, 116 earlier references and 33 final-correction references map to current passing XML. The owned Dapr/Redis fixture was cleaned up successfully. Evidence: [final verification](evidence/6-4-advisory-resolution-20261021/review-3/README.md). The one pre-existing G-6 default-policy issue is recorded in the deferred-work ledger. No unresolved implementation finding remains in this advisory scope.

The earlier review resolution below is retained as history.

All accepted findings are resolved. The seven bounded patches passed their regressions and full suites. PRD/consumer guidance now assigns manage capability to independent confirmation. The real gateway exposed dropped permission evidence in the existing Conversation facade; it now carries the exact evaluated observation per invocation, with an overlapping-request regression. The older strict test now verifies this preservation while retaining default-missing-evidence and native-proof requirements. No strict handler checks changed. Parent independently inspected the final diffs and audited every matrix reference against passing XML. Historical failing runs remain in the evidence directory. No unresolved implementation finding remains in the approved Conversation/Folder advisory scope; unsupported Files and deployment acceptance remain explicit limitations.

## Workflow Completion

Build workflow complete on 2026-10-22; the sprint story is ready for review. The approved frozen scope excludes root Git staging/commits, and the later explicit go-aheads covered Conversations and the Builds pin. That specific scope constraint takes precedence over the workflow's generic local-commit step. Root changes remain uncommitted; no root push was performed. The externally landed publication and consumer pin are independently verified and are not duplicated by this session.

## Verification

Build individual affected test projects Debug with `-p:UseHexalithProjectReferences=true`. Run built xUnit assemblies with `-class`/`-method` and `-result-xml`, not solution tests. Run focused new matrix tests, existing Resolution engine tests, then full affected Projects.Server and Conversations.Server suites. Record exact results and genuine blockers; missing G-4 runner/deployment acceptance is independent. Parent independently audits diff and matrix before review. Run `python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py`, production-authority validator and `git diff --check` (including owning submodule).

## Current Resumption Result — 2026-10-22

All 36 local fourth-pass patch groups and both subsequent workflow-review corrections are implemented and verified. The three independent review layers have reported; every finding has an individual verdict above. Both accepted corrections are complete: delegated owner clients resolve from active request services in either SDK registration order, and Contracts guidance separates strict and advisory reads. Existing deferrals remain recorded without duplicate ledger entries.

Final parent verification passed 1,089 Projects.Server tests and 1,162 Conversations.Server tests, zero failures/errors/skips/not-run. Together with unchanged Contracts 202, engine/confirmation 107, client-generation 101 and planning guard 29 results, 2,690 unique tests pass. Source Debug builds and the package-mode Release build have zero warnings/errors. The fresh authenticated signed gateway/owner/Dapr/Redis case passed, and both owned resources stopped successfully. The frozen scope remains unchanged. See [final parent evidence](evidence/6-4-advisory-resolution-20261021/review-4/parent-final/README.md).

The story remains `in-progress`: the sole unchecked release group requires separately approved Conversations 1.3.1 commit, push, release, verified package consumption and consumer pins. The candidate notes record that existing unlabeled rows improve only after reprojection or their next update. No Git staging/commit/push/branch, dependency-version change or deployment was performed. The frozen prohibition on root Git mutations and the explicit separate release approvals override the generic Build presentation step; do not mark the story done while this delivery work remains.

## Conversations Commit Approved and Created — 2026-10-23

The user explicitly replied `approve` to the prepared local Conversations commit request. Pinned commitlint 21.2.3 passed with zero problems/warnings, both repository hooks ran successfully, and the exact 13 reviewed files were committed as `7a38d3e48a41909d9db04188f31f230da4d2d7a9` on Conversations main, parent `71fab44b7dd664699cedbbfc17ed0d7822e8bdb9`. Parent verified every committed blob against the approved SHA-256 and confirmed a clean owner working tree. Source bytes are unchanged from the 2,690-test verification. No new test run was needed for this Git-only step. Evidence: [commit handoff](evidence/6-4-advisory-resolution-20261021/review-4/parent-final/commit-handoff/commit.json).

Push, Conversations 1.3.1 release, package verification and consumer pin remain pending their separate approvals. The root index remains untouched and the story remains in-progress.

## Conversations Push Approved and Verified — 2026-10-23

The user replied `yes` to pushing the prepared Conversations commit. A nonrecursive fetch found three nonoverlapping upstream commits at `26a269a5a73992d4df79d55460a04d1ab81dbfcc`; the reviewed change was rebased byte-identically as `7322ada8193583c0fcaba409b8e13f190a9b30b6`. Full Conversations.Server passed 1,320 tests and the focused Projects advisory/legacy/registration lane passed 193 tests, zero failures/skips. Both Debug builds and pinned commitlint passed.

The initial ordinary push was rejected because remote main advanced during validation. The additional upstream commit `0f179a21ad30995eb8ce350c39127926b5c35c25` adds only Epic 16 planning/schema/tooling files, without changing runtime sources or the 13 reviewed files. The identical patch was rebased as `b3962b007804aa47f5130417cd22da8436f79185` and pushed normally to `origin/main`; no force or submodule update was used. All 13 committed file hashes remain equal to the approved candidate, and the owner working tree is clean. The remote main ref was independently verified. Evidence: [push handoff](evidence/6-4-advisory-resolution-20261021/review-4/parent-final/push-handoff/push.json).

Exact-source CI run `39904117437` is pending/in progress. Commit policy passed. The read-only semantic-release plan confirms exactly 1.3.1 and official destination checks confirm its packages/tag/release are absent. Release dispatch, package verification and consumer pins remain pending the successful CI prerequisite and separate approvals; no publication was performed.

## Current Publication Gate — 2026-10-23

The authorized push is complete as `b3962b007804aa47f5130417cd22da8436f79185`, with exact reviewed bytes. Independent subsequent commit `91eae8f21dd80157824e7d71d4b7a6d64f02ed5c` advanced main and superseded CI run `39904117437`. All completed required jobs passed; repository tooling and Aspire were canceled by the later push. Latest CI run `39905869749` covers current main and is queued/in progress. The release-source guard exits 1 with `EXACT_CI_ABSENT`, correctly withholding publication until the newest live-main CI succeeds. Exact command/result and source history: [current push handoff](evidence/6-4-advisory-resolution-20261021/review-4/parent-final/push-handoff/README.md).

No publication or consumer pin was performed. The story remains in-progress pending successful CI on live main and separate release approval. The published source push is preserved; no further code changes, force-pushes or root staging were performed.

## Publication Prerequisite Fixture Repair and Approved Commit — 2026-10-23

The recorded latest CI run `39905869749` completed with one failing repository-tooling job: 36 setup errors came from the Epic 16 draft-consistency fixture copying unavailable nested Platform sources. The one-file repair uses deterministic external-source bytes only inside its disposable fixture, with its existing test-local hash binding. The production verifier, pins, schema, workflow, package and runtime sources remain unchanged; no nested submodule was initialized. Local reproduction recorded 1 pass/36 errors before the patch; all 37 affected cases plus the unchanged anti-skip guard now pass (38 total, zero failures/errors/skips), with every exact CI error mapped to a passing case.

The optional full tooling reproduction was interrupted to proceed with the approved commit after 1,821 completed passing cases and 17 existing deselections. It is not a full-suite pass; both owned processes exited. The actual prerequisite verifier still returns BLOCKED/INPUT_UNAVAILABLE with hold ACTIVE and all authority flags false. Parent independently audited the complete diff, all seven new source/config hashes and all 36 error-to-pass mappings. All 39 prior story source hashes remain unchanged and all 65 frozen-matrix references resolve to passing recorded XML. Story authority validation passes.

The user explicitly selected `Approve local commit`. Pinned commitlint 21.2.3 and both repository hooks passed. The parent created `94fcefbf0aa6744c0c4033fc904a22aae72e11b2` on Conversations main, parent `b3962b007804aa47f5130417cd22da8436f79185`, containing exactly `_bmad/scripts/tests/test_verify_epic_16_prerequisites.py`. Its committed bytes equal the approved SHA-256; the owner working tree is clean. Evidence: [fixture repair and commit handoff](evidence/6-4-advisory-resolution-20261021/review-4/parent-final/publication-prerequisite-repair-20261023/README.md).

The story remains in-progress. No push, publication or consumer pin was performed. The next separate approval is synchronization of this byte-identical patch over current Conversations main followed by an ordinary push. Read-only GitHub comparison shows two nonoverlapping upstream commits, with live main observed by Git as `bb6ae6e6574377e8fc628266f16b51a301f9736e`. Successful exact-source CI and the separate Conversations 1.3.1 release approval still precede publication; consumer verification/pins remain unfinished. Root changes and the original frozen scope are preserved.

## Approved Prerequisite Fixture Push Completed — 2026-10-23

The user replied `approve` to rebasing the one-file commit onto current Conversations main and pushing. A nonrecursive fetch found `bb6ae6e6574377e8fc628266f16b51a301f9736e`, with two nonoverlapping upstream commits. The complete approved patch and full message were preserved byte-identically in rebased commit `9911b4c9ad26f125a5271fc38f070297e4a9713f`. Pinned commitlint passed; the exact 38-case prerequisite/anti-skip lane passed again after rebase with zero failures/errors/skips. An ordinary, non-force, nonrecursive push succeeded. Independent Git readback verified remote main at that full SHA, and the owner working tree is clean. Evidence: [completed fixture push handoff](evidence/6-4-advisory-resolution-20261021/review-4/parent-final/publication-prerequisite-repair-20261023/push-handoff/README.md).

The pushed source has CI run `39932499668` (queued), plus CodeQL and commit-policy push runs. The exact-source publication guard exits 1 with `EXACT_CI_ABSENT` until this newest CI succeeds. The read-only semantic plan confirms exactly 1.3.1, and official release destinations are verified unused. No release dispatch, dependency pin, root Git commit or deployment was performed. Story 6.4 remains in-progress pending successful current-source CI, the separately required release approval, publication verification and consumer pins. The original frozen scope and root user changes remain preserved.

## Current Resumption Result — 2026-10-27

The advisory implementation is already present. Fresh focused checks passed 215 Projects cases, 116 Conversations cases and 37 planning-guard cases (368 total, zero failures/skips); both source Debug builds have zero warnings/errors. Story/index authority validation passes. Parent independently audited the fresh XML and build summaries. The frozen scope remains byte-identical. Eleven of the 39 previously recorded source files have later changes, so the older full-suite and gateway receipts remain historical rather than current-tree acceptance. Exact commands, current source hashes, passing XML and limitations: [resumption evidence](evidence/6-4-advisory-resolution-20261021/resumption-20261027/README.md).

Conversations live main is observed at `536bf80d5fd5c2a64c2f64815edc67074fd5a6ea`; [exact-source CI 40499324347](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/40499324347) failed. Later enrollment/build changes require a Platform source mapping absent from CI, the minimal-module fixture fails locked restore (`NU1004`), the dependency-mode tooling check fails, and Windows checkout rejects long Epic 16 fixture paths. The read-only release-source verifier exits 1 with `EXACT_CI_ABSENT`. The official verifier confirms Conversations 1.3.1 destinations remain absent; Builds and `tools/release-packages.json` still select 1.3.0.

Story status remains `in-progress`. Resume producer-owned enrollment/CI remediation before the separately approved 1.3.1 publication, official package verification and consumer pins. No source/dependency edits, Git mutations, remote writes, publication or deployment were performed. No fresh full-suite, signed gateway/store or live AppHost acceptance is claimed. The frozen no-host-migration/new-platform boundary and separate publication approval remain in force.

## Approved CI Repair Scope Extension — 2026-10-27

The user explicitly approved including the upstream Conversations/Builds CI repairs in this run. Preparation and testing are authorized by the latest `yes`; publication remains a separate later approval. The [CI repair specification](spec-6-4-ci-build-mapping-repair.md) governs this bounded extension. Preserve the original frozen advisory behavior and all existing authorization/build gates.

## Approved CI Repair Prepared and Reviewed — 2026-10-27

The separately approved upstream CI repair is implemented and reviewed in Builds and Conversations. It adds producer-owned runtime-free build mapping, CI/release consumption, safe isolated-consumer environment, Windows checkout ordering and a frozen-fixture package compatibility overlay with selected-catalog drift rejection. All six accepted review patch groups are resolved; final independent checks pass 430 producer cases, 8 helper cases, 3 overlay cases and 7 compatibility assertions. Both current source test-project builds and the standalone compatibility build pass without warnings/errors. Source hashes, frozen fixture preservation, exact commands and limitations are in the [final parent audit](evidence/story-6-4-ci-build-mapping-repair-20261027/parent-final/README.md). The broader 2,174-case producer attempt has two existing environment-dependent FAIL_SKIP failures and is not a full-suite pass.

Story status remains in-progress. Source preparation/testing is complete; no owning index, commit, push, release, accepted pin or runtime/deployment was changed. The accepted 4.35.2 CLI lacks the new command and the actual package tuple still fails closed. Producer CI/qualification, a compatible accepted tuple/tool and Conversations 1.3.1 publication/consumer pins remain delivery work. The next concrete approval is the reviewed Builds producer commit/push; publication remains separate.

## External Source Landing Observed — 2026-10-27

A separate `/pushall` operation committed and pushed the exact reviewed candidate files as Builds `22569245259c70f212e3073b5241c660b5d01487` and Conversations `cec9a1e1bbdf7e8efb2cb3aa299add7f52c2a6ef`; all 33 canonical Git blob IDs match the reviewed bytes. This session created no commit or push, and no duplicate is needed. Source trees/indices are clean. Later external updates superseded initial CI; the latest exact-source Builds run remains pending/in progress. Conversations' real package compatibility job now passes and Windows checkout succeeds; its preparation still correctly refuses the old 4.35.2 CLI's unavailable command. [Current landing/CI audit](evidence/story-6-4-ci-build-mapping-repair-20261027/parent-final/README.md) supersedes the earlier next-commit proposal. Source preparation/testing is complete; Story6.4 delivery and separate producer/publication/accepted tuple/pins remain pending.

## Resumption After External Landing — 2026-10-27

The implementation and approved CI repair remain landed; this pass made no source, dependency/tool-pin, Git or remote mutation. Root was externally advanced to `c978a5e86c8134ba09a166e093a1a4d6b4db77a8` with the preceding evidence checkpoint. Fresh planning guard passes 37 cases, and story/index authority pass. The original frozen scope remains byte-identical.

Candidate source preparation now succeeds directly in the clean actual umbrella without a disposable source workspace. Current mapped Conversations.Server.Tests Debug build fails closed with two HXW006 errors: Tenants source references carry appended `AdditionalProperties` that the producer's retained-reference policy rejects. Ordinary Projects source builds and narrower test-project fallbacks also refuse missing Conversations mapping; reusing the Conversation-active mapping correctly rejects the unmapped Projects root. No stale .NET assemblies were run as current-source proof. Exact commands, current source hashes, outputs and the distinct Tenants/producer compatibility mismatch are in [the new resumption receipt](evidence/6-4-advisory-resolution-20261021/resumption-after-landing-20261027/README.md).

Accepted CLI 4.35.2 still rejects `prepare-build` with HXC001. Candidate package preparation still refuses HXW011/HXW009/HXW010. Read-only observation finds newer Builds main `cbe58fa72f92327752fe0d75617ae91fe18da5d9` with queued exact-source CI `40561467073`; Conversations live main remains `26298b31eb4d362f88e008246ac3acb160128330`, with failed preparation jobs and no successful complete exact-source CI. Official release-source verification exits 1 with `EXACT_CI_ABSENT`; official destination verification confirms 1.3.1 remains absent. Story status stays in-progress pending the owner compatibility decision, producer qualification, compatible accepted tuple/tool, separately approved publication and verified consumer pins. No runtime/deployment acceptance is claimed.

## Active Producer Mapping Compatibility Correction — 2026-10-27

The resumed mapped build exposed Tenants `Directory.Build.targets` appending `HexalithEventStoreVersion` metadata after producer selection, which the unchanged Builds reconciliation guard correctly rejects as HXW006. The approved CI preparation/testing intent includes shared Builds support and consumer repairs needed to unblock Story 6.4. Correct only this build-configuration compatibility: ordinary local builds retain their existing per-edge propagation; builds with an active producer mapping defer dependency selection to that mapping. No accepted dependency version, runtime enrollment, resolver gate, index, commit, push or publication changes. This is an implementation adaptation within the approved CI preparation scope, not a new release approval.

- [x] `references/Hexalith.Tenants/Directory.Build.targets`: suppress the local per-edge override when `HexalithSourceMappingHash` is present and `HexalithSourceMappingApplies` is true; retain ordinary local behavior and container defaults.
- [x] Verify ordinary and mapped evaluated project metadata, retry the exact mapped Conversations Debug build and the narrowest affected advisory/strict tests available, and retain exact successful or blocked results with source hashes. Preserve the initial failed receipts.

Given an active producer mapping, when Tenants project references are evaluated, then local version propagation cannot override the producer. Given an ordinary local source build, when project references are evaluated, then existing propagation remains. Release qualification, compatible accepted tuples, Conversations 1.3.1 publication and consumer pins remain separate pending delivery requirements.

The restored mapped build then reached a pre-existing CA1062 analyzer failure in Conversations `ConversationTenantAccessRegistrationTest.MissingEnrolledDeliveryInputsFailBeforeConsumerRegistration`. Add the standard null check for its public theory parameter before use; preserve all seven negative enrollment cases and the analyzer gate. This direct test-harness correction is within the same CI preparation scope.

### Supplemental source review triage

All three source-review layers reported before triage. Edge Case Hunter returned no findings. The review diff contains only the two new local source corrections; prior independently reviewed advisory/CI implementation is unchanged.

| Finding | Verdict / route | Evidence and disposition |
| --- | --- | --- |
| BH1: reciprocal EventStore Admin source edge | medium / defer | Reproduced unchanged EventStore `Directory.Build.targets` appending `HexalithEventStoreVersion` on its Tenants edge; actual Admin.Server producer reconciliation exits 1 with HXW006. This pre-exists the Tenants correction and does not occur in the current passing Conversations graph. Retain exact command/output and defer broader EventStore Admin build compatibility; do not claim whole-workspace source acceptance. |
| BH2: automatic metadata regression coverage absent | medium / patch | The four current evaluated controls demonstrate behavior, but existing normal suites do not prevent recurrence. Add a real evaluated-MSBuild theory to the existing automatically executed Tenants Contracts governance suite. |
| VG1: same metadata branch lacks executed regression | medium / patch | Pre-verified: existing Builds mapping tests use synthetic projects and Conversation tests use stub dependencies, so neither evaluates actual Tenants targets. Same root cause as BH2; retain the individual verdict and apply one test patch. |

Surviving patch group: meaningful evaluated-MSBuild regression coverage, including ordinary source, either marker alone, active mapping, package mode and pre-existing metadata preservation. The pre-existing reciprocal EventStore Admin mismatch is recorded separately in deferred-work.md.

## Supplemental Source Review Resolution and Handoff — 2026-10-27

Both local build corrections and the review-required automated regression are implemented. Parent inspected the final three-file diff, reran all seven new real-target MSBuild cases, rebuilt the actual mapped Conversations.Server.Tests Debug graph with zero warnings/errors, and reran 243 advisory/strict/registration/projection/recovery tests. Planning guard 37 and both authority checks pass: 287 distinct passing tests, zero failures/skips. Source copies, canonical owner baselines, exact commands, raw XML/logs, source hashes and individual review verdicts are in the [current correction evidence](evidence/6-4-advisory-resolution-20261021/resumption-after-landing-20261027/mapping-correction/README.md). The original frozen advisory scope is byte-identical. All three review layers reported; the regression gap is resolved, and the pre-existing reciprocal EventStore Admin mapping failure is recorded in deferred-work.md without claiming broader source acceptance.

Tenants has two prepared files (Directory.Build.targets and PackageGovernanceTests); Conversations has one prepared test null-check. Exact full messages pass each owner's effective policy with pinned commitlint 21.2.3, zero problems/warnings. No index/ref, push, accepted dependency/tool version, publication or runtime/deployment changed. The next concrete approval is the two owning local commits, retaining later separate push/release decisions. Existing approved CI preparation/testing covers these bounded compatibility corrections; it does not grant delivery approval. The explicit frozen Git prohibition and fourth-pass separate-approval rule take precedence over the generic Build completion/local-commit step.

Story 6.4 remains in-progress: accepted tool 4.35.2 still lacks prepare-build, actual package preparation still rejects the Platform/catalog/tool tuple, and successful exact-source CI, qualified producer/tool/tuple, separately approved Conversations 1.3.1 publication and verified consumer pins remain unfinished. Root Projects source/gateway/full-suite and deployed acceptance remain unclaimed. Do not mark the story done from this supplemental source repair.
