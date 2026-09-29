# G-6 runtime/toolchain qualification candidate — 2026-09-29

**Status: pending exact-packet owner acceptance.** On 2026-09-29 Jérôme Piquot, as sole owner, approved the Toolkit `13.5.1-beta.770` prerelease exception and an isolated Dapr `1.18.2` two-sidecar rerun, with the local control-plane containers swapped and then restored by exact ID. That direction authorizes the run only; it does not accept this packet's hash. The accepted 2026-09-06 packet ([g-6-runtime-toolchain](../g-6-runtime-toolchain/packet.json)) and the superseded 2026-09-27 packet ([g-6-runtime-toolchain-20260927](../g-6-runtime-toolchain-20260927/packet.json)) are unchanged. The accepted EventStore `3.106.0` P1R record remains authoritative.

## Recapture after review loop 2

This packet replaces the earlier pending packet in this folder, raw SHA-256 `7e7d7ea2f39256b829367c56bf289ff97889e190f6e7481db35def986d53a5bb` (`capturedUtc=2026-09-29T16:56:41Z`, bound to Projects `bd180b66b81b2893f6249cc282929b9cf62e5557` and Builds `a912464e5f0294ccfb34a2a29d6d2072bafb6116`). That packet is **superseded**; it was never accepted, so recapturing it is not a reseal of an accepted or superseded packet. Review loop 2 changed the bound G-6 validator, self-test and schema (Builds owner commit `3f44bc01df6057cd265adb3f800e68b31e6185f0`), the Projects CI jobs and workflow gate, and found that nothing tied the packet's submodule revisions to the root gitlinks. The recapture repeated every command after those commits, including a second isolated Dapr `1.18.2` two-sidecar run with the same control-plane swap-and-restore procedure.

## Exact binding

- [Packet](packet.json): raw and normalized-LF SHA-256 `e3090bd938ccbea7ee63ddb7427ce75ab815397f6eaa6183ab97ec2da0f8b82b`, `status=pending`, `capturedUtc=2026-09-29T17:42:46Z`.
- Baseline: `references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json`, SHA-256 `260d1abae7d9eba358c5eaf749f12a0f46cf6adf5b0b4830d423615908820fbd` (unchanged). Tuple: .NET SDK `10.0.401`, Aspire SDK/CLI `13.5.4`, Toolkit `13.5.1-beta.770`, Dapr CLI `1.18.0`, Dapr runtime `1.18.2`, Dapr .NET `1.18.10`, Fluent UI `5.0.0-rc.5-26219.1`, NSubstitute `6.2.0`, Fluxor `6.11.0`; approval `2026-09-29` by `Jérôme Piquot` for the Builds, Platform and FrontComposer/Web roles.
- [Source state](source-state.json): 72 files across nine repositories (the same paths as the replaced packet), normalized-LF and raw SHA-256 `d883dfe75a8d62dfe80fde41fab57351dabb10f8a2fdcffd211690c84635b791`. Each revision below is the root gitlink recorded by Projects gitlinks commit `d580907210a134297675fcf0b8aa4a8923ed5999`, which is also the Projects revision:

| Repository | Revision |
| --- | --- |
| Hexalith.Projects | `d580907210a134297675fcf0b8aa4a8923ed5999` |
| Hexalith.Builds | `3f44bc01df6057cd265adb3f800e68b31e6185f0` |
| Hexalith.Conversations | `a0a6fa288a87ba7971113de420334fc7478d7123` |
| Hexalith.EventStore | `5b9494835fcba6152789653add8f84bee5666035` |
| Hexalith.Folders | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` |
| Hexalith.FrontComposer | `ccb75d5454b874fff9e5ccddb4598b8bc3860631` |
| Hexalith.Memories | `289773387e0c6b665b669031616c1e84cc079297` |
| Hexalith.Parties | `60b9836ea23151c5319dd06fd3deb80122f7abc3` |
| Hexalith.Tenants | `55f3dc63b6ce10bb0afdf929d026fa07cffd9105` |

- [Source closure](source-closure.json): `allBoundFilesCommitted=true` and `allRevisionsEqualRootGitlinks=true`. Every bound file is clean in its worktree and its `HEAD` blob hashes to the bound value. Builds `3f44bc01df6057cd265adb3f800e68b31e6185f0` changes only the G-6 validator, self-test, schema, README and P0 record relative to `a912464e5f0294ccfb34a2a29d6d2072bafb6116`; EventStore `5b9494835fcba6152789653add8f84bee5666035` is unchanged from the replaced packet.
- [Pin audit](pin-audit.json): all 44 baseline declarations match (9 `global.json`, 8 AppHost SDK declarations, 21 literal workflow/script pins, 6 central catalog pins). The tuple's Dapr CLI `1.18.0`, Dapr runtime `1.18.2` and Aspire CLI `13.5.4` each appear among the audited literal pins, as the validator now requires.

## Validator results at the committed gitlinks

```text
python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json --candidate
  exit 0; G6-EVIDENCE-CANDIDATE-VALID
