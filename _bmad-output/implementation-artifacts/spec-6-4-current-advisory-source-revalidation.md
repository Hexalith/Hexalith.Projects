---
title: 'Story 6.4: Revalidate advisory queries against the current source'
type: 'chore'
created: '2026-10-31'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
approved_at_utc: '2026-10-31T22:51:51Z'
approval_decision: 'Approve and continue'
story_key: '6-4-resolve-projects-with-transient-current-explanations'
baseline_commit: '353cfd33e54270a1acb028c1c66ecb7cd548f3e7'
context: []
---

<frozen-after-approval reason="Continuation of the approved advisory implementation; existing source verification is reversible and requires no new product decision">

## Intent

Revalidate the implemented Story 6.4 Conversation/Folder advisory queries after the independently recorded workspace, SDK and catalog changes. Produce current-source evidence using the supported Projects declaration, actual producer-generated environment, existing tests and actual authenticated gateway/owner/Dapr storage fixture. Preserve scoring, authorization, incomplete File coverage, no selection and zero query writes. Historical producer publication and source results retain their original identities.


## Approved scope extension — 2026-11-01

The user selected **1: Extend owner qualification and continue**, approving the concrete owner-qualification proposal. This explicitly authorizes the existing Folders/Conversations host, receiver-catalog, deletion transport/fixture and standalone source-graph compatibility work needed for full affected-project qualification. Earlier verification-only and metadata-only footprint limits are superseded for this continuation; security guards, existing behavior, preservation, publication hold and no Git/publication/dependency-version changes remain.

</frozen-after-approval>

## Implementation Notes

This bounded continuation changes only this spec and a new evidence directory under `evidence/6-4-advisory-resolution-20261021/current-advisory-source-20261031/`. There is no new API, dependency selection or irreversible operation. Preserve original source, configuration, indexes, refs, historical evidence and unrelated runtime resources. Keep canonical Story 6.4 and sprint in-progress; source proof does not accept producer security, Platform publication, deployed policy/runtime or consumer adoption. Do not stage, commit, push, publish, deploy, initialize nested submodules, edit generated imports, suppress analyzers, alter deadlines or substitute old passing binaries.

Use root `.gitmodules` to inventory exact direct source revisions and confirm checkout/gitlink equality. Run read-only Git commands with `GIT_OPTIONAL_LOCKS=0`. Read owning baseline/configuration before execution. Use the current Builds public CLI `prepare-build` with `module/hexalith.module-manifest.v2.json`, source mode and a fresh temporary mapping directory. Apply every emitted environment value unchanged to individual Debug builds of Contracts.Tests and Server.Tests. Do not use Conversations' mapping for Projects. A source warning remains a warning; package identity guards remain intact.

Execute existing contract advisory cases, `ProjectAdvisoryQueryTests`, `DelegatedProjectAdvisoryConversationDirectoryTests`, `SupportedProjectReadHttpTests`, module registration and existing resolution engine/mappers. Use direct xUnit v3 assembly selectors and retain XML. Exercise `SupportedProjectGatewayHttpTests.GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures` with command-owned isolated Redis/Dapr, actual JWT/gateway/delegated owner handlers and persisted storage; missing runtime means blocked, never a skipped success. Reuse the existing isolated gateway/store procedure after inspecting and adapting its fixed paths; stop only resources created by this run and verify cleanup. Check owner advisory/strict metadata regression tests if their current source can build through its own producer mapping. Do not force a Projects mapping onto owner builds.

Capture exact argv, cwd, safe explicit environment, UTC times, exits, source/generated-input/executable hashes, XML totals, matrix coverage and cleanup in the new directory. Exclude credentials and private runtime logs. If preparation/build/test fails, retain the failure, try the narrowest supported existing fallback, and document the exact remaining correction instead of modifying production source in this verification-only slice. Run production-authority validators and the appropriate whitespace check. Recheck source/repository identities and evidence hashes. Record a concise outcome and remaining work here and in the new receipt. A bounded verification blocker must not be converted into Story completion.

