---
title: '6.1-P1R Qualify and Accept the Current EventStore Baseline'
type: 'bugfix'
created: '2026-09-20'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c3b8c4a1cf24fe88ca4e124d0adebecb8bd88d07'
context:
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
  - '{project-root}/references/Hexalith.Builds/DEVELOPMENT.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** 6.1-P1R is open: the explicit EventStore selection is `v3.103.0`, while Builds catalogs `3.106.0` but its runner corpus remains on `3.102.0`. No current candidate passed qualification and four-owner acceptance.

**Approach:** Select one published coordinate, align an immutable Builds candidate, run contract phases 1–5 with retained evidence and executable `3.70.1` rollback, then apply the chosen acceptance boundary.

**Decisions:** Supersede the recorded `v3.103.0` selection with published EventStore `v3.106.0` / `76051c70cbf868c40edc00ca0344fa5bd8879b69`, keeping untagged HEAD unselected. Complete full P1R closure after every gate passes. Jérôme Piquot is the authorized named approver for the EventStore Owner, Builds Owner, Solution Architect, and Test Architect roles; each final role decision is recorded only against the completed finite record.

## Boundaries & Constraints

**Always:** Use disk-backed, reusable clean worktrees keyed by exact revision; materialize read-only dependency worktrees at exact EventStore gitlink SHAs; share ordinary NuGet and version-keyed Playwright caches; use an empty disposable cache only for the official-remote proof; retain first results as ledger rows, relevant logs, artifacts, and hashes without copying workspaces or caches; resume at the earliest invalidated row; clean disposable state on every exit; preserve `3.88.0`/`3.70.1` negatives; create commitlint-valid local candidate and two-commit rollback revisions.

**Never:** Initialize nested submodules, move gitlinks, edit EventStore production, weaken gates, relabel untagged HEAD as package source, rewrite history, push/publish/release, infer acceptance, or close downstream gates. Architecture/P1R change only after four-owner acceptance of one record.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Candidate | Selected tag/package and aligned Builds | All required phase rows pass | Retain first non-pass; leave P1R open |
| Materialization | Exact dependency bytes/Git objects | Governance tests run without nested initialization/live mutation | Missing/wrong SHA is non-qualifying |
| Rollback | `v3.70.1` plus two Builds commits | Accept rollback; reject candidate with `HXM016` | Any failure blocks acceptance |
| Acceptance | Complete finite record | Four named roles decide on identical evidence | Pending/reject leaves state unchanged |

</frozen-after-approval>

## Code Map

- `_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md` -- exact phases, commands, record schema, owner scopes, propagation.
- `_bmad-output/implementation-artifacts/deferred-work.md` (`DW-35`, `DW-68`) -- existing coordinate/materialization/rollback decisions.
- `references/Hexalith.Builds/{Props/Directory.Packages.props,Tools/package-version-audit.json}` -- current catalog/audit.
- `references/Hexalith.Builds/{src/libraries/Hexalith.Builds.Tooling/Manifest,schemas,test,Tools}` -- runner pins, fixtures, hashes, controls, tests.
- `references/Hexalith.EventStore/{Directory.Packages.props,Directory.Build.props,tools/release-packages.json,tests}` -- read-only import, dependency, 14-package, and governance surfaces.
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md` -- append-only record.
- `_bmad-output/implementation-artifacts/{qualification-evidence,sprint-status.yaml}` -- evidence and gated propagation.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/` -- align runner/schema/tests/fixtures/evidence/controls, recompute hashes, pass audit, and commit the candidate.
- [x] `references/Hexalith.EventStore/` -- create disposable exact-revision lanes, materialize dependencies, and run coordinate, source/package, seven-API, governance, and 14-package remote-restore gates.
- [ ] `references/Hexalith.Builds/` -- run candidate/G-4 lanes; create and prove the two-commit `3.70.1` rollback.
- [ ] `qualification-evidence/` and canonical record -- retain a manifest-verified bundle and append the finite record.
- [ ] Architecture, `sprint-status.yaml`, and DW-35/DW-68 -- propagate only the acceptance state authorized by the chosen completion boundary; preserve every downstream blocker.

**Acceptance Criteria:**
- Given the selected candidate and rollback, when contract phases 1–5 run from verified clean worktree bindings, then every required row is `PASS`, all evidence hashes resolve, exact-pin negatives are deterministic, unchanged passing rows are reused by binding, disposable state is cleaned, and no nested submodule or live checkout is changed.
- Given the finite record, when the completion boundary is applied, then technical-only work remains explicitly pending, or four named acceptances rebind Architecture and close only P1R against identical revisions.

## Implementation Notes

- Created immutable local Builds candidate
  `5730f5a60c0e106131498126cbee323e24db5f4e` on
  `fix/p1r-3106-candidate`, then superseded it with corrected candidate
  `ad52f350a2f0bc47849179ae17b4594dafff5363`. The first commit aligns 61
  active runner, schema, test, fixture, serialized-evidence, package-control,
  and coupled-hash files to `3.106.0`; the second changes only otherwise-valid
  retained artifacts and their YAML hashes to bind them to the current
  manifest bytes. Repository-pinned commitlint, static parity, the
  authoritative catalog, and the complete package audit passed.
- Materialized the selected EventStore tag plus all seven dependencies as
  disposable exact-revision worktrees without initializing a nested submodule.
  Coordinate capture, the seven-API comparison, source restore, and source
  build passed.
- Attempt 3 stopped at `es-source-003-contracts-test`: the contract-exact
  command ending in `-m:1` exited `5` with zero tests under the repository's
  pinned Microsoft.Testing.Platform runner. Per the stop-first rule, no later
  source/package/remote-restore/runner/rollback or acceptance phase ran.
- After the explicit test-option renegotiation, attempt 4 restarted from
  coordinate capture. The renegotiated Contracts test passed, but
  `es-source-004-client-test` stopped with `CS1704`: restored assets selected
  the `Hexalith.Commons.UniqueIds` 2.30.0 package while source-mode build
  evaluation selected the exact-dependency project output (version 1.0.0).
  The post-failure state check passed for EventStore and all seven exact
  dependency worktrees. The stop-first rule again prevented later phases.
- After the explicit `--no-build` renegotiation, attempt 5 restarted from
  coordinate capture. Source restore/build passed, but the first Contracts
  test row exited `2`: 2,022 of 2,023 tests passed and
  `ReleaseSourcePreflightFailsClosedAndEmitsSelectedSuccessfulPushProof`
  observed process exit `126` rather than `0`. The clean-state check passed;
  the explicit stop-first instruction prevented all later rows.
- Attempt 6 preserved attempt 5's first failure and used a fresh clean
  serialized context under the contract's contention-rerun rule. All source
  and package-source restore/build/test rows passed, including 2,023/2,023
  Contracts tests in both modes. The first remote-consumer restore then
  stopped with `NU1008` because the retained project inherited umbrella
  central package management and used inline exact versions. It was not
  corrected or rerun; the clean-state assertion passed.
- Attempt 7 corrected the consumer location and again passed the full source
  and package-source lanes. The pre-restore effective-CPM proof stopped with
  an unmatched shell quote before invoking MSBuild (exit `2`). The proof was
  not repaired or rerun; external consumer/config bytes and clean-state proof
  are retained.
- Attempt 8 restarted unchanged but stopped during source build after `/tmp`
  filled (`MSB3021`/`MSB3491`, exit `1`). Disk state and clean worktree proof
  are retained; no preserved attempt storage was deleted and no rerun ran.
- Attempt 9 used a new root after safe cleanup and passed every EventStore
  source, package-source, external-CPM, official-only remote-restore, and
  14-package inventory row. Builds candidate restore/build and Module tests
  also passed. The next Evidence-test row exited `2` with 59/68 tests passing
  and 9 failures centered on unexpected `HXE152` evidence-binding diagnostics.
  Stop-first prevented catalog/audit/G-4, rollback, and acceptance rows.
