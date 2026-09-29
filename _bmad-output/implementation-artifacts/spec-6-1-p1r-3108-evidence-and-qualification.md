---
title: '6.1-P1R Qualify the EventStore 3.109.0 candidate'
type: 'chore'
created: '2026-09-29'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: '1152c8397f35fed1580e20915833dd958e8dbe07'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-3108-exact-baseline-candidate.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The pending P1R candidate (EventStore 3.108.1, Builds `2326f983`, Toolkit `.767`) no longer matches the committed superproject. Builds `85ca19bc` pins EventStore 3.109.0, Toolkit `13.5.1-beta.770` and Dapr .NET 1.18.10, so the fresh G-6 packet fails while documents still claim it passes. Upstream edits also changed two baseline files that existing packets bind.

**Approach:** Retarget P1R to the published EventStore 3.109.0 family (tag `v3.109.0`, `818e28a8af421994e4f77e66327dc33a8c67ca5f`) with the committed Builds catalog. Gather exact owner-repository evidence, make the G-6 validator baseline-driven, capture a fresh G-6 packet bound to committed source, and correct every stale 3.108.1 claim. The candidate stays pending.

**Decision (2026-09-29):** After review found the drift, the user chose to retarget P1R to 3.109.0 instead of recording 3.108.1 as historical or re-pinning Builds.

**Decisions (2026-09-29, the user as sole owner):**
- **G-6:** Approved the Toolkit `13.5.1-beta.770` prerelease exception and an isolated Dapr 1.18.2 two-sidecar rerun, with local control-plane containers swapped and then restored by exact ID. This authorizes the run only; it is not acceptance of the resulting packet hash.
- **Release bypass:** Qualify 3.109.0. Record release run `36238526310` (`BYPASS_VALIDATION`) and the failing tag CI (3 OQ8 Story 4.15 governance tests) as an explicit EventStore Owner disposition item.
- **Commits:** Local Conventional Commits are authorized in Builds, EventStore and Projects, in order: owner commits, then gitlinks, then packet capture, then the Projects commit. No push; exact-SHA CI evidence is recorded after the user pushes.
- **Scope:** Keep one spec (about 2,500 planning tokens) rather than splitting.

## Boundaries & Constraints

**Always:** Bind every claim to full commits, package IDs and versions, hashes, commands, exit codes and evidence paths. Distinguish tag `v3.109.0` from EventStore checkout `489e5d76`, and published packages from local builds. Keep the accepted 3.106.0 P1R record, the 3.70.1 rollback, and the historical and superseded 3.108.1 packet files byte-unchanged. Keep P0 Stage 6, P2–P4, readiness `NOT_READY` and Story 6.1 `blocked`. Restore upstream-edited baseline files to the bytes their packets bind.

