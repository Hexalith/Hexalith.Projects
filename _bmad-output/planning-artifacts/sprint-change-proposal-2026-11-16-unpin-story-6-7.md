---
title: 'Unpin Story 6.7 and move it to the individual Debug test-project lane'
date: '2026-11-16'
status: 'approved'
approved_date: '2026-11-16'
approval_source: 'Jerome selected "Approve (Recommended)" on 2026-11-16 for E1-E7 as drafted, in batch mode'
change_scope: 'minor: guard and guard-test change plus planning-text edits; no product code, no status move'
mode: 'batch'
trigger: '/bmad-build 6.7 stopped at step 1 on 2026-11-16. The guard pins 6.7 at backlog, and 6.7 verifies with a common runner that has not been delivered.'
precedents:
  - 'sprint-change-proposal-2026-11-15-schedule-stories-6-8-and-6-6.md (E1 swap-loop release of 6.8; E3 lane extension to 6.6)'
  - 'sprint-change-proposal-2026-11-01.md (E1 individual Debug test-project lane)'
---

# Proposal: unpin Story 6.7 and verify it on the E1 lane

## 1. Issue summary

On 2026-11-16, `/bmad-build 6.7` stopped at step 1, before any spec existed. Two planning facts block the story. Every other Epic 6 story (6.1–6.6 and 6.8) is `done`.

1. **The guard forbids any progress.**
   - `tools/planning/validate_production_authority.py:85` keeps `6-7-…: "backlog"` in `EPIC6_DEVELOPMENT_STATUSES`.
   - `tests/tools/test_production_authority_guard.py:676-691` (`test_unauthorized_closure_fails_closed`) uses forward 6-7 statuses as its example of a forbidden transition.
   - The 2026-11-15 proposal and `epics.md:64` both say "6.7 stays in backlog". Neither names a condition that would release it.
   - The build would set 6.7 to `in-progress` when implementation starts, and the guard would then fail.
2. **The verification runner does not exist.**
   - The verification line (`epics.md:1565`) is `dotnet tool run hexalith-module test --profile read-cutover --filter Story=6.7` + `hexalith-evidence validate` row `nfr-10-reads`.
   - The common runner is still open under 6.1-P0, stages 2–7, in Epic 8.
   - Projects has no `.config/dotnet-tools.json`, so neither tool can run here.
   - No matrix row is named `nfr-10-reads`; the row is `nfr-10`.
   - Five matrix rows name the same runner for 6.7.

**Category:** process gap. The guard and the lane were not revisited once 6.7's prior stories were done. The PRD, the architecture and 6.7's acceptance criteria need no change.

### Evidence (2026-11-16, at `7e2823bb`, clean tree)

| Fact | Source |
| --- | --- |
| 6.7 is pinned at `backlog`; 6.6 and 6.8 are released through the `SCHEDULED_STORY_STATUSES` swap loop | `validate_production_authority.py:32-36`, `:81-86`, `:288-292` |
| Fixture resets cover 6-1, 6-5, 6-6 and 6-8 but not 6-7 | `test_production_authority_guard.py:92-97` |
| Forbidden-transition example uses 6-7 | `test_production_authority_guard.py:676-691` |
| Baseline: `--validate-index` PASS, `--story-id 6.7` PASS, 49 guard tests OK | local run, 2026-11-16 |
| 6.7 names the common runner and a dangling row `nfr-10-reads` | `epics.md:1555`, `:1565` |
| The runner command appears in matrix rows `nfr-10`, `finding-arch-001`, `finding-arch-002`, `finding-id-001` and `finding-api-001` | `implementation-readiness-traceability-matrix.yaml:592`, `:629`, `:645`, `:724`, `:738`; `.md:141`, `:143`, `:144`, `:149`, `:150` |
| The common runner, its publication and package parity still gate retirement of old runtime projects; the lane exception authorizes no runtime retirement | `ARCHITECTURE-SPINE.md:306` (AD-25 applicability, 2026-11-02); `epics.md` 6.1-P0 row |

