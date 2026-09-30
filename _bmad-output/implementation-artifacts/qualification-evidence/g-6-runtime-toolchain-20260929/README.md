# G-6 runtime/toolchain qualification candidate — 2026-09-29

**Status: pending exact-packet owner acceptance.** On 2026-09-29 Jérôme Piquot, as sole owner, approved the Toolkit `13.5.1-beta.770` prerelease exception and an isolated Dapr `1.18.2` two-sidecar rerun, with the local control-plane containers swapped and then restored by exact ID. That direction authorizes the run only; it does not accept this packet's hash. The accepted 2026-09-06 packet ([g-6-runtime-toolchain](../g-6-runtime-toolchain/packet.json)) and the superseded 2026-09-27 packet ([g-6-runtime-toolchain-20260927](../g-6-runtime-toolchain-20260927/packet.json)) are unchanged. The accepted EventStore `3.106.0` P1R record remains authoritative. **Projects releases stay blocked while this packet is pending:** `release.yml` `verify-source` requires a successful push run of `ci.yml`, and the `g6-acceptance` job fails every run until a named owner accepts this exact packet.

## Recapture after review loop 4

This packet replaces the earlier pending packet in this folder, raw SHA-256 `26d7a6e0d9b7aaa3136864529e261d36f9346a1166b08b31d6b8edda51d52000` (`capturedUtc=2026-09-30T06:34:07Z`, Projects `a263e0d0fcac28e28a01a00113a57f7fae19ac20`, Builds `86cda4703eeff4621098baad3cee82348d1522ca`), which had replaced `e3090bd938ccbea7ee63ddb7427ce75ab815397f6eaa6183ab97ec2da0f8b82b` (`capturedUtc=2026-09-29T17:42:46Z`, Projects `d580907210a134297675fcf0b8aa4a8923ed5999`, Builds `3f44bc01df6057cd265adb3f800e68b31e6185f0`), which had replaced `7e7d7ea2f39256b829367c56bf289ff97889e190f6e7481db35def986d53a5bb` (`capturedUtc=2026-09-29T16:56:41Z`, Projects `bd180b66b81b2893f6249cc282929b9cf62e5557`, Builds `a912464e5f0294ccfb34a2a29d6d2072bafb6116`). All three are **superseded**; none was accepted, so recapturing is not a reseal of an accepted or superseded packet. Review loop 4 changed the bound G-6 validator, self-test and schema (Builds owner commit `ac58d02cd69f4c74cc71fc05430983d51b58fd84`: every audited literal pin classified by one of twelve forms, including the `default:` workflow inputs by their enclosing input name and the `version:` inputs by their Dapr CLI setup step; an approval decision required for a support-table-listed Dapr pair; and the mutation-controls outcome required to be exactly the self-test's result line with every count). It moved every G-6 check into the status-aware `g6-candidate` job, added the G-6 document reference test and changed the P1R evidence replay. The recapture repeated every command after those commits, including a fourth isolated Dapr `1.18.2` two-sidecar run with the same control-plane swap-and-restore procedure.

## Exact binding

- [Packet](packet.json): raw and normalized-LF SHA-256 `f62a8f661c181f5f2554bfc21bf0da918fcf6448199186bbf5a8e8d9a2fb7ac2`, `status=pending`, `capturedUtc=2026-09-30T07:18:12Z`. This is the reviewed packet hash that an owner decision must name.
- Baseline: `references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json`, SHA-256 `260d1abae7d9eba358c5eaf749f12a0f46cf6adf5b0b4830d423615908820fbd` (unchanged). Tuple: .NET SDK `10.0.401`, Aspire SDK/CLI `13.5.4`, Toolkit `13.5.1-beta.770`, Dapr CLI `1.18.0`, Dapr runtime `1.18.2`, Dapr .NET `1.18.10`, Fluent UI `5.0.0-rc.5-26219.1`, NSubstitute `6.2.0`, Fluxor `6.11.0`; approval `2026-09-29` by `Jérôme Piquot` for the Builds, Platform and FrontComposer/Web roles.
- [Source state](source-state.json): 75 files across nine repositories: the replaced packet's 73 paths plus `tests/tools/run_g6_candidate_gate.py` and `tests/tools/test_g6_packet_references.py`, which the `g6-candidate` job runs. Normalized-LF and raw SHA-256 `1ba7b7d2124b3f98f3eacbd72425b44b8560d73dc42ed7c23248fbb5dafefa2e`. Each submodule revision below equals the root gitlink recorded by Projects gitlinks commit `9bacf3a6324f4b6402247289a1ed5a665ab91f0f`; the Projects revision is that commit itself, which this packet's capture and records follow:

| Repository | Revision |
| --- | --- |
| Hexalith.Projects | `9bacf3a6324f4b6402247289a1ed5a665ab91f0f` |
| Hexalith.Builds | `ac58d02cd69f4c74cc71fc05430983d51b58fd84` |
| Hexalith.Conversations | `a0a6fa288a87ba7971113de420334fc7478d7123` |
| Hexalith.EventStore | `5b9494835fcba6152789653add8f84bee5666035` |
| Hexalith.Folders | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` |
| Hexalith.FrontComposer | `ccb75d5454b874fff9e5ccddb4598b8bc3860631` |
| Hexalith.Memories | `289773387e0c6b665b669031616c1e84cc079297` |
| Hexalith.Parties | `60b9836ea23151c5319dd06fd3deb80122f7abc3` |
| Hexalith.Tenants | `55f3dc63b6ce10bb0afdf929d026fa07cffd9105` |

- [Source closure](source-closure.json): `allBoundFilesCommitted=true` and `allRevisionsEqualRootGitlinks=true`. Every bound file is clean in its worktree and its `HEAD` blob hashes to the bound value. Builds `ac58d02cd69f4c74cc71fc05430983d51b58fd84` changes only the G-6 validator, self-test, evidence schema, README and P0 record relative to `86cda4703eeff4621098baad3cee82348d1522ca`; EventStore `5b9494835fcba6152789653add8f84bee5666035` is unchanged from the replaced packets.
- [Pin audit](pin-audit.json): all 44 baseline declarations match (9 `global.json`, 8 AppHost SDK declarations, 21 literal workflow/script pins, 6 central catalog pins). Every literal pin sets exactly one role and equals the tuple value of that role: 9 Dapr CLI `1.18.0`, 10 Dapr runtime `1.18.2` and 2 Aspire CLI `13.5.4` pins over 22 active occurrences, including four `default:` inputs classified by their enclosing `dapr-version`/`dapr-runtime-version` workflow input and two `version:` pins classified by their Dapr CLI setup step. The tuple Aspire SDK `13.5.4` is one of the audited AppHost SDK pins (`13.5.3`, `13.5.4`).

## Validator and gate results at the committed gitlinks

```text
python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json --candidate
  exit 0; G6-EVIDENCE-CANDIDATE-VALID
(same command without --candidate)
  exit 1; G6-EVIDENCE-INVALID: Packet status must be accepted
python3 tests/tools/check_g6_packet_gitlinks.py --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json
  exit 0; G6-PACKET-GITLINKS-EXACT: 8 submodule revisions equal their root gitlinks
python3 tests/tools/run_g6_candidate_gate.py --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json
  exit 0; G6-GATE-PASSED: validator, packet gitlinks and Builds execution SHA (candidate mode, because the packet is pending)
python3 -m unittest tests/tools/test_g6_packet_references.py -v
  exit 0; 5 tests passed (this README, the 3.109.0 owner packet, the Architecture Spine and sprint-status.yaml quote the packet hash and gitlinks)
```

Candidate mode performs every artifact, source, command, count, topology, lifecycle, approval-authority and approval-before-capture check with a distinct marker; it cannot satisfy the accepted-only gate. In Projects CI every G-6 check runs in two jobs that need only `workflow-gates` and that no other job needs. The status-aware `g6-candidate` job runs `tests/tools/run_g6_candidate_gate.py` (the validator in `--candidate` mode while `packet.json` says `pending` and in accepted-only mode once it says `accepted`, then the packet-gitlink check, then the check that every CI `Hexalith.Builds` reference uses one SHA equal to the root Builds gitlink) and then the document reference test. The fail-closed `g6-acceptance` job runs the accepted-only validator and the packet-gitlink check. G-6 drift (a bound file, a submodule gitlink that moves after this capture, or a CI Builds execution SHA that differs from the Builds gitlink) therefore fails only those two jobs, while `workflow-gates`, the `ci` build and test job, `project-gates` (now only the FrontComposer and OpenAPI gates) and the scheduled `e2e` job still run. The workflow gate checks CI structure only: the exact `workflow-gates`, `g6-candidate`, `g6-acceptance` and `p1r-candidate-evidence` jobs, the `project-gates` steps, and that no job needs a G-6 job.

## Owner acceptance procedure (status flip only)

1. The G-6 Builds, Platform and FrontComposer/Web owners review this exact packet: reviewed SHA-256 `f62a8f661c181f5f2554bfc21bf0da918fcf6448199186bbf5a8e8d9a2fb7ac2` and the open boundaries below.
2. To accept it, one Projects commit changes only `"status": "pending"` to `"status": "accepted"` in [packet.json](packet.json) and adds a named decision record (approver, date, owner roles and the reviewed SHA-256 above) to `sprint-status.yaml` `qualification_gates.G-6`. It edits no bound file (every file in [source-state.json](source-state.json), including `ci.yml`, the workflow gate, both G-6 gate scripts, the Builds validator, self-test, schema and baseline) and no other packet artifact.
3. CI then passes both G-6 jobs without further change: `g6-candidate` runs the validator in accepted-only mode, `g6-acceptance` passes, and the reference test still finds the reviewed hash because it hashes `packet.json` with the status member restored to `pending`. The accepted file's own raw SHA-256 differs from the reviewed hash only by that status member.
4. A rejection leaves the packet pending; a new capture replaces it and is recorded as the new reviewed hash.

## Push order and required checks

- **Push order.** Builds `ac58d02cd69f4c74cc71fc05430983d51b58fd84` (owner commits `a912464e5f0294ccfb34a2a29d6d2072bafb6116`, `3f44bc01df6057cd265adb3f800e68b31e6185f0`, `86cda4703eeff4621098baad3cee82348d1522ca` and `ac58d02cd69f4c74cc71fc05430983d51b58fd84`) and EventStore `5b9494835fcba6152789653add8f84bee5666035` are on no remote branch yet. Push Builds and EventStore before Projects, then Projects `main` (gitlinks commit `9bacf3a6324f4b6402247289a1ed5a665ab91f0f` and the capture commit that follows it). Where a remote `main` has advanced, integrate it with a merge commit (or a pull request merged with a merge commit); never squash or rebase the bound commits, because the Projects gitlinks, the CI Builds execution SHA and this packet name those exact SHAs, and a rewritten or unpushed SHA fails submodule initialization in every CI job.
- **Branch protection.** Require `Validate workflow policy` (`workflow-gates`), the `ci` reusable-workflow jobs, `Projects generated-artifact gates` (`project-gates`) and `G-6 runtime/toolchain packet (status-aware)` (`g6-candidate`). Do not require `G-6 owner acceptance (accepted-only)` (`g6-acceptance`): it fails by design until owner acceptance and blocks releases through `release.yml` instead. Do not require `Scheduled managed AppHost E2E` (schedule-only) or `P1R candidate evidence replay` (it reads live GitHub state and is updated or retired on the triggers in its docstring); a failure of either still makes the push run unsuccessful and so blocks releases.

## Every command and its real exit code

The validator requires exactly the 15 purposes in [commands.json](commands.json). Additional commands are recorded in the files named below, not folded into a passing entry.

| Command | Exit | Result |
| --- | --- | --- |
| Install exact Dapr tuple and swap control plane | 0 | CLI `1.18.0` archive checksum passed; `daprd` `1.18.2` SHA-256 `e730688f…f4e6`; image `sha256:5a5b6be9…938d`; `docker create --entrypoint /daprd` and `docker cp` succeeded on the first attempt; originals restored by exact ID |
| Observe tool versions | 0 | .NET SDK 10.0.401; Aspire CLI 13.5.4 (`13.5.4+9c1b401dd67746739044f68959cbf4d3d7af93a6`); Dapr CLI 1.18.0; Dapr runtime 1.18.2 |
| Builds Module and Evidence test assemblies (Release) | 0 | Module 214 passed; Evidence 107 passed; 0 failed; 0 skipped |
| G-6 mutation controls | 0 | `G6-EVIDENCE-MUTATIONS-PASSED: 30 scenarios for each of 3 baselines; 48 baseline-drift controls; 141 authority controls; 2 historical baseline SHA-256 pins` (the self-test's own result line, recorded verbatim) |
| OQ8 PostgreSQL two-sidecar stop/restart qualifier | 0 | 1 passed; 0 failed; 0 skipped (26.9 s); checkout-source binaries, not the published archives |
| 21 deterministic support selectors | 0 | 21 selectors; 33 passed cases; 0 failed; 0 skipped |
| Strict EventStore capture validation | 0 | `OQ8 capture validation passed.` |
| Root workflow gate | 0 | 5 workflow files, CI structure only: exact `workflow-gates` job (every hermetic fixture step an exact blocking block), exact status-aware `g6-candidate` and fail-closed `g6-acceptance` jobs that need only `workflow-gates` and that no job needs, `project-gates` with only the FrontComposer and OpenAPI gates, `e2e` needing no G-6 job, the `p1r-candidate-evidence` job, and every CI Builds reference at one SHA; the validator, gitlink and Builds execution-SHA checks run only in the G-6 jobs |
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

The EventStore LiveSidecar and Server test binaries and the Builds test binaries were rebuilt (Release, `--no-incremental`, isolated NuGet cache) after Builds commit `ac58d02cd69f4c74cc71fc05430983d51b58fd84` and Projects gitlinks commit `9bacf3a6324f4b6402247289a1ed5a665ab91f0f`, from the committed sources at the gitlinks. Every test execution, the qualifier, the validator, the workflow gate, the Projects build and every AppHost restore/build ran after those commits.

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
- Builds CI at exact SHA `ac58d02cd69f4c74cc71fc05430983d51b58fd84` and Projects CI at the final commit can only be recorded after the owner pushes; neither has run.
- Projects releases stay blocked until owner acceptance, because every push CI run fails in `g6-acceptance` while this packet is pending.
- The exact packet hash above needs a named post-run G-6 owner decision. No G-4, G-5, P1R, P0 Stage 6, Story 6.1, deployment, or release acceptance follows from this technical candidate.

## Historical packets

The historical baselines keep their bound bytes at Builds `ac58d02cd69f4c74cc71fc05430983d51b58fd84`, and the self-test pins them: `runtime-toolchain-baseline.json` hashes to `525615c65ada8cabf5a6911a374bb22b62f6a3c1bfa6c91f7245312da9720265` (bound by the accepted 2026-09-06 packet) and `runtime-toolchain-baseline-2026-09-27.json` to `b9aa6791effe78cbb9bf405cd8384b81a6d750f996fabf8f79b7cd1c3bedad01` (bound by the superseded 2026-09-27 packet). The validator reproduces the historical 2026-09-06 command as exit `1` with `Central package pin drift: CommunityToolkit.Aspire.Hosting.Dapr`, the result recorded before the drift, and the 2026-09-27 `--candidate` command as exit `1` with the same message, because both approved Toolkit pins (`.757`, `.767`) differ from the committed `.770` catalog. Every literal pin of both historical baselines classifies to one role under the loop 4 rules and the Aspire SDK rule passes, so those results are unchanged. Neither packet is resealed.
