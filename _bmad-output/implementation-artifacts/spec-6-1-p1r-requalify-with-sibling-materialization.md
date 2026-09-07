---
title: '6.1-P1R Preserve the 2026-09-05 evidence and correct the blocker record'
type: 'bugfix'
created: '2026-09-07'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_revision: 'dffdd677e0058240d5951f507cbd2b159b9643a5'
baseline_commit: 'dffdd677e0058240d5951f507cbd2b159b9643a5'
context:
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** 6.1-P1R is the root of the Story 6.1 chain. Its 2026-09-05 attempt left two defects in the evidence trail. First, all of that attempt's command logs, hashes, and result narratives live only in an ephemeral session scratch directory — the record admits this as an open retention gap, and the directory can be pruned at any time. Second, the record's §5 asserts that every EventStore lane is structurally blocked by a nested-submodule boundary conflict needing an EventStore Owner ruling. That is a misdiagnosis: the MSBuild catalog import resolves through a repository-supported *sibling* fallback with no nested submodule, proven end-to-end by attempt-3 on 2026-08-24 and re-verified on 2026-09-07. The attempt placed its EventStore worktrees flat, where no `../references/Hexalith.Builds` existed, so all four import candidates missed. Owners are currently being asked to rule on a blocker that does not exist, while the real one — repository-governance tests reading dependency bytes at `<eventstore-root>/references/<Name>` — sits unnamed underneath it.

**Approach:** Relocate the surviving 2026-09-05 evidence into a durable, git-tracked bundle matching the established pattern, then append a dated correction to the canonical P1R record that re-frames §5 onto the real blocker and states the three owner decisions taken on 2026-09-07. Change no status and qualify no coordinate; this makes the evidence trail durable and the open question answerable.

## Boundaries & Constraints

**Decisions taken 2026-09-07 (record these; do not reopen):** Selected coordinate moves to EventStore `v3.103.0` (`059f6a8917bfab26b85775be464840a1610dfdeb`), superseding the `3.102.0` candidate `3d16d3e0…`. The dependency-bytes route for repository-governance tests is read-only materialization at EventStore's own exact gitlink SHAs inside disposable qualification worktrees — no `git submodule update --init`, no gitlink movement — recorded as an explicit judgement call against the umbrella's nested-submodule rule. The rollback audit gets a second, dependent child commit rather than a change to `validate-package-version-audit.ps1`.

**Always:** Treat the canonical record and `sprint-status.yaml` as append-only. Preserve every prior stopped bundle byte-for-byte. Hash each relocated artifact with SHA-256 and record it in a bundle manifest. List an artifact as absent when it cannot be verified. Make the Builds-repository change as a narrowly scoped, commitlint-valid commit in the repository that owns it.

**Never:** Run any qualification lane, select or qualify a coordinate, push, publish, release, move gitlinks, initialize nested submodules, edit the Architecture Spine, change any `sprint-status.yaml` status field, mark P1R or any downstream gate (P0/P2/P3/P4/Story 6.1) done or accepted, infer owner acceptance, reconstruct or re-derive a missing artifact, alter prior record text, or disturb the user's live checkouts — including the uncommitted `spec-6-5` edit and the four uncommitted submodule pointer moves.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Relocation | Surviving scratch results, `logs/`, manifests | Tracked bundle with `README.md`, every artifact, and a manifest naming each entry's SHA-256 | Unreadable artifact is listed as absent with its expected path; never reconstructed |
| Verification | Relocated bundle | Each entry re-hashes to its manifest value | Any mismatch fails the task and is reported, not silently re-hashed |
| Record correction | Canonical P1R record | New dated section appended; §5 re-framed onto governance-test dependency bytes; three decisions recorded | Prior text byte-identical; a diff touching earlier lines fails the task |
| Status boundary | `sprint-status.yaml` 6.1-P1R entry | Append-only comment pointing at the new bundle | Any parsed-field change fails the task |
| Scratch already pruned | Source directory missing | Task reports the loss explicitly and the record correction says so | Do not fabricate a bundle from the record's prose |

</frozen-after-approval>

## Code Map

