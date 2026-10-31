---
title: 'Story 6.4: Builds producer qualification and publication approval package'
type: 'chore'
created: '2026-10-31'
status: 'in-progress'
route: 'dispatch'
story_key: '6-4-resolve-projects-with-transient-current-explanations'
baseline_commit: '5724c2400c102c466247890c05bbbdabb898e7a4'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="User explicitly authorizes this preparation and permits questions only for unresolved blocking decisions">

## Intent

Prepare a concrete producer publication approval package for Story 6.4. Qualify an existing exact Builds SHA, compute its version with the real release analyzer, and retain actual results and blockers. Canonical Story 6.4 remains in progress. This scoped preparation may finish with publication blocked; it must never claim that blocked qualification passed.

## Boundaries & Constraints

**Always:** Preserve original dirty work, newer catalog selections and historical evidence. Selected producer is existing official `2186b9a4bc507a116a457c590c80945c2e232cbd`, observed main on 2026-10-31. It succeeds historical `6cc96b4ff23c40c77651e1e4f85ed3170c25c7a4` by fifteen commits; exact-live-tip release guard makes the old SHA ineligible while main differs. Explicit catalog changes are FrontComposer 4.7.1→4.7.2, AngleSharp 1.8.6→1.9.0, JsonSchema.Net 9.5.0→9.6.0; all other selections survive. Use the isolated detached worktree `/tmp/story64-builds-producer-2186b9a4` already created from that exact official object. Source qualification binds that SHA even if remote main advances. Read baseline from original workspace AI.Tools and ordinary owner guidance; never load skills under references. Read-only GitHub/NuGet, targeted nonrecursive official-object fetching, focused local checks and rerunning existing nonpublishing exact-source CI are authorized. Prepare necessary producer corrections locally, preserving exact-source failure and identifying any corrected tree separately.

**Never:** Stage, commit, push, create branches/tags, publish, deploy, dispatch release/publication, initialize nested submodules, suppress guards, invent a version/hash or swap source under a published version. Do not modify Platform bindings/pins or adopt the new producer in consumers. No unrelated full Story 6.4 reruns. Do not alter the user's NuGet configuration; provide a valid isolated config through HOME if essential and retain both blocker and retry.

</frozen-after-approval>

## Code Map

- Start: `evidence/6-4-advisory-resolution-20261021/current-tuple-preparation-20261030/parent-final/README.md`; canonical `spec-6-4-resolve-projects-with-transient-current-explanations.md` last checkpoint. Preserve all original bytes and main story baseline.
- New evidence: `evidence/6-4-advisory-resolution-20261021/producer-publication-preparation-20261031/`. Parent owns official observations, original-state audit, canonical checkpoint/sprint comment and final approval README. Implementation owns `qualification/` and `analyzer/` beneath it plus `implementation.md`.
- Selected owner `.github/workflows/build-release.yml`, `package.json`, `package-lock.json`, `Tools/verify-g4-release-source.sh`, `Tools/verify-g4-tool-release.ps1`, `Tools/test-g4-tool-package-contracts.ps1`, `Tools/G4PackageQualification.functions.ps1` and official builder/publisher own gates. CI inventory uses SkipSourceValidation and cannot earn release eligibility.
- Analyzer: use actual pinned semantic-release `get-last-release.js`, `get-commits.js`, `get-next-version.js`, and actual `@semantic-release/commit-analyzer` default configuration from selected package.json. Bind selected tip, all official reachable stable tag observations and exact previous tag source. No full configured dry-run: it reaches push verification and credential-dependent hooks. npm dependencies may be restored with selected lock, no dependency updates. Parent has official latest v4.38.3 tag at `d1931b1e803144048d927fff16cb8119afde541e`; independently verify reachable tag set before selecting previous release.
- Original owner `/home/administrator/projects/hexalith/projects/references/Hexalith.Builds` stays clean at `1160a3a7ddc0f4c6927456b5735a2963261016a0`. Parent snapshots thirteen original repositories. Detached checkout has no initialized nested submodules.

## Tasks & Acceptance

- [x] `analyzer/` — retain executable analysis script, exact selected inputs, official tag/source verification, actual analyzer/version-module results and logs. Given real history/config, proposal must derive from actual modules, with no lifecycle execution.
- [x] `qualification/` — assess transitive fixture behavior, run authorized focused source/release/catalog stages and official package builder, retain exact argv/cwd/public environment/UTC/exits, four actual archives and unsuppressed failures/fallbacks. No skip/custom builder/external qualification fixture clears producer eligibility.
- [ ] Full producer source/package qualifier with `-RequireControls -RetainPackageDirectory` — unexecuted pending fixture-Git scope answer; no release-eligible inventory or full qualification pass is claimed.
- [x] Necessary local producer corrections — only demonstrated issues within qualification scope; retain minimal patch, source fingerprints and focused existing checks. Never attribute corrected dirty-tree results to selected SHA or manufacture new recorded identity.
- [x] `implementation.md` — concise evidence/gate outcome table, exact source/catalog/release-input hashes, proposal, remaining blockers, required commands and publication prerequisite order. No credentials or private logs in evidence. Parent final review/checkpoint remains separate.

