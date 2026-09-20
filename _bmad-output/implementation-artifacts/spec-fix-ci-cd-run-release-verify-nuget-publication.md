---
title: 'Repair CI/CD and publish the first Projects release'
type: 'bugfix'
created: '2026-09-19'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '3670e93a22190129997d9cc9e625d1282c32839e'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-fix-ci-cd-2.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current `main` cannot obtain the green CI required by Release: the first gate has 25 BMAD Loop relay failures, and package-only restore is blocked because four pinned Conversations/Folders `1.0.0` dependencies are absent from NuGet.org. No Projects package, release configuration, or `production` environment exists.

**Approach:** Restore the relay's previously tested portable filename validation, make every prerequisite package available through its owning repository's guarded release path, prove Projects in Release/package-only mode, then push the reviewed fix, wait for exact-source CI, temporarily authorize publication, dispatch the manual release, and verify the immutable outputs before refreezing.

**Decision:** Execute the recommended end-to-end chain: repair and release Conversations, then Folders, then Projects. This authorizes the irreversible publication of their complete manifest inventories: 14 NuGet packages plus matching tags and GitHub releases across the three repositories.

**Expanded decision (2026-09-19):** Also repair and release FrontComposer `4.5.0`, update the Hexalith.Builds catalog to that exact package set, and prepare separate superseding evidence packets for the missing Conversations preservation evidence and Folders OQ3/PD10 v2 governance evidence. Historical and signed artifacts remain immutable; new approval records require explicit human review before publication.

## Boundaries & Constraints

**Always:** Work and commit from each owning repository; preserve user changes and gitlinks until the owning-repository commit is complete. Keep CI Release/package-only, retain tests and exact-source admission, validate Conventional Commits with pinned commitlint, publish only absent manifest inventories, configure `production` for `main`, enable publication only for each release window, and refreeze afterward.

**Never:** Initialize nested submodules; weaken tests, package gates, source freshness, package-only mode, or duplicate checks; publish outside guarded Release workflows; blind-retry partial publication; alter historical/signed evidence; or publish an occupied version.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Invalid relay component | Unsafe/oversized task ID or event name | Exit zero without creating an events directory or file | Silent no-op, preserving hook behavior |
| Valid first release | Exact green live `main`, absent `1.0.0` destinations, publication enabled | Manifest packages, symbols, `v1.0.0`, and GitHub release originate from the dispatched SHA | Stop before writes on any preflight mismatch |
| Frozen/stale source | Variable is not exact `true`, source moved, or exact CI proof is absent | No publication | Frozen run may skip; stale/invalid source fails |
| Partial publication | One immutable artifact exists before a later failure | Preserve logs and state; do not blind-rerun | Reconcile through a separately validated recovery path |

</frozen-after-approval>

## Code Map

- `.bmad-loop/bmad_loop_hook.py` -- installed hook relay; BMAD `6.12.0` update removed the tested filename-component guard added in `14dea93f`.
- `tests/tools/test_bmad_loop_hook.py` -- reproduces the live failure and defines accepted task/event grammar and boundary lengths; do not weaken.
- `.github/workflows/ci.yml` -- local policy, shared package-only CI, and generated-artifact gates.
- `Directory.Build.props`, `Directory.Packages.props`, `Hexalith.Projects.CI.slnx` -- enforce package-only CI; the four `1.0.0` pins currently resolve to NuGet 404.
- `.github/workflows/release.yml`, `release.config.cjs`, `tools/release-packages.json` -- exact-green-main manual release and authoritative five-package inventory.
- `references/Hexalith.Conversations/_bmad-output/implementation-artifacts/spec-publish-conversations-nuget-packages.md` -- existing four-package prerequisite release plan.
- `references/Hexalith.Folders/.github/workflows/{ci,release}.yml` and `references/Hexalith.Folders/_bmad-output/implementation-artifacts/spec-folders-ci-cd-reference-alignment.md` -- implemented five-package release path whose current CI remains red.
- `references/Hexalith.Folders/_bmad-output/implementation-artifacts/spec-1-17-generate-pd10-v2-relock-milestone.md` -- complete PD10 v2 candidate contract for the OQ3/A6b prerequisite; implement its candidate and deterministic conformance set without inferring the later approval.

