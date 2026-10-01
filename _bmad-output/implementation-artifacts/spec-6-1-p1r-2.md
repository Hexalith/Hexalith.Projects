---
title: '6.1-P1R Verify published EventStore 3.110.0 archives'
type: 'chore'
created: '2026-10-01'
status: 'done'
baseline_commit: 'de83364b5ea653b6893038ae8cd001473839ae47'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3109/README.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** EventStore `3.110.0` / `27279fe6431925a6ea046c3f89af61487185c7de` with Builds `4.29.1` / `21ce044ab465ccb2adab58b3d66e394ffbecf3c2` is accepted, and `usable_as_prerequisite` stays false. No current 14-package archive, signature, or consumer replay exists for that tuple.

**Approach:** Record that published-package proof for the accepted tag. Leave rollback, checkout compatibility, and the downstream schedule unchanged.

**Decision (2026-10-01):** Record the archive proof: the 14 published `3.110.0` packages, their signatures, and a NuGet.org consumer replay. Rollback and checkout stay open.

## Boundaries & Constraints

**Always:** Bind every package to tag commit `27279fe6431925a6ea046c3f89af61487185c7de` and manifest SHA-256 `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae`. Consume NuGet.org and the GitHub release only. Keep `usable_as_prerequisite: false`. Preserve the `3.109.0` evidence directory unchanged.

**Never:** Change schema `hexalith.projects.p1r-acceptance.v1`, the selected or rollback tuples, or Story 6.1. Do not treat G-6 attempt 16, a local EventStore build, or `tests/tools/test_p1r_candidate_evidence.py` as this proof. Do not add a CI caller for the replay. Do not publish, deploy, commit, or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Archives match | Live NuGet.org and GitHub `v3.110.0` assets for all 14 manifest IDs | Hashes, sizes, `.nuspec` commit, shared ZIP bytes, and 14 repository signatures match the record | Stop when any ID, hash, commit, or signature differs |
| Consumer restore | Fresh NuGet.org cache and the checked-in consumer | 13 libraries build at `3.110.0`; `eventstore-admin --version` prints `3.110.0+27279fe6431925a6ea046c3f89af61487185c7de` | Stop when a package comes from another source or version |
| Other limitations | Archive proof recorded | Rollback, checkout, and `usable_as_prerequisite: false` stay as they are | Guard and the G-6 reference test fail if usability is flipped |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3109/verify_public_packages.py` — replay to copy: tagged manifest, live assets, NuGet hashes, `.nuspec` commit, ZIP equality, `dotnet nuget verify --all`, and `tools/validate-release-packages.py`. Checkout `8096455e…` has the same manifest as `v3.110.0`.
- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3109/consumer/` — reuse `PublishedApiSmoke.cs` and the `Admin.Cli` install. Leave `rollback-probe/` behind.
- `_bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md` — line 36 says no `3.110.0` archive replay exists. Append the new evidence there.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `eventstore_archive_verification` is `not-run-for-3.110.0`. Point it at the new evidence and keep `usable_as_prerequisite: false`.
- `tools/planning/validate_production_authority.py` — `SELECTED_TUPLE` and `ROLLBACK_TUPLE` stay unchanged. The guard does not read archive evidence.
- `tests/tools/test_g6_packet_references.py` — requires `usable_as_prerequisite: false`. Keep that assertion.
- `tests/tools/test_p1r_candidate_evidence.py` — still replays `v3.109.0`. Leave it on that historical lane.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/public-packages.json` -- record the 14 published `3.110.0` hashes, sizes, release URLs, and `.nuspec` commit `27279fe6…`
- [x] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verify_public_packages.py` -- replay the 3109 checks against that record and `v3.110.0`
- [x] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/consumer/` -- restore and build the published libraries, then install `Hexalith.EventStore.Admin.Cli` `3.110.0` from NuGet.org
- [x] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/README.md` -- record the coordinates, commands, and results, including the already accepted release bypass
- [x] `_bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md` -- cite the new evidence and leave rollback and checkout open
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- set `eventstore_archive_verification` to the new evidence path and keep `usable_as_prerequisite: false`

