# G-6 runtime/toolchain qualification candidate — 2026-09-29

**Status: pending exact-packet owner acceptance.** On 2026-09-29 Jérôme Piquot, as sole owner, approved the Toolkit `13.5.1-beta.770` prerelease exception and an isolated Dapr `1.18.2` two-sidecar rerun, with the local control-plane containers swapped and then restored by exact ID. That direction authorizes the run only; it does not accept this packet's hash. The accepted 2026-09-06 packet ([g-6-runtime-toolchain](../g-6-runtime-toolchain/packet.json)) and the superseded 2026-09-27 packet ([g-6-runtime-toolchain-20260927](../g-6-runtime-toolchain-20260927/packet.json)) are unchanged. The accepted EventStore `3.106.0` P1R record remains authoritative.

## Exact binding

- [Packet](packet.json): raw and normalized-LF SHA-256 `7e7d7ea2f39256b829367c56bf289ff97889e190f6e7481db35def986d53a5bb`, `status=pending`, `capturedUtc=2026-09-29T16:56:41Z`.
- Baseline: `references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json`, SHA-256 `260d1abae7d9eba358c5eaf749f12a0f46cf6adf5b0b4830d423615908820fbd`. Tuple: .NET SDK `10.0.401`, Aspire SDK/CLI `13.5.4`, Toolkit `13.5.1-beta.770`, Dapr CLI `1.18.0`, Dapr runtime `1.18.2`, Dapr .NET `1.18.10`, Fluent UI `5.0.0-rc.5-26219.1`, NSubstitute `6.2.0`, Fluxor `6.11.0`; approval `2026-09-29`.
- [Source state](source-state.json): 72 files across nine repositories, normalized-LF and raw SHA-256 `fd7a4f4d4abd9b936874daf6deaf46ba3871160ae36e10828580a2bbd1c6639e`. It keeps the 71 paths of the 2026-09-27 source state and adds the new baseline. Revisions are the committed gitlinks recorded by Projects commit `bd180b66b81b2893f6249cc282929b9cf62e5557`:

| Repository | Revision |
| --- | --- |
| Hexalith.Projects | `bd180b66b81b2893f6249cc282929b9cf62e5557` |
| Hexalith.Builds | `a912464e5f0294ccfb34a2a29d6d2072bafb6116` |
| Hexalith.Conversations | `a0a6fa288a87ba7971113de420334fc7478d7123` |
| Hexalith.EventStore | `5b9494835fcba6152789653add8f84bee5666035` |
| Hexalith.Folders | `b9dd03ee56907ca17c8f8e29df6e6af0d6170dc0` |
| Hexalith.FrontComposer | `ccb75d5454b874fff9e5ccddb4598b8bc3860631` |
| Hexalith.Memories | `289773387e0c6b665b669031616c1e84cc079297` |
| Hexalith.Parties | `60b9836ea23151c5319dd06fd3deb80122f7abc3` |
| Hexalith.Tenants | `55f3dc63b6ce10bb0afdf929d026fa07cffd9105` |

- [Source closure](source-closure.json): `allBoundFilesCommitted=true`. Every bound file is clean in its worktree and its `HEAD` blob hashes to the bound value. EventStore `5b949483` adds only owner evidence files to `489e5d76253f68c6cf53c443aa8755ad059e621e`; Builds `a912464e` changes only the G-6 validator, self-test, schema, baselines, README and P0 record relative to `85ca19bc99b137f0825d6a441199164653f537e9`.
- [Pin audit](pin-audit.json): all 44 baseline declarations match (9 `global.json`, 8 AppHost SDK declarations, 21 literal workflow/script pins, 6 central catalog pins).

## Validator results at the committed gitlinks

```text
python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json --candidate
  exit 0; G6-EVIDENCE-CANDIDATE-VALID
(same command without --candidate)
  exit 1; G6-EVIDENCE-INVALID: Packet status must be accepted
```

Candidate mode performs every artifact, source, command, count, topology, lifecycle and approval check with a distinct marker; it cannot satisfy the accepted-only gate. Projects CI runs the candidate step before the unchanged accepted-only step, which stays closed until a named owner accepts this exact packet.

## Every command and its real exit code

The validator requires exactly the 15 purposes in [commands.json](commands.json). Additional commands are recorded in the files named below, not folded into a passing entry.