(same command without --candidate)
  exit 1; G6-EVIDENCE-INVALID: Packet status must be accepted
```

Candidate mode performs every artifact, source, command, count, topology, lifecycle, approval-authority and approval-before-capture check with a distinct marker; it cannot satisfy the accepted-only gate. In Projects CI, `project-gates` runs the exact candidate step and then the FrontComposer and OpenAPI gates, so it passes or fails on those checks. The accepted-only validator runs in the separate `g6-acceptance` job, which no other job needs and which fails until a named owner accepts this exact packet; owner acceptance flips `status` to `accepted` and removes the candidate step in one change. The workflow gate compares each G-6 step and job as a complete block and requires every submodule revision in this packet to equal its root gitlink.

## Every command and its real exit code

The validator requires exactly the 15 purposes in [commands.json](commands.json). Additional commands are recorded in the files named below, not folded into a passing entry.

| Command | Exit | Result |
| --- | --- | --- |
| Install exact Dapr tuple and swap control plane | 0 | CLI `1.18.0` archive checksum passed; `daprd` `1.18.2` SHA-256 `e730688f…f4e6`; image `sha256:5a5b6be9…938d`. The first `docker create` omitted `--entrypoint` and failed (`no command specified`) before any copy; the entrypoint form then succeeded. |
| Observe tool versions | 0 | .NET SDK 10.0.401; Aspire CLI 13.5.4 (`13.5.4+9c1b401dd67746739044f68959cbf4d3d7af93a6`); Dapr CLI 1.18.0; Dapr runtime 1.18.2 |
| Builds Module and Evidence test assemblies (Release) | 0 | Module 214 passed; Evidence 107 passed; 0 failed; 0 skipped |
| G-6 mutation controls | 0 | 22 scenarios for each of 3 baselines; 18 baseline-drift controls; 42 authority controls; both historical baseline SHA-256 pins held |
| OQ8 PostgreSQL two-sidecar stop/restart qualifier | 0 | 1 passed; 0 failed; 0 skipped (23.5 s); checkout-source binaries, not the published archives |
| 21 deterministic support selectors | 0 | 21 selectors; 33 passed cases; 0 failed; 0 skipped |
| Strict EventStore capture validation | 0 | `OQ8 capture validation passed.` |
| Root workflow gate | 0 | 5 workflow files; exact candidate step in `project-gates`, separate `g6-acceptance` job that no job needs, `p1r-candidate-evidence` job, Builds execution SHA equal to the root Builds gitlink, and every packet submodule revision equal to its root gitlink |
| Projects CI restore and Release build (package mode) | 0 | 0 warnings, 0 errors |
| Projects Integration tests | 0 | 27 passed; 0 failed; 0 skipped |
| Managed restart smoke credential preflight | **1** | Stopped before AppHost start: `TEST_USER_PASSWORD` was deliberately unset, as the command contract requires exit `1`. No managed restart smoke ran. |
| AppHost Toolkit restore (eight force-evaluated restores) | 0 | All eight resolve `13.5.1-beta.770` from nuget.org ([toolkit-resolution.json](toolkit-resolution.json)) |
| Conversations and FrontComposer pin selectors | 0 | 1/1 each |
| Shared workflow test-platform contracts | 0 | 169 assertions |
| Broad package-exception inventory | **1** | 14 pre-existing drifts (listed in `commands.json`) |
| Conversations, Memories, Tenants, EventStore, Folders AppHost builds (package mode) | 0 each | 0 warnings, 0 errors ([apphost-builds.json](apphost-builds.json)) |
| Projects AppHost (inside the Projects CI solution build) | 0 | 0 warnings, 0 errors |
| Parties AppHost build, package and default modes | **1** each | 3 `CS0234` errors in each mode: Parties.UI uses `FcModuleLandingPage` and `FrontComposerRouteOptions`, which the published FrontComposer `4.5.0` packages it restores do not contain |
| FrontComposer AppHost build, package and default modes | **1** each | 52 errors in each mode (`CS0103`, `CS0234`, `CS0246`, `MSB9008`, `RZ10012`): missing Parties/EventStore symbols and an unresolved EventStore project-reference path |

The EventStore LiveSidecar and Server test binaries and the Builds test binaries were rebuilt (Release, `--no-incremental`, isolated NuGet cache) after Builds commit `3f44bc01df6057cd265adb3f800e68b31e6185f0` and Projects gitlinks commit `d580907210a134297675fcf0b8aa4a8923ed5999`, from the committed sources at the gitlinks. Every test execution, the qualifier, the validator, the workflow gate, the Projects build and every AppHost restore/build ran after those commits.

**Scope of the OQ8 pass.** The qualifier and the 21 support selectors ran test binaries built from EventStore checkout source `5b9494835fcba6152789653add8f84bee5666035` (its `src` is identical to `489e5d76253f68c6cf53c443aa8755ad059e621e`, 28 commits after tag `v3.109.0`), not from the published `3.109.0` package archives. The pass qualifies the runtime/toolchain tuple with that checkout source; it is not runtime evidence for the published package family. The Projects Release build and Integration tests consumed the published `3.109.0` packages.

**Restart smoke.** The managed restart smoke credential preflight is deliberately run without `TEST_USER_PASSWORD` (`env -u TEST_USER_PASSWORD`), because the G-6 command contract requires that preflight to exit `1`. It stopped before AppHost start, so no managed AppHost restart smoke ran and no smoke result is claimed.

## Isolation and restoration

The qualifier used an isolated `HOME` (Dapr CLI `1.18.0` from the checksum-verified release archive, `daprd` `1.18.2` SHA-256 `e730688f06b9b7cea0d616a7dde8fc0124cb6cc8d6dae149192422c7ea67f4e6` copied from `ghcr.io/dapr/dapr:1.18.2`), an isolated Aspire CLI `13.5.4` tool path, and a fresh isolated NuGet cache with `CI=true`, because the shared cache holds locally packed Hexalith packages. For the run, the running `dapr_placement`/`dapr_scheduler` `1.18.4` containers were stopped and renamed, and temporary `ghcr.io/dapr/dapr:1.18.2` containers took their names and host ports; the temporary scheduler kept its data on tmpfs, so the original scheduler volume was not mounted. [Control-plane restoration](control-plane-restoration.json) shows the original container IDs `9d57dbed…b523` and `baede0ae…e077` renamed back and running, the temporary containers removed, no fixture PostgreSQL container, and no `daprd` process left. No retained domain data was rewritten; raw CTRF files were not retained.

## Boundaries that remain open

- `dispositions.attempts[0].accepted=true` only selects the single passing technical attempt, as the schema requires. It is not owner acceptance. There was no failed qualifier attempt; [no-failed-attempt.json](no-failed-attempt.json) fills the required slot.
- Two AppHosts do not compile at the committed gitlinks: Parties (3 errors: its UI source uses FrontComposer types that are not in the published FrontComposer `4.5.0` packages it restores) and FrontComposer (52 errors in both modes). Neither error set names the Toolkit, but full AppHost compatibility is unverified for those two owners.
- The OQ8 pass covers checkout-source test binaries, not the published `3.109.0` archives, and no managed AppHost restart smoke ran.
- The broad package-exception inventory still exits `1` with 14 pre-existing drifts, and five AppHosts still declare Aspire SDK `13.5.3` while three declare `13.5.4`.
- The limitation files ([source-closure.json](source-closure.json), [apphost-builds.json](apphost-builds.json), [toolkit-resolution.json](toolkit-resolution.json), [pin-audit.json](pin-audit.json), [control-plane-restoration.json](control-plane-restoration.json)) are not among the 14 artifact kinds the packet hash binds; they are committed beside it.
- Builds CI at exact SHA `3f44bc01df6057cd265adb3f800e68b31e6185f0` and Projects CI at the final commit can only be recorded after the owner pushes; neither has run.
- The exact packet hash above needs a named post-run G-6 owner decision. No G-4, G-5, P1R, P0 Stage 6, Story 6.1, deployment, or release acceptance follows from this technical candidate.

## Historical packets

The historical baselines keep their bound bytes at Builds `3f44bc01df6057cd265adb3f800e68b31e6185f0`, and the self-test now pins them: `runtime-toolchain-baseline.json` hashes to `525615c65ada8cabf5a6911a374bb22b62f6a3c1bfa6c91f7245312da9720265` (bound by the accepted 2026-09-06 packet) and `runtime-toolchain-baseline-2026-09-27.json` to `b9aa6791effe78cbb9bf405cd8384b81a6d750f996fabf8f79b7cd1c3bedad01` (bound by the superseded 2026-09-27 packet). The validator reproduces the historical 2026-09-06 command as exit `1` with `Central package pin drift: CommunityToolkit.Aspire.Hosting.Dapr`, the result recorded before the drift, and the 2026-09-27 `--candidate` command as exit `1` with the same message, because both approved Toolkit pins (`.757`, `.767`) differ from the committed `.770` catalog. Neither packet is resealed.
