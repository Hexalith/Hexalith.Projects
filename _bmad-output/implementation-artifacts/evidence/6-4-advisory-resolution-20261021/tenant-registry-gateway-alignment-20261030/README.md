# TenantRegistry Gateway alignment — 2026-10-30

The approved one-line owner patch is applied: `TenantRegistry.Host` now selects Gateway **3.128.4**. The actual host restores offline and builds in **Debug with zero warnings/errors**. Story 6.4 remains **in-progress**; this checkpoint does not accept a Platform tuple, runtime or release.

[Dependency audit](dependency-audit.json) and the unchanged captured [real assets](host.project.assets.json) show seven EventStore packages at **3.128.4** and Commons.UniqueIds **2.30.3**. All eight match the current sibling catalog, with no foreign source-project libraries or remaining version difference in this host closure. Cached NuSpec repository commits are recorded. This gateway host has no Tenants package; no Tenants service behavior is inferred. No feed/signature verification was refreshed.

The approved [historical proposal](../platform-tuple-owner-ci-20261030/tenant-registry-catalog-alignment.UNAPPLIED.patch) is preserved unchanged as a proposal receipt; it is now applied in the original Platform checkout. The resulting owner file matches its reviewed SHA-256 `af07af77a7ca12ad9120686d89e878e3701754e34ac4c40d3cb931507a152c23` exactly. Original Platform remains `9f43cefe11bd246ce0de1cd0740261b557c0c7a0`; sibling Builds remains `e909ffd9fe9a6097e36d932010b983d029396968`.

| Local verification | Result |
| --- | --- |
| Actual TenantRegistry offline restore and package-only Debug build | Pass; zero warnings/errors |
| Current owner Composition test-project offline restore and package-only Debug build | Pass; zero warnings/errors |
| Existing `GatewaySeedSubscriptionTests` | 4 passed; zero failures/errors/skips/not-run |
| Actual host HTTP controls | 10 passed: liveness 200; unavailable readiness/health 503; subscription discovery 200; four authentication denials 401; two authenticated authorization denials 403 |
| Command-owned host/Dapr/Redis cleanup | All stopped with exit 0 |

The four existing tests exercise the Platform and published Builds seed subscription configuration. They are a focused shared-host regression, distinct from the actual TenantRegistry process checks. Their test build uses the previously retained published Builds 4.38.3 assemblies. No hosted Release suite was repeated.

[Local runtime observations](local-host.json), [exact launch commands](runtime-commands.json) and [sanitized health failures](local-readiness-limitations.json) retain the real limitation: actor placement is disconnected/disabled and projection-delivery writer protocol v2 is inactive. No Tenants DomainService or scheduler was started. Consequently no successful authorized owner query, full runtime integration or ready host is claimed. The existing focused seed fixture supplies the bounded fallback while preserving the real host's 503 behavior and all validation gates.

Redis uses a temporary copy, named `redis-server`, of the installed Redis-tools binary; its hash matches the [earlier local runtime receipt](../local-store-verification-20261028/runtime-provenance.json). Redis 8.0.5 and installed Dapr 1.18.7 bind only to command-owned loopback ports. No package/system installation or deployment occurred. Ephemeral signing/channel keys stayed in memory and complete runtime logs remain under `/tmp`, outside this receipt.

Every `*.command.json` records exact argv, cwd, explicit environment, UTC start/end, timeout and exit; sibling stdout/stderr retain compiler/test results. `run-local-host.py` records the exact HTTP payload/control logic. `offline.NuGet.Config` clears all package sources. [Preservation](preservation.json) binds the original story prefix/frozen block and preserved changes/evidence. All twelve owner HEAD/index/ref identities match. The raw preservation command exits 1 because an independent root rebase advanced Projects from `135b90059ac37db2d214d5d42570a1b5b53d6559` to `5f7fd70996ad78cf8252e98c55254b8879070cb0` and changed only the four root pushall skill copies in its committed tree. Exact before/after root index/ref identities, reflog and path comparisons are retained in the same result. Those external changes remain untouched; the original snapshot was not rebased or replaced. Every other original file matches. Local qualification remains bound to its captured owner/package inputs. This task changes only the authorized owner pin, new evidence and appended spec/tracking checkpoint.

Remaining work follows the [existing owner handoff](../platform-tuple-owner-ci-20261030/qualification-next-actions.md): a coherent exact producer/Platform tuple, real matching owner recording and integration revision, owner/runtime/CI and policy qualification, separately authorized publication/adoption and complete consumer package/symbol verification. Original package HXW011/HXW009 findings were not rerun or cleared here. Unsupported File mapping remains explicit. No staging, commit, branch/ref mutation, remote operation, nested initialization or consumer-tuple adoption occurred.
