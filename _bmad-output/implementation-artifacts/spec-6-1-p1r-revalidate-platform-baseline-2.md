---
title: '6.1-P1R Align the Builds Runner with EventStore 3.90.0'
type: 'bugfix'
created: '2026-08-04'
status: 'in-review'
review_loop_iteration: 7
baseline_commit: 'a0dea374b3b990a38e23357934817969ba4a03e4'
context:
  - '{project-root}/_bmad-output/specs/spec-6-1-p1r-revalidation-owner-acceptance/qualification-contract.md'
  - '{project-root}/references/Hexalith.Builds/DEVELOPMENT.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** EventStore source and published packages converge on `v3.90.0` revision `7854f8e51ce9b852bb6c3cac6012670122e93792`, and the Builds catalog selects `3.90.0`, but the Builds runner, schema, fixtures, evidence, and package audit still select `3.88.0`.

**Approach:** Atomically align the Builds runner corpus and complete audit to `3.90.0`, preserve fail-closed stale-pin controls, run focused candidate validation, and append a truthful pending supersession to the P1R evidence record.

## Boundaries & Constraints

**Always:** Record `v3.90.0` and `7854f8e51ce9b852bb6c3cac6012670122e93792` as separate identities resolving to one revision; retain `3.70.1` as rollback and `3.88.0` as superseded; refresh the complete audit; serialize contending lanes; retain first results and fail-closed controls.

**Ask First:** Selecting another baseline; changing EventStore source or the already-aligned Builds catalog; committing, pushing, publishing, releasing, or moving submodule pointers.

**Never:** Rewrite historical evidence; claim P1R/owner acceptance; alter Architecture or Projects planning/status; implement P0 or G-1; weaken exact-pin, audit, fixture, or evidence validation.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Current manifest | EventStore pin `3.90.0` | Runtime, schema, fixtures, evidence, and audit agree | Any mismatch fails validation |
| Stale pin | EventStore pin `3.88.0` or `3.70.1` | Deterministic `HXM016` rejection | Nonzero exit; no unrelated rule contamination |
| Inconclusive command | Restore stalls or is cancelled | Exact result is retained; record stays pending | Never count as pass or acceptance |

</frozen-after-approval>

## Tasks & Acceptance

