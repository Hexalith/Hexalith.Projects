### DW-2: Correct the Hexalith.AI.Tools agent entrypoints to reference `hexalith-git-instructions.md` instead of the absent `hexalith-commit-instructions.md`.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: AGENTS.md, CLAUDE.md, and .github/copilot-instructions.md
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the same broken guidance filename in all three agent entrypoints, preventing commit rules from being discovered.
status: done 2026-08-25
archived: 2026-10-23
resolution: already resolved: Commit d0889739b33856e37a8cbbedbbb440940a43ab9b removed the absent hexalith-commit-instructions.md reference; references/Hexalith.AI.Tools/hexalith-llm-instructions.md:29-31 now names hexalith-git-instructions.md and all three entrypoints are byte-identical.

### DW-3: Make the Projects E2E dependency installation path suppress the recursive submodule postinstall command by construction.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: Projects E2E dependency installation
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Plain npm installation invoked `git submodule update --init --recursive --force`; only the inspected `CI=1` fallback prevented the forbidden mutation while current documentation still recommends an unsafe plain install.
status: done 2026-08-25
archived: 2026-10-23
resolution: already resolved: Commit f03a8d6deb28dbc60062e176ab5813114d5a0ca3 resolved the unsafe install path; tests/e2e/README.md:22-29 requires CI=1 npm ci --ignore-scripts and .github/workflows/ci.yml:143-149 uses it.

### DW-4: Constrain BMAD legacy cleanup targets to direct descendants of the resolved `_bmad` root.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD legacy cleanup classifier in both agent copies
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Adversarial and edge-case reviews found that absolute or parent-traversal module names can escape `_bmad` before `shutil.rmtree` is called in both agent copies.
status: done 2026-08-25
archived: 2026-10-23
resolution: resolved by sweep bundle dw-legacy-cleanup-path-safety
resolution-undo: fbc12d6bdcb9408d53250ac593b2b7c43d82af87fb6abe8e4c7808ddf8728dcb 2026-08-25 7374617475733a206f70656e

### DW-5: Add isolated destructive-path tests for the BMAD legacy cleanup classifier and replacement-skill guards.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD legacy cleanup and replacement-skill test coverage
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Verification-gap review found no tests proving live config directories and sole installed skill copies are protected while only verified duplicates are removed.
status: done 2026-08-25
archived: 2026-10-23
resolution: resolved by sweep bundle dw-legacy-cleanup-path-safety
resolution-undo: fbc12d6bdcb9408d53250ac593b2b7c43d82af87fb6abe8e4c7808ddf8728dcb 2026-08-25 7374617475733a206f70656e

### DW-6: Validate `merge-help-csv.py` argument relationships before writing the target CSV.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: merge-help-csv.py
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Two independent reviewers found that `--legacy-dir` without `--module-code` reports an error only after shared configuration has already been mutated.
status: done 2026-08-25
archived: 2026-10-23
resolution: already resolved: Commit c2f60e9cf0db942a54cd2c130884aec8459288f2; .agents/skills/bmad-bmb-setup/scripts/merge-help-csv.py:358-366 rejects --legacy-dir without --module-code before publication at line 416.

### DW-7: Make `merge-config.py` validate both documents before committing coordinated configuration writes.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: merge-config.py
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found that shared config is written before user config is loaded and converted, allowing an error to leave a partially applied configuration update.
status: done 2026-08-25
archived: 2026-10-23
resolution: already resolved: Commit c2f60e9cf0db942a54cd2c130884aec8459288f2; .agents/skills/bmad-bmb-setup/scripts/merge-config.py:579-630 loads and serializes both prospective documents before the failure-atomic pair publisher runs.

### DW-8: Remove the workspace-specific absolute path from the checked-in Codex BMAD Loop hook commands.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: .codex/hooks.json
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the hook commands hard-code the current checkout path, so hooks fail when the repository is cloned or moved.
status: done 2026-08-25
archived: 2026-10-23
resolution: resolved by sweep bundle dw-portable-safe-loop-hooks
resolution-undo: f07edf5b0801aa33fcef57cec31a638619b49084e2868b360043d30665422827 2026-08-25 7374617475733a206f70656e

