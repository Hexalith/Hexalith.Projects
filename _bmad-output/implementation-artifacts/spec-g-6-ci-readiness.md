---
title: 'G-6 CI Readiness for the Approved Current Tuple'
type: 'bugfix'
created: '2026-10-03'
status: 'done'
route: 'dispatch'
baseline_commit: '193fceecab74555f46016b2ab835b37f5673e7e6'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** CI run 37106225245 failed the approved current tuple because PostgreSQL was absent and Commons packing collided at README.md. IDE variables masked packing in the previous local proof.

**Approach:** Provision the pinned image in CI and release, correct Commons/shared packing, and isolate runner build properties. Commit all material fixes, obtain a fresh clean-shell local pre-check, then push submodules before root and use the pushed root's CI result as authority.

**Authorization:** The 2026-10-03 invocation explicitly authorizes these fixes, commits, and pushes only after local validate exits 0. No further tuple approval is needed.

## Boundaries & Constraints

**Always:** Preserve tuple SHA-256 52c8d36a9db4aae09e7a32f7a62be61ed7eee4e0f33a15099215bd61ff780098 and the existing named approval. Preserve real packing, source-mode Debug proof, all validator/gate semantics, exact source binding, and disabled reuse. Commit material inputs before proof. Validate each result before another commit; retain every attempt with byte-preserving attributes and SHA-256 index. Preserve shared containers/processes. Use owning repositories' commitlint. Update only G-6 current_checkout fields after CI prints G6-CURRENT-QUALIFIED, citing its URL, artifact and retained evidence.

**Never:** Change versions, approval, attempt-16, G-4/G-5, P1R or downstream statuses; initialize nested submodules; publish or dispatch release. EOL material fingerprint, Platform app-model startup, and test_g6_packet_references.py are separate deferred work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Fresh runner | Pinned PostgreSQL image absent | Both workflow lanes pull exact fixture digest before live proof | Pull failure stops the lane |
| Umbrella packing | Inherited root README and Commons package import | One packed root README, containing Commons-owned content | Keep packing/warnings enabled |
| Standalone packing | No inherited packed README | Module README remains packaged | Missing files remain failures |
| IDE caller | VS Code/Cursor/VS/ReSharper environment | Non-IDE GitHub Actions build properties, with dedicated source-proof CI unset | Retain sanitized environment controls |
| Failed proof | Failed commands, skipped tests or incomplete cleanup | No push or qualified tracking | Retain failed attempt |

</frozen-after-approval>

## Code Map

- Prior spec Implementation Notes/Review Triage Log and deferred-work.md CI/release/runner entries provide authority; failed evidence is qualification-evidence/g-6-current-20261003/ci-37106225245-1/.
- EventStore Oq8PostgresqlFixture.cs:29,1625 requires postgres@sha256:a02db8cac496f15b094798a38254f14d6e00741f709360e5e00bb6668ea31636.
- .github/workflows/ci.yml g6-current and release.yml fresh G-6 rerun need unconditional digest pulls. Comment the restore-only env -u CI with Jérôme Piquot's 2026-10-03 decision, verbatim “Unset CI in restore (Recommended)”.
- Commons Directory.Build.props must set ProjectRoot after imports, as Builds Samples/Module.Directory.Build.props does. Override alone still duplicates inherited Projects README.
- Builds Hexalith.Package.props must replace existing explicitly packed root README items before adding ProjectRoot/README.md. Filter Pack=true, Filename=README, Extension=.md, explicit root PackagePath (/ or backslash) with HasMetadata and WithMetadataValue item transforms; item metadata Conditions outside targets fail MSB4191. Preserve nonroot/unpacked readmes and other files.
- run_g6_qualification.py Qualification.env currently copies os.environ and removes CI only. Remove IDE indicators (case-insensitive MSBuild property names, all VSCODE_*), TERM_PROGRAM, IDEBuild, BuildingInsideVisualStudio, BuildingByReSharper and caller CI build overrides; normalize GITHUB_ACTIONS=true, TF_BUILD absent. Keep the approved CI removal for source proof. Share this environment across qualification commands.
- run_g6_current.py result environment can retain selected normalized build controls without storing arbitrary environment/secrets. Validator accepts an environment object; do not alter it.
- tests/tools/test_g6_qualification_runner.py, test_run_g6_current.py, test_run_g6_ci_gate.py and run-ci-workflow-gates.ps1 are existing controls. Builds Tools/test_g6_current.py governs tuple semantics.

