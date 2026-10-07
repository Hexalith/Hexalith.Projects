---
title: "Sprint Change Proposal: Reconcile Epic 6 Tracking with Its Story Records"
date: 2026-10-06
status: approved
approved: 2026-10-06
approved_by: Jerome
workflow: bmad-correct-course
review_mode: batch
proposal_approval: approved
implementation_status: applied
change_scope: minor
trigger: "The 2026-10-06 sprint-planning readiness gate found epic-6 and Story 6.5 tracking values that contradict their own records and are pinned by the production-authority guard."
affected_epics: [6]
supersedes: none
---

# Sprint Change Proposal: Reconcile Epic 6 Tracking with Its Story Records

## 1. Issue Summary

Sprint planning on 2026-10-06 confirmed that `sprint-status.yaml` contains every
epic and story in `epics.md`, but two Epic 6 values contradict the records that
own them:

| Key | Tracker | Owning record | Origin of the drift |
| --- | --- | --- | --- |
| `6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface` | `backlog` | Spec `status: blocked` since 2026-09-07 (`dfc4386`); `blocked_by: ['6.1', '6.2', '6.3', '6.4']`; `prerequisite_record: null` | The tracker line was written on 2026-07-17 (`5e32ece`) and never changed. Open ledger entry DW-69 already records this mismatch. |
| `epic-6` | `backlog` | Stories 6.2 and 6.3 are `done` (2026-09-06 and 2026-09-20) | Set on 2026-07-17 (`0dc835f`) under the rule "in-progress → backlog when every created story is blocked". Only Story 6.1 existed then. That condition no longer holds. |

On 2026-09-22, `bac6342` froze both values in
`tools/planning/validate_production_authority.py::EPIC6_DEVELOPMENT_STATUSES`.
The guard requires the Epic 6–8 statuses to match its map exactly, so the
tracker cannot be corrected without a guard change. The project instruction
forbids bypassing or weakening the guard. This proposal changes the pins
together with the tracker.

Two related inconsistencies are in scope:

- **DW-1 reads `status: done 2026-09-06`.** Its own closure condition requires a
  committed, hash-pinned `prerequisite_record` covering accepted Stories
  6.1–6.4. The Story 6.5 spec still has `prerequisite_record: null`, and 6.1 and
  6.4 are blocked. When 6.5 was re-blocked on 2026-09-06 (`8bc91b2`), its spec
  recorded that "the `DW-1` done row is orchestrator bookkeeping and was not
  treated as proof." The frozen gate says "Do not re-arm or dispatch Story 6.5
  while DW-1 is open", so the false closure removes one of its two safeguards.
- **The tracker's `blocked` definition says "Story file exists".** Stories 7.1,
  8.8 and 8.11 have no story files. The 2026-08-01 correction (`52ea0e4`) moved
  them from `backlog` to `blocked` when it decomposed them into prerequisite
  work packages (7.1-P1/P2, 8.8-P1/P2/P3, 8.11-P1/P2/P3). Those statuses are
  correct; the definition is too narrow.

## 2. Impact Analysis

- **Epic impact:** Epic 6 scope, order and acceptance criteria are unchanged.
  Epics 7 and 8 keep every pinned status; only the shared `blocked` definition
  wording changes.
- **Story impact:** Story 6.5 moves to the state its own spec already records.
  No story text, frozen block, acceptance criterion or `blocked_by` list changes.
- **Authorization impact:** None. `blocked` is stricter than `backlog`.
  `epic-6: in-progress` reflects work already done; under the build workflow's
  sync rule, epic status does not gate dispatch. `readiness_provenance.current_result`
  stays `NOT_READY`. Every Epic 6–8 action-item status, the P1R acceptance JSON,
  the historical Epic 1–5 digest and DW-35/DW-68 are unchanged.
- **Artifact conflicts:** No PRD FR/NFR, Architecture Spine or UX change.
  `epics.md`, `epic-6-context.md` and the traceability matrix restate neither
  value. By convention, the PRD addendum evidence index gains row E-34.