### DW-9: Validate BMAD Loop task IDs and event names before using them in event filenames.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Loop hook event filename handling
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found that separators or traversal segments from environment-derived identifiers can escape the events directory or make lifecycle hooks fail.
status: done 2026-08-25
archived: 2026-10-23
resolution: resolved by sweep bundle dw-portable-safe-loop-hooks
resolution-undo: f07edf5b0801aa33fcef57cec31a638619b49084e2868b360043d30665422827 2026-08-25 7374617475733a206f70656e

### DW-10: Render Quick Dev output into a validated temporary directory before replacing the previous workflow.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: Quick Dev workflow renderer
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the renderer deletes prior Markdown before reading and writing replacements, so a later failure can leave Quick Dev missing or partially rendered.
status: done 2026-08-25
archived: 2026-10-23
resolution: already resolved: Commit a0dea374b3b990a38e23357934817969ba4a03e4 replaced legacy Quick Dev rendering; _bmad/scripts/render_skill.py:295-319 renders to a sibling staging directory, atomically renames it, and cleans staging on every exit.

### DW-11: Add executable fixture coverage for Quick Dev renderer precedence, review-layer customization, placeholders, cleanup, and failure paths.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: Quick Dev renderer test coverage
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Verification-gap review found no test or normal gate invokes the new renderer, so syntactically valid but incomplete generated workflows can pass all current product checks.
status: done 2026-08-25
archived: 2026-10-23
resolution: resolved by sweep bundle dw-render-skill-fixture-coverage
resolution-undo: cd59c929b1db90412cd32cc4ca54c10fb3d7346e3c409daf3825a4ff991e43c4 2026-08-25 7374617475733a206f70656e

### DW-12: Add temporary-directory CLI tests for BMAD Loop hook event normalization and atomic delivery.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Loop hook CLI test coverage
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Verification-gap review found no test exercises absent or present environment variables, payload key variants, task attribution, or canonical event emission.
status: done 2026-08-25
archived: 2026-10-23
resolution: resolved by sweep bundle dw-portable-safe-loop-hooks
resolution-undo: f07edf5b0801aa33fcef57cec31a638619b49084e2868b360043d30665422827 2026-08-25 7374617475733a206f70656e

### DW-13: Make BMAD Dev Auto detect external HEAD, status, and path-set drift before reviewing or committing changes.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Dev Auto workflow
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found the unattended workflow derives and commits a baseline diff without ownership revalidation; the originating run directly experienced a concurrent root commit and submodule fast-forwards.
status: done 2026-08-25
archived: 2026-10-23
resolution: resolved by sweep bundle dw-build-auto-workspace-ownership
resolution-undo: 30119a05d487fd4a24797da98dce8f3d23409abcd886139546b8eb5d821df7a5 2026-08-25 7374617475733a206f70656e

### DW-14: Replace BMAD Dev Auto's unscoped revert instructions with isolated-worktree or owned-hunk reversal semantics.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: BMAD Dev Auto workflow
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Blind review found intent-gap and bad-spec loopbacks can erase concurrent edits because `revert code changes` has no path ownership or overlap guard.
status: done 2026-08-25
archived: 2026-10-23
resolution: resolved by sweep bundle dw-build-auto-workspace-ownership
resolution-undo: 30119a05d487fd4a24797da98dce8f3d23409abcd886139546b8eb5d821df7a5 2026-08-25 7374617475733a206f70656e

### DW-15: Add a Memories bounded-ledger test that refreshes an old workflow before trimming and then rejects its stale replay.

origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
location: Hexalith.Memories bounded workflow ledger tests
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
reason: Verification-gap review demonstrated that remove-and-reinsert watermark behavior can regress without any existing test failing once more than 256 workflow IDs are tracked.
status: done 2026-08-25
archived: 2026-10-23
resolution: already resolved: Commit 9af726a336aa604b051e2203438dc15ee728e9ce added references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterLogicTests.cs:164-190, which refreshes an old workflow at the 256-entry limit, trims, and rejects its stale replay.

### DW-16: Add a recurring managed live AppHost Playwright lane, including targeted AppHost startup smoke coverage, lifecycle ownership, and zero-live-skip enforcement.