## Tasks & Acceptance

**Execution:**
- [x] Reproduce core Commons packing before editing in clean shell: env -u CI -u TERM_PROGRAM -u VSCODE_PID -u VSCODE_CWD -u IDEBuild -u BuildingByReSharper -u BuildingInsideVisualStudio GITHUB_ACTIONS=true dotnet build src/libraries/Hexalith.Commons/Hexalith.Commons.csproj --configuration Debug --no-incremental -m:1 -p:UseHexalithProjectReferences=true -v:minimal. Exit 1, NU5118; /tmp/g6-ci-readiness-reproduce-commons.log.
- [x] Workflows -- exact image pulls and owner comment.
- [x] Commons/Builds props -- correct root and inherited README replacement; verify package contents and root-path cases.
- [x] Runner files and focused tests -- normalize build environment, retain controls, prove hostile caller variables cannot disable packing/CI properties.
- [x] Coordinator after implementation/review -- validate commit messages and commit fixes/gitlinks; restore all 11 policy consumers and run fresh clean-shell proof; validate exit 0 before later commits.
- [x] Coordinator -- retain pre-check, fetch/check divergence, push submodules then root, observe exact-SHA g6-current; retain downloaded CI artifact and logs.
- [x] Coordinator after qualifying CI -- update only current_checkout tracking and close these three deferred items, commit and push records.

**Acceptance Criteria:**
- Given the committed fixes and approved tuple, when a fresh clean-shell proof runs, then qualifier 1/1, support 33/33, zero skips, 10 AppHosts and McpCli succeed, all cleanup flags are true and validate exits 0.
- Given pushed root source, when CI g6-current completes, then it prints G6-CURRENT-QUALIFIED and uploads the exact-source artifact before tracking advances.
- Given the final diff, when compared with baseline, then protected historical/status records and tuple/approval are unchanged.

## Implementation Notes

Implementation handoff covers code/configuration and focused verification only. The coordinator owns commits, fresh full proof, push, CI observation and tracking after review. Original scope and conditional push authorization are explicit; no additional approval checkpoint is needed.

Focused verification passed: runner/gate controls 25/25, Builds README archive controls 6/6, current-tuple controls 3/3, workflow policy and production-authority index. The clean-shell Commons build exited 0 with zero warnings/errors; its archive contains one README.md whose bytes match Commons root README. Logs: /tmp/g6-ci-readiness-commons-fixed-20261003-1.log and /tmp/g6-ci-readiness-commons-readme-20261003-1.log. All commits, full proof, push, CI authority and final tracking remain coordinator work.

Review patches preserve same-source nonroot/unpacked README items using metadata matching. Real NuGet verification treats omitted PackagePath as default content packaging; the regression preserves that content and still requires one owned root README. Explicit-empty metadata can create duplicate root ZIP entries before this change, as can ./ and multi-destination shapes; the guard targets the explicit / and backslash roots used by the affected imports. DesignTimeBuild/BuildProjectReferences normalization and actual Commons ownership are covered by the real MSBuild regression, and preflight skip/bypass mutations are rejected.

Final focused verification after review patches: runner/gate controls 25/25 and README package controls 6/6, plus Builds current-tuple controls 3/3, workflow policy, production-authority index and whitespace checks. All review fixes preserve the approved tuple and historical records.

Completion: Builds `688eec9a4333245cc0ff7772115c769094471863`, Commons `116d26815eb81e35b3c161e1799e5ee12805fc0a`, root implementation `455b26fbea4c914cea643f5c37566e91ed3e599c` and root gitlinks `387b5af242bc99f48848f3d0265fa3c38575bc03` were committed with owning-repository commitlint before the fresh proof. The first local restore bootstrap stalled and was stopped using only its owned processes; the second local attempt failed Docker port forwarding before runtime proof, validated exit 1, removed owned containers and preserved shared resources. Both are retained. The third attempt used a fresh isolated cache, a non-IDE shell, serialized restore and disabled node reuse on the same committed root; qualifier 1/1, support 33/33, fixture controls 31/31, strict captures, all 10 AppHosts and McpCli passed. All 18 commands exited 0, zero tests skipped, all cleanup flags true, audit zero issues and local validate exit 0. Local result SHA-256: `b508bb380516ff4ed4f4f6cd63ded19eafe8af833dd4439a4f30dfb3161780a4`.

