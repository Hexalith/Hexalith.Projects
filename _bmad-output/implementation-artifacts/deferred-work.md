- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Correct the Hexalith.AI.Tools agent entrypoints to reference `hexalith-git-instructions.md` instead of the absent `hexalith-commit-instructions.md`.
  evidence: Blind review found the same broken guidance filename in AGENTS.md, CLAUDE.md, and the Copilot entrypoint, preventing commit rules from being discovered.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Make the Projects E2E dependency installation path suppress the recursive submodule postinstall command by construction.
  evidence: Plain npm installation actually invoked `git submodule update --init --recursive --force`; only the inspected `CI=1` fallback prevented the forbidden mutation, while current documentation still recommends an unsafe plain install.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Constrain BMAD legacy cleanup targets to direct descendants of the resolved `_bmad` root.
  evidence: Adversarial and edge-case reviews found that absolute or parent-traversal module names can escape `_bmad` before `shutil.rmtree` is called in both agent copies.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Add isolated destructive-path tests for the BMAD legacy cleanup classifier and replacement-skill guards.
  evidence: Verification-gap review found no tests proving live config directories and sole installed skill copies are protected while only verified duplicates are removed.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Validate `merge-help-csv.py` argument relationships before writing the target CSV.
  evidence: Two independent reviewers found that `--legacy-dir` without `--module-code` reports an error only after shared configuration has already been mutated.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Make `merge-config.py` validate both documents before committing coordinated configuration writes.
  evidence: Blind review found that shared config is written before user config is loaded and converted, allowing an error to leave a partially applied configuration update.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Remove the workspace-specific absolute path from the checked-in Codex BMAD Loop hook commands.
  evidence: Blind review found `.codex/hooks.json` hard-codes the current checkout path, so hooks fail when the repository is cloned or moved.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Validate BMAD Loop task IDs and event names before using them in event filenames.
  evidence: Blind review found that separators or traversal segments from environment-derived identifiers can escape the events directory or make lifecycle hooks fail.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Render Quick Dev output into a validated temporary directory before replacing the previous workflow.
  evidence: Blind review found the renderer deletes prior Markdown before reading and writing replacements, so a later failure can leave Quick Dev missing or partially rendered.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Add executable fixture coverage for Quick Dev renderer precedence, review-layer customization, placeholders, cleanup, and failure paths.
  evidence: Verification-gap review found no test or normal gate invokes the new renderer, so syntactically valid but incomplete generated workflows can pass all current product checks.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Add temporary-directory CLI tests for BMAD Loop hook event normalization and atomic delivery.
  evidence: Verification-gap review found no test exercises absent/present environment variables, payload key variants, task attribution, or canonical event emission.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Make BMAD Dev Auto detect external HEAD, status, and path-set drift before reviewing or committing changes.
  evidence: Blind review found the unattended workflow derives and commits a baseline diff without ownership revalidation; this run directly experienced a concurrent root commit and submodule fast-forwards.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Replace BMAD Dev Auto's unscoped revert instructions with isolated-worktree or owned-hunk reversal semantics.
  evidence: Blind review found intent-gap and bad-spec loopbacks can erase concurrent edits because `revert code changes` has no path ownership or overlap guard.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
  summary: Add a Memories bounded-ledger test that refreshes an old workflow before trimming and then rejects its stale replay.
  evidence: Verification-gap review demonstrated that the remove-and-reinsert watermark behavior can regress without any existing test failing once more than 256 workflow IDs are tracked.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
  summary: Add a recurring managed live AppHost Playwright lane, including targeted AppHost startup smoke coverage, lifecycle ownership, and zero-live-skip enforcement.
  evidence: Verification-gap review found the scheduled E2E job exercises only the offline lane and therefore cannot detect AppHost startup, discovery, authentication, or accidental live-test skips.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
  summary: Implement and verify real browser OIDC authorization for projects-ui.
  evidence: Review found token state can target the discovered UI origin, but projects-ui has no browser OIDC/session assertion; the current real-Keycloak proof protects the API access boundary only.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
  summary: Provision deterministic projected-tenant and sibling reference, proposal, and UI-state fixtures with parallel-safe identifiers.
  evidence: The full AppHost-backed run exposed a missing tenant access projection plus fixed sibling IDs and placeholder states that the current test harness cannot establish independently.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-prevent-submodule-skill-loading.md
  summary: Reconcile repository-required CRLF files with the default Git whitespace check.
  evidence: Review confirmed that ordinary `git diff --check` flags carriage returns on newly added CRLF lines because no repository attribute or `core.whitespace=cr-at-eol` policy exists, while `.editorconfig` requires CRLF.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-resolve-create-project-metadata-class-enforcement.md
  summary: Enforce the canonical ProjectMetadata displayName requirement without falling back to a legacy top-level name.
  evidence: Review confirmed the OpenAPI schema requires projectMetadata.displayName, but the pre-existing direct endpoint accepts a canonical object with missing or blank displayName when a top-level name is supplied; this is outside the bounded metadataClass E-9 correction.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md
  summary: Align proposal-confirmation file-reference ID ordering between accepted HTTP requests and generated-client fingerprints.
  evidence: Review confirmed the pre-existing endpoint accepts unsorted fileReferenceIds after sorting them for validation and server hashing, while the generated helper hashes the caller's original array order.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md
  summary: Align proposal-confirmation null fileReferenceIds semantics between the server and generated client.
  evidence: Review confirmed the pre-existing endpoint accepts null fileReferenceIds when no file references exist and hashes an empty array, while the generated helper hashes the null property as null.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-canonicalize-reference-health-freshness-vocabulary.md
  summary: Restore monotonic sprint tracker chronology after the pre-existing last_updated timestamp regression.
  evidence: Review confirmed sprint-status.yaml records July 31 action completions while its pre-existing dirty last_updated value moves backward to July 19, which may cause incremental consumers to miss updates.
