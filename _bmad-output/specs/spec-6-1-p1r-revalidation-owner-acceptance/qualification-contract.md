# 6.1-P1R Qualification Contract

## Authority and evidence precedence

The accepted finite record is the decision of record for P1R. Before acceptance, evidence has this precedence:

1. freshly captured immutable repository and package evidence;
2. the approved 2026-08-03 proposal as execution authority;
3. the historical `3.88.0` qualification record as preserved candidate evidence; and
4. diagnostic or workspace observations as non-qualifying leads only.

No coordinate becomes accepted through recency, a branch name, a package listing, or a passing subset of tests.

## Dated starting observations

| Surface | 2026-08-03 proposal observation | Disposition |
| --- | --- | --- |
| EventStore source checkout | `7854f8e51ce9b852bb6c3cac6012670122e93792`; then described as `v3.89.0-9-g7854f8e5` | Source-mode observation; not package identity |
| EventStore package source | `3.89.0`; tag `v3.89.0`; revision `c590590bc581a3f72ef6e67148eda988ba4b8fe6` | Package-mode candidate |
| EventStore package inventory | 14 package IDs; manifest SHA-256 `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae` | Remote listing observation, not consumption proof |
| Builds checkout | `7bdbd293991985d150dfca62f77709e61152de76` | Observation only |
| Builds catalog | `3.89.0`; introduced by `10af541e7b2a5a4664be37c9495930844e0954a8` | Candidate binding |
| Builds runner/schema/fixtures/tests/audit | `3.88.0` | Stale, internally consistent runner corpus and stale audit |
| Published G-4 tools | `4.23.0`; release revision `7ac2849d79e603b88c7cb76e178cd2ba106eaf00`; embeds EventStore `3.70.1` | Published but not the P1R/P0 baseline |
| Architecture Spine | EventStore `3.70.1`; source `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` | Last accepted binding |
| Rollback | EventStore `3.70.1` | Retained accepted fallback; `3.88.0` is not rollback |

A non-qualifying 2026-08-04 workspace check already found further drift: EventStore revision `7854f8e…` is now tagged `v3.90.0`, and Builds revision `a53166539bf4441d5e33d04281b14c2d59e950c3` selects catalog `3.90.0` while runner, schema, and audit remain `3.88.0`. Execution must not presume either the proposal tuple or this later observation is the selected tuple.

The 2026-08-03 diagnostics are historical leads only: the seven API blobs matched at `v3.88.0`, `v3.89.0`, and source revision `7854f8e…`; Module passed `107/107`; Evidence passed `24/24`; the structural catalog check passed 49 identities and three shared versions; and the audit failed with 33 mismatches, 13 EventStore plus 20 unrelated. An initial contended Module run failed in shared generated output before a serialized rerun passed. None of these results qualifies the selected tuple.

## Required coordinate record

Before Builds alignment, the finite record must resolve every field below with a full immutable value.

| Field | Required value |
| --- | --- |
| `eventstore_source_revision` | Full commit used for source-mode qualification |
| `eventstore_source_describe` | Exact tag/describe output captured from that revision |
| `eventstore_package_version` | Exact immutable package version |
| `eventstore_package_tag` | Exact repository tag for the package release |
| `eventstore_package_source_revision` | Full commit resolved from the package tag |
| `source_package_equivalent` | `true` only when both revisions are identical; otherwise `false` with owner rationale |
| `eventstore_release_manifest_sha256` | Hash of the package tag's release manifest |
| `eventstore_release_package_ids` | Exact 14-package inventory and remote listing result per ID |
| `builds_catalog_introducing_revision` | Full revision that selected the package version |
| `builds_qualifying_revision` | Full clean revision containing catalog, audit, runner, schema, fixtures, evidence, and hashes |
| `architecture_pre_acceptance_revision` | Full Projects revision whose Spine still holds the last accepted binding |
| `rollback_eventstore_revision` | `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` unless later owner authority changes it |
| `rollback_builds_revision` | Full clean revision atomically binding catalog, audit, runner, schema, fixtures, and evidence to `3.70.1` |

If the selected source and package revisions differ, the EventStore Owner and Solution Architect must either accept the intentional behavioral split or stop alignment until a new immutable package release names the selected source revision. A branch name or relabeled checkout is inadmissible.

