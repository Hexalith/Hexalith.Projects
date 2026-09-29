---
title: '6.1-P1R Complete 3.108.1 evidence and qualification'
type: 'chore'
created: '2026-09-26'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '2e0c0b55a732a474f3a70cbfcda279da392d17b5'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-3108-exact-baseline-candidate.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-1-p1r-acceptance.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The pending 3.108.1 P1R candidate has source inventory but lacks published-package provenance, later-checkout compatibility assessment, current Builds qualification, an Architecture Spine disposition, and a fresh G-6 result. Its historical 3.106.0 acceptance and G-6 packet cannot authorize the new tuple.

**Approach:** Gather exact, independently checkable evidence in each owner repository; prepare new Projects decision and G-6 packets; reconcile the Spine by showing the pending rebind while retaining its accepted 3.70.1 binding until the authorized owner decides.

## Boundaries & Constraints

**Always:** Bind claims to full Git commits, package IDs and versions, hashes, commands, exit codes, and evidence paths. Distinguish the 3.108.1 release tag from the later EventStore checkout and published packages from local builds. Preserve existing user changes, the accepted 3.106.0 P1R record, 3.70.1 rollback, historical G-6 packet, and blocked Stage 6 and Story 6.1 states.

**Never:** Infer four-role P1R acceptance or G-6 owner approval; reseal old evidence; substitute a local package build for a published artifact; advance P0, P2, P3, P4, implementation readiness, or Story 6.1.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Published release family | All 14 manifest IDs at 3.108.1 | Per-ID provenance, hash, and independent restore or consumption result tied to tag | Missing or mismatched ID stays unresolved |
| Source drift | Tag versus later checkout | Scoped compatibility diff and test evidence | Untested behavior is named, not assumed compatible |
| Changed toolchain | Current pins and source bindings | New G-6 packet and validator result | Historical approval remains scoped to its old packet |
| Pending architecture | Spine binds 3.70.1 | Explicit proposed rebind and rollback decision | Current binding stays authoritative pending sign-off |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/tools/release-packages.json` and the owner P1R artifacts -- 14-ID release inventory and source/package compatibility evidence; do not alter the tag.
- `references/Hexalith.Builds/Props/Directory.Packages.props`, `src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs`, `schemas/hexalith.module-manifest.v1.json`, and its owner P0 record -- current catalog, runner, schema, and qualification source.
- `_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain/` -- historical immutable packet and validator baseline; use a new candidate path.
- `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` -- accepted 3.70.1 Stack binding; record pending 3.108.1 conformance separately.
- `_bmad-output/implementation-artifacts/6-1-p1r-3108-exact-baseline-candidate.md` and `sprint-status.yaml` -- pending decision and blocked dependency chain; keep historical fixed JSON and guard intact.

## Tasks & Acceptance

**Execution:**
- [x] EventStore owner evidence -- verify published 14-package set, source-to-package provenance, hashes, restore and consumption, and tag-to-checkout compatibility.
- [x] Builds owner evidence -- qualify exact revision and 3.108.1 catalog, runner, schema, tests, and available published tool coordinates.
- [x] New G-6 candidate preflight -- execute current pin and source-state validation, record exact failure and owner disposition needed.
- [ ] Owner-accepted live G-6 packet -- requires the Toolkit .767 owner decision, exact CLI/runtime, two-sidecar capture, strict packet validation, and named approval.
- [x] Spine and Projects packet -- reconcile the binding as pending, assemble exact owner decisions and prerequisite effects without changing accepted authorities.
- [x] Validate narrow checks in each changed owner repository; report command, exit code, and remaining risks.

**Acceptance Criteria:**
- Given the tagged EventStore release, when all 14 package IDs are checked, then the evidence identifies each published artifact and any provenance or consumption gap explicitly.
- Given the newer EventStore checkout and Builds revision, when the packet is reviewed, then exact compatibility and qualification claims have reproducible evidence and scoped limitations.
- Given changed G-6 pins, when the new packet is validated, then its result and needed owner decisions are explicit while the earlier approval is unchanged.
- Given the incomplete prerequisite chain, when authority records are inspected, then 3.106.0 remains the accepted P1R tuple and P0 Stage 6 and Story 6.1 remain blocked.

## Implementation Notes

The original request authorized evidence gathering and owner-repository edits while withholding inferred acceptance. On 2026-09-27 Jérôme Piquot identified himself as sole contributor and owner and said “approve now” after the named candidate, rollback-target, Toolkit pin, and isolated test decisions were presented. This authorizes those directions and the qualification work. It does not make a pending technical result pass or supply a post-run, exact-packet acceptance decision.

- EventStore published package evidence covers all 14 manifest IDs with per-package NuGet.org and release-asset hashes, repository-commit metadata, identical unsigned payload entries, 14 verified signatures, independent 13-library restore/build and CLI tool startup. The release executed Builds 22a578b576a515d2af214fe81859447fffc97981; candidate Builds is a later descendant. The current EventStore checkout at 1cc6b44b4a71950fbd8f92469bebad3b0c9c05c5 adds public methods and behavior, so owner compatibility disposition remains open. A disposable 3.108.1-to-3.70.1 storage read and RDB restore pass for selected records; full actor/domain replay and API downgrade remain material gaps.
- Builds CI run 36193212159 passed at exact candidate SHA with 214 Module and 107 Evidence tests. Its 0.0.0-ci.255 tool-package qualification is local, 12 live integration tests were skipped, and no published consumer pin or current persisted proof exists.
- Fresh G-6 candidate packet binds 71 source files and separately audits 44 exact pin declarations. The isolated Dapr 1.18.2 two-sidecar qualification passed 1/1 with 33/33 support tests; eight AppHosts freshly resolve Toolkit .767 after the Conversations owner fix. Candidate validator exits 0; accepted-only validator exits 1 because packet status remains pending. FrontComposer broad AppHost build fails with 49 errors, package-exception inventory exits 1 with 14 drifts, and seven bound files remain uncommitted. Exact post-run owner acceptance is pending.
- The Architecture Spine records the proposed rebind as pending while its Stack table stays at 3.70.1. Projects sprint status indexes the new evidence and keeps the 3.106.0 record, P0 Stage 6, Story 6.1, and readiness states intact.

## Spec Change Log

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

## Verification

**Observed results:** production-authority guard exited 0; the 14-package public replay exited 0; the in-place 13-library consumer restore/build and CLI tool smoke exited 0; the exact Builds package/runner probe exited 0 with eight EventStore 3.108.1 host dependencies. The retained G-6 validator exits 1 on Toolkit Dapr .757 versus .767. The new G-6 candidate validator exits 0 for pending status, while its accepted-only mode exits 1 as intended; the live two-sidecar qualifier and support tests pass. Conversations .767 restore/build/focused tests pass. An isolated published-package 3.108.1-to-3.70.1 record read and disposable RDB restore pass, with actor/domain replay unproven. Root workflow policy gate, YAML parsing, blocked-status assertions, and whitespace checks pass. Exact P1R and G-6 post-run owner decisions, immutable source closure, published Builds tool pin, and current persisted Stage 6 proof remain open.

**Commands:**
- `python3 tools/planning/validate_production_authority.py --validate-index` -- historical authority still valid.
- `python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain/packet.json` -- current drift is reported with exit 1; new candidate preflight records pending owner/live work.
- `python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-27.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20260927/packet.json --candidate` -- exits 0, `G6-EVIDENCE-CANDIDATE-VALID`; without `--candidate` exits 1 because the packet is pending.
- `pwsh -NoProfile -File ./tests/tools/run-ci-workflow-gates.ps1` -- five workflow files pass, including exact Builds `2326f983...` CI refs and the accepted-only fresh G-6 gate.
- `git diff --check` in every modified owner repository -- no whitespace defects.
