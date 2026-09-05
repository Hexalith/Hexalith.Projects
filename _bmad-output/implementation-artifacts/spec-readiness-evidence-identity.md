---
title: 'Bind Readiness Evidence to Exact Input Identities'
type: 'bugfix'
created: '2026-09-05'
status: 'done'
review_loop_iteration: 0
followup_review_recommended: false
baseline_revision: '8f114abfba3d39fa3d1cf61eb7caba29da1e7efa'
baseline_commit: '8f114abfba3d39fa3d1cf61eb7caba29da1e7efa'
context:
  - '{project-root}/references/Hexalith.Builds/DEVELOPMENT.md'
warnings: []
deferred:
  - summary: >-
      Define the accepted serialization grammar for readiness verification commands before broadening identity binding beyond the current canonical token form.
    evidence: |-
      Review could not establish whether readiness rows are required to use the current space-delimited `--manifest`, `--profile`, and `--filter` form or may use equals forms, quoted/escaped values, shell wrappers, and trailing operators. An authoritative producer/consumer contract plus representative accepted commands would settle the question.
    location: >-
      references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Evidence/ReadinessEvidenceValidator.cs:1161
    severity: 'medium (unverified)'
---

<intent-contract>

## Intent

**Problem:** A passing readiness row can cite module-run evidence that agrees only on the `test` subcommand and profile, while its filter, fixture, manifest path, and input-byte hashes disagree. The canonical positive artifact also carries a synthetic all-`A` manifest hash instead of the hash of the manifest it names.

**Approach:** Make readiness validation bind every declared test input to the canonical module-run invocation and the current repository bytes, then replace the synthetic positive evidence identity and refresh every coupled artifact assertion and hash. Add isolated packaged negative controls for each newly enforced identity.

## Boundaries & Constraints

**Always:** Preserve `HXE152` as the stable binding-failure rule; compare canonical repository-relative manifest and fixture paths; derive the row filter hash from its exact UTF-8 value; verify manifest and fixture hashes against repository bytes; keep raw filters out of module-run evidence; retain the existing subcommand/profile and executed-test gates; update unrelated controls only enough to preserve their intended diagnostics.

**Never:** Edit the deferred-work ledger; weaken artifact schema/hash/outcome validation; accept missing identities on a passing row; expose filter text in evidence or diagnostics; change the module-run evidence schema; modify runtime manifest, fixture, or filter hashing semantics.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Canonical passing row | Row command names the actual manifest/profile/filter, row fixture names the profile fixture, and artifact paths/hashes match current bytes | Readiness validation passes | No error expected |
| Filter identity drift | Row filter hashes differently from the artifact command and invocation | Readiness validation fails without retaining the raw filter | Emit `HXE152` |
| Fixture identity drift | Fixture path or artifact fixture hash differs from the row/current bytes | Readiness validation fails | Emit `HXE152` |
| Manifest identity drift | Manifest path or artifact manifest hash differs from the command/current bytes | Readiness validation fails | Emit `HXE152` |
| Unrelated negative control | A control targets another policy rule while citing the canonical artifact | Its existing ordered diagnostic snapshot remains single-purpose | Do not add incidental `HXE152` |

</intent-contract>

## Code Map

