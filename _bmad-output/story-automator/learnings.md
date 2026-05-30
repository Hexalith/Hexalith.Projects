## Run: 2026-05-30T22:51:38Z

**Epic:** Hexalith.Projects - Epic Breakdown
**Stories:** 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10, 5.11

### Patterns Observed
- Codex handled most create/dev work, but several long-running child sessions ended in session-gone, crash, or interactive-prompt states; source-of-truth story and sprint-status checks were necessary to recover without losing progress.
- Review cycles caught the highest-value issues when they challenged acceptance-criteria claims against runnable evidence, especially around E2E accessibility, tenant-denial behavior, and parity fields.
- Root-level submodule hygiene mattered: E2E dependency setup had to avoid recursive submodule commands and preserve nested submodules as uninitialized.

### Code Review Insights
- Common issues: over-claimed test coverage, missing cross-surface parity fields, unsafe distinction between 403 and 404 denial paths, and responsive CSS that still forced mobile horizontal scrolling.
- Average cycles to clean: most stories cleared in one review cycle; Stories 5.9 and 5.11 needed fallback or re-review after substantive fixes.

### Timing Estimates
- create-story: usually 5-8 minutes per story after source context was available.
- dev-story: varied widely; narrow stories completed in 15-25 minutes, while final parity/accessibility hardening needed local parent recovery.
- code-review: typically 15-25 minutes per cycle, with additional time when review auto-fixed and revalidated.

### Recommendations for Future Runs
- Treat E2E prerequisites as first-class preflight: provision Node 24, browser strategy, and AppHost availability before stories that depend on Playwright evidence.
- Add a parity-field checklist for MCP/CLI/Web resources so docs, DTOs, and tests cannot drift on required fields such as `diagnosticUnavailable`.
- Keep explicit safe-denial tests for `401/403/404` on every new Web/MCP/CLI surface; this catches cross-tenant disclosure regressions early.
- Prefer no-AppHost fixture tests only as runnable contracts when live AppHost tests remain blocked, and keep the live tests marked with a concrete blocker.
