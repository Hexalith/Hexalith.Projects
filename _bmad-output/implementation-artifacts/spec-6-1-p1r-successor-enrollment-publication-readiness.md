---
title: '6.1-P1R Enroll the proposed successor and prepare publication readiness'
type: 'bugfix'
created: '2026-10-14'
status: 'done'
route: 'dispatch'
baseline_commit: '8a9b739a30ee2b501db53756b508efb535d10c41'
owner_repo_baseline_commit: '5c10a3c73f381abb75016125192401f516818797'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/projects/AGENTS.md'
  - '/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore/AGENTS.md'
  - '/home/administrator/projects/hexalith/projects/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="user explicitly authorized continuation, enrollment, independent review and verification">

## Intent

**Problem:** The verified fixture/isolation repair still enrolls only 3.121.0 in its bounded Counter snapshot path. Canonical 3.122.0 has since been published, so a fresh successor needs explicit finite enrollment and publication gates before it can be selected or qualified.

**Approach:** Propose 3.122.1 from canonical release analysis, contingent on a future authorized fix landing and repeated availability analysis. Enroll that single additional stable version throughout qualification tooling, prove real strict snapshot validation executes and rejects absent/substituted evidence, independently review and verify, then retain a concrete publication handoff.

## Boundaries & Constraints

**Always:** Preserve existing dirty repair work, fixture correction, private Docker isolation, complete shared inventory equality, exact owned cleanup and untouched unowned resources. Preserve all historical/failed evidence, rollback=null, Dapr 1.18.2, PostgreSQL digest/profile, comparisons 3.70.1/3.110.0, p1r-executed-checks-v1, Reminder recovery and logical-event evolution. Keep four-role acceptance, conformance and disposition pending, P1R unusable and Story 6.1 blocked. Distinguish observed local/canonical Builds identities and future frozen source/package/publication identities. Source controls cannot qualify unpublished packages.

**Never:** Stage, commit, push, publish, change dependencies, synchronize sprint state, alter production snapshot enrollment, relax guards, broaden enrollment to arbitrary successors, delete/replace prior evidence or modify the original repair proposal. Only add 3.122.1 to existing finite allowlists.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Behavior |
| --- | --- | --- |
| Compiler enrollment | 3.122.1 or distinct Debug/source consumer | All three defines, shared fixture, evolution and parity compile sources enabled |
| Finite enrollment | Later unselected version | No successor-specific compiler sources; genuine seed refuses |
| Genuine seed | 3.122.1 or source, interval ten | Nine commits, manual snapshot, three tail commits, preserved bytes; no diagnostic synthesis |
| Strict proof | Required 3.122.1 cases including restore | Actual strict validator runs with selected candidate/source writer and accepts intact evidence |
| Missing/substituted proof | Deleted seed/source, altered version-one envelope, physical probe, launch version or provider proof | Strict validation rejects; recomputed dependent hashes cannot bypass it |
| Historical behavior | Existing 3.121.0 and historical comparison fixtures | Existing genuine and diagnostic paths/controls remain valid |

</frozen-after-approval>

## Code Map

Owner: `references/Hexalith.EventStore`; paths below are relative to that repository. Do not discover/load any skills there.

- `tools/p1r-published-consumers/Directory.Build.props`: three capability/candidate/folded-snapshot defines. `host/Host.csproj`: evolution and shared fixture inclusion. `domain/Domain.csproj`: evolution exclusion, fixture inclusion, parity exclusion. Probe inherits shared props.
- `tools/p1r_published_executor.py::seed`: explicit manual seed guard; retain real nine/manual/three path. `Executor.case`: retained shared snapshot fixture-source witness guard.
- `tools/p1r_check_witnesses.py`: candidate/writer requirement, strict writer allowlist, post-upgrade writer selection. Derive restore writer from candidate_version; use candidate-specific case writer coordinates, preserving finite candidate enrollment.
- `tools/tests/test_p1r_published_executor.py::QualifiedSnapshotWitnessTests`: complete synthetic retained evidence already reaches real validator; parameterize for 3.122.1 and source without discarding 3.121.0 controls. Existing MeasurementTests cover seed and historical diagnostic.
- `tools/p1r-published-consumers/README.md`: document finite proposal and source/package distinction. `.github/workflows/p1r-qualification.yml` already runs executor and isolation controls; use existing suite discovery.
- Prior immutable context: `evidence/6-1-p1r-31210-blocker-repair-20261014/README.md` and `successor-proposal.md`. New evidence only: `evidence/6-1-p1r-successor-enrollment-20261014/`.
- Root retains release analysis, source hashes, preservation baseline, independently reruns checks and authors final handoff. Implementation agent handles enrollment, focused controls and consumer README only; append verification facts here.