**Acceptance Criteria:**
- Given the accepted tag, when the replay and consumer commands finish, then all 14 manifest IDs match NuGet.org, the GitHub release, and commit `27279fe6431925a6ea046c3f89af61487185c7de`.
- Given that archive proof, when the guard and G-6 reference test run, then the acceptance tuple is unchanged and `usable_as_prerequisite` remains `false`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

- Untracked evidence under the EventStore gitlink — `medium`, rejected. The files are untracked, and a clean checkout of `8096455e…` will not contain them. The frozen intent forbids commit and push, so that durability step is outside this build.
- `eventstore_archive_verification` has no content digest — `low`, rejected. The spec requires a path. The replay re-checks the recorded hashes, and adding a second digest is more than a direct correction.
- `g6_failures` and `containment` still say verification limitations — `false`. Those sentences still correctly keep usability false. They do not say the archive replay is missing. `eventstore_archive_verification` now points at the new evidence.
- Stale Code Map and empty logs — `low`, rejected. The fix would edit this build's spec. The Code Map records the pre-change state the tasks then update.
- Unit-test note records exit 1 — `low`, rejected. The fix would edit this build's spec. The eight failures are pre-existing G-6 gitlink drift, and the guard still passes `usable_as_prerequisite: false`.
- Consumer restore is not inside `verify_public_packages.py` — `false`. The spec keeps that restore as its own task, matching the 3.109.0 split.
- `fixture_sha256` is not rechecked and the consumer date is a calendar day — `low`, rejected. Those fields record the run that already happened. Rechecking them on every archive replay is not a demonstrated failure.
- No SDK pin, lock file, or fallback-folder clear — `low`, rejected. The consumer config is the specified 3.109.0 shape. This machine's NuGet config has no fallback folders, and the recorded restore metadata names NuGet.org.
- Registration URLs, `release_asset_count`, and non-`.nupkg` assets are unchecked — `false`. The acceptance stop condition is an ID, hash, commit, or signature mismatch for the 14 packages. The replay already requires those 14 `.nupkg` names.
- Checkout commit and the 28-commit stat are unchecked — `false`. The PASS line is the archive contract. Checkout compatibility stays open, and the script does hash the shared release manifest.
- `validate-release-packages.py` reads the working tree, so post-tag `Dapr.Actors.AspNetCore` is invisible — `false`. That reference is absent at `v3.110.0` and the contract ignores non-Hexalith package references. The published archives are the tagged packages.
- `PublishedApiSmoke` compiles seven types from two packages — `false`. The spec reuses that 3.109.0 smoke file. All 13 libraries are still restored as exact `3.110.0` package references.
- `subprocess.run` has no timeout — `low`, patch. `download()` bounds HTTP at 90 seconds, and `run_command()` does not. A hung `git` or `dotnet` child blocks the replay. Smallest fix: timeout the child and close its stdin.
- `PackageReference Version="3.110.0"` can float upward — `false`. NuGet selects the lowest version that satisfies that constraint. A later `3.111.0` is not selected unless another constraint requires it, and the `3.110.0` graph does not.

## Verification

**Commands:**
- `python3 references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verify_public_packages.py` -- exit 0; `PASS: 14 public packages match recorded hashes, tagged source metadata, release assets, ZIP payloads, signatures, and manifest contract`
- Fresh NuGet.org consumer restore, Release build, and `eventstore-admin --version` -- exit 0; build `0 Warning(s)` / `0 Error(s)`; version `3.110.0+27279fe6431925a6ea046c3f89af61487185c7de`; 13 restored library archives match the recorded NuGet.org hashes and name NuGet.org as the source
- `python3 tools/planning/validate_production_authority.py --validate-index` -- exit 0; `PASS: production-authority index is [6, 7, 8]`
- `python3 -m unittest tests/tools/test_production_authority_guard.py tests/tools/test_g6_packet_references.py -v` -- exit 1. All 21 guard tests passed, including the selected tuple and `usable_as_prerequisite: false`. Eight failures are pre-existing G-6 gitlink drift: current Builds `611f7e882ec615bad534de73149c7e95431b4cd4`, Commons `c13dc6679aa91144b6d541078f3f20019d79c2eb`, and McpCli `cc51de992c3f328207de4851a6d35fbd35f5c63f` are not the revisions quoted by the accepted attempt-16 packet. This archive proof does not rewrite that packet.
