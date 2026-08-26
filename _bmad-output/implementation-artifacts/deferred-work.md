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
status: done 2026-08-25
resolution: already resolved: Commit c2f60e9cf0db942a54cd2c130884aec8459288f2; .agents/skills/bmad-bmb-setup/scripts/merge-help-csv.py:358-366 rejects --legacy-dir without --module-code before publication at line 416.

### DW-7: Make `merge-config.py` validate both documents before committing coordinated configuration writes.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: merge-config.py
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found that shared config is written before user config is loaded and converted, allowing an error to leave a partially applied configuration update.
status: done 2026-08-25
resolution: already resolved: Commit c2f60e9cf0db942a54cd2c130884aec8459288f2; .agents/skills/bmad-bmb-setup/scripts/merge-config.py:579-630 loads and serializes both prospective documents before the failure-atomic pair publisher runs.

### DW-8: Remove the workspace-specific absolute path from the checked-in Codex BMAD Loop hook commands.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: .codex/hooks.json
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the hook commands hard-code the current checkout path, so hooks fail when the repository is cloned or moved.
status: done 2026-08-25
resolution: resolved by sweep bundle dw-portable-safe-loop-hooks
resolution-undo: f07edf5b0801aa33fcef57cec31a638619b49084e2868b360043d30665422827 2026-08-25 7374617475733a206f70656e

### DW-9: Validate BMAD Loop task IDs and event names before using them in event filenames.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Loop hook event filename handling
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found that separators or traversal segments from environment-derived identifiers can escape the events directory or make lifecycle hooks fail.
status: done 2026-08-25
resolution: resolved by sweep bundle dw-portable-safe-loop-hooks
resolution-undo: f07edf5b0801aa33fcef57cec31a638619b49084e2868b360043d30665422827 2026-08-25 7374617475733a206f70656e

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
status: done 2026-08-25
resolution: resolved by sweep bundle dw-render-skill-fixture-coverage
resolution-undo: cd59c929b1db90412cd32cc4ca54c10fb3d7346e3c409daf3825a4ff991e43c4 2026-08-25 7374617475733a206f70656e

### DW-12: Add temporary-directory CLI tests for BMAD Loop hook event normalization and atomic delivery.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Loop hook CLI test coverage
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Verification-gap review found no test exercises absent or present environment variables, payload key variants, task attribution, or canonical event emission.
status: done 2026-08-25
resolution: resolved by sweep bundle dw-portable-safe-loop-hooks
resolution-undo: f07edf5b0801aa33fcef57cec31a638619b49084e2868b360043d30665422827 2026-08-25 7374617475733a206f70656e

### DW-13: Make BMAD Dev Auto detect external HEAD, status, and path-set drift before reviewing or committing changes.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Dev Auto workflow
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the unattended workflow derives and commits a baseline diff without ownership revalidation; the originating run directly experienced a concurrent root commit and submodule fast-forwards.
status: done 2026-08-25
resolution: resolved by sweep bundle dw-build-auto-workspace-ownership
resolution-undo: 30119a05d487fd4a24797da98dce8f3d23409abcd886139546b8eb5d821df7a5 2026-08-25 7374617475733a206f70656e

### DW-14: Replace BMAD Dev Auto's unscoped revert instructions with isolated-worktree or owned-hunk reversal semantics.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Dev Auto workflow
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found intent-gap and bad-spec loopbacks can erase concurrent edits because `revert code changes` has no path ownership or overlap guard.
status: done 2026-08-25
resolution: resolved by sweep bundle dw-build-auto-workspace-ownership
resolution-undo: 30119a05d487fd4a24797da98dce8f3d23409abcd886139546b8eb5d821df7a5 2026-08-25 7374617475733a206f70656e

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

### DW-39: Existing-generation verification follows symlinked output files, so matching external bytes can satisfy an immutable snapshot check.
origin: spec-deferred bfaafa1248ab
location: _bmad/scripts/render_skill.py:279
source_spec: `spec-render-skill-fixture-coverage.md`
severity: high
reason: `_verify_existing` builds its file set with `Path.is_file()` and hashes outputs with `Path.read_bytes()`; both follow symlinks. A symlink to external bytes matching the manifest therefore verifies successfully, while the external target can change after verification without changing the generation directory or manifest.
status: open