- `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/RunEvidence/ModuleRunEvidenceArtifactSummary.cs` -- safe validated-artifact projection; extend it with manifest, fixture, and filter identity fields already present in v1 JSON.
- `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/RunEvidence/ModuleRunEvidenceArtifactValidator.cs:166` -- constructs the summary after strict schema validation; extract all invocation identity fields here.
- `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Evidence/ReadinessEvidenceValidator.cs:1032` -- `ArtifactBindsToRow` gate and command-token helpers; bind row options and fixture to summary values and repository byte hashes while retaining `HXE152`.
- `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/RunEvidence/ModuleRunEvidenceFactory.cs:56` -- read-only hashing authority: repository-relative paths, SHA-256 file bytes, and exact UTF-8 filter hashing define the producer semantics to mirror.
- `references/Hexalith.Builds/.gitattributes` -- Git whitespace policy must recognize repository-mandated CRLF C# endings so the required diff gate evaluates content rather than carriage returns.
- `references/Hexalith.Builds/test/fixtures/evidence/positive/evidence/release-passed.json` -- canonical source artifact whose invocation must cite the actual manifest, profile fixture, filter hash, and byte hashes.
- `references/Hexalith.Builds/test/fixtures/evidence/positive/readiness.yaml` -- canonical passing row and coupled artifact SHA-256.
- `references/Hexalith.Builds/test/fixtures/evidence/negative/` -- packaged fail-closed corpus and snapshots; add isolated filter-hash, fixture-path/hash, and manifest-path/hash mismatch controls and keep existing controls diagnostically stable.
- `references/Hexalith.Builds/test/Hexalith.Builds.Evidence.Tests/ReadinessEvidenceValidatorTests.cs` -- focused positive and stable-rule/snapshot coverage for every new control.
- `references/Hexalith.Builds/Tools/test-g4-tool-package-contracts.ps1:923` and `references/Hexalith.Builds/Tools/publish-g4-tool-packages.ps1:254` -- source-artifact assertions currently pin the synthetic hash/short command; bind them to computed source manifest identity and the refreshed canonical command.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/RunEvidence/ModuleRunEvidenceArtifactSummary.cs` and `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/RunEvidence/ModuleRunEvidenceArtifactValidator.cs` -- expose validated manifest path/hash, fixture path/hash, and filter hash to readiness policy without exposing secrets.
- [x] `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Evidence/ReadinessEvidenceValidator.cs` -- require a passing row's test command, manifest, profile, exact filter hash, fixture, and current manifest/fixture byte hashes to agree with both artifact command and invocation fields; fail every mismatch as `HXE152`.
- [x] `references/Hexalith.Builds/test/fixtures/evidence/positive/evidence/release-passed.json`, `references/Hexalith.Builds/test/fixtures/evidence/positive/readiness.yaml`, and existing `references/Hexalith.Builds/test/fixtures/evidence/negative/*.yaml` consumers -- replace synthetic identities with actual producer-shaped identities and recompute all coupled artifact SHA-256 values while preserving deliberate mismatch hashes.
- [x] `references/Hexalith.Builds/test/fixtures/evidence/negative/{filter-hash,fixture-path,fixture-hash,manifest-path,manifest-hash}-mismatch.*` and `references/Hexalith.Builds/test/Hexalith.Builds.Evidence.Tests/ReadinessEvidenceValidatorTests.cs` -- add one isolated `HXE152` packaged control and deterministic snapshot per identity.
- [x] `references/Hexalith.Builds/Tools/test-g4-tool-package-contracts.ps1` and `references/Hexalith.Builds/Tools/publish-g4-tool-packages.ps1` -- replace the synthetic source manifest expectation and abbreviated command with values derived from the tracked manifest and canonical filter.
- [x] `references/Hexalith.Builds/.gitattributes` -- align Git's whitespace checker with the tracked CRLF C# policy so `git diff --check` remains a valid portable gate.

**Acceptance Criteria:**
- Given the canonical readiness matrix and artifact, when `hexalith-evidence validate` runs, then it passes only when manifest and fixture paths/hashes plus the exact UTF-8 filter hash all bind to current repository inputs.
- Given any isolated filter, fixture path/hash, or manifest path/hash mismatch control, when source tests or packaged controls execute, then the result fails with `HXE152` and contains no raw secret/filter material.
- Given pre-existing negative readiness fixtures, when snapshot tests execute, then their intended rule ordering is unchanged except for controls explicitly dedicated to identity binding.
- Given package qualification and publication validation, when the source artifact is inspected, then its manifest hash equals the actual positive module-manifest byte hash and its coupled readiness SHA-256 is current.

## Spec Change Log

## Review Triage Log

### 2026-09-05 — Review pass
- verdicts: 27 findings — high 0, medium 13, low 5, false 4, maybe-false 5
- findings:
  - `[medium]` `[patch]` An artifact command using raw `--filter=value` could bypass the raw-filter rejection — artifact commands are now accepted only when they exactly equal the canonical command reconstructed from validated manifest, profile, and filter-hash fields; isolated equals-form and raw-filter controls cover the fix.
  - `[maybe-false]` `[defer]` Valid CLI equals forms such as `--manifest=...` are rejected by the row parser — the accepted readiness-command serialization is not defined; an authoritative grammar and accepted-command corpus are needed before treating equals forms as valid.
  - `[maybe-false]` `[defer]` Quoted or escaped filter values containing spaces are not tokenized as a shell would tokenize them — whether such serialized commands are supported is unspecified; the same authoritative grammar and examples would settle this.
  - `[maybe-false]` `[defer]` Row wrappers or trailing operators may be ignored even though artifact extras are now rejected — exact artifact serialization is enforced, but the allowed row-command envelope remains unspecified and needs an authoritative grammar.
  - `[low]` `[reject]` Drive-like `C:/...` text is not classified as rooted on non-Windows hosts — repository resolution still confines it beneath the repository root, everyday tracked fixtures do not use drive-qualified paths, and a portable-path policy would require additional guards.
  - `[low]` `[reject]` Binding hashes files synchronously and does not observe cancellation during the reads — the inputs are small tracked manifest/fixture files, so this is unlikely to affect everyday validation and an asynchronous pipeline would add nontrivial complexity.
  - `[low]` `[reject]` The manifest is hashed and then reopened for parsing, allowing a concurrent mutation between operations — ordinary validation runs against stable tracked inputs, and eliminating this race requires a larger byte-based manifest-loading change.
  - `[medium]` `[patch]` Byte-bound JSON/YAML fixtures lacked a pinned checkout EOL — `.gitattributes` now pins all evidence fixture JSON and YAML to LF, keeping the recorded SHA-256 identities stable across platforms.
  - `[false]` `[reject]` PowerShell source assertions do not separately inspect every new structured field — the packaged gate exercises the positive artifact and isolated structured-field mismatch controls through the shipping validator, so each identity is asserted without duplicating the validator in PowerShell.
  - `[medium]` `[patch]` Artifact command/profile divergence lacked an independent binding check — exact canonical artifact-command reconstruction now compares the command to the validated profile, with an isolated profile-command mismatch control.
  - `[medium]` `[patch]` Cross-field identity seams were not isolated by the initial control set — command-versus-structured manifest/profile/filter, row-versus-artifact profile, and manifest-profile-versus-fixture controls were added with deterministic `HXE152` snapshots.
  - `[false]` `[reject]` Completed spec tasks lacked recorded verification outcomes during review — the workflow intentionally records outcomes only after patch verification; the final result below now contains the observed commands and results.
  - `[false]` `[reject]` The dirty submodule pointer was not yet reproducible — this was expected before finalization; the owning repository and then the superproject are committed below in dependency order.
  - `[medium]` `[patch]` Artifact raw-filter equals syntax could pass the initial token check — exact canonical artifact-command comparison and an isolated equals-form mismatch control now fail it closed as `HXE152`.
  - `[maybe-false]` `[defer]` Quoted filter whitespace is not preserved by simple splitting — support for shell quoting is not established by the readiness contract; an authoritative serialization grammar would settle the expected behavior.
  - `[medium]` `[patch]` A row option could consume the next option name as its value — option extraction now rejects values beginning with `--`, with an isolated malformed-row control.
  - `[low]` `[reject]` A manifest can change between hash verification and profile loading — this duplicates the verified time-of-check/time-of-use race; stable tracked inputs make it unlikely and a byte-based loader is not a direct correction.
  - `[medium]` `[patch]` Extra artifact-command tokens were ignored after selected identities matched — the artifact command must now equal the complete reconstructed canonical command, and extra/duplicate-token controls fail closed.
  - `[low]` `[reject]` The hash/load race could admit profile data from bytes other than those hashed — this is the same low-likelihood manifest race, whose smallest robust fix requires broader loader changes.
  - `[medium]` `[patch]` No artifact-side negative control proved raw filters are rejected — raw space-form and equals-form artifact fixtures and snapshots now assert stable `HXE152` failures without exposing filter text.
  - `[medium]` `[patch]` Three-way row, command, and invocation identity comparisons were not independently controlled — isolated manifest, profile, and filter command-divergence fixtures now cover those branches.
  - `[medium]` `[patch]` The manifest-profile-to-fixture failure branch only had a happy path — an isolated manifest/profile fixture-divergence artifact and readiness control now exercise the failure.
  - `[medium]` `[patch]` Duplicate artifact options lacked a regression control — a duplicate-option artifact fixture and deterministic snapshot now prove canonical-command rejection.
  - `[medium]` `[patch]` Artifact `--filter=...` syntax lacked direct regression coverage — a dedicated artifact and readiness fixture now prove it fails with `HXE152`.
  - `[maybe-false]` `[defer]` The implementation enforces literal token form rather than all potentially equivalent command normalizations — the intent does not define which serializations are equivalent; an authoritative readiness-command grammar would settle this reading.
  - `[false]` `[reject]` The change does not add a successful live producer-to-consumer run — the requested bundle explicitly targets the canonical static artifact and consumer binding, and the packaged positive path validates that artifact; a new live producer scenario was not required.
  - `[medium]` `[patch]` The initial tests did not cover every comparison seam implied by exact identity binding — the expanded isolated `HXE152` matrix now covers artifact command fields, row profile, malformed options, and manifest-profile fixture selection.

### 2026-09-05 — Review pass (follow-up)
- verdicts: 39 findings — high 0, medium 12, low 14, false 11, maybe-false 2
- findings:
  - `[medium]` `[patch]` `HXE152` message and remediation still described only a verification-command match after the rule was broadened to eight identity branches — both strings at `ReadinessEvidenceValidator.cs:342` and `:379` now name the canonical command, manifest, profile, filter, fixture, and current-byte requirements.
  - `[medium]` `[patch]` Consumer readiness matrices (rows without `--manifest`, logical fixture labels) fail closed the moment a row flips to `passed`, with no authoring guidance — the README Evidence Validator Contract now states the passing-row binding contract; rewriting a consumer matrix is outside this repository.
  - `[medium]` `[patch]` Byte-hashed manifests and fixtures hosted outside this repository can hash differently on a CRLF checkout — the same README paragraph requires LF checkout for byte-bound inputs; normalizing hashes is forbidden by the intent.
  - `[false]` `[reject]` `.gitattributes` declares `*.cs whitespace=cr-at-eol` without a `text eol=crlf` pin — the worktree `.cs` files are CRLF (1404 CR in the validator) and `.editorconfig` sets `end_of_line = crlf`; the attribute's only job is to stop `git diff --check` flagging those CRs, and on an LF checkout there are no CRs to flag. Adding `text eol=crlf` would renormalize every tracked `.cs` file.
  - `[false]` `[reject]` The redundant `IsCanonicalRelativePath(fixture)` check double-reports `HXE109` and `HXE152` — no `.expected.json` in the corpus contains `HXE109`, so no control gained an incidental `HXE152`, and removing the check changes nothing because the `fixture` versus `summary.FixturePath` equality already fails for any non-canonical row fixture.
  - `[low]` `[reject]` The `!manifestResult.IsValid`, unresolvable-path, and `IOException` branches have no control — they are defense-in-depth on an already fail-closed path (`TryResolveExistingFile` at `:1110` cannot fail after `RepositoryFileMatchesHash` resolved the same path), and a control needs a new invalid-manifest fixture plus artifact and snapshot.
  - `[medium]` `[patch]` No control pinned a passing row that omits `--manifest` or `--filter` — added `negative/row-missing-manifest-mismatch.*` and `negative/row-missing-filter-mismatch.*`, registered in both the rule and snapshot theories.
  - `[low]` `[reject]` The 14-case rule theory is subsumed by the snapshot theory — the two assert different contracts (failed status/exit code plus rule presence versus exact ordered rule sequence), so deleting one removes coverage rather than duplication.
  - `[medium]` `[patch]` The raw-filter leak assertion covered only the row-side fixture — it is now a theory that also covers `artifact-raw-filter-mismatch` and `artifact-raw-filter-equals-mismatch`, whose artifact commands embed raw `Category=smoke` text.
  - `[low]` `[reject]` The canonical command string is spelled in C# and two PowerShell gates with no shared constant — no constant crosses that language boundary without new exported surface, and the drift risk it names is now pinned by the producer-to-consumer command test added below.
  - `[low]` `[reject]` Binding re-reads and re-validates the same manifest once per passing row — the inputs are small tracked files and per-run memoization adds caching state, more than a direct correction; extends the previously rejected synchronous-hashing row.
  - `[false]` `[reject]` The spec's Verification section records expectations but no outcomes — the fix would edit this build's spec, and the workflow records outcomes at finalization, below.
  - `[false]` `[reject]` The spec forbids editing the deferred-work ledger while the change set edits it — the fix would edit this build's intent contract; the ledger edits are the orchestrator's sweep bookkeeping, which this session must not touch.
  - `[low]` `[reject]` DW-65 omits `severity`, drops a blank line, and cites a line number one helper early — the fix edits the orchestrator-owned deferred-work ledger, which this session must not modify.
  - `[low]` `[reject]` Fourteen near-identical 71-line fixtures repeat the same boilerplate — the corpus is deliberately self-contained so the packaged PowerShell gate can discover and validate each control as a whole document; a builder refactor is more than a direct correction.
  - `[low]` `[patch]` Snapshots this change added were split between minified and pretty-printed forms — the nine minified ones were reformatted to the corpus's two-space style; the pre-existing minified `positive/readiness.yaml.expected.json` was left untouched.
  - `[low]` `[patch]` Three `InlineData` entries drifted from the surrounding alphabetical order — `fixture-hash`/`fixture-path`, `manifest-hash`/`manifest-path`, and `row-profile`/`secret-metadata` reordered.
  - `[low]` `[reject]` The `release-unavailable` control's key and description no longer match its filter — they name the row's declared `outcome: unavailable`, not its filter, and the intent permits updating unrelated controls only enough to preserve their diagnostics.
  - `[false]` `[reject]` A filterless producer run can never back a passing row — the intent makes the filter a required passing-row identity, so failing closed is the specified behavior; the README paragraph and the new diagnostic text now say so.
  - `[maybe-false]` `[defer]` carried — Quoted or escaped filter values are not tokenized as a shell would tokenize them; same claim and location as the logged 2026-09-05 row, and `ReadinessEvidenceValidator.cs:1164-1197` still reads as logged. DW-65 already records the missing grammar.
  - `[low]` `[reject]` carried — The manifest can change between hash verification and profile loading; same claim and location as two logged rows, rejected on the same stable-tracked-inputs grounds.
  - `[false]` `[reject]` A manifest that hashes correctly but fails to load is collapsed into `HXE152`, rejecting correct evidence — every manifest path in the corpus (`descriptorAssembly`, `profiles[].fixture`) points at tracked JSON, not build output, and a manifest that does not load could not have produced the run it claims; no reachable case was shown.
  - `[low]` `[reject]` A readiness document and manifest in different repository roots can never bind — no such row exists (the only real matrix cites no manifest at all), the intent explicitly specifies repository-relative paths and repository bytes, and multi-root resolution is far more than a direct correction.
  - `[medium]` `[patch]` A CRLF checkout outside `test/fixtures` changes the recorded byte hashes — grouped with the consumer-contract entry; the README now requires LF for byte-bound inputs.
  - `[false]` `[reject]` The spec's ledger claim is contradicted by the diff — duplicate of the intent-contract row above.
  - `[medium]` `[patch]` The absent-option guard on passing rows is unpinned, so removing it would throw instead of emitting `HXE152` — grouped with the missing-identity controls added above.
  - `[medium]` `[patch]` Row-side duplicate-option rejection was unverified; the duplicate control puts its duplicate in the artifact command, which fails earlier on exact equality — added `negative/row-duplicate-option-mismatch.*`, whose row command repeats `--profile` and reaches the duplicate branch at `:1176-1189`.
  - `[medium]` `[patch]` Nothing tied `ModuleRunEvidenceFactory.CreateCommand` to the consumer's `CreateCanonicalArtifactCommand`, so reordering a producer segment would unbind every real artifact with the suite green — added `CreateTestEvidenceMatchesReadinessCanonicalBindingCommand`, which produces a real `test` artifact from the tracked positive manifest and asserts its command equals the canonical reconstruction with every binding identity populated.
  - `[low]` `[reject]` The `.gitattributes` LF pin is never exercised — every CI leg is `ubuntu-latest`, so the only real check is a Windows job, well beyond a direct correction, and a no-CR assertion would duplicate what `PositiveFixturePassesAsync` already fails on.
  - `[false]` `[reject]` The README documents a filterless `test` invocation the change makes unusable for passing rows — duplicate of the filterless row above; the filterless invocation remains valid for its own purpose.
  - `[medium]` `[patch]` Every row of the real consumer matrix would emit `HXE152` once marked `passed` — grouped with the consumer-contract entry; the matrix itself is a superproject artifact outside this repository.
  - `[low]` `[reject]` `artifact_sha256` compares case-insensitively while the new hash comparisons use `Ordinal` — the producer always emits uppercase hex, and loosening only those two comparisons would still fail the exact canonical-command equality that embeds the same hash, so it is not a direct correction.
  - `[maybe-false]` `[defer]` carried — The implementation enforces a literal token form rather than a parsed CLI grammar; same claim as the logged row, settled only by the authoritative serialization grammar DW-65 records.
  - `[false]` `[reject]` The artifact-versus-itself axis is policy beyond the intent's I/O matrix — deliberate hardening consistent with keeping raw filters out of module-run evidence, already routed as patches in the logged pass; no bad outcome shown.
  - `[medium]` `[patch]` The new row `fixture` and command requirements live only in code and fixture data — grouped with the consumer-contract entry; the README documents them, while the JSON schema keeps `fixture` a free string because the constraint is policy, not shape.
  - `[medium]` `[patch]` The intent's expectations live at the authored-matrix surface while every test lives in the fixture corpus — grouped with the consumer-contract entry.
  - `[false]` `[reject]` The ledger boundary is contradicted by the diff — duplicate of the intent-contract row above.
  - `[low]` `[reject]` The unrelated-control guarantee rests on branch ordering rather than an asserted property — the ordered snapshots do pin each control's exact rule sequence, which is the property in question; grouped with the `release-unavailable` row.
  - `[false]` `[reject]` Two files lost their trailing newline — `.editorconfig` sets `insert_final_newline = false` and all 56 sampled `src/**/*.cs` files end without one; `ModuleRunEvidenceArtifactSummary.cs` had none before the change, and the tests file was brought into line with the convention.

## Design Notes

The artifact intentionally stores a filter hash rather than filter text. Binding therefore hashes the row's exact `--filter` token with the producer's SHA-256/UTF-8 convention, then requires equality with both `invocation.filterHash` and the artifact command's `--filter-sha256`. Manifest and fixture paths remain canonical repository-relative identities, and their hashes are recomputed from resolved repository files so a mutually edited row and artifact cannot validate against different bytes.

## Verification

**Commands:**
- `dotnet test test/Hexalith.Builds.Evidence.Tests/Hexalith.Builds.Evidence.Tests.csproj --configuration Release` from `references/Hexalith.Builds` -- expected: canonical positive plus all existing and new identity controls pass.
- `dotnet build Hexalith.Builds.slnx --configuration Release` from `references/Hexalith.Builds` -- expected: warnings-as-errors build succeeds.
- `pwsh -NoProfile -File ./Tools/test-g4-tool-package-contracts.ps1 -Version 999.0.0-readiness-identity -RequireControls` from `references/Hexalith.Builds` -- expected: packaged positive and every negative readiness control satisfy their exact expectations.
- `git diff --check` in `references/Hexalith.Builds` and the superproject -- expected: no whitespace errors; `_bmad-output/implementation-artifacts/deferred-work.md` remains unchanged.

## Auto Run Result

Follow-up review pass over the committed readiness-identity binding. Six medium and two low patch entries were applied on top of the existing implementation: the `HXE152` diagnostic now describes the full binding contract, the README documents the passing-row contract and the LF requirement for byte-bound inputs, three row-side controls close the missing-identity and duplicate-option coverage gaps, a producer-to-consumer test pins the canonical command serialization, the raw-filter leak assertion covers artifact-side filters, and the added snapshots and theory lists were normalized.

**Files changed:**
- `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Evidence/ReadinessEvidenceValidator.cs` — `HXE152` message and remediation now name the canonical command, manifest, profile, filter, fixture, and current-byte requirements.
- `references/Hexalith.Builds/README.md` — Evidence Validator Contract documents the passing-row binding contract and the LF checkout requirement for byte-bound inputs.
- `references/Hexalith.Builds/test/fixtures/evidence/negative/row-{missing-manifest,missing-filter,duplicate-option}-mismatch.{yaml,expected.json}` — isolated `HXE152` controls for an absent `--manifest`, an absent `--filter`, and a repeated identity option.
- `references/Hexalith.Builds/test/Hexalith.Builds.Evidence.Tests/ReadinessEvidenceValidatorTests.cs` — registers the three controls in both theories, restores alphabetical ordering, and turns the raw-filter leak assertion into a theory covering artifact-side raw filters.
- `references/Hexalith.Builds/test/Hexalith.Builds.Module.Tests/ModuleRunEvidenceSerializationTests.cs` — `CreateTestEvidenceMatchesReadinessCanonicalBindingCommand` pins the produced command to the consumer's canonical reconstruction.
- `references/Hexalith.Builds/test/fixtures/evidence/negative/*.expected.json` — nine snapshots added by this change reformatted to the corpus's two-space style.
- `_bmad-output/implementation-artifacts/spec-readiness-evidence-identity.md` — records this pass's triage and outcome.

**Review findings breakdown:** 39 findings from four layers. Eight entries patched (six medium, two low): diagnostic text, consumer-contract documentation covering six findings, missing-identity controls covering two, duplicate row-option control, producer-to-consumer command test, artifact-side raw-filter coverage, snapshot formatting, and theory ordering. Two `maybe-false` rows were carried from the logged pass and remain deferred under DW-65 (serialization grammar). Fourteen `low` findings were rejected as unlikely-in-practice or requiring more than a direct correction: unreached defensive branches, theory duplication, cross-language constant sharing, per-row hashing cost, ledger formatting owned by the orchestrator, fixture boilerplate, control self-description, TOCTOU (carried), cross-repository roots, absent Windows CI, and hash case sensitivity. Eleven `false` findings were rejected on their refutations: the `.gitattributes` whitespace attribute works as intended, the `HXE109` double-report does not occur in the corpus, spec-editing findings are out of bounds, filterless runs fail closed by design, invalid-manifest rejection is unreachable for tracked inputs, the artifact self-consistency axis is deliberate, and the trailing-newline change follows `insert_final_newline = false`.

**Follow-up review recommendation:** `false` — this was a follow-up pass and it patched no `high` entry (patched counts by entry verdict: high 0, medium 6, low 2). The work has converged.

**Verification performed:**
- `dotnet build Hexalith.Builds.slnx --configuration Release` — succeeded, 0 warnings, 0 errors.
- `dotnet test test/Hexalith.Builds.Evidence.Tests/Hexalith.Builds.Evidence.Tests.csproj --configuration Release` — reported `Zero tests ran` (exit 5) in this environment for every test project, including `Hexalith.Builds.Tooling.IntegrationTests`, which this pass did not touch; the `dotnet test` host never starts here. The built test binaries were run directly instead: Evidence 68 passed, Module 115 passed, Integration 1 passed; 0 failed, 0 skipped.
- `pwsh -NoProfile -File ./Tools/test-g4-tool-package-contracts.ps1 -Version 999.0.0-readiness-identity -RequireControls` — passed; packed G-4 tool contract qualification succeeded, exercising the auto-discovered fixture corpus including the three new controls.
- `git diff --check` in `references/Hexalith.Builds` and the superproject — no whitespace errors.
- `_bmad-output/implementation-artifacts/deferred-work.md` — untouched by this session; its working-tree edits are the orchestrator's and were left exactly as found.

**Residual risks:** The accepted serialization grammar for readiness row commands is still unspecified (DW-65), so equals forms, quoting, wrappers, and trailing operators remain unsupported. Consumer matrices that predate this contract still carry rows without `--manifest` and with logical fixture labels; they now have documentation but not a migration, and rewriting them belongs to their owning repository. The LF requirement for byte-bound inputs is documented but only enforced by `.gitattributes` inside this repository, and no Windows CI leg exercises it.
