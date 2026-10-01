# 6.1-P1R current exact-baseline candidate — owner decision packet

Recorded 2026-10-01. **Status: accepted for P1R only; unusable as a current prerequisite while G-6 is stale.** Jérôme Piquot explicitly accepted the exact EventStore `3.110.0` / Builds `4.29.1` tuple below in all four roles at `2026-10-01T06:17:01Z`, including the Solution Architect EventStore Stack rebinding, with this packet's recorded limitations. The decision is recorded in [the implementation spec](spec-6-1-p1r-current-baseline-acceptance.md) and the fixed [acceptance record](6-1-p1r-acceptance.json). Selection and CI success were not substituted for those explicit decisions.

This packet supersedes the [3.109.0 candidate](6-1-p1r-3109-exact-baseline-candidate.md) as the current owner packet. The 3.109.0 and [3.108.1](6-1-p1r-3108-exact-baseline-candidate.md) packets and their evidence remain historical. The [2026-09-22 record](evidence/6-1-p1r-acceptance-20260922-3.106.0.json) for EventStore `3.106.0` / Builds `ad52f350a2f0bc47849179ae17b4594dafff5363` is preserved verbatim as historical evidence outside the fixed gate. The fixed record now selects `3.110.0` / Builds `21ce044ab465ccb2adab58b3d66e394ffbecf3c2` on the new explicit decisions; older decisions supply no authority for this transition.

## Exact coordinates and provenance