**Execution:**
- [x] Align `3.90.0` corpus/hashes and CRLF; retain exact stale/negative/evidence contracts; add a positive packaged `module test` path and exact tool-version/revision/invocation binding.
- [x] Audit owned `csproj/props/targets` and verifiable explicit fixtures. Require typed, semantic history; accepted means preserved; round-trip every owner field. Recompute all provenance and cover every generator/validator drift/rejection branch.
- [x] Qualification requires clean fixture bytes versus `HEAD` for release eligibility, genuine JSON types, exact null/numeric outcomes, real CLI help, required controls/evidence coverage, and inventory only after all outputs succeed. External/untracked candidates remain non-release-eligible.
- [x] Publisher requires Boolean `true`, parses every package nuspec ID/version, resolves/parses required evidence and fixture coverage, rejects tampering/bypass/incomplete modes before pushes, and excludes or labels synthetic fixtures. Test all paths.
- [x] Regenerate zero-mismatch audit/ledger and durable logs. Restore `/tmp/p1r-loop4-durable.bdA0hw/qualification-evidence`; retain the final relocatable candidate plus prior bundles byte-for-byte.
- [x] Validator independently rediscovers every tracked `.csproj`/`.props`/`.targets` `PackageReference` relation and requires exact set equality against the audit; no accepted drift between rediscovery and audit output. Implemented in a disposable, unpushed worktree: `validate-package-version-audit.ps1` gained `Get-TrackedProjectFiles`/`Get-PackageReferenceConsumers` plus a new `-ConsumerScanRoot` parameter; every rediscovered identity must have audit evidence or validation fails closed. Scope note: the shared 284-package catalog also serves consumer repositories outside this checkout, so equality is enforced in the direction that actually detects drift (nothing this repository's own tracked files reference may be missing audit evidence), not a literal bidirectional match against catalog rows with no local consumer.
- [x] Semantically validate and exactly round-trip every typed package/family history and owner field — including collection-valued provenance — through the full read/write cycle, not structural/shape checks alone. Implemented via `Assert-PackageRoundTrip`/`Assert-FamilyRoundTrip` (typed read → JSON write → JSON read → re-write, byte-compared) plus `Get-UnknownFieldNames`, which fails closed if any owner-authored field the typed model does not cover would be silently dropped.
- [x] Publisher parses and validates the content of every required qualification-evidence artifact before treating evidence coverage as satisfied; hashes, sizes, and filenames alone are insufficient. Implemented via `Assert-QualificationEvidenceContent` in the new shared `Tools/G4PackageQualification.functions.ps1`, dot-sourced by `test-g4-tool-package-contracts.ps1`; every captured control output is parsed and checked against the real `hexalith-module`/`hexalith-evidence` `--output json` shape (`status`, `outcome.exitCode`, `outcome.ruleId`) before counting toward coverage.
- [x] Prove repository-tracked fixture bytes exactly match `HEAD` at publication time; publication fails closed on any drift between tracked bytes and the published candidate. Implemented via `Assert-TrackedFixtureBytesMatchHead` (byte-exact `git cat-file blob` comparison, stronger than the pre-existing untracked/ignored-only check).
- [x] Require a clean, immutable source tree and bind the qualified source revision/tree state before `releaseEligible=true` can be set. Implemented via `Get-SourceTreeState`; a dirty tree does not abort the run (an external/untracked candidate still completes with full evidence) but permanently forecloses `releaseEligible`, matching the acceptance criterion below.
- [x] Reject swapped or canonical-role-invalid nupkg/snupkg records; add zero-nuspec and multiple-nuspec negative coverage. Implemented via `Get-NuGetPackageRole`/`Assert-CanonicalNuGetArtifact` (exactly one `.nuspec`, correct id/version, `SymbolsPackage` nuspec type required on `.snupkg` and forbidden on `.nupkg`).
- [x] Add exhaustive regression tests for every new rejection path above; retain durable loop-6 logs and inventories alongside the existing loop bundles. The final working-tree suites pass `Tools/test-g4-tool-package-artifact-validator.ps1` at 27 scenarios, `Tools/test-package-version-audit-validator.ps1` at 48 scenarios, and `Tools/test-publish-g4-tool-packages.ps1` across both feed-positive routes plus 22 fail-closed rejection cases. The 24/25 counts recorded inside the retained loop-6 bundle describe its earlier frozen capture, not the final expanded suites; that historical evidence remains byte-for-byte unchanged at `_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-390-loop6-20/` alongside the existing `.8`/`.9`/`.13`/`.16`/`.17`/`.18` bundles.

**Acceptance Criteria:**
- Serial gates, positive CLI paths, and exact controls pass; audit mismatches are zero.
- Failures expose no inventory; only clean tracked source-validating controls are release-eligible.
- Architecture stays `3.70.1`; P1R and all four owner decisions remain pending.
- Package audit set-equality, typed history/owner round-trips, publisher evidence-artifact parsing, tracked-fixture-vs-`HEAD` proof, clean/immutable-tree binding, and swapped/invalid nupkg-snupkg rejection are exact, with exhaustive regression coverage and durable loop-6 evidence retained.

## Spec Change Log

- **2026-08-04, review loop 1:** Audit regeneration replaced curated decisions with generic metadata and falsely named EventStore as a consumer. The contract now requires field preservation, consumer-derived defaults, and generator regression tests. **KEEP:** 3.90 runner/schema/fixture/evidence alignment; exact 3.70/3.88 `HXM016` controls and unrelated rules; recomputed hashes/all-`F` failure; honest first/rerun and package evidence; unchanged Architecture/P1R/owner state; all build, test, audit, and package lanes. Re-derivation must also isolate transient fixtures and strengthen exact-negative and serialized-pin assertions.
- **2026-08-04, review loop 2:** Blind preservation left stale “latest” claims; Evidence negatives were contains-only, unavailable evidence lacked a pin assertion, and inventory preceded remaining controls. Require evidence-aware preservation, exact Module/Evidence contracts, both packaged pin checks, and success-only durable retention; avoid contradictory audits and complete-looking partial evidence. **KEEP:** loop-1 3.90 alignment, hashes, exact stale/module controls, accepted decisions without invented EventStore consumers, 284/139 zero-mismatch audit, honest inconclusive/rerun results, passing build/test/package lanes, and unchanged Architecture/P1R/owners.
- **2026-08-04, review loop 3:** Consumer drift preserved stale acceptance; metadata drift dropped durable constraints; packaged final-state, reused-directory, durable-path, malformed-prior coverage, and CRLF gaps remained. Add provenance-aware preservation, historical-fact merging, fail-closed retention, and exhaustive regressions; avoid stale acceptance and lossy audit refresh. **KEEP:** loop-2 alignment, exact diagnostics/pins, 28/18 audit tests, 284/139 audit, `.3`–`.8` ledger and final pass/hashes, no invented EventStore consumer for its changed family, and pending Architecture/P1R/owners.
- **2026-08-04, review loop 4:** Help probes hit wrapper help; relation hashes, validator proofs, historical fields, modes, post-package failure, and fixture binding were incomplete. Require exact tool/provenance/release contracts and exhaustive regressions; avoid false help passes, stale acceptance, and publishable partial evidence. **KEEP:** loop-3 alignment, CRLF, exact negatives/evidence, 284/139 audit, `.9` pass and 39-file bundle, prior ledger, and pending Architecture/P1R/owners.
- **2026-08-04, review loop 5:** Publisher Boolean/evidence/nuspec checks, tracked-byte cleanliness, history-schema semantics, props consumers, exact owner-field tests, and packaged `module test` were incomplete. Require semantic release proof and exhaustive regressions; avoid wrong-package publication and untyped/stale provenance. **KEEP:** loop-4 3.90 alignment, exact controls/help, 41/27 audit, 284/139 audit, fail-closed `.10`–`.12`, `.13` pass/non-release state, all durable bundles, and pending Architecture/P1R/owners.
- **2026-08-04, review loop 6 (exceptional, human-authorized — exceeds the standard five-loop limit):** Loop 5 review surfaced bad_spec release-boundary gaps that pushed `review_loop_iteration` past 5, triggering the standard HALT-and-escalate. The human explicitly authorized exactly one additional re-derivation to resolve the confirmed gaps: validator `PackageReference` rediscovery lacked exact set equality against the audit; typed package/family history and owner fields, including collection-valued provenance, were not semantically validated/round-tripped; the publisher accepted qualification-evidence artifacts on hash/size/filename alone instead of parsing/validating content; nothing proved repository-tracked fixture bytes matched `HEAD` at publication time; `releaseEligible=true` was not bound to a clean, immutable source tree and the qualified source revision/tree state; swapped or canonical-role-invalid nupkg/snupkg records were not rejected, and zero-/multiple-nuspec negative coverage was missing; and new rejection paths lacked exhaustive regression tests and durable loop-6 logs/inventories. Require all seven fixes with exhaustive regression coverage; if any intent gap or non-trivial specification defect remains after this exceptional iteration, halt and escalate rather than starting loop 7. **KEEP:** loop-5 3.90 alignment (EventStore `v3.90.0` @ `7854f8e51ce9b852bb6c3cac6012670122e93792`), Builds catalog @ `a53166539bf4441d5e33d04281b14c2d59e950c3`, Architecture binding `3.70.1`, all prior evidence bundles byte-for-byte, and pending P1R/four-owner decisions; do not alter Projects planning/status, change submodule pointers, commit, push, publish, release, or claim acceptance.

## Verification

Final verification completed serially with Release restore/build (warnings as errors), all 140 .NET tests, the full packaged-tool qualification, all audit/catalog/publisher gates, durable hashes, diff, CRLF, syntax, and scope checks passing. The actual package audit validates 284 packages, 139 families, and one source with zero mismatches; the final focused suites report 27 artifact-validator scenarios and 48 audit-validator scenarios.

**I/O matrix traceability:**
- **Current manifest:** `PersistedFixtureAssetTests.LoadP0TwoModuleManifestReturnsValidContract` and `ManifestValidationTests.PlatformPinSchemaAndRuntimeValidationRemainInParity` passed in the 108-test Module suite; the full packaged-tool qualification and the 284/139/1 audit validation also passed with EventStore `3.90.0`.
- **Stale pin:** both exact cases of `PersistedFixtureAssetTests.LoadManifestNegativeControlReturnsStableRule` — `superseded-platform-pin.json` (`3.88.0`) and `tampered-platform-pin.json` (`3.70.1`) — passed, asserting nonzero `UsageOrManifest` and the exact expected diagnostic list containing only `HXM016`.
- **Inconclusive command:** `ModuleCommandApplicationTests.CancelledDownWritesCancelledEvidenceAsync` passed, asserting `ToolExitCode.Cancelled`, `HXC130`, retained `finalStatus=cancelled`, and no completed status. A separate record assertion passed for the retained first inconclusive result with no inventory, top-level `pending-acceptance`, and all four owner rows still pending.
