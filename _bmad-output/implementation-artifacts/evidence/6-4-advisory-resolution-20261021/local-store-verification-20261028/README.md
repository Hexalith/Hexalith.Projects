# Story 6.4 current source, store and consumer verification — 2026-10-28

The original checkout now passes the complete affected source suites, including the authenticated gateway, signed owner queries, persisted byte comparisons and store-client restart. The normal package-based Projects.Server Release build also passes with Conversations Client/Contracts 1.3.1. Story 6.4 remains `in-progress`: the recorded public Platform/Builds package qualification gate still refuses the current tuple. No production source or dependency version changed during this continuation.

## Executed verification

| Check | Result |
| --- | --- |
| Fresh public Projects source preparation | Pass |
| Projects Contracts, Server and core test-project Debug builds | Pass, zero warnings/errors |
| Projects.Server full suite | 1,306 passed |
| Projects.Contracts full suite | 230 passed |
| Projects core, engine and confirmation full suite | 684 passed |
| Fresh public Conversations source preparation and Server test-project Debug build | Pass, zero warnings/errors |
| Conversations.Server full suite | 1,372 passed |
| Production-authority planning guard | 37 passed |
| Story and index authority validators | Both pass |
| Normal package-based Projects.Server Release build | Pass, zero warnings/errors |
| Root and Conversations whitespace checks | Both pass |
| Public Projects package preparation | Exit 2: HXW011 and HXW009 |

The four full xUnit suites executed **3,592 passing cases**, with zero failures, errors, skips or not-run cases. Together with the planning guard, **3,629 cases pass**. Earlier focused runs and repeated fixture executions are not added. Some theory display names truncate distinct URL arguments; their separate XML case IDs and full-suite counts are retained.

The parent independently parsed every full-suite XML result. All 65 historical matrix references map to current passing cases across all seven frozen rows; see [matrix audit](parent-audit/matrix-audit.json). The current Server XML contains both `SupportedProjectGatewayHttpTests.GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures` and `SupportedProjectReadHttpTests.ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart` as passing. Source inspection confirms the gateway case checks advisory output, owner denial and nonempty persisted before/after byte equality. These are controlled local authority/storage fixtures, not live OIDC or deployed authorization acceptance. File inputs remain explicitly unsupported/incomplete.

`commands.json` records exact source preparation, individual Debug build and direct xUnit argument arrays, working directories, producer environments, UTC times and exit codes. Both original producer mappings are retained unchanged in `generated-projects-source-mapping/` and `generated-conversations-source-mapping/`. `executed-recorder.py` is the exact executed initial recorder. `parent-audit/commands.json` and its stdout/stderr retain the independent planning, authority, whitespace, consumer build and package-preparation commands.

## Isolated local persistence fixture

Remote Docker port publication and host-network routing are unnecessary for this lane. The installed Redis tools include the real Redis server code. A matching Redis server package was downloaded and extracted only under `/tmp`; its launcher and a copy of the installed `redis-check-rdb` binary run Redis 8.0.5. No system package was installed and no repository dependency changed. [Runtime provenance](runtime-provenance.json) records package/binary hashes and the actual Redis/Dapr versions; it does not claim fresh upstream release qualification.

Redis and Dapr bind to loopback-only, command-owned ephemeral ports with an isolated state component. The test hosts use their existing authenticated gateway/owner fixtures and the real Dapr/Redis store. [Cleanup](cleanup.json) records both owned processes stopped with exit 0. No existing AppHost or unrelated resource was stopped. Runtime/application stdout and stderr remain private in the temporary directory; the retained xUnit XML contains no captured runtime output elements or credentials.

The initial focused command combined a gateway `-class` filter with a persistence `-method` filter. xUnit intersected those filters and selected zero tests despite exit 0. Its original zero-case XML and summary are retained as **failed coverage verification**, not acceptance evidence. The subsequent unfiltered full Server suite executed and passed both exact cases, resolving that coverage gap without adding a repeated-test count.

## Package consumer result and remaining qualification

The normal repository Release configuration defaults to package references for external modules. No producer import, source mapping, guard suppression or dependency override was supplied to the successful consumer command:

```text
dotnet build src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
```

[Consumer assets](parent-audit/consumer-assets-audit.json) and the complete retained `project.assets.json` prove Conversations Client/Contracts 1.3.1 are packages and no external module is a project reference. The current catalog also selects EventStore 3.128.4 and FrontComposer 4.7.1; this build does not reconcile unrelated older constraints in `tools/release-packages.json` or establish complete release-manifest qualification.

The previously retained official Conversations 1.3.1 release/source receipts and both cached signed package archive hashes still match. Published source `986875bfcb5471953378b600710b5d859f3eaba4` is an ancestor of current Conversations `ef01d578a00efd527bae9226dca302f54cd012f0`; their complete `src/` trees have no difference. The original root index already pins that current owner revision. No publication, pin mutation or fresh feed/remote observation was performed here. Cached signatures retain their earlier offline-revocation limitation; this consumer compilation does not qualify every package/symbol or the public producer tuple.

The first public-package retry supplied an already-created output directory and correctly failed HXW007. That failed receipt is retained. The corrected command uses a fresh, nonexistent child output path, and exits 2:

```text
dotnet tool run hexalith-module -- prepare-build --manifest /home/administrator/projects/hexalith/projects/module/hexalith.module-manifest.v2.json --mode package --workspace /tmp/story64-package-audit-final-<unique>/package --output json
```

The exact path and owner working directory are in `parent-audit/commands.json`; `parent-audit/public-package-prepare-final.stdout` records both current blockers:

- **HXW011:** exact Platform `fcee54288c75feaadd673e0484da1e5b5d742cc8` has no local SemVer release tag. This is not a fresh remote release-absence claim.
- **HXW009:** that Platform pins Builds `81ab049208678bd6616828824729c0ddac02f7d1`, while the umbrella index and checkout select `2574bf2f3da5367d8801f0d7661d66155e5d90b0`.

The source/store prerequisite and current consumer compilation gaps are resolved. Qualified Platform/Builds composition and complete consumer/release acceptance remain open under the current specification. No new complete CI, host migration, deployment or production acceptance is claimed. Existing separately reviewed producer preparation remains available; no new generic platform work was introduced.

## Preservation and audit

Root HEAD remains `22c99ad1ae0aa88cd563f2d85346c9d680acc2ca`. [Preservation](preservation.json) compares every original direct owner's HEAD/index and 44,383 tracked source/configuration/documentation hashes before and after source execution. The parent separately retained 819 original root source/configuration hashes. Existing root module declarations and the four previously prepared Conversations 1.3.1 manifest values are unchanged.

The original specification prefix, canonical baseline/frontmatter and frozen block remain byte-identical; only a new checkpoint is appended. Sprint tracking keeps `in-progress` and updates its obsolete comment. No Git staging, commit, push, branch, remote write, dependency adoption, nested initialization or deployment occurred. The workflow does not advance past its incomplete delivery gate.

`verify-evidence.py` checks retained hashes, all passing full suites, matrix references, the gateway/persistence cases, package selection, cleanup, current source bytes and original Git identities. `SHA256SUMS.json` is a local integrity inventory, not an external signature or release authority. Preparatory shell/file-copy invocations are not reconstructed as a complete command transcript.
