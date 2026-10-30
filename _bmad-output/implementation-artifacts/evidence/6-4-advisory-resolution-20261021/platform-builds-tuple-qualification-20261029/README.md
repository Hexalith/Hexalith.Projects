# Story 6.4 Platform/Builds tuple qualification preparation — 2026-10-29

Select **Builds/tool 4.38.3**, exact source **`d1931b1e803144048d927fff16cb8119afde541e`**, as the proposed correction. Its published identity, signed tool archive, producer qualification and declared dependency compatibility verify. **No published tuple qualifies the current Platform checkout for acceptance.** Platform integration/CI, publication and consumer qualification remain open. Story 6.4 stays **in-progress**.

The existing [analyzer repair receipt](../platform-analyzer-fixes-20261029/README.md), four local analyzer edits, main story/sprint bytes and historical evidence are preserved. Only this new receipt and its scoped preparation spec are added. Both proposed patches remain unapplied; no actual dependency version, gitlink, HEAD or index is changed.

## Identities that must not be conflated

| Role | Exact Builds source | EventStore | Tenants |
| --- | --- | --- | --- |
| Platform HEAD's recorded gitlink | `e129a29e6c6e2f8a200581e09b861d75a9f3a7b8` | 3.128.3 | 5.11.0 |
| Prepared tool 4.38.2 | `2574bf2f3da5367d8801f0d7661d66155e5d90b0` | 3.128.4 | 5.11.0 |
| Selected published tool/catalog 4.38.3 | `d1931b1e803144048d927fff16cb8119afde541e` | 3.128.4 | 5.11.1 |
| Actual sibling source and umbrella HEAD/index gitlink | `e909ffd9fe9a6097e36d932010b983d029396968` | 3.128.4 | 5.11.1 |

Platform is `494ecb32802d1454c15704066487fa206f5bcec0`, with the four uncommitted test corrections. Projects is `0338d6ebf9f1be1bd38a8745759984d2066da5e7`. The old two catalog trees allow a conditional `CatalogCandidateTenantsVersion` override; 4.38.3 removes that override and selects published 5.11.1 directly. No override was supplied here.

The selected and sibling catalog Git blobs have SHA-256 `501d72650e3a42afc8fde0345a69208669e7fabdb98b6300f20afca07585d405`. This hashes Git blob bytes; CRLF working-tree hashes can differ. Sibling source adds six files/252 lines beyond the published source, including CI and historical toolchain annotation changes. Matching catalog bytes do not make it the published source.

Other selected versions include Conversations 1.3.1, FrontComposer 4.7.1, Commons 2.30.3, PolymorphicSerializations 1.19.5, Folders 1.3.2, McpCli 1.1.0, Memories 3.0.3, Parties 2.0.0 and Projects 1.2.0. [tuple-identity.json](tuple-identity.json) retains the full evaluated package map, conditional declarations, runtime catalog, all source comparisons and exact Git command outputs. The Folders.Aspire exception remains Toolkit 13.0.0; the neutral Platform selection is 13.6.2-beta.959.

## Official publication verification