### DW-40: The fixture suite's `assert_manifest_integrity` helper re-derives root_hash, generation_hash, and project_slug with its own copy of the renderer's hashing/slugging formulas instead of importing the re
origin: spec-deferred c023fdd6d1ee
location: tests/tools/test_render_skill.py:316-358
source_spec: `spec-render-skill-fixture-coverage.md`
severity: medium
reason: `tests/tools/test_render_skill.py` recomputes `_sha256(str(project_root).encode())[:12]` and `_sha256(_canonical_json(inputs))[:20]` inline rather than calling into `render_skill.py` (already loadable via the file's own `_load_renderer_module()` helper). A regression in the real hashing/slugging algorithm would be mirrored by the test's parallel implementation and pass silently.
status: open

### DW-41: The keyed review layer's `when` clause is only ever asserted absent from rendered output, never asserted present for a layer whose `when` clause survives to the final resolution.
origin: spec-deferred f63e08052e13
location: _bmad/scripts/render_skill.py:171-172
source_spec: `spec-render-skill-fixture-coverage.md`
severity: low
reason: `test_layer_precedence_lists_and_keyed_review_replacement` only exercises the negative case (`assertNotIn("Run only when: always", rendered)`) for a layer whose `when` field is dropped by a whole-table override. No fixture keeps a `when` clause on a surviving layer through to final render, so a regression in the `Run only when: {value}` formatting at `render_skill.py:172` would go undetected.
status: open

### DW-42: The `customization.workflow.open_spec` field's special-cased empty-string allowance has no fixture coverage.
origin: spec-deferred 8b5c84060986
location: _bmad/scripts/render_skill.py:180
source_spec: `spec-render-skill-fixture-coverage.md`
severity: low
reason: `_resolve_customization_value` at `render_skill.py:180` hardcodes `label == "customization.workflow.open_spec"` as an exception permitting an empty override even when the default is non-empty. No fixture defines an `open_spec` customization key, so this named carve-out ships with zero coverage.
status: open

### DW-43: No fixture demonstrates the existing render-source escape guard, so a regression there would ship undetected.
origin: spec-deferred e5621d164d42
location: _bmad/scripts/render_skill.py:105-106
source_spec: `spec-render-skill-fixture-coverage.md`
severity: low
reason: `_load_sources` at `_bmad/scripts/render_skill.py:105-106` resolves each candidate source and raises `RenderError(f"render source escapes skill directory: {name}")` when the resolved path is not relative to `skill_dir` (guarding against a symlinked or otherwise escaping source file). `tests/tools/test_render_skill.py` has no fixture that creates such a source and asserts this HALT message, so a regression that weakens or removes the check would pass the current suite silently.
status: open

### DW-44: Introduce an admission state (responseState, components, recoveryActions) for the Conversation-start read.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
reason: ProjectContextInclusionPolicy.Assemble hardcodes Assembled on the success path and the wire DTO deliberately omits AssemblyOutcome, so a Chatbot receiving 200 has no signal on which to withhold a first response. 200 means "authorized", not "usable".
status: open
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-45: Bind the Conversation-start snapshot to an authorized projectVersion.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
reason: ProjectDetailItem carries a Sequence watermark but the DTO explicitly excludes it, enforced by GetConversationStartSetup_BodyDoesNotContainAuditMetadata. Nothing ties the setup a Chatbot admits on to a Project version, so a concurrent archive or setup update is undetectable by the caller.
status: open
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-46: Evaluate exactly-one-Folder eligibility on the Conversation-start read.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:126
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
reason: The handler constructs ProjectContextReferenceEvidence with ProjectFolder null and empty collections while authorization.ProjectDetail already carries ProjectFolderReference. A Project with no Folder, an archived Folder, or an ambiguous Folder produces the same admissible-looking 200.
status: open
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-47: Derive dual-principal authority (delegation, workload, scopes, audience) for Conversation-start reads.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs:350
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
reason: HttpContextProjectTenantContextAccessor resolves only a tenant claim and a NameIdentifier/sub principal; no delegation, scope or audience is derived anywhere in Hexalith.Projects.Server. A delegated Chatbot workload is authorized as if it were the original actor, which is the exact distinction first-response admission rests on.
status: open
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-48: Source freshness from the projection that supplies the Conversation-start data.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:114
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: critical
reason: Freshness is mapped from the tenant-access projection while the payload comes from ProjectDetailItem, whose Sequence watermark is never consulted. A rebuilding or stale ProjectDetail is reported Fresh whenever tenant-access is fresh, so even the advisory label is untrustworthy. Needs the Story 6.2 persisted read model with provenance. Promoted 2026-08-26 from deferred to a blocking prerequisite of Story 6.2 AC2: the "block on non-current evidence" ruling fires on the wrong signal in both directions until this is fixed, blocking when tenant-access is stale but the detail is fine, and failing to block when the detail is stale but tenant-access is fresh.
status: open
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-49: Generate a correlation id when the caller supplies none, or correct the declared adapter behavior.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:141
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: OpenAPI declares caller-provided-or-generated-correlation but nothing in the module generates one; a caller that omits or malforms the header gets no X-Correlation-Id back. Systemic across ResolveProjectFromAttachments, ResolveProjectFromConversation, GetProjectContextExplanation and eight sites in ProjectsDomainServiceEndpoints, so fixing one endpoint alone would be inconsistent.
status: open

### DW-50: Echo X-Correlation-Id on error responses, not only on 200.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:141
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: The correlation header assignment sits after every SafeDenial, ReadModelUnavailable and ValidationProblem return, so 400/404/503 carry no correlation id — the exact responses where a caller most needs one. Systemic across the module, and no test asserts headers on any error response.
status: open

### DW-51: Produce persisted before/after zero-write evidence for the Conversation-start read.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupTests.cs
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: The strongest existing assertion is sibling-scoped call counts in GetConversationStartSetup_DoesNotCallSiblingAcls. The path does appear write-free, but no state-store end-state assertion exists, so there is no evidence artifact to carry into AC4.
status: open
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-52: Close or explicitly accept the denial-depth timing channel.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Server/Authorization/ProjectAuthorizationGate.cs:350
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: AuthorizeAsync returns at six different depths, each skipping the remaining awaited calls. Status, headers and body are uniform because SafeDenial discards the reason, so timing is the only residual discriminator against AC3's requirement that every caller-visible category including timing be indistinguishable. Currently untested.
status: open
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-53: Extract the shared query-handler request preamble.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Server/Queries/
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: Correlation and task id reads, the IsCanonicalIdentifier guard, the Idempotency-Key check, the X-Hexalith-Freshness strict-equality check and the TenantAccessResult defensive collapse are duplicated across at least four query handlers, so any fix to them must be applied N times. The Story 3.5 handler documents itself as a port of Story 3.2.
status: open

### DW-54: Resolve OpenAPI spine inconsistencies surfaced by the Story 3.5 audit.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: One payload mixes PascalCase (Active, Fresh) with camelCase (authorizedReferences) enum values, a permanent client-side trap; the requested-versus-authoritative tenant guard is tautological on this path because both arguments are the authoritative tenant, leaving one declared defense layer dead; and the new public route landed with no changelog entry and no info.version bump. All are consistent with Stories 3.2-3.4 precedent, so they need a spine-wide fix rather than a per-endpoint one.
status: open
decision: 2026-08-26 Break v1 in place — Normalize current v1 wire enums and guards, bump contract metadata, regenerate every client, and provide explicit migration evidence for consumers.

### DW-55: Create the standalone Hexalith.Projects.UI.Contracts split story and land it before Stories 6.6, 8.4 and 8.5.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj:22-33
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
reason: AC6 was removed from Story 6.2 on 2026-08-26 because it is packaging hygiene with no runtime admission consequence and Story 6.2 already carries multiple-goals and oversized warnings. The coupling is structural rather than file placement: src/Hexalith.Projects.Contracts/Ui/ProjectContextFreshness.cs:10,25,29 decorates a domain/wire enum with [ProjectionBadge] from Hexalith.FrontComposer.Contracts.Attributes, so the split requires stripping UI attributes off domain enums and re-expressing badge mapping in the UI layer. Acceptance gate already exists at tests/tools/run-package-dependency-gate.ps1. Must land before Stories 6.6, 8.4 and 8.5 or the CLI and MCP surfaces inherit Blazor/Fluxor transitively through the supported contracts. Story creation requires tools/planning/validate_production_authority.py --story-id and a sprint-status.yaml entry; neither may be performed by Story 6.2.
status: open
decision: 2026-08-26 Create and execute split story — Run the planning authority gate, create and schedule the standalone story, introduce the non-packable UI.Contracts boundary, strip UI attributes from domain and wire enums, re-home descriptor mapping, and update the package dependency gate before Stories 6.6, 8.4, and 8.5.

### DW-56: Give the supported Conversation-start response structural equality over its bounded collections.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: ConversationStartSetup.FromContext aliases the caller-owned collections held by ProjectContext.Setup rather than copying them, so the bounded subset is a live view of caller state when the source is a mutable list. The obvious fix was attempted during the 2026-08-26 review and reverted: ConversationStartSetup is a record in a packable assembly and record equality compares IReadOnlyList<T> members by reference, so defensive copying makes two projections of the same context unequal and breaks Project_IsPureFunction_SameInputProducesSameOutput. Copying therefore changes public DTO equality semantics for every consumer and must not be done to the legacy type in isolation. Resolve it on the Story 6.2 supported response wrapper by using a collection type with structural equality (for example ImmutableArray<T> with an explicit equality contract) from the start, then align the legacy type at the Story 6.7 cutover.
status: open
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-57: Make the conversation-start freshness response header reflect actual freshness.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:144
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: The header is written unconditionally as eventually_consistent while OpenAPI declares freshnessBehavior returns-projection-watermark-when-available, so a stale or unavailable read is indistinguishable from a fresh one at the header and no watermark is ever returned. Deferred to Story 6.7 rather than patched now because changing legacy response headers moves the baseline the shadow comparison is specified against.
status: open
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-58: Reject Idempotency-Key supplied as a query parameter, not only as a header.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:93
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: The operation description states that Idempotency-Key is not a query parameter and is rejected if present, but only HasHeader is checked, and GetConversationStartSetup_ExtraQueryParameters_AreIgnoredNotFailed makes the query form pass silently. Deferred to Story 6.7 because it changes legacy request-validation behavior.
status: open
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-59: Reconcile the declared 401 and 403 responses with the safe-denial collapse.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:584
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: The operation declares 401 and 403 and the generated client throws distinct typed exceptions for both, but the handler only ever emits 200, 400, 404 and 503 because all denial outcomes collapse to a safe 404. Either the declaration is wrong or the collapse is under-specified, and Story 6.2 AC3 needs an agreed baseline status set to compare against. Deferred to Story 6.7 because editing the spine requires client regeneration and a fingerprint update, and it changes the published contract.
status: open
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-60: Widen the retryable collapse so transient authorization failures return 503, not a permanent 404.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:87
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: The branch requires both Retryable and Reason == ReferenceState.Unavailable, so a retryable outcome carrying a different reason collapses to a permanent 404 and the caller never retries. ProjectDaprPolicyEvidenceResult.Unavailable sets Retryable true with reason dapr_policy_unavailable, so reachability depends on the gate's reason mapping and should be confirmed before the fix. Deferred to Story 6.7 because it changes legacy status-code behavior.
status: open
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-61: Enforce the declared bounded-collection limits on the conversation-start response.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: The spine constrains goals and userInstructions to 16 items with per-item length 1 to 512, and the source-kind arrays to 4 items, but ConversationStartSetup copies whatever the projection holds with no truncation or validation, so the server can emit a body violating its own published schema. Story 3.5 deviation L3 accepted this on the stated grounds that the lengths are bounded upstream in ProjectSetup validation; that premise is false, as no count or length check on Goals or UserInstructions exists anywhere in src/. Deferred to Story 6.7 because enforcement changes legacy response behavior for already-stored oversized setups.
status: done 2026-08-26
resolution: already resolved: src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs:41-43 and :850-893 enforce the exact 16-item/512-character and 4-source-kind bounds declared at src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:3917-3946; git blame attributes the checks to pre-origin commits a12ca891 and 88be25e2.
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-62: Add cache directives to the tenant-sensitive conversation-start read.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:145
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: goals and userInstructions are tagged x-hexalith-sensitive-metadata-tier tenant_sensitive, yet the response is emitted with no Cache-Control no-store and no Vary handling. This is the fast-path endpoint called at every conversation start, so it is the one intermediaries are most likely to cache. Deferred to Story 6.7 because it changes legacy response headers and therefore the shadow baseline.
status: open
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback
