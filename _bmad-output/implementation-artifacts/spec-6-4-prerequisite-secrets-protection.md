---
title: 'Story 6.4 prerequisite: repair reusable-content secret scanning'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
owner_repository: 'references/Hexalith.EventStore'
owner_baseline_commit: '794369b30c2f3615a7f1d70a5beb1935f6470b36'
context: []
---

<frozen-after-approval reason="explicit bounded prerequisite repair requested by the user">

## Intent

**Problem:** EventStore CI at `794369b30c2f3615a7f1d70a5beb1935f6470b36` fails `SecretsProtectionTests.TrackedReusableContent_DoesNotContainUsableSecrets`. The preceding sentinel repair requires an exact-source CI/P1R preparation check and the actual scan violations require reproduction and diagnosis.

**Approach:** Retain completed hosted results and reproduce the exact guard locally; identify the actual content and repair only bounded false-positive recognition with regression controls through the canonical reader/scanner. Every reusable text file and every credential detector remains active. Literal credentials, malformed metadata, substitutions and adjacent credentials must still fail. Do not add evidence exclusions, retirements, new hash allowances or weaker placeholders. Preserve historical evidence bytes, package pins, acceptance records, Story 6.4's blocked state and its original draft/sprint status. Do not publish packages or perform commit/push/dependency changes.

</frozen-after-approval>

## Implementation Notes

- This is prerequisite repair, not implementation of epic Story 6.4; leave `story_key` unset so workflow status synchronization cannot advance the blocked story.
- Both repositories began clean on main. Projects source: `3a08ccb813251fa21fdd6b28d0516ad15ea438e4`. EventStore is exactly the requested full commit above.
- Read the workspace Hexalith baseline, owning entry point, EditorConfig/attributes, build/project/package configuration, CI workflow and `docs/architecture/evidence-secret-scanning.md` before edits. No nested submodules initialized.
- Hosted exact-source CI `39108443964` failed only the tracked reusable-content test with 2,812 findings. P1R Preparation `39108443823` and Integration Tests `39108443772` passed. Other blocking CI jobs passed; optional performance/Aspire jobs were skipped.
- Investigation map: `SecretsProtectionTests.cs` owns `FindViolations`, `ScanTrackedFile`, canonical decoding and schema-qualified `IsNuGetDependencyProperty`. Existing evidence ingestion helpers provide isolated-file/encoding controls. `SecretsProtectionTests.HistoricalLines.cs`, the retirement registry and historical captures are preservation boundaries. First reported paths are content-addressed OQ8 assets whose NuGet dependency identities are mistaken for credentials; the full path/line inventory will determine the exact repair.
- Intent gaps: none. Irreversible actions: none. Expected footprint: existing scanner, one focused test partial, architecture explanation and new diagnostic/verification evidence. Small reversible repair follows the oneshot route with independent review.
- Added isolated-file regressions for central package versions, package dependency edges and the two framework reference assemblies across eight decoder lanes. Initial red run: 114 cases, 85 failures. The first repair exposed an incorrect URI test fixture (unescaped random Base64 could contain `/`) and assets schema version 4 in the actual original; escaped the URI userinfo and recognized only assets versions 3/4. All attempted results are retained.
- Recognition is restricted to six named package identities and exact metadata positions. Framework reference dependencies additionally require matching runtime/compile-only target identity and referenceassembly/serviceable/hash metadata; the existing package hash branch remains intact. No evidence seal, retirement, path or detector rule was relaxed.

- Independent review found four bounded verification/schema gaps; patched each: require matching target-framework metadata; assert the exact injected credential line even for malformed decoder lanes; scan the new untracked test partial through existing owning-source ingestion; cover original assets from both affected historical directories. Every historical asset continues to be read and scanned.

## Review Triage Log

