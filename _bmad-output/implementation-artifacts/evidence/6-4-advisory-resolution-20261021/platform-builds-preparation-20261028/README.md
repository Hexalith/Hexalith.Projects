# Story 6.4 Platform/Builds package preparation — 2026-10-28

The supported correction is prepared locally in Platform; package preparation remains blocked by the committed owner identity and qualification gates. Builds requires no source correction. Story 6.4 remains in progress. No owner or umbrella index, commit, branch, push, publication or deployment was changed, and existing user changes were preserved.

This receipt supersedes the package identity and publication observations in [the initial October 28 handoff](../resumption-20261028/README.md). It does not replace its historical advisory test results with new acceptance evidence.

## Remote identity and owner pins

`commands.json` records exact argument arrays, working directories, explicit environments, exit codes, durations and UTC observation times. Each command retains separate stdout/stderr. The initial metadata snapshots and tag refresh establish:

| Selection | Exact revision or observation |
| --- | --- |
| Projects HEAD and index: Platform | `a5e1389fe2c97b14e8c697a2e54f45631d07ef95` |
| Projects HEAD and index: Builds | `1745ea0f09df9e518aeea3eeb1a7df836f67e09d` |
| That committed Platform identity's Builds pin | `51a71cb1cfe860d5fc135eb3b8521fd80c413810` |
| Current Platform checkout | `8de90958bc41b7fd01b7ed22b8ab364447a800e4` |
| Current Platform checkout's committed Builds pin | `9f85d8e66e5c16675cee8c224f1fd88430d96007` |
| Current Builds checkout | `abc0e1be8069d1c7a567515a13bb015adc407536`, with a pre-existing dirty catalog |
| Remote Platform `v1.0.0` peeled commit | `b742b1b7a496ac72d6961ca6db5213d3df42d234` |
| Remote Platform `v1.0.1` peeled commit | `99ed78996f4ac23b3851a0609640c179d8822aea` |

Both Platform release tags were **already local**. Fetching those two exact official tag refs nonrecursively changed no ref. Neither remote tag identifies the selected `a5e1389...` revision or the current `8de9095...` revision; the GitHub releases API returned an empty release list. Thus HXW011 is an **unreleased exact Platform identity**, not merely missing local tags. An ancestor release cannot qualify that identity. No tag was created or moved.

HXW009 independently reports the committed Platform pin `51a71cb...`, umbrella staged Builds `1745ea0...`, and dirty checkout `abc0e1b...`. Current Platform HEAD's pin `9f85d8e...` does not override the old committed identity. Checkout advancement also produces HXW008. The exact-identity, clean-catalog and index equality guards remain unchanged.

## Prepared correction and qualification limits

Platform's `.config/dotnet-tools.json` now prepares CLI **4.38.1**, and `hexalith-platform.json` prepares the matching range floor `[4.38.1, 5.0.0-0)`. The README describes the candidate and pending gates, with Aspire CLI 13.6.3 and Dapr CLI/runtime 1.18.2/1.18.7 from the actual released tool catalog. The direct file AppHost still selects Aspire 13.6.4; restoring it does not satisfy the tool's 13.6.3 prerequisites. This separate integration mismatch remains explicit and its pins were not changed. This is a local candidate, not accepted Platform integration.