| Command | Exit | Result |
| --- | --- | --- |
| Install exact Dapr tuple and swap control plane | 0 | CLI `1.18.0` archive checksum passed; `daprd` `1.18.2` SHA-256 `e730688f…f4e6`; image `sha256:5a5b6be9…938d` |
| Observe tool versions | 0 | .NET SDK `10.0.401`; Aspire CLI `13.5.4`; Dapr CLI `1.18.0`; runtime `1.18.2` |
| Builds Module and Evidence test assemblies (Release) | 0 | Module 214/214; Evidence 107/107 |
| G-6 mutation controls | 0 | 22 scenarios for each of 3 baselines; 18 baseline-drift controls |
| OQ8 PostgreSQL two-sidecar stop/restart qualifier | 0 | 1/1 passed, 0 skipped |
| 21 deterministic support selectors | 0 | 33/33 cases passed, 0 skipped |
| Strict EventStore capture validation | 0 | `OQ8 capture validation passed.` |
| Root workflow pin contract | 0 | 5 workflow files; Builds execution SHA equals the root Builds gitlink |
| Projects CI restore and Release build (package mode) | 0 | 0 warnings, 0 errors |
| Projects Integration tests | 0 | 27/27 passed |
| Managed restart smoke credential preflight | **1** | Blocked before AppHost start: `TEST_USER_PASSWORD` absent. No managed restart smoke ran. |
| AppHost Toolkit restore (eight force-evaluated restores) | 0 | All eight resolve `13.5.1-beta.770` from nuget.org ([toolkit-resolution.json](toolkit-resolution.json)) |
| Conversations and FrontComposer pin selectors | 0 | 1/1 each |
| Shared workflow test-platform contracts | 0 | 169 assertions |
| Broad package-exception inventory | **1** | 14 pre-existing drifts (listed in `commands.json`) |
| Conversations, Memories, Tenants, EventStore, Folders AppHost builds (package mode) | 0 each | 0 warnings, 0 errors ([apphost-builds.json](apphost-builds.json)) |
| Projects AppHost (inside the Projects CI solution build) | 0 | 0 warnings, 0 errors |
| Parties AppHost build, package and default modes | **1** each | 3 `CS0234` errors: Parties.UI uses `FcModuleLandingPage` and `FrontComposerRouteOptions`, which exist in FrontComposer source at `ccb75d54` but not in the published FrontComposer `4.5.0` packages it restores |
| FrontComposer AppHost build, package and default modes | **1** each | 52 errors (`CS0103`, `CS0234`, `CS0246`, `MSB9008`, `RZ10012`): missing Parties/EventStore symbols and an unresolved EventStore project-reference path |

The EventStore LiveSidecar/Server and Builds test binaries were built (Release, `--no-incremental`, isolated NuGet cache) shortly before the EventStore and Builds owner commits; those commits change no `src`, `tests` or `test` input, so the binaries correspond to the committed sources. All test executions, the qualifier, the validator, the workflow gate, the Projects build and every AppHost restore/build ran after Projects commit `bd180b66`.

## Isolation and restoration

The qualifier used an isolated `HOME` (Dapr CLI `1.18.0`, `daprd` `1.18.2`), an isolated Aspire CLI `13.5.4` tool path, and an isolated NuGet cache with `CI=true`, because the shared cache holds locally packed Hexalith packages. For the run, the running `dapr_placement`/`dapr_scheduler` `1.18.4` containers were stopped and renamed, and temporary `ghcr.io/dapr/dapr:1.18.2` containers took their names and host ports; the temporary scheduler kept its data on tmpfs, so the original scheduler volume was not mounted. [Control-plane restoration](control-plane-restoration.json) shows the original container IDs `9d57dbed…b523` and `baede0ae…e077` renamed back and running, the temporary containers removed, no fixture PostgreSQL container, and no `daprd` process left. No retained domain data was rewritten; raw CTRF files were not retained.

## Boundaries that remain open

- `dispositions.attempts[0].accepted=true` only selects the single passing technical attempt, as the schema requires. It is not owner acceptance. There was no failed attempt; [no-failed-attempt.json](no-failed-attempt.json) fills the required slot.
- Two AppHosts do not compile at the committed gitlinks: Parties (new since 2026-09-27: its UI source uses FrontComposer types that are not in published `4.5.0`) and FrontComposer (52 errors in both modes, up from 49). Neither error set names the Toolkit, but full AppHost compatibility is unverified for those two owners.
- The managed AppHost restart smoke did not run (credential preflight exit `1`), so no live multi-resource AppHost restart is claimed.
- The broad package-exception inventory still exits `1` with 14 pre-existing drifts, and five AppHosts still declare Aspire SDK `13.5.3` while three declare `13.5.4`.
- Builds CI at exact SHA `a912464e5f0294ccfb34a2a29d6d2072bafb6116` and Projects CI at the final commit can only be recorded after the owner pushes; neither has run.
- The exact packet hash above needs a named post-run G-6 owner decision. No G-4, G-5, P1R, P0 Stage 6, Story 6.1, deployment, or release acceptance follows from this technical candidate.

## Historical packets

The historical baselines are restored byte-for-byte at Builds `a912464e`: `runtime-toolchain-baseline.json` hashes to `525615c65ada8cabf5a6911a374bb22b62f6a3c1bfa6c91f7245312da9720265` (bound by the accepted 2026-09-06 packet) and `runtime-toolchain-baseline-2026-09-27.json` to `b9aa6791effe78cbb9bf405cd8384b81a6d750f996fabf8f79b7cd1c3bedad01` (bound by the superseded 2026-09-27 packet). The baseline-driven validator reproduces the historical 2026-09-06 command as exit `1` with `Central package pin drift: CommunityToolkit.Aspire.Hosting.Dapr`, the result recorded before the drift, and the 2026-09-27 `--candidate` command as exit `1` with the same message, because both approved Toolkit pins (`.757`, `.767`) differ from the committed `.770` catalog. Neither packet is resealed.
