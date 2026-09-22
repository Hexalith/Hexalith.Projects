---
title: '6.1-P1R Pragmatic Qualification Storage'
type: 'bugfix'
created: '2026-09-21'
status: 'done'
disposition: 'superseded'
superseded_by: 'spec-6-1-p1r-minimal-acceptance-gate.md'
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
  failure and its unused 243 MB workspace was moved off tmpfs and then to the
  user's recoverable trash after evidence capture.
- Updated the canonical P1R SPEC, qualification contract, active wrapper, and
  evidence README. Ordinary restores now use shared caches; only the official
  remote proof is cold. Retries reuse clean exact-revision worktrees and bound
  `PASS` rows, evidence is content-addressed, and cleanup is mandatory on every
  exit.
- Preserved the existing compact diagnostic bundles and historical attempt
  notes. They occupy about 5.6 MB on the disk-backed project filesystem and are
  not the discarded multi-gigabyte worktrees/caches that exhausted `/tmp`.

## Review Triage Log

- `medium` — the row schema lacked the binding digest required for reuse;
  `binding_sha256` and `reused_from` are now mandatory.
- `high` — binding complete contract bytes invalidated historical rows; bindings
  now use the applicable command-contract slice and legacy rows require a
  migration attestation or rerun.
- `medium` — shared cache contents were mutable and unbound; retained asset,
  NuGet-content, and Playwright tool/browser hashes now bind resolved inputs.
- `medium` — evidence guidance contradicted reuse with cleanup; shared resources
  now live outside the run root and cleanup applies to run-local state.
- `high` — moving only the worktree left tool temp paths on tmpfs; `TMPDIR`,
  `DOTNET_CLI_HOME`, and Playwright storage are now explicitly disk-backed.
- `high` — Git cleanliness missed stale ignored outputs; worktrees now clear or
  key generated output, and each `--no-build` test binds its build row.
- `medium` — cleanup was not evidenced; a final row must prove each disposable
  canonical path absent.
- `low` — the storage preflight lacked canonical mount and capacity criteria;
  it now records resolved paths and requires 20 GiB and 10% free space.
- `medium` — the artifact field was opaque; structured manifests now bind
  logical ID, SHA-256, byte size, and durable path.
- `false` — the missing full-spec scaffold is intentional for a `oneshot` spec;
  its context and implementation notes identify every changed policy file.
- `low` — recoverable quarantine contradicted cleanup; the unused attempt-11
  workspace was moved to the user's recoverable trash.
