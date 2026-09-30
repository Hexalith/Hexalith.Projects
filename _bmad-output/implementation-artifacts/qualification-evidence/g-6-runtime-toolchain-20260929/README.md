# G-6 runtime/toolchain qualification candidate — 2026-09-29

**Status: pending exact-packet owner acceptance.** On 2026-09-29 Jérôme Piquot, as sole owner, approved the Toolkit `13.5.1-beta.770` prerelease exception and an isolated Dapr `1.18.2` two-sidecar rerun, with the local control-plane containers swapped and then restored by exact ID. That direction authorizes the run only; it does not accept this packet's hash. The accepted 2026-09-06 packet ([g-6-runtime-toolchain](../g-6-runtime-toolchain/packet.json)) and the superseded 2026-09-27 packet ([g-6-runtime-toolchain-20260927](../g-6-runtime-toolchain-20260927/packet.json)) are unchanged. The accepted EventStore `3.106.0` P1R record remains authoritative. **Projects releases stay blocked while this packet is pending:** `release.yml` `verify-source` requires a successful push run of `ci.yml`, and the `g6-acceptance` job fails every run until a named owner accepts this exact packet.

## Recapture after review loop 3

This packet replaces the earlier pending packet in this folder, raw SHA-256 `e3090bd938ccbea7ee63ddb7427ce75ab815397f6eaa6183ab97ec2da0f8b82b` (`capturedUtc=2026-09-29T17:42:46Z`, bound to Projects `d580907210a134297675fcf0b8aa4a8923ed5999` and Builds `3f44bc01df6057cd265adb3f800e68b31e6185f0`), which had itself replaced `7e7d7ea2f39256b829367c56bf289ff97889e190f6e7481db35def986d53a5bb` (`capturedUtc=2026-09-29T16:56:41Z`, Projects `bd180b66b81b2893f6249cc282929b9cf62e5557`, Builds `a912464e5f0294ccfb34a2a29d6d2072bafb6116`). Both are **superseded**; neither was accepted, so recapturing is not a reseal of an accepted or superseded packet. Review loop 3 changed the bound G-6 validator and self-test (Builds owner commit `86cda4703eeff4621098baad3cee82348d1522ca`: tuple CLI/runtime values tied to pins of the same role, the tuple Aspire SDK required among the audited AppHost SDK pins, and the mutation-controls outcome grounded in the self-test's real result line), moved the packet-revision-equals-root-gitlink comparison from the workflow gate into the G-6 jobs, and changed the P1R evidence replay. The recapture repeated every command after those commits, including a third isolated Dapr `1.18.2` two-sidecar run with the same control-plane swap-and-restore procedure.

## Exact binding

- [Packet](packet.json): raw and normalized-LF SHA-256 `26d7a6e0d9b7aaa3136864529e261d36f9346a1166b08b31d6b8edda51d52000`, `status=pending`, `capturedUtc=2026-09-30T06:34:07Z`.
- Baseline: `references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json`, SHA-256 `260d1abae7d9eba358c5eaf749f12a0f46cf6adf5b0b4830d423615908820fbd` (unchanged). Tuple: .NET SDK `10.0.401`, Aspire SDK/CLI `13.5.4`, Toolkit `13.5.1-beta.770`, Dapr CLI `1.18.0`, Dapr runtime `1.18.2`, Dapr .NET `1.18.10`, Fluent UI `5.0.0-rc.5-26219.1`, NSubstitute `6.2.0`, Fluxor `6.11.0`; approval `2026-09-29` by `Jérôme Piquot` for the Builds, Platform and FrontComposer/Web roles.
- [Source state](source-state.json): 73 files across nine repositories: the replaced packet's 72 paths plus `tests/tools/check_g6_packet_gitlinks.py`, which the G-6 jobs now run. Normalized-LF and raw SHA-256 `4f89deb5623d470f4c96c4b18d80c76ec7a80e1929b1ff35873678d7534cafd9`. Each submodule revision below equals the root gitlink recorded by Projects gitlinks commit `a263e0d0fcac28e28a01a00113a57f7fae19ac20`; the Projects revision is that commit itself, which this packet's capture and records follow:

| Repository | Revision |
| --- | --- |
| Hexalith.Projects | `a263e0d0fcac28e28a01a00113a57f7fae19ac20` |
| Hexalith.Builds | `86cda4703eeff4621098baad3cee82348d1522ca` |
| Hexalith.Conversations | `a0a6fa288a87ba7971113de420334fc7478d7123` |
| Hexalith.EventStore | `5b9494835fcba6152789653add8f84bee5666035` |
| Hexalith.Folders | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` |
| Hexalith.FrontComposer | `ccb75d5454b874fff9e5ccddb4598b8bc3860631` |
| Hexalith.Memories | `289773387e0c6b665b669031616c1e84cc079297` |
| Hexalith.Parties | `60b9836ea23151c5319dd06fd3deb80122f7abc3` |
| Hexalith.Tenants | `55f3dc63b6ce10bb0afdf929d026fa07cffd9105` |

- [Source closure](source-closure.json): `allBoundFilesCommitted=true` and `allRevisionsEqualRootGitlinks=true`. Every bound file is clean in its worktree and its `HEAD` blob hashes to the bound value. Builds `86cda4703eeff4621098baad3cee82348d1522ca` changes only the G-6 validator, self-test, README and P0 record relative to `3f44bc01df6057cd265adb3f800e68b31e6185f0`; EventStore `5b9494835fcba6152789653add8f84bee5666035` is unchanged from the replaced packets.
- [Pin audit](pin-audit.json): all 44 baseline declarations match (9 `global.json`, 8 AppHost SDK declarations, 21 literal workflow/script pins, 6 central catalog pins). The tuple's Dapr CLI `1.18.0`, Dapr runtime `1.18.2` and Aspire CLI `13.5.4` each equal every audited literal pin of the same role (5 Dapr CLI, 8 Dapr runtime and 2 Aspire CLI pins); the 6 pins whose text names no role (`version:`, `default:`) ground nothing. The tuple Aspire SDK `13.5.4` is one of the audited AppHost SDK pins (`13.5.3`, `13.5.4`).

## Validator results at the committed gitlinks

```text
python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json --candidate
  exit 0; G6-EVIDENCE-CANDIDATE-VALID
(same command without --candidate)
  exit 1; G6-EVIDENCE-INVALID: Packet status must be accepted
python3 tests/tools/check_g6_packet_gitlinks.py --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json
  exit 0; G6-PACKET-GITLINKS-EXACT: 8 submodule revisions equal their root gitlinks
```

Candidate mode performs every artifact, source, command, count, topology, lifecycle, approval-authority and approval-before-capture check with a distinct marker; it cannot satisfy the accepted-only gate. In Projects CI, `project-gates` runs the exact candidate step, the FrontComposer and OpenAPI gates, and last the exact G-6 gitlink step. The accepted-only validator runs in the separate `g6-acceptance` job, followed by the same gitlink step; no other job needs `g6-acceptance`, and it fails until a named owner accepts this exact packet. Owner acceptance flips `status` to `accepted` and removes the candidate step in one change. The gitlink comparison runs only in those two G-6 jobs: when a submodule gitlink moves after this capture, `project-gates` and `g6-acceptance` fail while `workflow-gates`, the `ci` build and test job and the earlier `project-gates` steps still run. The workflow gate checks the CI structure, including both exact gitlink steps and their hermetic fixtures, and still requires the executed Builds SHA to equal the root Builds gitlink.

## Every command and its real exit code

The validator requires exactly the 15 purposes in [commands.json](commands.json). Additional commands are recorded in the files named below, not folded into a passing entry.

| Command | Exit | Result |
| --- | --- | --- |
| Install exact Dapr tuple and swap control plane | 0 | CLI `1.18.0` archive checksum passed; `daprd` `1.18.2` SHA-256 `e730688f…f4e6`; image `sha256:5a5b6be9…938d`; `docker create --entrypoint /daprd` and `docker cp` succeeded on the first attempt; originals restored by exact ID |
| Observe tool versions | 0 | .NET SDK 10.0.401; Aspire CLI 13.5.4 (`13.5.4+9c1b401dd67746739044f68959cbf4d3d7af93a6`); Dapr CLI 1.18.0; Dapr runtime 1.18.2 |
| Builds Module and Evidence test assemblies (Release) | 0 | Module 214 passed; Evidence 107 passed; 0 failed; 0 skipped |
| G-6 mutation controls | 0 | `G6-EVIDENCE-MUTATIONS-PASSED: 24 scenarios for each of 3 baselines; 48 baseline-drift controls; 69 authority controls; 2 historical baseline SHA-256 pins` (the self-test's own result line, recorded verbatim) |
| OQ8 PostgreSQL two-sidecar stop/restart qualifier | 0 | 1 passed; 0 failed; 0 skipped (25.7 s); checkout-source binaries, not the published archives |
| 21 deterministic support selectors | 0 | 21 selectors; 33 passed cases; 0 failed; 0 skipped |
| Strict EventStore capture validation | 0 | `OQ8 capture validation passed.` |
| Root workflow gate | 0 | 5 workflow files; exact candidate step and final exact G-6 gitlink step in `project-gates`, separate `g6-acceptance` job (accepted-only step, then the gitlink step) that no job needs, `p1r-candidate-evidence` job, hermetic gitlink-check fixtures in `workflow-gates`, and Builds execution SHA equal to the root Builds gitlink; the packet-versus-gitlink comparison itself runs only in the G-6 jobs |
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

The EventStore LiveSidecar and Server test binaries and the Builds test binaries were rebuilt (Release, `--no-incremental`, isolated NuGet cache) after Builds commit `86cda4703eeff4621098baad3cee82348d1522ca` and Projects gitlinks commit `a263e0d0fcac28e28a01a00113a57f7fae19ac20`, from the committed sources at the gitlinks. Every test execution, the qualifier, the validator, the workflow gate, the Projects build and every AppHost restore/build ran after those commits.

**Scope of the OQ8 pass.** The qualifier and the 21 support selectors ran test binaries built from EventStore checkout source `5b9494835fcba6152789653add8f84bee5666035` (its `src` is identical to `489e5d76253f68c6cf53c443aa8755ad059e621e`, 28 commits after tag `v3.109.0`), not from the published `3.109.0` package archives. The pass qualifies the runtime/toolchain tuple with that checkout source; it is not runtime evidence for the published package family. The Projects Release build and Integration tests consumed the published `3.109.0` packages.

**Restart smoke.** The managed restart smoke credential preflight is deliberately run without `TEST_USER_PASSWORD` (`env -u TEST_USER_PASSWORD`), because the G-6 command contract requires that preflight to exit `1`. It stopped before AppHost start, so no managed AppHost restart smoke ran and no smoke result is claimed.

## Isolation and restoration

The qualifier used an isolated `HOME` (Dapr CLI `1.18.0` from the checksum-verified release archive, `daprd` `1.18.2` SHA-256 `e730688f06b9b7cea0d616a7dde8fc0124cb6cc8d6dae149192422c7ea67f4e6` copied from `ghcr.io/dapr/dapr:1.18.2`), an isolated Aspire CLI `13.5.4` tool path, and a fresh isolated NuGet cache with `CI=true` (546 packages, all from nuget.org), because the shared cache holds locally packed Hexalith packages. For the run, the running `dapr_placement`/`dapr_scheduler` `1.18.4` containers were stopped and renamed, and temporary `ghcr.io/dapr/dapr:1.18.2` containers took their names and host ports; the temporary scheduler kept its data on tmpfs, so the original scheduler volume was not mounted. [Control-plane restoration](control-plane-restoration.json) shows the original container IDs `9d57dbed…b523` and `baede0ae…e077` renamed back and running, the temporary containers removed, no fixture PostgreSQL container, and no `daprd` process left. No retained domain data was rewritten; raw CTRF files were not retained.

## Boundaries that remain open

- `dispositions.attempts[0].accepted=true` only selects the single passing technical attempt, as the schema requires. It is not owner acceptance. There was no failed qualifier attempt; [no-failed-attempt.json](no-failed-attempt.json) fills the required slot.
- Two AppHosts do not compile at the committed gitlinks: Parties (3 errors: its UI source uses FrontComposer types that are not in the published FrontComposer `4.5.0` packages it restores) and FrontComposer (52 errors in both modes). Neither error set names the Toolkit, but full AppHost compatibility is unverified for those two owners.
- The OQ8 pass covers checkout-source test binaries, not the published `3.109.0` archives, and no managed AppHost restart smoke ran.
- The broad package-exception inventory still exits `1` with 14 pre-existing drifts, and five AppHosts still declare Aspire SDK `13.5.3` while three declare `13.5.4`.
- The limitation files ([source-closure.json](source-closure.json), [apphost-builds.json](apphost-builds.json), [toolkit-resolution.json](toolkit-resolution.json), [pin-audit.json](pin-audit.json), [control-plane-restoration.json](control-plane-restoration.json)) are not among the 14 artifact kinds the packet hash binds; they are committed beside it.
- Builds CI at exact SHA `86cda4703eeff4621098baad3cee82348d1522ca` and Projects CI at the final commit can only be recorded after the owner pushes; neither has run.
- Projects releases stay blocked until owner acceptance, because every push CI run fails in `g6-acceptance` while this packet is pending.
- The exact packet hash above needs a named post-run G-6 owner decision. No G-4, G-5, P1R, P0 Stage 6, Story 6.1, deployment, or release acceptance follows from this technical candidate.

## Historical packets

The historical baselines keep their bound bytes at Builds `86cda4703eeff4621098baad3cee82348d1522ca`, and the self-test pins them: `runtime-toolchain-baseline.json` hashes to `525615c65ada8cabf5a6911a374bb22b62f6a3c1bfa6c91f7245312da9720265` (bound by the accepted 2026-09-06 packet) and `runtime-toolchain-baseline-2026-09-27.json` to `b9aa6791effe78cbb9bf405cd8384b81a6d750f996fabf8f79b7cd1c3bedad01` (bound by the superseded 2026-09-27 packet). The validator reproduces the historical 2026-09-06 command as exit `1` with `Central package pin drift: CommunityToolkit.Aspire.Hosting.Dapr`, the result recorded before the drift, and the 2026-09-27 `--candidate` command as exit `1` with the same message, because both approved Toolkit pins (`.757`, `.767`) differ from the committed `.770` catalog. The loop 3 pin-role and Aspire SDK rules pass for both historical baselines, so those results are unchanged. Neither packet is resealed.
