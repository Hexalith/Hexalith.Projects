# Deferred Work

### DW-1: Deliver and accept Story 6.5 supported-read prerequisites

origin: bmad-loop-resolve for spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md, 2026-08-25
location: _bmad-output/implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md:31
severity: critical
reason: Story 6.5 must not dispatch until its prerequisite record pins accepted Story 6.1–6.4 supported contracts, read models, projections, handlers, identity inputs, and executable G-4 profiles. Close this entry only after every artifact required by the frozen prerequisite gate exists and is accepted; legacy REST and planning-only artifacts never satisfy the gate.
status: open
gate: 6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface

### DW-2: Correct the Hexalith.AI.Tools agent entrypoints to reference `hexalith-git-instructions.md` instead of the absent `hexalith-commit-instructions.md`.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: AGENTS.md, CLAUDE.md, and .github/copilot-instructions.md
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the same broken guidance filename in all three agent entrypoints, preventing commit rules from being discovered.
status: done 2026-08-25
resolution: already resolved: Commit d0889739b33856e37a8cbbedbbb440940a43ab9b removed the absent hexalith-commit-instructions.md reference; references/Hexalith.AI.Tools/hexalith-llm-instructions.md:29-31 now names hexalith-git-instructions.md and all three entrypoints are byte-identical.

### DW-3: Make the Projects E2E dependency installation path suppress the recursive submodule postinstall command by construction.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: Projects E2E dependency installation
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Plain npm installation invoked `git submodule update --init --recursive --force`; only the inspected `CI=1` fallback prevented the forbidden mutation while current documentation still recommends an unsafe plain install.
status: done 2026-08-25
resolution: already resolved: Commit f03a8d6deb28dbc60062e176ab5813114d5a0ca3 resolved the unsafe install path; tests/e2e/README.md:22-29 requires CI=1 npm ci --ignore-scripts and .github/workflows/ci.yml:143-149 uses it.

### DW-4: Constrain BMAD legacy cleanup targets to direct descendants of the resolved `_bmad` root.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD legacy cleanup classifier in both agent copies
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Adversarial and edge-case reviews found that absolute or parent-traversal module names can escape `_bmad` before `shutil.rmtree` is called in both agent copies.
status: done 2026-08-25
resolution: resolved by sweep bundle dw-legacy-cleanup-path-safety
resolution-undo: fbc12d6bdcb9408d53250ac593b2b7c43d82af87fb6abe8e4c7808ddf8728dcb 2026-08-25 7374617475733a206f70656e

### DW-5: Add isolated destructive-path tests for the BMAD legacy cleanup classifier and replacement-skill guards.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD legacy cleanup and replacement-skill test coverage
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Verification-gap review found no tests proving live config directories and sole installed skill copies are protected while only verified duplicates are removed.
status: done 2026-08-25
resolution: resolved by sweep bundle dw-legacy-cleanup-path-safety
resolution-undo: fbc12d6bdcb9408d53250ac593b2b7c43d82af87fb6abe8e4c7808ddf8728dcb 2026-08-25 7374617475733a206f70656e

### DW-6: Validate `merge-help-csv.py` argument relationships before writing the target CSV.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: merge-help-csv.py
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Two independent reviewers found that `--legacy-dir` without `--module-code` reports an error only after shared configuration has already been mutated.
status: open

### DW-7: Make `merge-config.py` validate both documents before committing coordinated configuration writes.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: merge-config.py
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found that shared config is written before user config is loaded and converted, allowing an error to leave a partially applied configuration update.
status: open

### DW-8: Remove the workspace-specific absolute path from the checked-in Codex BMAD Loop hook commands.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: .codex/hooks.json
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the hook commands hard-code the current checkout path, so hooks fail when the repository is cloned or moved.
status: open

### DW-9: Validate BMAD Loop task IDs and event names before using them in event filenames.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Loop hook event filename handling
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found that separators or traversal segments from environment-derived identifiers can escape the events directory or make lifecycle hooks fail.
status: open

### DW-10: Render Quick Dev output into a validated temporary directory before replacing the previous workflow.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: Quick Dev workflow renderer
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the renderer deletes prior Markdown before reading and writing replacements, so a later failure can leave Quick Dev missing or partially rendered.
status: done 2026-08-25
resolution: already resolved: Commit a0dea374b3b990a38e23357934817969ba4a03e4 replaced legacy Quick Dev rendering; _bmad/scripts/render_skill.py:295-319 renders to a sibling staging directory, atomically renames it, and cleans staging on every exit.

### DW-11: Add executable fixture coverage for Quick Dev renderer precedence, review-layer customization, placeholders, cleanup, and failure paths.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: Quick Dev renderer test coverage
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Verification-gap review found no test or normal gate invokes the new renderer, so syntactically valid but incomplete generated workflows can pass all current product checks.
status: open

### DW-12: Add temporary-directory CLI tests for BMAD Loop hook event normalization and atomic delivery.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Loop hook CLI test coverage
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Verification-gap review found no test exercises absent or present environment variables, payload key variants, task attribution, or canonical event emission.
status: open

### DW-13: Make BMAD Dev Auto detect external HEAD, status, and path-set drift before reviewing or committing changes.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Dev Auto workflow
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the unattended workflow derives and commits a baseline diff without ownership revalidation; the originating run directly experienced a concurrent root commit and submodule fast-forwards.
status: open

### DW-14: Replace BMAD Dev Auto's unscoped revert instructions with isolated-worktree or owned-hunk reversal semantics.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Dev Auto workflow
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found intent-gap and bad-spec loopbacks can erase concurrent edits because `revert code changes` has no path ownership or overlap guard.
status: open

### DW-15: Add a Memories bounded-ledger test that refreshes an old workflow before trimming and then rejects its stale replay.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: Hexalith.Memories bounded workflow ledger tests
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Verification-gap review demonstrated that remove-and-reinsert watermark behavior can regress without any existing test failing once more than 256 workflow IDs are tracked.
status: done 2026-08-25
resolution: already resolved: Commit 9af726a336aa604b051e2203438dc15ee728e9ce added references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterLogicTests.cs:164-190, which refreshes an old workflow at the 256-entry limit, trims, and rejects its stale replay.

### DW-16: Add a recurring managed live AppHost Playwright lane, including targeted AppHost startup smoke coverage, lifecycle ownership, and zero-live-skip enforcement.

origin: migrated from legacy ledger ("flat append from spec-5-12-live-apphost-operational-console-verification.md"), 2026-08-25
location: scheduled E2E workflow and AppHost Playwright lane
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
reason: Verification-gap review found the scheduled E2E job exercises only the offline lane and therefore cannot detect AppHost startup, discovery, authentication, or accidental live-test skips.
status: open

### DW-17: Implement and verify real browser OIDC authorization for projects-ui.

origin: migrated from legacy ledger ("flat append from spec-5-12-live-apphost-operational-console-verification.md"), 2026-08-25
location: projects-ui browser authentication
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
reason: Review found token state can target the discovered UI origin, but projects-ui has no browser OIDC or session assertion; the current real-Keycloak proof protects only the API access boundary.
status: open

### DW-18: Provision deterministic projected-tenant and sibling reference, proposal, and UI-state fixtures with parallel-safe identifiers.

origin: migrated from legacy ledger ("flat append from spec-5-12-live-apphost-operational-console-verification.md"), 2026-08-25
location: AppHost-backed Projects E2E fixture provisioning
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
reason: The full AppHost-backed run exposed a missing tenant access projection plus fixed sibling IDs and placeholder states that the current test harness cannot establish independently.
status: open

### DW-19: Reconcile repository-required CRLF files with the default Git whitespace check.

origin: migrated from legacy ledger ("flat append from spec-prevent-submodule-skill-loading.md"), 2026-08-25
location: .editorconfig and Git whitespace configuration
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-prevent-submodule-skill-loading.md
reason: Review confirmed ordinary `git diff --check` flags carriage returns on newly added CRLF lines because no repository attribute or `core.whitespace=cr-at-eol` policy exists while `.editorconfig` requires CRLF.
status: open

### DW-20: Enforce the canonical ProjectMetadata displayName requirement without falling back to a legacy top-level name.

origin: migrated from legacy ledger ("flat append from spec-resolve-create-project-metadata-class-enforcement.md"), 2026-08-25
location: Create Project endpoint ProjectMetadata validation
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-resolve-create-project-metadata-class-enforcement.md
reason: Review confirmed the OpenAPI schema requires `projectMetadata.displayName`, but the pre-existing direct endpoint accepts a canonical object with a missing or blank displayName when a top-level name is supplied; this is outside the bounded metadataClass E-9 correction.
status: open

### DW-21: Align proposal-confirmation file-reference ID ordering between accepted HTTP requests and generated-client fingerprints.

