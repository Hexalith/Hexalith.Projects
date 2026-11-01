---
title: 'Story 6.4: finish Builds producer qualification and publication verification'
type: 'chore'
created: '2026-10-31'
status: 'superseded'
superseded_note: 'Remaining work moved to DW-74/DW-75 under Hexalith.Builds by sprint-change-proposal-2026-11-01-close-story-6-4.md'
route: 'dispatch'
story_key: '6-4-resolve-projects-with-transient-current-explanations'
baseline_commit: '8fa68d0c8b8a3d53b4d70e5456dc11084811a9f8'
context: []
---

<frozen-after-approval reason="User authorized this continuation and asks only for decisions that prevent authorized work">

## Intent

Finish the existing producer qualification and verify the independently published Builds release for Story 6.4. Selected existing source is `2186b9a4bc507a116a457c590c80945c2e232cbd`, tree `18d735e56eeb17332e2d2b57a6b116097f42fdec`. Analyzer proposed 4.39.0. Official release run 41172847732 attempt 1 is now completed/success; tag v4.39.0 points to that source. Main has independently advanced to `ff1c2952fa6aec113975bbe78b4d2487d6ec0b3d`; never substitute it for the published source.

## Boundaries & Constraints

Preserve all existing dirty work, newer catalog selections and historical evidence. No staging, commits, pushes, branch/ref changes, nested initialization or dependency updates in real repositories; set `GIT_OPTIONAL_LOCKS=0` for their read-only Git commands. No publication workflow dispatch/rerun or duplicate release. Do not change Platform, consumer pins or adoption. Keep Story 6.4 in-progress and candidateAcceptance false. No invented exception, successor SHA/version or corrected-source repackaging at 4.39.0. Tests may create commits/tags/nested fixture submodules only in command-owned disposable temporary repositories. Read-only official network verification, focused checks, targeted nonrecursive fetch into disposable repositories and existing nonpublishing exact-source CI reruns are authorized.

The implementation subagent owns only the local qualification and npm assessment tasks below. Parent owns official publication observations/bindings, preservation and canonical checkpoint. Do not delegate further. Original detached `/tmp/story64-builds-producer-2186b9a4` has an existing two-null-guard correction: preserve it. For clean exact-source qualification, create a separate command-owned temporary repository from existing objects or official targeted fetch, without modifying real indices/refs/worktrees. Read the baseline already available at workspace `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` after checking root .gitmodules; never discover/load skills in references.

</frozen-after-approval>

## Code Map

- Historical package: `_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/producer-publication-preparation-20261031/README.md`, scoped preparation spec and canonical `spec-6-4-resolve-projects-with-transient-current-explanations.md` latest checkpoint. Read them; do not rewrite historical results.
- New evidence root: `_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/producer-publication-verification-20261031/`. Write local results exclusively in `local/` and npm assessment in `security/`. Parent writes other paths.
- Producer source scripts: `Tools/test-g4-tool-package-contracts.ps1`, `G4PackageQualification.functions.ps1`, builder, artifact validator and control suites; selected repo guidance/props/editorconfig/tests/documentation govern execution. Use normal default output in /tmp to avoid inherited Projects analyzer policy. No -SkipSourceValidation, alternative builder or external fixture proof.
- Historical `qualification/capture.py` provides receipt pattern (argv/cwd/public environment/UTC/exits/hashes). Isolated HOME may repair malformed user NuGet config without modifying it. Reuse installed SDK/PowerShell/cache where safe. Exact commands and unsuppressed failures are required.
- Selected package.json/package-lock.json, actual configured semantic-release plugins and installed npm dependency tree govern applicability. The retained npm audit reports 37 moderate and one high entry, bundled npm/undici 6.29.0. Signature verification alone is insufficient. Assess every unique retained advisory and affected dependency path against actual executable use, including npm commands and release plugins, not merely plugin declaration. Fetch official advisories as needed. Preserve locked dependency files.

## Tasks & Acceptance

