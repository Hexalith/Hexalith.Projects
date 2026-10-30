# Story 6.4 current local delivery gate — 2026-10-30

The advisory implementation is already present. This continuation prepares only
Conversations' local CLI pin **4.38.2 → 4.38.3** and its adjacent historical note,
as permitted by the specification when the prepared Platform floor requires it.
Platform now requires `[4.38.3, 5.0.0-0)`. Fresh public package preparation clears
**HXW010** and still refuses **HXW011/HXW009**. Story 6.4 stays **in-progress**.

The source implementation, .NET dependency versions, catalog, recorded Git pins,
existing evidence and all thirteen repository HEAD/index/ref identities are
preserved. No remote operation, staging, commit, branch, push, publication,
deployment or nested initialization occurred. Tool restore used a configuration
with every package source cleared; the consumer build used `--no-restore`.

## Current identities and exact blockers

| Input | Exact local identity |
| --- | --- |
| Projects HEAD | `29a61cb079328e6dffdafb77ff117f89d1930a1c` |
| Platform HEAD and root gitlink | `9f43cefe11bd246ce0de1cd0740261b557c0c7a0` |
| Platform's committed Builds pin | `d1931b1e803144048d927fff16cb8119afde541e` |
| Both prepared tools | `4.38.3+d1931b1e803144048d927fff16cb8119afde541e` |
| Root Builds HEAD/index/checkout | `e909ffd9fe9a6097e36d932010b983d029396968` |
| Conversations HEAD | `a40cfb3ed68f20201cb759f754270b83ea02efb3` |

The final actual command runs from Conversations' tool context with the original
workspace/index and a fresh, nonexistent output directory:

```text
dotnet tool run hexalith-module -- prepare-build --manifest /home/administrator/projects/hexalith/projects/module/hexalith.module-manifest.v2.json --mode package --workspace /tmp/story64-local-delivery-gate-20261030-n0ldp00f/prepared-public-package --output json
```

It exits **2** with exactly these diagnostics:

- **HXW011:** exact Platform `9f43cefe...` has no local SemVer release tag.
  No official release or remote tag observation was refreshed.
- **HXW009:** Platform pins Builds `d1931b1...`; the original umbrella index and
  checkout select `e909ffd...`. Matching catalog contents cannot substitute for
  exact source identity.

The initial Conversations 4.38.2 invocation also reports HXW010; its original
failed receipt is retained. The 4.38.3 control from Platform and the final
Conversations invocation both retain the two real identity blockers. No pin,
tag, alternate index or synthetic commit was used to conceal them.

## Executed checks

| Check | Result |
| --- | --- |
| Offline prepared-tool restore, version and preparation help | Pass |
| Fresh public source preparation before/after tool pin | Pass; explicit tuple warnings |
| Actual public package preparation after pin | Exit 2; HXW011 and HXW009 |
| Ordinary Projects.Server package Release build, `--no-restore` | Pass; zero warnings/errors |
| Existing release-package tooling tests | 19 passed |
| Existing production-authority planning guard | 37 passed |
| Story/index authority and owning whitespace checks | Pass |
| Original Git/source/evidence/spec preservation audit | Pass |

There are **56 current passing Python tests**, zero failures/skips. Commands,
working directories, explicit environment, UTC times, exits and raw streams are
in `commands.json` and the named command files. The two expected package
refusals before correction and the final refusal remain failures, not test
passes. No unaffected source/store suite was repeated.

`installed-tool-identity.json` proves that installed Tooling bytes equal the
retained signed 4.38.3 archive. That archive's SHA-256 remains
`6c074186a9bbfb66c558fe5d1090e1e35fdd58577c0396266074bd1cea8919fd`,
and its NuSpec names exact source `d1931b1...`. The prior official signature and
publication receipt remains historical; this check refreshes neither feed nor
revocation status. Offline restore used `offline.nuget.config.txt` with no feeds.

The complete cached consumer assets are retained compressed. They select
Conversations Client/Contracts **1.3.1 packages** and no external module source
projects. The successful Release command performs no restore and establishes
compilation against that cached closure, without fresh package/feed or complete
consumer qualification. Public source mapping is retained unchanged; no new
source build or mapped-suite acceptance is claimed.

## Superseded checkpoint and remaining work

The previous story comment's four Platform analyzer failures were resolved by
the retained [76-case analyzer receipt](../../platform-analyzer-fixes-20261029/README.md).
The later [owner CI receipt](../../platform-tuple-owner-ci-20261030/README.md)
records exact hosted Platform candidate `6a186754...` passing a package-only
Release preflight: 82 required cases, 14 controls and 27 restored graphs. That
receipt explicitly keeps `candidateAcceptance: false`. This continuation did
not rerun or refresh hosted CI, substitute that candidate for local `9f43cefe...`,
or accept the independently advanced remote-main catalog.

The current handoff remains [the retained exact qualification plan](../../platform-tuple-owner-ci-20261030/qualification-next-actions.md):
resolve the separately proposed TenantRegistry Gateway dependency change or
governed exception, choose and qualify a coherent exact producer/Platform tuple,
complete actual integration/runtime and policy decisions, obtain separate
publication/adoption authority, and verify actual Projects package preparation
plus every consumer package/symbol. The existing 46 inventory discrepancies and
newer catalog observations are preserved; no .NET version or future commit is
invented here. Unsupported File mapping remains explicit.

The root and Conversations production/test source paths have no delta from the
historical full-suite inputs; `historical-full-suite-input-delta.json` records
that limited comparison. Historical source/store/gateway results remain their
original executions and are not promoted to acceptance for this tuple.

`verify-evidence.py` checks the original story prefix/frozen block, the single
tracking-comment change, exact two-file owner patch, unchanged HEADs/indices/refs,
unmodified source/history, diagnostics, command outcomes and retained assets.
`SHA256SUMS.json` supplies local integrity checks, without release authority.