The initial current-source Server.Tests build failed with CS7036 at two gateway router registrations because EventStore now requires committed route catalog authority. The bounded repair updates only those existing test fixtures and a shared immutable test catalog, including signed surface claims and exact tenant/query bindings. Production source, public API and dependencies remain unchanged. This small test compatibility correction is necessary to execute the approved advisory verification; retain the original build failure.

The real enforced gateway now reproduces HTTP 500: `DaprDomainQueryInvoker` rejects the returned projection identity. Replan: omit the query discriminator from computed query results in Projects and its two owners. Payloads, security and persisted projections remain unchanged. Seven-file candidate patches are prepared but unapplied; all apply checks pass. This expands beyond verification, so implementation awaits the Build checkpoint. Honor the active Parties publication hold; perform no further shared Parties builds until released, or use isolated dependencies.

Pre-approval evidence: 591 distinct focused cases pass; the enforced gateway case fails with the demonstrated projection-identity mismatch. Full affected-project qualification remains pending. The canonical story/sprint stay in-progress, and the active producer publication hold is preserved.

On approval, the tasks below supersede the initial verification-only footprint and permit only the listed protocol/test corrections.

Implementation continuation: the user approved this full repair and continued on 2026-10-31. Preserve every existing dirty fixture and historical evidence byte; create fresh output paths for new execution. The Parties publication hold remains active at the last observation: validation must isolate dependencies so it performs no shared Parties build or edit. Command-owned disposable repository clones/checkouts and sparse materialization are permitted for that isolation; real repository indexes/refs stay untouched. This is the parent Build implementation handoff; implement and verify this spec without restarting or rendering the Build workflow.

## Code Map

Evidence paths below resolve from this spec’s directory; source paths resolve from their owning repository.

- `evidence/6-4-advisory-resolution-20261021/current-advisory-source-20261031/proposed-repair.json` — exact repository revisions, seven file paths/hashes and three patches. Read it and every patch; change only the listed computed response metadata and corresponding assertions.
- `evidence/6-4-advisory-resolution-20261021/current-advisory-source-20261031/applied-fixture.patch` — preserve the three existing fixture changes: immutable schema-three routes, named human policy and signed surface claims. Never bypass catalog enforcement.

## Tasks & Acceptance

- [x] Apply the three owner-scoped patches listed above; preserve query discriminators, payload schemas, authentication and real persisted projection identities.
- [x] `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectReadTests.cs` — assert successful list/open results omit projection metadata; retain payload/audit checks. Verify catalog refusals and missing surface claims in the real gateway lane.
- [x] Run the existing focused and full affected test projects using fresh producer mappings and owned storage; record every failure, zero-write snapshots, binary identities and cleanup in the receipt. Preserve the publication hold.
- [x] Update this spec and the receipt with executed results and remaining independent delivery gates. Keep main Story 6.4/sprint in-progress.

Acceptance: Given the enforced computed catalog, when all six fixture queries execute, then authorized requests succeed with unchanged payloads and no fabricated projection identity. Given denied or missing authority, when queried, then safe refusal and persisted zero writes remain. Given affected projects, when validated, then their actual builds/tests pass without skipped acceptance cases or weakened guards.

## Execution decision: authority fixture compatibility

The first full current-source Server run exposed the SDK guard rejecting the embedded issuer in Production: 42 existing authority cases and three new catalog controls failed before query assertions. The parent Build continuation authorized a narrow compatibility correction on 2026-10-31: only `ProjectReadAuthorityFixture.StartAuthorityAsync` uses Development with `WorkloadAuthorityOptions.NonconformingDevelopmentFixture = true`. The gateway, Projects and Folders receiver hosts remain Production-configured; their catalog enforcement, named policies, credentials and all prior dirty changes remain. No SDK guard changes or production bypass are permitted. This is a Development test issuer with Production-configured receivers and supplies no deployment acceptance. Original failures remain in the fresh approved-repair evidence directory.

## Executed outcome: approved repair

The three approved owner patches are applied exactly. Successful computed results omit projection identity; request query discriminators, schemas, persisted projection identities, scoring, authorization, deadlines, incomplete File coverage and no-selection behavior remain unchanged. The additional supported read assertions cover list/open metadata, three real catalog refusals, Production-configured receivers and credential-free physical snapshot digests.