## Tasks & Acceptance

**Execution:**
- [x] `.bmad-loop/bmad_loop_hook.py` -- restore bounded portable ASCII component validation before payload parsing or filesystem writes.
- [x] Prerequisite owning repositories -- make exact-source CI green, validate first-release candidates, publish manifest inventories at `1.0.0`, verify official NuGet/GitHub state, and refreeze.
- [x] Projects gates -- run workflow policy, package-only restore/build, manifest/consumer checks, and every configured test project individually; fix only newly exposed root-owned defects.
- [x] Projects Git/GitHub state -- validate/commit/push current `main`, wait for exact-source CI, configure the main-only environment, unfreeze, and dispatch once.
- [x] Release evidence -- monitor to terminal success, verify five Projects `1.0.0` NuGet packages and matching GitHub tag/release/source SHA, then refreeze and record links/results. Symbol packages are not part of the repository's authoritative five-package manifest or current pack configuration.

**Acceptance Criteria:**
- Given every unsafe relay component in the regression matrix, when the hook runs, then it exits successfully without any filesystem publication.
- Given prerequisite `1.0.0` dependencies are public, when Projects CI runs at the pushed `main` SHA, then all blocking jobs pass in Release/package-only mode.
- Given exact-source CI success and empty destinations, when Release is dispatched, then exactly the five manifest Projects packages publish at `1.0.0` and the matching tag/release targets that SHA.
- Given terminal release completion, when official NuGet and GitHub APIs are queried, then all expected artifacts are observable and publication is frozen again.

## Implementation Notes