- source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-canonicalize-reference-health-freshness-vocabulary.md
  summary: Reconcile the U+2028/U+2029 sprint action promised by its approved July 31 duplicate-trigger closure proposal.
  evidence: Review confirmed the pre-existing approved proposal states that action was moved to done, while sprint-status.yaml still leaves the matching action in-progress; this correction intentionally changed only the freshness-vocabulary action.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md`
  summary: Preserve caller cancellation through Web warning/dashboard list and diagnostic requests.
  evidence: Review confirmed `ProjectWarningsDashboardSource` catches `OperationCanceledException` through its general exception handlers, returning safe feedback or a synthetic unavailable row instead of propagating requested cancellation; this production behavior predates the coverage-only change.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md`
  summary: Make the Web diagnostic-unavailable tile drill-in select only rows represented by its count.
  evidence: Review confirmed the tile count tracks diagnostic failures while its existing state-only filter selects every `ReferenceState.Unavailable` row, including ordinary unavailable references; changing filter semantics is outside this coverage-only task.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md`
  summary: Define and align warning diagnostic scan cardinality across Web, MCP, and CLI.
  evidence: Review confirmed Web enriches every returned visible Project while MCP and CLI scan at most 25 Projects, and MCP also uses query `Take` to bound the scanned set; inventories larger than the bound can therefore produce surface-specific unavailable counts.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md`
  summary: Define MCP warning-queue output when diagnostics fail but no healthy warning row is emitted.
  evidence: Review confirmed the existing MCP warning resource carries `DiagnosticUnavailable` only on emitted warning rows, so a nonzero count is unobservable from an empty warning queue; the approved spec explicitly reserved no-row semantics for a separate contract decision.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md`
  summary: Build the MCP operational dashboard from one visible-inventory snapshot.
  evidence: Review confirmed the existing dashboard reads inventory once for lifecycle totals and again inside warning scanning, so concurrent inventory changes can yield counters derived from different snapshots; production scan restructuring is outside this coverage-only task.
