# 6.1-P1R Builds Loop-6 Gap-Closure Evidence (`.20`)

This directory is a durable copy of the loop-6 qualification evidence captured on
2026-08-05. It is **candidate evidence, not P1R or owner acceptance**. Architecture
remains bound to `3.70.1`; the immutable qualifying Builds revision, reciprocal
rollback, EventStore source/package-mode qualification, and all four owner
decisions remain pending. Nothing here was committed, pushed, merged, or used to
move any submodule pointer in the tracked repositories.

## What this bundle proves

Per the spec's loop-6 scope (`spec-6-1-p1r-revalidate-platform-baseline-2.md`,
tasks 6-12), a **disposable, unpushed** `git worktree` of `Hexalith.Builds` was
created at the currently-tracked revision
`a53166539bf4441d5e33d04281b14c2d59e950c3` (HEAD of `references/Hexalith.Builds`,
untouched throughout — see "Scope and honesty" below). Inside that worktree only:

1. The runner corpus (platform pin constant, manifest schema, fixtures, and C#
   tests) was aligned from the stale `3.88.0` pin to the catalog's already-`3.90.0`
   pin, and a new `superseded-platform-pin` fixture pair was added so the runner
   explicitly rejects `3.88.0` with `HXM016` in addition to the existing `3.70.1`
   rollback-rejection fixture.
2. `Tools/package-version-audit.json` was regenerated for real against live NuGet
   metadata (`Tools/audit-central-package-versions.ps1`, network-verified), closing
   the pre-existing 33-package drift (13 EventStore rows plus 20 unrelated
   packages) between the audit and the catalog. The regenerated audit passes
   `validate-package-version-audit.ps1` at **284 packages, 139 families, 1 source,
   zero mismatches**.
3. `Tools/validate-package-version-audit.ps1` gained two new checks:
   - independent `PackageReference`/`GlobalPackageReference` rediscovery across
     every Git-tracked `.csproj`/`.props`/`.targets` file, failing closed if
     anything actually consumed lacks audit evidence (a new `-ConsumerScanRoot`
     parameter lets tests isolate this from the real repository tree);
   - a full typed read → JSON write → JSON read round-trip for every package and
     family record, including their collection-valued fields (`sourceResults`,
     `packageIds`, `representativeConsumers`), plus detection of any
     owner-authored field the typed model does not yet cover (which a
     shape-only check would silently drop).
4. A new shared file, `Tools/G4PackageQualification.functions.ps1`, adds: content
   parsing/validation of every qualification-evidence artifact (not just its hash,
   size, and filename); a tracked-fixture-vs-`HEAD` byte proof (stronger than the
   pre-existing untracked/ignored-only check — it also catches a tracked file that
   was locally edited); clean/immutable source-tree binding; and nupkg/snupkg
   canonical-role validation (exactly one `.nuspec`, correct id/version, and the
   `SymbolsPackage` nuspec type on the `.snupkg` only). `Tools/test-g4-tool-package-contracts.ps1`
   dot-sources this file and now writes a `g4-tool-package-inventory.json` with a
   `releaseEligible` boolean that is `true` only when source validation ran
   (never `-SkipSourceValidation`), controls ran, the source tree was clean, and
   every fixture/artifact/evidence proof above passed; a positive packaged
   `hexalith-module test` control was also added (task 1's still-missing packaged
   path).
5. A new regression suite, `Tools/test-g4-tool-package-artifact-validator.ps1`,
   dot-sources the same shared functions (not a driftable copy) and exercises 24
   scenarios: valid/zero-nuspec/multiple-nuspec/swapped-role/wrong-identity nupkg
   and snupkg archives; well-formed/wrong-ruleId/falsely-passed/non-JSON/empty/
   missing-status/missing-ruleId qualification evidence; clean/dirty/non-Git
   source trees; and byte-identical/edited/absent-at-revision tracked fixtures.
   `Tools/test-package-version-audit-generator.ps1` was given a one-line
   `-ConsumerScanRoot` isolation fix so its pre-existing 3-package fixture
   scenario is not affected by the new PackageReference-rediscovery default.

## Captured lane (this bundle)

