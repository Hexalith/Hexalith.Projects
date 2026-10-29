# Current Projects root-enrollment candidate — 2026-10-28

The updated candidate is prepared at `/tmp/story64-projects-current-enrollment-20261028-krzrd_eu/projects`. Its real Projects declaration validates, public source preparation succeeds, and both Contracts.Tests and Server.Tests Debug builds pass with zero warnings/errors. Current reference metadata needs no correction. The exact [two-file unapplied patch](projects-current-enrollment.UNAPPLIED.patch) adds only `module/hexalith.module-manifest.v2.json` and `module/README.md`; read-only `git apply --check` succeeds against the original working tree. It is not installed there.

The focused current candidate ran **181 passing cases and one failing prerequisite case**: Contracts 8/8; Server 173/174. The failure is `FAIL_SKIP` for `SupportedProjectReadHttpTests.ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart`, which requires Dapr persistence. It is retained as a failure, not a skipped/pass result. Attempts to supply that existing local fixture failed before test execution: Docker published-port forwarding returned HTTP 500, and the approved host-network fallback could not expose its Redis loopback listener to the local Dapr process. No source or assertion was changed to avoid these failures. Story 6.4 remains in progress.

## Exact inputs and bounded candidate

The original root and candidate baseline are `22c99ad1ae0aa88cd563f2d85346c9d680acc2ca`. All twelve isolated direct owners select their current original owner HEADs and match the original root gitlinks; `selected-inputs.json` records every full identifier. In particular, current Builds is `2574bf2f3da5367d8801f0d7661d66155e5d90b0`, unlike the earlier candidate's deliberate released Builds source. There are no original or disposable gitlink changes and no nested initialization. Clones, detached checkouts and direct Git-directory absorption are local isolated preparation operations; no remote operation or delivery Git action ran.

Only the previously reviewed real declaration and its documentation were copied from [the older isolated preparation](../projects-root-enrollment-20261028/README.md). Their prior receipt hashes were checked. The declaration is byte-identical to the reviewed version; the README changes only its source baseline to the current full root identifier. No prior candidate Contracts project was copied: this candidate keeps current root Contracts metadata from `dd2b871bac16fbba62703a14ab32d9ba9b579c8f`, which already satisfies the actual complete producer mapping. Original working-tree line endings may differ from canonical checkout endings; exact isolated source bytes are inventoried rather than represented as a raw-byte copy of every original file.

The original workspace's four already approved `tools/release-packages.json` Conversations 1.3.1 pin edits are explicitly copied and inventoried as the current-root overlay. They are excluded from the new two-file patch because they are already present in the original workspace. No unqualified package or accepted dependency/tool version was selected as a replacement.

The accepted public CLI reports `4.38.1+16ff70a2321ad576faaac3fe53088c2f1bdacae3`. The actual generated Projects mapping hash is `a1df40fbbd0e9c087bf1d3c7dea96c819487151ffe6c805593e0a2857445462a`; `generated-source-mapping/` and `generated-input-hashes.json` retain its files unchanged. Both builds use all seven emitted environment values without alteration. Producer preparation still warns HXW011 for exact Platform `fcee54288c75feaadd673e0484da1e5b5d742cc8` and HXW009 because that Platform pins Builds `81ab049208678bd6616828824729c0ddac02f7d1`, while the actual selected owner is `2574bf2f3da5367d8801f0d7661d66155e5d90b0`. Source success does not qualify this package tuple. Package preparation/release qualification was not repeated or bypassed in this bounded pass.

## Current execution

`commands.json` records 102 subprocesses with exact arguments, working directories, explicit/effective allowlisted build environments, UTC starts, durations, exit codes and separate stdout/stderr. No credential environment inventory is retained. Script creation/inspection/outer-shell invocations are not reconstructed as ledger entries. Preparation, focused execution, fallback and finalization scripts are retained for inspection; their unique output/workspace guards intentionally reject reuse.

From the isolated Conversations checkout:

```text
dotnet tool run hexalith-module -- validate --manifest /tmp/story64-projects-current-enrollment-20261028-krzrd_eu/projects/module/hexalith.module-manifest.v2.json --output json
dotnet tool run hexalith-module -- prepare-build --manifest /tmp/story64-projects-current-enrollment-20261028-krzrd_eu/projects/module/hexalith.module-manifest.v2.json --mode source --workspace /tmp/story64-projects-current-enrollment-20261028-krzrd_eu/prepared-source --output json
```

From the isolated Projects root, using all producer environment values:

```text
dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
```

Both exit 0 with zero warnings/errors. Individual freshly built xUnit assemblies were invoked directly with single-dash filters and `-failSkips`.

| Narrow lane | Result |
| --- | --- |
| ProjectAdvisoryContractTests | 8 passed |
| ProjectAdvisoryQueryTests; delegated owner/client registration; principal binding; tenant isolation; supported HTTP tests | 173 passed, 1 FAIL_SKIP failure |
| Existing signed gateway/store case | Not executed: fixture prerequisites failed before readiness |

Raw XML: [current-contracts-tests.xml](current-contracts-tests.xml), [current-server-tests.xml](current-server-tests.xml). `test-results.json` retains every exact case. `matrix-map.json` maps the seven advisory rows to these current cases and explicitly retains the missing fresh gateway/store proof. No earlier 175-case candidate receipt or 299-case shared-checkout receipt is recounted here.

## Fixture failures and cleanup

The original fixture used the exact cached qualified Redis image digest `docker.io/library/redis@sha256:ff02b58f971e7d7d156a1267e283fcbbeee91773b6aa36c49dac28ecfe28eadf`, with `--pull=never`, a unique command-owned name and a fresh loopback port. `current-runtime-runtime-redis-create` exits 125: Docker's `/forwards/expose` returned HTTP 500 while publishing `127.0.0.1:51739`. The failed name was confirmed absent.

The permitted installed-server probe exits 127: `redis-server` is unavailable. The approved fallback starts that same image with `--network host`, explicit fresh port 40621 and Redis `--bind 127.0.0.1`. The local Dapr 1.18.7 process then exits 1 before readiness: `statestore (state.redis/v1)` cannot connect to `127.0.0.1:40621` (connection refused). This is a local fixture network prerequisite, not an observed advisory implementation defect. `runtime-prerequisite-blocker.json` retains the structured diagnosis; complete runtime private logs remain outside the receipt. The fallback sidecar exited and its exact owned container was removed. The original and final full container-ID censuses match. No existing service was stopped or modified.

The next fixture prerequisite is a Redis endpoint reachable from the local Dapr process. Published-port forwarding must work, or an approved local fixture Redis must be available in that process's network namespace. No additional installation, runtime alternative, host migration or deployment was attempted.

## Preservation and next decision

`source-before.json` and `source-after.json` match for **20,108 actual isolated C#/build/configuration inputs**, plus identical selected owner states. `executables-before.json` and `executables-after.json` match for **667 compiled test/runtime files** across both test outputs. Source capture occurred after both builds and before test execution; it is not a claim of a pre-build file census. The actual selected canonical commits and recorded clean pre-overlay source states establish the build inputs separately.

`preservation-before.json` and `preservation-after.json` are byte-semantically equal: all original owner HEADs, branches, refs, raw index hashes, stage-zero entries, existing dirty/untracked bytes and nested markers are unchanged. The prior checkpoint, original specification bytes/frozen block, older isolated candidate and its evidence were not edited. New evidence is the sole original-root addition during this continuation. No original declaration, source metadata, index, ref, remote, tool pin or runtime was changed.

The concrete review decision is whether to apply the exact two-file declaration/README patch to the original root beyond the previously approved isolated scope. It now matches current source/reference metadata without a further source correction. Before claiming the complete advisory chain, restore the reachable local fixture prerequisite and rerun the failed persistence case and signed gateway/store lane. Exact Platform/Builds package qualification, consumer package-graph acceptance, CI and deployed acceptance remain separate. File mapping remains explicitly unsupported/incomplete. No full suite, fresh signed gateway/store or Story completion is claimed.

`SHA256SUMS.json` binds retained file hashes and sizes. It is a local integrity inventory, not an external signature or release acceptance.