## 2. Impact analysis

- **Epic 6.** 6.7 becomes schedulable. Its own acceptance criteria stay unchanged: exhaustive equivalence, reversible routing, legacy kept until Epic 8 release acceptance, and no rewrite of history. Every other Epic 6 story stays `done`, and the epic stays `in-progress`.
- **Epic 7.** Not touched. 7.1 stays `blocked`, and its dependency on an accepted 6.7 with rollback still applies.
- **Epic 8.** Not touched. 6.1-P0 (the common runner) and the AD-25 gate on runtime retirement remain Epic 8 obligations.
- **PRD, architecture, UX.** No change.
- **Tracking and tooling.**
  - The guard and its tests change together.
  - Five matrix rows change, in both the YAML and the Markdown view.
  - Two `epics.md` lines change, and one correction note is added.
  - `sprint-status.yaml` gets one line comment and no status move.
- **Code.** None in this correction.

## 3. Recommended approach

**Direct adjustment.** Release 6.7 the way 6.8 was released: through the existing swap loop, with readiness still `NOT_READY`. Then extend the E1 lane to 6.7, as was done for 6.6.

- **Effort:** low. The edits touch one guard constant, one loop, four test edits and five text locations.
- **Risk:** low for this correction. The real risk sits in 6.7 itself, which is sized L: equivalence must cover every applicable Epic 6 read, and normalization needs owner approval. The 6.7 spec deals with that.
- **Considered and dropped:**
  - *A dependency check like 6.6's,* which would let 6.7 leave backlog only when 6.1–6.6 and 6.8 are done. All of them are done, and the done rule never reopens a story, so the check would change nothing.
  - *Waiting for the common runner.* That ties 6.7 to 6.1-P0, which E1 already rejected for 6.1, 6.5 and 6.6.

## 4. Detailed change proposals

### E1. Guard: `tools/planning/validate_production_authority.py` (LF)

Add after the 6.6/6.8 block (after `:36`). The existing comment at `:34-35` is left as is, because it describes the 2026-11-15 exception.

```python
STORY_6_7_KEY = "6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback"
# Approved 2026-11-16 unpin: 6.7 may progress like 6.8. Grants no readiness, P1R, Epic 7/8
# or release acceptance.
```

Remove `"6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback": "backlog",` from `EPIC6_DEVELOPMENT_STATUSES` (`:85`).

Extend the swap loop (`:288`):

```python
    for key, story in ((STORY_6_8_KEY, "6.8"), (STORY_6_6_KEY, "6.6"), (STORY_6_7_KEY, "6.7")):
```

**Effect:**

- 6.7 may take any value in `SCHEDULED_STORY_STATUSES`.
- A missing, renamed or out-of-set 6.7 value fails closed with "scheduled status".
- The `NOT_READY` check, every Epic 7 and Epic 8 pin, and the 6.6 dependency check are unchanged.

### E2. Guard tests: `tests/tools/test_production_authority_guard.py` (CRLF; same commit as E1)

- **Fixture independence.** Add `GUARD.STORY_6_7_KEY: "backlog",` to the `baselines` map (`:92-97`). Fixtures then stay valid when the live file records 6.7 progress or carries the E6 comment.
- **Move the forbidden example.** In `test_unauthorized_closure_fails_closed` (`:676-691`), replace the 6-7 line with `  7-2-update-project-setup-idempotently`, which `TRANSITIVE_EPIC_STATUSES` still pins at `backlog`. Keep the same loop and the same "open boundary" error.
- **Extend the fail-closed loop.** In `test_scheduled_story_keys_and_statuses_fail_closed` (`:970-984`), loop over `(GUARD.STORY_6_6_KEY, GUARD.STORY_6_8_KEY, GUARD.STORY_6_7_KEY)`.
- **New case: `test_read_cutover_story_can_progress_without_opening_other_gates`.** On both the open and the accepted snapshot, every value in `SCHEDULED_STORY_STATUSES` passes for 6-7. Readiness stays `NOT_READY`, and `7-1-…` stays `blocked`.
- **Existing assertions** stay unchanged. In `test_contracts_split_story_can_progress_without_opening_other_gates`, the check that 6-7 is still `backlog` keeps holding through the fixture reset.

