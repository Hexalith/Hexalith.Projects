---
title: 'G-6 Runtime and Toolchain Baseline'
type: 'feature'
created: '2026-09-06'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'a643fc79374e9b9e7212030bc2024131f087ff65'
context:
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** G-6 remains open because Dapr/Aspire pins disagree and retained two-host proof predates the current toolchain, leaving live qualification fail-closed.

**Approach:** Select the current-patch tuple—.NET SDK `10.0.400`; Aspire SDK/CLI `13.5.3`; Toolkit Dapr `13.5.0-preview.1.260825-0345`; Dapr CLI `1.18.0`, runtime `1.18.2`, .NET packages `1.18.5`; Fluent UI `5.0.0-rc.5-26219.1`; NSubstitute `6.2.0`; Fluxor `6.11.0`—then align pins and capture fresh restart/two-instance evidence. Record Dapr as an explicit exception to the packaged table's runtime `1.18.0` / .NET SDK `1.18.1` pair so current patch fixes are retained.

**Governance decision:** Jérôme Piquot, acting as the accountable G-6 Owner for Builds, Platform, and FrontComposer/Web, approved the exception tuple on 2026-09-06. Acceptance remains conditional on the complete validated evidence packet; no approval is inferred for G-4, G-5, deployment, or release.

## Boundaries & Constraints

**Always:** Bind proof to repository revisions or diff hashes, catalog/fixture hashes, observed versions, commands, test counts, two distinct sidecars, stop/survivor/restart observations, owner dispositions, containment, and rollback. Retain metadata-only, sentinel-scanned evidence. Name Fluent RC5 and Toolkit preview exceptions; record NSubstitute and Fluxor as stable. Follow each owner repository's guidance.

**Never:** Call the exception support-table-listed; infer approval; activate catalog-only `Dapr` or unselected `Dapr.Workflow`; accept mocks, one process, skips, stale captures, or browser workers as two-instance proof; weaken G-4/G-5/release gates; initialize nested submodules; publish, deploy, commit, push, or mutate domain data.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Baseline audit | Root and owners at selected revisions | Every consumed pin matches or is classified catalog-only/unselected | Drift or omitted exception fails |
| Two instances | Independent EventStore sidecars share PostgreSQL | One execution and exact replay survive owner stop | Duplicate work/shared identity fails |
| Restart | Stopped owner returns against persisted state | Replayed result and authority remain exact after restart | Lost state, changed result, or retry mutation fails |
| Mutation | Pin, hash, result, identity, approval, or artifact changes | Validator rejects deterministically | No partial acceptance |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/{Props,Tools,schemas,src/libraries/Hexalith.Builds.Tooling,test}` -- central pins, exception inventory, evidence contract, gate, and mutation tests; preserve the separate descriptor-ABI blocker.
- `src/Hexalith.Projects.AppHost`, `.github/workflows`, and `tests/e2e` -- Projects AppHost/bootstrap consumption and live-lane docs.
- `references/Hexalith.{Conversations,Memories,Parties,Tenants}` AppHost/workflow pins -- align tracked root-declared consumers still on Aspire `13.4.6` or runtime `1.18.0/1.18.1`.
- `references/Hexalith.EventStore/{tests/Hexalith.EventStore.Server.LiveSidecar.Tests,.github/workflows/integration.yml,tools/validate-oq8-platform-evidence.py}` -- reuse and fingerprint the real PostgreSQL two-sidecar stop/restart fixture without changing domain behavior.
- `_bmad-output/implementation-artifacts/{qualification-evidence,sprint-status.yaml}` and the architecture spine -- retain validated proof and report only the proven/approved state.

## Tasks & Acceptance

**Execution:**
- [ ] Builds governance/tests -- encode the tuple, exceptions, rollback, strict evidence validator, and conditional G-6 disposition without bypassing G-4.
- [ ] Owner pins -- align consumed AppHost/Aspire/Dapr settings and assertions; classify rather than activate catalog-only entries.
- [ ] EventStore qualifier -- capture two sidecars, stop, survivor replay, restart, persisted state, versions, and negative mutations.
- [ ] Packet/planning truth -- retain hashed results/dispositions; accept G-6 only with genuine approvals.

**Acceptance Criteria:**
- Given the exception tuple, when pin audits run, then no affected live consumer resolves a different target without explicit classification.
- Given two sidecars and persisted PostgreSQL, when the owner stops, the survivor replays, and the owner restarts, then exactly one execution and identical result/authority are proven with zero skips.
- Given the packet, when validation and negative mutations run, then stale, secret-bearing, unhashed, mismatched, single-instance, non-restart, or unapproved evidence cannot pass, and rollback requires no domain-data mutation.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The exception avoids downgrading current fixes but requires fresh proof and explicit accountability. Story 4.14 supplies the fixture, not current-tuple evidence: it captured runtime `1.18.1`, SDK `10.0.302`, Aspire `13.4.6`, and an older Toolkit preview.

## Verification

**Commands:**
- Builds restore/build and focused module/evidence executables -- expected: clean build and all tuple/schema/gate mutations pass.
- Changed owner AppHost/package-governance tests -- expected: `10.0.400` / `13.5.3` / central-catalog resolution without stale targets.
- EventStore OQ8 method, 21 support selectors, and validator at runtime `1.18.2` -- expected: zero skips, two identities, stop/survivor/restart, exact persisted replay.
- Projects restore/build, Integration tests, managed restart smoke, and evidence validation -- expected: tuple observed and artifacts hash-valid; missing credentials block rather than pass.
- `git diff --check` in every changed repository -- expected: no whitespace errors.