## Tasks & Acceptance

**Execution:**

- [x] Consumer props and both project files: explicitly enroll 3.122.1 in all eight relevant conditions, preserving historical enrollments.
- [x] Executor and strict witness validator: enroll 3.122.1 in all five guard groups; bind restore writer to selected version.
- [x] Executor test suite: add meaningful positive/negative strict validation, seed and emitted case witness controls for proposed version/source; retain prior regressions.
- [x] Consumer README: document bounded proposed enrollment, unchanged production and pending package/authority gates.
- [x] Root evidence: actual MSBuild property/Compile evaluation for Host, Domain and Probe; rerun relevant Python suites, independently review changed closure and verify preservation.
- [x] Root handoff: record canonical analysis and registry absence, reviewed diff hash, observed identities, outstanding authorized landing/publication identities and exact gated publication/qualification commands.

**Acceptance Criteria:**

- Given complete proposed-version/source evidence, when strict validation runs, then the actual genuine snapshot validator executes with the correct writer; missing or substituted witnesses are rejected.
- Given finite enrollment, when all three consumer projects are evaluated, then only explicitly enrolled versions/source receive successor snapshot definitions and compilation inputs.
- Given reviewed source verification, when handed off, then no publication/package qualification or role/usability/Story transition is claimed.

## Implementation Notes

The user expressly authorizes extending the dirty repair tree; approval and commit/sprint-sync workflow defaults are overridden. No additional approval is needed for this reversible implementation. Root independently verifies after dispatch.

Implementation preserved the existing dirty repair and adds only 3.122.1 to the finite consumer and genuine-snapshot guards. Strict case requirements use the selected candidate's writer coordinates; post-upgrade restore derives its writer from `candidate_version`. Existing complete retained-evidence tests now cover both 3.121.0 and 3.122.1, with actual strict validation for candidate/source writers in every required snapshot lane and selected-package restore. Seed, missing/substituted proof and emitted fixture-source controls remain tooling evidence only.

Following the user's clarified upstream-refresh request, root safely refreshed the owning EventStore checkout to observed `origin/main` commit `210b90da172cc7ca946fbb06cfd44390bffa707b` and reported exact preservation of all 1,541 carried local files (318,786,996 bytes), no incoming overlap and zero staged paths. This is the refreshed implementation baseline; the original baseline frontmatter remains unchanged. No source freeze, publication, package qualification or acceptance transition is claimed.

## Spec Change Log

Independent review exposed genuine-provider byte-order refusal, missing temporal/serving-binary/mutation proof binding, imported-receipt forwarding coverage, ambient-marker admission, cleanup-time source mutation, late creation/absence proof and final isolation import gaps. Fix these demonstrated boundaries with meaningful controls before final freeze. KEEP: finite version enrollment, production source, pinned runtime/provider/runner bytes, exact cleanup ownership, complete shared equality, fixture correction, both additions and all historical/failed evidence. Include the four existing dated CI helper sources in the reviewed publication closure and mandatory landing inventory. Host packet preparation is replaced in the handoff with the same pinned runner interpreter; shell commands must fail closed. No human intent or acceptance/Story transition changed.


## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patched | Original provider bytes vs canonical snapshot hash rejects valid base64 envelopes. |
| B2 | medium | patched | Host preparation interpreter differs from pinned execution; handoff now uses pinned isolated runner for boundary/preparation/validation and independently reviewed. |
| B3 | medium | patched | Four dated CI helper sources were omitted from publication diff; required-ci-test-dependencies.json now binds unchanged helper bytes and final closure must include them. |
| B4 | high | patched | Operational imports must bind successful matching final private/outer isolation receipts rather than accepting inner receipts after failed teardown. |
| B5 | high | patched | Nonempty isolation marker is not real daemon/namespace proof; marked direct admission must revalidate private_proof. |
| B6 | high | patched | Cleanup handler can mutate source after source check; verify again after all owned processes stop. |
| B7 | high | patched | Successful rm is not observed absence; retain exact identity and repeated container/volume absence proofs. |
| B8 | medium | defer | Pre-existing TLS-authenticated remote outer Docker controller profile loses authentication. Current observed qualification profile uses local Unix outer endpoint; no TLS profile qualification is claimed. |
| B9 | medium | patched | Serving Host/Domain binaries need independent physical digest binding and missing/substitution controls. |
| B10 | medium | patched | Successful provider mutation inside genuine seed proof window must be refused, while later allowed diagnostics remain intact. |
| E1 | high | patched | Independent duplicate of B5: inherited marker bypasses actual namespace proof. |
| E2 | high | patched | Pending create ownership dropped after first absent inspection allows late container to escape retry. |
| E3 | medium | patched | Command ID ordering does not guarantee nine seed finishes before manual creation; enforce actual times. |
| V1 | medium | patched | Imported successor receipts lack forwarding coverage; real receipt_outcome/bind_lane controls must fail if selected-version argument is dropped. |
| R1 | medium | patched | Publication command snippets could continue after failed tests; all blocks now set -euo pipefail. |
| R2 | medium | patched | Process-substitution errors could yield partial receipt arguments; checked producer now validates all paths before emitting data. |

| R3 | high | patched | Actual importer now rejects rehashed backwards/overlapping outer cleanup and lane/case commands outside nested executor/private intervals; blind final recheck passed. |
| R4 | high | patched | Standalone and embedded cleanup bind exact executor invocation and container IDs; edge final recheck passed. |

## Verification

Run existing executor, isolation, preparation and published-qualification unittest discovery commands individually. Evaluate compiler conditions with actual `dotnet msbuild -getProperty:DefineConstants -getItem:Compile` without restoring unpublished packages. Retain literal commands/results in the new evidence directory. Root verifies unchanged production/tooling outside the allowed delta, historical evidence and canonical indexes.

Implementation-agent check before upstream refresh: from `references/Hexalith.EventStore`, `python3 -m unittest discover -s tools/tests -p 'test_p1r_published_executor.py'` exited 0, reporting `Ran 90 tests in 15.546s` and `OK`. After refresh, `git diff --check` exited 0. Root independently reruns the final checks and retains their literal results in the new evidence directory.

Implementation-agent final recheck on refreshed EventStore baseline `210b90da172cc7ca946fbb06cfd44390bffa707b`: the same executor discovery command exited 0, reporting `Ran 90 tests in 19.587s` and `OK`; final `git diff --check` exited 0. The Python 3.14 run emitted a nonfailing `ResourceWarning` for an unclosed `/proc/269697/environ` read during existing process controls. It did not skip or fail a test and is recorded separately from the enrollment behavior.


Root final verification on refreshed upstream HEAD `210b90da172cc7ca946fbb06cfd44390bffa707b`: 228 Python controls (executor 104, isolation 28, published qualification 61, preparation 35), 178 Server snapshot/read/manual/folded controls and 24 actual MSBuild evaluations passed. The Server Debug build had zero warnings/errors. A fresh real private runner executed only a source-control command, checked source again after descendant cleanup and retained successful exact outer teardown; it qualified no successor package or scenario. Independent blind and edge final rechecks resolved the temporal and cross-invocation findings. The pre-existing remote TLS outer-controller profile remains deferred in the ledger; verification used the observed local Unix profile.

Final source diff: `publication-source-v3.diff`, SHA-256 `54263498531e2efe7b3c100d4244269a57dbf0afb8d23f3a21ed0708a91cde7d`, with 23 exact source/helper paths. Evidence is retained in EventStore's `6-1-p1r-successor-enrollment-20261014` directory. All 191,058 historical files (15,134,803,364 bytes) across thirteen evidence directories remained byte-identical; no files were missing or added there. Protected Story, acceptance, sprint and prior repair spec remained unchanged. Production matches refreshed upstream; the original EventStore main remains `5c10a3c73f381abb75016125192401f516818797` and the current checkout is detached at latest origin/main. Both repositories have no staged paths.

This spec's `done` records completion of source enrollment and publication-readiness handoff only. Story 6.1 stays blocked; four-role acceptance, conformance and disposition stay pending, P1R stays unusable, and package qualification is false. Publication remains blocked by missing authorized landing/frozen identities, exact-source CI, one-use authority wiring and authenticated OCI availability. User instructions override Step 5's commit and sprint-sync defaults: no staging, commit, push, publication or Story status synchronization was performed. A dispatched agent briefly rendered the workflow a second time before root clarified that dispatch was already active; it then stopped the duplicate workflow and used the frozen root spec, with no source or acceptance effect.
