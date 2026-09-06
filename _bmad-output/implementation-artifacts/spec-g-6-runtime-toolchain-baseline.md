---
title: 'G-6 Runtime and Toolchain Baseline'
type: 'feature'
created: '2026-09-06'
status: 'done'
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
- [x] Builds governance/tests -- encode the tuple, exceptions, rollback, strict evidence validator, and conditional G-6 disposition without bypassing G-4.
- [x] Owner pins -- align consumed AppHost/Aspire/Dapr settings and assertions; classify rather than activate catalog-only entries.
- [x] EventStore qualifier -- capture two sidecars, stop, survivor replay, restart, persisted state, versions, and negative mutations.
- [x] Packet/planning truth -- retain hashed results/dispositions; accept G-6 only with genuine approvals.

**Acceptance Criteria:**
- Given the exception tuple, when pin audits run, then no affected live consumer resolves a different target without explicit classification.
- Given two sidecars and persisted PostgreSQL, when the owner stops, the survivor replays, and the owner restarts, then exactly one execution and identical result/authority are proven with zero skips.
- Given the packet, when validation and negative mutations run, then stale, secret-bearing, unhashed, mismatched, single-instance, non-restart, or unapproved evidence cannot pass, and rollback requires no domain-data mutation.

## Implementation Notes

- The exact tuple, support-table exception, prerelease/stable dispositions, containment, and data-preserving rollback are encoded in `references/Hexalith.Builds/Tools/runtime-toolchain-baseline.json`.
- The strict packet validator requires the complete unique 14-kind artifact set and a real packet on every CLI acceptance path. It cross-checks Git worktree ancestry, strict UTC timestamps, active pins, exact tool-version/command outcomes, the OQ8 identity and retained support oracles, capture hashes, accepted attempt, containment, independent processes, authority, persistence, and stop/survivor/restart observations. Twenty-one negative controls plus the valid control (22 scenarios total) reject stale, fabricated, malformed, incomplete, contradictory, failed-command, empty-rollback, private-path, and secret-bearing evidence.
- `RuntimePrerequisiteGate` now accepts only runtime `1.18.2` with Dapr .NET packages `1.18.5`; the separate descriptor-ABI prerequisite remains fail-closed as `HXR003`, so G-4 is not bypassed.
- Root and owner AppHosts consume Aspire `13.5.3` with the CLI bundle explicit. Shared CI workflows keep Dapr CLI `1.18.0` distinct from runtime `1.18.2`; root CI validates the retained packet and standalone Builds CI runs the hermetic mutation suite. The immutable root release callee has no separate runtime input and remains explicitly outside G-6 acceptance. `Dapr` remains absent/catalog-only and `Dapr.Workflow` remains unselected.
- Retained metadata-only evidence is under `_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain/`. Raw CTRF files were removed after sanitized EventStore projections were validated because CTRF contains private absolute paths.
- The first OQ8 attempt timed out waiting for Sample boundary count 4 and is retained as a non-passing sanitized diagnostic. A fresh clean-topology retry passed and alone supplies acceptance evidence.

## Spec Change Log