### E3. Story 6.7 text: `epics.md` (CRLF)

**Verification (`:1565`).**

OLD:

> `- **Verification:** \`dotnet tool run hexalith-module test --profile read-cutover --filter Story=6.7\` + \`hexalith-evidence validate\` row \`nfr-10-reads\`. **Evidence:** …`

NEW (Evidence, Estimate and Completion boundary unchanged):

> `- **Verification (E1 lane, extended 2026-11-16):** the individual Debug Projects test projects named in the 6.7 spec, each built with \`--configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers\` and run as a built xUnit v3 assembly with native XML. No common runner; AD-25 still gates runtime retirement. **Evidence:** …`

**Why the AD-25 clause:** the completion boundary says legacy read plumbing is "retired only after gates pass". The clause makes clear that lane evidence is not that gate (`ARCHITECTURE-SPINE.md:306`). It does not change scope, because AC1 already keeps legacy until Epic 8 release acceptance.

**Traceability (`:1555`).** `evidence row \`nfr-10-reads\`` becomes `evidence row \`nfr-10\``. That is the existing row; no row named `nfr-10-reads` exists.

### E4. Epic 6 shared attributes: `epics.md:1375`, `:1377`

- "under the approved 6.1/6.5/6.6 exceptions" becomes "under the approved 6.1/6.5/6.6/6.7 exceptions".
- "for 6.1/6.5/6.6 (6.6 added 2026-11-15)" becomes "for 6.1/6.5/6.6/6.7 (6.6 added 2026-11-15, 6.7 added 2026-11-16)".

### E5. Traceability matrix: five `verification_command` values

Change these in the YAML (LF) and in the Markdown command table (CRLF). The evidence artifacts, statuses, blockers and entry gates stay unchanged. `finding-arch-001` keeps its "G-4 composition runner" blocker because AD-25 retirement still needs it.

| Row | YAML / MD line | NEW `verification_command` |
| --- | --- | --- |
| `nfr-10` | `:592` / `:141` | `6.7 E1 lane (extended 2026-11-16): individual Debug Projects test projects named in the 6.7 spec, run as built xUnit v3 assemblies with native XML; no common runner.` |
| `finding-arch-001` | `:629` / `:143` | same as `nfr-10` |
| `finding-arch-002` | `:645` / `:144` | same, followed by ` Contract-boundary cases.` |
| `finding-id-001` | `:724` / `:149` | same, followed by ` ULID identity cases.` |
| `finding-api-001` | `:738` / `:150` | same, followed by ` OpenAPI/runtime reconciliation cases.` |

### E6. `epics.md`: correction note (insert after `:64`)

> **Approved 6.7 scheduling correction — 2026-11-16.** Jerome approved [sprint-change-proposal-2026-11-16-unpin-story-6-7.md](sprint-change-proposal-2026-11-16-unpin-story-6-7.md). Story 6.7 may be scheduled now and runs on the extended E1 individual Debug test-project lane; AD-25 still gates runtime retirement. Historical NOT_READY and every release obligation remain.

The 2026-11-15 note at `:64` is not edited. The new note supersedes its "6.7 stays in backlog" sentence.

### E7. `sprint-status.yaml:289` (LF; hand edit, no status move)

```yaml
  6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback: backlog # schedulable under the 2026-11-16 exception (sprint-change-proposal-2026-11-16-unpin-story-6-7.md)
```

- `last_updated` is already 2026-11-16.
- No new key is added.
- Never run `sprint_plan.py`.
- `/bmad-build 6.7` moves the story to `in-progress` when implementation starts.

### Not changed

