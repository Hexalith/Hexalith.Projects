---
title: '6.1-P1R Verify published EventStore 3.110.0 archives'
type: 'chore'
created: '2026-10-01'
status: 'ready-for-dev'
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
- [ ] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/public-packages.json` -- record the 14 published `3.110.0` hashes, sizes, release URLs, and `.nuspec` commit `27279fe6…`
- [ ] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verify_public_packages.py` -- replay the 3109 checks against that record and `v3.110.0`
- [ ] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/consumer/` -- restore and build the published libraries, then install `Hexalith.EventStore.Admin.Cli` `3.110.0` from NuGet.org
- [ ] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/README.md` -- record the coordinates, commands, and results, including the already accepted release bypass
- [ ] `_bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md` -- cite the new evidence and leave rollback and checkout open
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- set `eventstore_archive_verification` to the new evidence path and keep `usable_as_prerequisite: false`

**Acceptance Criteria:**
- Given the accepted tag, when the replay and consumer commands finish, then all 14 manifest IDs match NuGet.org, the GitHub release, and commit `27279fe6431925a6ea046c3f89af61487185c7de`.
- Given that archive proof, when the guard and G-6 reference test run, then the acceptance tuple is unchanged and `usable_as_prerequisite` remains `false`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `python3 references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verify_public_packages.py` -- expected: exit 0 and `PASS: 14 public packages`
- `python3 tools/planning/validate_production_authority.py --validate-index` -- expected: exit 0
- `python3 -m unittest tests/tools/test_production_authority_guard.py tests/tools/test_g6_packet_references.py -v` -- expected: exit 0, with `usable_as_prerequisite: false`