## Execution phases

| Phase | Entry condition | Required output | Exit gate |
| --- | --- | --- | --- |
| 0. Preserve history | Approved proposal available | Original `3.88.0` commands and results retained byte-for-byte; dated supersession section or sibling record opened | Historical evidence is traceable and not current authority |
| 1. Select coordinates | Fresh repository/package capture complete | Required coordinate record plus EventStore Owner disposition | Exact source/package choice is immutable and divergence is resolved |
| 2. Align Builds | Phase 1 passes and owning-repository implementation is authorized | Catalog, full audit, runner, schema, positive fixtures, serialized evidence, and hashes use the selected package version at one Builds revision | Full audit and static parity checks pass |
| 3. Qualify EventStore | Clean source and package-source worktrees exist | Seven-API comparison, source-mode behavior results, package-mode behavior results, and remote package restore evidence | Every EventStore lane passes independently |
| 4. Qualify runner | Phase 2 passes in a clean Builds worktree | Serialized Module/Evidence/catalog/audit/package/control results with retained packages and hashes | Every runner and packaged-command lane passes |
| 5. Qualify rollback | Clean rollback EventStore and Builds worktrees exist | Executed `3.70.1` restore/build/control evidence and reciprocal rejection of the selected candidate | Rollback is independently executable |
| 6. Accept record | Phases 1–5 pass and evidence is complete | Four named, dated owner decisions on the same coordinates and evidence | Every decision is `accept`; no placeholder or exception remains |
| 7. Propagate | Phase 6 passes | Architecture and planning updates tied to the accepted record | Only P1R closes; all downstream gates retain their own blockers |

Do not start a later phase from a failed, pending, inconclusive, cancelled, or non-qualifying predecessor.

## Clean execution protocol

- Use a separate clean worktree at each recorded EventStore and Builds revision; do not qualify from a dirty checkout or move an existing user worktree.
- Capture `git status --porcelain=v1`, `git rev-parse HEAD`, and `git describe --tags --always --dirty` before the first command and after the last command in each worktree.
- Serialize build and test lanes that share generated output. Use unique qualification-scoped NuGet, CLI-home, temporary, and package-output directories.
- Run solution restore/build only through `.slnx`; run test projects individually.
- Retain every first result. A passing rerun may supersede a contention result only when both remain in the ledger and the rerun uses a clean serialized context.
- Record UTC start/end, working directory, exact command, environment overrides, exit, stdout/stderr log path and SHA-256, produced artifact path and SHA-256, and `PASS|FAIL|INCONCLUSIVE` disposition for every row.
- Any missing log, unresolved placeholder, cancellation, stall, exit `143`, or unexpected diagnostic is non-qualifying.

## EventStore qualification matrix

### Coordinate and source comparison

Run these command families in the relevant clean EventStore worktree and retain their exact expanded commands:

```text
git status --porcelain=v1
git rev-parse HEAD
git describe --tags --always --dirty
git rev-parse '<PACKAGE_TAG>^{commit}'
git show <PACKAGE_TAG>:tools/release-packages.json
git diff --name-status <PACKAGE_SOURCE_REV>..<SOURCE_REV> -- src
```

The source/package diff must explicitly classify at least the proposal-observed production paths when they remain different:

- `src/Hexalith.EventStore.Server/Commands/CanonicalIdempotencyIntentEncoder.cs`
- `src/Hexalith.EventStore.Server/Commands/IdempotencyIntentAdapterRegistry.cs`
- `src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs`

Additional production changes discovered from the selected coordinates join the affected-behavior lane; they cannot be omitted because they were absent from the proposal.

### Seven-API comparison

Compare Git blob identities at the rollback revision, superseded `v3.88.0`, selected package-source revision, and selected source revision for:

1. `src/Hexalith.EventStore.DomainService/IAsyncDomainProjectionHandler.cs`
2. `src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs`
3. `src/Hexalith.EventStore.Client/Projections/IReadModelBatchStore.cs`
4. `src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs`
5. `src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs`
6. `src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs`
7. `src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs`

Selected source/package APIs must be identical or carry an explicit compatible owner disposition. The rollback comparison may retain the previously observed additive `QueryCursorScope.AddProjectionWatermark(long?)`; any newly removed or changed surface blocks acceptance.