- **Technical impact:** Two guard pins, four guard negative controls, the tracker,
  the deferred-work ledger and one addendum row. Changing the tracker changes its
  file hash. As already recorded in
  [the 2026-10-06 P1R proposal §7](sprint-change-proposal-2026-10-06.md#7-approval-and-execution-record),
  the attempt-21 packet validator rejects any newer live tracker by design. Its
  captured tracker stays recoverable from `311aa85c`, and nothing is resealed.

## 3. Recommended Approach

**Direct adjustment.** Move the tracker, guard and ledger together under the
project's candidate-then-active guard procedure. Effort: low. Risk: low. Every
change narrows or preserves authorization, and the new negative controls stop
the corrected values from drifting again.

Alternatives considered:

- **Leave the tracker as is and annotate the drift.** Rejected: queue consumers
  would keep two authoritative states for Story 6.5, which is the problem DW-69
  records.
- **Keep `epic-6: backlog` and widen the backlog definition.** Rejected: the
  tracker would then contradict the "first story created" rule that every other
  epic follows.
- **Rollback or MVP review.** Not applicable.

## 4. Detailed Change Proposals

### Change 1 — Tracker development statuses

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml` → `development_status`

```text
OLD:
  epic-6: backlog
  6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface: backlog

NEW:
  epic-6: in-progress
  6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface: blocked
```

**Rationale:** Applies the tracker's own transition rules to the recorded facts.
Every other development status keeps its bytes.

### Change 2 — Tracker status vocabulary

**Artifact:** `sprint-status.yaml` header comments (Story Status and Story Status Transitions)

```text
OLD:
#   - blocked: Story file exists but an accepted prerequisite or readiness gate is missing; implementation must not start
...
#   - ready-for-dev → blocked: A prerequisite or specification-readiness failure is confirmed

NEW:
#   - blocked: Story file or approved prerequisite work-package ledger exists but an accepted prerequisite or readiness gate is missing; implementation must not start
...
#   - backlog → blocked: An approved course correction records prerequisite work packages before a story file exists
#   - ready-for-dev → blocked: A prerequisite or specification-readiness failure is confirmed
```

**Rationale:** Describes the existing, approved 7.1/8.8/8.11 states without
changing any value. Comment-only, so the guard is unaffected.

### Change 3 — Guard pins

**Artifact:** `tools/planning/validate_production_authority.py::EPIC6_DEVELOPMENT_STATUSES`

```text
OLD:
    "epic-6": "backlog",
    "6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface": "backlog",

NEW:
    "epic-6": "in-progress",
    "6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface": "blocked",
```

**Rationale:** The exact-match boundary stays exact. Only the two corrected
values move, and no status family is widened.

### Change 4 — Guard negative controls

**Artifact:** `tests/tools/test_production_authority_guard.py::test_each_downstream_category_remains_closed`

```text
NEW cases, each expected to raise "open boundary":
  - Epic 6 completion:  "  epic-6: in-progress" → "  epic-6: done"
  - Epic 6 regression:  "  epic-6: in-progress" → "  epic-6: backlog"
  - Story 6.5 dispatch: "  6-5-…-read-surface: blocked" → "…: ready-for-dev"
  - Story 6.5 regression: "  6-5-…-read-surface: blocked" → "…: backlog"
```

**Rationale:** Proves the new pins fail closed in both directions. Existing
cases, which derive their fixtures from the live tracker, keep passing unchanged.

### Change 5 — Resolve DW-69

**Artifact:** `_bmad-output/implementation-artifacts/deferred-work.md` → `DW-69`

```text
OLD:
status: open

NEW:
status: done
resolution: 2026-10-06 Approved sprint-change-proposal-2026-10-06-epic-6-tracking-reconciliation.md set the sprint tracker and production-authority guard to `blocked` for Story 6.5, matching its specification; no dispatch, readiness or prerequisite state changed.
```

### Change 6 — Reopen DW-1

**Artifact:** `deferred-work.md` → `DW-1`

```text
OLD:
status: done 2026-09-06

NEW:
status: open
reopened: 2026-10-06 The 2026-09-06 closure was orchestrator bookkeeping, recorded as not proof by the Story 6.5 spec on 2026-09-06 (`8bc91b2`). `prerequisite_record` remains null and Stories 6.1 and 6.4 are blocked, so the closure condition is unmet. Approved by sprint-change-proposal-2026-10-06-epic-6-tracking-reconciliation.md.
```

**Rationale:** Restores the second safeguard on Story 6.5's frozen dispatch gate.
The guard reads only DW-35 and DW-68, so its behavior is unaffected.

### Change 7 — Index the decision

**Artifact:** PRD `addendum.md` §8 evidence index, after E-33

```text
NEW row:
| **E-34** | `_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-06-epic-6-tracking-reconciliation.md` | Approved 2026-10-06; applied | Jerome (approval); Product Owner / Projects maintainer | Minor tracking reconciliation: epic-6 `in-progress` after Stories 6.2/6.3 done; Story 6.5 `blocked` to match its spec; guard pins and negative controls updated; DW-69 resolved; DW-1 reopened; `blocked` definition covers prerequisite work-package ledgers. No FR/NFR, UX, architecture, story-scope, readiness, prerequisite-usability or release change | Story 6.5 prerequisite gate (DW-1); production-authority guard (rerun-4 §5.10); NOT_READY and every Epic 6–8 action status unchanged |
```

## 5. Implementation Handoff

**Classification:** Minor. The Developer applies the changes directly. There is
no backlog reorganization and no story, epic or requirement change.

| Recipient | Responsibility |
| --- | --- |
| Developer / Projects maintainer | Apply Changes 1–7 in the sequence below; touch no other tracker or ledger bytes. |
| Product Owner (Jerome) | Approve this proposal; no further decision is required. |

**Sequence:**

1. Preflight: `python3 tools/planning/validate_production_authority.py --validate-index` passes on the active tracker.
2. Apply Changes 3–4 to the guard and test.
3. Build a candidate tracker with Changes 1–2 outside the active path. Run
   `--validate-index --sprint-status <candidate>`; it must exit 0.
4. Atomically replace the active tracker with that exact candidate, then re-run
   the active-index check.
5. Apply Changes 5–7.
6. Run `python3 -m unittest tests/tools/test_production_authority_guard.py -v`
   and the other `tests/tools` suites that read the tracker or ledger
   (`test_p1r_candidate_evidence.py`, `test_g6_packet_references.py`,
   `test_git_whitespace_policy.py`).
7. Confirm `git diff` touches only the six named artifacts plus this proposal.

**Success criteria:** The active tracker passes the guard with `epic-6:
in-progress` and Story 6.5 `blocked`. The four new negative controls fail closed
and every existing guard test passes. DW-69 is `done`, DW-1 is `open`, and E-34
exists. `NOT_READY`, all Epic 6–8 action statuses, the historical digest, the P1R
JSON and every other tracker value are unchanged.

## 6. Checklist and Validation Record

| Item | Status | Note |
| --- | --- | --- |
| 1.1 Trigger | [x] | 2026-10-06 sprint-planning readiness gate; no story triggered it |
| 1.2 Problem | [x] | Stale tracking state frozen by guard pins |
| 1.3 Evidence | [x] | Commits `5e32ece`, `0dc835f`, `8bc91b2`, `dfc4386`, `52ea0e4`, `bac6342`; DW-69; Story 6.5 spec |
| 2.1–2.5 Epics | [x] | No scope, order or dependency change |
| 3.1 PRD | [x] | E-34 index row only |
| 3.2 Architecture | [N/A] | — |
| 3.3 UX | [N/A] | — |
| 3.4 Other | [x] | Guard, test, ledger; attempt-21 captured-hash behavior already recorded |
| 4.1 Direct adjustment | Viable | Low effort, low risk |
| 4.2 Rollback | Not viable | Nothing to revert |
| 4.3 MVP review | Not viable | MVP unaffected |
| 4.4 Selection | [x] | Direct adjustment |
| 6.3 Approval | [x] | Jerome replied **yes** on 2026-10-06 |
| 6.4 Tracker update | [x] | Changes 1–2 applied through the validated candidate |

## 7. Approval and Execution Record

Jerome explicitly replied **yes** on 2026-10-06 after batch review of this
complete proposal. Approval covers Changes 1–7 and the §5 sequence. It does not
change readiness, prerequisite usability, any action-item status or any story
scope.

Execution on 2026-10-06:

1. Preflight `--validate-index` on the active tracker: `PASS`, exit 0.
2. Changes 3–4 applied to the guard and its test.
3. Candidate tracker with Changes 1–2 built outside the active path:
   `--validate-index --sprint-status <candidate>` printed `PASS`, exit 0. The
   unchanged active tracker under the new guard printed `BLOCKED: Epic 6-8
   development statuses exceed the open boundary`, exit 1, confirming the pins
   moved.
4. The candidate atomically replaced the active tracker (byte-identical; SHA-256
   `81e441f16066acb70f54e855fb44c63d563fbe8e7096194921331ec4c4b9b9dc`).
   The active re-check printed `PASS`, exit 0.
5. Changes 5–7 applied: DW-1 `open`, DW-69 `done`, addendum row E-34.
6. `test_production_authority_guard.py` 22/22 passed, including the four new
   negative controls. `test_p1r_candidate_evidence.py` 16/16 and
   `test_git_whitespace_policy.py` 5/5 passed. `git diff --check` was clean.
7. The diff touches only the five named artifacts and this proposal.

**Pre-existing, out-of-scope failure:** `test_g6_packet_references.py` reports 20
failures. The test requires the tracker, P1R owner packet and Architecture Spine
to quote the committed Builds gitlink `ba4ca78c3868a4757cb92d912a54c8a237871b54`
and EventStore gitlink `283b07a52c9c70e1c940164a7011ee8c3ad98b2d`. Neither
appears in the tracker at `HEAD`, and this change's tracker diff contains no
revision hash, so the failure predates this proposal. It is G-6 quoted-gitlink
drift from recent submodule updates. It stays with the G-6 owners and is not
silently resolved here.