- [x] `local/` — run the existing full nonpublishing qualifier at 4.39.0 with `-RequireControls -RetainPackageDirectory` from clean selected source in a fresh disposable repository. Given selected clean source and normal output, when qualifier runs, then record actual exits, source/tests/skips, inventory eligibility, fixture provenance and controls; fail honestly and climb focused fallback ladder if blocked. Local archives cannot supply official publication bindings.
- [x] `local/` — complete existing missing control suites relevant to publication qualification, using official successful run logs to avoid duplicating already completed hosted gates unnecessarily. Given fixture Git writes, then they remain entirely in disposable repositories and fixtures. Retain useful command outputs and exact source/working-tree identities. No guard suppression.
- [x] `security/` — refresh vulnerability audit read-only with exact locked dependencies, explain applicability of unique advisories using official affected/fixed ranges plus actual installed call paths/runtime. Given a reachable vulnerability or insufficient proof, then prepare minimal necessary corrections locally as unapplied patches with concrete verification and remaining blockers. Do not invent an exception or future publication identity. Document what is still vulnerable on disk even when not invoked. No blanket vulnerability-pass claim from signatures, unused-plugin assertion, or successful release.
- [x] `local/README.md` and `security/README.md` — concise actual results, limitations, exact next commands and artifact hashes. Report finished and unfinished work to parent. Parent independently audits these results, binds official packages/inventory/catalog/Tooling and appends the canonical checkpoint.

## Continuation notes

Parent has already refreshed official source/run/tag/release observations under `observations/` and downloaded the official inventory under `downloads/`; do not change them. It reports releaseEligible true, clean exact source, executed/passed source and controls, 292 fixtures and 55 qualification evidence entries; its byte digest is `f07332c8c005bfa91896034933fd556ae0dfef47c3f759f712f797251c8bf555`. Raw release logs prove actual 4.39.0 pre-tag qualification and publication; do not invoke any publisher locally. Complete this bounded package even if an external gate remains blocked, with exact next commands. No full Story 6.4 runtime/owner reruns are in scope.


Final official main observation is `a22b55f0a4f6d2aaa3a5ccc505896064ac595364`, four commits after the selected source, including a later Tenants 5.11.2 catalog/audit update. It is not adopted or substituted for the selected published source. Selected CI/release attempt 1, tag, inventory digest and feeds remain unchanged.


## Implementation Results

The bounded evidence package is implemented. Official 4.39.0 publication is verified from selected source, including exact source/tree/CI/tag, release-eligible inventory digest, both signed tool packages and symbols, repository identities, embedded-versus-evaluated catalog and all three Tooling hashes. Evidence-only binding passes the existing Platform parser; no owner binding/pin/adoption changes occur.

Local qualification is explicitly blocked: three full executions retain their exact failures; final Module result is 2,628 passed, one readiness failure, five skipped, after Evidence 191 passed. Integration/package stages are not reached and no local inventory is emitted. Focused eleven-, four- and six-case passes are separate evidence. Hosted real-version qualification remains 2,821 passed/17 skipped. Source/guidance and original repository state are preserved.

Security assessment covers eight advisories, all 38 locked dependency paths and the actual hosted npm frontend distribution. Security acceptance stays false; the guard patch is a proposal, not a dependency fix or policy exception. Hidden-fixture robustness and audit-guard patches remain unapplied and bound to selected source. No unsupported timeout/source correction, successor SHA/version or repackaging is invented.

The pre-review 49 local and 101 security artifacts and 23 original local/security command receipts verify; the reviewed manifests now bind 70 local and 129 security artifacts, including additional targeted correction evidence. Parent publication verification passes; twelve helper controls pass normally and under Python optimization. Remaining work is precisely documented in the package, with no publishing commands or consumer adoption. Canonical Story 6.4/sprint stay in-progress; this scoped artifact's review status does not promote the story.

## Verification

Use new output paths for replays. Existing source full failures are acceptance blockers, not commands to repeat without a new correction.

- `python3 verify-publication-reviewed.py --source /tmp/story64-producer-verification-1e388du9/source --output /tmp/<new-report>.json`
- `python3 verification-controls-final.py --source /tmp/story64-producer-verification-1e388du9/source`
- `python3 audit-preservation-reviewed.py --output /tmp/<new-preservation-report>.json`
- Root `python3 tools/planning/validate_production_authority.py --story-id 6.4` and `--validate-index`.