- `_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md` -- authoritative record schema (§8 retained hashes, §10 owner table) and the clean-execution protocol the correction must stay consistent with. Do not restate or relax.
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md` -- canonical append-only record. Its 2026-09-05 §5 ("nested-submodule boundary conflict") and §8 (retention gap) are what this spec corrects. Append only.
- `references/Hexalith.Builds` branch `docs/p1r-397-attempt3-record` (`6b42f74`) -- already documents the working sibling route ("repository-supported sibling materialization", restore and build `PASS`). Cite it as the prior proof; do not merge or alter it.
- `/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/` -- source of truth for relocation: `eventstore-3102-selected-result.md`, `eventstore-3701-rollback-result.md`, `builds-3102-candidate-result.md`, `builds-3701-rollback-result.md`, `logs/{eventstore,builds}-{3102,3701}/`, `release-packages-3.102.0.json`, `release-packages-3.70.1.json`, `p1r-diff.txt`, `remote-consumer-3102/`. Verified present 2026-09-07.
- `_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-397-20260824-attempt3/` -- the bundle shape to match, including `README.md` and per-entry hashes.
- `_bmad-output/implementation-artifacts/sprint-status.yaml:294-332` -- 6.1-P1R entry; it already carries an append-only evidence-pointer comment from 2026-09-05. Extend that pattern; change no parsed field.
- `references/Hexalith.EventStore/Directory.Packages.props:5-8` -- the sibling seam behind the correction: `Hexalith2BuildPackageProps` = `$(MSBuildThisFileDirectory)../references/Hexalith.Builds/Props/Directory.Packages.props`. Verified 2026-09-07 to resolve with no nested submodule present.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.Contracts.Tests/Packaging/ReleasePackageManifestTests.cs:669,802` and `Packaging/ContainerPublishingGovernanceTests.cs:616-641` -- the real blocker the correction names: EventStore-root-relative reads plus a `git ls-tree HEAD references/Hexalith.Builds` gitlink assertion.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3102-20260905/` -- create the bundle, copy every surviving artifact from the scratch directory, and write a `README.md` plus a manifest recording each entry's path and SHA-256 -- closes the record's §8 retention gap before the scratch is pruned.
- [x] Same bundle -- re-hash every relocated entry against the manifest and record the verification result -- proves the relocation is faithful rather than merely complete.
- [x] `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md` -- append one dated section: the 2026-09-07 sibling-import verification and its method, the attempt-3 corroboration, the restatement of §5 onto governance-test dependency bytes, the three decisions from Boundaries above, and the relocated bundle's path and manifest hash -- committed narrowly in the owning repository.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- append an evidence-pointer comment naming the new bundle and the correction section; leave `status: open` and every parsed field untouched -- P1R closes only on four-owner acceptance.

**Acceptance Criteria:**
- Given the relocated bundle, when every entry is re-hashed against its manifest, then all values match and any artifact absent from the scratch directory is listed as absent rather than reconstructed.
- Given the canonical record after the append, when it is diffed against its prior revision, then only new trailing lines appear and no earlier byte changes.
- Given an owner reading the corrected §5, when they look for the open question, then it names governance-test dependency bytes and no longer asserts that catalog import requires nested submodule initialization.
- Given the whole task, when the umbrella and every submodule are inspected, then no qualification lane ran, no coordinate was qualified, no gitlink moved, no nested submodule was initialized, and the user's uncommitted `spec-6-5` edit and four submodule pointer moves are exactly as they were.

## Implementation Notes

Executed 2026-09-07.

**Bundle** `_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3102-20260905/`
— 217 files: 211 relocated byte-for-byte, 1 derived (`packages-cache-inventory.txt`),
5 authored (`README.md`, `.gitignore`, `.gitattributes`, `artifact-manifest.sha256`,
`manifest-verification.txt`). The manifest covers 215 — everything but itself and
`manifest-verification.txt`, whose SHA-256
`497bcaf44db6a39b6f198bf1fc6939affa172080497698526167b3b92ca2a9f7` the `README.md`
records, so no bundle file is unattested. Manifest SHA-256
`de3b59d4f4d84b2e568f56a9f1aa0f41119bfaa09beedb7412e1e8b7c89cf423`; re-hash
215/215 OK; copy fidelity 211/211 byte-identical. **No artifact absent**; nothing
reconstructed. The 630 MiB restore cache and ~8.5 GiB of disposable worktrees/SDK
install are labelled deliberate exclusions, with a derived
`packages-cache-inventory.txt` (227 lines) preserving the 215 restored
`<id>/<version>` pairs plus the `dotnet tool install` entry. The retained G-4
packages are relocated and re-hash to exactly the four values the record's §8
already published.

The README deliberately does not quote the manifest hash and
`manifest-verification.txt` deliberately does not quote it either: the manifest
covers the README, so a README quoting that manifest's own hash is the same
unsatisfiable self-reference §7 of the record describes for
`generatedFromRevision`. The manifest hash is attested outside the bundle — in
the record append and the `sprint-status.yaml` pointer.

Two bundle-local dotfiles, scoped to that directory only:
- `.gitignore` — negations for `logs/`, `*.log`, `*.meta`, `*.diag`, `*.nupkg`,
  `*.snupkg`, `bin/`, `obj/`. Without them 127 of the 217 files would be
  untracked (117 under `logs/` via `[Ll]ogs/`, 2 `qualification.log` via `*.log`,
  8 packages) — the state the three earlier `6-1-p1r-397-*` bundles are in.
- `.gitattributes` — `* -text` so a clone with `core.autocrlf=true` cannot rewrite
  line endings and break every text manifest entry (the umbrella `.gitattributes`
  is only `* whitespace=cr-at-eol` and pins no text/eol handling), plus
  `-whitespace` on the relocated subtrees so the trailing whitespace and
  blank-at-EOF that five relocated evidence files genuinely contain are never
  "fixed". After it, `git diff --check` reports 0 offenders across the bundle.

**2026-09-07 import verification** — evaluation-only, no lane. Read
`Directory.Packages.props` at `v3.103.0`/`v3.102.0` (both SHA-256 `4fe25096…`)
and `v3.70.1`; built three synthetic layouts from read-only `git show`/`git archive`
extracts; evaluated `dotnet msbuild Probe.csproj -getProperty:HexalithEventStoreVersion`.
Flat → exit 1, `MSB4019` on the unconditional 4th candidate (reproduces the
2026-09-05 `3.102.0` lane). `<lane>/EventStore` + `<lane>/references/Hexalith.Builds`
→ exit 0 via candidate 2. `<lane>/references/Hexalith.EventStore` +
`<lane>/references/Hexalith.Builds` (the DW-68 layout) → exit 0 via candidate 3.
All with zero nested submodules initialized.

**Record correction** — two commits on new local branch
`docs/p1r-3102-correction-20260907` (based on the checkout's `071ef99…`,
unpushed, tree clean, commitlint exit 0 on both):
- `18b1bdac3fe96f07ea4cb65dc045e51578d086cb` — sections A–G: relocation, the
  verification method, attempt-3 corroboration, the §5 restatement onto
  governance-test dependency bytes, the three decisions, observed state changes,
  non-closure. 277 insertions, 0 deletions.
- `5d4414a` — sections H–L: the §B log attribution fix (only
  `logs/eventstore-3102/` holds `NU1010`/`MSB4019`; `logs/eventstore-3701/` holds
  none and all 12 of its `.meta` rows are `EXIT=0`), the reconciliation of the
  3701 lane's `00b-submodule-init.log` and its 12 passing rows against decision 2,
  both working harness layouts with the candidate each resolves through, and the
  superseded first-append values. 146 insertions, 0 deletions.

**The `references/Hexalith.Builds` checkout was returned to `main` at
`071ef99733ba398362ae838e4696b4b1641ed07a`** after committing, so the umbrella
gitlink is exactly the user's pre-work state and no clone or CI
`git submodule update` can be pointed at an unfetchable SHA. The two commits live
only on the branch, as the earlier `docs/p1r-397-attempt3-record` and
`docs/p1r-3102-3701-record` record branches do. **The correction sections are
therefore not in the working checkout until that branch is merged.**

**Other umbrella edits** — `qualification-evidence/README.md` gained index rows
for the new bundle and for `attempt2`/`attempt3`, which the index had never
listed. `deferred-work.md` DW-68 was reshaped to the ledger schema (`origin`,
`location`, `source_spec`, `severity`, `reason`, `status`, `decision`): the
bespoke nested `owner_decisions_2026_09_07` mapping and the `notes` key are gone,
`gate: 6.1-P1R` is dropped because P1R is a planning gate rather than a story-key
slug (DW-35 carries none for the same reason), and the entry now states that it
extends still-open DW-35 and that its dependency-materialization decision
restates DW-35's 2026-08-25 "External exact worktrees" decision rather than being
new authority.

**Sprint status** — 31-line append-only comment after the existing 2026-09-05
pointer. `yaml.safe_load` of the file before and after is `==`, so no parsed field
changed; `status: open` intact.

**Observations recorded, not acted on:** the `3.102.0` candidate `3d16d3e0…` was
merged to Builds `main` by separate user work (`aee36f4`) after 2026-09-05, so
§9's "unmerged" is a correct as-of-2026-09-05 statement; `origin/main`'s
`7b0b183` already bumps the catalog to `3.103.0` but leaves
`SupportedPlatformPins.EventStoreVersion` at `3.102.0`, so the runner/schema/
fixture/evidence rebind required by decision 1 is outstanding; six of the seven
gitlink SHAs `v3.103.0` names are already present in the umbrella's module object
stores, only `Hexalith.PolymorphicSerializations` has no local repository.

## Verification Results

| Check | Command | Result |
| --- | --- | --- |
| Manifest re-hash | `sha256sum -c artifact-manifest.sha256` | 215/215 OK, 0 failed |
| Copy fidelity | per-file `sha256sum` + `diff -rq` vs. scratch | 211/211 identical |
| Prior bundles preserved | `sha256sum -c` on the three `6-1-p1r-397-*` manifests | all OK |
| Bundle trackability | `git add -A -n` vs. `find -type f` | 217 of 217; `git check-ignore` matches none |
| Bundle whitespace | `git diff --no-index --check` over every bundle file | 0 offenders |
| Record additions-only | `git diff --numstat` per commit | `277 0` then `146 0`; zero removed lines |
| Prior record bytes | `head -1165` / `head -1442` \| `sha256sum` | match each pre-append file hash |
| Builds repo state | `git log --oneline -1`, `git status --porcelain=v1` | two commits on the branch, clean tree, no upstream, checkout back on `main` at `071ef99` |
| Commit messages | `git log -1 --format=%B \| commitlint` | exit 0 (0 problems) on both |
| Sprint status parsed fields | `yaml.safe_load` before vs. after | identical |
| Deferred-work schema | field-key census across all 68 entries | DW-68 uses schema keys only; 68/68 entries have `location:`; no open entry uses `resolution:` |
| Umbrella state | `git status --porcelain=v1`, `git submodule status` | user's `spec-6-5` edit and all four submodule pointer moves intact and unchanged, Builds pointer back at `071ef99` |
| Nested submodules | `git submodule status` in all 9 submodules | none initialized anywhere |
| EventStore checkout | `git -C references/Hexalith.EventStore status/rev-parse` | clean, still `034c177e…` |

## Spec Change Log

## Review Triage Log

| # | Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- | --- |
| 1 | Umbrella gitlink moved to `18b1bda`, which exists only on a local unpushed branch | high | `git branch -a --contains 18b1bda` -> only `docs/p1r-3102-correction-20260907`; `origin/main` = `7b0b183` does not contain it. A clone or CI `git submodule update` cannot fetch it. Also leaves the submodule on a feature branch. | patch |
| 2 | Record SSB attributes `NU1010`/`MSB4019` to both `logs/eventstore-3102/` and `logs/eventstore-3701/` | high | `grep -rl 'NU1010\|MSB4019' logs/eventstore-3701/` -> 0 files; `logs/eventstore-3102/` -> 12. All twelve `eventstore-3701` `.meta` rows record `EXIT=0`. | patch |
| 3 | Retained `logs/eventstore-3701/00b-submodule-init.log` shows `git submodule update --init` and a fully passing lane; correction narrative never reconciles it | high | Log records all seven submodules cloned; all 12 source- and package-mode rows exit 0, including the governance tests decision 2 treats as unsolved. Material to that decision. | patch |
| 4 | Counts wrong: "213 relocated / 213 identical" (actual 212), "96 would be untracked" (actual ~127), "222 cache pairs" (inventory has 227 lines, 1 tool entry) | medium | 216 files - 4 authored (README, `.gitignore`, manifest, verification) = 212 relocated; manifest holds 214 entries. Counts appear in README, record SSA, and Implementation Notes. | patch |
| 5 | Harness layout ambiguity: DW-68 and Code Map say `<lane>/references/Hexalith.EventStore`; record SSB verified `<lane>/EventStore` | medium | Path math: `<lane>/references/Hexalith.EventStore/../references/Hexalith.Builds` = `<lane>/references/references/Hexalith.Builds`, which does not exist -- that layout resolves via candidate 3, not candidate 2. Both layouts work, via different candidates; the docs conflate them. | patch |
| 6 | No bundle `.gitattributes`; manifest breaks on any clone with `core.autocrlf=true`; 5 files carry whitespace errors | medium | Root `.gitattributes` is one line (`* whitespace=cr-at-eol`), no `text`/`eol` pinning; relocated logs are LF. 5 bundle files flagged by `git diff --check --no-index`. | patch |
| 7 | Bundle `.gitignore` negations omit `*.meta`, `*.diag`, `bin/`, `obj/`; comment cites a `[Pp]ackages/` rule that does not exist | medium | 28 `.meta` files survive only incidentally via `!logs/**`; a future `.meta` outside `logs/` would be silently untracked. | patch |
| 8 | `manifest-verification.txt` is the only bundle file no manifest entry covers | low | 216 files, 214 entries; the two uncovered are the manifest and the verification record. The artifact attesting verification is itself unattested. | patch |
| 9 | DW-68 drifts from ledger schema: no `location:`, bespoke nested mapping, `gate: 6.1-P1R` is not a story-key slug | medium | 67 of 68 entries carry `location:`; every other `gate:` is a slug. Gate-keyed sweeps (`bmad-loop-sweep`) will not resolve it. | patch |
| 10 | DW-68 duplicates still-open DW-35, whose 2026-08-25 `decision:` already authorized the same materialization route | medium | DW-35 reads: "materializes every recorded dependency gitlink as an isolated exact-revision worktree at the paths required by governance tests without nested submodule initialization". Decision 2 restates it rather than creating it. | patch |
| 11 | `qualification-evidence/README.md` index omits the new bundle and attempt2/attempt3 | low | Index table stops at `6-1-p1r-397-20260824`. New bundle undiscoverable from the directory's own index. | patch |
| 12 | Record cites a bundle that is still untracked while the Builds commit is already made | low | `git ls-files` on the bundle returns 0. Resolves when the user commits the umbrella, but the record should say so. | patch |
| 13 | `spec-6-5` reads `blocked` while `sprint-status.yaml:140` reads `backlog` | medium | Real disagreement, but the edit is the user's pre-existing uncommitted work, explicitly fenced out by this spec's Never list. Not caused by this change. | defer |
| 14 | Prior `6-1-p1r-397-*` bundles keep their `logs/` untracked | medium | `git ls-files` returns 0 tracked files under each; 23 log files perishable. Pre-existing; this spec forbids touching prior bundles. | defer |
| 15 | No executable check re-hashes bundle manifests; nothing enforces durability | medium | No script in `tests/tools/` or `.github/workflows/` reads a bundle manifest. Real gap, but building a repo-wide harness exceeds this spec's intent. | defer |
| 16 | Relocated bundle has no machine-readable command ledger, unlike the attempt-3 bundle it was told to match | low | Attempt-3 carries `command-ledger.jsonl`; the 2026-09-05 scratch never produced one. Caused by that attempt, not this change; fabricating one would violate the no-reconstruction rule. | defer |
| 17 | 8 binary packages committed with no LFS policy; fourth retained bundle, growth unbounded | low | Pre-existing pattern across all four bundles; no retention policy stated anywhere. | defer |
| 18 | Relocated evidence embeds 4,307 machine-specific `/home/administrator` paths | false | The intent mandates byte-for-byte relocation and forbids reformatting; normalizing the paths would violate it. Not a defect of this change. | rejected |
| 19 | Three byte-identical release manifests stored under version-specific names | false | They are relocated verbatim artifacts; dropping or renaming copies would break the byte-for-byte fidelity the bundle exists to provide. The observation describes the source evidence, not this change. | rejected |

## Design Notes

The correction's value is that it changes what owners are asked. Attempt-3 (2026-08-24) already proved the sibling route: the mandated Debug source-mode restore and serialized build both `PASS` through a sibling catalog, and the lane then stopped at `es-source-003-contracts-test` with 29 failures whose earliest causes are absent nested dependency bytes. Import and compilation are solved; repository-governance tests are not. Leaving §5 as written would send the next attempt back through a full flat-layout lane to rediscover a misdiagnosis.

## Verification

**Commands:**
- `sha256sum` over each relocated entry compared to the bundle manifest -- expected: every value matches.
- `git diff` of the canonical record against its pre-append revision -- expected: additions only, no modified or deleted lines.
- `git -C references/Hexalith.Builds log --oneline -1` and `git -C references/Hexalith.Builds status --porcelain=v1` -- expected: one new commitlint-valid commit, clean tree, no branch pushed.
- `git status --porcelain=v1` in the umbrella -- expected: the pre-existing `spec-6-5` edit and four submodule pointer moves, plus only this spec's intended additions.
- `git submodule status` and a check for initialized nested submodules -- expected: unchanged gitlinks, no nested submodule initialized.