### Source-mode lane

From the selected source revision, expand and retain this serialized lane:

```text
dotnet restore Hexalith.EventStore.slnx -p:UseHexalithProjectReferences=true --force --no-cache --disable-parallel --verbosity minimal
dotnet build Hexalith.EventStore.slnx --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
dotnet test tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
dotnet test tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
dotnet test tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
dotnet test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1
```

Repository-required environment-only pins may be added when recorded; they may not weaken warnings, tests, exact pins, or audit behavior.

### Package-source and remote-package lane

From the selected package-source revision, expand and retain this serialized lane:

```text
dotnet restore Hexalith.EventStore.slnx -p:UseHexalithProjectReferences=false --force --no-cache --disable-parallel --verbosity minimal
dotnet build Hexalith.EventStore.slnx --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1
dotnet test tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1
dotnet test tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1
dotnet test tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1
dotnet test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -m:1
```

Separately, use a disposable clean consumer with a NuGet configuration that contains only the official remote V3 source, an empty qualification-scoped package cache, and exact references to all 14 release-manifest package IDs. Retain the consumer project and NuGet configuration bytes and hashes, then run:

```text
dotnet restore <REMOTE_CONSUMER_PROJECT> --configfile <REMOTE_NUGET_CONFIG> --force --no-cache --disable-parallel --verbosity minimal -p:RestorePackagesPath=<EMPTY_PACKAGE_CACHE>
```

The restore must resolve every package at the selected version; local feeds, fallback folders, floating versions, and pre-populated caches are forbidden.

The record must distinguish source-at-package-tag behavior from actual remote package consumption. A successful tag build cannot substitute for remote restore, and a remote listing cannot substitute for consumption.

## Builds alignment and qualification matrix

### Static alignment

At `builds_qualifying_revision`, verify:

- all 13 catalog rows resolve through `HexalithEventStoreVersion=<SELECTED_PACKAGE_VERSION>`;
- `SupportedPlatformPins.EventStoreVersion`, the manifest schema, current positive fixtures, serialized evidence, and coupled hashes use that same version;
- explicit active-runner negatives reject both `3.88.0` and `3.70.1` with `HXM016`;
- unrelated negative fixtures retain their original rule IDs, including invalid-profile `HXM009` where applicable; and
- deliberate evidence-hash mismatch controls remain invalid.

### Serialized command lane

From the clean qualifying Builds revision, run and retain:

```text
dotnet restore Hexalith.Builds.slnx --disable-parallel --verbosity minimal
dotnet build Hexalith.Builds.slnx --configuration Release --no-restore -p:GeneratePackageOnBuild=false -m:1
dotnet test test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj --configuration Release --no-restore -m:1
dotnet test test/Hexalith.Builds.Evidence.Tests/Hexalith.Builds.Evidence.Tests.csproj --configuration Release --no-restore -m:1
pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1
pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1
pwsh -NoProfile -File ./Tools/test-g4-tool-package-contracts.ps1 -Version <QUALIFICATION_VERSION> -RequireControls -RetainPackageDirectory
```

`<QUALIFICATION_VERSION>` is a unique disposable SemVer recorded in the ledger. The official packaged-tool gate must finish without `-SkipSourceValidation`; a manual consumer run may add diagnostic detail but cannot replace it. Retain both CLI packages, symbol packages when produced, consumer evidence, and SHA-256 values.

The authoritative catalog test proves structure only. The audit validator must separately pass the complete catalog at the same revision with zero stale catalog/audit mismatches, including the 20 non-EventStore mismatches observed on 2026-08-03.

## Reciprocal rollback matrix

The rollback worktree binds EventStore `3.70.1` atomically across catalog, audit, runner, schema, fixtures, evidence, and hashes at `rollback_builds_revision`.

| Worktree | Positive pin | Required rejected pin(s) | Required result |
| --- | --- | --- | --- |
| Selected candidate | Selected package version | `3.88.0`, `3.70.1` | Positive passes; each stale pin exits nonzero with `HXM016` |
| Rollback | `3.70.1` | Selected package version | Rollback passes; candidate exits nonzero with `HXM016` |

