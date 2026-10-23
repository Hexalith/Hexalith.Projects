# Deferred Work

### DW-1: Deliver and accept Story 6.5 supported-read prerequisites

origin: bmad-loop-resolve for spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md, 2026-08-25
location: _bmad-output/implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md:31
severity: critical
reason: Story 6.5 must not dispatch until its prerequisite record pins accepted Story 6.1–6.4 supported contracts, read models, projections, handlers, identity inputs, and executable G-4 profiles. Close this entry only after every artifact required by the frozen prerequisite gate exists and is accepted; legacy REST and planning-only artifacts never satisfy the gate.
status: open
reopened: 2026-10-06 The 2026-09-06 closure was orchestrator bookkeeping, recorded as not proof by the Story 6.5 spec on 2026-09-06 (`8bc91b2`). `prerequisite_record` remains null and Stories 6.1 and 6.4 are blocked, so the closure condition is unmet. Approved by sprint-change-proposal-2026-10-06-epic-6-tracking-reconciliation.md.
gate: 6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface

### DW-2: Correct the Hexalith.AI.Tools agent entrypoints to reference `hexalith-git-instructions.md` instead of the absent `hexalith-commit-instructions.md`.

status: done 2026-08-25
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-3: Make the Projects E2E dependency installation path suppress the recursive submodule postinstall command by construction.

status: done 2026-08-25
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-4: Constrain BMAD legacy cleanup targets to direct descendants of the resolved `_bmad` root.

status: done 2026-08-25
resolution: resolved by sweep bundle dw-legacy-cleanup-path-safety
resolution-undo: fbc12d6bdcb9408d53250ac593b2b7c43d82af87fb6abe8e4c7808ddf8728dcb 2026-08-25 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-5: Add isolated destructive-path tests for the BMAD legacy cleanup classifier and replacement-skill guards.

status: done 2026-08-25
resolution: resolved by sweep bundle dw-legacy-cleanup-path-safety
resolution-undo: fbc12d6bdcb9408d53250ac593b2b7c43d82af87fb6abe8e4c7808ddf8728dcb 2026-08-25 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-6: Validate `merge-help-csv.py` argument relationships before writing the target CSV.

status: done 2026-08-25
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-7: Make `merge-config.py` validate both documents before committing coordinated configuration writes.

status: done 2026-08-25
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-8: Remove the workspace-specific absolute path from the checked-in Codex BMAD Loop hook commands.

status: done 2026-08-25
resolution: resolved by sweep bundle dw-portable-safe-loop-hooks
resolution-undo: f07edf5b0801aa33fcef57cec31a638619b49084e2868b360043d30665422827 2026-08-25 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-9: Validate BMAD Loop task IDs and event names before using them in event filenames.

status: done 2026-08-25
resolution: resolved by sweep bundle dw-portable-safe-loop-hooks
resolution-undo: f07edf5b0801aa33fcef57cec31a638619b49084e2868b360043d30665422827 2026-08-25 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-10: Render Quick Dev output into a validated temporary directory before replacing the previous workflow.

status: done 2026-08-25
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-11: Add executable fixture coverage for Quick Dev renderer precedence, review-layer customization, placeholders, cleanup, and failure paths.

status: done 2026-08-25
resolution: resolved by sweep bundle dw-render-skill-fixture-coverage
resolution-undo: cd59c929b1db90412cd32cc4ca54c10fb3d7346e3c409daf3825a4ff991e43c4 2026-08-25 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-12: Add temporary-directory CLI tests for BMAD Loop hook event normalization and atomic delivery.

status: done 2026-08-25
resolution: resolved by sweep bundle dw-portable-safe-loop-hooks
resolution-undo: f07edf5b0801aa33fcef57cec31a638619b49084e2868b360043d30665422827 2026-08-25 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-13: Make BMAD Dev Auto detect external HEAD, status, and path-set drift before reviewing or committing changes.

status: done 2026-08-25
resolution: resolved by sweep bundle dw-build-auto-workspace-ownership
resolution-undo: 30119a05d487fd4a24797da98dce8f3d23409abcd886139546b8eb5d821df7a5 2026-08-25 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-14: Replace BMAD Dev Auto's unscoped revert instructions with isolated-worktree or owned-hunk reversal semantics.

status: done 2026-08-25
resolution: resolved by sweep bundle dw-build-auto-workspace-ownership
resolution-undo: 30119a05d487fd4a24797da98dce8f3d23409abcd886139546b8eb5d821df7a5 2026-08-25 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-15: Add a Memories bounded-ledger test that refreshes an old workflow before trimming and then rejects its stale replay.