| Coordinate | Selected candidate / observed source |
| --- | --- |
| EventStore published package family | `3.110.0`, tag `v3.110.0`, canonical tag commit `27279fe6431925a6ea046c3f89af61487185c7de` |
| Candidate Builds | `4.29.1`, tag `v4.29.1`, revision `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`; this is the current root Builds gitlink |
| Projects observation | Root HEAD `4d8dcf65803792f7def3b10ed21227329536154b`; the existing user changes are preserved |
| EventStore source checkout / root gitlink | `6dededdecd62dd6dc6d1f15810108d860ec70c8f`, **13 commits after the published tag**, with **43 changed `src` paths** (40 added, 3 modified; no deleted paths). The additions chiefly introduce reminder contracts and runtime services. This checkout is a separate source coordinate; it is not the source of the published `3.110.0` archives. |
| EventStore tag CI | [36608682063](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/36608682063): successful push `.github/workflows/ci.yml` at `27279fe6431925a6ea046c3f89af61487185c7de`, attempt 1. Started `2026-09-29T17:58:07Z`, completed by `2026-09-29T18:03:06Z`. Contracts, tenants-source-mode, semantic-release-governance, and build-and-test succeeded; Aspire and performance jobs were skipped. |
| EventStore release | [36608763986](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/36608763986): successful `workflow_dispatch` `.github/workflows/release.yml` at the same tag commit, attempt 1. Started `2026-09-29T17:58:49Z`, completed by `2026-09-29T18:08:07Z`; the [release](https://github.com/Hexalith/Hexalith.EventStore/releases/tag/v3.110.0) was published `2026-09-29T18:07:59Z`. |
| Release source proof | **`BYPASS_VALIDATION: true`**. Verification-job log `109544505142` records that input. Publish-job log `109544551372` records `HEXALITH_RELEASE_SOURCE_CI_WORKFLOW: commitlint.yml`. Source proof was successful push Commitlint [36608682105](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/36608682105), not CI. The successful tag CI is independent evidence; it does not change the release's selected proof or supply an owner disposition of the bypass. |
| Release publication identity | Artifact `release-evidence-36608763986-1`, id `11052572354`, downloaded ZIP size `14729`, verified SHA-256 `6837c6db518f98228962c512106f83bdd618e2f47284bf4fa7895f95e51572d1`; `3.110.0/preflight/publication-identity.json` binds source `27279fe6431925a6ea046c3f89af61487185c7de`, version `3.110.0`, Commitlint proof `36608682105`, and release-time Builds `22a578b576a515d2af214fe81859447fffc97981`. |
| EventStore release manifest | `tools/release-packages.json` at `v3.110.0`, 14 package IDs, SHA-256 `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae`; publication identity reports the same hash. The release API lists all 14 `.nupkg` assets with sizes and hashes. Listing assets is not independent archive, NuGet signature, or payload verification. |
| Candidate Builds CI and release | Exact-source CI [36740809340](https://github.com/Hexalith/Hexalith.Builds/actions/runs/36740809340) succeeded at `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`; release [36743051740](https://github.com/Hexalith/Hexalith.Builds/actions/runs/36743051740) succeeded at the same revision. These are publication observations, not P1R or P0 acceptance. |
| Projects CI Builds execution | `.github/workflows/ci.yml` still executes `212583e08c7b6db22c7ccee881ad11699e1f7522`, different from the selected/root Builds gitlink `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`. No workflow or dependency pointer is changed here. |

The candidate tuple has exactly the four coordinates required by the minimal record:

- `eventstore_version`: `3.110.0`
- `eventstore_tag`: `v3.110.0`
- `eventstore_revision`: `27279fe6431925a6ea046c3f89af61487185c7de`
- `builds_revision`: `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`

The rollback remains EventStore `3.70.1` / `v3.70.1` / `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` with Builds `7af20f8bafbfe561df6f7705913a0800603090b5`. The Architecture Spine's accepted EventStore binding is now `3.110.0` at the exact tagged coordinate under the explicit Solution Architect decision at `2026-10-01T06:17:01Z`. Independent runtime/toolchain dispositions stay separate.

## Existing evidence and its limits

- **Pin agreement.** At candidate Builds, `Props/Directory.Packages.props`, `SupportedPlatformPins.EventStoreVersion`, and the module-manifest schema all pin EventStore `3.110.0`. Projects `tools/release-packages.json` also expects `Hexalith.EventStore.Contracts` `3.110.0`. This establishes coordinate agreement only.
- **Published Builds tool availability.** The existing [P0 planning preflight](evidence/6-1-p0-20261001-preflight.json) records a successful isolated-cache tool restore from NuGet.org and both `--version` commands reporting `4.29.1+21ce044ab465ccb2adab58b3d66e394ffbecf3c2`. Signed remote archive hashes differ from the unsigned release inventory. Payload equivalence, persisted qualification, cleanup/rollback, a checked-in consumer pin, and owner acceptance were not proven. This is an existing planning observation, not current P0 qualification.
- **Older package evidence.** EventStore's [3.109.0 package record](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3109/public-packages.json) and [probe](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3109/rollback-probe/README.md) remain scoped to 3.109.0. They cannot verify 3.110.0 archives, signatures, consumption, or downgrade behavior. No current 14-package `3.110.0` archive/signature/consumer replay is supplied by this packet.
- **Metadata rollback is unverified.** `EventEnvelope` and `SnapshotRecord` source blobs match between `v3.70.1` and `v3.110.0`, but `AggregateMetadata` does not: rollback blob `8c55205276890385d3725a18f78671b78871b4c6`, selected tag blob `8d694d6617da42217cc3962eb757207f1b3fb0e2`. The selected tag carries `RetainedFloor`, a preserved three-argument constructor, and a three-member deconstruction overload. Source compatibility accommodations do not prove that `3.70.1` safely reads and replays metadata or streams written by `3.110.0`, particularly retained/trimmed streams. The old envelope/snapshot probe never exercised this metadata shape.
- **Actor/domain replay and operational rollback are unverified.** No `3.110.0` to `3.70.1` actor/domain replay, mixed-version call, API downgrade, or stop-writers/full-backup/restore rehearsal is presented. The old disposable probe used Dapr `state.redis` on runtime `1.18.4`, not the G-6 PostgreSQL/runtime `1.18.2` topology.
- **CI evidence coverage is stale.** The existing `p1r-candidate-evidence` lane still fetches `v3.109.0` and replays its historical package record; it supplies no current `3.110.0` evidence. That lane remains historical and is not changed by this P1R acceptance.
- **Checkout compatibility is unverified.** The 13-commit, 43-source-path difference is measured between the tagged source and the committed EventStore gitlink. Neither successful tag CI nor older checkout-source test binaries proves interchangeability with the current checkout or runtime behavior of the tagged packages. The existing uncommitted EventStore test/documentation changes are separate observations and are not included in this committed-source comparison.

## Current G-6 run — failed pending candidate

The approved 2026-10-01 spec/tuple/run now binds [attempt 3](qualification-evidence/g-6-runtime-toolchain-20261001/attempt-3/packet.json), SHA-256 `724939520f2d8850073fd0091ffb7f0ada88e87231bb6ff457063bacb562a6bd`. SDK `10.0.401`, Aspire `13.6.0`, Toolkit `13.6.0-beta.910`, Dapr CLI/runtime/.NET `1.18.0`/`1.18.2`/`1.18.10`, Fluent UI `5.0.0`, NSubstitute `6.2.0` and Fluxor `6.11.0` are run-approved, including the explicit Toolkit prerelease and Dapr exceptions. Final G-6 acceptance is absent; packet status remains pending with `technicalValidity=false` and `usableAsPrerequisite=false`.

Actual results are 1 failed qualifier, 33 passed support cases across 21 selectors and 21 passed fixture controls, all with zero skips. The namespace-isolated retry still receives HTTP 404 from `sample/process` and HTTP 500 at concurrent-writer admission. Five AppHosts fail upstream source/UI requirements; Conversations/EventStore/Folders/Builds Module/Platform and the separate McpCli consumer build. Platform file-based Debug consumption uses explicit NuGet directives; this does not qualify checkout-source runtime behavior. All prior failed attempts remain preserved, including an explicit actual-count supplement for the first producer's placeholders.

The status-aware candidate gate exits `1` with two checks: stale Memories resolved `Aspire.Hosting` assets after its source guard stopped restoration, and actual EventStore checkout HEAD `96a6041c5d63a933a58f1df09f7fad123c9ada41` versus root gitlink `6dededdecd62dd6dc6d1f15810108d860ec70c8f`. CI Builds execution now equals `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`. Dirty source and missing exact committed closure independently prevent immutable acceptance. Shared container/binary snapshots are unchanged, all four owned containers are removed, owned process groups stop, and scratch is removed.

This failed checkout qualification supplies no current archive, metadata rollback, actor replay, mixed-version compatibility or operational rollback proof for published EventStore `3.110.0`. P1R acceptance and all downstream states remain unchanged.

## Superseded G-6 observation and containment

The superseded [2026-09-29 G-6 packet](qualification-evidence/g-6-runtime-toolchain-20260929/packet.json), SHA-256 `f62a8f661c181f5f2554bfc21bf0da918fcf6448199186bbf5a8e8d9a2fb7ac2`, is still pending and is stale for this candidate. The current catalog has Toolkit `13.6.0-beta.910` and Fluent UI `5.0.0`; neither has current G-6 acceptance. Historical G-6 approval does not apply to those changed pins.

Before the current qualification changes, the superseded gate command exited `1` with **seven failed checks**:

```bash
python3 tests/tools/run_g6_candidate_gate.py --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json
```

1. Validator rejects `Central package pin drift: CommunityToolkit.Aspire.Hosting.Dapr`.
2. Builds: packet `ac58d02cd69f4c74cc71fc05430983d51b58fd84`; root gitlink `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`.
3. Conversations: packet `a0a6fa288a87ba7971113de420334fc7478d7123`; root gitlink `8519c24bfb0a04da8710c03174ce3576898e3e03`.
4. EventStore: packet `5b9494835fcba6152789653add8f84bee5666035`; root gitlink `6dededdecd62dd6dc6d1f15810108d860ec70c8f`.
5. FrontComposer: packet `ccb75d5454b874fff9e5ccddb4598b8bc3860631`; root gitlink `48f7dfef920e8217e5c6221f364f0a1e0f61f387`.
6. Tenants: packet `55f3dc63b6ce10bb0afdf929d026fa07cffd9105`; root gitlink `54ceb3e1d50d3fc7bf1846e08cd2835828adfcdf`.
7. CI executes Builds `212583e08c7b6db22c7ccee881ad11699e1f7522`; root gitlink is `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`.

This P1R acceptance cannot itself make G-6 current. A fresh valid G-6 capture and its independent owner acceptance remain necessary before current prerequisite usability can become true. No packet status is flipped, source binding waived, or gate weakened here.

## Four-role acceptance

Each role explicitly accepted **the candidate tuple above**, bound to `_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json` and its `#/selected` tuple. Jérôme Piquot stated, “I Jérôme Piquot am the Owner for all roles and I accept,” in response to the exact packet decision request at `2026-10-01T06:17:01Z`. The fixed record names him for each role; this acceptance includes the recorded release-bypass and verification limitations. G-6 acceptance is outside that decision.

| Role | Current decision / named approver | Review scope |
| --- | --- | --- |
| EventStore Owner | **Accept / Jérôme Piquot / 2026-10-01T06:17:01Z** | Exact tagged package family; successful tag CI separately from release bypass and Commitlint proof; unverified current archives, `RetainedFloor` metadata rollback, actor/domain replay, and tag/checkout compatibility |
| Builds Owner | **Accept / Jérôme Piquot / 2026-10-01T06:17:01Z** | Exact `4.29.1` / `21ce044ab465ccb2adab58b3d66e394ffbecf3c2` pair; release-time Builds `22a578b576a515d2af214fe81859447fffc97981`; tool archive/persisted-qualification limits and CI execution mismatch |
| Solution Architect | **Accept / Jérôme Piquot / 2026-10-01T06:17:01Z** | Exact candidate conformance and an explicit EventStore Stack rebinding decision; preserved `3.70.1` rollback; independent toolchain dispositions |
| Test Architect | **Accept / Jérôme Piquot / 2026-10-01T06:17:01Z** | Exact provenance and remaining archive, metadata rollback, actor replay, checkout compatibility, and stale G-6 evidence limits |

**Missing P1R decisions: none.** The transition is authorized by the four explicit named accepts. Missing, rejected, malformed, or mixed-tuple records still fail closed; the archived historical record is preserved. Current G-6 acceptance remains absent.

## Coordinated acceptance transition

The fixed record, guard selected tuple and focused controls, minimal owner-acceptance `SPEC.md` and qualification contract, Architecture Spine EventStore binding under the explicit Solution Architect decision, and sprint/P0/DW-35/DW-68 representations are coordinated for the accepted exact tuple. The record uses `2026-10-01T06:17:01Z` and the unchanged `hexalith.projects.p1r-acceptance.v1` schema; no evidence bundles or schema fields are added. The rollback coordinates are preserved.

Validate a separate staging tree with matching companion files and its fixed record using `--validate-index --sprint-status <staging-sprint> --deferred-work <staging-ledger> --p0-artifact <staging-p0> --workspace-root <staging-root>` before replacing the index. Missing/rejected roles, malformed records, mixed coordinates, and downstream closure must fail closed.

P1R, P0 Stage 1, DW-35, and DW-68 retain their already-done statuses for the newly accepted exact tuple. Current P1R usability remains false. P0 stages 2–7, P2–P4, readiness `NOT_READY`, Story 6.1 `blocked`, and dependent work retain their existing states. This packet grants no deployment, publication, dependency update, commit, or push authority.


Post-capture contract correction allows later metadata-only Projects evidence/acceptance commits through ancestor and committed-source checks; bound runtime/build/test/configuration bytes and exact submodule HEAD/gitlinks remain mandatory. The final accepted positive control and 40 current mutation controls pass. The retained attempt 3 packet remains failed and additionally stale against those validator/runner/test bytes; no packet/source bindings were rewritten. Final candidate validation rejects the current mutation-test artifact hash (`test_runtime_toolchain_v2.py`), and the EventStore HEAD/gitlink mismatch remains. Current usability stays false.