origin: migrated from legacy ledger ("flat append from spec-5-12-live-apphost-operational-console-verification.md"), 2026-08-25
location: scheduled E2E workflow and AppHost Playwright lane
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
reason: Verification-gap review found the scheduled E2E job exercises only the offline lane and therefore cannot detect AppHost startup, discovery, authentication, or accidental live-test skips.
status: done 2026-08-27
archived: 2026-10-23
resolution: already resolved: Commit ae37c2341f026ddca29551156150193aba269a53; .github/workflows/ci.yml:13-14,145-203 defines the scheduled managed lane and teardown, tests/e2e/run-live-apphost.sh:31-63 owns lifecycle, and tests/e2e/reporters/zero-live-skip-reporter.ts:16-20 rejects empty or skipped live runs.

### DW-17: Implement and verify real browser OIDC authorization for projects-ui.

origin: migrated from legacy ledger ("flat append from spec-5-12-live-apphost-operational-console-verification.md"), 2026-08-25
location: projects-ui browser authentication
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
reason: Review found token state can target the discovered UI origin, but projects-ui has no browser OIDC or session assertion; the current real-Keycloak proof protects only the API access boundary.
status: done 2026-08-27
archived: 2026-10-23
resolution: already resolved: Commit ae37c2341f026ddca29551156150193aba269a53; src/Hexalith.Projects.UI/Program.cs:34-47,56-72 composes OIDC, authorization, token relay, and protected routes, while tests/e2e/specs/projects-authentication.spec.ts:5-40 proves the real Keycloak authorization-code challenge and HttpOnly session.

### DW-18: Provision deterministic projected-tenant and sibling reference, proposal, and UI-state fixtures with parallel-safe identifiers.

origin: migrated from legacy ledger ("flat append from spec-5-12-live-apphost-operational-console-verification.md"), 2026-08-25
location: AppHost-backed Projects E2E fixture provisioning
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
reason: The full AppHost-backed run exposed a missing tenant access projection plus fixed sibling IDs and placeholder states that the current test harness cannot establish independently.
status: done 2026-08-27
archived: 2026-10-23
resolution: already resolved: Commits 87bf82d597235b2663db385bfc8bc9d8b353478e and ae37c2341f026ddca29551156150193aba269a53; tests/e2e/support/factories/live-fixture-identities.ts:36-73 derives parallel-safe identities, tests/e2e/global-setup.ts:19-33 establishes tenant access, and src/Hexalith.Projects.AppHost/ProjectsLiveE2EFixtureProfile.cs:59-85 provisions profile-scoped sibling graphs.

### DW-19: Reconcile repository-required CRLF files with the default Git whitespace check.

origin: migrated from legacy ledger ("flat append from spec-prevent-submodule-skill-loading.md"), 2026-08-25
location: .editorconfig and Git whitespace configuration
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-prevent-submodule-skill-loading.md
reason: Review confirmed ordinary `git diff --check` flags carriage returns on newly added CRLF lines because no repository attribute or `core.whitespace=cr-at-eol` policy exists while `.editorconfig` requires CRLF.
status: done 2026-08-27
archived: 2026-10-23
resolution: already resolved: Commit fc359e96ba6dd83d8cfe9b2c0775bfcee1044443; .gitattributes:1 declares * whitespace=cr-at-eol, tests/tools/test_git_whitespace_policy.py:53-69 verifies acceptance of CRLF and rejection of real trailing whitespace, and .github/workflows/ci.yml:49-52 runs the gate.

### DW-20: Enforce the canonical ProjectMetadata displayName requirement without falling back to a legacy top-level name.

origin: migrated from legacy ledger ("flat append from spec-resolve-create-project-metadata-class-enforcement.md"), 2026-08-25
location: Create Project endpoint ProjectMetadata validation
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-resolve-create-project-metadata-class-enforcement.md
reason: Review confirmed the OpenAPI schema requires `projectMetadata.displayName`, but the pre-existing direct endpoint accepts a canonical object with a missing or blank displayName when a top-level name is supplied; this is outside the bounded metadataClass E-9 correction.
status: done 2026-08-27
archived: 2026-10-23
resolution: already resolved: Commit 97b7d2e5a0169d6c21279b5505dc36a717e19237; src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:596-610 rejects missing or blank canonical projectMetadata.displayName without legacy top-level fallback, with null/omitted/empty/whitespace coverage at tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs:109-145.

### DW-21: Align proposal-confirmation file-reference ID ordering between accepted HTTP requests and generated-client fingerprints.