status: done 2026-08-25
origin: migrated from legacy ledger ("flat append from spec-fix-all-test-failures.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md
archived: 2026-10-23

### DW-16: Add a recurring managed live AppHost Playwright lane, including targeted AppHost startup smoke coverage, lifecycle ownership, and zero-live-skip enforcement.

status: done 2026-08-27
origin: migrated from legacy ledger ("flat append from spec-5-12-live-apphost-operational-console-verification.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
archived: 2026-10-23

### DW-17: Implement and verify real browser OIDC authorization for projects-ui.

status: done 2026-08-27
origin: migrated from legacy ledger ("flat append from spec-5-12-live-apphost-operational-console-verification.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
archived: 2026-10-23

### DW-18: Provision deterministic projected-tenant and sibling reference, proposal, and UI-state fixtures with parallel-safe identifiers.

status: done 2026-08-27
origin: migrated from legacy ledger ("flat append from spec-5-12-live-apphost-operational-console-verification.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md
archived: 2026-10-23

### DW-19: Reconcile repository-required CRLF files with the default Git whitespace check.

status: done 2026-08-27
origin: migrated from legacy ledger ("flat append from spec-prevent-submodule-skill-loading.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-prevent-submodule-skill-loading.md
archived: 2026-10-23

### DW-20: Enforce the canonical ProjectMetadata displayName requirement without falling back to a legacy top-level name.

status: done 2026-08-27
origin: migrated from legacy ledger ("flat append from spec-resolve-create-project-metadata-class-enforcement.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-resolve-create-project-metadata-class-enforcement.md
archived: 2026-10-23

### DW-21: Align proposal-confirmation file-reference ID ordering between accepted HTTP requests and generated-client fingerprints.

status: done 2026-08-28
origin: migrated from legacy ledger ("flat append from spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md
archived: 2026-10-23

### DW-22: Align proposal-confirmation null fileReferenceIds semantics between the server and generated client.

status: done 2026-08-28
origin: migrated from legacy ledger ("flat append from spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-u2028-u2029-idempotency-canonicalizer-parity-coverage.md
archived: 2026-10-23

### DW-23: Restore monotonic sprint tracker chronology after the pre-existing last_updated timestamp regression.

status: done 2026-08-25
origin: migrated from legacy ledger ("flat append from spec-canonicalize-reference-health-freshness-vocabulary.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-canonicalize-reference-health-freshness-vocabulary.md
archived: 2026-10-23

### DW-24: Reconcile the U+2028/U+2029 sprint action promised by its approved July 31 duplicate-trigger closure proposal.

status: done 2026-08-25
origin: migrated from legacy ledger ("flat append from spec-canonicalize-reference-health-freshness-vocabulary.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-canonicalize-reference-health-freshness-vocabulary.md
archived: 2026-10-23

### DW-25: Preserve caller cancellation through Web warning/dashboard list and diagnostic requests.

status: done 2026-08-28
origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
archived: 2026-10-23

### DW-26: Make the Web diagnostic-unavailable tile drill-in select only rows represented by its count.

status: done 2026-08-28
origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
archived: 2026-10-23

### DW-27: Define and align warning diagnostic scan cardinality across Web, MCP, and CLI.

status: done 2026-09-05
resolution: resolved by sweep bundle dw-warning-scan-consistency
resolution-undo: 2b0d6b83ce84e10cffba780e9b5354af635d22acc510feca65e3a0dd91cadb97 2026-09-05 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
archived: 2026-10-23

### DW-28: Define MCP warning-queue output when diagnostics fail but no healthy warning row is emitted.

status: done 2026-09-05
resolution: resolved by sweep bundle dw-warning-scan-consistency
resolution-undo: 2b0d6b83ce84e10cffba780e9b5354af635d22acc510feca65e3a0dd91cadb97 2026-09-05 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
archived: 2026-10-23

### DW-29: Build the MCP operational dashboard from one visible-inventory snapshot.

status: done 2026-09-05
resolution: resolved by sweep bundle dw-warning-scan-consistency
resolution-undo: 2b0d6b83ce84e10cffba780e9b5354af635d22acc510feca65e3a0dd91cadb97 2026-09-05 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-partial-failure-diagnosticunavailable-parity-coverage.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-partial-failure-diagnosticunavailable-parity-coverage.md
archived: 2026-10-23

### DW-30: Add the mandatory 6.1-P1R prerequisite to the Story 6.4 implementation artifact.

status: done 2026-09-05
resolution: resolved by sweep bundle dw-story-6-4-prerequisite-truth
resolution-undo: 9de8bb3b016b3f891a16d3ddf291689d8b5209123894ba2af7a022516c412931 2026-09-05 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
archived: 2026-10-23

### DW-31: Replace Story 6.4's stale current EventStore 3.86.0 observations with the truthful unaccepted 3.88.0 candidate state.

status: done 2026-09-05
resolution: resolved by sweep bundle dw-story-6-4-prerequisite-truth
resolution-undo: 9de8bb3b016b3f891a16d3ddf291689d8b5209123894ba2af7a022516c412931 2026-09-05 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
archived: 2026-10-23

### DW-32: Bind canonical readiness artifacts to the actual module-manifest byte hash.

status: done 2026-09-05
resolution: resolved by sweep bundle dw-readiness-evidence-identity
resolution-undo: 0e50a15ad24c573f405846987442bab6fba21795f2c13dcc294bdc7ab5f71411 2026-09-05 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
archived: 2026-10-23

### DW-33: Bind passing readiness rows to normalized filter, fixture, manifest, and hash identities in module-run evidence.

status: done 2026-09-05
resolution: resolved by sweep bundle dw-readiness-evidence-identity
resolution-undo: 0e50a15ad24c573f405846987442bab6fba21795f2c13dcc294bdc7ab5f71411 2026-09-05 7374617475733a206f70656e
origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline.md"), 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md
archived: 2026-10-23

### DW-34: Propagate the verified EventStore 3.90.0 candidate and exact qualifying Builds revision through the Projects sprint ledger, Epic 6 context, epics, and readiness matrix views.

origin: migrated from legacy ledger ("flat append from spec-6-1-p1r-revalidate-platform-baseline-2.md"), 2026-08-25
location: Projects sprint ledger, Epic 6 context, epics, and readiness matrix views
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline-2.md
reason: These root planning edits form a separately reviewable repository change and depend on the exact qualifying Builds revision produced by the narrowed Builds alignment task.
status: open

### DW-35: Accept the selected P1R baseline through the minimal gate.

origin: Epic 6 production-readiness correction
location: Projects sprint ledger and fixed P1R acceptance record
source_spec: _bmad-output/implementation-artifacts/spec-6-1-p1r-minimal-acceptance-gate.md
reason: Jérôme Piquot explicitly accepted the exact 3.110.0 / Builds 4.29.1 tuple in all four roles; current runtime qualification remains unavailable while G-6 is stale.
status: done
resolution: 2026-10-01 Rebound the fixed four-role record to the exact 3.110.0 / Builds 4.29.1 tuple accepted by Jérôme Piquot at 2026-10-01T06:17:01Z; existing done status retained.
historical_resolution: 2026-09-22 acceptance for 3.106.0 / Builds ad52f350a2f0bc47849179ae17b4594dafff5363 preserved at _bmad-output/implementation-artifacts/evidence/6-1-p1r-acceptance-20260922-3.106.0.json
acceptance_record: _bmad-output/implementation-artifacts/6-1-p1r-acceptance.json
selected: EventStore 3.110.0 / v3.110.0 / 27279fe6431925a6ea046c3f89af61487185c7de; Builds 4.29.1 / 21ce044ab465ccb2adab58b3d66e394ffbecf3c2
rollback: EventStore 3.70.1 / v3.70.1 / f13f9925fdca53efa2ab8c90d396ab106f91bb9c; Builds 7af20f8bafbfe561df6f7705913a0800603090b5
current_candidate: EventStore 3.110.0 / v3.110.0 / 27279fe6431925a6ea046c3f89af61487185c7de; Builds 4.29.1 / 21ce044ab465ccb2adab58b3d66e394ffbecf3c2
current_candidate_packet: _bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md
current_candidate_usable: false
current_decisions: Accept by Jérôme Piquot as EventStore Owner, Builds Owner, Solution Architect (including EventStore Stack rebinding), and Test Architect at 2026-10-01T06:17:01Z; each binds the fixed record and #/selected. G-6 is not accepted by this decision.

### DW-36: Sequential shutil.rmtree calls across multiple validated targets are not atomic, so a later target's filesystem failure can leave earlier targets already deleted.
origin: spec-deferred 8d336adc2d02
location: .agents/skills/bmad-bmb-setup/scripts/cleanup-legacy.py:cleanup_directories
source_spec: `spec-legacy-cleanup-path-safety.md`
severity: medium
reason: cleanup_directories() in all six cleanup-legacy.py copies loops over validated targets and calls shutil.rmtree per target inside its own try/except; if an OSError/RuntimeError is raised on target N after targets 1..N-1 already succeeded, main() exits via runtime_error (exit 2) but the already-removed directories are not restored. This sequential, non-atomic deletion behavior is unchanged from the pre-diff implementation -- the diff only changed which exception types are caught and how the failure is reported.
status: open

### DW-37: A symlink cycle nested inside an already-validated cleanup target can make find_skill_dirs and count_files raise an unhandled RecursionError instead of the documented JSON exit-2 contract.

status: done 2026-08-27
origin: spec-deferred 6e82e0d0ef7e
source_spec: `spec-legacy-cleanup-path-safety.md`
severity: medium
archived: 2026-10-23

### DW-38: Follow-up review still recommended for dw-legacy-cleanup-path-safety after the damping cap was spent

status: done 2026-10-23
origin: review-budget-followup
source_spec: `spec-legacy-cleanup-path-safety.md`
severity: low
archived: 2026-10-23

### DW-39: Existing-generation verification follows symlinked output files, so matching external bytes can satisfy an immutable snapshot check.

status: done 2026-10-23
origin: spec-deferred bfaafa1248ab
source_spec: `spec-render-skill-fixture-coverage.md`
severity: high
archived: 2026-10-23

### DW-40: The fixture suite's `assert_manifest_integrity` helper re-derives root_hash, generation_hash, and project_slug with its own copy of the renderer's hashing/slugging formulas instead of importing the re

status: done 2026-10-23
origin: spec-deferred c023fdd6d1ee
source_spec: `spec-render-skill-fixture-coverage.md`
severity: medium
archived: 2026-10-23

### DW-41: The keyed review layer's `when` clause is only ever asserted absent from rendered output, never asserted present for a layer whose `when` clause survives to the final resolution.

status: done 2026-10-23
origin: spec-deferred f63e08052e13
source_spec: `spec-render-skill-fixture-coverage.md`
severity: low
archived: 2026-10-23

### DW-42: The `customization.workflow.open_spec` field's special-cased empty-string allowance has no fixture coverage.

status: done 2026-10-23
origin: spec-deferred 8b5c84060986
source_spec: `spec-render-skill-fixture-coverage.md`
severity: low
archived: 2026-10-23

### DW-43: No fixture demonstrates the existing render-source escape guard, so a regression there would ship undetected.

status: done 2026-10-23
origin: spec-deferred e5621d164d42
source_spec: `spec-render-skill-fixture-coverage.md`
severity: low
archived: 2026-10-23

### DW-44: Introduce an admission state (responseState, components, recoveryActions) for the Conversation-start read.

status: done 2026-09-05
origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth
archived: 2026-10-23

### DW-45: Bind the Conversation-start snapshot to an authorized projectVersion.

status: done 2026-09-05
origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth
archived: 2026-10-23

### DW-46: Evaluate exactly-one-Folder eligibility on the Conversation-start read.

status: done 2026-09-05
origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-25
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
gate: 6-2-retrieve-conversation-start-setup-with-admission-truth
archived: 2026-10-23

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
decision: 2026-08-27 Mitigate and test — Design a bounded safe-denial indistinguishability strategy, implement it at the shared authorization boundary, and add stable timing-distribution tests without exposing protected outcomes.
decision: 2026-08-27 Mitigate and test — Design a bounded safe-denial indistinguishability strategy, implement it at the shared authorization boundary, and add stable timing-distribution tests without exposing protected outcomes.
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
decision: 2026-08-26 Add versioned normalized contract — Introduce an additive corrected supported contract, use camelCase for its enum wire names, feed real QueryEnvelope authority into its tenant guard, regenerate clients and fingerprints, and record release metadata while preserving v1.
decision: 2026-08-26 Break v1 in place — Normalize current v1 wire enums and guards, bump contract metadata, regenerate every client, and provide explicit migration evidence for consumers.

### DW-55: Create the standalone Hexalith.Projects.UI.Contracts split story and land it before Stories 6.6, 8.4 and 8.5.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj:22-33
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: high
reason: AC6 was removed from Story 6.2 on 2026-08-26 because it is packaging hygiene with no runtime admission consequence and Story 6.2 already carries multiple-goals and oversized warnings. The coupling is structural rather than file placement: src/Hexalith.Projects.Contracts/Ui/ProjectContextFreshness.cs:10,25,29 decorates a domain/wire enum with [ProjectionBadge] from Hexalith.FrontComposer.Contracts.Attributes, so the split requires stripping UI attributes off domain enums and re-expressing badge mapping in the UI layer. Acceptance gate already exists at tests/tools/run-package-dependency-gate.ps1. Must land before Stories 6.6, 8.4 and 8.5 or the CLI and MCP surfaces inherit Blazor/Fluxor transitively through the supported contracts. Story creation requires tools/planning/validate_production_authority.py --story-id and a sprint-status.yaml entry; neither may be performed by Story 6.2.
status: open
decision: 2026-08-26 Create and execute split story — Run the planning authority gate, create and schedule the standalone story, introduce the non-packable UI.Contracts boundary, strip UI attributes from domain and wire enums, re-home descriptor mapping, and update the package dependency gate before Stories 6.6, 8.4, and 8.5.
decision: 2026-08-26 Create and execute split story — Run the planning authority gate, create and schedule the standalone story, introduce the non-packable UI.Contracts boundary, strip UI attributes from domain and wire enums, re-home descriptor mapping, and update the package dependency gate before Stories 6.6, 8.4, and 8.5.

### DW-56: Give the supported Conversation-start response structural equality over its bounded collections.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Contracts/Models/ConversationStartSetup.cs
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: ConversationStartSetup.FromContext aliases the caller-owned collections held by ProjectContext.Setup rather than copying them, so the bounded subset is a live view of caller state when the source is a mutable list. The obvious fix was attempted during the 2026-08-26 review and reverted: ConversationStartSetup is a record in a packable assembly and record equality compares IReadOnlyList<T> members by reference, so defensive copying makes two projections of the same context unequal and breaks Project_IsPureFunction_SameInputProducesSameOutput. Copying therefore changes public DTO equality semantics for every consumer and must not be done to the legacy type in isolation. Resolve it on the Story 6.2 supported response wrapper by using a collection type with structural equality (for example ImmutableArray<T> with an explicit equality contract) from the start, then align the legacy type at the Story 6.7 cutover.
status: open
decision: 2026-09-06 Dedicated supported value — Keep the legacy DTO unchanged and introduce a supported setup value that defensively copies bounded collections and explicitly implements structural equality and hashing, then update the supported response, serialization, and contract tests.
decision: 2026-09-06 Dedicated supported value — Keep the legacy DTO unchanged and introduce a supported setup value that defensively copies bounded collections and explicitly implements structural equality and hashing, then update the supported response, serialization, and contract tests.
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
decision: 2026-08-26 Declare safe 404 only — Remove unreachable 401 and 403 responses from this operation, document the safe-denial status set, regenerate clients and fingerprints, and preserve gateway authentication handling separately.
decision: 2026-08-26 Declare safe 404 only — Remove unreachable 401 and 403 responses from this operation, document the safe-denial status set, regenerate clients and fingerprints, and preserve gateway authentication handling separately.
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

status: done 2026-08-26
origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback
archived: 2026-10-23

### DW-62: Add cache directives to the tenant-sensitive conversation-start read.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md, 2026-08-26
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupEndpoint.cs:145
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth.md
severity: medium
reason: goals and userInstructions are tagged x-hexalith-sensitive-metadata-tier tenant_sensitive, yet the response is emitted with no Cache-Control no-store and no Vary handling. This is the fast-path endpoint called at every conversation start, so it is the one intermediaries are most likely to cache. Deferred to Story 6.7 because it changes legacy response headers and therefore the shadow baseline.
status: open
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-63: Migrate the Projects Server hand-rolled `/process`, `/project`, and `/query` endpoints onto the SDK's `AddEventStoreDomainService()`/`UseEventStoreDomainService()` host.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth-2.md, 2026-09-05
location: src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:212-321
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth-2.md
severity: medium
reason: hexalith-state-instructions.md mandates a two-line host (AddEventStoreDomainService/UseEventStoreDomainService) that already maps /process, /replay-state, /query, /project, and more, with route-validation and duplicate-registration guards Hexalith.Projects.Server does not get by hand-rolling its own MapPost calls. This predates Story 6.2 (/process and /project were already hand-rolled); Story 6.2 added a hand-rolled /query following the same existing local pattern rather than introducing a new deviation. Deferred because migrating requires re-wiring the whole endpoint surface, not a Story-6.2-scoped fix.
status: open
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-64: Concurrent Story 6.2 review, deferred-work ledger, and submodule updates were committed while the warning-scan bundle was running.
origin: spec-deferred 026631391ace
location: Repository change set after baseline 2e096e4cb13fc121c6fb03a4b2e8960270bdc9b2
source_spec: `spec-warning-scan-consistency-2.md`
severity: medium
reason: The warning-scan run began from clean baseline 2e096e4cb13fc121c6fb03a4b2e8960270bdc9b2. Commits f4509ef1d0bfe045b0733d8ef2906d3e32eb44eb and 152ca393bb7128d0ff1e5fe54531624af8534506 appeared on main and origin/main during implementation and review; they contain the unrelated Story 6.2 artifact, deferred-work ledger entry, and EventStore/Folders/Parties submodule pointer updates. The warning-scan implementation session did not author those edits, and separating already-published concurrent work requires its owner or orchestrator.
status: open

### DW-65: Define the accepted serialization grammar for readiness verification commands before broadening identity binding beyond the current canonical token form.

status: done 2026-09-06
origin: spec-deferred cf4fbe1482fb
source_spec: `spec-readiness-evidence-identity.md`
archived: 2026-10-23

- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-g-6-runtime-toolchain-baseline.md`
  summary: Remove the duplicate DW-56 decision line introduced by concurrent ledger work.
  evidence: The same dated decision appears twice consecutively; this is unrelated to G-6 and should be corrected by the ledger-owning work.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-g-6-runtime-toolchain-baseline.md`
  summary: Constrain legacy cleanup targets to safe descendants of the selected `_bmad` directory.
  evidence: Absolute and parent-traversal directory names are joined without containment validation, so concurrent cleanup code can relocate and delete paths outside `_bmad`.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-g-6-runtime-toolchain-baseline.md`
  summary: Make legacy cleanup recovery durable across abrupt process termination and partial final deletion.
  evidence: Staging has no persistent original-to-staged journal, and a partially failed `rmtree` reports remaining paths without proving byte-complete recovery.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-g-6-runtime-toolchain-baseline.md`
  summary: Run the failure-atomic legacy cleanup suite in a blocking root CI job.
  evidence: The seven cleanup tests pass locally but no workflow invokes them, so rollback regressions can merge unnoticed.

### DW-66: Implement the `Partial` branch of the AD-32 admission matrix for Conversation-start setup.

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth-2.md, 2026-09-05
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:389-391
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth-2.md
severity: medium
reason: ConversationStartResponseState.Partial is declared and is a required scenario in the spec's own I/O matrix ("Required evidence current; optional values explicitly omitted" -> Partial), but responseState is a binary hasFolder ? Complete : Unavailable -- no branch ever yields Partial. Needs a design decision on what counts as "optional" evidence distinct from the required Project/Folder/Setup set before this can be implemented; deferred to resolve alongside the cutover/shadow-comparison work.
status: open
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-67: Distinguish Folder `ReferenceState` outcomes instead of collapsing them to one generic "Unavailable".

origin: bmad-code-review of spec-6-2-retrieve-conversation-start-setup-with-admission-truth-2.md, 2026-09-05
location: src/Hexalith.Projects.Server/Queries/GetConversationStartSetupQueryHandler.cs:385
source_spec: /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-2-retrieve-conversation-start-setup-with-admission-truth-2.md
severity: medium
reason: hasFolder = detail.ProjectFolder?.ReferenceState == ReferenceState.Included treats Pending, Excluded, Unauthorized, Unavailable, Stale, Archived, Ambiguous, TenantMismatch, Conflict, and InvalidReference identically. An authorization-denied folder reference (Unauthorized, fail-closed) receives the same RefreshContext/ContactAdministrator recovery guidance as a folder simply never linked, masking a genuine access denial as a transient/recoverable condition. Deferred: same admission-matrix design work as DW-66 (Partial); resolve together in Story 6.7.
status: open
gate: 6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback

### DW-68: Keep the P1R coordinate qualification open until the minimal record is accepted.

origin: Epic 6 P1R coordinate qualification
location: Projects sprint ledger and fixed P1R acceptance record
source_spec: _bmad-output/implementation-artifacts/spec-6-1-p1r-minimal-acceptance-gate.md
reason: Jérôme Piquot explicitly accepted the exact 3.110.0 / Builds 4.29.1 tuple in all four roles; current runtime qualification remains unavailable while G-6 is stale.
status: done
resolution: 2026-10-01 Rebound the fixed four-role record to the exact 3.110.0 / Builds 4.29.1 tuple accepted by Jérôme Piquot at 2026-10-01T06:17:01Z; existing done status retained.
historical_resolution: 2026-09-22 acceptance for 3.106.0 / Builds ad52f350a2f0bc47849179ae17b4594dafff5363 preserved at _bmad-output/implementation-artifacts/evidence/6-1-p1r-acceptance-20260922-3.106.0.json
acceptance_record: _bmad-output/implementation-artifacts/6-1-p1r-acceptance.json
selected: EventStore 3.110.0 / v3.110.0 / 27279fe6431925a6ea046c3f89af61487185c7de; Builds 4.29.1 / 21ce044ab465ccb2adab58b3d66e394ffbecf3c2
rollback: EventStore 3.70.1 / v3.70.1 / f13f9925fdca53efa2ab8c90d396ab106f91bb9c; Builds 7af20f8bafbfe561df6f7705913a0800603090b5
current_candidate: EventStore 3.110.0 / v3.110.0 / 27279fe6431925a6ea046c3f89af61487185c7de; Builds 4.29.1 / 21ce044ab465ccb2adab58b3d66e394ffbecf3c2
current_candidate_packet: _bmad-output/implementation-artifacts/6-1-p1r-current-exact-baseline-candidate.md
current_candidate_usable: false
current_decisions: Accept by Jérôme Piquot as EventStore Owner, Builds Owner, Solution Architect (including EventStore Stack rebinding), and Test Architect at 2026-10-01T06:17:01Z; each binds the fixed record and #/selected. G-6 is not accepted by this decision.

### DW-69: Reconcile Story 6.5 status across its spec and sprint tracker.

origin: restored independent obligation from the pre-minimal P1R ledger
location: Story 6.5 specification and sprint-status.yaml
source_spec: _bmad-output/implementation-artifacts/spec-6-1-p1r-requalify-with-sibling-materialization.md
reason: The Story 6.5 spec records `blocked` while the sprint tracker records `backlog`, so queue consumers do not receive one authoritative state.
status: done
resolution: 2026-10-06 Approved sprint-change-proposal-2026-10-06-epic-6-tracking-reconciliation.md set the sprint tracker and production-authority guard to `blocked` for Story 6.5, matching its specification; no dispatch, readiness or prerequisite state changed.

### DW-70: Make retained EventStore 3.97 qualification logs durable.

origin: restored independent obligation from the pre-minimal P1R ledger
location: qualification-evidence/6-1-p1r-397-20260824*/logs
source_spec: _bmad-output/implementation-artifacts/spec-6-1-p1r-requalify-with-sibling-materialization.md
reason: The three preserved 3.97 evidence roots contain untracked log files that remain vulnerable to workspace cleaning; this obligation concerns only preserved historical evidence and does not authorize another 3.106 attempt.
status: open

### DW-71: Verify retained qualification manifests against tracked artifacts.

origin: restored independent obligation from the pre-minimal P1R ledger
location: qualification-evidence manifest validation
source_spec: _bmad-output/implementation-artifacts/spec-6-1-p1r-requalify-with-sibling-materialization.md
reason: No executable repository check re-hashes each retained `artifact-manifest.sha256` entry and confirms that every referenced artifact is tracked.
status: open

### DW-72: Define the command-ledger policy for retained qualification evidence.

origin: restored independent obligation from the pre-minimal P1R ledger
location: qualification-evidence governance
source_spec: _bmad-output/implementation-artifacts/spec-6-1-p1r-requalify-with-sibling-materialization.md
reason: The Test Architect still must decide when retained evidence requires a machine-readable mapping from commands to exits, timestamps, and logs; no historical ledger should be reconstructed or used to revive the removed 3.106 attempt flow.
status: open

### DW-73: Define package retention and LFS policy for qualification evidence.

origin: restored independent obligation from the pre-minimal P1R ledger
location: qualification-evidence binary packages
source_spec: _bmad-output/implementation-artifacts/spec-6-1-p1r-requalify-with-sibling-materialization.md
reason: Preserved `.nupkg` and `.snupkg` artifacts have no bounded retention, pruning, or Git LFS policy, so historical evidence growth remains unbounded.
status: open

- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-bmad-config-merge-atomicity.md`
  summary: Allowlist live `answers.core` keys the same way legacy import allowlists `_CORE_KEYS`.
  evidence: `merge_config` still writes every non-user core answer onto the shared root, so a crafted answers file can overwrite another module section. This is preserved vendor success behavior, not an atomicity regression.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-bmad-integrity-items.md; Live answers are validated against the established core key set before publication, and shared-core merging also filters that set. Across all nine script copies, unknown/module-shaped keys return exit 1 with byte/mode preservation of both outputs and legacy inputs.

- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-bmad-config-merge-atomicity.md`
  summary: Reconcile help-CSV `HEADER` (`after`/`before`) with production `preceded-by`/`followed-by` columns.
  evidence: `render_csv` keeps the target header and appends source rows, so a real asset merge can produce mixed column shapes. The vendor document shape was preserved by this story.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-bmad-integrity-items.md; All nine help merger copies normalize legacy aliases to the production header, align reordered rows by column name before identity/filtering, and reject missing, duplicate, alias-colliding, unknown or malformed schemas before publication and cleanup. The blocking merge suite covers both schema generations and preservation controls.

- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-bmad-config-merge-atomicity.md`
  summary: Copy root `user_name` / `communication_language` into `config.user.yaml` when they are stripped from `config.yaml`.
  evidence: `extract_user_settings` only reads answers, so an existing shared file can lose those values on a successful pair publish. Same vendor behavior as before this story.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-bmad-integrity-items.md; Existing shared root/nested core user preferences migrate through coordinated pair publication. Explicit answers override existing user config, which overrides legacy/shared fallbacks; unrelated user settings survive. All nine copies have absent/existing-user, explicit-answer and failed-user-publication rollback controls.

- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-ci-cd-2.md`
  summary: Harden manual release publication and exact GitHub/NuGet post-publication verification.
  evidence: Split at human request because release planning, frozen/no-op routing, duplicate preflight, strict publication, and public verification are independently shippable after package-only CI and candidate validation; exact-green-source gating keeps release blocked meanwhile.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-fix-ci-cd-2.md`
  summary: Narrow the BMad spec and sprint-planning persistent-context overrides to the Projects root context file.
  evidence: The current `**/project-context.md` glob also matches eight sibling contexts under `references/`, which can inject contradictory planning facts; review policy defers fixes to agent-context configuration.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-bmad-integrity-items.md; Both overrides now select only file:{project-root}/_bmad-output/project-context.md. A real renderer fixture uses each checked-in override with synthetic root and sibling contexts and proves that only the root context is selected.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-ci-cd-run-release-verify-nuget-publication.md`
  summary: Make the Folders generated redaction contract distinguish an omitted member from the explicit `Not_redacted` value in source-reference builds.
  evidence: Both the current source client and official `Hexalith.Folders.Client` `1.0.0` package generate a non-nullable enum whose zero value is `Not_redacted`, so omitted JSON defaults to an accepted value and presence cannot be recovered after deserialization. An explicit `redacted` wire value fails generated-client deserialization and the Projects adapter denies it; the remaining omission ambiguity requires an owning Folders contract change.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-migrate-nuget-trusted-publishing.md`
  summary: Audit the pre-existing EventStore, Folders, and FrontComposer gitlink update and repair the Folders source-reference Debug compile break.
  evidence: Baseline commit `ffa922d0f7a278da3bb02ee09769564df02d5f01` advanced all three gitlinks before this implementation began; the current Folders client exposes a four-argument cancellation overload while `FoldersProjectFolderDirectory` still calls it with five arguments, producing CS1501 in source-reference Debug builds.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-3-retrieve-assembled-project-context-through-supported-read-models.md`
  summary: Hermetically execute the release exact-green-source preflight across its accepted and rejected source states.
  evidence: The release workflow has source-text checks but no execution coverage for a green SHA, stale main, malformed API data, or missing successful push CI; this is a real release-verification gap unrelated to Story 6.3.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-verification-items.md; The real workflow shell now has green and stale-main controls plus isolated run SHA, branch, event, completion, conclusion, missing/malformed API data, wrong dispatch ref and API-failure cases.

## Deferred from: code review of spec-6-3-retrieve-assembled-project-context-through-supported-read-models.md (2026-09-20)

- Folders file-reference linking now sends `PathPolicyClass.Metadata_only` instead of `tenant_sensitive_document`. This is maybe-false: production harm exists only if Folders treats `Metadata_only` as a weaker ACL class than the previous value. Settle it against the Folders path-policy contract before keeping or reverting `src/Hexalith.Projects.Server/Folders/FoldersProjectFileReferenceDirectory.cs:62`.
- source_spec: `/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/spec-6-3-retrieve-assembled-project-context-through-supported-read-models.md`
  summary: Require an all-absent package inventory before unrelated NuGet `--skip-duplicate` release publication.
  evidence: The release workflow can invoke Semantic Release while its NuGet command tolerates duplicate packages, so a partially occupied version can yield a mixed or incomplete public release; this release-hardening defect predates Story 6.3.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-minimal-acceptance-gate.md`
  summary: Document all companion-path and workspace-root arguments required to validate an atomic P1R candidate set in the repository agent context.
  evidence: The current `_bmad-output/project-context.md` candidate command names only `--sprint-status`, while accepted closure spans sprint status, deferred work, the P0 artifact, and the acceptance record under a candidate workspace root; review policy defers agent-context edits.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-verification-items.md; The project context names all four candidate arguments and the fixed workspace-relative acceptance-record path; the documented explicit command passes the real production-authority validator without changing index or acceptance state.
- source_spec: `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`
  summary: Add a CI job that runs the G-4 live composition lane (HEXALITH_G4_LIVE=1) with Docker and a verified Dapr 1.18.0/1.18.2 home.
  evidence: No workflow sets HEXALITH_G4_LIVE or HEXALITH_DAPR_HOME; ci.yml runs the IntegrationTests project where every live test reports skipped (Stage 3 review R15).
- source_spec: `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`
  summary: Make runner cleanup proof cross-platform (process enumeration beyond /proc, graceful AppHost stop on Windows).
  evidence: CompositionRunResourceScanner.FindProcesses returns [] off Linux and DownAsync sends no graceful stop on Windows (Stage 3 review R16).
- source_spec: `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`
  summary: Surface AppHost-side failures (cutover fault, blocked stdin listener, unbounded readiness probes) as fast, specific diagnostics instead of a generic HXR021 timeout.
  evidence: AppHostRunner/StopSignal/RunReadiness/WriterProtocolCutover loop or block without per-probe bounds; outcome stays fail-closed (Stage 3 review R17).
- source_spec: `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`
  summary: Verify RunTopology endpoint handling for consumer module projects whose launch profiles expose only https (unverified, would be medium).
  evidence: Settle by composing a module whose launchSettings declares only an https endpoint and checking RunEndpoints.TryGet (Stage 3 review R18).
- source_spec: `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`
  summary: Replace the stale "candidate" XML doc wording on SupportedPlatformPins.EventStoreVersion at the next G-6 re-seal.
  evidence: The file is G-6-bound source state; editing it now would invalidate the accepted packet (Stage 3 review R19).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Fix the Folders malformed-success tests so they reach the changed fallback under the published `Hexalith.Folders.Client` 1.0.0; `ValidateLink_MalformedSuccess_IsUnavailable` currently fails in CI package mode.
  evidence: Commit `6bc5155` (not P1R); under `CI=true` a 200 `{}` body deserializes and `Evaluate` returns Denied, while the folder test passes through `ValidateLifecycle` instead of `MapFoldersStatus` (review VG-01/BH-28/EC-20).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Restrict the Folders `MapFoldersStatus` Unavailable fallback to unexpected 2xx and restore a deliberate fail-closed default for unlisted 3xx/4xx statuses.
  evidence: Commit `6bc5155` changed `_ => Denied` to `_ => Unavailable` in both adapters, so 405/410/412/429 and folder-side 413/422 become retryable (review BH-27/EC-08/EC-09/VG-07).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Stop the accepted-only G-6 step from masking the FrontComposer inspect and OpenAPI fingerprint gates and the scheduled E2E lane in Projects CI.
  evidence: `project-gates` runs the G-6 validator first and it exits 1 while the packet is pending (already true for the historical packet at baseline), so later gates never run (review BH-19/EC-04/VG-06).
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-verification-items.md; The independent CI layout is retained. Real PowerShell gate fixtures reject an inserted accepted-only check, coupled G-6 dependencies on project/E2E lanes and reversal of G-6 independence. Explicit dependency guards prevent regression.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Record approval provenance for the 2026-09-27 "Approved McpCli course correction" notes in the Spine, PRD, and epics, and reconcile Spine line 342 with AD-29 (unverified, would be medium).
  evidence: Commit `b42beb3` added the notes but no `sprint-change-proposal-2026-09-27*` exists; settle by locating the approval record and comparing AD-29 with line 342 (review BH-29/EC-24).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Confirm whether `prepare-oq8-v5-candidate.py` reports `workingTreeDirty=false` while reading ROOT selector inputs from a dirty worktree (unverified, would be medium).
  evidence: Story 4.15 helper change swept into EventStore `1cc6b44b`; settle by running with `--for-v5-activation` against a dirty selector and inspecting the output (review EC-18).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Add a Conversations check that the AppHost restores the catalog-pinned `CommunityToolkit.Aspire.Hosting.Dapr` version.
  evidence: Removing the direct reference added in Conversations `62ddb9e` falls back to `.757` with no failing test (review VG-03).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Bind the G-6 limitation files (source closure, AppHost builds, Toolkit resolution, pin audit, control-plane restoration) into the packet hash.
  evidence: The validator's fixed `EXPECTED_ARTIFACT_KINDS` leaves them unbound, so owner acceptance of a packet hash does not cover them (review R2-BH-01).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Let the G-6 command contract record a real managed restart smoke instead of requiring the credential preflight to exit 1.
  evidence: `REQUIRED_COMMAND_EXIT_CODES` forces exit 1, so the 2026-09-29 run unset `TEST_USER_PASSWORD` to comply (review R2-BH-03b).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Record EventStore container image digests, per-platform manifests and signatures in the P1R package evidence.
  evidence: Neither the 3.108.1 nor the 3.109.0 evidence verifies `registry.hexalith.com/eventstore` images (review R2-BH-05).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Extend G-6 catalog and pin audit to Dapr actor/AspNetCore packages, `Aspire.Hosting` and `HexalithEventStoreVersion`.
  evidence: Only `Dapr.Client`/`Dapr.Workflow` and four other packages are checked; the P1R coordinate itself can drift unnoticed (review R2-BH-09).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Align or explicitly exempt the Projects `release.yml` Builds execution SHA from the root Builds gitlink.
  evidence: `release.yml` runs Builds `a07078ad…` while the gitlink and G-6 bindings use another revision (review R2-BH-11/R2-EC-04).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Add a release containment rule for Projects `main` while it builds against an unaccepted EventStore tuple, and name the rollback pin target.
  evidence: Package builds consume EventStore 3.109.0 while the accepted P1R tuple is 3.106.0; semantic-release could publish from that state (review R2-BH-12).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Refresh the Story 6.1 draft spec's Design Notes, which still call 3.108.1 the current Builds baseline.
  evidence: `spec-6-1-list-and-open-projects-through-supported-authenticated-paths-2.md` Design Notes predate the 3.109.0 retarget (review R2-EC-17).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Archive the EventStore 3.109.0 release bypass log excerpts, failing tag-CI test output, and `release-evidence-36238526310-1` `publication-identity.json` before GitHub retention expires (about 2026-12-25).
  evidence: The bypass and release-time Builds claims are only replayable from Actions logs and artifacts (review R3-BH-07).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Decide whether the 14-package `verify_public_packages.py` replay runs automatically before the P1R decision.
  evidence: No CI caller exists; the lightweight evidence job lacks `dotnet nuget verify` and the archive downloads (review R3-BH-08/R3-VG-04).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Let the G-6 command contract accept a clean package-exception inventory and check the reason for any required exit-1 preflight.
  evidence: `REQUIRED_COMMAND_EXIT_CODES` requires the inventory to exit 1, so fixing its 14 drifts would invalidate a packet (review R3-BH-10).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Compile the APIs changed in 3.109.0 (`CommandStatusQueryResponse.EventCount`/`TenantId`, `Contracts.Effects`, `IAggregateActor.ProcessTrustedEffectAsync`) in the EventStore P1R consumer smoke.
  evidence: The consumer compiles only types unchanged since 3.106.0 (review R3-BH-17).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Record the test-binary source commit in the EventStore OQ8 capture artifacts, and state when `shippedReleaseEntryAssemblies` means checkout-built rather than published binaries.
  evidence: The hash-bound `eventstore/observations.json` omits the source commit, so the bound packet alone can be misread as evidence for the published release (review R4-BH-09).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Require a named G-6 decision record (reviewed hash, allowlisted approver, owner roles, date on or after capture) whenever the packet status is `accepted`, and define its `sprint-status.yaml` keys and follow-up fields.
  evidence: A bare status flip turns both G-6 jobs green today; the validator has never checked a decision record (review R5-BH-01/02, R5-EC-10).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Audit and bind the Builds `Github/dapr-init/action.yml` defaults, whose runtime falls back to 1.18.0 when callers omit `runtime-version`.
  evidence: The action is outside the 2026-09-29 `pinAudit` and source-state (review R5-BH-05).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: Make the Builds execution-SHA check follow nested Builds action refs, or narrow its message to direct `ci.yml` references.
  evidence: `domain-ci.yml` pins `dapr-init@410bd595…`, which is neither the gitlink nor bound; latent while byte-identical (review R5-BH-06).
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-ci-qualification-items.md; The historical candidate gate, PowerShell gate and CI wrapper now explicitly limit their Builds execution-SHA diagnostics to direct workflow references; a mismatched direct immutable SHA is rejected by the executable fixture. No transitive identity claim is made.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: At the next G-6 recapture, tighten `test_g6_packet_references.py` (exact replaced-hash and gitlink matches, fixtures, an accepted-status case) and add expected-repository-set and argument assertions to the G-6 gate tests.
  evidence: These packet-bound files accept loose matches and never exercise the accepted branch (review R5-BH-09/11, R5-VG-02).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-3108-evidence-and-qualification.md`
  summary: At the next G-6 recapture, build the Builds self-test catalog fixture from a test-owned package table and add a drift control per catalog package.
  evidence: Removing entries from `CATALOG_TUPLE_FIELDS` leaves the self-test green and lets a `daprDotnetPackages` 1.18.8 baseline validate (review R5-VG-01).
- source_spec: `_bmad-output/implementation-artifacts/spec-update-eventstore-package-version.md`
  summary: The Builds package audit still has a duplicate xunit consumer, so an incremental refresh of one family fails closed.
  evidence: `package-version-audit.json` lists `P0Fixture.NativeTests.VsTest.csproj` twice under family `xunit` (6 rows, 5 unique). `audit-central-package-versions.ps1 -Family hexalith-eventstore` stopped on that prior-family check. This pin used a complete refresh instead. The duplicate predates the EventStore `3.110.0` pin.
- source_spec: `_bmad-output/implementation-artifacts/spec-update-eventstore-package-version.md`
  summary: Projects package validation still fails because ServiceDefaults packs OpenTelemetry 1.19.1 while `tools/release-packages.json` says 1.19.0.
  evidence: `python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test` and `scripts/validate-nuget-packages.py` exit 1 with `wrong_versions` for `OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, and `OpenTelemetry.Extensions.Hosting`. The four `Hexalith.EventStore.Contracts` nuspecs are `3.110.0`. The OpenTelemetry mismatch was already in the catalog.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths-3.md`
  summary: Prove shadow equivalence of supported list/open against legacy GET.
  evidence: Split from the Story 6.1 spec because the 2142-token draft mixed the read with its legacy comparison harness. Routing cutover stays in Story 6.7; this slice keeps the equivalence proof.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths-3.md`
  summary: Inspect a quarantined folderless Project on the Tenant-role open path.
  evidence: Split from the Story 6.1 spec because operator inspection of Safe Metadata, the Project name, and any pre-v1 Folder or creation receipt is a separate edge from authorization-filtered list/open. Chatbot still omits these Projects.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths-4.md`
  summary: Limit Chatbot list and open to Projects whose Folder that actor can currently read.
  evidence: Split from the Story 6.1 spec because Chatbot Folder-read visibility, Unavailable non-current Folder rows, and hiding pre-activation work are a separate disclosure goal from the supported Safe Metadata list/open path.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths-4.md`
  summary: Return the Project name on Tenant-role list and open only when inspection is authorized, and write no FR-21 event.
  evidence: Split from the Story 6.1 spec because inspection-gated name disclosure is a separate goal from the supported Safe Metadata list/open path. Story 8.1 records that inspection.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p3-approve-production-identity-authentication-contract.md`
  summary: Unverified medium concern: determine whether a reachable additional-identity producer can feed unauthenticated authority claims into the pre-existing Projects tenant-context accessor.
  evidence: Resumed Blind B4 found principal-wide accessor reads while the P3 transformer isolates its authenticated identity; the current bearer host produces a single identity. A concrete secondary-identity producer or explicit accessor contract is needed to settle the HTTP-path claim. No platform or accessor policy was changed.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p3-approve-production-identity-authentication-contract.md`
  summary: Unverified medium concern: bind the eventual production deployment graph to managed identity configuration instead of the local AppHost UI fixture credential.
  evidence: Resumed Blind B7 identified the explicitly local E2E credential in the Development AppHost, but no production deployment graph or secret-binding record was available. Inspect the owner-supplied production graph to establish whether that fixture could reach deployment. P3 local verification does not authorize deployment or select an identity provider.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-current-baseline-acceptance.md`
  summary: Pre-existing medium guard consistency gap: optional current usability flags and accepted revalidation metadata can drift while the minimal acceptance/index guard still passes.
  evidence: Disposable fixtures flipping p1r_current_revalidation.usable_as_prerequisite and both current_candidate_usable fields to true, or changing the optional candidate to 9.0.0, returned (6, 7, 8). The authoritative fixed record/gate remain strict and the actual transition preserves false flags. Add a separate consistency policy that still allows pending future candidates to differ from historical acceptance.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-current-baseline-acceptance.md`
  summary: Pre-existing medium authentication issue: a validly signed raw numeric sub is normalized to a string subject and passes the current callback.
  evidence: The raw-JSON scratch reproduction signed sub=42 with RS256; JsonWebTokenHandler accepted it as string 42 and OnTokenValidated reported no failure. RFC 7519 section 4.1.2 defines a string subject (https://www.rfc-editor.org/rfc/rfc7519#section-4.1.2). Authentication files are concurrent P3 work untouched by P1R; add raw-type rejection and middleware fixtures there.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-authentication-items.md; Verified existing HasStringSubject guard and Production/Staging signed middleware rejection; the fixture now asserts that the signed payload sub is a JSON number, independently of identity aliases. No new authentication policy or production acceptance is claimed.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-current-baseline-acceptance.md`
  summary: Pre-existing medium coverage gap: verify bearer forwarding through the registered outbound gateway pipeline.
  evidence: Both forwarding controls construct EventStoreGatewayTokenForwardingHandler directly, so removal of AddHttpMessageHandler registration in ProjectsServerServiceCollectionExtensions.cs would escape those tests. Exercise the actual registered client in the owner authentication verification.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-authentication-items.md; RegisteredGateway_RealQueryControllerPreservesDualPrincipalClaims now invokes the production-registered IEventStoreGatewayClient from an authenticated HTTP request through the real downstream QueriesController. The destination alone is redirected; the fixture asserts exact forwarded bearer bytes plus actor, Tenant, workload, delegation, scope/scp and audience for all four combinations.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-current-baseline-acceptance.md`
  summary: Pre-existing medium coverage gap: use a signed tenant-b token against existing tenant-a Project data with authorized controls in both tenants.
  evidence: ProtectedRead_CrossTenantHint_ReturnsSameSafeDenial changes only a hint while its token remains tenant-a. Separate principal-binding controls use constructed principals, leaving the complete JWT-to-tenant read composition without this negative control.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-authentication-items.md; ProtectedRead_SignedForeignTenant_ReturnsSameSafeDenial proves each signed Tenant principal reads its own seeded Project, then compares the foreign existing Project with a nonexistent Project. Production and Staging cover requests without a hint and with a conflicting tenant-a hint; both return identical redacted safe-404 bodies.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-current-baseline-acceptance.md`
  summary: Pre-existing medium coverage gap: reject a signed middleware token with no expiration.
  evidence: CreateToken always sets Expires; no no-exp middleware fixture was found. RequireExpirationTime currently protects the host, but disabling it would not be detected by the existing expired-token control.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-authentication-items.md; Production/Staging InvalidCredentials controls create signed tokens with automatic timestamp insertion disabled and assert exp is absent. Real authentication returns an empty Bearer 401 and the protected Project read returns safe 404 after a valid-token control.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-current-baseline-acceptance.md`
  summary: Unverified medium concern: bind the concurrent no-build Aspire health observation to retained command output and executed AppHost/service binary hashes.
  evidence: The P3 manifest records summary results, source hashes, and test-assembly hashes without the launch/describe streams or AppHost/service hashes. No wrong runtime bytes were demonstrated. Retain those outputs, launch metadata, and matching binary hashes to settle the binding; P1R does not grant runtime qualification.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-current-baseline-acceptance.md`
  summary: Pre-existing medium CI issue: update or retire the obsolete 3.109.0 P1R candidate replay and its matching CI invariants.
  evidence: test_p1r_candidate_evidence.py documents retirement on changed checkout src or a superseding accepted record. The source had already drifted at the initial 6dededde EventStore gitlink, and P1R now accepts 3.110.0. The old CI job and exact-job invariants remain active; it is historical evidence, not current qualification.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-current-baseline-acceptance.md`
  summary: Pre-existing medium verification gap: isolate duplicate-sub rejection from conflicting-alias rejection in the signed middleware fixture.
  evidence: The multiple-sub fixture supplies NameIdentifier=actor-a and first sub=actor-b, so choosing the first subject would still reject on the alias rule and pass the fixture. Add multiple subjects with no conflicting alias and assert authentication failure plus safe protected-read denial in the concurrent P3 suite.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-authentication-items.md; Production/Staging InvalidSubjectIdentity controls now include multiple-sub,false, which supplies no name-identifier alias. Real middleware rejects authentication and protected Project reads return safe denial independently of the alias rule.


- source_spec: `_bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-failures.md`
  summary: Low owner-packet documentation drift: append a dated clarification that the Projects P1R CI lane now includes the selected EventStore 3.110.0 replay.
  evidence: `6-1-p1r-current-exact-baseline-candidate.md:40` says the lane still fetches only v3.109.0, while `.github/workflows/ci.yml`, its exact policy fixture, and the 16-test selected replay now include v3.110.0. Preserve the historical owner decision and remaining rollback/runtime limitations; this correction grants no new acceptance.

- source_spec: `_bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-failures.md`
  summary: Medium pinned-package parity blocker: qualify a Folders client that implements the current metadata v2 route before changing the dependency pin.
  evidence: The Release Server run against pinned Folders.Client 1.0.0 has 807 passing tests and one failing required v2 metadata-route assertion; its generated client calls v1. The corresponding current Debug/source client passes. The empty-response defect was repaired without changing pins. Log: `/tmp/projects-build-20261002-release-Server-reviewed.Tests.log`; this task's frozen Intent prohibits dependency updates and weakening the assertion.

- source_spec: `_bmad-output/implementation-artifacts/spec-g-6-qualify-current-toolkit-preview-tuple.md`
  summary: High G-6 CI readiness blocker: make the CI `g6-current` job able to qualify the approved current tuple.
  evidence: CI run 37106225245 at root e7dc4d876793f6254ce42409c9067bd6516901d4 reported `G6-CURRENT-FAILED` with a clean, approved audit and support 33/33. The local final run at dac6ef2c was `qualified` with validate exit 0. There are two gaps. First, the qualifier failed 0/1 because `Oq8PostgresqlFixture.cs:1625` runs `docker image inspect` on the pinned PostgreSQL digest, which no step pulls onto a fresh runner. Second, the FrontComposer and Parties AppHost source builds fail with NU5118, because packing Hexalith.Commons adds `[workspace]/references/README.md` beside `/README.md`. This is a Hexalith.Commons packing defect, not a GitHub Actions gap. `$(ProjectRoot)/README.md` (`Hexalith.Package.props:32`, with `ProjectRoot` set at `Hexalith.Build.props:24`) resolves to `references/README.md`, and it reproduces in any non-IDE shell. The local runs avoided it only because the inherited `TERM_PROGRAM=vscode` set `IDEBuild=true` (`Props/Environment.Build.props:9`), which makes `IsPackable=false` (`Hexalith.Package.props:20`). The local pre-check therefore never exercised that pack path. Evidence is in `_bmad-output/implementation-artifacts/qualification-evidence/g-6-current-20261003/ci-37106225245-1/`. The owner chose to defer this from the G-6 tuple spec on 2026-10-03. Until it is fixed, `current_checkout_status` stays `failed`.
  status: resolved
  resolution: 2026-10-03; `spec-g-6-ci-readiness.md`; CI [37111866011](https://github.com/Hexalith/Hexalith.Projects/actions/runs/37111866011) at root `387b5af242bc99f48848f3d0265fa3c38575bc03` printed `G6-CURRENT-QUALIFIED`; retained evidence `qualification-evidence/g-6-ci-readiness-20261003/SHA256SUMS`. Exact PostgreSQL pull and Commons/Builds README ownership fixes passed fresh non-IDE local validation and authoritative CI; `current_checkout_status` is now `qualified`.

- source_spec: `_bmad-output/implementation-artifacts/spec-g-6-qualify-current-toolkit-preview-tuple.md`
  summary: Medium verification gap: build and start the Platform AppHost app model against the approved preview Toolkit and the published EventStore.Aspire 3.110.0 binary.
  evidence: The G-6 runner only compiles `references/Hexalith.Platform/apphost.cs` (`tools/qualification/run_g6_current.py:186-190`). Compiling does not exercise the Toolkit calls made inside the EventStore.Aspire 3.110.0 package IL: `WithDaprSidecar`, `AddDaprComponent`, `DaprSidecarOptions`, and the `SidecarOf` annotation check at `apphost.cs:206-215`. That package was compiled against Toolkit 13.5.1-beta.770. An app-model step changes runner material inputs and requires fresh qualification.

- source_spec: `_bmad-output/implementation-artifacts/spec-g-6-qualify-current-toolkit-preview-tuple.md`
  summary: High release-path blocker: the release G-6 rerun has the same missing pinned-PostgreSQL pull and the same NU5118 packing path as CI `g6-current`.
  evidence: `.github/workflows/release.yml:139` runs `tools/qualification/run_g6_current.py` on a fresh `ubuntu-latest` runner, and no step pulls `postgres@sha256:a02db8…`, which `Oq8PostgresqlFixture.cs:1625` requires via `docker image inspect`. Fix it together with the CI readiness item. When `ci.yml` is touched again, add a comment that names the 2026-10-03 owner decision behind `env -u CI`, so it is neither removed nor copied elsewhere.
  status: resolved
  resolution: 2026-10-03; `spec-g-6-ci-readiness.md`; CI [37111866011](https://github.com/Hexalith/Hexalith.Projects/actions/runs/37111866011) at root `387b5af242bc99f48848f3d0265fa3c38575bc03` printed `G6-CURRENT-QUALIFIED`; retained evidence `qualification-evidence/g-6-ci-readiness-20261003/SHA256SUMS`. The release rerun now unconditionally pulls the same pinned digest and uses the shared packing fixes. Workflow pull ordering/failure propagation are covered; the CI restore comment names the owner decision. Release was not dispatched and release readiness is unchanged.

- source_spec: `_bmad-output/implementation-artifacts/spec-g-6-qualify-current-toolkit-preview-tuple.md`
  summary: Medium runner isolation gap: the G-6 qualification runner inherits the caller's environment, so local pre-checks can pass where CI fails.
  evidence: `tools/qualification/run_g6_qualification.py:135-139` copies `os.environ` and removes only `CI`. When the inherited `TERM_PROGRAM=vscode` or `VSCODE_PID`/`VSCODE_CWD` is set, `IDEBuild=true` and Commons gets `IsPackable=false` (`Props/Environment.Build.props:9`, `Hexalith.Package.props:20`). That masked NU5118 locally. `GITHUB_ACTIONS`-gated properties (`ContinuousIntegrationBuild`, `CIBuild`) also differ between local and CI runs, and none of this is recorded in `result.json` `environment`.
  status: resolved
  resolution: 2026-10-03; `spec-g-6-ci-readiness.md`; CI [37111866011](https://github.com/Hexalith/Hexalith.Projects/actions/runs/37111866011) at root `387b5af242bc99f48848f3d0265fa3c38575bc03` printed `G6-CURRENT-QUALIFIED`; retained evidence `qualification-evidence/g-6-ci-readiness-20261003/SHA256SUMS`. Runner commands and tool installation share normalized GitHub Actions build controls with caller IDE/MSBuild overrides removed. Real hostile-environment MSBuild tests and the retained local/CI environment receipts verify packing remains enabled.

- source_spec: `_bmad-output/implementation-artifacts/spec-g-6-qualify-current-toolkit-preview-tuple.md`
  summary: Medium fingerprint reproducibility gap: the G-6 material fingerprint hashes working-tree bytes, so EOL-drifted checkouts produce fingerprints that cannot be reproduced.
  evidence: The local final run recorded `3ad7d5d5…` and CI recorded `81d2066a…` for the same tracked files. Submodule working trees hold CRLF bytes for LF-committed files while `git status` is clean: FrontComposer 6082, Memories 4372, McpCli 224, Builds 197 (`git ls-files --eol`). Hash committed content (e.g. `git cat-file --filters`) or require a renormalized checkout before `reuseEnabled` can be considered.

- source_spec: `_bmad-output/implementation-artifacts/spec-g-6-qualify-current-toolkit-preview-tuple.md`
  summary: Medium stale-gate issue: `tests/tools/test_g6_packet_references.py` fails 17 subtests and no workflow runs it.
  evidence: It fails at HEAD and already failed at baseline `53c6f29` on packet-bound gitlink and Builds execution-SHA differences (for example `51af786…` vs. the Builds gitlink). Its only reference is a `Forbid-Match` in `tests/tools/run-ci-workflow-gates.ps1:690`. Retire it or rebase it on the accepted attempt-16 provenance so it is not mistaken for live coverage.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-verification-items.md; Rebased the suite on archived attempt-16 packet/README provenance, removing comparisons with current gitlinks and current planning documents. Exact full bindings, changed-packet and accepted-normalization controls pass; the suite now runs in the blocking workflow fixture lane.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-compatibility-replay-and-rollback-verification.md`
  summary: Parent gitlinks other than the EventStore verification commit, and later EventStore commits after that verification commit, need their own provenance check.
  evidence: Since `cbcf54fa` the Projects diff moves Conversations, Folders, FrontComposer, Memories, Parties, Tenants, and Builds, and EventStore HEAD `ff7f07d1` is many commits after the verification commits. The verifier SOURCES still bind the approved comparison coordinates. Those pointer moves are not produced by the fixture runner.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-compatibility-replay-and-rollback-verification.md`
  summary: Story 6.1 non-frozen acceptance text was condensed to "full frozen scope" while the test task still names Folder filtering and inspection audit.
  evidence: `spec-6-1-list-and-open-projects-through-supported-authenticated-paths-6.md` lost the explicit Folder-before-pagination, FR-21 audit, and indistinguishable-denial acceptance sentences. The 2026-10-03 frozen prerequisite line matches the recorded P1R decision. Restore the detailed acceptance text in that story, not in the verification packet.
  status: resolved
  resolution: 2026-10-06; Jerome approved sprint-change-proposal-2026-10-06.md. The non-frozen Story 6.1 acceptance section now explicitly requires Folder filtering before pagination, inspection authorization plus durable FR-21 audit, and indistinguishable safe-404 denial. Frozen intent, runtime/tests, pins and blocked readiness remain unchanged.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-compatibility-replay-and-rollback-verification.md`
  summary: Unverified whether a null `CommandEnvelope` body reaches the verification host lambda or is rejected by minimal-API binding.
  evidence: `host/Program.cs` maps `/command` to a non-null `CommandEnvelope` parameter and dereferences `command.AggregateIdentity` immediately. Confirm with a null-body request whether ASP.NET returns 400 before the lambda. If the lambda runs, the host turns a null body into an unstructured failure.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-ci-gate-catalog-pins.md`
  summary: The CI workflow gate does not stop the root from overriding the catalog's `HexalithConversationsVersion`/`HexalithFoldersVersion` version-set properties.
  evidence: `references/Hexalith.Builds/Props/Directory.Packages.props` sets both properties to `1.0.0` only when they are empty. Setting either one in root `Directory.Packages.props`, in `Directory.Build.props`, through a CI `-p:` switch, or through an environment variable changes the whole set without touching any `PackageVersion`, and `run-ci-workflow-gates.ps1` passes. This predates the fix, which deliberately scoped itself to root `PackageVersion` declarations.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-ci-qualification-items.md; The gate rejects case-insensitive declarations directly in both root props files (including namespaced MSBuild XML), literal workflow environment/MSBuild assignments, and inherited environment overrides. Imported props closure and dynamic script evaluation are outside this static check. Hermetic positive and negative fixtures execute the real gate without initialized submodules.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-ci-gate-catalog-pins.md`
  summary: `run-ci-workflow-gates.ps1` reports only its first collected failure.
  evidence: The script sets `$ErrorActionPreference = 'Stop'`, and its final reporting loop calls `Write-Error` once per failure. The first `Write-Error` therefore ends the script, and the CI log shows one failure even when several exist. On 2026-10-06, a copy that printed every failure listed 4 Conversations/Folders pin failures, while CI printed only the first. Fix: print every failure, then `exit 1`.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-ci-qualification-items.md; Console stderr reporting prints every collected diagnostic before exit 1; a four-package simultaneous-violation fixture verifies all four messages.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-ci-gate-catalog-pins.md`
  summary: `run-ci-workflow-gates.ps1` has no regression test; its negative cases are checked by hand in scratch copies.
  evidence: No `tests/tools/test_*.py` drives this gate, although `test_release_package_tools.py` shows the pwsh-from-unittest pattern. The 10 Directory.Packages.props mutation cases from the catalog-pin fix, and every other gate invariant, can regress silently on the next gate edit. The live-apphost spec already plans gate edits.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-ci-qualification-items.md; tests/tools/test_run_ci_workflow_gates.py runs the actual PowerShell gate against temporary checkout fixtures for catalog imports/items/properties, workflow and environment overrides, complete reporting, direct SHA drift and existing safety invariants. CI runs it as a blocking step, guarded against removal and weakening.

- source_spec: `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-6-1-p1r-31190-requalification.md`
  summary: P1R published-executor restore receipts bind recovery steps to `self.commands[-1]`, so the `create-database` and `backup` steps cite the wrong command.
  evidence: In `restore_case`, `create-database` records the last `pg_isready` probe instead of `docker create`/`docker start`, and `backup` records the `docker cp` copy-out instead of `pg_dump`. Baseline 785260fa had the same pattern on its Redis path, where these steps bound the `docker inspect` and `docker cp` rows. No check depends on the step label, but owners reading restore receipts are misled. Fix: return command ids from `container`/`postgres_backup` and pass them to `recovery_command`.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-restore-receipt-command-attribution.md`
  summary: Bind both the PostgreSQL dump and its Docker copy-out to the P1R restore receipt's backup provenance.
  evidence: The recovery receipt schema binds one command to its backup artifact hash. Before the attribution fix that command was `docker cp`; afterward it is `pg_dump`. The raw execution journal retains both, and inventory equality checks the restored state, but the receipt does not explicitly link the two commands as one byte-transfer chain. This is a pre-existing evidence-model limitation requiring a separate compatible receipt contract change.

- source_spec: `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/spec-6-1-p1r-current-ci-repair.md`
  summary: Add isolated public projection-only domain diagnostics discovery coverage in EventStore.
  evidence: Both public projection fixtures use widget, already contributed by an aggregate, so the exact diagnostics-domain assertion cannot detect omitted synchronous or asynchronous projection-domain enumeration; this existing gap predates the P1R CI fixture repair.

- source_spec: `spec-6-1-p1r-successor-enrollment-publication-readiness.md`
  summary: Qualify or explicitly refuse TLS-authenticated remote outer Docker controllers in the P1R isolation wrapper.
  evidence: Independent review B8 found the pre-existing controller strips DOCKER_TLS_VERIFY/certificate/configuration inputs; this task uses the observed local Unix outer endpoint and does not qualify the separate remote TLS profile.


- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-5.md`
  summary: Bind the recovered P1R final-isolation importer to literal container creation and actual image inspection for all three pinned tools.
  evidence: B1: the exact reviewed proposed-successor fixture imports without create/image inspection records or a Python-tools container; independently reproduced on 2026-10-16. Pre-existing in recovery commit b7a1917cec2a59ffe80dc3ee977cf3aa31376b14; changing the frozen 23-path recovery is excluded.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-5.md`
  summary: Bind the recovered P1R final-isolation importer to the actual SDK launch, complete arguments and selected input hash.
  evidence: B2: deleting --inputs from both retained command fields still imports successfully; literal SDK docker exec evidence is not required. Independently reproduced with temporary receipts; pre-existing reviewed source, separate hardening required.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-5.md`
  summary: Reject unrelated outer Docker mutations when importing P1R final-isolation journals.
  evidence: B3: adding a correctly hashed docker rm -f unowned-id journal entry still imports as passed; probe never executes Docker. The exact reviewed validator predates this recovery and must be hardened separately before future reliance.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-5.md`
  summary: Validate private supervisor, owned process identities and cleanup observations in recovered P1R final-isolation imports.
  evidence: B4: a temporary proposed-successor fixture imports after supervisor, owned-process and cleanup observation fields are absent. This pre-existing reviewed validator gap requires separate source rebinding and tests.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-5.md`
  summary: Bind all measured Docker and dotnet tool identities and versions in recovered P1R final-isolation imports.
  evidence: B5: arbitrary dotnet/Docker paths and hashes plus dotnet version 0.0.0 still import; only Python is checked. Independently reproduced with temporary receipts; no operational qualification is claimed by recovery.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-5.md`
  summary: Require distinct ordered repeated absence observations in recovered P1R final-isolation imports.
  evidence: B6: replacing each second inspect command with an exact copy of the first still imports. Independently reproduced with temporary receipts; pre-existing in the exact reviewed source, requiring separate hardening.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-5.md`
  summary: Qualify or explicitly refuse TLS-authenticated remote outer Docker controllers in the recovered P1R isolation wrapper.
  evidence: B7: p1r_isolation.py strips DOCKER_TLS_VERIFY/certificate/configuration inputs while accepting tcp://. Source-only recovery retains the exact pre-existing code and the historical local-Unix-only qualification boundary.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-5.md`
  summary: Bound private Docker startup with an overall deadline and short probe timeouts in the recovered P1R isolation wrapper.
  evidence: B8: 120 readiness attempts each inherit a 300-second Docker timeout, allowing roughly ten hours before startup refusal. This control flow already exists in the exact recovery commit; adjust it in separately rebound source.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-p1r-5.md`
  summary: Test the recovered P1R controller published-run receipt finalization through its real caller after outer cleanup.
  evidence: V1: an in-memory mutation disabling IsolatedRunner._run finalization left all 228 existing Python tests passing. Direct finalizer tests and generic controller tests miss this call site. Add a controlled published-run test whose persisted receipts pass the real importer; exact recovered runner/test hashes remain unchanged here.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths-7.md`
  summary: Story 6.1 cannot serve successful production callbacks until EventStore verifies originating query authority and Folders supports its delegated owner reads.
  evidence: B10/E08; the genuine workload-policy HTTP test admits SDK projection but safely denies queries because the workload principal lacks the originating actor/inspection permission. See docs/supported-project-reads.md for exact producer/consumer paths. Dependency edits remain outside the approved local Projects scope; Story is incomplete.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths-7.md`
  summary: Resolution of the local Story 6.1 B09/B10/E08 dependency gaps under the user's 2026-10-17 exception.
  status: resolved-local-production-blocked
  resolution: SDK-owned conditional-batch recovery and exact signed query-authority transport, current delegated Folder authorization, and their Projects consumers are implemented. Final local verification passed 4,941/4,941 with no skips, including real gateway/owner HTTP and Dapr recovery; evidence is `/tmp/story61-dependency-fixes-20261017-jt9oasms/local-completion-summary.json`. The preceding entry records the earlier scope and remains historical. Production OIDC issuer binding capability, accepted deployed fixture qualification, P1R and formal Story 6.1 acceptance remain blocked. No package pins or release state changed.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-p1r-source-readiness.md`
  summary: Specify the server-owned action for future Story 6.4 resolution authorization.
  evidence: The existing blocked Story 6.4 draft names Tenant, actor, workload and delegation but omits the action required by Epic 6; this EventStore prerequisite explicitly excludes resolution handlers.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-p1r-source-readiness.md`
  summary: Distinguish unauthorized or foreign attachment no-match behavior from safe denial of a protected Project target in Story 6.4.
  evidence: The existing draft has one protected-target row; Epic 6 requires unauthorized attachment evidence to behave as no match. Resolution implementation remains gated.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-p1r-source-readiness.md`
  summary: Specify Conversation membership and explicitly opened session Project fallback for Story 6.4.
  evidence: Epic 6 names the precedence rule, while the pre-existing blocked draft does not; owner metadata contracts remain absent.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-p1r-source-readiness.md`
  summary: Enumerate current Project, Folder, Setup and first-response authority evidence required for usable Partial resolution.
  evidence: The pre-existing Story 6.4 draft uses a generic current-required-evidence phrase; the prerequisite package cannot set future query policy.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-p1r-source-readiness.md`
  summary: Enforce exactly one authorized Folder and quarantine folderless legacy Projects in Story 6.4 candidate admission.
  evidence: Epic 6 states the Folder invariant, while the pre-existing blocked draft does not make it an explicit candidate test.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-p1r-source-readiness.md`
  summary: Spell out AD-32 snapshot fields in Story 6.4 acceptance before the draft becomes ready.
  evidence: The main draft abbreviates the snapshot as shared; the blocked story source lists response state, as-of, disclosable version, component freshness, safe reasons and recovery actions.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-p1r-source-readiness.md`
  summary: Add Epic 6 metadata-read latency targets to future Story 6.4 resolution acceptance.
  evidence: The pre-existing draft omits p95 under 500 ms at 1,000 Projects/500 references and under one second at supported maximum size; no resolution read is implemented yet.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-current-authorized-resolution-evidence.md`
  summary: Qualify or repair cold native source inspection before Story 6.4 G-4 consumption.
  evidence: AggregateActor.OnActivateAsync can perform recovery/reminder writes before InspectSourceProofAsync; stable warm-turn tests do not prove cold zero-write behavior. Owner evidence and parent gates remain blocked.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-prerequisite-current-authorized-resolution-evidence.md`
  summary: Deliver and qualify the existing Conversations metadata mutation dispatcher before claiming production scoring-label reclassification.
  evidence: SL-B1; UpdateConversationMetadataCommand pre-exists without a native production command/boundary/aggregate dispatch path; this source slice verifies contracts and event replay only.

- source_spec: `spec-6-4-prerequisite-current-authorized-resolution-evidence.md`
  summary: Reconcile G-6 CLI default policy with the approved current workflow selection.
  evidence: NH-B8; runner and CI gate defaults still use g6-current-policy.json while the workflows explicitly select the dated policy. Preflight fails closed, but the historical default path targets the older tuple; this predates the native metadata source slice.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-ci-qualification-items.md; Both Python entry points now default to g6-current-policy-2026-10-20.json; the runbook agrees. Behavioral fixtures prove default and explicit policy forwarding. This is the same obligation as the later October 20 defaults entry and grants no qualification acceptance.

- source_spec: `spec-6-4-prerequisite-current-authorized-resolution-evidence.md`
  summary: Reconcile the P0 handoff's approved continuation with its older missing-approval paragraph.
  evidence: NH-B9; the existing P0 handoff records the approved current-catalog tuple near its start but its measured continuation still says no replacement approval was located. P0 owners must retain historical evidence while identifying the current execution boundary.

- source_spec: `spec-6-4-prerequisite-current-authorized-resolution-evidence.md`
  summary: Bind portable immutable references for P0 receipts currently named only by external absolute checkout paths.
  evidence: NH-B10; the existing owner proposal and continuation receipt paths resolve outside this Projects checkout. A reviewer or CI consumer needs owner-controlled repository/artifact identities and hashes; no P0 acceptance or receipt is fabricated by this source change.

- source_spec: `spec-6-4-prerequisite-current-authorized-resolution-evidence.md`
  summary: Add behavioral verification of G-6 placement and scheduler immutable image identities.
  evidence: NH-V1; the independent verification-gap reviewer traced all three runner tests to exits before image selection and found the retained validator checks roles/IDs but not image values. Add a launch-path regression and mutable-tag negative control in the existing G-6 work; the source pin change predates this resume.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-verification-items.md; A controlled run_g6_current.main execution reaches both real launch calls and verifies the exact approved Dapr manifest for each role; an in-memory mutable-tag mutation demonstrates the same observations distinguish a substituted tag. This closes launch verification only and grants no live qualification or imported-receipt acceptance.

- source_spec: `spec-6-4-prerequisite-current-authorized-resolution-evidence.md`
  summary: Reconcile the EventStore authoring guardrail's nested Tenants path with supported umbrella source layout.
  evidence: Independent full SDK DomainService source run on 2026-10-21 executed 1091 tests (1090 passed, 1 failed, no skips); DomainModuleAuthoringGuardrailTests.TenantsDomainService_DoesNotReferenceGeneratedApiHostOrDeclarePerMessageControllers requires references/Hexalith.EventStore/references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj. Only root-declared dependencies may be initialized, so this run preserves the guard and layout. Exact command, native XML and failure log are retained in the native provenance evidence bundle; focused source results are separate.

## Deferred from: code review of spec-6-4-resolve-projects-with-transient-current-explanations.md (2026-10-21)

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md`
  summary: Run the delegated Conversation advisory owner hop (`DelegatedProjectAdvisoryConversationDirectory` → `GetConversationAdvisoryMetadata.v1`) in a routinely executed lane.
  evidence: The only test that exercises this hop is `SupportedProjectGatewayHttpTests`. That file is wrapped in `#if HEXALITH_SOURCE_FIXTURE`, a symbol defined only when `UseHexalithProjectReferences=true`, and CI forces `UseHexalithProjectReferences=false`. The test also skips unless `STORY61_DAPR_HTTP_ENDPOINT`/`STORY61_DAPR_GRPC_ENDPOINT` are set, and no CI workflow sets them. Every always-run test substitutes `AdvisoryTestConversationDirectory`, and no unit harness exists for `DelegatedOwnerQueryClient`. A change to the request's domain, aggregate or payload would therefore break every Conversation suggestion while CI stays green (medium; Verification Gap finding 2).

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md`
  summary: Align the default G-6 runner, CI wrapper and topology runbook with the current October 20 qualification policy.
  evidence: Final BH2 verified that tools/qualification/run_g6_current.py and docs/runbooks/projects-topology.md default to g6-current-policy.json (SDK 10.0.401/Aspire 13.6.0), while the checkout uses SDK 10.0.402 and CI explicitly selects g6-current-policy-2026-10-20.json. The same runner default exists at the original Story 6.4 baseline; this is independent qualification-tooling work.
  status: resolved
  resolution: 2026-10-23; spec-five-deferred-ci-qualification-items.md; Both runner defaults and documented validation commands select g6-current-policy-2026-10-20.json, matching CI/release. Behavioral fixtures cover default and explicit policy forwarding; historical evidence and acceptance state remain unchanged. Also closes the earlier NH-B8 description of this same defect.

## Deferred from: code review of spec-6-4-resolve-projects-with-transient-current-explanations.md (2026-10-22)

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md`
  summary: Decide whether local `DelegatedOwnerQueryClient` precondition failures may surface as a Conversation 404 in the advisory query.
  evidence: Fourth-pass chunk-1 BH11, maybe-false (medium if true). `ProjectQueryEnvelopePrincipalBinding.TryBind` admits a JWT principal without a `VerifiedQueryCaller`. `DelegatedOwnerQueryClient.InvokeAsync` then returns the same `safe-denial` string it uses for an owner 404, and `ProjectAdvisoryQueryExecutor.ConversationAsync` maps that to Denied, so every Conversation query would look missing. To settle it, confirm whether a direct, non-gateway `/query` composition is a supported deployment; if it is, check `VerifiedQueryCaller` before the owner hop and report Incomplete.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md`
  summary: Report Conversation-owner unavailability on the retained `GET /api/v1/projects/resolution/from-conversation` route instead of NoMatch.
  evidence: Fourth-pass chunk-1 AA4b. This is pre-existing, documented fail-closed behavior: the legacy `IProjectConversationResolutionDirectory` returns degraded metadata, and the endpoint maps it to NoMatch (200). The legacy `ProjectResolution` response has no coverage field. Address it when the CLI and UI move to `/query` and the legacy routes are retired.

## Deferred from: renderer snapshot hardening review (2026-10-23)

- source_spec: `_bmad-output/implementation-artifacts/spec-dw-39-43-renderer-snapshot-hardening.md`
  summary: Constrain renderer publication and reuse beneath trusted render-tree ancestors.
  evidence: Independent review and a temporary CLI-equivalent reproduction moved destination.parent outside the render tree and replaced it with a symlink; rendering still succeeded. Ancestor containment is a pre-existing limitation outside DW-39's generation-directory and descendant checks; qualify the trusted boundary before changing publication paths.

- source_spec: `_bmad-output/implementation-artifacts/spec-dw-39-43-renderer-snapshot-hardening.md`
  summary: Make existing-snapshot verification resistant to concurrent substitutions between structural checks and byte reads.
  evidence: Independent review and a deterministic temporary reproduction replaced guide.md with a matching external symlink during manifest.read_text, after rglob/lstat; _verify_existing still succeeded. This pre-existing concurrent-writer limitation requires separately scoped descriptor-based traversal and reads, while DW-39 rejects links already present at verification preflight.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-ci-qualification-items.md`
  summary: Extend catalog property ownership validation to the effective local props import closure.
  evidence: Independent review reproduced a pre-existing bypass: root Directory.Build.props imports override.props, which declares HexalithFoldersVersion=9.9.9; the static direct-file gate passes while dotnet msbuild reports 9.9.9. This slice checks direct root declarations and literal workflow/environment inputs without initializing submodules; import evaluation needs a separately bounded resolver or an effective-MSBuild gate.