- Attempt 10 restarted with corrected candidate `ad52f350...`. Every selected
  EventStore, external-consumer, candidate build/test, and official required-
  control G-4 row passed; the G-4 bundle retains both CLI packages, both symbol
  packages, and 53 qualification artifacts. Two local rollback commits were
  created (`44bd5eb...` for the `3.70.1` binding and `caf49c5...` for its
  incremental audit), but the first rollback qualification row stopped because
  both commit bodies exceeded commitlint's 200-character line maximum. The
  commits were not rewritten and no later rollback or acceptance row ran.
- Attempt 11 preserved the failed rollback branch and created distinct branch
  `fix/p1r-3106-rollback-candidate` with commitlint-valid commits
  `38bac790...` and `1f46bc3...`. Their trees exactly equal attempt 10's
  corresponding rollback trees. Adoption, reciprocal candidate controls,
  rollback static alignment, restore/build, Module, Evidence, and catalog rows
  passed. Audit validation then stopped because the byte-identical audit names
  old commit `44bd5eb...`, which is not an ancestor of the new linear branch.
  No rollback G-4, `v3.70.1` EventStore, or acceptance row ran.
- Appended the stopped-run record on local Builds branch
  `docs/p1r-3106-attempt3-record`, including attempt-4 commit
  `78760548cd270877e58fb6998c1812a354b30196` and attempt-5 commit
  `df01dffe808cec58190dbf52ca7d2f72a0b697d2`, plus attempt-6 commit
  `d6778bcddfc878cc9e2ccf32e3e00d65c28c6751` and attempt-7 commit
  `11601491421bc7ecacdf0e8cbe771faf259b5b23`, plus attempt-8 commit
  `a240dbdc70f79cd319c4398e2e58a12f3685cc05`, plus attempt-9 commit
  `6f1d3e0373229010f102de47c098e455d5d8c0fd` and retention-correction commit
  `132bb86ab1526dff1a28b73b054081330de60a6d`, plus attempt-10 commit
  `bc9d464d15804bd81906171829348c9503d6b0d8`, plus attempt-11 commit
  `a7958e0bed2c3ebc03e5f88669c95aed25c99adb`; Architecture, sprint status,
  DW-35, DW-68, and every downstream blocker remain unchanged.

## Spec Change Log

- 2026-09-21: The human explicitly replaced per-attempt isolation and append-only
  workspace retention with pragmatic binding-based reuse. Qualification now
  uses a disk-backed root, reuses exact-revision worktrees and ordinary
  dependency/browser caches, reserves an empty cache for the official-feed
  proof, resumes at the earliest invalidated row, stores evidence once by
  content hash, and cleans disposable state on every exit.
- 2026-09-21: The human explicitly renegotiated all ten qualification
  `dotnet test` rows to add `--no-build`, retaining `--no-restore` and
  `--max-parallel-test-modules 1`. All `dotnet build -m:1` rows, production
  source, prior evidence, and the approved frozen block remain unchanged;
  qualification restarts from coordinate capture as attempt 5.
- 2026-09-21: The human explicitly renegotiated the qualification contract's
  `dotnet test` serialization option from `-m:1` to
  `--max-parallel-test-modules 1`. All `dotnet build -m:1` rows and the
  approved frozen intent, boundaries, decisions, and matrix remain unchanged.
  Prior stopped attempts and their evidence are preserved; qualification
  restarts from coordinate capture as attempt 4.
- 2026-09-21: Recorded the immutable Builds alignment candidate and the
  stopped, non-qualifying EventStore `3.106.0` attempts. Left the spec
  `in-progress` because required contract rows and all four acceptances remain
  incomplete.
- 2026-09-21: Recorded attempt 9's complete passing EventStore/remote-package
  phase and its first Builds Evidence-test failure. The exact contract and
  candidate remain unchanged; rollback and all four acceptances remain
  incomplete.
- 2026-09-21: Recorded corrected candidate `ad52f350...` and attempt 10. The
  full selected-coordinate and candidate/G-4 technical lanes passed; rollback
  stopped at commitlint before static/build/test qualification. The frozen
  block, contract, Architecture, sprint status, deferred work, downstream
  gates, and all four owner decisions remain unchanged.