Run the Builds static alignment, Module, Evidence, catalog, audit, packaged-command, hash, and state assertions from the rollback revision. Re-run the applicable EventStore source/package/API lane at `v3.70.1`. A procedure that has not executed from clean worktrees is not rollback evidence.

## Finite record schema

The superseding record contains these sections with no unresolved value:

1. selected and rollback coordinate tables;
2. source/package divergence decision and affected production diff;
3. 14-package manifest and remote-restore inventory;
4. seven-API blob comparison;
5. timestamped EventStore source/package behavior ledger;
6. timestamped Builds alignment/runner/package ledger;
7. reciprocal rollback ledger;
8. retained log, evidence, package, and manifest hashes;
9. downstream non-closure assertions; and
10. four-owner acceptance table.

Each command row contains `id`, `repository`, `revision`, `cwd`, `started_utc`, `ended_utc`, `command`, `environment_overrides`, `exit`, `result`, `log`, `log_sha256`, `artifacts`, and `notes`. `result` is exactly `PASS`, `FAIL`, or `INCONCLUSIVE`; only `PASS` satisfies a gate.

## Owner acceptance

| Role | Required decision scope |
| --- | --- |
| EventStore Owner | Attest source and package identities, 14-package release evidence, affected behavior, and any intentional divergence |
| Builds Owner | Attest full catalog/audit alignment, runner/schema/fixture/evidence parity, exact qualifying revision, and rollback Builds revision |
| Solution Architect | Accept the multi-coordinate binding, executable rollback, Architecture Spine update, and G-1 non-selection wording |
| Test Architect | Independently verify clean execution, retained evidence, negative controls, remote restore, and rollback proof |

Every row records named approver, decision, UTC date, exact accepted coordinates/revisions, and evidence reference. Acceptance requires four `accept` decisions against the same finite record; role placeholders, inferred consent, proposal approval, or acceptance of different revisions fails the gate.

## Propagation contract

After four-owner acceptance:

- update the Architecture Spine with the exact source-mode revision, immutable package version/tag/source revision, qualifying Builds catalog/runner revision, acceptance date and roles, and executable `3.70.1` rollback;
- clarify G-1 against the accepted package/runner evidence without claiming that P1R selects Durable Task or Confirmation Artifact capability;
- change P1R from `open` to `done` with exact revisions and the finite-record link;
- mark only P0 Stage 1 complete so P0 may consume the baseline;
- retain P0 supported-composition, publication, consumer-pin, persisted-qualification, acceptance-record, and owner-acceptance gates, including absent `.config/dotnet-tools.json`, `module/hexalith-projects.module.json`, and `evidence/g4/6.1-p0-acceptance.json` until P0 creates and accepts them;
- retain P2, P3, P4, Story 6.1, readiness, and transitive Epic 7/8 gates; and
- preserve the sequence `6.1-P1R -> {6.1-P0, 6.1-P2} -> 6.1-P3 -> Solution Architect sign-off -> 6.1-P4 -> specification readiness -> independent READY -> Story 6.1`;
- prevent any intermediate planning state from representing the selected tuple as accepted before Architecture and the P1R evidence link agree.

## Preservation ledger

| Source claim group | Contract landing |
| --- | --- |
| Multi-coordinate drift and dated observations | Dated starting observations; required coordinate record |
| Source/package divergence and post-tag production changes | Coordinate selection; EventStore source comparison |
| Seven stable Story 6.1 APIs | Seven-API comparison |
| Stale audit, runner, and published-tool evidence | Builds matrix; Non-goals in `SPEC.md` |
| Superseded `3.88.0` evidence and `3.70.1` rollback | Preserve-history phase; reciprocal rollback matrix; adopted historical record |
| Clean source/package, runner, remote restore, negative, and rollback gates | Qualification matrices and clean execution protocol |
| Four-owner acceptance | Finite record schema; owner acceptance |
| Architecture and downstream state boundary | Propagation contract; adopted Architecture Spine |
| P0 success boundary | `SPEC.md` constraints/non-goals; propagation contract |

Wrapper-only content intentionally omitted: correct-course review ceremony, option-scoring prose, checklist bookkeeping, handoff narration, and approval metadata that do not change execution or acceptance.