- 2026-09-06: Implemented and qualified the frozen G-6 tuple; moved to review without changing the approved intent block.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| BH-01 | high | patch | Root release passes `dapr-runtime-version` to immutable Builds commit `a07078ad...`, whose reusable workflow does not declare that input, so GitHub rejects the call. |
| BH-02 | high | patch | The required `in-review` status transition changed the spec hash from recorded `ddefe67c...` to actual `a56525f8...`; the packet now fails its validator. |
| BH-03 | false | reject | `RuntimePrerequisiteGate` intentionally enforces the manifest tuple only; packet integrity belongs to the repository qualification gate, while `HXR003` still blocks G-4 execution. |
| BH-04 | medium | patch | `--packet` is optional and baseline-only validation prints the same `G6-EVIDENCE-VALID` marker, allowing an incomplete invocation to be misread as packet acceptance. |
| BH-05 | medium | patch | Searches of root and Builds workflows find no invocation of either the retained-packet validator or its mutation suite. |
| BH-06 | low | patch | Timestamp checks accept malformed values such as `not-a-dateZ`; source hashes provide age relevance, but the recorded timestamp still needs real UTC parsing. |
| BH-07 | medium | patch | Repository revisions are shape-checked only; a synchronized invented 40-hex revision is accepted. |
| BH-08 | false | reject | `diffSha256` is the documented digest of the enumerated source-file manifest, not a claim to hash every unrelated working-tree change; affected-consumer completeness is a separate pin-audit concern. |
| BH-09 | medium | patch | `command-record` is hash-scanned but not parsed, and its mutation outcome still says 8 while the suite now proves 12 scenarios. |
| BH-10 | medium | patch | Most observed versions are declarations cross-checked to source pins, not to a required successful tool-version command record. |
| BH-11 | false | reject | The bound OQ8 fixture computes `independentProcessIdentities` from six present, pairwise-distinct OS process IDs; retaining the true result plus the fixture hash proves independence without leaking ephemeral PIDs. |
| BH-12 | high | patch | Rehashed contradictory authority and persisted-row observations are accepted because only packet booleans and writer replay are checked. |
| BH-13 | high | patch | The sentinel regex misses quoted JSON credential keys and common macOS/Windows user paths. |
| BH-14 | medium | patch | The published schema allows one arbitrary artifact while the executable validator requires fourteen unique kinds, creating divergent contracts. |
| BH-15 | medium | patch | The mutable literal audit omits consumed root release/CLI and Memories deployment pins and accepts literals found only in comments. |
| BH-16 | low | patch | `docs/runbooks/projects-topology.md` still instructs SDK `10.0.302` and Aspire `13.4.6`. |
| BH-17 | low | defer | Concurrent non-G-6 work added the same DW-56 decision twice, which creates ledger noise but is not caused by this story. |
| BH-18 | false | reject | The frozen text was completed before the user explicitly approved it; the implementation changelog correctly says the approved form was not changed afterward. |
| EC-01 | high | defer | Concurrent cleanup code joins unvalidated absolute or parent-traversal names to `_bmad`, allowing relocation/deletion outside the intended root. |
| EC-02 | medium | defer | Concurrent cleanup staging has no durable journal, so abrupt process termination can hide original-to-staged recovery mappings. |
| EC-03 | high | defer | A partially failed recursive final deletion reports surviving staging paths without proving their contents remain complete. |
| EC-04 | medium | patch | The reviewer reproduced acceptance of fabricated repository revisions; actual Git object/ancestry validation is missing. |
| EC-05 | medium | patch | Malformed nested JSON can raise uncaught type/key errors and produce a traceback instead of deterministic `G6-EVIDENCE-INVALID`. |
| EC-06 | medium | patch | The retained command record contradicts the executed 12-scenario suite and is not semantically validated. |
| EC-07 | medium | patch | Omitting `--packet` still returns the acceptance marker. |
| EC-08 | medium | patch | Baseline-controlled audit arrays do not yet enumerate every affected active CLI/runtime consumer. |
| EC-09 | low | patch | UTC fields are suffix-checked rather than parsed. |
| EC-10 | high | patch | Any green qualification name and any 21 unique support selectors pass when totals match; exact OQ8 identities are not reconciled. |
| EC-11 | high | patch | Authority and PostgreSQL persisted-state facts can contradict packet lifecycle booleans after a self-consistent rehash. |
| EC-12 | high | patch | Quoted JSON secret fields bypass the current sentinel patterns. |
| EC-13 | medium | patch | Baseline and packet rollback actions can be empty despite the executable-rollback requirement. |
| EC-14 | false | reject | Approval is genuine user input from this session and is bound through the approved baseline hash; external signature infrastructure was not part of the approved intent. |
| EC-15 | high | patch | The workflow-mandated status transition makes the source-bound packet stale, reproduced by the documented validator. |
| VG-01 | medium | defer | Pre-verified: the concurrent cleanup test passes locally but no normal workflow invokes it, so rollback regressions are invisible to CI. |
| VG-02 | medium | patch | Pre-verified: the G-6 packet validator and mutation suite are absent from root and Builds automated gates. |
| VG-03 | medium | patch | Pre-verified: a rehashed required command with exit code 1 is accepted. |
| VG-04 | medium | patch | Pre-verified: a revision changed to forty `f` characters in packet and source-state is accepted. |
| VG-05 | high | patch | Pre-verified: rehashed false authority replay and zero persisted events are accepted. |
| VG-O1 | high | patch | The actual packet validator exits 1 after the spec status transition because the spec was included as volatile source evidence. |
| VG-O2 | high | patch | The immutable release callee lacks the newly passed runtime input. |
| VG-O3 | medium | patch | `commands.json` says 8 scenarios while the current suite prints 12. |

## Design Notes

The exception avoids downgrading current fixes but requires fresh proof and explicit accountability. Story 4.14 supplies the fixture, not current-tuple evidence: it captured runtime `1.18.1`, SDK `10.0.302`, Aspire `13.4.6`, and an older Toolkit preview.

## Verification

Observed 2026-09-06:

- Dapr reinitialization reported CLI `1.18.0` and runtime `1.18.2`.
- Builds focused module/evidence tests: 117 passed, 0 failed, 0 skipped. Final G-6 validator mutation suite: 22 scenarios passed (1 valid packet plus 21 fail-closed mutations).
- EventStore OQ8: 1 passed, 0 failed, 0 skipped; 21 support selectors expanded to 33 passed cases with 0 failures/skips; the strict OQ8 validator passed.
- Projects restore/Release build: 0 warnings/errors; Integration executable: 27 passed, 0 failed, 0 skipped; workflow gate passed.
- Conversations, Memories, Parties, and Tenants AppHosts built with 0 warnings/errors. Conversations SDK assertion and FrontComposer tuple assertion each passed 1/1.
- Managed AppHost smoke stopped before startup with exit 1 because `TEST_USER_PASSWORD` was absent, satisfying the required fail-closed missing-credential behavior without a false pass.
- The broad pre-existing Builds package-exception inventory remains non-passing outside the G-6 affected set: it expects EventStore admin CLI `3.48.0` but observes `3.82.0`, and names absent ChatBot and Timesheets repositories. The G-6 pin/packet audit passes; no acceptance is claimed for those unrelated inventory entries. The Conversations full Integration test project also retains 16 pre-existing CA2007 warning-as-error failures, so the changed SDK assertion was run after suppressing only CA2007 for that focused build.

**Commands:**
- Builds restore/build and focused module/evidence executables -- expected: clean build and all tuple/schema/gate mutations pass.
- Changed owner AppHost/package-governance tests -- expected: `10.0.400` / `13.5.3` / central-catalog resolution without stale targets.
- EventStore OQ8 method, 21 support selectors, and validator at runtime `1.18.2` -- expected: zero skips, two identities, stop/survivor/restart, exact persisted replay.
- Projects restore/build, Integration tests, managed restart smoke, and evidence validation -- expected: tuple observed and artifacts hash-valid; missing credentials block rather than pass.
- `git diff --check` in every changed repository -- expected: no whitespace errors.