- 2026-09-21: Recorded attempt 11's tree-equivalent, commitlint-valid rollback
  branch and first deterministic audit failure. The exact-tree constraint
  retains old audit provenance that is not ancestral to the new branch, so
  rollback and owner acceptance remain incomplete; no propagation occurred.

## Review Triage Log

## Design Notes

EventStore HEAD is `v3.106.0-29-gffb6901a` with production changes. Exact dependency materialization may shadow sibling Builds; record the catalog each lane resolves.

The current `global.json` opts `dotnet test` into Microsoft.Testing.Platform,
whose SDK `10.0.401` command surface does not expose `-m:1`; the human
explicitly renegotiated those test rows to use
`--max-parallel-test-modules 1`. Attempt 4 then exposed a distinct source-mode
graph inconsistency: restore retained the `Hexalith.Commons.UniqueIds` package
while build evaluation enabled the exact dependency project reference.

## Verification

**Commands:**
- Execute every command row in the qualification contract's EventStore, Builds, remote-package, and reciprocal-rollback matrices -- expected: retained `PASS` rows only.
- `sha256sum -c artifact-manifest.sha256` in the new evidence bundle -- expected: every listed artifact matches.
- `git diff --check` and repository status captures before/after every owned checkout and lane -- expected: no whitespace defects, hidden drift, gitlink movement, or initialized nested submodule.

**Observed:**
- Builds candidate commitlint, static parity, catalog, and audit: `PASS`.
- EventStore coordinate, seven-API, source restore, and source build rows:
  `PASS`.
- `dotnet test tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1`:
  `FAIL` (exit `5`, zero tests).
- Attempt 4 Contracts test with `--max-parallel-test-modules 1`: `PASS`.
- Attempt 4 Client test with `--max-parallel-test-modules 1`: `FAIL`
  (exit `1`, `CS1704` duplicate `Hexalith.Commons.UniqueIds`).
- Attempt 5 Contracts test with `--no-build --no-restore` and
  `--max-parallel-test-modules 1`: `FAIL` (exit `2`; 2,022 passed, one
  governance test failed with internal process exit `126`).
- Attempt 6 clean source and package-source lanes: `PASS`, including all eight
  test rows. Remote consumer restore: `FAIL` (exit `1`, `NU1008` before
  package resolution).
- Attempt 7 source and package-source lanes: `PASS`. Effective-CPM proof:
  `FAIL` (exit `2`, unmatched shell quote); remote restore did not run.
- Attempt 8 source restore: `PASS`; source build: `FAIL` (exit `1`, `/tmp`
  full, 93 build errors). No later row ran.
- Attempt 9 complete EventStore source/package-source lanes, direct CPM proof,
  official-only remote restore, and all-14-package verification: `PASS`.
  Builds candidate restore/build/Module tests: `PASS`. Evidence tests: `FAIL`
  (exit `2`; 59 passed, 9 failed of 68; unexpected `HXE152` binding
  diagnostics). No later qualification row ran.
- Attempt 10 selected EventStore source/package-source, direct CPM proof,
  official-only remote restore, 14-package inventory, corrected candidate
  restore/build/Module/Evidence, and official G-4 rows: `PASS`. Rollback audit
  refresh: `PASS` (299 packages, 145 families, one source). Rollback commitlint:
  `FAIL` (exit `1`; both commit bodies exceeded the 200-character line limit).
  No later rollback or acceptance row ran.
- Attempt 11 phase 1–4 adoption, selected-candidate reciprocal controls,
  rollback tree/commitlint/static/restore/build/Module/Evidence/catalog rows:
  `PASS`. Rollback audit: `FAIL` (exit `1`; exact copied audit provenance
  `44bd5eb...` is not an ancestor of new head `1f46bc3...`). No later rollback
  or acceptance row ran.
- Attempt bundle manifest verification: every retained entry `OK`.