- **`epic-6-context.md`.** Recompiled earlier this session and still uncommitted, it says 6.7 is pinned. After E3, E4 and E6, `epics.md` is newer, so `/bmad-build 6.7` recompiles it automatically.
- **Historical records.** The 2026-11-15 proposal, the 2026-11-01 proposal, and the `:64` note are left as they are.
- **Spine, PRD and UX.** No edits.
- **Observed, outside the brief:**
  - `finding-arch-002` (the UI.Contracts boundary) still names 6.7 as its primary story and is `pending`, although Story 6.8 delivered the split.
  - `nfr-10` lists `6.1-P1R` among its entry gates.

  Say if either should become a deferred-work entry; neither blocks 6.7.

## 5. Checklist record

| Item | Status | Note |
| --- | --- | --- |
| 1.1–1.3 Trigger, problem, evidence | [x] | §1 |
| 2.1 Current epic | [x] | Completable: 6.7 is the last open story |
| 2.2 Epic-level changes | [x] | Story verification text only |
| 2.3–2.4 Future epics | [x] | Epic 7 and 8 pins unchanged; nothing obsolete |
| 2.5 Order | [N/A] | 6.7 is the only remaining Epic 6 story |
| 3.1 PRD | [N/A] | No requirement change |
| 3.2 Architecture | [x] | AD-25 retirement gate restated, not changed |
| 3.3 UX | [N/A] | None |
| 3.4 Other artifacts | [!] | Guard, tests, matrix (E1, E2, E5) |
| 4.1 Direct adjustment | Viable | Low effort, low risk |
| 4.2 Rollback | Not viable | No done work conflicts |
| 4.3 MVP review | Not viable | MVP unaffected |
| 4.4 Selected path | [x] | Direct adjustment |
| 5.1–5.5 Proposal components | [x] | §1–§6 |
| 6.1–6.2 Review | [x] | Cited lines re-read on 2026-11-16 |
| 6.3 Approval | [x] | Jerome approved E1–E7 as drafted on 2026-11-16 (§7) |
| 6.4 sprint-status | [x] | E7 applied by hand; no status move |
| 6.5 Handoff | [x] | §6 |

## 6. Implementation handoff

- **Scope:** minor. The developer agent applies E1–E7 after approval. Jerome's approval is the Product Owner and Solution Architect sign-off.
- **Order:**
  1. E1 and E2 together.
  2. E7.
  3. E3–E6.
- **Validation:**
  - `python3 tools/planning/validate_production_authority.py --validate-index` and `--story-id 6.7`;
  - `python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py`, expecting 50 tests;
  - the guard on a scratchpad copy of `sprint-status.yaml` with 6-7 set to `in-progress`, which must pass;
  - a YAML load of the edited matrix and sprint status;
  - `git diff --stat`, to confirm each file keeps its line endings.
- **Success criteria:**
  - The guard accepts every scheduled 6.7 status.
  - The guard still rejects every Epic 7 and Epic 8 move and any readiness other than `NOT_READY`.
  - All guard tests pass.
  - No product source, dependency, submodule or Git change.
- **Next step:** `/bmad-build 6.7`.
- **Not authorized:** commits, pushes, `sprint_plan.py`, and any change outside E1–E7.

## 7. Approval and application record

Jerome approved E1–E7 as drafted on 2026-11-16. The edits were applied in the approved order on top of `9452fcc1`. They are uncommitted, and no commit was requested.

**Validation results:**

- `--validate-index`: PASS. `--story-id 6.7`: PASS.
- Scratchpad copies of `sprint-status.yaml`:
  - 6-7 set to `in-progress`, `review` or `done`: PASS each time.
  - 7-2 set to `in-progress`: `BLOCKED: Epic 6-8 development statuses exceed the open boundary`, exit 1, as intended.
- Guard tests: 50 run, OK.
- YAML loads of the matrix and sprint status: OK.
- `git diff --check`: clean.
- Line endings: CRLF kept in `epics.md`, the Markdown matrix and the guard tests; LF kept in the guard, the YAML matrix and sprint status.
