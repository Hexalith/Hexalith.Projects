---
title: 'Story 6.4 prerequisite: separate tooling and published source'
type: 'chore'
created: '2026-10-18'
status: 'done'
baseline_commit: '18b3ce9ff9a84625f61f2a42519238173a4ee39a'
owner_baseline_commit: '2490f4f409e015155c046afa55b37997b04a271d'
route: 'dispatch'
owner_repository: 'references/Hexalith.EventStore'
candidate_version: '3.125.1'
candidate_source: '55432c50d4b6f1666b4bd6d4a60bfc263b2d0e0b'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-6-1-p1r-31251-published-qualification-handoff.md'
---

<frozen-after-approval reason="simplified proposal; implementation awaits approval">

## Intent

Allow reviewed qualification tooling to run from its own pinned commit while runtime/source stays at `v3.125.1`, commit above. Reuse the existing owner-input, repository-inventory and receipt mechanisms.

## Proposed change

1. Add an optional `tooling: {commit, builds_commit}` block to existing planning/owner inputs. Both values are full Git commits, selected later after implementation. Require tooling HEAD and its actual Builds checkout to match these pins, with clean tracked inputs and no untracked code/configuration inputs. Apply the same checks to `observe` and `run` before restore or resource creation.
2. Add `--source-root` and `--source-builds-root`. Require source HEAD and `v3.125.1` to resolve to the fixed release commit, with clean source inputs. Source Builds must match the release gitlink `4629ff2d3bd66376b29a553663c241d3a7b87853`. Missing pins or mismatches refuse; there is no bypass flag. Without the optional block, retain the existing equality guard and behavior.
3. Keep scripts, fixtures and witnesses under tooling ROOT. Direct Debug/source ProjectReferences and the PostgreSQL template to the release source root. Pass the verified source Builds path through the existing MSBuild import properties; check actual imported paths, source references and effective version 3.125.1. Preserve Release/package-only lanes. No released source or catalog edits are needed.
4. Extend the existing source-binding record with `runtime_source` containing its path, repository inventory and source Builds identity. Keep existing fields attributed to tooling. Reuse current receipt hashes and source rechecks through isolation, executor, import and packet validation. Both roots must agree everywhere; changes during execution remain failures while owned cleanup continues.

Keep `candidate.builds` attributed to package construction. Record actual qualification Builds separately through the tooling binding. Preserve the handoff's existing tag/package/CI/release observations; they do not require a new provenance subsystem.

## Boundaries

Existing enrollment, package/graph/loaded-identity checks, snapshot/writer/isolation/cleanup controls, `rollback=null` and Projects AD-17 recovery remain mandatory. Extend existing contracts with the bounded optional fields; create no new verifier CLI, selection files, trust framework or schema family. Legacy packets retain their meaning. Only exact 3.125.1 gets the separate-root path.

Stop at specification approval. This task authorizes no implementation, published qualification, dependency/ref/gitlink update, owner selection/acceptance creation, Stack/sprint change or Story 6.4 implementation.

## Acceptance tests

Use temporary repositories and fake operational boundaries. Every refusal before execution must leave restore/resource/reset/writer seams untouched.

| Given | When | Then |
| --- | --- | --- |
| Pinned tooling differs from the exact release | Both entry paths and packet validation run | Separate roots succeed and retain both identities |
| Tooling has wrong HEAD, edited/staged inputs or an added module | Preflight runs | Refuse before execution |
| Source is changed or redirected to tooling | Preflight/source references are checked | Refuse |
| Release tag is missing or resolves to another commit | Either entry path validates | Refuse |
| Tooling Builds or source Builds is substituted, including an unexpected imported catalog | Identity/import checks run | Refuse; one Builds role cannot replace another |
| Runtime binding is removed/swapped, even with recomputed receipt hashes | Isolation/import/final validation runs | Refuse against the selected inputs and current inventories |
| Historical inputs or unenrolled successors are supplied | Existing admission tests run | Historical behavior remains; unknown successors refuse |