- The implementation/review phase is local-only: prepare and verify changes without pushes or other remote mutations. Authorized GitHub delivery and package publication follow reviewed local completion.
- Restored the relay guard exactly from the previously tested implementation at `14dea93f`; all invalid components now return before JSON parsing or filesystem access, while the 160-character task and 64-character event boundaries remain valid.
- Conversations now has a locally verified four-package Release toolchain and package-only graph. Release remains fail-closed because three blocking preservation tests require `candidate-restore.log` (SHA-256 `1407c6c5837cdbe1a170e08a04b3d4d2b9cae2236883636781804c7edd6e7b00`, 2,902 bytes) and `conformance-build-remediated.log` (SHA-256 `36f09a1249f840ffe33cbc11897bf01758af76b4b78820501723632b7419afa3`, 6,198 bytes); neither byte stream exists in reachable Git history or the searched local filesystem.
- Folders now produces and validates a five-package `1.0.0` candidate with package-only consumers. Release remains fail-closed on missing OQ3/PD10 v2 approval evidence and requires FrontComposer `4.5.0` plus a Builds catalog update to resolve the package-mode duplicate-heading failures. FrontComposer/Builds changes and additional publication were not authorized by the frozen three-repository decision and were not performed.
- The superseding Conversations preservation packet adds the two previously missing byte streams without altering historical evidence. Exact-source Release run `35437945119` published the four-package `v1.0.0` inventory from `c98b37958c6ed49bc82badc701b5a3d4f9b76229`; the eight GitHub package/symbol assets and all four NuGet flat-container endpoints are present, and `HEXALITH_RELEASE_PUBLISH_ENABLED` is refrozen to `false`.
- Folders carries a separate draft PD10 v2 relock specification and digest-bound A7/A7b governance updates. Its focused governance lane remains correctly fail-closed because the current OQ3 matrix digest has no fresh Security/PM approval and the OQ4 planning evidence is absent; no A6b/OQ3/OQ4 approval was inferred.
- FrontComposer shared CI is green at `7028d2d9766560c28c523639de5cb1d4370bba72`, but supplemental Quality run `35453476895` is terminal red. Its authenticated application assertions all pass; the causal failure is the cleanup proof after the lone path-based `HexalithTenantsSample` resource rebuilds during `aspire start --no-build`, adding three Tenants assets graphs, six Hexalith package directories, five IdentityModel package directories, and Debug outputs after the ledger was sealed. A disposable exact-source repair restores `SuppressBuild => true` on that metadata type and adds a regression guard covering all seven path-based resources; the 68-test AppHost smoke suite passes there. The real clean diverged FrontComposer checkout remains untouched pending integration of that patch onto current `origin/main`. Version `4.5.0` remains absent from NuGet.org, so the Builds catalog, Folders release, Projects package-only build, and downstream releases must not advance yet.
- The preserved local repair is `_bmad-output/implementation-artifacts/evidence/frontcomposer-quality-35453476895-local.patch` (SHA-256 `131da095bcc90254b97c3d5668319be03fe5ebaca471609747bbf103ae7f750a`, 1,827 bytes). `git apply --check --reverse` succeeds against the tested disposable exact-source tree at `/tmp/frontcomposer-quality-fix.aiIzyk/repo`, proving the artifact exactly represents its two-file working diff. The remaining blocker is integration onto a clean branch from FrontComposer `origin/main` without mutating the preserved ahead-one/behind-seven checkout, followed by a complete AppHost restore/build and an exact-source Quality rerun; the isolated compile-level restore remained in dependency resolution for approximately three minutes and was canceled without a result.
- During the local audit, an external concurrent `/pushall` run created and pushed Projects commit `fe23c31d0019d9ca048a8af19d5c89782cc10255`; this workflow run did not initiate that remote mutation. Exact-source CI run `35454194613` proves the policy lane and restored relay are green, but the shared build cannot initialize `references/Hexalith.Builds` because gitlink `4f522a8caa62ad82584bdf56d54e16109b717b1c` is not present on its remote. The same parent commit also records the unpushed FrontComposer gitlink `367324b198a12b3ec76f863f2eca40dcbc1a3fd4`, so the parent must be repaired only after owning-repository commits are reachable.
- Human scope decision (2026-09-19): the user explicitly answered yes to expanding this task into implementation of the complete Folders PD10 v2 candidate and preparation of its fresh governance packet. This authorizes executing the separate Story 1.17 candidate specification and generating its final deterministic conformance set. It does not authorize fabricating A6b acceptance: Product, Architecture, and Security review remains a human checkpoint after the candidate bytes and digest are stable.
- The full Folders PD10 v2 candidate is now locally complete and non-routed. Independent audit added the missing typed CLI `77` value, replaced the abridged MCP extension with the complete 46-kind failure vocabulary, synchronized the canonical error catalog and CI lane inventory, and made every frozen matrix state/family an exact test denominator. The final 75-artifact manifest SHA-256 is `649ecfffd95b54ce086777496d612af2793e6b8d254d4985f35b0cbc85ae90ad`; candidate-set SHA-256 is `535c675159047c0ff325995d351dafaa99aac13a1a542ba07a150f9a26ae27ed`; authorization-matrix SHA-256 is `1d60f21874e0c2e4e44ebc839786d8f65e76ea56c748afb26376e5996cf0d7ef`. A6b human acceptance is still pending, so no Folders publication may proceed.
- Projects local Release/package-mode validation is green after normalizing the Folders client `Redaction` value across published-string and source-enum forms and refreshing the release manifest dependency pins. The Release build completes with zero warnings/errors, all eight configured test shards pass (1,852 tests), OpenAPI selection passes 101/101, all five `91.92.93-ci.1` candidate packages and isolated consumers validate, and workflow/release/relay gates pass.
- Revalidation on 2026-09-20 exposed one later catalog-alignment regression: the release manifest still expected FrontComposer `4.4.0` after the pinned Builds catalog advanced the actual package graph to published `4.5.0`. Updated all eight Contracts/Shell dependency assertions across the four affected Projects packages to `4.5.0`; the five-package candidate and every isolated real-API consumer then passed.
- Projects exact-source delivery repaired Folders permission argument forwarding, refreshed the .NET/Aspire/runtime qualification tuple, added a fail-closed clean-package redaction regression, and made G-6 evidence hashing portable across checkout line endings. The Builds portability correction is commit `2fba3497043fe5ffcfe4dc44c51a09eae9b950ab`; its CI and Commitlint runs are green.
- Stable `1.0.0` packing originally stopped on approved prerelease Fluent UI dependencies because `NU5104` inherited warnings-as-errors. Commit `c767d38d8ae76f9ad949965ac4870a842c699f9c` keeps the warning visible while excluding that single approved code from error promotion; the five-package dependency gate and isolated consumer builds then passed.
- Projects CI, Commitlint, and CodeQL are green for release source `c767d38d8ae76f9ad949965ac4870a842c699f9c`. Release run `35482414271` passed exact-source admission and Semantic Release, received five NuGet.org HTTP `201 Created` push receipts, created tag/release `v1.0.0` at that exact SHA, and uploaded all five primary packages as GitHub release assets. `HEXALITH_RELEASE_PUBLISH_ENABLED` was restored to `false` immediately afterward.
- NuGet.org warned that the publication API key expires in zero days. This release completed successfully, but the repository `NUGET_API_KEY` secret must be rotated before a subsequent stable release.

