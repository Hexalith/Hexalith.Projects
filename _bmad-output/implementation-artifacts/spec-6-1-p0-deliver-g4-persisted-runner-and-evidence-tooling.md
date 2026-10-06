---
title: '6.1-P0: Qualify published G-4 tools and complete the owner handoff'
type: 'feature'
created: '2026-10-01'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
work_package_id: '6.1-P0'
implementation_repository: 'references/Hexalith.Builds'
observed_builds_revision: '21ce044ab465ccb2adab58b3d66e394ffbecf3c2'
baseline_commit: '4d8dcf65803792f7def3b10ed21227329536154b'
context:
  - '{project-root}/references/Hexalith.Builds/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** P0's Stages 1–5 have historical slice qualification; current published tools lack accepted baseline, persisted qualification, rollback, and handoff evidence.

**Approach:** Resume Stages 6–7 in Hexalith.Builds after independently accepted P1R/G-6 inputs exist. All owner-story ACs remain mandatory, including required profile classes and retained failure evidence.

**Decision (2026-10-01):** User instructed “do recommended”: target EventStore `3.110.0`, Builds `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`, and published tools `4.29.1`. Prepare isolated remote-consumer controls while prerequisite acceptance is pending; this is no owner acceptance.

## Boundaries & Constraints

**Always:** Bind one immutable source/package tuple; preserve completed implementation and historical evidence; require clean qualification, real persisted event/projection sequences, both native test platforms, metadata-only retention, and three dated owner approvals.

**Never:** Self-accept P1R/G-6; alter historical packets; infer acceptance from publication or synthetic fixtures; change Projects runtime/routing; initialize nested submodules; publish, commit, push, or update dependencies.

## I/O & Edge-Case Matrix

| State | Required behavior |
|---|---|
| P1R/G-6 missing or drifting | Record blocker; stop live qualification |
| Exact published tools restore | Record version/feed/hash availability; acceptance remains pending |
| Failed, unavailable, zero/all-skipped native lane | Retain causal nonpassing evidence |
| Cleanup repeated | Only invocation-owned resources removed |
| Record missing approval or bound artifact | Packaged validator exits 6 |

</frozen-after-approval>

## Code Map

All implementation paths below are relative to `references/Hexalith.Builds`.

Projects `evidence/6-1-p0-20261001-preflight.json` beside this spec records the earlier pins/restore/G-6 checkpoint; P1R subsequently accepted `3.110.0`.

- `_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md` — binding Stages 6–7 and hard stops.
- `src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs` and `Props/Directory.Packages.props` — current candidate pins.
- `Tools/test-g4-tool-package-contracts.ps1` — clean source/package controls.
- `Tools/G4PackageQualification.functions.ps1` and `Tools/publish-g4-tool-packages.ps1` — source/fixture guards and signing-aware remote comparison.
- `test/Hexalith.Builds.Tooling.IntegrationTests/Live/PackagedPersistedProfileTests.cs` — installed VSTest/MTP persisted lanes.
- `src/libraries/Hexalith.Builds.Tooling/Evidence/G4P0AcceptanceValidator.cs` — acceptance binding; rollback remains an attestation.
- `Tools/README.md` and `README.md` — consumer contracts; reconcile stale HXR003 availability text.

## Tasks & Acceptance

**Execution:**

- [x] Owner story — consume independently recorded four-role P1R acceptance of `3.110.0` / Builds `21ce044` at `2026-10-01T06:17:01Z`.
- [ ] Owner story — consume fresh accepted G-6 inputs before any live run.
- [x] `test/fixtures/package-consumer/.config/dotnet-tools.json` — pin `4.29.1`; isolated restore/version and 97 synthetic controls passed. Record candidate results under `evidence/g4/`; P1R is accepted; G-6 remains pending.
- [x] `Tools/validate-runtime-toolchain-evidence.py` — correct stable/RC Fluent UI disposition validation; preserve historical bytes and verify positive/negative regression controls. No baseline acceptance is inferred.
- [ ] `test/fixtures/module/executable/` and `test/Hexalith.Builds.Tooling.IntegrationTests/Live/PackagedPersistedProfileTests.cs` — reuse fixture/profile inputs for clean Debug/source and Release/package qualification; retain native reports, sequences, negative controls, cancellation, and cleanup evidence.
- [ ] `evidence/g4/` — capture remote package provenance, signing-aware comparisons, idempotent down, and an exercised rollback drill; keep failed evidence.
- [ ] `evidence/g4/6.1-p0-acceptance.json` — assemble actual bound artifacts, then obtain Builds Owner, Platform Owner, and named Test Architect decisions on that exact delivery.
- [ ] `README.md`, `Tools/README.md`, and owner story — document tested adoption/rollback and hand off to P4 after independent packaged validation.