- **medium / patched:** Central version metadata could lack a corresponding target framework. Added exact object target linkage and a mismatched-framework regression. Actual captured central metadata all has this linkage, including ten legitimate documents with empty libraries.
- **medium / patched:** Malformed/control-prefixed injection cases could pass because the metadata itself was already a finding. Put password and URI injections on their own line and require that exact line in the result in every decoder lane.
- **medium / patched:** The new untracked partial was absent from owning-source ingestion. Added its filename to the canonical ingestion theory so it is scanned before staging.
- **low / patched:** Original-asset controls covered only the OQ8 evidence directory. Added central and framework-reference originals from runtime-corrections, retaining renamed-path and adjacent-credential rejection checks.
- Final focused verification passed 105/105 cases, zero failures/errors/skips. Final Debug project build passed with zero warnings/errors. One new test expectation was corrected: the existing `System.IdentityModel.Tokens.Jwt` property does not itself match a credential name, so malformed package metadata alone need not produce a finding; removed that irrelevant new test input without changing detector semantics. Superseded, red and intermediate runs remain explicitly recorded. Complete reviewed guard verification and baseline inventory are still pending.

- Full baseline diagnosis reproduced exactly 2,812 findings across 670 files: 2,792 NuGet metadata findings plus 20 residual source/report lines. Added runtime-only C# GetString() recognition, reused the existing nine approved fixture identities for xUnit XML escaped display names, and replaced four maintained fixture assignments with runtime RNG values. No production runtime source changed; historical source/report bytes remain intact.
- Residual independent review found a mixed CR/LF XML masking bypass in the initial new helper. Patched XML newline accounting and verified the located raw attribute name, separator and decoded value before masking. Added numeric-entity/mixed-newline and CR-only controls. The independent retained reproduction reports the adjacent credential at line 2 and normal CR-only parsing. **high / patched:** the demonstrated masking bypass no longer occurs; no version containing it was committed or pushed.
- Final focused verification: 960 affected Server cases, eight DomainService cases and three historical-preservation controls passed (971 total; zero failures, errors or skips). Both final Debug test-project builds passed with zero warnings/errors. An initial CA1062 test-parameter error was corrected, and a transient shared-output build-copy warning was eliminated by serialized final builds; attempts remain retained.
- The full intermediate secret suite was red at 16 findings. The final canonical focused rescan of all 670 originally failing paths leaves seven findings in five historical files: five repeated-character assignments and two unapproved URI-bearing XML displays. These have no existing approval and were deliberately retained as findings. No new fixture exception, hash allowance, retirement or historical edit was added. All 666 historical files from the failing inventory and all 502 content-addressed assets are byte-preserved; protected pins, fixed acceptance, original Story 6.4 and sprint files are unchanged.
- **BLOCKER:** This prerequisite is not complete and this spec remains `in-progress`. The strict tracked secret gate cannot be reported green while the seven findings remain. A separate governed disposition under `docs/architecture/evidence-secret-scanning.md` is required; this run supplies none. Story 6.4 remains blocked. No commit, push or package publication occurred.
- Retained evidence: `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-4-prerequisite-secrets-protection-20261018/README.md`, `final-results.json`, before/after path-line inventories, hosted CI/P1R observations, exact command records, final build/test logs, review-control results, source hashes and protected-history checks. Raw artifacts and all attempts remain under `/tmp/story64-secrets-794369b/`, indexed by retained hashes.

- Subsequent user authorization (2026-10-18): "push all changes in submodules and repository then pull". Committed the reviewed EventStore repair with the pinned validator and intact commit hook, then rebased cleanly over unrelated Story 9.1 review documentation, producing `37bdc90a72b9f0ea547852e903334cc8cc541641`. The exact reviewed source/evidence hashes remain unchanged; push to EventStore origin/main succeeded. The parent Git handoff records this gitlink and the validation receipts. The seven historical findings, this prerequisite and Story 6.4 remain blocked. Earlier evidence, package pins and fixed acceptance records remain unchanged; no release/package publication is authorized by this Git operation.


- Successor closure (2026-10-18): the user explicitly selected option 1 for the separate exact five-file governed-retirement proposal. `spec-6-4-prerequisite-governed-retirement.md` implements that later approval without rewriting original evidence or expanding fixture recognition. The strict tracked-content gate now passes; after correcting two unquoted random C# test fixtures, all 940 remaining guard cases pass. This closes the local scanner prerequisite only. Earlier blocked/interrupted/failed results above remain historical facts. Hosted CI/P1R, package publication and Project Story 6.4 acceptance remain pending; original story/sprint and fixed acceptance records are unchanged.
