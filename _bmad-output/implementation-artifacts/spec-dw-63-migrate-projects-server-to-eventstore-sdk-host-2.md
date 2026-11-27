---
title: 'DW-63 Close remaining distributed qualification blockers and refresh acceptance evidence'
type: 'bugfix'
created: '2026-11-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'd5dfcda5cdccdf6309d9052965e219ad0cd1f68b'
context: ['{project-root}/_bmad-output/implementation-artifacts/evidence/epic7/dw-63-acceptance-2026-11-27.md', '{project-root}/docs/read-cutover.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The DW-63 build spec is `done`, but `DW-63` stays `open`. Whole-lane qualification failed on `NativeConversationMembershipUsesTheRealGatewayOwnerAndPersistedNativeSource` because `run_supported_project_reads.py` binds Server regressions to `eventstore-dapr-cli` (missing `projects-conversation-start-setup`), and `full-replay` failed external resource preservation from host container churn.

**Approach:** Bind Server regression Dapr HTTP and gRPC endpoints to `projects-dapr-cli` (which mounts both stores), unit-test the binding, run the complete DW-63 technical qualification against current `HEAD` plus the runner fix, and refresh acceptance evidence and the ledger disposition.

## Boundaries & Constraints

**Always:** Keep `projects-conversation-start-setup` on `projects` only (`OrdinaryRunModeCompositionTests.cs:146-148`). Preserve `endpoints["state-grpc"]` (`eventstore-dapr-cli` gRPC) for `SupportedProjectReadDistributedFixture.cs:286`. Keep `preexisting_resources()` strict. Record full Git SHAs, native XML SHA-256 digests, and exact commands.

**Never:** Do not mount `projects-conversation-start-setup` on `eventstore-dapr-cli`. Do not skip/weaken tests or preservation checks. Do not record owner sign-offs or DW-114 custody proof without actual owner inputs and execution.

**Scope (human decision, 2026-11-27):** Runner fix plus complete DW-63 technical qualification (launcher unit tests, mapped source Server/Domain/Client, Release package Server, 77-cell v4 matrix, 7 separate rollback reads, and distributed `duplicate`, `full-replay`, and `main --regressions`). Keep DW-114, DW-93, DW-94, DW-95, DW-97, and DW-109 separate.

**Candidate revision (human decision, 2026-11-27):** Current `HEAD` (`d5dfcda5cdccdf6309d9052965e219ad0cd1f68b` plus this build's runner fix and tracked submodules) so all technical receipts bind to one current source state matching `docs/read-cutover.md`.

**Ledger disposition (human decision, 2026-11-27):** Update acceptance evidence and the `DW-63` disposition in `deferred-work.md` with the fresh passing receipts while keeping `DW-63` `open` pending DW-114 and dated owner/architect approvals.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Initial capture | AppHost with `projects-dapr-cli` & `eventstore-dapr-cli` | `endpoints.json` writes `projects-dapr-cli`, `projects-state-grpc` (`projects-dapr-cli` gRPC), and keeps `state-grpc` (`eventstore-dapr-cli` gRPC) | Fail if `projects-dapr-cli` gRPC is missing |
| Post-restart refresh | `refresh_owned_endpoints()` after `projects` restart | Refreshes `projects-dapr-cli`, `projects-state-grpc`, and `state-grpc` (`0600`) | Raise on lookup error; keep `0600` |
| Regression env | `regression_environment(env, endpoints_path)` | Sets `Projects__SupportedReads__Enabled=false`, `STORY61_DAPR_HTTP_ENDPOINT=endpoints["projects-dapr-cli"]`, `STORY61_DAPR_GRPC_ENDPOINT=endpoints["projects-state-grpc"]` | Raise `KeyError` if either key is absent |
| Main + Server regressions | Quiescent Docker/Aspire/Dapr; `--scenario main --regressions` | Main, Integration, and full Server suite pass (0 fails, 0 skips, cleanup & `preexistingResourcesPreserved: true`) | Non-zero exit on failure, skip, or churn |

</frozen-after-approval>

## Code Map

- `tools/qualification/run_supported_project_reads.py:358-364,910-921,2697-2703` — add `projects-state-grpc` from `projects-dapr-cli` gRPC in capture & refresh; point `regression_environment()` to `projects-dapr-cli` + `projects-state-grpc` (matching `tests/Hexalith.Projects.PackageFixtures/PackageFixtureRun.cs:261-262`). Keep `state-grpc`.
- `src/Hexalith.Projects.AppHost/SupportedProjectReadFixtureProfile.cs:205-217` — mounts `statestore` + `pubsub` on all 6 sidecars and `projects-conversation-start-setup` on `projects` only. Do not change.
- `tests/tools/test_supported_project_reads_launcher.py:1117-1127` — update `test_regressions_use_the_refreshed_endpoint_file`; assert `refresh_owned_endpoints()` refreshes `projects-state-grpc` and keeps `state-grpc`.
- `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.Cutover.cs:53-125` — requires both stores via `STORY61_DAPR_*`. Do not weaken.
- `artifacts/dw-63/v4-fixture-control/verify_v4.py` — reference 77-cell v4 + 7-read rollback harness (run outputs into local state under `~/.local/state/hexalith-projects/` rather than polluting tracked repo paths).
- `_bmad-output/implementation-artifacts/evidence/epic7/dw-63-acceptance-2026-11-27.md` & `_bmad-output/implementation-artifacts/deferred-work.md:539-550` — acceptance record & open ledger entry.

## Tasks & Acceptance

**Execution:**
- [x] `tools/qualification/run_supported_project_reads.py` — record `projects-state-grpc` (`:2697-2702`, `:910-921`) and bind `regression_environment()` (`:358-364`) to `projects-dapr-cli` / `projects-state-grpc` — exposes both stores to Server regressions without altering `state-grpc`.
- [x] `tests/tools/test_supported_project_reads_launcher.py` — update `test_regressions_use_the_refreshed_endpoint_file` and test `refresh_owned_endpoints()` for `projects-state-grpc` and `state-grpc` — covers endpoint I/O matrix.
- [x] `tools/qualification/run_supported_project_reads.py` & `artifacts/dw-63/v4-fixture-control/verify_v4.py` — run launcher tests, mapped source Server/Domain/Client checks, Release package Server suite, 77-cell v4 matrix + 7 separate rollback reads, and clean `duplicate`, `full-replay`, and `main --regressions` on `d5dfcda5cdccdf6309d9052965e219ad0cd1f68b` + runner fix (retaining raw receipts under `~/.local/state/hexalith-projects/`) — produces complete passing receipts.
- [x] `_bmad-output/implementation-artifacts/evidence/epic7/dw-63-acceptance-2026-11-27.md` & `_bmad-output/implementation-artifacts/deferred-work.md` — record verified full SHAs, source manifest/packet/XML SHA-256 digests, cleanup/preservation results, and remaining DW-114/owner sign-offs while keeping `DW-63` `open` — keeps governance evidence current and auditable.

**Acceptance Criteria:**
- Given the AppHost graph, when `run_supported_project_reads.py` captures/refreshes `endpoints.json` and builds `regression_environment()`, then `STORY61_DAPR_HTTP_ENDPOINT` and `STORY61_DAPR_GRPC_ENDPOINT` target `projects-dapr-cli` while `state-grpc` stays on `eventstore-dapr-cli`.
- Given `d5dfcda5cdccdf6309d9052965e219ad0cd1f68b` plus the runner fix and quiescent host resources, when mapped source, Release package, v4/rollback, and distributed `duplicate`/`full-replay`/`main --regressions` lanes run, then every suite passes with 0 failures, 0 skips, verified cleanup, and `preexistingResourcesPreserved: true`.
- Given the passing receipts, when `dw-63-acceptance-2026-11-27.md` and `deferred-work.md` are updated and `validate_production_authority.py --validate-index` runs, then recorded digests match native XML receipts, `DW-63` stays `open`, and the guard exits 0.

## Implementation Notes

- Updated `tools/qualification/run_supported_project_reads.py` (`regression_environment`, `refresh_owned_endpoints`, and initial `endpoints.json` capture) to capture/refresh `projects-state-grpc` from `projects-dapr-cli` gRPC, preserve `state-grpc` on `eventstore-dapr-cli` gRPC, and bind `STORY61_DAPR_HTTP_ENDPOINT` / `STORY61_DAPR_GRPC_ENDPOINT` to `projects-dapr-cli` / `projects-state-grpc`.
- Updated `tests/tools/test_supported_project_reads_launcher.py` (`test_regressions_use_the_refreshed_endpoint_file` and new `test_refresh_owned_endpoints_updates_projects_state_grpc_and_preserves_state_grpc`) covering `Projects__SupportedReads__Enabled=false`, `projects-dapr-cli` + `projects-state-grpc` binding, `KeyError` on missing keys, `state-grpc` preservation, `0600` permissions, and lookup-failure preservation.
- Resolved two `d5dfcda5` regressions discovered while running the distributed qualification lanes on `HEAD`:
  1. `src/Hexalith.Projects.AppHost/SupportedProjectReadDevelopmentInfrastructure.cs:51` loaded `Hexalith.Folders.Server.dll` instead of `Hexalith.Folders.Contracts.dll` (where `[RouteCatalogDeclarationSource]` moved in `references/Hexalith.Folders` `v2.2.1` `bf60ba51`), causing `RouteCatalogUnavailableException: No compiled owner declaration source was enrolled.` on `aspire start`. Updated line 51 to load `Hexalith.Folders.Contracts.dll`.
  2. `tests/Hexalith.Projects.Integration.Tests/Story71/DockerDaprRedisTwoReplicaFixture.cs:115-153` created `_componentsDir` and `statestore.yaml` without explicit POSIX read/traverse modes, so under `run_supported_project_reads.py`'s `os.umask(0o077)` the nonroot `daprio/daprd:1.19.2` (`65532:65532`) container could not read `/components/statestore.yaml` and timed out during `main --regressions`. Added non-Windows `File.SetUnixFileMode` (`0755` on `_componentsDir`, `0644` on the non-secret `statestore.yaml`).
- Retained all raw receipts under `/home/administrator/.local/state/hexalith-projects/dw-63-qualification-d5dfcda5-2026-11-27-final/` (pre-review run) and `/home/administrator/.local/state/hexalith-projects/dw-63-qualification-d5dfcda5-2026-11-27-reviewed/` (post-review run, binding to code-scoped `sourceDiffSha256` `4fadba5808049ed8f56ae6f0a46daed6fb098e407b91351b35b940becebbccf8` and 18,673-entry source manifest `58c7e6642fb722a0669acff586219e1d8ffe1421eadb6efca7d822f9e8055ced`).
- Review patches applied: extracted shared `populate_owned_endpoints` / `write_owned_endpoints` (`0o600`) in `tools/qualification/run_supported_project_reads.py` with initial-capture unit test in `tests/tools/test_supported_project_reads_launcher.py`; extracted `LoadCatalogOwners` (`InternalsVisibleTo`) on `SupportedProjectReadDevelopmentInfrastructure` with `SupportedProjectReadDevelopmentInfrastructureLoadsDeclaredCatalogOwners` in `SupportedProjectReadTopologyTests.cs`; extracted `WriteStatestoreComponentAsync` on `DockerDaprRedisTwoReplicaFixture` with `DockerDaprRedisTwoReplicaFixtureTests.cs`; refreshed `ordinary-preflight.json` on `d5dfcda5`, linked all 13 root gitlinks (`gitlinks.txt`), explicitly scoped `sourceDiffSha256` to `src/ tests/ tools/`, and verified `CI=true` Release package builds of `AppHost`, `Integration.Tests`, and `Server.Tests`.
- Updated `_bmad-output/implementation-artifacts/evidence/epic7/dw-63-acceptance-2026-11-27.md` and `_bmad-output/implementation-artifacts/deferred-work.md` while keeping `DW-63` `status: open` pending DW-114 and dated owner/architect sign-offs.

## Spec Change Log

## Review Triage Log

| ID | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|
| BH-1 | `PackageActualOwnerInfrastructure.cs:65-66` still loads `Hexalith.Folders.Server.dll` and deferred-work entry is stale | medium | Confirmed at `PackageActualOwnerInfrastructure.cs:65-66`; pre-existing from the Folders `v2.2.1` split before baseline `d5dfcda5` and outside this spec's frozen scope. | defer (with VG-1) |
| BH-2 | Missing regression tests for `SupportedProjectReadDevelopmentInfrastructure.cs:51` and `DockerDaprRedisTwoReplicaFixture.cs:91-105` | medium | Confirmed: neither C# fix introduced by this diff has a dedicated unit/integration regression test in normal test runs. | patch (with VG-2, VG-3) |
| BH-3 | Initial `endpoints.json` capture in `_main()` duplicates `refresh_owned_endpoints()`, lacks `0o600` chmod, and has no unit test | medium | Confirmed at `run_supported_project_reads.py:2698-2704`; Matrix Row 1 initial capture and missing-gRPC failure are not unit-tested. | patch (with VG-4) |
| BH-4 | `regression_environment()` does not validate non-empty string values for Dapr endpoints | low | `endpoints.json` is written solely from Aspire `describe` URLs in a `0o700` private dir under `0o077` umask; adding new value guards adds complexity for an unreachable state. | reject |
| BH-5 | `refresh_owned_endpoints()` rewrites `endpoints.json` directly instead of temp-file + `os.replace()` | low | Pre-existing pattern in a single-threaded restart step under `umask 0o077` and `0o700` dir; fixing adds file-replacement complexity for an unlikely everyday issue. | reject |
| BH-6 | Transient `full-replay` attempt 1 startup flake (`The real protected query catalog is unavailable.`) is not logged as a bug | medium | Confirmed in `live/full-replay-attempt1/distributed.xml` (`MetadataAsync:1533`); `hexalith-llm-instructions.md` requires recording non-reproducing retried flakes. | defer |
| BH-7 | `sourceDiffSha256` (`13a6a88d...`) hashes `git diff --binary -- src/ tests/ tools/` rather than full workspace `git diff --binary` (`b453e6b1...`) | medium | Verified directly: full normalized diff is `b453e6b1...` after doc/ledger edits while `13a6a88d...` is the `-- src/ tests/ tools/` diff; evidence text must state the exact scope. | patch |
| BH-8 | DW-114 remaining-decision bullet links stale `928f66f5` `ordinary-preflight.json` after AppHost catalog files changed in `d5dfcda5` | low | Confirmed via `git diff 928f66f5..d5dfcda5 -- src/Hexalith.Projects.AppHost/`; refreshing `ordinary-preflight.json` on `d5dfcda5` is a direct correction. | patch |
| BH-9 | Acceptance record lists only 6 of 13 tracked root gitlinks and does not link `gitlinks.txt` | low | Confirmed at `dw-63-acceptance-2026-11-27.md:27`; `gitlinks.txt` already exists (`c1bbf302...`) and linking it with its SHA-256 is a direct correction. | patch |
| BH-10 | Spec `Code Map`, `Tasks & Acceptance`, `Spec Change Log`, and `Verification` omit the two extra C# fixes and exact guard output | low | Fix would edit this build's spec; per step-04 triage rules, reject findings whose fix is to edit the build's spec. | reject |
| BH-11 | `CI=true` Release package build compiled only `Server.Tests`, not the two modified C# projects (`AppHost`, `Integration.Tests`) | low | Both modified C# files are `#if SUPPORTED_PROJECT_READ_SOURCE`-gated, so package-mode code is unchanged; verifying `CI=true` Release builds of `AppHost` and `Integration.Tests` and clarifying scope is direct. | patch |
| VG-1 | `PackageActualOwnerInfrastructure.Add` still loads `Hexalith.Folders.Server.dll` as `folderOwner` | medium | Pre-verified gap; pre-existing issue from Folders `v2.2.1` assembly split, not caused by this diff. | defer (with BH-1) |
| VG-2 | `SupportedProjectReadDevelopmentInfrastructure.Add` Folders owner assembly selection is unverified in normal test runs | medium | Pre-verified gap caused by this diff's change at `SupportedProjectReadDevelopmentInfrastructure.cs:51`. | patch (with BH-2) |
| VG-3 | `DockerDaprRedisTwoReplicaFixture` restrictive-umask permission fix is unverified under `umask 0077` in normal test runs | medium | Pre-verified gap caused by this diff's change at `DockerDaprRedisTwoReplicaFixture.cs:91-105`. | patch (with BH-2) |
| VG-4 | `_main()` initial `endpoints.json` capture of `projects-state-grpc` is untested and masked in `main --regressions` by post-restart refresh | medium | Pre-verified gap caused by duplicated capture at `run_supported_project_reads.py:2701`. | patch (with BH-3) |

## Verification

**Commands (post-review pass under `dw-63-qualification-d5dfcda5-2026-11-27-reviewed/`):**
- `python3 -m unittest discover -s tests/tools -p test_supported_project_reads_launcher.py` -- PASS (169/169 tests, 0 failures, 0 skips).
- Mapped Debug source checks + 77-cell v4 matrix + 7 separate `LegacyRollback` reads + `CI=true` Release package builds (`AppHost`, `Integration.Tests` 217/217, `Server.Tests` 1,662/1,662 — all 0 warnings/0 errors) -- PASS (262 Server + 2 Domain + 3 Client, 77 v4 cells + 7 legacy security slices, 7 HTTP 200 rollback reads + 2 adverse controls -> `Unavailable`; 0 failures/skips).
- `run-with-temp python3 tools/qualification/run_supported_project_reads.py --scenario duplicate --timeout 600`, `--scenario full-replay --timeout 600`, and `--scenario main --regressions --timeout 600` -- PASS on first attempt (all exit 0, 0 failures/skips, verified owned cleanup, `preexistingResourcesPreserved: true`; `main --regressions` passed 9 main cases, owned `projects` restart, 368/368 Integration, and 1,733/1,733 Server tests).
- `python3 tools/planning/validate_production_authority.py --validate-index` -- PASS (`exit 0`, `PASS: production-authority index is [6, 7, 8]`).