origin: migrated from legacy ledger ("flat append from spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md"), 2026-08-25
location: proposal-confirmation endpoint and generated client
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md
reason: Review confirmed the pre-existing endpoint accepts unsorted `fileReferenceIds` after sorting them for validation and server hashing, while the generated helper hashes the caller's original array order.
status: open

### DW-22: Align proposal-confirmation null fileReferenceIds semantics between the server and generated client.

origin: migrated from legacy ledger ("flat append from spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md"), 2026-08-25
location: proposal-confirmation endpoint and generated client
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md
reason: Review confirmed the pre-existing endpoint accepts null `fileReferenceIds` when no file references exist and hashes an empty array, while the generated helper hashes the null property as null.
status: open

### DW-23: Restore monotonic sprint tracker chronology after the pre-existing last_updated timestamp regression.

origin: migrated from legacy ledger ("flat append from spec-canonicalize-reference-health-freshness-vocabulary.md"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-canonicalize-reference-health-freshness-vocabulary.md
reason: Review confirmed the sprint tracker records July 31 action completions while its pre-existing dirty `last_updated` value moves backward to July 19, which may cause incremental consumers to miss updates.
status: done 2026-08-25
resolution: already resolved: Commit 729798ab09cbff27223e06c019a1532865713da1 advanced sprint chronology; current _bmad-output/implementation-artifacts/sprint-status.yaml:2 records last_updated 2026-08-03T08:21:35+02:00, later than the July 31 completions.

### DW-24: Reconcile the U+2028/U+2029 sprint action promised by its approved July 31 duplicate-trigger closure proposal.

origin: migrated from legacy ledger ("flat append from spec-canonicalize-reference-health-freshness-vocabulary.md"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-canonicalize-reference-health-freshness-vocabulary.md
reason: Review confirmed the pre-existing approved proposal states that the action was moved to done while the sprint tracker still leaves the matching action in-progress; the originating correction intentionally changed only the freshness-vocabulary action.
status: done 2026-08-25
resolution: already resolved: Commit 729798ab09cbff27223e06c019a1532865713da1 moved the U+2028/U+2029 action to done; current _bmad-output/implementation-artifacts/sprint-status.yaml:203-209 records done with fresh green evidence.

### DW-25: Preserve caller cancellation through Web warning/dashboard list and diagnostic requests.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: ProjectWarningsDashboardSource
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed `ProjectWarningsDashboardSource` catches `OperationCanceledException` through its general exception handlers, returning safe feedback or a synthetic unavailable row instead of propagating requested cancellation; this production behavior predates the coverage-only change.
status: open

### DW-26: Make the Web diagnostic-unavailable tile drill-in select only rows represented by its count.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: Web diagnostic-unavailable tile filter
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed the tile count tracks diagnostic failures while its existing state-only filter selects every `ReferenceState.Unavailable` row, including ordinary unavailable references; changing filter semantics was outside the coverage-only task.
status: open

### DW-27: Define and align warning diagnostic scan cardinality across Web, MCP, and CLI.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: Web, MCP, and CLI warning scans
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed Web enriches every returned visible Project while MCP and CLI scan at most 25 Projects, and MCP also uses query `Take` to bound the scanned set; inventories larger than the bound can therefore produce surface-specific unavailable counts.
status: open
decision: 2026-08-25 Shared 25-project window — Define one deterministic 25-project diagnostic window shared by Web, MCP, and CLI, keep inventory totals separate from scanned-warning totals, decouple MCP output Take from scan scope, and add inventories-over-25 parity tests.

### DW-28: Define MCP warning-queue output when diagnostics fail but no healthy warning row is emitted.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: MCP warning resource
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed the existing MCP warning resource carries `DiagnosticUnavailable` only on emitted warning rows, so a nonzero count is unobservable from an empty warning queue; the approved spec explicitly reserved no-row semantics for a separate contract decision.
status: open
decision: 2026-08-25 Add summary resource — Add a dedicated MCP warning-scan summary DTO and resource that always emits scan cardinality and DiagnosticUnavailable while leaving warning rows unchanged, then update dispatch, documentation, and contract tests.

### DW-29: Build the MCP operational dashboard from one visible-inventory snapshot.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: MCP operational dashboard
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed the existing dashboard reads inventory once for lifecycle totals and again inside warning scanning, so concurrent inventory changes can yield counters derived from different snapshots; production scan restructuring was outside the coverage-only task.
status: open

### DW-30: Add the mandatory 6.1-P1R prerequisite to the Story 6.4 implementation artifact.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
location: Story 6.4 implementation artifact
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
reason: Review confirmed the pre-existing Story 6.4 `blocked_by` metadata and entry-condition prose omit P1R even though the current Epic 6 dependency context requires accepted P1R for later consumers; Story 6.4 was outside the candidate-baseline patch.
status: open

### DW-31: Replace Story 6.4's stale current EventStore 3.86.0 observations with the truthful unaccepted 3.88.0 candidate state.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
location: Story 6.4 implementation artifact
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
reason: Review confirmed the pre-existing Story 6.4 artifact still calls 3.86.0 the current central pin after the source, catalog, and committed runner candidate moved to 3.88.0; Story 6.4 was outside the candidate-baseline patch.
status: open

### DW-32: Bind canonical readiness artifacts to the actual module-manifest byte hash.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
location: canonical readiness fixture and module manifest
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
reason: Review confirmed the pre-existing readiness fixture uses a synthetic all-`A` `manifestHash` even though production run evidence computes SHA-256 from the manifest file, allowing the fixture to validate without proving the manifest identity it cites.
status: open

### DW-33: Bind passing readiness rows to normalized filter, fixture, manifest, and hash identities in module-run evidence.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
location: ArtifactBindsToRow readiness evidence validation
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
reason: Review confirmed the pre-existing `ArtifactBindsToRow` check compares only the module subcommand and profile, so a row declaring a filter and fixture can pass with an artifact that records neither; broadening the evidence contract was outside the pin-revalidation patch.
status: open

### DW-34: Propagate the verified EventStore 3.90.0 candidate and exact qualifying Builds revision through the Projects sprint ledger, Epic 6 context, epics, and readiness matrix views.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline-2.md"), 2026-08-25
location: Projects sprint ledger, Epic 6 context, epics, and readiness matrix views
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline-2.md
reason: These root planning edits form a separately reviewable repository change and depend on the exact qualifying Builds revision produced by the narrowed Builds alignment task.
status: open

### DW-35: Complete clean EventStore source/package and reciprocal rollback qualification, obtain four-owner P1R acceptance, then rebind Architecture and close only P1R.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline-2.md"), 2026-08-25
location: EventStore qualification, P1R acceptance, and Architecture binding
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline-2.md
reason: Acceptance and Architecture propagation require external owner decisions and complete retained clean-worktree evidence after the narrowed Builds candidate is aligned; they cannot be truthfully completed by the candidate-alignment change alone.
status: open
decision: 2026-08-25 External exact worktrees — Authorize a fresh attempt that materializes every recorded dependency gitlink as an isolated exact-revision worktree at the paths required by governance tests without nested submodule initialization or source/gitlink changes; restart at coordinate capture, complete candidate and rollback qualification, and prepare the four-owner acceptance packet.

### DW-36: Sequential shutil.rmtree calls across multiple validated targets are not atomic, so a later target's filesystem failure can leave earlier targets already deleted.
origin: spec-deferred 8d336adc2d02
location: .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:cleanup_directories
source_spec: `spec-legacy-cleanup-path-safety.md`
severity: medium
reason: cleanup_directories() in all six cleanup-legacy.py copies loops over validated targets and calls shutil.rmtree per target inside its own try/except; if an OSError/RuntimeError is raised on target N after targets 1..N-1 already succeeded, main() exits via runtime_error (exit 2) but the already-removed directories are not restored. This sequential, non-atomic deletion behavior is unchanged from the pre-diff implementation -- the diff only changed which exception types are caught and how the failure is reported.
status: open

### DW-37: A symlink cycle nested inside an already-validated cleanup target can make find_skill_dirs and count_files raise an unhandled RecursionError instead of the documented JSON exit-2 contract.
origin: spec-deferred 6e82e0d0ef7e
location: .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:find_skill_dirs,count_files
source_spec: `spec-legacy-cleanup-path-safety.md`
severity: medium
reason: Both functions catch only (OSError, RuntimeError) around their Path.rglob() calls. rglob recurses into symlinked subdirectories, so a self-referential symlink inside a target's subtree (not the top-level target itself, which is already rejected by the direct-child symlink guard in resolve_cleanup_targets) can trigger unbounded recursion. This traversal behavior predates this diff -- both functions used the same rglob pattern before this change, with no handling for this case either.
status: open

### DW-38: Follow-up review still recommended for dw-legacy-cleanup-path-safety after the damping cap was spent
origin: review-budget-followup
location: n/a
source_spec: `spec-legacy-cleanup-path-safety.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260825-081747-f4a1; this entry preserves the lingering recommendation for a deliberate later review.
status: open
