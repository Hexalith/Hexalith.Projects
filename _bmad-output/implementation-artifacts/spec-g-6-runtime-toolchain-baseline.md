---
title: 'G-6 Runtime and Toolchain Baseline'
type: 'feature'
created: '2026-09-06'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** G-6 is still open because repository pins describe inconsistent Dapr/Aspire combinations and the retained two-host proof predates the current toolchain. This keeps supported live qualification fail-closed and makes release claims ambiguous.

**Approach:** Select the current-patch exception tuple—.NET SDK `10.0.400`; Aspire SDK/CLI `13.5.3`; CommunityToolkit Dapr `13.5.0-preview.1.260825-0345`; Dapr CLI `1.18.0`, runtime `1.18.2`, and .NET packages `1.18.5`; Fluent UI `5.0.0-rc.5-26219.1`; NSubstitute `6.2.0`; Fluxor `6.11.0`—then align governed pins and capture fresh, machine-checkable restart/two-instance evidence. The Dapr patch pair is an explicit exception to the packaged support-table pair (`1.18.0` runtime / `1.18.1` .NET SDK), chosen to retain current runtime and SDK fixes.

## Boundaries & Constraints

**Always:** Bind the decision and proof to exact repository revisions or dirty-diff hashes, catalog and fixture hashes, observed CLI/runtime/control-plane versions, commands, exit codes, test counts, two distinct process/sidecar identities, stop/survivor/restart observations, accountable dispositions, containment, and executable rollback. Keep the evidence metadata-only and sentinel-scan it before retention. Treat Fluent v5 RC5 and the Toolkit preview as named exceptions; record NSubstitute and Fluxor as resolved stable pins. Work in each owning repository and validate its local guidance.

**Never:** Claim that an unlisted Dapr patch combination is support-matrix-listed; infer owner approval; treat catalog-only `Dapr` or unselected `Dapr.Workflow` entries as runtime dependencies; accept mocks, one process, skipped/unavailable tests, stale captures, or browser-worker concurrency as two-instance proof; weaken G-4/G-5 or release gates; initialize nested submodules; publish, deploy, commit, push, or mutate domain data.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Baseline audit | Root and root-declared owners at selected revisions | Every consumed SDK, package, CLI, and runtime pin matches the tuple or is explicitly catalog-only/unselected | Drift, prerelease omission, or stale exception fails validation |
| Two instances | Two independent EventStore processes/sidecars share PostgreSQL | One execution and exact replay survive owner stop and survivor handling | Duplicate work, shared process identity, or unavailable survivor fails |
| Restart | Stopped owner returns against persisted state | Replayed result and authority remain exact after restart | Lost state, changed result, or retry mutation fails |
| Evidence mutation | Pin, hash, result, identity, approval, or artifact is removed/changed | Validator rejects deterministically | No partial or warning-only acceptance |

</frozen-after-approval>

## Open Questions

- Approval authority — options: record this session's approval as the G-6 Builds/Platform/FrontComposer governance approval, with the exact approver name and roles you provide (the validated packet may close G-6) / produce a candidate packet with approvals pending (proof is retained, but G-6 and `RuntimePrerequisiteGate` remain closed).

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props`, `Tools/package-version-{audit,exceptions}.json`, and `src/libraries/Hexalith.Builds.Tooling/{Manifest,Runtime}` -- central tuple authority, exception inventory, stale supported pins, and fail-closed G-6 gate; preserve the independent descriptor-ABI blocker.
- `references/Hexalith.Builds/schemas/hexalith.module-run-evidence.v1.json` and `test/Hexalith.Builds.*` -- reuse strict evidence conventions and add mutation coverage for tuple/restart/two-instance facts.
- `{project-root}/global.json`, `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj`, `.github/workflows/{ci,release}.yml`, and `tests/e2e/{run-live-apphost.sh,README.md}` -- Projects SDK/AppHost/bootstrap consumption and current live-lane documentation.
- `references/Hexalith.{Conversations,Memories,Parties,Tenants}` AppHost/workflow pins -- align only tracked, root-declared owner repositories that still consume `13.4.6` or Dapr runtime `1.18.0/1.18.1`.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.LiveSidecar.Tests/Fixtures/Oq8PostgresqlFixture.cs`, `.github/workflows/integration.yml`, and `tools/validate-oq8-platform-evidence.py` -- reuse the real two-process/sidecar PostgreSQL stop, survivor, restart, and replay fixture; extend capture with complete G-6 fingerprints rather than altering domain behavior.
- `_bmad-output/implementation-artifacts/qualification-evidence/g-6-*`, `sprint-status.yaml`, and the architecture spine -- retain the validated packet and update planning truth only to the actually proven/approved state.

## Tasks & Acceptance

**Execution:**
- [ ] Builds governance and tests -- encode the selected tuple, named exceptions, rollback baseline, strict G-6 evidence schema/validator, and conditional prerequisite disposition without bypassing the later G-4 descriptor gate.
- [ ] Root and root-declared owner pins -- align consumed AppHost/Aspire/Dapr settings and update their pin assertions/documentation; leave catalog-only and unselected packages classified, not activated.
- [ ] EventStore live qualifier -- capture two independent sidecars, owner stop, survivor exact replay, owner restart, persisted end-state, observed versions, and negative mutations under the selected tuple.
- [ ] Qualification packet and planning truth -- retain hashed commands/results/revisions/dispositions, validate them, document rollback, and mark G-6 accepted only if all required approvals are genuine and present.

**Acceptance Criteria:**
- Given the selected exception tuple, when every governed source and package audit runs, then no affected live consumer resolves a different SDK, Aspire, Dapr, Fluent, Toolkit, NSubstitute, or Fluxor target without an explicit classification.
- Given two independent sidecars and persisted PostgreSQL state, when the owner stops, the survivor handles replay, and the owner restarts, then exactly one execution and the same result/authority are proven with zero skips.
- Given the retained packet, when the strict validator and negative mutations run, then missing, stale, secret-bearing, unhashed, mismatched, single-instance, non-restart, or unapproved evidence cannot pass.
- Given rollback instructions, when the candidate is rejected, then prior pins and containment can be restored without event-history rewrite or domain-data mutation.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The support exception is narrower and safer than downgrading the ecosystem: it preserves the current `1.18` patch line and requires live evidence plus explicit owner accountability. Existing Story 4.14 evidence is a reusable fixture, not proof for this tuple, because it captured Dapr runtime `1.18.1`, SDK `10.0.302`, Aspire `13.4.6`, and an older Toolkit preview.

## Verification

**Commands:**
- Builds solution restore/build and focused module/evidence test executables -- expected: zero warnings/errors and all tuple/schema/gate mutations pass.
- Each changed owner solution's narrow AppHost/package-governance build/tests -- expected: exact `10.0.400` / `13.5.3` / central-catalog resolution with no stale target.
- EventStore OQ8 production method plus its 21 deterministic support selectors and evidence validator at runtime `1.18.2` -- expected: all pass, zero skips, two identities, stop/survivor/restart proof, persisted exact replay.
- Projects restore/build, Integration tests, managed AppHost restart smoke, and evidence validator -- expected: selected tuple observed and all retained artifacts hash-valid; unavailable credentials remain a reported blocker, never a pass.
- `git diff --check` in every changed repository -- expected: no whitespace errors.
