---
title: '6.1-P1R Pragmatic Qualification Storage'
type: 'bugfix'
created: '2026-09-21'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-p1r.md'
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/SPEC.md'
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** P1R retries treated clean execution as a requirement to recreate and retain complete worktrees, NuGet caches, and browser assets under RAM-backed `/tmp`. Eleven append-only attempts exhausted the 48 GB tmpfs and blocked unrelated EventStore work without strengthening acceptance evidence.

**Approach:** Make cleanliness binding-based rather than attempt-based: reuse disk-backed exact-revision worktrees and shared dependency/browser caches, reserve an empty cache only for the official-feed proof, resume at the earliest invalidated row, retain evidence by hash without copying it, and clean disposable state on every exit.

</frozen-after-approval>

## Implementation Notes

- The reported 46 GB attempt root had already been removed when inspected;
  `/tmp` had returned to 13% usage. Attempt 11 later completed its stop-first
  failure and its unused 243 MB workspace was moved off tmpfs to recoverable
  quarantine at
  `/home/administrator/.cache/hexalith/p1r-quarantine/hexalith-p1r-3106-attempt11.LhzYSJ`.
- Updated the canonical P1R SPEC, qualification contract, active wrapper, and
  evidence README. Ordinary restores now use shared caches; only the official
  remote proof is cold. Retries reuse clean exact-revision worktrees and bound
  `PASS` rows, evidence is content-addressed, and cleanup is mandatory on every
  exit.
- Preserved the existing compact diagnostic bundles and historical attempt
  notes. They occupy about 5.6 MB on the disk-backed project filesystem and are
  not the discarded multi-gigabyte worktrees/caches that exhausted `/tmp`.
