# Story 6.4 consumer-pin and current-source checkpoint — 2026-10-28

The remaining local Conversations release-manifest update is implemented: all four `Hexalith.Conversations.Contracts` constraints in `tools/release-packages.json` now select 1.3.1. The existing Builds catalog already selected 1.3.1. A structural comparison proves these four values are the only manifest changes. This is local consumer metadata preparation; actual current consumer package-graph acceptance remains unfinished. Story 6.4 remains in progress.

This pass started from clean Projects `22c99ad1ae0aa88cd563f2d85346c9d680acc2ca`. The specification's canonical baseline `83781b31e06214ad6da788c6535f9044df06ed5e` and frontmatter are unchanged. `source-audit.json` separately records this execution baseline, every current direct owner revision/index fingerprint and 62 source/configuration hashes. The complete original specification byte prefix and frozen block are preserved; the frozen block SHA-256 remains `78e8a29557f949b064db4111f8d487ff4c4d15f3f7d57284e72905f797b9a7df`.

## Consumer package evidence

The retained official October 28 release response identifies non-draft, non-prerelease Conversations 1.3.1 published at `2026-10-28T03:08:07Z`, with source `986875bfcb5471953378b600710b5d859f3eaba4`. The prior receipt's integrity manifest verifies the exact release/tag response bytes; `consumer-pin-decision.json` links and hashes those retained responses. This run did not refresh remote metadata or dispatch publication.

Both cached Client and Contracts 1.3.1 archives contain that exact repository commit. Their SHA-512 values match the adjacent restored-package checksums, and their NuGet.org repository signatures pass `dotnet nuget verify --all` with `NUGET_CERT_REVOCATION_MODE=offline`. `local-conversations-131-packages.json` records their paths, byte sizes and SHA-256 values. The command and complete signature output are retained under `cached-conversations-signatures-final`. This verifies the selected cached package identities and local signatures; it is not fresh feed/revocation checking, all-package/symbol release qualification, or a current consumer build.

## Executed current checks

| Lane | Current result |
| --- | --- |
| Actual mapped Conversations.Server.Tests Debug build | Pass, zero warnings/errors |
| Six focused advisory/strict/registration/projection/recovery owner classes | 243 unique cases passed; zero failures/errors/skips/not-run |
| Release-package tools using the updated manifest | 19 tests passed |
| Production-authority planning guard | 37 tests passed |
| Story and index authority validators | Both pass |
| Current Projects.Server.Tests source build | Blocked before compilation, four required Conversations-mapping errors |
| Narrower current Projects.Contracts.Tests source build | Blocked before compilation, one required Conversations-mapping error |
| Current Projects.Server.Tests with the actual Conversations mapping | Correct refusal, HXW006 for unmapped Projects.Server |
| Package preparation | Exit 2, HXW011 and HXW009 |

The current total is **299 distinct passing tests**. Raw owner XML is [owner-focused.xml](owner-focused.xml). `results.json` retains counts, and `commands.json` retains all fourteen exact argument arrays, working directories, explicit producer environments, UTC start/finish times and exit codes; each command has separate stdout/stderr in `logs/`. `run.py` is the subprocess recorder. Preparatory inspection/file-copy/render invocations are not reconstructed as recorded subprocesses; no complete outer-shell/environment transcript is claimed. Two early duplicate preparation/signature observations were superseded by the fully ledgered final invocations; they contribute no additional acceptance count.

From the actual Conversations checkout, the successful current preparation and build are:

```text
dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace /tmp/story64-continuation-20261028-6763s6on/source --output json
dotnet build tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
```

The owner build uses all seven exact producer environment values from `source-prepare.stdout`. `owner-source-mapping/` retains the emitted files unchanged. The successful owner build/243-case run preceded the intentionally failed root builds; no stale root assembly was executed.

The actual root failures were:

```text
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
```

Both exit 1: `Conversations requires the Platform tool-selected source mapping; validation does not resolve dependency mappings.` Repeating the Server command with the actual Conversations producer environment exits 1 with HXW006: Projects.Server is outside its active module and mapped source roots. No producer gate or dependency root was overridden.

From the actual Conversations checkout, the package refusal is:

```text
dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode package --workspace /tmp/story64-continuation-20261028-6763s6on/package --output json
```

It exits 2. HXW011 names unreleased local exact Platform identity `fcee54288c75feaadd673e0484da1e5b5d742cc8`; HXW009 names its committed Builds pin `81ab049208678bd6616828824729c0ddac02f7d1` versus current umbrella/index/checkout `2574bf2f3da5367d8801f0d7661d66155e5d90b0`. This is a local qualification refusal, not proof that no new official release exists. Current Commons matches its root gitlink; the earlier HXW004 mismatch is superseded.

## Isolated enrollment and remaining acceptance

The separately approved [root-enrollment preparation](../projects-root-enrollment-20261028/README.md) remains isolated. Its existing read-only `verify-evidence.py --live` passes, and a fresh public producer invocation against its captured real declaration also succeeds. `isolated-candidate-source-mapping/` retains that new output. The mapping is not installed in the original root.

Its 175 passing cases and actual gateway/store lane are retained **historical candidate evidence**, not additional current-root cases. All selected direct owner revisions match the current owners except its deliberate qualified Builds release `16ff70a2321ad576faaac3fe53088c2f1bdacae3`; current Builds is `2574bf2f3da5367d8801f0d7661d66155e5d90b0`. Its reviewed Contracts project metadata also differs from current root `dd2b871bac16fbba62703a14ab32d9ba9b579c8f`. No candidate assembly was run or rebuilt during this pass, and no claim is made that it verifies the current original checkout.

| Frozen matrix row | Current checkpoint scope |
| --- | --- |
| Advisory | Current owner metadata tests pass; root query composition not executed |
| Protected | Current owner disclosure/identity controls pass; root candidate filtering not executed |
| Order/archive | Current root scoring/archive behavior not executed |
| Bounded | Current root attachment/fan-out/deadline behavior not executed |
| Degraded | Current owner malformed/pending/unavailable controls pass; root incomplete coverage not executed |
| Revoked | Current owner reauthorization controls pass; root recomposition not executed |
| Effects | Current owner guarded metadata-store controls pass; no fresh signed gateway/store comparison or confirmation run |

The next reviewable action is to reconcile the prepared declaration with current Projects reference metadata and advance it beyond the explicitly isolated preparation scope, then build through its actual public producer environment. Package delivery separately requires a qualified exact Platform/Builds tuple and complete consumer/package verification. The existing root-enrollment patch is preserved, not applied over the later Contracts change. File mapping remains unsupported/incomplete; no full File, root advisory-matrix, live OIDC/AppHost, CI, deployed runtime, or production release acceptance is claimed.

Only four local manifest values, an appended main-spec checkpoint and this receipt changed. No index, ref, accepted tool version, nested submodule, remote, publication, runtime or deployment was changed. `verify-checkpoint.py` performs read-only receipt/source/prefix checks; `SHA256SUMS.json` binds retained files and byte sizes. It is an integrity inventory, not an external signature or release authority.