Given no passing complete producer qualification, approval package must clearly say publication blocked, identify precise next correction/qualification action, and still supply concrete conditional release commands. Given external head advancement, retain selected source and require reconciliation before later dispatch. Parent will verify live source/CI separately; do not rerun CI or write remotely from implementation.

## Implementation Notes

No unresolved human decision prevents authorized preparation. User authorization overrides generic intermediate approval, dirty-tree halt, no-remote and completion-promotion instructions solely within this scope. Do not delegate further. Leave canonical Story 6.4 and sprint in progress.


## Implementation Results and Review Scope

The concrete package is prepared at [the producer receipt](evidence/6-4-advisory-resolution-20261021/producer-publication-preparation-20261031/README.md). Actual analyzer proposes 4.39.0; official exact-source CI passes. Source Release build and safe source/control checks pass with explicit live/hosted skips. Four original unsigned archives are retained, but their builder failed only in the inherited strict output context and emitted no inventory. Original isolated loader and a separately fingerprinted two-null-guard correction both pass their own focused controls.

Full qualifier invocation remains **unexecuted**, pending the user's scope clarification for disposable Git fixtures. No release eligibility is claimed and that execution task remains open. The package explicitly retains the accidental transitive fixture-commit deviation and independent preservation audits. npm signature verification passes; separate vulnerability audit exits 1 with 37 moderate and one high vulnerable dependency entry. No lockfile changes or applicability/exemption decision was invented. Independent release run 41172847732 was observed, never dispatched or approved here; duplicate dispatch is prohibited.

Review covers the preparation artifacts, conditional publication commands, actual-source fixture patch, result/identity claims and checkpoint changes. Original dirty Platform preparation and historical evidence remain context, not new changes. All original story frontmatter/frozen/prefix bytes remain preserved. User's instruction to retain actual blockers overrides generic completion/local-commit promotion: canonical Story 6.4 and this scoped producer qualification remain in progress when this package is delivered.


## Review Triage Log

All three independent layers reported before triage. Verification Gap reported no gaps. Each Blind Hunter finding and the separate Edge Case finding has an individual verdict before grouping.

| Finding | Verdict and disposition |
| --- | --- |
| BH1 dispatch proceeds after failed shell tests | medium / patch — confirmed plain Bash continues after failed tests; add fail-closed shell options to every block and exercise the exact dispatch block using process doubles. |
| BH2 analyzer reads mutable config/modules | medium / patch — original captured hashes match selected source, but standalone replay lacks its own precondition enforcement. Preserve original script/result; add separately bound replay checks. |
| BH3 analyzer can overwrite result | medium / patch — confirmed writeFileSync default truncates; separate replay must require fresh output and refuse overwrite. |
| BH4 orchestration returns success on child failure | medium / patch — loop has no failure exit. Preserve executed script; corrected replay propagates failure and names its reviewed scope. |
| BH5 interrupted child cleanup unproved | medium / patch — original child exit was not observed; retain that unknown and add scoped current process audit. Current audit finds no matching owned processes, without claiming immediate historical cleanup. |
| BH6 receipt checker omits analysis/input/manifest digests | medium / patch — shape checks alone do not bind those data; add separate stronger checker and tamper controls. |
| BH7 operational prerequisites omit npm observation | medium / patch — add explicit remediation/applicability prerequisite, distinct from signature success; invent no policy exception. |
| BH8 deviation describes corrected checkout as unchanged | low / patch — qualify the clean state as immediately after deviation audit, then identify later dirty fixture correction. |
| BH9 redundant generated payload footprint | low / patch — losslessly archive redundant expansions with every file hash and reconstruction command; original archives, unique proof and all 116 bound implementation files remain directly available. |
| BH10 tasks unchecked | low / rejected as spec-only review request — normal task verification separately marks completed preparation and leaves full qualifier explicitly unchecked. No gate is promoted. |
| EC1 child failure propagation | medium / patch — independently reproduced by review with all sixteen process-double children returning 1 while original helper exits normally. Same root cause as BH4; one corrected replay fixes both. |

Surviving patch groups are fail-closed operational commands, pinned/nonoverwriting analysis replay, control failure propagation, stronger receipt binding, honest cleanup limits, and accurate security/state/storage documentation. Original executed evidence remains immutable. No finding justifies a new publication authority or completion claim.


## Review Resolution

All surviving review groups are resolved in separately named replay/checker helpers and operational documentation. Twenty focused helper controls and three exact-dispatch process-double controls pass. Parent independently verifies source hashes, all twenty control exits/stream digests, shell syntax and direct current blocked-state integrity. All 116 original implementation files and the trusted original manifest remain byte-identical; no historical result is rewritten. The interrupted child's immediate historical exit remains explicitly unknown, while a later scoped process inspection finds no matching owned capture/control process. Raw redundant expanded payloads are losslessly archived and every archived byte verified before removing only the new expansion copies.

The npm audit remains exit 1 and unresolved; signature verification is a separate pass. Full producer qualification remains unchecked and unexecuted pending the user scope answer. Independent release run is observed only; there is no dispatch or approval from this session. Preparation/review is complete, while canonical Story 6.4 and this scoped producer qualification stay in progress. No staging, commit, tag, push or promotion follows the generic workflow presentation step.