</frozen-after-approval>

## Code Map and Tasks

EventStore-relative paths; reuse existing helpers rather than adding another harness.

- [x] `tools/p1r_qualification.py` — extend `repository_binding`, `source_binding` and `check_current_source` for the optional runtime source; reuse inventories and add cleanliness checks.
- [x] `tools/p1r-published-executor.py`, `tools/p1r_published_executor.py` — accept/forward source paths, validate both pins at both entry points, and route source builds/configuration correctly.
- [x] `tools/p1r-published-qualification.py`, `tools/p1r_published_qualification.py` — accept optional tooling inputs and source paths; preserve both identities when creating, importing and validating packets.
- [x] `tools/p1r_isolation.py`, `tools/p1r_isolation_evidence.py` — forward source arguments and recheck the extended binding using existing lifecycle checks. Keep witness attribution to tooling.
- [x] `tools/tests/test_p1r_qualification.py`, `test_p1r_published_executor.py`, `test_p1r_published_qualification.py`, `test_p1r_isolation_hardening.py` — cover binding/cleanliness, both entry paths, receipt substitution and post-start drift with cleanup. Preserve enrollment tests.
- [x] `tools/p1r-published-consumers/README.md` and the Projects handoff — document the two roots and pending execution gates after implementation.

## Design Notes

Planning recheck (2026-10-18): `validate_execution_inputs` and `observe_inputs` still require tooling HEAD = release tag. Reuse `EventStoreSourceRoot` and existing Builds-import properties, checking evaluated imports for every source consumer. No intent gaps or irreversible actions remain for this proposed tooling repair. Its approval would leave published execution and Story 6.4 behind their existing gates.

## Verification

After approval, run the existing executor, published-qualification, qualification, isolation, isolation-hardening and snapshot-registration unittest suites individually. Use tooling fixtures only; do not run published qualification. Check protected hashes and `git diff --check` in both repositories. Follow the repository's baseline requirement before code edits and record any environment blocker separately.

## Implementation Notes

- 2026-10-18: User explicitly approved this prerequisite with "approve and continue" after the specification checkpoint. This supersedes the drafting-only stop at specification approval in the preserved proposal and authorizes its tooling implementation, tests and review. All other frozen boundaries remain in force; published qualification, owner selection/acceptance, dependency/ref/gitlink updates, sprint changes and Story 6.4 implementation remain unauthorized. The approved file is unchanged apart from workflow status, baseline identities and this approval record.

- Implemented the approved optional tooling pins, separate release roots, strict inventories (including ignored compiler inputs), source MSBuild routing/evaluation, and source-aware isolation/receipt/packet rechecks. Historical inputs retain their existing equality guard. README and qualification handoff describe the remaining execution gates.
- Verification: six complete focused suites passed, 323 tests total. Primary named matrix/legacy verification passed 18 tests; optimized isolation hardening passed 27 on final sources. [Retained commands, outputs and matrix coverage](evidence/6-4-p1r-31251-immutable-binding-20261018/README.md).
- The Aspire check ran after the initial helper edit; build passed with zero warnings/errors, startup exited 2 (AppHost 134) due to absent nested Tenants hosts. This sequencing deviation and original logs are recorded in the linked evidence. No nested submodules were initialized.
- Primary diff audit confirmed all tasks and matrix cases, unchanged production/deploy/catalog inputs and Git identities, preserved frozen intent, and unchanged Story 6.4/sprint/P1R acceptance state. Independent review remains pending.

- Review completion: all 16 findings were individually triaged and corrected in 13 bounded patch groups. The final primary run passed **332 tests** across the six required suites, plus **27 optimized isolation-hardening tests**, with zero skips. Full native outputs, exact commands and reviewed-source hashes are retained in the [final verification record](evidence/6-4-p1r-31251-immutable-binding-20261018/final/commands.json). No findings were deferred.
- Final audit confirms the approved matrix, actual source graph before/after restore, input stability through provisioning, fail-closed hidden/untracked inputs, and independent receipt/import checks. Production files, catalogs, Git identities, frozen scope, Story 6.4 tracking and P1R acceptance remain unchanged. The prerequisite is complete; exact tooling/qualification selection and downstream gates remain pending.
- No local commit was created: the approved frozen scope prohibits Git-ref updates, so the workflow's generic local-commit step is superseded. No staging, commit, push or dependency change was performed.