After checking divergence and commit ranges, Builds and Commons were pushed before root. Authoritative CI [37111866011](https://github.com/Hexalith/Hexalith.Projects/actions/runs/37111866011), job `111171338892`, passed on exact root `387b5af242bc99f48848f3d0265fa3c38575bc03` and printed `G6-CURRENT-QUALIFIED` from both the runner and validator. CI matched the local proof counts, all successful commands and cleanup, with the same tuple, named approval and gitlinks. Its artifact `g6-current-387b5af242bc99f48848f3d0265fa3c38575bc03-37111866011-1` is id `11270158612`, uploaded ZIP digest `sha256:a85f319814a2dbc8c2761909d6f5af99e6498b4b78e516d1fa1330aff5de3cf4`; result SHA-256 `bc04ab1d5ec236db3b7fcdc18b41cb89ef355e3f4731af9f69dcd95e35c7b280`, internal artifactSha256 `48239d5fdeee8a59a42f4031906ac68b6f8b63d7b87111af449d7b6c5a93ff66`. Original ZIP digest, all canonical result digests and bound receipt hashes were verified before current_checkout tracking advanced.

Durable evidence: [qualification-evidence/g-6-ci-readiness-20261003/README.md](qualification-evidence/g-6-ci-readiness-20261003/README.md) and SHA256SUMS. Only the G-6 current_checkout fields advance; the three readiness ledger items are resolved. The approved tuple/policy, attempt-16, G-4/G-5, P1R and downstream statuses are preserved. Release was not dispatched. Existing EOL fingerprint drift, Platform app-model startup and test_g6_packet_references.py remain separate deferred work; no reuse was enabled.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| blind: shared-identity README removal drops nonroot/unpacked items | medium | Reproduced with real MSBuild; identity-only removal drops docs/shared and Pack=false copies. MatchOnMetadata FullPath/Pack/PackagePath preserves both. | patch RC-A |
| blind: omitted PackagePath is mistaken for explicit empty root | medium | The empty metadata filter also selects omitted metadata; omitted destinations are NuGet content assets. Require explicit PackagePath metadata before root selection. | patch RC-A |
| blind: ./ root destination is not recognized | low | Synthetic equivalent-root archive case is real but predates this change; no tracked affected consumer uses this destination. Additional normalization logic is unnecessary for the current failing imports. | reject (rare pre-existing shape) |
| blind: multiple PackagePath destinations retain a root collision | low | Synthetic multi-destination collision predates the fix; no affected tracked consumer uses it. Splitting destination lists would add new shared behavior for an unused shape. | reject (rare pre-existing shape) |
| blind: DesignTimeBuild/BuildProjectReferences remain ambient | medium | A caller can retain these source-build controls; strip them and assert real MSBuild evaluates ordinary source builds. | patch RC-B |
| blind: new preflight requirements can be skipped by if/continue-on-error | medium | Mutating the preflight with if:false passes the current workflow policy; forbid both skip and failure bypass controls on this required step. | patch RC-C |
| blind: synthetic tests do not exercise Commons ProjectRoot ownership | medium | Synthetic ProjectRoot is independent; the actual Commons evaluation can verify its real root and packed README identity. The manual package bytes check passed, but automate that regression boundary. | patch RC-D |
| implementation review: explicit-empty PackagePath produces duplicate root ZIP entries | low | The duplicate archive shape occurs with the original shared props too; HasMetadata excludes empty metadata. No affected tracked README import uses explicit-empty metadata. General destination normalization would add new behavior. | reject (rare pre-existing shape) |
| edge: shared-identity removal drops intended package content | medium | Independent fixture confirms the same RC-A outcome. | patch RC-A |
| verification-gap: actual Commons import ownership lacks regression coverage | medium | Pre-verified: changing Commons ProjectRoot to Projects root leaves existing automated checks passing. Extend the existing real evaluation test. | patch RC-D |

## Verification

- Focused Python runner/gate tests, Builds current-tuple controls and new README packaging controls.
- Clean-shell Commons build and package README bytes check, plus fresh full proof/validate.
- pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1; production-authority index; git diff --check.
