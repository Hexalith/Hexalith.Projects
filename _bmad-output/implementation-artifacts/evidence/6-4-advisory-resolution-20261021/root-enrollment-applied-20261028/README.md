# Story 6.4 local enrollment applied — 2026-10-28

The current Build continuation installs the reviewed, tested Projects declaration and README in the original checkout. This additive local configuration is needed to complete the requested source verification. It does not activate a host, CI, ingress or deployment. The original approved advisory behavior and frozen specification block are unchanged. Git HEAD and index remain unchanged at the recorded identities in `results.json`.

The installed files are byte-identical to the [current isolated candidate](../projects-current-enrollment-20261028/README.md). No Contracts reference-metadata correction was needed. The four approved Conversations Contracts release-manifest constraints also select 1.3.1, matching the existing Builds catalog and verified cached official package identities in the [consumer-pin receipt](../consumer-pin-checkpoint-20261028/README.md).

## Actual original-checkout verification

Public validation and source preparation both pass using the accepted Conversations tool manifest and the absolute Projects declaration path. Every emitted producer environment value is passed unchanged to both local Debug builds and the focused test runs. `commands.json` retains exact argument arrays, working directories, explicit environments, UTC execution times and exit codes; each command has separate stdout/stderr. `verify-current-root.py` is the executed recorder.

| Check | Result |
| --- | --- |
| Public Projects declaration validation | Pass |
| Public Projects source preparation | Pass |
| Projects.Contracts.Tests Debug build | Pass, zero warnings/errors |
| Projects.Server.Tests Debug build | Pass, zero warnings/errors |
| Advisory contract tests | 8 passed |
| Advisory, delegation, identity, isolation and supported HTTP tests | 173 passed, one prerequisite failure |
| Public Projects package preparation | Exit 2: HXW011 and HXW009 |

There are **181 passing Projects cases and one failed prerequisite case**, not a full-suite pass. The isolated-candidate execution repeats these same cases and is not counted again. Combined with the earlier 243 owner cases, 19 release-tooling cases and 37 planning-guard cases, this continuation has **480 distinct passing tests**. Both production-authority validators also passed. Raw current-root results are `contracts-tests.xml` and `server-tests.xml`.

The exact root focused Server command is recorded as:

```text
dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class *ProjectAdvisoryQueryTests -class *DelegatedProjectAdvisoryConversationDirectoryTests -class *DelegatedProjectDirectoryRegistrationTests -class *ProjectQueryEnvelopePrincipalBindingTests -class *ProjectQueryTenantFilterTests -class *SupportedProjectReadHttpTests -result-xml <this-receipt>/server-tests.xml -failSkips
```

It exits 1 because `SupportedProjectReadHttpTests.ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart` reports `FAIL_SKIP : Requires the isolated Story 6.1 Dapr persistence lane.` This is retained as a failed test. The unchanged signed gateway/store case was not executed. Two actual fixture attempts and their cleanup are retained in the [current-candidate receipt](../projects-current-enrollment-20261028/README.md): Docker port publication exits 125 on `/forwards/expose` HTTP 500; the host-network Redis fallback starts, but local Dapr exits 1 when its state store cannot connect to the remote container's loopback listener. No installed `redis-server` is available. A Redis endpoint reachable from local Dapr is still required.

## Package qualification remains open

The public package command, run from `references/Hexalith.Conversations`, is:

```text
dotnet tool run hexalith-module -- prepare-build --manifest /home/administrator/projects/hexalith/projects/module/hexalith.module-manifest.v2.json --mode package --workspace <fresh-directory> --output json
```

The exact fresh directory is in `commands.json`. Exit 2 retains HXW011 for exact Platform `fcee54288c75feaadd673e0484da1e5b5d742cc8`, which has no local release tag, and HXW009 because it pins Builds `81ab049208678bd6616828824729c0ddac02f7d1` while the original umbrella/index/checkout select `2574bf2f3da5367d8801f0d7661d66155e5d90b0`. No fresh official release observation or package qualification is claimed. No release gate, dependency version or mapping was overridden to manufacture acceptance.

The prior missing Projects mapping/HXW006 blocker is resolved in the actual original checkout. Story 6.4 remains `in-progress` because current persistence/gateway proof, the qualified exact Platform/Builds package composition and complete consumer delivery acceptance remain unfinished. No Git staging, commit, push, branch, remote write, publication, host migration, deployment or nested submodule initialization occurred.

The earlier checkpoint verifier intentionally describes its pre-enrollment state and is historical after installing the declaration. Its original frozen-prefix, source-hash and index evidence remains intact. Current `results.json` records this later root state. The exact frozen block and original specification prefix are preserved. Parent independently audited both candidate and current-root XML, installed bytes, cached package/source receipts and Git preservation; generated Python bytecode was removed only from the new receipt that created it.
