# Story 6.4 P1R successor publication decision request — 2026-10-18

**Disposition: HOLD; prerequisites first, owner decision pending.** CI has not supplied a successful exact-source push proof. The owner request below is prepared conditionally and is not ready for dispatch. This packet is a proposed release tuple, not publication authority, package selection, qualification or P1R acceptance. [Fresh read-only evidence](evidence/6-4-p1r-successor-decision-20261018/refresh-20261018T114001Z/README.md) supersedes the earlier observations for current applicability; historical failed runs and receipts remain intact.

## Rechecked coordinates

Live EventStore `main` was unchanged at `39b366bca3dbe508b4cf3c5afa69db1ddadb6dd2` from `2026-10-18T11:49:22.130475Z` through `2026-10-18T11:49:26.545729Z`. No local checkout or gitlink was moved.

| Coordinate | Full revision and evidence |
| --- | --- |
| Proposed EventStore source | `39b366bca3dbe508b4cf3c5afa69db1ddadb6dd2`. Local EventStore remains `47e234ccb535a71e7dd4a039fd0e2281083083ae`; local source is not a substitute for the observed live revision. |
| Last actual release/tag | [`v3.124.0`](https://github.com/Hexalith/Hexalith.EventStore/releases/tag/v3.124.0), published `2026-10-16T18:39:44Z`, directly targets `0737c0befce5338e294a789092a0e223e8264a04`. Twenty recent releases and release-workflow runs were rechecked; no later release was observed. |
| Proposed version/tag | Installed `@semantic-release/commit-analyzer` **13.0.1** reports **minor** across **22** post-tag commits: prospective `3.125.0` / `v3.125.0`. Both proposed tag and release probes returned HTTP 404. Semantic Release chooses the actual version; the actual tag must target the approved source. [Inputs, config hash and output](evidence/6-4-p1r-successor-decision-20261018/refresh-20261018T114001Z/release-analysis.json). |
| Package-build Builds | EventStore gitlink `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`. Its catalog still selects EventStore `3.124.0`. This is the **expected** package-build checkout, not yet actual publication provenance. |
| CI reusable Builds | `domain-ci.yml@main` resolved in [CI run 39123504769](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/39123504769) to `4629ff2d3bd66376b29a553663c241d3a7b87853`. Run status is recorded below; resolving this identity alone does not establish a successful proof. |
| Release-execution Builds | `22a578b576a515d2af214fe81859447fffc97981`, fixed separately in the exact-source release workflow and publication preflight. Do not replace it with the package-build or CI revision. |
| Workspace / remote Builds | Projects gitlink `6e8b71e9bc5fe4a38c86069fd8e37f6a6cafa292`; Builds remote `main` observed at `4629ff2d3bd66376b29a553663c241d3a7b87853`. Neither updates a consumer pin or authorizes publication. |

All five official NuGet flat-container indexes returned HTTP **200** during `2026-10-18T11:40:06.496852Z`–`2026-10-18T11:40:07.391964Z`; each still ends at **3.124.0** and omits **3.125.0**. [Actual per-request timestamps, headers, hashes and raw bodies](evidence/6-4-p1r-successor-decision-20261018/refresh-20261018T114001Z/nuget-observations.json) cover Client, Contracts, DomainService, Server and ServiceDefaults. Version `3.125.0` remains **proposed and unpublished as observed**; older signed archives prove only their older source.

## Exact-source release prerequisites

All rows below are `push` runs on `main` at `39b366bca3dbe508b4cf3c5afa69db1ddadb6dd2`, rechecked at `2026-10-18T11:49:26.545729Z`. Full responses and job/step outcomes are retained in [gate observations](evidence/6-4-p1r-successor-decision-20261018/refresh-20261018T114001Z/gate-observations.json).

| Required workflow | Run | Result |
| --- | --- | --- |
| `ci.yml` | [39123504769](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/39123504769) | **still in progress** |
| `p1r-qualification.yml` | [39123504568](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/39123504568) | **success**; preparation/harness/process controls only |
| `commitlint.yml` | [39123504763](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/39123504763) | **success** |

At capture, `ci / contracts` is running Contracts tests and `ci / build-and-test` is running Tier 1 Microsoft.Testing.Platform tests; the other four CI jobs have succeeded. No current failed step is reported.

The [sentinel repair](spec-6-4-prerequisite-p1r-sentinel-lifecycle.md) was committed as `794369b30c2f3615a7f1d70a5beb1935f6470b36`; the current green preparation run supersedes the old red sentinel proof for tooling readiness. The [scanner repair and separately approved retirement](spec-6-4-prerequisite-secrets-protection.md) are later history, not authority to carry the old 2,812-findings CI failure forward. No new failure or further repair is established by a running CI job. If the current CI fails, retain its exact failed step/test, diagnose it, and propose the smallest owner-repository repair; never bypass the gate.

The [source-readiness receipt](spec-6-4-prerequisite-p1r-source-readiness.md) remains bound to `bb986902d3c1b2f3b1c209fe8eb0ffa0292153d9`, Debug/source checks and Dapr **1.18.7/Redis**. Current hosted preparation is not fresh live source readiness or published qualification. Later query, recovery, payload-protection and governance changes are outside that receipt. Before publication authorization, obtain fresh current-source readiness evidence **or explicit dated, named EventStore/Builds owner disposition of this gap**. Pinned Dapr **1.18.2/PostgreSQL** package execution and exact shared-resource preservation remain for the separately authorized qualification; none was run here. The readiness disposition must identify this full current source and the older receipt, address authority/error semantics, pending-batch/create-only recovery, folded-snapshot/retained-replay/restart and strict witness/isolation controls, cite the current evidence owners rely on, and state remaining limitations. It cannot waive any required push proof or replace the later pinned-profile package qualification.

The [normal release workflow at this source](https://github.com/Hexalith/Hexalith.EventStore/blob/39b366bca3dbe508b4cf3c5afa69db1ddadb6dd2/.github/workflows/release.yml) accepts **no inputs**, including no version. Operators must recheck live-main equality before dispatch. After dispatch, its unprotected `verify-source` job requires live-main equality and the three successful exact-source push proofs; the protected release job rechecks them immediately before Semantic Release. Publication requires `production` review. The repository publication variable `HEXALITH_RELEASE_PUBLISH_ENABLED` is currently `true`; production requires reviewer `jpiquot`, permits reviewer self-review (`prevent_self_review=false`), disallows admin bypass and restricts deployment to `main`. These observations do not constitute the review or owner authorization. [Configuration and protection evidence](evidence/6-4-p1r-successor-decision-20261018/refresh-20261018T114001Z/publication-config.json). NuGet publishing-account, registry and credential validation remains in the normal protected workflow; it was not executed or established by this source-readiness observation.

The release-execution implementations are pinned independently: [initializer](https://github.com/Hexalith/Hexalith.Builds/blob/22a578b576a515d2af214fe81859447fffc97981/Github/initialize-build/action.yml), [container action](https://github.com/Hexalith/Hexalith.Builds/blob/22a578b576a515d2af214fe81859447fffc97981/Github/publish-containers/action.yml), [publisher](https://github.com/Hexalith/Hexalith.Builds/blob/22a578b576a515d2af214fe81859447fffc97981/Github/publish-containers/publish-containers.sh) and [shared preflight](https://github.com/Hexalith/Hexalith.Builds/blob/22a578b576a515d2af214fe81859447fffc97981/Github/publish-containers/publication_preflight.py). The exact EventStore caller/preflight text is retained with the configuration evidence.

## Concrete owner approval request — conditional, not granted

**To the authorized EventStore and Builds owners:** after CI success and the current-source readiness evidence/disposition above are recorded, provide a dated, named decision authorizing **one normal `release.yml` dispatch from `main`**, with this exact tuple:

- EventStore source `39b366bca3dbe508b4cf3c5afa69db1ddadb6dd2`.
- Expected package-build Builds `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`; separately observed CI Builds `4629ff2d3bd66376b29a553663c241d3a7b87853`; release-execution Builds `22a578b576a515d2af214fe81859447fffc97981`.
- Required exact-source push runs **39123504769 / 39123504568 / 39123504763**, all of which must be successful before authorization (**CI 39123504769 remains pending in this observation**), revalidated live, and an explicit disposition of the older readiness scope.
- Protected `production` review and the declared **fourteen NuGet packages plus EventStore container**. The five P1R packages are a later qualification subset.
- Semantic Release's actual version/tag, prospectively `3.125.0` / `v3.125.0`; require its actual tag to target the approved source. Any changed source, Builds coordinate, version analysis or required gate returns for a new exact decision.

No owner approval is recorded here. No dispatch command was executed. Publication does not authorize qualification or move any Projects dependency.

After release, retain an immutable **fourteen-package/container inventory**, actual publication outcome, tag/source, archive SHA-256/signatures/NuGet content hashes/repository identities, actual package-build Builds provenance, separate CI/release-execution identities, resolved graphs and container digest. A failure after any push may leave published bytes: inventory every visible artifact and obtain a fresh owner disposition before retry or selection. Then obtain a **separate owner selection** of the complete five-package execution tuple before the [bounded qualification handoff](spec-6-1-p1r-31250-published-qualification-handoff.md) can run. Four-role acceptance and same-baseline conformance follow measured qualification.

Retain successor **`rollback=null`** and Projects AD-17 single-writer drain, mutation freeze and forward recovery. The fixed [historical acceptance](6-1-p1r-acceptance.json) remains historical and supplies no writable successor rollback. Package pins, acceptance, P1R usability=false, blocked Story 6.4 and its sprint key are unchanged; G-2/RTI, G-4, P2/P3/P4 and G-6 remain independent prerequisites.