origin: migrated from legacy ledger ("flat append from spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md"), 2026-08-25
location: proposal-confirmation endpoint and generated client
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md
reason: Review confirmed the pre-existing endpoint accepts unsorted `fileReferenceIds` after sorting them for validation and server hashing, while the generated helper hashes the caller's original array order.
status: done 2026-08-28
archived: 2026-10-23
resolution: already resolved: Commit 005c2d5d8fea513a8c15c0fb95d0da0fd1fcc5e8; src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs:225 ordinally orders FileReferenceIds before hashing, with parity coverage at tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs:729-751.

### DW-22: Align proposal-confirmation null fileReferenceIds semantics between the server and generated client.

origin: migrated from legacy ledger ("flat append from spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md"), 2026-08-25
location: proposal-confirmation endpoint and generated client
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md
reason: Review confirmed the pre-existing endpoint accepts null `fileReferenceIds` when no file references exist and hashes an empty array, while the generated helper hashes the null property as null.
status: done 2026-08-28
archived: 2026-10-23
resolution: already resolved: Commit 005c2d5d8fea513a8c15c0fb95d0da0fd1fcc5e8; src/Hexalith.Projects.Client/Generated/HexalithProjectsIdempotencyHelpers.g.cs:225 maps null FileReferenceIds to Array.Empty<string>(), with null/empty parity coverage at tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs:366-387.

### DW-23: Restore monotonic sprint tracker chronology after the pre-existing last_updated timestamp regression.