The fresh [official Builds 4.38.3 release](https://github.com/Hexalith/Hexalith.Builds/releases/tag/v4.38.3) was published 2026-10-28T11:53:53Z. The tag resolves directly to the selected full source above. Its exact-source [CI run 40669428503](https://github.com/Hexalith/Hexalith.Builds/actions/runs/40669428503) and [Release run 40673566658](https://github.com/Hexalith/Hexalith.Builds/actions/runs/40673566658) succeeded. Python contracts, CodeQL, build/test, source verification and release jobs succeeded; Dependency review was skipped for this event. Raw API responses are retained as `github-*.json`.

The official inventory's digest and size match the release asset: SHA-256 `2421408b564af44ba75a83670aee147d21347d09ed5b823d091c24a6dbc29b5e`, 74,432 bytes. It records Release, official package build, executed source validation and controls, clean exact source, release eligibility and no ineligibility reasons. This is producer evidence, not Platform integration evidence.

The freshly downloaded [NuGet tool 4.38.3](https://www.nuget.org/packages/Hexalith.Builds.Module.Cli/4.38.3) is 37,673,564 bytes; signed archive SHA-256 is `6c074186a9bbfb66c558fe5d1090e1e35fdd58577c0396266074bd1cea8919fd`. Its computed SHA-512 and size equal the official NuGet catalog, its NuSpec source commit equals the tag, and `dotnet nuget verify --all` exits 0 with a NuGet.org repository signature. No offline revocation override was supplied. The producer inventory describes the unsigned ZIP; that ZIP's hash/size differs from NuGet's signed ZIP, and no unsigned reconstruction is claimed.

Invoking the extracted official CLI reports `4.38.3+d1931b1e803144048d927fff16cb8119afde541e`. No tool installation or manifest update was performed. The actual Tooling assembly SHA-256 is `4e307577ae4fb02104914951a9905a1c9dd453447a80735443d24fbc83496823`. Its embedded runtime resource selects Aspire SDK/Hosting 13.6.4, Dapr SDK 1.19.0, Dapr CLI 1.18.2, runtime 1.18.7, Toolkit 13.6.2-beta.959, EventStore 3.128.4 and FrontComposer 4.7.1, with the exact Redis image digest in the identity report.

**The embedded resource contains no Tenants field.** Tenants is verified through the exact Git source catalog, fresh official source blob identity, isolated MSBuild package evaluation and official dependency metadata. All five [Tenants 5.11.1](https://github.com/Hexalith/Hexalith.Tenants/releases/tag/v5.11.1) packages have source `db83309146816602033320270f10b1cea28d3100`, matching its official tag. Tenants.Aspire requires EventStore.Aspire >=3.128.4 and Toolkit >=13.6.2-beta.959. Eleven observed dependency packages supply 196 net10.0 constraints: 195 have exact catalog selections and all accept them. One remains unresolved: FrontComposer.Shell requires FrontComposer.Contracts.UI >=4.7.1, but that ID has no exact selected catalog version. [dependency-publications.json](dependency-publications.json) and [dependency-range-final.json](dependency-range-final.json) retain every checked and unresolved row. The final verifier binds these summaries to raw registration/catalog hashes, requires the exact eleven package identities, recomputes the range results and checks coverage. This is metadata preflight, not a restored full graph, binary acceptance or live behavior proof.

## Concrete unapplied correction

[platform-tuple-correction.UNAPPLIED.patch](platform-tuple-correction.UNAPPLIED.patch) proposes exactly:

- Platform Builds gitlink: e129a29… → full selected d1931b1… source.
- `.config/dotnet-tools.json`: 4.38.2 → 4.38.3, preserving disabled roll-forward.
- `hexalith-platform.json`: floor `[4.38.3, 5.0.0-0)`.
- README: exact candidate source, Tenants 5.11.1 and pending qualification.

[platform-direct-apphost-alignment.UNAPPLIED.patch](platform-direct-apphost-alignment.UNAPPLIED.patch) separately proposes EventStore.Aspire and ServiceDefaults 3.128.0 → 3.128.4 and Toolkit 13.6.1-beta.935 → 13.6.2-beta.959 in `apphost.cs`. SDK 13.6.4 already agrees. This patch is required to include the direct file-host entry point in this tuple's qualification. The generated module-tool host and direct file host are distinct launch paths; qualifying one does not qualify the other.

Both patches pass `git apply --check` against current Platform. Their complete proposed file bytes are retained in `proposed-platform-files/`. The existing four analyzer changes remain a separate preserved patch and must be included in the recorded Platform candidate. The historical 4.38.2 gitlink patch is stale and remains untouched.

## Executed checks and current blockers

[COMMANDS.md](COMMANDS.md) gives exact argv, working directories, environments, UTC timing and exits; each command has raw `.stdout`, `.stderr` and `.command.json` files.

| Check | Result | Meaning |
| --- | --- | --- |
| Official publication/identity verifier | exit 0 | Published Builds candidate bindings verified |
| Signature, actual CLI version and embedded resource | exit 0 each | Official archive observed, no install |
| Exact source default/Folders/Contracts evaluations | exit 0 each | Declared package selection; no restore |
| 195 known dependency ranges | exit 0; 195 pass | Metadata compatibility only |
| Existing four analyzer regression classes | exit 0; 76/76 pass | Current built Debug source diagnostics |
| Existing inventory/checklist method | exit 0; 1/1 pass | Unobserved evidence remains pending |
| Existing owner-pin method | exit 1; 0/1 pass | Missing v4.38.2 tag in nested path's Git context |
| Actual public package preparation, extracted 4.38.3 | exit 2 | HXW011 twice, HXW009 once |
| Actual public package preparation, prepared 4.38.2 | exit 2 | Same three findings |
| Both unapplied patch checks; Platform/root whitespace | exit 0 each | Reviewable patches, current whitespace valid |

No new build was needed for evidence-only preparation. The retained prior Debug build passed with zero warnings/errors; these 77 passing cases rerun the existing built assembly. They use sibling source e909ffd… and do not establish an exact d1931b1… Platform build. Property evaluation confirms Debug, analyzers enabled, warnings as errors, source project references and the actual sibling Builds path. SDK observed from Projects is 10.0.403; no dependency restore was run.

The owner-pin failure is a release-tag-context failure before its final identity equality assertion. Independent Git reads prove the real recorded pin mismatch regardless. Do not initialize nested submodules, create substitute tags, change HEAD/index temporarily or weaken the assertion to make it pass. An initial mistyped inventory method selected zero tests with exit 0; that output is retained and earns no pass credit. The corrected exact method passes one case. The whole enrollment class was not run because other methods stage/commit fixture repositories.

Both package preparations use the actual Projects declaration and fresh scratch output. HXW011 reports (1) current Platform has no local exact release tag and (2) its checkout has the preserved analyzer edits. HXW009 reports umbrella Builds staged/checked out at e909ffd… while committed Platform pins e129a29…. No preparation mapping was accepted. Fresh official Platform refs contain only v1.0.0/v1.0.1; their peeled revisions and old catalog pins are recorded in the identity report. Neither names the current or proposed Platform candidate. Official Platform releases are empty and the current Platform SHA has zero observed workflow runs. Fetching old tags cannot publish the missing identity.

There is no tracked Platform `.github/workflows` directory. The owner must identify or establish a qualifying CI entry point. Current Projects CI workflow execution source `51af786cf156d2a3396dbd49f5e4898222e55c12` and release execution source `a07078ad74d3727bc5a6b6d85d47d56a6e5c9fec` are separate identities, not inferred to equal this catalog/tool source or silently replaced here.

Package-only owner CI has another unresolved configuration gate: Composition still references Builds.Tooling source unless `BuildsToolingAssemblyPath` is supplied, and Composition.Tests unconditionally references the sibling Builds.Module.EventStoreHost project as `buildsseed`. `UseHexalithProjectReferences=false` alone cannot remove these foreign source inputs. The supported Tooling override must point to the verified release DLL with its dependency closure; the host reference needs an owner-supported Release artifact/package binding and a reviewed configuration correction. The two tuple patches do not implement that binding. It must be resolved before selecting the final recorded candidate for package CI, then verified against the evaluated/restored graph.

The existing consumer acceptance manifest has **46 differences across 18 dependency IDs** against the selected catalog, including EventStore.Contracts 3.128.3 vs 3.128.4, FrontComposer Contracts/Shell 4.7.0 vs 4.7.1 and Microsoft packages 10.0.13 vs 10.0.14. [consumer-acceptance-inventory-drift.json](consumer-acceptance-inventory-drift.json) lists every comparison. A fresh restored per-package closure may reveal additional additions/removals; this static comparison is not a proposed transitive lock update.

Consumer evidence retention is also pending: the current package-consumer validation script deletes its temporary restored graphs and outputs. Final qualification needs an approved extension or equivalent probe that retains each exact per-package assets graph, sanitized restore/build logs and package hashes before cleanup, plus explicit symbol validation. No such option or completed acceptance is invented here.

## Next action and authorization

The precise next action is owner authorization to **apply the two reviewable Platform patches**, preserving the four analyzer edits, and to resolve the identified owner CI artifact-binding/configuration prerequisite. Then separately **record the resulting exact Platform candidate** and run its owner integration/CI plan. Applying these patches changes dependency/tool/gitlink versions and is outside this pass's authority. The additional CI binding needs its own reviewed owner configuration change. No future Platform revision or release version is invented. No publication or consumer adoption is authorized by this proposal.

[qualification-plan.md](qualification-plan.md) defines the required standalone owner environment, legitimate tag context, exact-candidate CI bindings, direct/generated launch coverage, publication boundary and consumer all-package/symbol checks. After verified Platform publication and separately approved umbrella adoption, use CLI 4.38.3 to rerun Projects package preparation; require HXW009/HXW011 to clear, refresh the acceptance inventory from actual restored package closures, then qualify every Projects package and symbol archive against the adopted exact source. Earlier runtime/store/full-suite evidence remains historical and cannot be relabelled for new inputs.

`preservation-before.json` hashes 114,238 original tracked/historical files and records all 13 repository HEAD/index identities. `preservation-result.json` records the final recheck. Story and sprint-state bytes remain unchanged; their old analyzer-error text is historical checkpoint text, superseded only by the additive analyzer receipt and this report.

Independent review raised nine preparation/evidence findings; all were checked, patched within this packet and confirmed closed by the reviewer. [review-triage.md](review-triage.md) records every finding and resolution. The reviewed verifier exits 0 with empty stderr. The scoped preparation spec is complete; main story 6.4 stays in progress. [qualification-handoff.json](qualification-handoff.json) provides machine-readable coordinates, patch hashes, blockers and authorizations. No Git recording occurred.