**Acceptance Criteria:**

- Given accepted matching prerequisites, when clean source and installed package lanes execute, then both native platforms prove persisted two-module behavior with no critical skips.
- Given actual cleanup/rollback results and dated owner decisions, when the packaged acceptance validator runs, then it passes the exact delivery record; omission/tampering controls fail.
- Given accepted P0 delivery, when P4 receives the handoff, then Story 6.1 retains its remaining independent gates.

## Implementation Notes

- Target and continuation authorized on 2026-10-01. Preserve concurrent P1R/P2/P3 work; do not edit those artifacts or the EventStore worktree.
- Candidate evidence: [published controls](../../references/Hexalith.Builds/evidence/g4/published-4.29.1-20261001/README.md); 97 controls, 107 evidence tests, 214 module tests passed. Source/package gate exited 143 without inventory; first module attempt and harness failures retained.
- [G-6 preparation](../../references/Hexalith.Builds/evidence/g4/g6-current-target-preparation-20261001/README.md): 165 authority controls; current packet/catalog/gitlink/CI drift remains blocked. P1R acceptance was consumed from the concurrent owner record; final qualification, rollback, and exact P0 owner decisions remain open.
- **Blocker (2026-10-06), recorded at user direction:** live qualification stopped under the matrix's first row. No Stage 6–7 run, acceptance record, or owner decision was produced.
  - **P1R:** the [2026-10-06 proposal](../planning-artifacts/sprint-change-proposal-2026-10-06.md) keeps the `3.110.0` acceptance. It marks current usability false after attempt 21 measured seven incompatibilities. [P1R remediation](spec-6-1-p1r-remediation.md) is `handed-off` and waiting for repository-local scope.
  - **G-6:** current G-6 is failed. CI authority run `37106225245` at `e7dc4d876793f6254ce42409c9067bd6516901d4` failed for two reasons: the pinned PostgreSQL image was missing, and Hexalith.Commons hit `NU5118`. The packet is still pending.
  - **Drift:** Builds is at `ba4ca78c3868a4757cb92d912a54c8a237871b54` (`v4.29.1-22-gba4ca78`, equal to the Projects gitlink). Since `567a80f5726960c7ec66864d67849b22f9164bf0` it defaults EventStore to `3.113.0`. The consumer fixture still pins tools `4.29.1`.
  - Resume only once a usable, accepted P1R tuple and an accepted G-6 result bind the same tuple. Any tuple other than the frozen Decision's requires renegotiating it.

## Spec Change Log

## Review Triage Log

## Verification

- Independently validate P1R and fresh G-6 against the selected immutable tuple before execution.
- Current outcomes: production-authority story/index guards pass for the independently accepted P1R tuple; G-6 candidate gate exits 1 with seven drift checks.
- Passed: 97 installed controls, 107 evidence tests, 214 module tests, and G-6 mutations (30 packet scenarios per each of three baselines, 48 drift controls, 165 authority controls, two historical pins). Original-validator regression fails as expected.
- Independent audit verifies 215 candidate artifacts, 194 command streams, 81 exact negative vectors, and 233 fixture bindings. Source/package gate remains nonpassing at exit 143/no inventory. Historical snapshots stay preserved; live/native failure, cancellation, cleanup/rollback, and final acceptance matrix coverage remain incomplete.
- Run `Tools/test-g4-tool-package-contracts.ps1 -Version` with a unique local version and `-RequireControls`; use fresh output.
- In the clean remote consumer, run `dotnet tool restore`, installed `hexalith-module test` with `full`/`full-mtp`, required negative controls, and repeated `down`.
- Run `dotnet tool run hexalith-evidence validate evidence/g4/6.1-p0-acceptance.json`; require exit 0 and all blocking controls to fail as specified.