All commands above were executed with retained results; current output paths and exact argv are in the package receipts. Review findings must preserve existing failed executions and source identities. No real index/ref mutations or evidence-overwriting replays are authorized.


## Review Triage Log

All three independent layers reported. Verification Gap found no substantiated gap. Every Blind Hunter and Edge Case finding is recorded separately below; duplicates are grouped only after individual verdicts. No deferred finding or acceptance exception is invented.

| ID | Verdict / route | Evidence and correction |
| --- | --- | --- |
| BH1 | medium / patch | Publication replay omitted delivered binding: verify-publication-reviewed.py checks every evidence binding field and requires literal candidateAcceptance:false; three binding-tamper controls pass. |
| BH2 | medium / patch | Catalog receipt was not input-bound: Reviewed replay extracts evaluator/catalog/global configuration from selected Git bytes into disposable scratch and re-evaluates; stale catalog control fails closed. |
| BH3 | medium / patch | Signature result was not archive-bound: Reviewed replay freshly runs dotnet nuget verify for each exact archive, records its input digest and full output, and detects archive changes; corrupt-signature control rejects old success text. |
| BH4 | medium / patch with disclosed historical limit | Commit-only audit missed same-SHA attachment change: Reviewed audit captures and compares symbolic HEAD and HEAD-file digest across the review phase. Initial snapshot lacked those fields; no retroactive proof is claimed. Actual Git fixture control preserves commit/refs/index while changing attachment and is detected. |
| BH5 | medium / patch | Preservation omitted new paths/status deltas: Reviewed audit compares all tracked/nonignored paths and statuses against original capture, allowing only the exact packet/scoped-spec additions and canonical/sprint edits. New unrelated path/status controls pass. |
| BH6 | medium / patch with disclosed historical limit | Protected original detached checkout absent from primary capture: Historical dirty hash/patch, 22 committed inputs and ten raw checkout hashes match. Supplement verifies historical HEAD/status/empty staged paths/submodules; missing historical raw index/ref/symbolic state remains explicit, with new monitoring separate. |
| BH7 | medium / patch | Capture helpers could truncate orphan streams: Executed helpers retained capture-only. local/record-command-reviewed.py refuses every existing component, opens outputs exclusively, and passes orphan/overwrite controls. |
| BH8 | medium / patch | Launch errors lacked receipts: Reviewed recorder emits launch-failed JSON, UTC times, retained streams and nonzero status for missing executable and invalid cwd; nonzero-child propagation also passes. |
| BH9 | low / patch | Patched-version field falsely said same branch: Current applicability.json uses firstPatchedVersion and marks glob/minimatch major upgrades with compatibility requirements. Original metadata remains in applicability-executed-history.json. |
| BH10 | medium / patch | Security reconstruction depended on disposable/user paths: Reviewed reconstruction script has explicit prerequisites, exact selected commit/tree/file hashes, script-disabled audited locked installation and scratch-only patch/replay. One fresh reconstruction passes; both vulnerability guards refuse affected tree. |
| EC1 | medium / patch (duplicate root cause BH7) | Orphan stream truncation boundary: Same exclusive reviewed recorder fixes this independently reported path; original helpers/logs are untouched. |
| EC2 | medium / patch with disclosed historical limit (duplicate BH4) | Symbolic HEAD attachment boundary: Same review-phase HEAD capture/comparison plus honest initial-coverage limit fixes future checking without inventing the missing original observation. |

| F1 | low / patch | Follow-up reviewer found tamper-case label shadowing. The final control program uses helper_name and enforces distinct labels; original executed helper/capture remains unchanged. A new optimized 17-case report supplies individually named results. |

The initial symbolic-HEAD and protected-detached raw-index/ref observations cannot be recreated retroactively. The delivery states their exact coverage limits rather than claiming a complete historical audit. Source/dirty bytes and measured repository identities remain independently checked.


## Delivery State

The reviewed qualification/publication-binding package is delivered with actual local and npm blockers. Canonical Story 6.4, sprint and this scoped continuation remain in-progress as requested. All original twelve review findings and the follow-up label issue are addressed by targeted helpers/controls or expressly stated historical coverage limits. No stage, commit, push, publication, deployment or consumer-pin change is performed.