**Never:** Infer four-role P1R acceptance or G-6 owner approval. Reseal or edit old packet files. Substitute a local build for a published artifact. Claim a validator result that was not reproduced at the committed gitlinks. Push without explicit instruction.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Published family | 14 manifest IDs at 3.109.0 | Per-ID provenance, hash, signature, and independent restore/consumption | A missing or mismatched ID stays unresolved |
| Release validation bypass | Release run `36238526310` used `BYPASS_VALIDATION`; tag CI `36237813612` failed 3 Contracts OQ8 tests | Recorded with the failing tests as an owner-disposition item | Never reported as green CI |
| Source drift | Tag versus `489e5d76` (28 commits, `AggregateMetadata.RetainedFloor`, breaking interface change) | Scoped compatibility diff | Untested behavior is named, not assumed compatible |
| Toolchain | Committed Builds catalog | New baseline and packet; candidate mode exits 0 at the committed gitlinks | Drift is reported with the exact validator message |
| Historical packets | Accepted 2026-09-06 packet and superseded 2026-09-27 packet | Their baselines hash to the bound values; expectations come from each baseline | No reseal |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py` -- `EXPECTED_TUPLE` :17-28, approval checks :239-249, catalog checks :280-289, approval block :316-322, artifact tuple :359/:362. Derive these from `--baseline`; keep the structural checks and `--candidate`.
- `references/Hexalith.Builds/Tools/test-runtime-toolchain-evidence-validator.py` -- :16, :78 and :264 cover one baseline only; run the mutation scenarios per baseline.
- `references/Hexalith.Builds/schemas/hexalith.runtime-toolchain-evidence.v1.json` -- consts at :94-98 and :183-192 are already stale; use type/pattern instead. The validator only hashes this file.
- `references/Hexalith.Builds/Tools/runtime-toolchain-baseline.json` and `runtime-toolchain-baseline-2026-09-27.json` -- `aada815` and `85ca19b` edited them. Restore the `2326f983` bytes (SHA-256 `525615c6…`) and the `3e38a18` bytes (`b9aa6791…`).
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- catalog source of truth (3.109.0, `.770`, 1.18.10). Edit only if the Toolkit question chooses revert.
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md` -- its 2026-09-29 alignment note omits the Toolkit drift.
- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3108/` -- copy into `6-1-p1r-3109/`. The verifier hard-codes 3.108.1 at L2, L21, L110 and L117; so do `consumer/Consumer.csproj` L10-22 and `rollback-probe/v3108/Probe.csproj` L11. `Program.cs` and `v370/` are reused unchanged. Release evidence artifact: `release-evidence-36238526310-1`; release-time Builds: `22a578b5`.
- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/6-1-p1r-3108-release-source-revalidation.md` -- older record with no forward link.
- `_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain/` and `g-6-runtime-toolchain-20260927/` -- immutable; the new folder copies the 20260927 layout (commands, pin-audit, source-state, source-closure, toolkit-resolution, dispositions, control-plane restoration).
- `.github/workflows/ci.yml` :91, :129-141, :171, :174 and `tests/tools/run-ci-workflow-gates.ps1` :19, :502-510 -- Builds execution SHA and G-6 paths.
- `_bmad-output/implementation-artifacts/6-1-p1r-3108-exact-baseline-candidate.md`, `sprint-status.yaml` `p1r_current_revalidation` (:58-91), Spine :374 -- index of the pending candidate.
- Do not change: `6-1-p1r-acceptance.json`, `tools/planning/validate_production_authority.py`, the P2/P3/Story 6.1 specs.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Tools/` validator, self-test and schema -- make expectations baseline-driven so each baseline passes its own mutation suite -- BH-17/EC-23.
- [x] `references/Hexalith.Builds/Tools/` baselines and P0 record -- restore the two edited baselines; add `runtime-toolchain-baseline-2026-09-29.json` for the approved `.770` tuple; correct the P0 note -- BH-15/BH-16.
- [x] `references/Hexalith.EventStore/.../evidence/6-1-p1r-3109/` -- 14-ID public replay, consumer restore/build, 3.109.0→3.70.1 rollback probe, the bypass and tag-CI failure, and tag-to-checkout drift; add a forward link from the 3108 record -- BH-31.
- [x] `_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/` -- fresh packet bound to committed source and gitlinks; record every command with its real exit code, including blocked ones -- BH-21.
- [x] `.github/workflows/ci.yml` and `tests/tools/run-ci-workflow-gates.ps1` -- new baseline and packet; a candidate-mode step before the accepted-only step; the Builds execution SHA must equal the Builds gitlink, with a gate check -- VG-02/EC-07.
- [x] `_bmad-output/implementation-artifacts/6-1-p1r-3109-exact-baseline-candidate.md`, the 3108 owner packet, `sprint-status.yaml` and the Spine -- new pending owner packet; mark 3.108.1 superseded with the reproduced current result; name the open technical gaps instead of "complete" -- BH-13/BH-14/BH-20.

**Acceptance Criteria:**
- Given the committed gitlinks, when the candidate-mode validator runs on the new packet, then it exits 0, and accepted-only mode exits 1 until a named owner acceptance exists.
- Given the historical and 3.108.1 baselines, when hashed, then they equal their packets' bound SHA-256 values, and the self-test passes for all three baselines.
- Given tag `v3.109.0`, when the public replay runs, then all 14 IDs are verified or explicitly unresolved, and the validation bypass appears as an owner-disposition item.
- Given the changed documents, when reviewed, then none claims a 3.108.1 pass at the current state, and the 3.106.0 record, 3.70.1 rollback and blocked states are unchanged.

## Implementation Notes

The original request authorized evidence gathering and owner-repository edits while withholding inferred acceptance. On 2026-09-27 Jérôme Piquot identified himself as sole contributor and owner and said “approve now” after the named candidate, rollback-target, Toolkit pin, and isolated test decisions were presented. This authorizes those directions and the qualification work. It does not make a pending technical result pass or supply a post-run, exact-packet acceptance decision.

- 2026-09-29 retarget, implemented. Builds owner commit `a912464e5f0294ccfb34a2a29d6d2072bafb6116` makes the G-6 validator, self-test and schema baseline-driven, restores `runtime-toolchain-baseline.json` (`525615c6…`) and `runtime-toolchain-baseline-2026-09-27.json` (`b9aa6791…`), adds `runtime-toolchain-baseline-2026-09-29.json` (`260d1aba…`, Toolkit `.770`, Dapr .NET `1.18.10`) and corrects the P0 note. The self-test passes 22 scenarios for each of the three baselines plus 18 baseline-drift controls.
- EventStore owner commit `5b9494835fcba6152789653add8f84bee5666035` adds `evidence/6-1-p1r-3109/`: all 14 IDs verified (hashes, nuspec commit `818e28a8…`, payload equality, 14 signatures, release contract; verifier exit 0), CPM-off consumer restore/build and CLI startup, a bounded `3.109.0`→`3.70.1` storage probe (sidecar restart and RDB restore), release run `36238526310` with `BYPASS_VALIDATION` (Commitlint source proof, release-time Builds `22a578b5…`), failing tag CI `36237813612` (three Contracts OQ8 tests) as an owner-disposition item, and the scoped `v3.109.0`→`489e5d76` drift (28 commits, 22 `src` paths, `RetainedFloor`, interface changes) as untested. The older 3108 record links forward.
- Projects gitlinks commit `bd180b66b81b2893f6249cc282929b9cf62e5557` pins both owner commits, runs CI Builds at `a912464e`, and adds a candidate-mode G-6 step before the accepted-only step; the workflow gate fails when the executed Builds SHA differs from the root Builds gitlink.
- The fresh G-6 packet `g-6-runtime-toolchain-20260929/packet.json` (SHA-256 `7e7d7ea2…`) binds 72 committed source files with zero uncommitted bindings. At the committed gitlinks the candidate validator exits 0 and the accepted-only validator exits 1. The isolated Dapr `1.18.2` two-sidecar qualifier passed 1/1 with 33/33 support tests; original control-plane containers were restored by exact ID. Every command is recorded with its real exit code: the managed restart smoke preflight and the package-exception inventory exit 1, and the Parties (3 errors) and FrontComposer (52 errors) AppHost builds exit 1 in `apphost-builds.json`.
- The new owner packet `6-1-p1r-3109-exact-baseline-candidate.md`, the superseded 3108 packet, `sprint-status.yaml` and the Spine index the pending candidate and name its open gaps; the 3.106.0 record, 3.70.1 rollback, P0 Stage 6, readiness and Story 6.1 states are unchanged. Exact-SHA CI is pending the owner's push.
- Matrix test audit (2026-09-29): the release-bypass and checkout-drift rows had no re-runnable check. `tests/tools/test_p1r_candidate_evidence.py` now compares the recorded release run, Commitlint source proof, failing tag CI job, Projects index text, commit and `src` path counts, and storage-record blobs against GitHub Actions and EventStore Git; 6/6 passed.

## Spec Change Log

- 2026-09-29 — Triggered by review loop 1: BH-13/14/15/16 (intent_gap; the user retargeted to 3.109.0) and BH-17/EC-23 (bad_spec; the validator hard-codes one baseline). Amended the Intent, Code Map, Tasks, AC and Verification for 3.109.0. Added baseline-driven validation and the restoration of edited baselines. Known-bad state avoided: evidence claiming a pass that fails at the committed gitlinks, and historical packets unverifiable against their own baselines. KEEP: the public-package replay and signature checks; the CPM-off consumer and probes with a checked-in `NuGet.Config`; the honestly scoped rollback-probe wording; the separation between candidate and accepted-only modes; immutable old packet folders.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 Docker command lacked quotes | medium | patch | Re-ran the quoted command with exit 0 and corrected the command audit. |
| BH-02 consumer fixture inherited central versions | medium | patch | Fixture now overrides central management; in-place fresh restore and Release build pass. |
| BH-03 CLI install could use other feeds | medium | patch | Fresh tool install now uses only the checked-in NuGet.Config and passes. |
| BH-04 public archive checks lacked replay | medium | patch | New verifier downloads all 14 archive pairs and passes hash, source metadata, payload, signature, and manifest checks. |
| BH-05 provenance implied a bit-identical rebuild | false | reject | The exact-source release run, publication preflight, signed metadata, and asset comparison establish release association; the owner evidence explicitly states that no independent bit-identical rebuild was performed. |
| BH-06 exact-pair consumer probe absent | medium | patch | Clean Builds revision probe now restores eight EventStore 3.108.1 host dependencies and passes packaged controls; live persisted behavior remains pending. |
| BH-07 later checkout lacks full behavioral tests | false | reject | The packet identifies source-breaking interface additions and behavior drift and requests a separate owner decision; it makes no checkout/package interchangeability claim. |
| BH-08 new rollback proof absent | medium | patch | Projects packet now states the missing 3.108.1 to 3.70.1 executable and persisted-data result as an acceptance blocker. |
| BH-09 new G-6 packet placeholder | medium | patch | Spec now cites the actual failing historical validator and leaves owner-accepted live G-6 qualification open. |
| BH-10 G-6 decision versus recapture dates | medium | patch | Sprint index now distinguishes 2026-09-06 original decision from 2026-09-22 accepted packet recapture. |
| BH-11 Builds checkout called clean | low | patch | Builds owner text now names the source commit without claiming its evidence worktree is clean. |
| BH-12 advanced EventStore gitlink implies use | false | reject | The pointer advance preceded this evidence edit; candidate release source remains the tag and Story 6.1 is blocked, so no later-checkout implementation is authorized. |
| EC-01 checked-in consumer restore | medium | patch | Same defect as BH-02; in-place restore/build verified after fixture correction. |
| EC-02 Docker command replay | medium | patch | Same defect as BH-01; exact quoted command re-run and recorded. |
| EC-03 CLI NuGet source | medium | patch | Same defect as BH-03; configfile-only fresh install verified. |
| BH-13 committed gitlinks differ from packet bindings | high | intent_gap | Root `1152c83` pins Builds `85ca19bc`/EventStore `489e5d76`; packet and README bind `2326f983`/`1cc6b44b`, and README still says no commit or push was made although `d685f65` committed the bound files. |
| BH-14 fresh packet fails at committed state | high | intent_gap | Re-ran the documented `--candidate` command on 2026-09-29: exit 1, `Central package pin drift: CommunityToolkit.Aspire.Hosting.Dapr`; README, exact-baseline packet, Verification, sprint-status and Spine still claim exit 0. |
| BH-15 Builds rebase note omits Toolkit drift | high | intent_gap | Builds `85ca19bc` catalog pins Toolkit `13.5.1-beta.770` and EventStore `3.109.0` (via rebase onto `aada815`); baseline and validator keep owner-approved `.767`, and `.770` has no owner approval. |
| BH-16 historical baseline hash changed | high | intent_gap | `runtime-toolchain-baseline.json` hashes `62a6a555…` at `85ca19bc` versus `525615c6…` bound by the accepted 2026-09-06 packet; `aada815` edited it. |
| BH-17 historical validator result no longer reproduces | medium | bad_spec | Validator line 240 and schema hard-code `approvedOn == 2026-09-27`; re-run of the historical command now fails `Baseline approval date drift`, not the recorded pin drift. |
| BH-18 no post-run exact-hash acceptance field | low | reject | Historical acceptance used a status flip plus a sprint-status record; adding schema acceptance fields is more than a direct correction. |
| BH-19 accepted-only G-6 step masks later project-gates | medium | defer | Pre-existing: at baseline the historical packet step already exited 1 before the FrontComposer/OpenAPI gates; the mixed Builds revision part is covered by BH-13. |
| BH-20 sprint-status overstates technical completeness | medium | patch | `technical-evidence-complete-pending-exact-owner-decisions` while the FrontComposer AppHost build, actor/domain rollback replay, source closure and persisted Stage 6 proof remain open per this spec. |
| BH-21 command record omissions | medium | patch | `commands.json` records the managed restart smoke exit 1 (`TEST_USER_PASSWORD` absent) but README and exact-baseline packet never mention it; the aggregated AppHost entry reports exit 0 around the FrontComposer failure. |
| BH-22 approval may postdate the run | maybe-false | reject | Captures are 2026-09-26T22:38–22:51Z (00:38–00:51 CEST on 09-27); settling needs the approval timestamp, and a post-run exact-hash acceptance is still required, so harm would be low. |
| BH-23 unexplained `productionConfigurationUntouched: false` | false | reject | Value is accurate beside `environmentName: Testing`, `testOnlyHostingStartup: true` and the listed test seams. |
| BH-24 consumer and probe not isolated | false | reject | `consumer/obj/project.assets.json` has no project libraries; all 13 EventStore packages resolve from NuGet at 3.108.1 with CPM off. |
| BH-25 rollback rehearsal overclaims | false | reject | Probe README lines 111–119 state the non-actor path, identical record blobs, runtime 1.18.4, and name actor/domain replay a material unresolved blocker. |
| BH-26 public-package verifier provenance gaps | low | reject | Tag commit is compared with the recorded commit and current manifest hash still matches; drift fails loudly. |
| BH-27 Folders fallback maps all unlisted statuses to Unavailable | medium | defer | From unrelated commit `6bc5155`; 405/410/429 and folder-side 413/422 now become retryable Unavailable. |
| BH-28 Folders malformed-success tests miss the fallback | high | defer | From unrelated commit `6bc5155`; see VG-01 (fails in CI package mode). |
| BH-29 unrecorded McpCli course correction | maybe-false | defer | From unrelated commit `b42beb3`; no `sprint-change-proposal-2026-09-27*` exists and Spine line 342 still cites the old AD-29 wording; would be medium. |
| BH-30 spec records stale | low | reject | Fix edits this build's spec. |
| BH-31 older EventStore revalidation record lacks later-drift link | low | patch | `6-1-p1r-3108-release-source-revalidation.md` records `8ac62359`/43 paths with no link to the later `public-packages.json` and README; committed `489e5d76` part is BH-13. |
| EC-04 accepted-only CI step on pending packet | medium | defer | Same defect as BH-19. |
| EC-05 baseline `.767` versus catalog `.770` | high | intent_gap | Same defect as BH-15. |
| EC-06 CI gate regex does not forbid `--candidate` | low | reject | Guards an undemonstrated edit; adds a new guard. |
| EC-07 no Builds gitlink versus execution-SHA check | high | intent_gap | Same root cause as BH-13: CI executes `2326f983` workflows against the `85ca19bc` catalog. |
| EC-08 file-reference fallback breadth | medium | defer | Same defect as BH-27. |
| EC-09 folder fallback breadth and 422 divergence | medium | defer | Same defect as BH-27. |
| EC-10 verifier merges stderr into hashed output | low | reject | Successful `git show` writes no stderr; any drift fails loudly. |
| EC-11 verifier CRLF manifest hashing | low | reject | Linux/WSL and CI checkouts keep LF; a CRLF checkout fails loudly. |
| EC-12 verifier subprocess timeout | low | reject | Evidence replay tool; a hang is visible and adds a guard. |
| EC-13 rollback probe prefix unencoded | low | reject | Operator-supplied documented prefixes; mismatch fails loudly. |
| EC-14 rollback probe missing key | false | reject | Empty body fails loudly during deserialization; no false pass. |
| EC-15 OQ8 helper null `subjectInputs` | low | reject | Story 4.15 helper swept into `1cc6b44b`; malformed packet fails loudly. |
| EC-16 OQ8 helper missing `reviewedCommit` | false | reject | Falls back to HEAD and computes `workingTreeDirty` from `git status`; not silent. |
| EC-17 OQ8 helper short `reviewedCommit` | low | reject | `git cat-file -e` verifies the commit; ambiguity is unlikely. |
| EC-18 OQ8 helper reports clean while selector reads worktree | maybe-false | defer | Story 4.15 helper; settle by checking whether `workingTreeDirty` describes only the archived commit or also the ROOT selector inputs; would be medium. |
| EC-19 schema accepts pending in all modes | low | reject | No schema-only consumer found; the CLI enforces accepted-only mode. |
| EC-20 removed redacted-metadata test | high | defer | Same defect as BH-28. |
| EC-21 packet claim fails at committed tuple | high | intent_gap | Same defect as BH-14. |
| EC-22 Builds gitlink consumes EventStore 3.109.0 | high | intent_gap | Same root cause as BH-13/BH-15. |
| EC-23 validator hard-requires 2026-09-27 | medium | bad_spec | Same defect as BH-17. |
| EC-24 Spine/PRD authority changes ride along | maybe-false | defer | Same defect as BH-29. |
| VG-01 Folders fallback untested in CI package mode | high | defer | Pre-verified gap from unrelated commit `6bc5155`: `ValidateLink_MalformedSuccess_IsUnavailable` returns Denied with published `Hexalith.Folders.Client` 1.0.0 under `CI=true`. |
| VG-02 no passing check of fresh baseline against real catalog | medium | patch | Pre-verified gap: Builds self-test writes its own catalog; the only real-workspace run is accepted-only and always exits 1. |
| VG-03 Conversations AppHost resolved Toolkit version untested | medium | defer | Pre-verified gap; Conversations owner, no resolved-version check exists to extend. |
| VG-04 malformed-success test fails in CI | high | defer | Same defect as VG-01. |
| VG-05 G-6 evidence text wrong at current pointer | high | intent_gap | Same defect as BH-14. |
| VG-06 pending packet blocks other CI gates | medium | defer | Same defect as BH-19. |
| VG-07 Denied-to-Unavailable breadth | medium | defer | Same defect as BH-27. |


## Design Notes

- The rollback target stays 3.70.1, as in the accepted record, so the existing probe can be reused. The storage record blobs are identical from v3.70.1 through v3.109.0; `AggregateMetadata.RetainedFloor` arrives only after the tag.
- The tag's CI failure is three Contracts tests in the OQ8 Story 4.15 v5 evidence validation (governance tests, not library runtime). Green tag CI is still absent.
- Environment: `dotnet test` and live Dapr runs need the sandbox disabled and serial `-m:1`. Qualification restores use an isolated `NUGET_PACKAGES` and `CI=true`, because the global NuGet cache holds locally packed Hexalith packages that shadow nuget.org.
- Commit sequence: Builds and EventStore owner commits, then superproject gitlinks, then packet capture, then the Projects commit. `source-closure.json` must show zero uncommitted bindings.

## Verification

**Commands:**
- `python3 references/Hexalith.Builds/Tools/test-runtime-toolchain-evidence-validator.py` -- expected: every scenario passes for each baseline.
- `python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-29.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260929/packet.json --candidate` -- expected: exit 0; exit 1 without `--candidate` while the packet is pending.
- `sha256sum references/Hexalith.Builds/Tools/runtime-toolchain-baseline.json references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-27.json` -- expected: `525615c6…` and `b9aa6791…`.
- `python3 references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3109/verify_public_packages.py` -- expected: exit 0.
- `python3 -m unittest tests/tools/test_p1r_candidate_evidence.py -v` -- expected: 6 tests pass (needs network for GitHub Actions run metadata).
- `python3 tools/planning/validate_production_authority.py --validate-index` -- expected: exit 0.
- `pwsh -NoProfile -File ./tests/tools/run-ci-workflow-gates.ps1` -- expected: pass.
- `git diff --check` in each changed repository -- expected: no whitespace errors.
