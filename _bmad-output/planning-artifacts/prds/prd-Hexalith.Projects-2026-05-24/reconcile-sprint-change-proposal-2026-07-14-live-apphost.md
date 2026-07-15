# Final Reconciliation: Live AppHost Route and Epic 5 Playwright Closure

## Input

- Source: `sprint-change-proposal-2026-07-14-live-apphost.md` (approved and implemented 2026-07-14).
- Compared with the current `prd.md`, `addendum.md`, and `.memlog.md`.
- Verdict: **Fully reconciled. No current PRD-package gaps remain. Verification enablement is complete; release acceptance remains blocked by the recorded live failures.**

## Extracted Product Requirements and Decisions

The proposal does not change product behavior, MVP scope, roles, journeys, or Functional Requirement ownership. Its product-level content is the obligation to prove the already-approved contract:

- FR-21 audit behavior and FR-22 authorized, Tenant-scoped, metadata-only operator access remain release-blocking.
- NFR-9 continues to require authenticated automated and manual accessibility evidence for operator journeys, including keyboard, responsive, focus, announcement, and safe-denial behavior.
- NFR-11 requires critical release evidence to pass; failed critical cases, unexplained skips, or unavailable environments cannot be represented as acceptance.
- Evidence must remain metadata-only and must not expose tokens, credentials, command/setup payloads, private paths, or protected content.
- Safe-denial results are useful negative-path evidence but do not prove the authorized positive FR-22 path.

These obligations are retained in `prd.md` through FR-21, FR-22, NFR-1, NFR-9, NFR-11, SM-3, SM-5, and SM-6. The addendum maps the failed live evidence to FR-21, FR-22, NFR-9, and NFR-11 and preserves the distinction between a runnable verification mechanism and release acceptance.

## Extracted Implementation and Verification Detail

The following source content is implementation/how and correctly remains outside the capability-oriented PRD:

- Story 5.12 ownership and preservation of Story 5.11's historical no-AppHost evidence.
- Aspire-managed startup, readiness, dynamic `projects-ui` endpoint discovery, and teardown without a guessed port.
- Root-declared sibling checkout use without nested-submodule initialization, owned AppHost/hosting toolchain alignment, and the separate Conversations sibling-root correction.
- Explicit live Playwright opt-in, fail-fast URL validation, an independently runnable no-AppHost lane, conditional replacement of permanent `test.fixme` declarations, and concrete reasons for every retained skip.
- The recorded focused result of 13 passed/13 failed and full Chromium result of 19 passed/56 failed across 75 cases, including missing deterministic `tenant-a` access-projection seeding and warning-console UI/static-asset prerequisites.
- Remediation ownership through Story 8.6, deterministic authorized fixtures and required assets, and rerun evidence before release acceptance.

This material is now captured in addendum sections **Current Readiness, Release Containment, and Supersession**, **7.2 Cross-cutting test and release evidence**, and evidence entry **E-6**. Exact commands, environment variables, version pins, fixture recipes, and raw results remain properly routed to architecture, runbooks, test strategy, and implementation artifacts.

## Gap Summary

**None remain.** The current addendum closes all four gaps identified by the earlier version of this reconciliation report:

- It preserves the live topology and repository/toolchain invariants.
- It preserves the dual-lane Playwright execution contract and skip discipline.
- It records the decisive result totals and both known blocker classes.
- It records requirement traceability, safe-denial interpretation, remediation ownership, and the separate Conversations correction.

## Conflict Summary

**No conflict with `.memlog.md` decisions.** The proposal's statement that Story 5.12 verification was completed is compatible with the memlog's release thresholds only because completion means that the live mechanism ran and produced reproducible evidence. It does not mean FR-22, NFR-9, NFR-11, Epic 5 release readiness, or production release passed. The current PRD and addendum make that distinction explicit.

## Disposition

- Leave `prd.md`, `addendum.md`, and `.memlog.md` unchanged.
- Accept this input as fully reconciled into the current PRD package.
- Keep release acceptance blocked until the authorized-path and UI/static-asset failures are remediated and the live lane passes with deterministic authenticated evidence.