origin: migrated from legacy ledger ("flat append from spec-canonicalize-reference-health-freshness-vocabulary.md"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-canonicalize-reference-health-freshness-vocabulary.md
reason: Review confirmed the sprint tracker records July 31 action completions while its pre-existing dirty `last_updated` value moves backward to July 19, which may cause incremental consumers to miss updates.
status: done 2026-08-25
archived: 2026-10-23
resolution: already resolved: Commit 729798ab09cbff27223e06c019a1532865713da1 advanced sprint chronology; current _bmad-output/implementation-artifacts/sprint-status.yaml:2 records last_updated 2026-08-03T08:21:35+02:00, later than the July 31 completions.

### DW-24: Reconcile the U+2028/U+2029 sprint action promised by its approved July 31 duplicate-trigger closure proposal.

origin: migrated from legacy ledger ("flat append from spec-canonicalize-reference-health-freshness-vocabulary.md"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-canonicalize-reference-health-freshness-vocabulary.md
reason: Review confirmed the pre-existing approved proposal states that the action was moved to done while the sprint tracker still leaves the matching action in-progress; the originating correction intentionally changed only the freshness-vocabulary action.
status: done 2026-08-25
archived: 2026-10-23
resolution: already resolved: Commit 729798ab09cbff27223e06c019a1532865713da1 moved the U+2028/U+2029 action to done; current _bmad-output/implementation-artifacts/sprint-status.yaml:203-209 records done with fresh green evidence.

### DW-25: Preserve caller cancellation through Web warning/dashboard list and diagnostic requests.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: ProjectWarningsDashboardSource
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed `ProjectWarningsDashboardSource` catches `OperationCanceledException` through its general exception handlers, returning safe feedback or a synthetic unavailable row instead of propagating requested cancellation; this production behavior predates the coverage-only change.
status: done 2026-08-28
archived: 2026-10-23
resolution: already resolved: Commit 8b60ab6fcd5d9fd371dd711717f71b1362c720f7; src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardSource.cs:41-43 and :86-88 propagate requested cancellation, with coverage at tests/Hexalith.Projects.UI.Tests/Diagnostics/ProjectWarningsDashboardSourceTests.cs:164-219.

### DW-26: Make the Web diagnostic-unavailable tile drill-in select only rows represented by its count.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: Web diagnostic-unavailable tile filter
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed the tile count tracks diagnostic failures while its existing state-only filter selects every `ReferenceState.Unavailable` row, including ordinary unavailable references; changing filter semantics was outside the coverage-only task.
status: done 2026-08-28
archived: 2026-10-23
resolution: already resolved: Commit 8b60ab6fcd5d9fd371dd711717f71b1362c720f7; src/Hexalith.Projects.UI/Components/Pages/Home.razor:390-393 applies the diagnostic-only drill-in predicate defined at src/Hexalith.Projects.UI/Diagnostics/ProjectWarningsDashboardMapper.cs:63-66, with coverage at tests/Hexalith.Projects.UI.Tests/Components/ProjectInventoryPageTests.cs:189-220.

### DW-27: Define and align warning diagnostic scan cardinality across Web, MCP, and CLI.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: Web, MCP, and CLI warning scans
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed Web enriches every returned visible Project while MCP and CLI scan at most 25 Projects, and MCP also uses query `Take` to bound the scanned set; inventories larger than the bound can therefore produce surface-specific unavailable counts.
status: done 2026-09-05
archived: 2026-10-23
resolution: resolved by sweep bundle dw-warning-scan-consistency
resolution-undo: 2b0d6b83ce84e10cffba780e9b5354af635d22acc510feca65e3a0dd91cadb97 2026-09-05 7374617475733a206f70656e
decision: 2026-08-25 Shared 25-project window — Define one deterministic 25-project diagnostic window shared by Web, MCP, and CLI, keep inventory totals separate from scanned-warning totals, decouple MCP output Take from scan scope, and add inventories-over-25 parity tests.

### DW-28: Define MCP warning-queue output when diagnostics fail but no healthy warning row is emitted.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: MCP warning resource
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed the existing MCP warning resource carries `DiagnosticUnavailable` only on emitted warning rows, so a nonzero count is unobservable from an empty warning queue; the approved spec explicitly reserved no-row semantics for a separate contract decision.
status: done 2026-09-05
archived: 2026-10-23
resolution: resolved by sweep bundle dw-warning-scan-consistency
resolution-undo: 2b0d6b83ce84e10cffba780e9b5354af635d22acc510feca65e3a0dd91cadb97 2026-09-05 7374617475733a206f70656e
decision: 2026-08-25 Add summary resource — Add a dedicated MCP warning-scan summary DTO and resource that always emits scan cardinality and DiagnosticUnavailable while leaving warning rows unchanged, then update dispatch, documentation, and contract tests.

### DW-29: Build the MCP operational dashboard from one visible-inventory snapshot.

origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
location: MCP operational dashboard
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
reason: Review confirmed the existing dashboard reads inventory once for lifecycle totals and again inside warning scanning, so concurrent inventory changes can yield counters derived from different snapshots; production scan restructuring was outside the coverage-only task.
status: done 2026-09-05
archived: 2026-10-23
resolution: resolved by sweep bundle dw-warning-scan-consistency
resolution-undo: 2b0d6b83ce84e10cffba780e9b5354af635d22acc510feca65e3a0dd91cadb97 2026-09-05 7374617475733a206f70656e

### DW-30: Add the mandatory 6.1-P1R prerequisite to the Story 6.4 implementation artifact.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
location: Story 6.4 implementation artifact
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
reason: Review confirmed the pre-existing Story 6.4 `blocked_by` metadata and entry-condition prose omit P1R even though the current Epic 6 dependency context requires accepted P1R for later consumers; Story 6.4 was outside the candidate-baseline patch.
status: done 2026-09-05
archived: 2026-10-23
resolution: resolved by sweep bundle dw-story-6-4-prerequisite-truth
resolution-undo: 9de8bb3b016b3f891a16d3ddf291689d8b5209123894ba2af7a022516c412931 2026-09-05 7374617475733a206f70656e

### DW-31: Replace Story 6.4's stale current EventStore 3.86.0 observations with the truthful unaccepted 3.88.0 candidate state.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
location: Story 6.4 implementation artifact
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
reason: Review confirmed the pre-existing Story 6.4 artifact still calls 3.86.0 the current central pin after the source, catalog, and committed runner candidate moved to 3.88.0; Story 6.4 was outside the candidate-baseline patch.
status: done 2026-09-05
archived: 2026-10-23
resolution: resolved by sweep bundle dw-story-6-4-prerequisite-truth
resolution-undo: 9de8bb3b016b3f891a16d3ddf291689d8b5209123894ba2af7a022516c412931 2026-09-05 7374617475733a206f70656e
decision: 2026-08-28 Modernize current observation — Replace Story 6.4's 3.86.0 claim with the exact unaccepted 3.97.0 catalog and stale 3.90.0 runner split, retain the 3.70.1 rollback baseline, and preserve the story's blocked state.
decision: 2026-08-28 Modernize current observation — Replace Story 6.4's 3.86.0 claim with the exact unaccepted 3.97.0 catalog and stale 3.90.0 runner split, retain the 3.70.1 rollback baseline, and preserve the story's blocked state.

### DW-32: Bind canonical readiness artifacts to the actual module-manifest byte hash.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
location: canonical readiness fixture and module manifest
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
reason: Review confirmed the pre-existing readiness fixture uses a synthetic all-`A` `manifestHash` even though production run evidence computes SHA-256 from the manifest file, allowing the fixture to validate without proving the manifest identity it cites.
status: done 2026-09-05
archived: 2026-10-23
resolution: resolved by sweep bundle dw-readiness-evidence-identity
resolution-undo: 0e50a15ad24c573f405846987442bab6fba21795f2c13dcc294bdc7ab5f71411 2026-09-05 7374617475733a206f70656e

### DW-33: Bind passing readiness rows to normalized filter, fixture, manifest, and hash identities in module-run evidence.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
location: ArtifactBindsToRow readiness evidence validation
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
reason: Review confirmed the pre-existing `ArtifactBindsToRow` check compares only the module subcommand and profile, so a row declaring a filter and fixture can pass with an artifact that records neither; broadening the evidence contract was outside the pin-revalidation patch.
status: done 2026-09-05
archived: 2026-10-23
resolution: resolved by sweep bundle dw-readiness-evidence-identity
resolution-undo: 0e50a15ad24c573f405846987442bab6fba21795f2c13dcc294bdc7ab5f71411 2026-09-05 7374617475733a206f70656e

### DW-37: A symlink cycle nested inside an already-validated cleanup target can make find_skill_dirs and count_files raise an unhandled RecursionError instead of the documented JSON exit-2 contract.
origin: spec-deferred 6e82e0d0ef7e
location: .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:find_skill_dirs,count_files
source_spec: `spec-legacy-cleanup-path-safety.md`
severity: medium
reason: Both functions catch only (OSError, RuntimeError) around their Path.rglob() calls. rglob recurses into symlinked subdirectories, so a self-referential symlink inside a target's subtree (not the top-level target itself, which is already rejected by the direct-child symlink guard in resolve_cleanup_targets) can trigger unbounded recursion. This traversal behavior predates this diff -- both functions used the same rglob pattern before this change, with no handling for this case either.
status: done 2026-08-27
archived: 2026-10-23
resolution: already resolved: .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:196 and :422 use Path.rglob, which does not follow symlinked directories under the current Python runtime; a focused nested self-cycle probe returned normally with empty results.

### DW-38: Follow-up review still recommended for dw-legacy-cleanup-path-safety after the damping cap was spent
origin: review-budget-followup
location: n/a
source_spec: `spec-legacy-cleanup-path-safety.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260825-081747-f4a1; this entry preserves the lingering recommendation for a deliberate later review.
status: done 2026-10-23
archived: 2026-10-23
decision: 2026-08-27 Run bounded follow-up — Run exactly one independent review over the synchronized cleanup scripts and tests, patch only newly substantiated in-scope defects, and preserve DW-36 as separately tracked work.
resolution: spec-five-deferred-bmad-integrity-items.md; Exactly one dedicated independent follow-up identified four vendor-update regressions. Restored direct-child target validation, live migration protection, mandatory safe external SKILL.md replacements, and guarded JSON/exit-2 inspection failures across all six scripts. Restored eleven safety tests alongside eleven existing quarantine/CI/synchronization tests and added low-level filesystem-error preservation controls; final canonical suite passed 23/23. Explicit stat/lstat and scandir operations prevent Python predicate/traversal error suppression from bypassing guards. Current quarantine transaction behavior is preserved; DW-36 remains separately open.


### DW-39: Existing-generation verification follows symlinked output files, so matching external bytes can satisfy an immutable snapshot check.
origin: spec-deferred bfaafa1248ab
location: _bmad/scripts/render_skill.py:279
source_spec: `spec-render-skill-fixture-coverage.md`
severity: high
reason: `_verify_existing` builds its file set with `Path.is_file()` and hashes outputs with `Path.read_bytes()`; both follow symlinks. A symlink to external bytes matching the manifest therefore verifies successfully, while the external target can change after verification without changing the generation directory or manifest.
status: done 2026-10-23
archived: 2026-10-23
resolution: Reject unlinked-directory violations, symlinks and nonregular entries before reading an existing snapshot. Hermetic CLI cases cover linked outputs, manifest, nested directory, whole generation, dangling links and FIFO while preserving links and external bytes. Evidence: spec-dw-39-43-renderer-snapshot-hardening.md; tests/tools/test_render_skill.py (17/17 passing, no skips).

### DW-40: The fixture suite's `assert_manifest_integrity` helper re-derives root_hash, generation_hash, and project_slug with its own copy of the renderer's hashing/slugging formulas instead of importing the re
origin: spec-deferred c023fdd6d1ee
location: tests/tools/test_render_skill.py:316-358
source_spec: `spec-render-skill-fixture-coverage.md`
severity: medium
reason: `tests/tools/test_render_skill.py` recomputes `_sha256(str(project_root).encode())[:12]` and `_sha256(_canonical_json(inputs))[:20]` inline rather than calling into `render_skill.py` (already loadable via the file's own `_load_renderer_module()` helper). A regression in the real hashing/slugging algorithm would be mirrored by the test's parallel implementation and pass silently.
status: done 2026-10-23
archived: 2026-10-23
resolution: Extract shared root-hash, project-slug and generation-hash helpers used by render() and assert_manifest_integrity(). Retain independent byte hashes and add literal fixed vectors for path identity, slug fallback/truncation and Unicode canonical input. Evidence: spec-dw-39-43-renderer-snapshot-hardening.md; tests/tools/test_render_skill.py (17/17 passing, no skips).

### DW-41: The keyed review layer's `when` clause is only ever asserted absent from rendered output, never asserted present for a layer whose `when` clause survives to the final resolution.
origin: spec-deferred f63e08052e13
location: _bmad/scripts/render_skill.py:171-172
source_spec: `spec-render-skill-fixture-coverage.md`
severity: low
reason: `test_layer_precedence_lists_and_keyed_review_replacement` only exercises the negative case (`assertNotIn("Run only when: always", rendered)`) for a layer whose `when` field is dropped by a whole-table override. No fixture keeps a `when` clause on a surviving layer through to final render, so a regression in the `Run only when: {value}` formatting at `render_skill.py:172` would go undetected.
status: done 2026-10-23
archived: 2026-10-23
resolution: Add a surviving review-layer condition fixture asserting the exact rendered Run only when line and the resolved manifest value; retain whole-table replacement coverage that removes a condition. Evidence: spec-dw-39-43-renderer-snapshot-hardening.md; tests/tools/test_render_skill.py (17/17 passing, no skips).

### DW-42: The `customization.workflow.open_spec` field's special-cased empty-string allowance has no fixture coverage.
origin: spec-deferred 8b5c84060986
location: _bmad/scripts/render_skill.py:180
source_spec: `spec-render-skill-fixture-coverage.md`
severity: low
reason: `_resolve_customization_value` at `render_skill.py:180` hardcodes `label == "customization.workflow.open_spec"` as an exception permitting an empty override even when the default is non-empty. No fixture defines an `open_spec` customization key, so this named carve-out ships with zero coverage.
status: done 2026-10-23
archived: 2026-10-23
resolution: Add a nonempty-default open_spec fixture with an empty user override, immutable prior generation and empty manifest value, plus a control that rejects an empty override for another required string. Evidence: spec-dw-39-43-renderer-snapshot-hardening.md; tests/tools/test_render_skill.py (17/17 passing, no skips).

### DW-43: No fixture demonstrates the existing render-source escape guard, so a regression there would ship undetected.
origin: spec-deferred e5621d164d42
location: _bmad/scripts/render_skill.py:105-106
source_spec: `spec-render-skill-fixture-coverage.md`
severity: low
reason: `_load_sources` at `_bmad/scripts/render_skill.py:105-106` resolves each candidate source and raises `RenderError(f"render source escapes skill directory: {name}")` when the resolved path is not relative to `skill_dir` (guarding against a symlinked or otherwise escaping source file). `tests/tools/test_render_skill.py` has no fixture that creates such a source and asserts this HALT message, so a regression that weakens or removes the check would pass the current suite silently.
status: done 2026-10-23
archived: 2026-10-23
resolution: Add a CLI source-escape fixture proving external Markdown symlinks halt without publication or target mutation, plus an internal-source-link positive control. Evidence: spec-dw-39-43-renderer-snapshot-hardening.md; tests/tools/test_render_skill.py (17/17 passing, no skips).

### DW-44: Introduce an admission state (responseState, components, recoveryActions) for the Conversation-start read.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects/Context/ProjectContextInclusionPolicy.cs
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
reason: ProjectContextInclusionPolicy.Assemble hardcodes Assembled on the success path and the wire DTO deliberately omits AssemblyOutcome, so a Chatbot receiving 200 has no signal on which to withhold a first response. 200 means "authorized", not "usable".
status: done 2026-09-05
archived: 2026-10-23
resolution: already resolved: Commit d3a115df3c7243204ce9fe76757d39937761477f; src/Hexalith.Projects.Contracts/Queries/ConversationStartSetupResponse.cs:14-46 defines ResponseState, Components, and RecoveryActions, and src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:60-65,86-100 computes admission state and omits setup when unavailable.
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-45: Bind the Conversation-start snapshot to an authorized projectVersion.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
reason: ProjectDetailItem carries a Sequence watermark but the DTO explicitly excludes it, enforced by GetConversationStartSetup_BodyDoesNotContainAuditMetadata. Nothing ties the setup a Chatbot admits on to a Project version, so a concurrent archive or setup update is undetectable by the caller.
status: done 2026-09-05
archived: 2026-10-23
resolution: already resolved: Commit d3a115df3c7243204ce9fe76757d39937761477f; src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:86-90 binds ProjectVersion to detail.Sequence, with version and persisted AsOf assertions at tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupQueryHandlerTests.cs:46-51.
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-46: Evaluate exactly-one-Folder eligibility on the Conversation-start read.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:126
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
reason: The handler constructs ProjectContextReferenceEvidence with ProjectFolder null and empty collections while authorization.ProjectDetail already carries ProjectFolderReference. A Project with no Folder, an archived Folder, or an ambiguous Folder produces the same admissible-looking 200.
status: done 2026-09-05
archived: 2026-10-23
resolution: already resolved: Commit d3a115df3c7243204ce9fe76757d39937761477f; src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:55-65 requires the singular ProjectDetailItem.ProjectFolder to be Included and returns Unavailable without setup otherwise, covered at tests/Hexalith.Projects.Server.Tests/Queries/GetConversationStartSetupQueryHandlerTests.cs:66-76.
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth

### DW-61: Enforce the declared bounded-collection limits on the conversation-start response.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: The spine constrains goals and userInstructions to 16 items with per-item length 1 to 512, and the source-kind arrays to 4 items, but ConversationStartSetup copies whatever the projection holds with no truncation or validation, so the server can emit a body violating its own published schema. Story 3.5 deviation L3 accepted this on the stated grounds that the lengths are bounded upstream in ProjectSetup validation; that premise is false, as no count or length check on Goals or UserInstructions exists anywhere in src/. Deferred to Story 6.7 because enforcement changes legacy response behavior for already-stored oversized setups.
status: done 2026-08-26
archived: 2026-10-23
resolution: already resolved: src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs:41-43 and :850-893 enforce the exact 16-item/512-character and 4-source-kind bounds declared at src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:3917-3946; git blame attributes the checks to pre-origin commits a12ca891 and 88be25e2.
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-65: Define the accepted serialization grammar for readiness verification commands before broadening identity binding beyond the current canonical token form.
origin: spec-deferred cf4fbe1482fb
location: references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Evidence/ReadinessEvidenceValidator.cs:1161
source_spec: `spec-readiness-evidence-identity.md`
reason: Review could not establish whether readiness rows are required to use the current space-delimited `--manifest`, `--profile`, and `--filter` form or may use equals forms, quoted/escaped values, shell wrappers, and trailing operators. An authoritative producer/consumer contract plus representative accepted commands would settle the question.
status: done 2026-09-06
archived: 2026-10-23
resolution: already resolved: Commit d0049833f2ac0416075eed37f173712f3ac90e56; references/Hexalith.Builds/README.md:288-298 defines the accepted canonical space-delimited readiness command grammar, references/Hexalith.Builds/test/Hexalith.Builds.Evidence.Tests/ReadinessEvidenceValidatorTests.cs:158-202 rejects noncanonical identity forms, and references/Hexalith.Builds/test/Hexalith.Builds.Module.Tests/ModuleRunEvidenceSerializationTests.cs:205-247 pins producer serialization.
