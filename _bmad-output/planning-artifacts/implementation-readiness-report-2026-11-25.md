---
project: Hexalith.Projects
date: 2026-11-25
gate: FAIL
status: NOT_READY
baseline_commit: 294181274e2c02530d2cf417e8b94ffec69d1e2c
---

# Implementation readiness rerun — November 25

**Result: FAIL / NOT_READY.** No Epic 7 or Epic 8 story can be implemented as recorded. 7.1-P1 was accepted on 2026-11-25, but none of the blocking findings of the November 24 rerun is cleared. Sprint regeneration stayed gated on readiness. The tracker already matches `epics.md`, so no regeneration was needed. [Previous rerun](implementation-readiness-report-2026-11-24-rerun.md) · [Readiness-blocker correction](sprint-change-proposal-2026-11-24-readiness-blockers.md) · [Spine upstream sync](sprint-change-proposal-2026-11-25-spine-upstream-sync.md)

## Blocking findings

| Priority | Finding | Accountable input |
|---|---|---|
| High | **Epic 7 cannot start.** The Epic 7 entry gate requires G-1 and G-2. 7.1-P1 is `done` (pinned at `b738fc5b`), but 7.1-P2 is `open`, DW-63 is `open` and Story 7.1 is `blocked`. Every Epic 7 story, including 7.2, sits behind the gate. | EventStore, Platform and Folders owners deliver 7.1-P2. Projects and EventStore owners accept the DW-63 SDK-host integration. Use `bmad-correct-course` only if the owners change scope or sequencing. [Epic 7 entry gate](epics.md#epic-7-users-complete-durable-project-decisions-and-recover-them) · [Tracker](../implementation-artifacts/sprint-status.yaml) · [DW-63](../implementation-artifacts/deferred-work.md) |
| High | **Epic 8 cohesion decision pending.** Both owner rows in Section G read `Pending`. The 2026-11-25 manifest is current, so the owners can decide against it now. | Product Owner and authorized Solution Architect. A rejection triggers a structural `bmad-correct-course`. [Owner decision record](epics-architecture-conformance-checklist-2026-07-16.md#owner-decision-record) |
| High | **Chatbot host prerequisite open.** 8.8-P3-H is `blocked-external` and gates Story 7.16. | Chatbot Presentation Owner and Chatbot Test Owner. [8.8 packages](epics.md#story-88-prerequisite-evidence-packages) |
| High | **Story 8.1 acceptance blocked.** The internal preview still waits on 7.1-P2, Story 7.15, the authenticated persisted fixtures and blocking CI. | Projects, platform workflow, Builds and Platform owners with the Test Architect. [Story 8.1 specification](../implementation-artifacts/spec-8-1-inspect-task-audit-and-reconciliation-truth.md#implementation-notes) |

## Planning gaps

| Priority | Finding | Fix |
|---|---|---|
| Medium | **Most FR-26 consequences have no acceptance criterion.** Story 7.17 proves command equivalence with the existing `GrantProjectChatBotAuthority` and `RevokeProjectChatBotAuthority` implementation (`b7ae7273`). DW-112 already records that this implementation's safe denial does not conform. No acceptance criterion verifies the Tenant-owner-only grant, the expiry, or that a Folder revocation wins. [Story 7.17](epics.md#story-717-cut-over-project-writes-to-the-sdk-writer-in-one-fenced-deploy) · [FR coverage map](epics.md#fr-coverage-map) | `bmad-correct-course` |
| Medium | **Entry decisions recorded only in proposals or the ledger.** The spine sync listed these gaps without editing them. Stories 7.11, 7.12 and 7.16 do not name the architect-owned component package and FrontComposer hosting contract that gate their entry. Story 7.12 has no acceptance criterion that closes DW-96 or DW-87. Story 8.8 does not name the Chatbot-authority safe 404. [Listed gaps](sprint-change-proposal-2026-11-25-spine-upstream-sync.md) | `bmad-correct-course` |
| Medium | **Checklist conflicts with AD-17.** In Section C, the Story 7.15 entry says "single-writer command cutover completes". AD-17 and Story 7.17 assign the one writer cutover to 7.17. [Checklist](epics-architecture-conformance-checklist-2026-07-16.md) | `bmad-architecture` |
| Medium | **Evidence tooling is unverified.** `hexalith-evidence` is absent from the Projects tool manifest (DW-105), so governed matrix validation cannot run. Section G also records that `ci / build-and-test` failed on `fdc8ca69` while G-6 passed. [Section G checks](epics-architecture-conformance-checklist-2026-07-16.md#superseding-byte-manifest--reviewed-candidate) | Projects build-tooling owner (DW-105) |
| Low | **Stale counts and lists in `epics.md`.** The frontmatter still names FR-1..FR-25, AD-1..AD-34 and "35 stories: 8/16/11"; the plan now has 8/17/11. The Epic List gives Epic 6 seven stories and Epic 7 fifteen. The FR coverage map introduction names AD-1…AD-34 and the table has no FR-25 row. The Epic 7 introduction omits FR-26. | `bmad-correct-course` |

Edits to `epics.md` or the checklist change digests in the Section G manifest and require a new manifest. Applying the planning-gap fixes in one correction before the owners sign avoids a second signature.

## Checks

- **Production authority.** `python3 tools/planning/validate_production_authority.py --validate-index` passed with `[6, 7, 8]`.
- **Section G baseline.** All 18 SHA-256 digests in the 2026-11-25 manifest match the bytes on disk. The working tree was clean at `29418127`.
- **Traceability.** All 65 keys in the canonical YAML matrix appear in the Markdown view. [Canonical matrix](implementation-readiness-traceability-matrix.yaml) · [Markdown view](implementation-readiness-traceability-matrix.md)
- **Tracker.** `sprint_plan.py generate --dry-run` ran on a scratchpad copy. Its differences are known script limits: key truncation and retitled-story renames, `blocked` statuses demoted or dropped (7.1, 8.1, 8.8, 8.11), and prerequisite-package headings read as stories. Story 7.17 is already tracked under its approved key `7-17-cut-over-project-writes-to-the-sdk-writer`. The tracker stays hand-reconciled.
- **Next actionable work.** `sprint_plan.py status` recommends starting Story 7.2, but 7.2 is behind the Epic 7 entry gate. The actionable items are the Epic 6 `epic-6-retro-requalify-v4` qualification (Projects maintainer and Test Architect), 7.1-P2, DW-63 acceptance and the Section G owner decisions.
