---
title: '6.1-P1R Revalidate the 3.88.0 Platform Baseline'
type: 'bugfix'
created: '2026-08-01'
status: 'done'
review_loop_iteration: 0
baseline_commit: '3fe3c7eea4de1056f69438d8ed94147872506384'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-p1r-baseline-revalidation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/references/Hexalith.Builds/DEVELOPMENT.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** EventStore source and the Builds catalog select `3.88.0`, but the G-4 runner, schema, fixtures, and current planning evidence still select or describe older baselines. This leaves 6.1-P1R unresolved and blocks P0 from consuming a coherent candidate.

**Approach:** Align the active Builds runner contract and its test/evidence corpus on the exact EventStore `v3.88.0` candidate, propagate the truthful pre-acceptance state, and create a finite qualification record. Keep Architecture and completion state unchanged until clean validation and four-owner acceptance exist.

## Boundaries & Constraints

**Always:** Bind candidate source to EventStore `v3.88.0` revision `4843b492dff7c16a4bc74db67509263f969c78c6`; retain `3.70.1` as the rollback and stale-pin negative; preserve historical P1 evidence; keep P1R/P0 open and Story 6.1 blocked; record stalled or cancelled commands as inconclusive; make all current positive and unrelated-negative fixtures use the active pin atomically.

**Ask First:** Selecting any baseline other than `3.88.0`; changing Dapr or FrontComposer pins; rebinding the Architecture Spine; marking P1R or P0 done; claiming named-owner acceptance; committing, pushing, publishing, or updating submodules.

**Never:** Rewrite historical P1/CHANGELOG evidence; mutate EventStore source or tags; infer package publication from the release manifest; weaken exact ordinal pin validation or negative controls; claim P0 live-runner qualification; clear P2/P3/P4/Story 6.1 gates.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Candidate manifest | EventStore pin `3.88.0` | Manifest and packaged control accept the pin | Any `HXM016` fails qualification |
| Rollback manifest | EventStore pin `3.70.1` while candidate active | Runner rejects it deterministically | Nonzero exit with `HXM016` |
| Unrelated negative fixture | Invalid path/schema/profile with pin `3.88.0` | Original single-purpose diagnostic remains | No incidental `HXM016` |
| Canonical evidence | Evidence JSON changes to `3.88.0` | Readiness documents reference recomputed SHA-256 values | Hash mismatch stays fail-closed |
| Contended validation | Restore/test stalls or is cancelled | Record is `pending-acceptance` with exact inconclusive result | Do not close P1R or update Architecture |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs` -- runtime exact-pin authority.
- `references/Hexalith.Builds/schemas/hexalith.module-manifest.v1.json` -- published manifest-schema pin.
- `references/Hexalith.Builds/test/Hexalith.Builds.Module.Tests/` -- manifest, CLI, state, and evidence regression tests.
- `references/Hexalith.Builds/test/fixtures/{module,evidence}/` -- positive, isolated-negative, and canonical evidence corpus.
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md` -- finite pending qualification ledger.
- `_bmad-output/{implementation-artifacts,planning-artifacts}/` -- current P1R/P0/story/readiness propagation surfaces.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs`, `references/Hexalith.Builds/schemas/hexalith.module-manifest.v1.json`, and `references/Hexalith.Builds/test/Hexalith.Builds.Module.Tests/ManifestValidationTests.cs` -- change the active EventStore pin and P1R wording to `3.88.0`; add a schema/runtime pin-parity assertion.
- [x] `references/Hexalith.Builds/test/Hexalith.Builds.Module.Tests/*.cs` and `references/Hexalith.Builds/test/fixtures/module/**/*.json` -- move valid/current literals to `3.88.0`, change only `tampered-platform-pin.json` to `3.70.1`, and preserve stable expected diagnostics.
- [x] `references/Hexalith.Builds/test/fixtures/evidence/**/*.json` and referencing `references/Hexalith.Builds/test/fixtures/evidence/**/*.yaml` -- update canonical platform values and recompute every coupled SHA-256; retain the intentionally wrong all-`F` hash control.
- [x] `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md` -- record exact known source/catalog/API facts, command ledger, rollback procedure, scope guard, and explicitly pending Builds/Architecture revisions, validation, and approvals.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`, `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`, `_bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths.md`, `_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.{md,yaml}`, and `_bmad-output/planning-artifacts/epics.md` -- replace stale `3.86.0`/`b529b66` current observations with the `3.88.0` candidate and exact revisions while preserving every blocker and historical statement.
- [x] Execute the focused validation ladder and update only the qualification ledger with actual results; leave Architecture and completion transitions for accepted evidence.

**Acceptance Criteria:**
- Given the aligned source, schema, fixtures, and tests, when runner validation executes, then `3.88.0` is accepted and stale `3.70.1` fails with `HXM016` without contaminating other negative cases.
- Given canonical evidence changes, when evidence tests and packaged controls execute, then all truthful hashes validate and the deliberate mismatch remains rejected.
- Given current planning propagation, when YAML and state assertions run, then P1R/P0 remain open, Story 6.1 and matrix rows remain blocked, and `3.88.0` appears only as candidate source/catalog state.
- Given incomplete validation or owner signatures, when the record is reviewed, then its status remains `pending-acceptance` and Architecture remains bound to `3.70.1`.

## Spec Change Log

- 2026-08-01: Aligned the candidate runner/schema/test/evidence corpus to
  `3.88.0`, propagated the truthful pending state, and recorded passing,
  inconclusive, and pending qualification results without rebinding
  Architecture or closing P1R.
