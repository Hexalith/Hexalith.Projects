---
title: 'Close Story 6.5 on its own acceptance and move live multiple-Folder advisory latency to its owners'
date: '2026-11-15'
status: 'approved'
approved_date: '2026-11-15'
approval_source: 'User selected "Yes, apply E1–E5"'
change_scope: 'minor (tracking and definition-of-done only; no product code)'
trigger: 'User decision 2026-11-15 during /bmad-build 6.5: "Close on own ACs" after the live multiple-Folder advisory readiness failed again (0/27).'
mode: 'batch'
precedent: 'sprint-change-proposal-2026-11-01-close-story-6-4.md (Epic 6 story completion rule)'
---

# Proposal: close Story 6.5 and stop the resumption loop

## 1. Issue summary

The Story 6.5 read surface was implemented and accepted on its own criteria on 2026-11-06. It stayed open for nine more days because of one live check added during review.

- **Feature delivered 2026-11-06.** The opt-in `/inspect/projects` inventory/detail/advisory surface uses typed SDK queries and the FrontComposer authentication, current-Tenant and per-circuit credentials. The legacy routes are kept for 6.7.
- **Review added a live positive Folder journey.** Review finding B10/V2 became patch P7: "add an existing seeded supported Folder positive journey". P7's native mapping and rendering cases pass. Its live part, a positive multiple-Folder advisory result in the managed topology, has never passed.
- **The live check fails on platform latency, not on 6.5 behavior.** The supported Folder advisory makes four sequential delegated Folder authorization waves (initial inputs, discovery, final inputs, final candidates). Each takes about 0.5 s, and all four share the 2 s interactive timeout (NFR-7, `ProjectAdvisoryQueryExecutor` `CancelAfter(2 s)`). Single-Folder is borderline. Multiple-Folder returns HTTP 200 with a null result, incomplete coverage and zero candidates in every attempt since 2026-11-07, most recently 0/27 at `6bed1ee4`. The 6.5 UI renders that incomplete result correctly, as the frozen matrix requires.
- **The loop.** The spec gained more than 40 dated diagnostic and EventStore performance sections, and about 50 `story-6-5-*` evidence packets. The 2026-11-07 scope extension approved several EventStore performance repairs. Each one qualified and had measured component benefit, yet the live multiple-Folder result stayed at zero.

**Category:** failed approach (process). The product requirements need no change.

## 2. Evidence for closing

The Story 6.5 acceptance criteria in `epics.md` and their evidence:

| Acceptance criterion (epics.md §6.5) | Evidence |
| --- | --- |
| Authenticated, Tenant-scoped, authorization-filtered, Safe Metadata by default, AD-32 fields, read-only; descriptive inspection authorized and audited | Native UI 211, Server 324, Contracts 46 and Integration 45 pass at `eaa95558`, plus 460 EventStore owner cases: 1,086 unique, zero failures/errors/skips ([current-source packet](../implementation-artifacts/evidence/story-6-5-current-verification-20261115/README.md)). A real OIDC managed run at `b79d46e1` passes native seed 1/1 and browser 1/1 with unchanged persisted domain fingerprints, separate inspection-audit growth and continuous telemetry checks ([implementation packet](../implementation-artifacts/evidence/story-6-5-implementation-20261106/README.md), [assistive packet](../implementation-artifacts/evidence/story-6-5-native-assistive-20261106/README.md)). |
| Audit tab renders `not yet available` | Covered by the same native UI lane; maintenance controls and unsupported Refresh calls are removed. |
| Denied or cross-Tenant target fails closed, indistinguishable from absence | Frozen matrix row 4. All five frozen matrix rows bind to 12 executed passing UI cases at `eaa95558` (`native-and-matrix-audit.json`). |
| Keyboard, screen reader, 200% zoom and 320px: axe plus authenticated manual evidence, no unresolved critical/serious findings | Real Orca/AT-SPI and native Chrome 100%/200% runs after real OIDC at `b79d46e1`: six axe scans with zero violations and no unresolved contrast targets. After the review patches, seven axe scans had zero violations and all ten spoken-state cases passed ([review verification](../implementation-artifacts/evidence/story-6-5-review-verification-20261106/README.md)). These are agent-driven assistive-technology runs, not human audio evaluation. |

**CI.** `ci / build-and-test`, the generated-artifact gates and the P1R replay pass on `eaa95558` (run [43654669161](https://github.com/Hexalith/Hexalith.Projects/actions/runs/43654669161)). That run's G-6 job fails at restore with `NU1202`: `Hexalith.Commons.UniqueIds` 3.0.0 supports only net11.0, while `references/Hexalith.McpCli` still targets net10.0 at gitlink `a92a2969`. This comes from the .NET 11 move, not from 6.5; it is DW-81 below.

**Not claimed.** The following are not claimed:

- A positive live multiple-Folder advisory result.
- A browser journey on the post-`7bcb375f` .NET 11 sources.
- Release or package latency.
- Human audio evaluation or exhaustive WCAG certification.
- Production readiness.
- 6.7 route equivalence.

## 3. Impact analysis

- **Epic 6:** 6.5 moves from `blocked` to `done`. The epic stays `in-progress` with 6.6–6.8 unchanged; 6.6 is still gated by 6.8.
- **Other epics:** Story 8.9 already owns NFR-5 (metadata reads p95 < 500 ms) and NFR-7 (2 s interactive timeout). DW-80 names it as the consumer. Epics 7 and 8 need no other change.
- **PRD / Architecture / UX:** no change. The incomplete-coverage display (UX), the AD-32 response fields and the advisory contract (AD-7/14/32) already describe what 6.5 renders.
- **Guard:** `tools/planning/validate_production_authority.py` already allows `done` for 6.5 when 6.1–6.4 and DW-1 are done, and they are. A scratch candidate with 6.5 `done` passes both `--story-id 6.5` and `--validate-index`. The guard does not change. Its unit test needed one fixture fix, recorded as E6.
- **Tests and tooling:** the positive single- and multiple-Folder assertions in `tests/e2e` and in the managed launcher stay unchanged and become the DW-80 acceptance probe. The uncommitted launcher repair (`supported_browser_reads.py` retries the OIDC container on a new port only after Docker's exact refused-port response; 164/164 launcher controls) is kept as tooling.
- **Code / dependencies / submodules:** none.

## 4. Recommended approach

**Direct adjustment.** Close 6.5 on its own acceptance criteria and anchor it at the recorded commits above. Move the live multiple-Folder latency and the unrelated G-6 restore failure to owned deferred-work entries.

- **Effort:** low. Tracking and document edits only.
- **Risk:** low. Nothing is claimed beyond the delivered read surface. The latency stays visible as an owned NFR-7 item with the unchanged live probe as its acceptance test.
- **Rejected alternatives:** each of these was offered on 2026-11-15 and not selected:
  - *Stop gating on multi-Folder:* would make the launcher stop requiring the positive multi-Folder result.
  - *Longer Debug-lane deadline:* changes NFR-7 in one lane.
  - *Merge the repeated Folder check:* changes the approved authority-observation order.
  - *Keep diagnosing:* the 2026-11-07 to 2026-11-15 loop did not converge.

## 5. Detailed change proposals

### E1. Spec: `implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md`

Frontmatter:

```
OLD: updated: '2026-11-15'
     status: in-progress
     blocked_by:
       - "Multiple-Folder readiness still returns null/incomplete with the current canonical memo; authenticated browser acceptance remains blocked."
NEW: updated: '2026-11-15'
     status: done
     completed: '2026-11-15'
     accepted_at: 'Projects eaa95558dfcd1aed9816a9506f650cfa512bf45f (native 1,086; CI 43654669161 build-and-test); browser/a11y b79d46e1930d60cd5dcf999c19b9202a563f1f2f'
     closure_approval: 'sprint-change-proposal-2026-11-15-close-story-6-5.md'
     blocked_by: []
```

Tasks & Acceptance: replace the one open task with two lines that state what passed and what moved.

```
OLD: - [ ] Rebuild ordinary source binaries without private diagnostic overrides; execute owner regression controls plus current Projects native/matrix coverage, then pass the real positive Folder readiness and authenticated managed browser journey with persisted-state/privacy/preservation/cleanup evidence.
NEW: - [x] Rebuild ordinary source binaries without private diagnostic overrides; execute owner regression controls plus current Projects native/matrix coverage (1,086 unique passes and five frozen rows bound to 12 executed UI cases at `eaa95558`).
     - [moved → DW-80] Pass real positive multiple-Folder readiness and the authenticated managed browser journey that depends on it. Not claimed; closed out of this story by sprint-change-proposal-2026-11-15-close-story-6-5.md.
```

Append a short `## Closure — 2026-11-15` section that points to this proposal. It states that the frozen intent and the earlier sections stay unchanged, that there will be no further resumption sections, and that new findings become deferred-work entries or new stories. Add one line to the Review Triage Log: *"P7 live positive Folder journey moved to DW-80 by the 2026-11-15 closure; native P7 cases remain passing."* The frozen block, history and evidence stay untouched.

### E2. `implementation-artifacts/sprint-status.yaml`

```
OLD:   6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface: blocked
NEW:   6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface: done # accepted 2026-11-15 on own ACs at eaa95558 (native 1,086; CI 43654669161 build-and-test) + browser/a11y at b79d46e1; live multiple-Folder latency moved to DW-80 (sprint-change-proposal-2026-11-15-close-story-6-5.md)
```

Update `last_updated` in the header comment and the key from 2026-11-05 to 2026-11-15. Edit by hand (never `sprint_plan.py`), then run the production-authority guard.

### E3. Planning text

- `planning-artifacts/epics.md` §Story 6.5: append after the Verification bullet: *"**Closure (2026-11-15):** done on its own acceptance criteria by sprint-change-proposal-2026-11-15-close-story-6-5.md; live multiple-Folder advisory latency is DW-80 (consumer Story 8.9)."* The Prerequisites paragraph stays as history.
- `implementation-artifacts/epic-6-context.md` Cross-Story Dependencies: in the bullet starting "Stories 6.2–6.4 are recorded done", change "Story 6.5 requires accepted 6.1–6.4 contracts and handlers, usable FrontComposer server authentication/current-Tenant/per-circuit credentials, and DW-1 completion; it remains blocked." to "Story 6.5 is done on its own acceptance criteria (2026-11-15); live multiple-Folder advisory latency is DW-80."

### E4. `implementation-artifacts/deferred-work.md`: new entries, neither gating Story 6.5

| ID | Item | Owner | Gate |
| --- | --- | --- | --- |
| DW-80 | Bring the live multiple-Folder advisory read inside the 2 s interactive timeout (NFR-7) in the managed topology. Four sequential delegated Folder authorization waves of about 0.5 s share one 2 s deadline; MULTIPLE has been 0 positive since 2026-11-07 (latest 0/27 at `6bed1ee4`). During readiness the gateway uses about 1.17 CPU-s per wall second and reads about 9.2 GB from page cache per 10 s, which is consistent with fresh Development route-catalog image reads (an inference). Release behavior is unmeasured. Evidence: `evidence/story-6-5-acceptance-20261115/`. Do not relax the authority-observation order, privacy floor or deadline without a separate decision. Removal: the unchanged `E2E_SUPPORTED_READS_ONLY=1 … npm --prefix tests/e2e run test:live:managed` passes positive SINGLE and MULTIPLE readiness, the native seed and the authenticated browser case at a recorded commit. | Hexalith.EventStore (gateway route-catalog observation cost) with Hexalith.Folders (owner authorization fan-out) | Story 8.9 (NFR-5/NFR-7); not Story 6.5 |
| DW-81 | Restore G-6 current runtime qualification after the .NET 11 move: restore fails `NU1202` because `Hexalith.Commons.UniqueIds` 3.0.0 is net11.0-only and `references/Hexalith.McpCli` (gitlink `a92a2969`) targets net10.0 (run 43654669161 at `eaa95558`; `ci / build-and-test` passes). | Hexalith.McpCli with Projects build hygiene | CI on main; not a story gate |

### E5. Story 6.5 evidence index

`evidence/story-6-5-acceptance-20261115/README.md`: append one line: *"Closed out of Story 6.5 by sprint-change-proposal-2026-11-15-close-story-6-5.md; continues under DW-80."* Earlier packets stay immutable.

### E6. Guard test fixture (added during application, 2026-11-15)

`tests/tools/test_production_authority_guard.py` builds its fixtures from the live `sprint-status.yaml` and already reset Story 6.1 to `blocked` "to keep historical open/accepted fixtures independent of active feature progress". With 6.5 `done` in the live file, every fixture failed with `Story 6.5 / DW-1 requires done Story 6.1` (175 failures, 19 errors). The fixture now resets both the 6.1 and 6.5 status lines to bare `blocked` lines, dropping any trailing closure comment. The guard logic and every assertion are unchanged. Result: 46/46 pass against the new sprint status, and 46/46 still pass against the pre-change file.

## 6. Implementation handoff

- **Scope:** minor. The developer agent implements E1–E6 directly: documents plus one test fixture.
- **Validation:**
  - `python3 tools/planning/validate_production_authority.py --story-id 6.5` and `--validate-index`
  - `python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py`
  - `git diff --check`
- **Success criteria:**
  - `sprint-status.yaml` shows 6.5 `done` and the guard passes.
  - The 6.5 spec is `done`, with its frozen block byte-unchanged.
  - DW-80 and DW-81 exist with owners and removal conditions.
  - There is no product source, dependency, submodule or Git change; the only test change is the E6 fixture.
- **Not authorized:** commits, pushes, deleting or pruning existing evidence, dependency or submodule updates, and any change to the advisory deadline, authority observations or the live positive assertions.