Fresh current Builds producer mappings were generated independently for Projects and Conversations in command-owned disposable checkouts. All emitted environment values were applied unchanged. All 12 direct checkouts match their root gitlinks; no shared Parties build or edit occurred. Current source warnings remain visible and package identity guards remain unchanged.

Final Projects full results: Contracts 230/230, domain 684/684, Server 1309/1309, all without failures or skips. The enforced gateway/owner/Dapr storage case passes across six catalog routes, including unchanged scores, explicit unsupported Files, safe refusals, missing signed surface claims and zero advisory writes. The final 115-case read/authority lane passes and records equal physical byte/ETag digests for all three catalog refusals. Two existing SDK bootstrap guard regression cases pass. Owner focused cases pass: Conversations advisory/strict/scoring-label 119/119 and Folders owner-read 30/30.

Full affected-project acceptance remains incomplete: Conversations full is 1429/1508 with 79 failures; Folders full is 1245/1283 with 38 failures, all without skips. The initial Folders standalone source build fails CS1704 due its existing mixed source/package Commons.UniqueIds graph; a narrow no-restore test-project build against exclusively fresh isolated dependencies passes without forcing a Projects mapping onto the owner. The 74 Conversations projection failures require current committed receiver catalog enrollment in `ResolutionEnrollmentFixture`; five deletion-host cases require separate named-policy and invocation-transport enrollment. Folders' production `FoldersServerHostComposition` lacks the current HTTP endpoint-source/security composition required by its SDK endpoint mapping. These separate production/qualification changes were not applied to conceal the broad failures.

Receipt and exact command/source/generated-input/binary/XML mappings: `evidence/6-4-advisory-resolution-20261021/current-advisory-source-20261031/approved-repair-20261031T225439Z/README.md`. Original build/test failures and all earlier evidence bytes remain. Production-authority validators and whitespace checks pass. Owned Redis/Dapr processes and listeners are removed; shared Parties inputs/build outputs, real repository indexes/refs/gitlinks and canonical Story 6.4/sprint bytes remain unchanged. Producer security, Platform publication, deployed policy/runtime, consumer adoption and the Parties publication hold remain independent gates; the canonical story and sprint remain in-progress.



Owner qualification is now governed by [the approved continuation specification](spec-6-4-owner-host-qualification.md). The earlier source-query repair and its original results remain preserved.

## Executed successor: owner-host qualification

The approved owner continuation now passes all affected full projects: Projects Contracts 230/230, core 684/684, Server 1309/1309; Conversations Server 1522/1522; Folders Server 1287/1287 (5032 total, no failures/skips/errors). SDK security/catalog/redirect guards remain active, including eleven persisted-state authority controls, an actual redirect control, original participant CLR/default-policy compatibility, all three Folders routing modes and the signed process/replay fixture. The final gateway run repeats all six query routes and three real-Dapr catalog refusals with fourteen unchanged physical byte/ETag digest observations per refusal.

Current receipt and exact selected evidence: `evidence/6-4-advisory-resolution-20261021/owner-host-qualification-20261101/README.md`. Earlier repair/source/full failures are retained. Folders uses its documented global Debug/source properties with full dependency rebuilding and an empty mapping hash; plain Debug without those inputs is not the selected source environment. Both production-authority validators, owning whitespace and preservation checks pass. Canonical Story 6.4/sprint remain in-progress; no publication/deployment/consumer acceptance or release of the Parties publication hold is implied.

## Post-review source successor

The [parent review verification](evidence/6-4-advisory-resolution-20261021/owner-host-review-verification-20261101/README.md) passes all five full affected suites (5,047 cases) and 26 additional probe/operations contract cases, with zero failures/skips/errors/not-run. Review patches retain the signed gateway/store behavior, computed-query metadata fix and API compatibility. Real catalog-observer/adoption and the SDK OTLP synchronous-send issue remain deferred and are not deployment or delivery acceptance. Main Story 6.4/sprint remain in-progress; no real Git mutations or publication occurred.