- 2026-08-01: Review patch corrected rollback/proposal provenance, restored the
  active Dapr SDK pin in module-run evidence, strengthened schema/runtime pin
  parity, and made the deterministic package-version audit an explicit
  acceptance-blocking gate.
- 2026-08-01: Post-review propagation recorded Builds runner candidate
  `4351d7cba7545a96661ca2ee2ca2629df6d0a118` and qualification revision
  `d3239aff003c64b40cbc074e68ec7923924cfc96` without claiming acceptance.

## Design Notes

This is a two-phase correction. This implementation produces a coherent, testable candidate and truthful pending record. A later owner-acceptance transition supplies immutable post-change revisions, rebinds Architecture, and closes only P1R.

## Verification

**Commands:**
- From `references/Hexalith.EventStore`: `git rev-parse 'v3.88.0^{commit}'` plus focused `git diff --exit-code`/blob comparisons for the seven named Story 6.1 APIs -- expected: exact candidate revision, six byte-identical APIs, and one additive-compatible `QueryCursorScope` change.
- From `references/Hexalith.EventStore`: package-mode `dotnet restore Hexalith.EventStore.slnx -p:UseHexalithProjectReferences=false --force --no-cache --verbosity minimal`, Release build, and Contracts tests -- expected: clean 3.88.0 package-mode evidence, or an exact inconclusive blocker recorded without acceptance.
- From `references/Hexalith.Builds`: `dotnet restore Hexalith.Builds.slnx --verbosity minimal` -- expected: restore completes.
- From `references/Hexalith.Builds`: targeted Module and Evidence test projects in Release with `--no-restore` -- expected: both suites pass.
- From `references/Hexalith.Builds`: `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` -- expected: 49 identities and three shared versions pass.
- From `references/Hexalith.Builds`: `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` -- expected: the deterministic package-version audit passes for final acceptance, or its exact nonzero result and stale rows remain recorded as an acceptance blocker.
- From `references/Hexalith.Builds`: `pwsh -NoProfile -File ./Tools/test-g4-tool-package-contracts.ps1 -Version 999.0.0-p1r -RequireControls` -- expected: isolated package restore and positive/negative controls pass.
- `git diff --check && git -C references/Hexalith.Builds diff --check` -- expected: no whitespace errors.
- Parse changed YAML and assert P1R/P0 open, P0 external in progress, Story 6.1 blocked, matrix rows blocked, and no stale current `3.86.0`/`b529b66` observation remains.

## Suggested Review Order

**Baseline decision and completion boundary**

- Start with the candidate-versus-acceptance boundary and explicit non-closure rules.
  [`qualification-record.md:15`](../../references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md#L15)

- Confirm the approved correction retains Architecture and downstream blockers.
  [`sprint-change-proposal.md:20`](../planning-artifacts/sprint-change-proposal-2026-08-01-p1r-baseline-revalidation.md#L20)

- Inspect observed, failed, inconclusive, and pending validation separately.
  [`qualification-record.md:133`](../../references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md#L133)

- Verify rollback provenance does not claim a nonexistent atomic Builds revision.
  [`qualification-record.md:175`](../../references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md#L175)

**Runner and evidence contract**

- Review the active EventStore pin as a candidate, never owner-authorized.
  [`SupportedPlatformPins.cs:9`](../../references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Manifest/SupportedPlatformPins.cs#L9)

- Confirm the published schema selects the same exact candidate pin.
  [`hexalith.module-manifest.v1.json:65`](../../references/Hexalith.Builds/schemas/hexalith.module-manifest.v1.json#L65)

- Check schema/runtime parity also preserves all four required properties.
  [`ManifestValidationTests.cs:201`](../../references/Hexalith.Builds/test/Hexalith.Builds.Module.Tests/ManifestValidationTests.cs#L201)

- Confirm the positive manifest exercises candidate `3.88.0`.
  [`hexalith.module-manifest.v1.json:25`](../../references/Hexalith.Builds/test/fixtures/module/positive/hexalith.module-manifest.v1.json#L25)

- Confirm the rollback pin remains an isolated deterministic rejection.
  [`tampered-platform-pin.json:15`](../../references/Hexalith.Builds/test/fixtures/module/negative/tampered-platform-pin.json#L15)

- Verify canonical evidence carries candidate EventStore and unchanged Dapr pins.
  [`release-passed.json:1`](../../references/Hexalith.Builds/test/fixtures/evidence/positive/evidence/release-passed.json#L1)

**Downstream planning truth**

- Confirm P1R remains open with immutable revision and approvals pending.
  [`sprint-status.yaml:252`](sprint-status.yaml#L252)

- Ensure Story 6.1 remains blocked on every external prerequisite.
  [`spec-6-1-list-and-open-projects-through-supported-authenticated-paths.md:92`](spec-6-1-list-and-open-projects-through-supported-authenticated-paths.md#L92)

- Check readiness rows retain blocked-external status and unavailable tooling.
  [`implementation-readiness-traceability-matrix.yaml:55`](../planning-artifacts/implementation-readiness-traceability-matrix.yaml#L55)

- Verify the Epic 6 table exposes candidate state without closing P1R.
  [`epics.md:1293`](../planning-artifacts/epics.md#L1293)

**Deferred review findings**

- Follow up Story 6.4's missing P1R gate and stale baseline separately.
  [`deferred-work.md:86`](deferred-work.md#L86)

- Harden full manifest, fixture, and filter evidence binding separately.
  [`deferred-work.md:92`](deferred-work.md#L92)