- Command version: `0.0.0-p1r390-loop6.20`
- Disposable worktree revision (never pushed, no branch): `da6490d941878baf9688b89df831cf8a3b128a78`
- Worktree base (unchanged tracked revision): `a53166539bf4441d5e33d04281b14c2d59e950c3`
- Exit/result: `0` / `PASS`
- `releaseEligible`: `true` (source tree was clean *relative to the disposable
  worktree's own local commit* — see "Scope and honesty")

## Durable files

| File | SHA-256 | Meaning |
| --- | --- | --- |
| `g4-tool-package-inventory.json` | `a93010ffeee7404759c8dc8f2f728f110abc73eeb576dbce1b3e1acc94533e4b` | Full inventory: 2 packages (4 artifacts), 34 content-validated qualification-evidence entries, fixture proof counts, bound source revision, `releaseEligible=true` |
| `qualification.log` | `69b108405bdd515641d0cd9bfaba86d936064a17643833ac686703f3260e3acc` | Exact console output of the official gate run that produced the inventory above |
| `final-regression-battery.log` | `6cea143c1059d9a9b2d03d142e23a9679c02d632b4d995575fc438f068a19df0` | Build, all three test projects, `validate-package-version-audit.ps1` against the real regenerated audit, `test-package-version-audit-validator.ps1` (25 scenarios), `test-package-version-audit-generator.ps1` (14 scenarios), `test-authoritative-package-catalog.ps1`, `validate-consumer-package-authority.ps1`, and `test-g4-tool-package-artifact-validator.ps1` (24 scenarios) — all `PASS` |
| `source-changes.diff` | `ce0da3cf8ab220754480a3faaebb115d84ba9394f6598a6ecbb1bbeef44e661d` | Full unified diff, `a5316653..da6490d9`, 31 files changed / 1549 insertions / 381 deletions — every source change described above, reviewable in full |
| `source-changes-stat.txt` | (see file) | `git diff --stat` summary of the same range |
| `Hexalith.Builds.{Module,Evidence}.Cli.0.0.0-p1r390-loop6.20.{nupkg,snupkg}` | recorded in the inventory | Disposable candidate G-4 tool packages; not publication artifacts |
| `qualification-evidence/` | recorded in the inventory | 34 content-validated control outputs (packaged down/unavailable/test/readiness, all module and evidence negatives) |

## Scope and honesty

- **The tracked `references/Hexalith.Builds` submodule checkout was never
  modified.** `git status --porcelain=v1` in that checkout was clean before,
  during, and after this work. All source changes above exist only in a disposable
  `git worktree` created under this session's scratchpad directory and removed
  after evidence was captured.
- The disposable worktree required one local commit (`da6490d9`, on a detached
  `HEAD`, never a branch, never pushed) so the new tracked-fixture-vs-`HEAD` byte
  proof and clean/immutable source-tree binding had a coherent revision to check
  the working tree against — the same "qualifying Builds revision" pattern used by
  every prior P1R loop's evidence (e.g. loop 1's `4351d7c...`), none of which were
  ever merged into the tracked branch either.
- `releaseEligible=true` in this bundle's inventory is **candidate evidence about
  this disposable worktree's own internal consistency**, not a release decision
  about `references/Hexalith.Builds`. It proves the new gates work exactly as
  designed (see the negative-path proof below) — it does not and cannot make a
  claim about the real repository, since that repository was never touched.
- A separate run of the same gate with `-SkipSourceValidation` was also captured
  during development (not retained here) and correctly produced
  `releaseEligible: false` with reason `"source validation was skipped
  (-SkipSourceValidation)."`, proving the soft-ineligibility path fires instead of
  silently passing.
- **Not attempted in this loop:** EventStore source-mode/package-mode dual builds,
  the 14-package remote NuGet consumer restore, the reciprocal `3.70.1` rollback
  worktree, and the four-owner acceptance table. These remain exactly as pending
  as the qualification-contract and prior loop evidence already record. This
  loop's scope was the seven Builds-runner/audit/publisher gaps named in the
  spec's loop-6 change-log entry, not a re-run of the full multi-phase
  qualification-contract.
- No commit, push, publish, release, or submodule-pointer change occurred against
  any tracked repository. Architecture, `_bmad-output/planning-artifacts/`, and
  sprint/readiness status were not modified by this work.