## Spec Change Log

- 2026-09-19: Restored and verified the root-owned BMAD relay filename guard. Recorded the exact upstream publication boundary; release-chain and remote-mutation tasks remain open.
- 2026-09-19: Recorded the verified Conversations `v1.0.0` publication/refreeze and the separate fail-closed Folders governance successor state. FrontComposer, Builds, Folders, and Projects release work remains open.
- 2026-09-19: Diagnosed FrontComposer Quality run `35453476895`, preserved and unit-verified the exact-source two-file repair as a standalone patch, and left the authoritative diverged checkout unchanged. Clean-branch integration, compile/live verification, and all downstream release work remain open.
- 2026-09-19: Recorded the user's explicit scope expansion to implement the complete Folders PD10 v2 candidate and prepare, but not self-approve, its A6b governance packet.
- 2026-09-19: Completed and independently audited the non-routed 75-artifact Folders PD10 v2 candidate; recorded its final digest and green local suites. Marked Projects local gates complete after the Release/package-only candidate graph and all 1,852 tests passed. A6b and remote releases remain open.
- 2026-09-20: Revalidated the current published prerequisite graph, corrected the Projects package manifest from FrontComposer `4.4.0` to `4.5.0`, and reconfirmed the complete local Release/package-only test and consumer gates. Remote CI and Projects publication remain open.
- 2026-09-20: Repaired and verified all exposed Projects/Builds CI defects, published Projects `v1.0.0` from exact green commit `c767d38d8ae76f9ad949965ac4870a842c699f9c` through Release run `35482414271`, and restored the publication freeze.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| EC-1 | low | reject | A 21-digit nanosecond epoch plus both documented maximum components would make the temporary filename 256 bytes, but that epoch is millennia away; changing the current portable limits for an unreachable everyday case is disproportionate. |
| EC-2 | medium | defer | Both source mode and official `Hexalith.Folders.Client` `1.0.0` use the generated enum whose zero value is `Not_redacted`, so omitted JSON cannot be distinguished from an explicit safe value after deserialization; correcting presence semantics belongs to Folders. |
| EC-3 | false | reject | Case-insensitive comparison still accepts only the single semantic value `not_redacted`; casing variants are not unknown redaction states, while every other string remains denied. |
| BH-1 | medium | defer | Confirmed with the current generated Folders source client: an omitted non-nullable enum member takes the zero `Not_redacted` value. This is the same upstream source-contract limitation as EC-2. |
| BH-2 | false | reject | Debug intentionally consumes current sibling source while Release/CI consumes immutable packages. Both generated clients expose the same enum shape, and no bad production outcome beyond the separately logged omitted-enum case was demonstrated. |
| BH-3 | medium | patch | The adapter had only the canonical `not_redacted` positive case. Added `ValidateLink_RedactedMetadata_FailsClosedAsRedacted`, which proves an explicit disallowed enum value is denied; a real `redacted` wire value also fails generated-client deserialization. |
| BH-4 | false | reject | The Builds catalog already omitted `Hexalith.Conversations.Client` at the spec baseline, and the root intentionally removes/re-adds all four prerequisite pins as one documented local set. The current package-only graph restores and builds successfully. |
| BH-5 | low | reject | The preserved patch is dated diagnostic evidence and is not consumed by tooling; the pinned FrontComposer history already contains its fix. Removing the historical artifact and rewriting its narrative offers negligible operational benefit. |
| BH-6 | low | reject | The notes are a dated append-only investigation history and later entries record the superseding published state. Rewriting the spec history is the proposed fix, which review policy rejects. |
| BH-7 | low | reject | The initial frozen Problem statement imprecisely says release configuration was absent even though the baseline already had release files. It is historical intent text, and its only fix would edit the frozen spec rather than runtime behavior. |
| BH-8 | low | reject | The local candidate used committed `HEAD` as package repository metadata while the manifest correction was uncommitted, so the note is not immutable exact-tree evidence. The candidate was never published, and delivery revalidates the committed exact source; the proposed correction is only to the spec claim. |
| BH-9 | false | reject | Remote CI, release, NuGet, tag, source-SHA, and refreeze evidence are explicitly unchecked delivery tasks; placeholder verification commands do not claim that those operations have already succeeded. |
| BH-10 | low | reject | The disposable-checkout proof was historical and its exact patch is tracked; the now-pinned FrontComposer commit contains the fix. No current CI or release path depends on the temporary directory. |
| BH-11 | false | reject | The Commons/EventStore gitlinks were committed before this resumed patch, are reachable from their remotes, and the complete current package-only build/test graph passes. The reviewer identified no failure caused by either pin. |
| VG-1 | medium | patch | Repository-wide test tracing confirmed no unacceptable metadata value reached the changed normalization. Added and ran the focused redacted-metadata regression against the generated client. |
| VG-O1 | medium | defer | Independently confirms EC-2/BH-1: omitted redaction data defaults to the accepted zero enum value in source and official-package clients. Explicit disallowed values fail closed; the generated contract must gain presence/unknown-state semantics upstream. |