## Spec Change Log

User requested a pragmatic reduction: one specification, existing contracts and helpers, focused substitution tests. The larger companion design is withdrawn.

## Review Triage Log

### Pass 1

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| B1: hidden tracked edits | high: primary temporary-repository probes admitted modified runtime source under both assume-unchanged and skip-worktree; index IDs and diff do not establish physical content equality. | patch |
| B2: native/sourceless modules | high: primary probes admitted untracked .so and .pyc tooling modules because neither inventory nor suffix checks cover them. | patch |
| B3: missing source references | high: closure expectations are derived from submitted references, so deleting all references and their evaluations leaves only the three accepted consumers. Require the known release projects independently. | patch |
| B4: consumer output root | medium: validate_source_evaluations derives the source consumer root from its first submitted project; the validated isolation execution path is available to bind it independently. | patch |
| B5: MSBuild command grammar | medium: the validator checks only -p: switches and a prefix, admitting overriding /p: switches or response files outside that comparison. | patch |
| B6: external imports | medium: foreign .targets paths satisfy current checks because only tooling and package-catalog paths are constrained. Use the existing selected roots and pinned SDK identity. | patch |
| B7: pre-restore graph only | medium: build_consumers evaluates the source closure before restore and builds after restore without another evaluation; generated imports can differ. Recheck the same graph after restore. | patch |
| B8: imported workspace containment | medium: producer containment exists, but independent isolation import compares runtime paths only to the binding; it must also require containment in the verified workspace mount. | patch |
| B9: observation input drift | medium: provisioning rechecks physical selected inputs only for run; observe rereads its planning file after provisioning without preserving the original hash. | patch |
| B10: real MSBuild coverage | medium: source-wiring tests fabricate output, leaving actual helper/property/project integration unverified. Same root cause as V2. | patch |
| E1: hidden tracked edits | high: independently reproduced by primary for both index flags; same defect as B1. | patch |
| E2: embedded repository | high: primary probe admitted src/Injected/Payload.cs because Git reports the embedded repository as a directory and the filename filter drops it. | patch |
| E3: empty source closure | high: the independent reviewer demonstrated coordinated deletion of source references and evaluations; current closure loop has no independent known-project floor. Same defect as B3. | patch |
| V1: emitted observation pins | medium: verified mutation removed the generated tooling block while all 321 relevant tests passed; existing observation tests stop before writing owner-inputs.json. | patch |
| V2: source-mode helper integration | medium: verified mutation removed UseCurrentSource while the mocked source-wiring test still passed; real evaluation would choose the wrong branch and refuse. Add real nonoperational MSBuild evaluation. | patch |
| V3: PostgreSQL template routing | medium: verified mutations restored both reads to tooling ROOT while 321 tests passed because the fixtures use default or identical templates. Use distinct valid templates to prove both producer and importer behavior. | patch |

Groups: B1/E1; B3/E3; B10/V2. Every other finding has a distinct cause and remains a separate patch. All fixes preserve the existing interface and implement explicit frozen source, input, import or verification requirements; no intent or specification change is needed. No findings are deferred.

### Pass 1 patch verification

All recorded rows are resolved. Physical-file hashes now detect both index flags; native/sourceless inputs and collapsed embedded repositories refuse. Source evaluation binds the known connected five-project graph to the validated execution directory, enforces complete argv/import locations and repeats after restore. Both runtime roots must be inside the verified mount, and observe/run physical input bytes stay fixed through provisioning. Completed observation, real MSBuild and distinct-template tests close the three verification gaps. Primary full-suite output confirms every covering test ran and passed; no deferred entry is required.
