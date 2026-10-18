---
title: 'Story 6.4 prerequisite: separate tooling and published source'
type: 'chore'
created: '2026-10-18'
status: 'draft'
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

- [ ] `tools/p1r_qualification.py` — extend binding/recheck helpers for the optional runtime source; reuse repository inventories and add cleanliness checks for this path.
- [ ] `tools/p1r-published-executor.py`, `tools/p1r_published_executor.py` — accept/forward source paths, validate both pins at both entry points, and route source builds/configuration correctly.
- [ ] `tools/p1r-published-qualification.py`, `tools/p1r_published_qualification.py` — accept optional tooling inputs and source paths; preserve both identities when creating, importing and validating packets.
- [ ] `tools/p1r_isolation.py`, `tools/p1r_isolation_evidence.py` — forward source arguments and recheck the extended binding using existing lifecycle checks. Keep witness attribution to tooling.
- [ ] `tools/tests/test_p1r_published_executor.py`, `test_p1r_published_qualification.py`, `test_p1r_isolation_hardening.py` — add the table's positive/refusal cases and a post-start change/cleanup regression. Preserve enrollment tests.
- [ ] `tools/p1r-published-consumers/README.md` and the Projects handoff — document the two roots and pending execution gates after implementation.

## Verification

After approval, run the existing executor, published-qualification, qualification, isolation, isolation-hardening and snapshot-registration unittest suites individually. Use tooling fixtures only; do not run published qualification. Check protected hashes and `git diff --check` in both repositories. Follow the repository's baseline requirement before code edits and record any environment blocker separately.

## Implementation Notes

## Spec Change Log

User requested a pragmatic reduction: one specification, existing contracts and helpers, focused substitution tests. The larger companion design is withdrawn.
