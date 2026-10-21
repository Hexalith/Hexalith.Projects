---
title: 'Story 6.4: Authorized advisory Project suggestions'
type: 'feature'
created: '2026-10-17'
updated: '2026-10-21'
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

## Review Resolution

All accepted findings are resolved. The seven bounded patches passed their regressions and full suites. PRD/consumer guidance now assigns manage capability to independent confirmation. The real gateway exposed dropped permission evidence in the existing Conversation facade; it now carries the exact evaluated observation per invocation, with an overlapping-request regression. The older strict test now verifies this preservation while retaining default-missing-evidence and native-proof requirements. No strict handler checks changed. Parent independently inspected the final diffs and audited every matrix reference against passing XML. Historical failing runs remain in the evidence directory. No unresolved implementation finding remains in the approved Conversation/Folder advisory scope; unsupported Files and deployment acceptance remain explicit limitations.

## Verification

Build individual affected test projects Debug with `-p:UseHexalithProjectReferences=true`. Run built xUnit assemblies with `-class`/`-method` and `-result-xml`, not solution tests. Run focused new matrix tests, existing Resolution engine tests, then full affected Projects.Server and Conversations.Server suites. Record exact results and genuine blockers; missing G-4 runner/deployment acceptance is independent. Parent independently audits diff and matrix before review. Run `python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py`, production-authority validator and `git diff --check` (including owning submodule).