## Design Notes

The dependency releases are ordered prerequisites, not a source-mode fallback: Projects directly consumes the four absent packages, and its candidate consumer gate independently restores `Hexalith.Conversations.Contracts` from NuGet.org. The release workflow otherwise completes as a frozen no-op until the repository variable is explicitly enabled.

## Verification

**Commands:**
- `python3 -m unittest tests/tools/test_bmad_loop_hook.py -v` -- all relay cases pass.
- `python3 -m unittest tests/tools/test_release_package_tools.py -v && pwsh ./tests/tools/run-ci-workflow-gates.ps1` -- local CI/release policy gates pass.
- `CI=true dotnet restore Hexalith.Projects.CI.slnx -p:Configuration=Release -m:1 && CI=true dotnet build Hexalith.Projects.CI.slnx --no-restore --configuration Release -warnaserror -m:1` -- package-only graph succeeds.
- `dotnet test <each configured test project> --configuration Release --no-build` -- every blocking shard passes individually.
- `gh run watch <run-id> --exit-status` and NuGet flat-container/registration checks -- CI/Release succeed and exact artifacts are public.

**Observed 2026-09-19:**
- Relay suite: 11/11 test methods pass, including all 25 unsafe task/event cases and both valid maximum lengths.
- Release-package fixtures: 18/18 pass; CI workflow policy gate passes; relay diff passes `git diff --check`.
- Clean-cache package-only restore stops only at `NU1101` for `Hexalith.Conversations.Client`, `Hexalith.Conversations.Contracts`, `Hexalith.Folders.Client`, and `Hexalith.Folders.Contracts` `1.0.0`; build and test shards cannot run beyond that boundary.
- No push, environment mutation, release dispatch, or package publication was performed during the local-only implementation phase.
- A later clean-cache restore resolves both Conversations packages and now stops only at `NU1101` for `Hexalith.Folders.Client` and `Hexalith.Folders.Contracts` `1.0.0`.
- Official-state verification finds all four Conversations `1.0.0` packages, all four symbol assets, tag `v1.0.0` at `c98b37958c6ed49bc82badc701b5a3d4f9b76229`, and the publication variable set back to `false`.
- Folders `GovernanceCompletenessGateTests`: 20/22 pass; the two failures are the expected stale OQ3 approval digest and absent OQ4 planning-evidence guard. Folders `1.0.0`, FrontComposer `4.5.0`, and Projects `1.0.0` remain absent from NuGet.org.
- Exact-source Folders CI run `35453588021` at `23c3e4590af6644f9b837657408a505524903764` confirms those governance failures and the 36/63 published-FrontComposer duplicate-heading failures; its separate contract-spine run `35453587576` also remains red.
- Exact-source FrontComposer Quality run `35453476895` fails only after the authenticated AppHost smoke completes its health, command submit/status, handler-computed query, and SignalR assertions. The cleanup reports `apphost.cleanup.incomplete`: `Hexalith.Tenants.Sample`, `Hexalith.Tenants.Client`, and `Hexalith.Tenants.Contracts` assets/output trees plus their newly restored packages differ from the pre-start binding. Quarantine Summary and Coverage Summary are downstream failure aftermath, not independent causes.
- Disposable exact-source FrontComposer verification: `python3 -m unittest tests.eng.test_pact_provider_apphost_smoke` passes 68/68 after initializing only the six pinned top-level source submodules used by the suite; the focused new regression passes independently; both the two-file diff and the preserved patch pass whitespace/applicability checks. `dotnet-inspect` against `Aspire.Hosting` `13.5.4` confirms that `IProjectMetadata.SuppressBuild` controls whether Aspire builds the project before running it.
- Projects exact-source CI run `35454194613` at concurrent parent commit `fe23c31d0019d9ca048a8af19d5c89782cc10255`: workflow policy (including the relay suite) passes; shared CI stops at root-submodule initialization because Builds commit `4f522a8caa62ad82584bdf56d54e16109b717b1c` is not available from `Hexalith/Hexalith.Builds`.
- Folders final local verification: Release solution build succeeds with zero warnings/errors; Contracts 322/322, Server 728/728, Client 318/318, CLI 734/734, MCP 683/683, and UI 517/517 pass. The contract-spine gate passes 118 contract plus 24 client tests, and every lane of `run-contract-parity-ci-gates.ps1 -NoRestore` passes, including the new `pd10-v2-conformance-set` lane.
- Current Projects revalidation at `67c93b6fc1b7e88a6ae94b1cdaed42c4f5cef194`: package-only Release restore/build succeeds with zero warnings/errors; all eight configured projects pass 1,852/1,852 tests; relay/authority/release fixtures pass 36/36; workflow policy and FrontComposer inspection pass; the OpenAPI selector passes 101/101 from the completed Release build after a redundant gate rebuild stalled in dependency resolution and was canceled; five `91.92.93-ci.1` packages and all five isolated real-API consumers validate after the manifest correction.
- Projects release source `c767d38d8ae76f9ad949965ac4870a842c699f9c`: CI run `35482177927`, Commitlint run `35482177931`, and CodeQL run `35482177925` all completed successfully. Release run `35482414271` completed successfully; its log records five NuGet.org HTTP `201 Created` responses and five successful push receipts. GitHub release `v1.0.0` is public, contains all five primary `.nupkg` assets, and its tag resolves to the exact source SHA. The repository publication variable is `false` after the release window.
- All five NuGet.org flat-container indexes expose `1.0.0`. Fresh public downloads pass the repository manifest validator for exact IDs, dependency ranges, version, and repository commit `c767d38d8ae76f9ad949965ac4870a842c699f9c`; `dotnet nuget verify --all` validates the NuGet.org repository signature on every package. Download SHA-256 values are: Projects `95de9ebced081ed5bb95faac7ae12221c3913a190f4bf274e360472d505cdbbb`, Client `345bf5f4233a20ced6ec354801e343219d8c918e1c4b1072afd729d6b09b5dbd`, Contracts `51f84fbb2bd90a087a2f5c5edd144e6a0cf95341fc3e0361e7126c02ea578785`, ServiceDefaults `df96d5fae87f085404209dc99f0ba20f04f451748163ca8365b23368642e9c73`, and Testing `fc5be1ac28b2e1225e4243b17953869bca573a2e9c29ce120b81ac1f5b43796f`.