The release is already selected by Conversations' local tool manifest and was inside Platform's prior accepted tool range. [Official Builds 4.38.1](https://github.com/Hexalith/Hexalith.Builds/releases/tag/v4.38.1) resolves to exact Git source `16ff70a2321ad576faaac3fe53088c2f1bdacae3`. Its retained official inventory reports package/source/control qualification passed. Exact-source [CI 40589495278](https://github.com/Hexalith/Hexalith.Builds/actions/runs/40589495278) and [release 40595436967](https://github.com/Hexalith/Hexalith.Builds/actions/runs/40595436967) succeeded. A later release attempt was canceled, and a separate SonarCloud analysis failed; neither is represented as a successful run.

A fresh official NuGet download equals the installed accepted archive byte-for-byte: signed SHA-256 `f5710df04af88744a48e73f51155dbe41c1e91489ca7cac92d2bb7f53948dff3`, 37,561,135 bytes. Its SHA-512/size match the fresh NuGet catalog, `dotnet nuget verify --all` exits 0, and the NuSpec repository commit equals the release tag. `official-tool-identity.json` and the actual embedded catalog retain these bindings. The qualified inventory records the unsigned package hash; it differs from the signed feed ZIP. No local reconstruction of that unsigned ZIP is claimed.

The complete owner correction also requires Platform's Builds gitlink to identify `16ff70a...`. [The exact gitlink patch](platform-builds-gitlink.UNAPPLIED.patch) is **unapplied**; `git apply --check --cached` succeeds without modifying the index. Recording it would require an owner commit, which the user prohibited. No moving branch, unqualified source version or dependency override was selected. Builds' existing catalog edits, including Conversations 1.3.1 and other version changes, were preserved byte-for-byte.

## Verification

- Builds Module.Tests Debug build: **zero warnings/errors**. Existing `PlatformIdentityTests`, `BuildPreparationTests` and `CatalogVersionTests`: **88 passed**, zero failures/errors/skips/not-run, with exact XML retained. These include valid tuples and rejection of stale/dirty/mismatched identities.
- Candidate Platform `dotnet tool restore` succeeds; its public version is `4.38.1+16ff70a2321ad576faaac3fe53088c2f1bdacae3`.
- Full Platform Composition.Tests Debug build exits 1 with three existing analyzer errors: CA2007 in `AgentsAdmissionCurrentBoundaryTests.cs:93`, CA1062 in `AgentsHostStartupBoundaryTests.cs:36`, and CA1062 in `AcceptedLaunchBoundaryTests.cs:27`. No analyzer was suppressed.
- A temporary narrow project compiles the **unchanged existing** `DomainEnrollmentKitTests.cs` against current source Tooling in Debug with zero warnings/errors. The inventory/checklist case passes. The owner-pin case fails: it looks for `v4.38.1` under the uninitialized nested Platform Builds path. No nested submodule was initialized and no stale test assembly was run. Separately, direct checks against the allowed sibling Builds repository prove the current committed owner pin `9f85d8e...` differs from release `16ff70a...`; the committed-pin gate remains unsatisfied. The two disposable harness files are retained as `.txt` evidence.
- Planning guard: **37 passed** (200 passing subtests). Combined with the focused lanes above: **126 passed and one owner-pin failure**, plus the separately blocked broad build. These results do not qualify the full Platform integration or Story acceptance.
- Semantic/source/index preservation verification passes; Platform whitespace check passes. `verification.json` retains the initial audit; `reviewed-source-baseline.json` binds the final source after review corrections. `reviewed-immutable-audit.stdout` independently compares those retained hashes, the original story prefix, complete HEAD/index snapshots, release source/asset qualification, exact command exits/diagnostic instances and the owner test's specific missing-tag failure. The verifier is read-only; it never replaces its baselines. The frozen advisory block remains SHA-256 `78e8a29557f949b064db4111f8d487ff4c4d15f3f7d57284e72905f797b9a7df`.

The initial package invocation used a nonexistent output parent and returned HXW007; it is retained as an input failure. Its corrected retry, tag-refresh retry and final retry are separately recorded. The initial disposable harness restore returned NU1015; the corrected harness uses automatic central package import and retains both attempts.

## Final preparation and next blockers

From Conversations, the final public request was:

```text
dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode package --workspace /tmp/story64-platform-builds-20261028/package-reviewed --output json
```

It exits **2** with **HXW008, HXW011 and HXW009**. HXW011 now also records Platform's intentional uncommitted candidate configuration. Exact diagnostics are in `package-reviewed.stdout`. No package preparation success is claimed.

The immediate package blocker is still a recorded, integration-qualified, released exact Platform identity with the matching Builds release pin and a consistent clean consumer composition. The candidate cannot satisfy that committed-identity gate without the separately prohibited owner recording/publication and later consumer adoption. No approval was inferred or requested to cross that boundary.

The `source-reviewed` source fallback was also rerun with the accepted public command and a fresh workspace. It exits **2**, first **HXW004**: Commons checkout `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` differs from umbrella staged `1663028fb6d9f46a0bf17919edeb21bb99be1587`. This is the next concrete current-source verification blocker after the package boundary; neither staging nor checkout rollback was used to conceal it. The broader Platform analyzer failures and supported Projects root mapping remain additional verification work.

[Conversations 1.3.1](https://github.com/Hexalith/Hexalith.Conversations/releases/tag/v1.3.1) was published externally at `2026-10-28T03:08:07Z`; the previous handoff's destination-absence observation is historical. This session did not publish it or perform full official package acceptance. Projects `tools/release-packages.json` still selects Conversations 1.3.0; actual official package/consumer verification remains unfinished. No current full advisory suite, signed gateway/store, runtime or deployed acceptance is claimed.

## Review and repeatable audit

The configured Blind Hunter review returned eight findings, all verified and patched: candidate Aspire/Dapr setup consistency, the old-tool range qualification, immutable source/receipt comparison, exact release qualification and official asset digest, portable archive input, recorded exits/distinct HXW011 instances, and the specific owner-pin failure cause. Full triage is in `review-triage.md`. There are no new deferred review items. Existing integration, source-composition and delivery blockers above remain open.

The initial verifier and its observations are retained as `verify-preparation.initial.py.txt`, `semantic-and-preservation-verification.stdout`, `semantic-final-verification.stdout` and `verification.json`; they precede the review fixes and are not the final audit authority. The final read-only verifier resolves its workspace from this receipt and needs no original NuGet cache. Run `python3 <receipt>/verify-preparation.py` for retained evidence/source checks. To reverify actual signed ZIP bytes, redownload the official archive using the recorded `builds-4381-official-nupkg` URL and pass `--archive <path-to-nupkg>`. The JSON explicitly states whether archive bytes were reverified. An absent archive is never represented as a fresh payload check.

Large raw command streams are losslessly compressed with gzip; `commands.json` names the actual stored stdout/stderr paths. `SHA256SUMS.json` binds the final evidence files. Repository-qualified and captured synthetic-fixture checks remain distinct from Platform integration acceptance.

The original reviewer independently confirmed all eight findings closed and reran the signed-archive audit with exit 0. Parent controls prove changed source-baseline hashes, changed official inventory bytes and an unrelated failed owner assertion are rejected; positive offline checks pass and every audit leaves its input evidence unchanged. The original story was updated by appending this checkpoint only; the final audit verifies its entire pre-existing byte prefix.
