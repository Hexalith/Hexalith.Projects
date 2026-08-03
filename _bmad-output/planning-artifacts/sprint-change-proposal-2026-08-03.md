---
title: "Sprint Change Proposal: Revalidate the Post-P1R EventStore and G-4 Baseline"
date: 2026-08-03
status: approved
approved: 2026-08-03
approved_by: Jerome
workflow: bmad-correct-course
review_mode: batch
edit_review: approved
proposal_approval: approved
handoff_status: routed
change_scope: moderate
trigger: "Revalidate the EventStore source, Builds catalog, Architecture Spine, and G-4 runner baseline after post-P1 dependency drift."
affected_epic: 6
affected_story: "6.1"
primary_action: "6.1-P1R"
supersedes_candidate_from: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-p1r-baseline-revalidation.md"
---

# Sprint Change Proposal: Revalidate the Post-P1R EventStore and G-4 Baseline

## 1. Issue Summary

The approved 2026-08-01 correction established EventStore `3.88.0` as an
unaccepted 6.1-P1R candidate while preserving `3.70.1` as the last accepted
baseline. The dependency state moved again before P1R acceptance. The current
workspace now contains five distinct coordinates:

1. the EventStore package release and Builds catalog select `3.89.0`;
2. the EventStore source checkout is nine commits after the `v3.89.0` tag and
   contains post-tag production changes;
3. the current Builds runner source and its schema/test/evidence corpus select
   `3.88.0`;
4. the latest published G-4 tools are `4.23.0` and embed EventStore `3.70.1`;
5. the Architecture Spine remains bound to EventStore `3.70.1`.

The core problem is therefore **post-P1 technical dependency drift across
source-mode, package-mode, runner-source, published-runner, and architecture
authority**. The 3.88.0 candidate was never accepted and cannot silently become
the rollback baseline. The current EventStore checkout is not the source of the
published 3.89.0 packages and cannot be described as such.

This does not invalidate historical P1, change product scope, or authorize
Story 6.1 implementation. It requires the existing P1R action to supersede its
stale candidate observation and qualify one explicit multi-coordinate baseline.

### 1.1 Revalidated state on 2026-08-03

| Surface | Current observation | Disposition |
|---|---|---|
| EventStore source checkout | `7854f8e51ce9b852bb6c3cac6012670122e93792`; `v3.89.0-9-g7854f8e5`; clean `main` | Exact source-mode candidate only; not package-source identity |
| EventStore package source | tag `v3.89.0`; revision `c590590bc581a3f72ef6e67148eda988ba4b8fe6` | Immutable package-mode candidate |
| EventStore release inventory | 14 package IDs; manifest SHA-256 `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae` | All 14 version `3.89.0` packages were listed by the official NuGet V3 API at observation time |
| Builds checkout | `7bdbd293991985d150dfca62f77709e61152de76`; `v4.23.0-37-g7bdbd29`; clean `main` | Exact current Builds observation |
| Builds catalog | 13 EventStore rows resolve through `HexalithEventStoreVersion=3.89.0` | Introduced by `10af541e7b2a5a4664be37c9495930844e0954a8`; candidate package binding |
| Builds package audit | EventStore rows still select/audit `3.88.0` | Fails current-catalog validation; 13 EventStore mismatches among 33 total mismatches |
| Current runner source | `SupportedPlatformPins.EventStoreVersion=3.88.0`; current schema, fixtures, and tests use the same pin | Internally consistent but stale, unaccepted, and not remotely published at this source revision |
| Published G-4 tools | `Hexalith.Builds.Module.Cli` and `Hexalith.Builds.Evidence.Cli` version `4.23.0`; release revision `7ac2849d79e603b88c7cb76e178cd2ba106eaf00` | Remotely available, but that release embeds EventStore `3.70.1`; it is not the current P1R runner candidate |
| Projects G-4 consumer | `.config/dotnet-tools.json`, `module/hexalith-projects.module.json`, and `evidence/g4/6.1-p0-acceptance.json` are absent | No pinned consumer, supported composition, or P0 acceptance record |
| Architecture Spine | EventStore package/source binding `3.70.1` / `f13f9925fdca53efa2ab8c90d396ab106f91bb9c` | Last accepted baseline; must not move before P1R acceptance |
| Rollback | `3.70.1` historical accepted baseline | Retain; `3.88.0` is an unaccepted intermediate, not a rollback promotion |

### 1.2 Source and compatibility evidence

The seven Story 6.1-dependent API files have identical Git blob identities at
`v3.88.0`, `v3.89.0`, and the current EventStore checkout. The additive
`QueryCursorScope.AddProjectionWatermark(long?)` change previously observed
between `3.70.1` and `3.88.0` therefore remains the only change in that focused
comparison.

The broader source checkout is not tag-equivalent. Between `v3.89.0` and
`7854f8e…`, three production files under `Hexalith.EventStore.Server` changed:

- `Commands/CanonicalIdempotencyIntentEncoder.cs`;
- `Commands/IdempotencyIntentAdapterRegistry.cs`; and
- `Configuration/ServiceCollectionExtensions.cs`.

Associated server tests also changed. P1R must consequently validate source
mode at `7854f8e…` and package mode from `v3.89.0`/`c590590…` as separate,
intentional coordinates. If the owners require identical source/package
behavior instead, EventStore must publish a new immutable release from the
selected source revision; P1R must not relabel `7854f8e…` as 3.89.0 package
source.

### 1.3 Executed diagnostic results

These results are diagnostic evidence only. They are not a clean-checkout P1R
or P0 qualification record.

| Check | Result |
|---|---|
| `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` | PASS — 49 approved identities and three shared versions |
| `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` | FAIL — 33 stale audit/catalog mismatches, including all 13 EventStore rows at `3.88.0` versus `3.89.0` |
| Module tests, Release, `--no-restore` | PASS on isolated rerun — 107/107 |
| Evidence tests, Release, `--no-restore` | PASS — 24/24 |
| Initial parallel Module/Evidence attempt | Module lane encountered `MSB4018` on a shared generated `.deps.json`; the isolated rerun passed. This attempt is retained as non-qualifying contention evidence, not hidden or counted as a product failure. |
| `dotnet tool search` against NuGet | Both G-4 CLI packages are listed at `4.23.0` |
| Official NuGet V3 flat-container lookup | All 14 EventStore release-manifest package IDs list `3.89.0` |

The passing runner tests prove internal 3.88.0 consistency; they do not prove
3.89.0 compatibility. The catalog test proves central binding structure; it
does not cure the stale package audit. Published tool availability proves a
remote artifact exists; it does not provide a supported 3.89.0 runner or a
Projects consumer pin.

## 2. Impact Analysis

### 2.1 Epic and story impact

- Epic 6 remains viable without scope redefinition.
- Story 6.1 remains blocked by the existing prerequisite chain:
  `6.1-P1R -> {6.1-P0, 6.1-P2} -> 6.1-P3 -> Solution Architect sign-off -> 6.1-P4 -> specification readiness -> independent READY`.
- Historical 6.1-P1 remains complete evidence for the accepted 3.70.1 tuple.
- 6.1-P1R remains open. Its 3.88.0 candidate evidence becomes historical
  superseded-candidate evidence, not an accepted or rollback baseline.
- 6.1-P0 remains in progress and blocked. Published 4.23.0 tools reduce package
  availability uncertainty but do not satisfy P0 because they embed 3.70.1 and
  Projects has no exact consumer manifests or acceptance record.
- P2, P3, P4, and Story 6.1 remain blocked with no premature status transition.
- Epics 7 and 8 are affected transitively wherever they depend on G-4 or the
  accepted EventStore tuple. No planned epic becomes obsolete.
- No new epic, story, action, resequencing, or priority change is required.

### 2.2 Artifact impact

| Artifact | Impact |
|---|---|
| PRD | No change. Product outcomes, FRs, MVP scope, and NFR-11 remain achievable. |
| UX specification | No change. No user flow, component, interaction, or accessibility contract changes. |
| Architecture Spine | Post-acceptance update required for the separate source/package coordinates, runner binding, rollback, and G-1 evidence wording. |
| Epic 6 / sprint ledger | Immediate factual correction required after proposal approval: replace stale 3.88-only observation with the five-coordinate state while keeping all gates open. |
| Traceability matrix | Update P1R pin narrative and G-4 tool status. A published-but-stale tool is not `not-available`, but remains `not-supported/not-accepted` for the candidate tuple. |
| P1R specification and record | Supersede the 3.88.0 candidate without rewriting its historical command evidence; add a dated revalidation section or sibling record for 3.89.0. |
| Builds catalog/audit | Catalog remains 3.89.0; refresh and validate the full audit against the exact accepted Builds revision. The 20 non-EventStore mismatches must not be concealed by a scoped pass. |
| Builds runner/schema/tests/evidence | Atomically move semantically current values from 3.88.0 to the accepted EventStore package pin; preserve explicit 3.88.0 stale-candidate and 3.70.1 rollback controls. |
| G-4 publication and consumer | Publish/identify an exact tool version containing the accepted runner pin, then add exact Projects tool/module manifests and the P0 acceptance artifact under P0 authority. |
| CI/test evidence | Repeat source-mode, package-mode, runner, audit, remote restore, negative controls, and rollback from clean exact revisions; retained evidence and hashes are required. |

### 2.3 Technical and delivery assessment

- **Classification:** Moderate. The correction is bounded to an existing action
  but crosses EventStore, Builds, Projects planning, architecture, and test
  ownership.
- **Implementation effort:** Medium for P1R alignment and documentation; P0
  persisted qualification remains its existing XL work package.
- **Current risk:** High until source/package divergence, the stale audit, and
  the unpublished current runner are resolved. Focused API compatibility lowers
  contract risk but does not establish operational parity.
- **Schedule:** Uncommitted. No credible date exists before exact owner-selected
  revisions and clean qualification are available.

## 3. Path Forward Evaluation

### Option 1 — Direct adjustment within 6.1-P1R: viable and recommended

Supersede the unaccepted 3.88.0 candidate, preserve its evidence historically,
and qualify an explicit source/package/runner/architecture tuple. This retains
the current epic structure and prevents dependency observations from becoming
implicit authority.

- Effort: Medium for P1R.
- Risk: High initially, reducible to Medium/Low only through the defined gates.
- Timeline effect: bounded but uncommitted.

### Option 2 — Roll back current dependencies: not recommended

Reverting the catalog and source checkout to 3.70.1 would recreate the last
accepted package/architecture alignment, but it would discard current
dependency progress, would not complete G-4, and would still require exact
source/package and rollback validation. No completed story needs to be undone.

- Effort: Medium.
- Risk: High due to regression and repeat-drift risk.
- Use: retain 3.70.1 only as the exercised operational fallback.

### Option 3 — Review or reduce PRD MVP scope: not viable

The drift is an implementation-authority and evidence problem, not a failure of
the product goals. Reducing MVP scope would not create a trustworthy persisted
runner or accepted dependency tuple.

- Effort: High and misdirected.
- Risk: High to business value, with no technical resolution.

### Recommendation

Select **Option 1: Direct adjustment**. It has the smallest authority-preserving
change surface, keeps source and package modes truthful, and uses the existing
P1R/P0 gates rather than creating parallel governance.

## 4. Detailed Change Proposals

### Change 1 — Correct the P1R observation without accepting it

**Artifacts:**

- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.{md,yaml}`

**Old current-state model:**

```yaml
candidate_baseline: "3.88.0"
eventstore_source:
  version: "3.88.0"
  revision: "4843b492dff7c16a4bc74db67509263f969c78c6"
builds_catalog:
  version: "3.88.0"
runner_candidate_version: "3.88.0"
architecture_version: "3.70.1"
```

**New current-state model:**

```yaml
candidate_baseline: "multi-coordinate-3.89.0-pending-acceptance"
eventstore_source_checkout:
  revision: "7854f8e51ce9b852bb6c3cac6012670122e93792"
  describe: "v3.89.0-9-g7854f8e5"
  package_equivalent: false
eventstore_package_source:
  version: "3.89.0"
  tag: "v3.89.0"
  revision: "c590590bc581a3f72ef6e67148eda988ba4b8fe6"
builds_catalog:
  version: "3.89.0"
  introducing_revision: "10af541e7b2a5a4664be37c9495930844e0954a8"
builds_runner_source:
  version: "3.88.0"
  revision_observed: "7bdbd293991985d150dfca62f77709e61152de76"
published_g4_tools:
  version: "4.23.0"
  release_revision: "7ac2849d79e603b88c7cb76e178cd2ba106eaf00"
  embedded_eventstore_version: "3.70.1"
architecture_version: "3.70.1"
rollback_baseline: "3.70.1"
decision: "pending owner selection, alignment, clean qualification, and four-owner acceptance"
```

P1R remains `open`; P0 and Story 6.1 remain blocked. Planning text must call
3.88.0 a superseded unaccepted candidate wherever it appears as the current
state.

### Change 2 — Supersede, do not rewrite, the P1R qualification evidence

**Artifacts:**

- `_bmad-output/implementation-artifacts/spec-6-1-p1r-revalidate-platform-baseline.md`
- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md`

Append a dated supersession section or create a dated sibling record. Preserve
the original 3.88.0 commands and results as historical candidate evidence. The
new evidence must record:

- source checkout `7854f8e…` and its post-tag production diff;
- package source `v3.89.0` / `c590590…`, 14-package manifest hash, and remote
  availability evidence;
- Builds catalog provenance `10af541…` and the exact qualifying Builds revision;
- focused API blob comparison plus source-mode/package-mode behavior results;
- audit, runner, package, remote restore, negative-control, and rollback results;
- explicit containment of the superseded 3.88.0 candidate; and
- dated EventStore Owner, Builds Owner, Solution Architect, and Test Architect
  dispositions.

If the owners select a newly released EventStore revision instead of the
observed two-coordinate candidate, the record must name that immutable tag and
revision explicitly before runner alignment proceeds.

### Change 3 — Align Builds catalog evidence and runner source atomically

**Owner repository:** `references/Hexalith.Builds`

After EventStore-owner selection:

1. retain `HexalithEventStoreVersion=3.89.0` if `v3.89.0` is accepted, or apply
   one owner-approved catalog change if another immutable release is selected;
2. refresh the entire package-version audit against the exact catalog, including
   the 20 current non-EventStore mismatches, and require the validator to pass;
3. move `SupportedPlatformPins.EventStoreVersion`, schema enums, current
   positive fixtures, serialized evidence, and coupled hashes from 3.88.0 to
   the selected version;
4. preserve explicit controls that reject both the stale 3.88.0 candidate and
   the 3.70.1 rollback pin while the new runner is active;
5. in a clean rollback worktree, accept 3.70.1 and reject the new candidate;
6. rerun Module, Evidence, authoritative-catalog, audit, packaged-command, and
   clean source/package lanes without shared-output contention; and
7. record the exact Builds revision. A passing test corpus still bound to
   3.88.0 is not acceptance evidence for 3.89.0.

This change aligns P1R runner source. It does not by itself complete P0 or prove
the supported persisted composition.

### Change 4 — Rebind the Architecture Spine only after P1R acceptance

**Artifact:**
`_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md`

**Old binding:** EventStore package `3.70.1`, source
`f13f9925fdca53efa2ab8c90d396ab106f91bb9c`, Builds catalog/runner 3.70.1,
rollback 3.67.3.

**Proposed binding after acceptance:** record separately:

- the exact accepted EventStore source-mode revision;
- the exact immutable EventStore package version/tag/source revision;
- the exact Builds catalog and runner-source revision;
- the Architecture acceptance date and owners; and
- rollback to the last accepted 3.70.1 tuple with executable evidence.

For the currently observed candidate, those first two EventStore coordinates
are `7854f8e…` and `3.89.0`/`c590590…`; they may be adopted only if both modes
pass and the owners accept their intentional difference.

Update G-1 wording to reference the accepted package/runner evidence without
claiming that P1R selects the Durable Task engine or Confirmation Artifact
capability. G-1 remains independently gated. Do not edit the Architecture
binding based solely on this proposal or a passing focused API comparison.

### Change 5 — Correct the G-4 publication and consumer baseline under P0

**Artifacts:**

- `references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`
- `_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`
- `_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml`
- future Projects `.config/dotnet-tools.json`
- future Projects `module/hexalith-projects.module.json`
- future Projects `evidence/g4/6.1-p0-acceptance.json`

Replace `published_tools: not-available` with a precise state: version 4.23.0 is
published, but it embeds the historical 3.70.1 runner and is not the accepted
P1R/P0 consumer baseline. Retain `supported_composition: blocked`,
`published_consumer_pin: absent`, `persisted_qualification: not-run`, and
`owner_acceptance: absent`.

After P1R accepts the runner source:

1. publish or identify an exact Builds tool version containing that accepted
   runner pin;
2. remotely restore both packages from a clean consumer;
3. check in the exact Projects local-tool manifest and module manifest;
4. execute supported `run`, `test`, `down`, evidence-validation, negative, and
   rollback lanes; and
5. produce `evidence/g4/6.1-p0-acceptance.json` with named Builds Owner,
   Platform Owner, and Test Architect approval.

These are P0 completion conditions. Until they pass, the existence of published
4.23.0 packages must not change G-4 rows to passing or unblock P4.

### Change 6 — Propagate acceptance without widening scope

After the finite P1R record passes and all four P1R owners accept the exact
coordinates:

- change P1R from `open` to `done` with exact revisions and the evidence link;
- update the Architecture Spine atomically with the accepted record;
- mark only P0 Stage 1 complete so P0 can consume the baseline;
- retain P0's supported-composition, persisted-qualification, consumer-pin,
  acceptance-record, and owner-acceptance gates;
- retain P2, P3, P4, Story 6.1, and all transitive Epic 7/8 gates; and
- rerun implementation readiness only after the existing prerequisite chain is
  complete.

No dependency update, source change, publication, submodule move, story
creation, or completion transition is authorized by this proposal alone.

## 5. Implementation Handoff

### 5.1 Roles and responsibilities

| Recipient | Responsibility |
|---|---|
| Product Owner / planning maintainer | Apply factual state corrections, preserve the dependency chain, and prevent premature status changes |
| EventStore Owner | Select and attest the exact source-mode and package-mode coordinates; require a new release if divergence is unacceptable |
| Builds Owner / Developer | Align audit and runner source, preserve negative controls, qualify the exact revision, and later publish the P0-consumable tool version |
| Solution Architect | Approve the multi-coordinate binding, rollback, Architecture Spine change, and G-1 clarification |
| Test Architect | Independently verify clean source/package, runner, remote-restore, negative, and rollback evidence |
| Platform Owner | Under P0, accept the supported G-4 composition, consumer manifests, and persisted qualification |

### 5.2 Required sequence

1. On proposal approval, update current-state planning observations while
   leaving every gate open.
2. EventStore Owner selects the exact source/package coordinates.
3. Builds Owner aligns catalog evidence and runner source at one exact revision.
4. Test Architect runs and retains clean P1R qualification and rollback proof.
5. All four P1R owners accept the finite record.
6. Solution Architect and planning maintainer update Architecture and close only
   P1R.
7. P0 publishes/restores the aligned tool version, adds consumer manifests,
   proves persisted composition, and obtains its separate three-owner
   acceptance.
8. Continue the existing P2/P3/sign-off/P4/readiness sequence.

### 5.3 P1R success criteria

- Source mode and package mode each name an exact immutable revision and are
  never conflated.
- The accepted EventStore package pin, Builds catalog, current runner source,
  schema, fixtures, and audit agree.
- The seven required APIs and affected source/package behaviors pass the
  defined compatibility lanes.
- Candidate and rollback worktrees exercise reciprocal exact-pin controls.
- The full package audit passes at the accepted Builds revision.
- The EventStore Owner, Builds Owner, Solution Architect, and Test Architect
  approve the same evidence record.
- P1R alone closes; P0 and Story 6.1 remain blocked.

### 5.4 P0 success boundary retained

P0 additionally requires exact published tool packages containing the accepted
runner, a checked-in Projects consumer pin and module manifest, real persisted
multi-module qualification, the independently valid P0 acceptance record, and
three-owner acceptance. P1R success cannot be substituted for these outcomes.

## 6. Checklist Disposition

| ID | Status | Finding |
|---|---|---|
| 1.1 | Done | Trigger is existing action 6.1-P1R under Story 6.1. |
| 1.2 | Done | Technical dependency drift across source, package, runner, publication, and architecture authority. |
| 1.3 | Done | Exact revisions, package listings, file comparisons, test results, audit failure, and missing consumer artifacts recorded. |
| 2.1 | Done | Epic 6 remains completable through the existing prerequisite package. |
| 2.2 | N/A | No epic scope, acceptance-criteria, addition, removal, or redefinition needed. |
| 2.3 | Done | Epics 7 and 8 retain transitive dependency impact only. |
| 2.4 | N/A | No future epic is invalidated and no new epic is required. |
| 2.5 | Done | Existing sequence and priority remain correct. |
| 3.1 | Done | PRD goals and MVP remain achievable; no PRD edit required. |
| 3.2 | Action-needed | Architecture source/package/runner binding and G-1 wording require post-acceptance correction. |
| 3.3 | N/A | No UI/UX impact identified. |
| 3.4 | Action-needed | Builds audit, runner corpus, qualification records, G-4 publication/consumer artifacts, tests, and planning docs require updates. |
| 4.1 | Viable | Direct adjustment; Medium P1R effort, High initial risk. |
| 4.2 | Not viable as primary | Rollback remains an operational fallback; it does not solve G-4 or current dependency alignment. |
| 4.3 | Not viable | MVP review would not address the technical/evidence defect. |
| 4.4 | Done | Option 1 selected for authority preservation and bounded scope. |
| 5.1 | Done | Issue and discovery context summarized. |
| 5.2 | Done | Epic and artifact impacts documented with six concrete changes. |
| 5.3 | Done | Recommendation, rationale, alternatives, and trade-offs documented. |
| 5.4 | Done | No MVP change; action plan, dependencies, and sequence defined. |
| 5.5 | Done | Cross-repository roles and responsibilities assigned. |
| 6.1 | Done | All applicable checklist sections addressed; open actions are explicit. |
| 6.2 | Done | Proposal reconciled against current clean repository revisions and executed diagnostics. |
| 6.3 | Done | Jerome explicitly approved the complete proposal on 2026-08-03. |
| 6.4 | N/A | No epic/story add, remove, or renumber operation is proposed; approved factual sprint-state edits are routed as implementation work. |
| 6.5 | Done | Moderate-scope Product Owner / Developer handoff, owner responsibilities, uncommitted schedule, and success criteria confirmed by approval. |

## 7. Approval and Handoff Record

- **Approval:** Approved by Jerome on 2026-08-03.
- **Scope:** Moderate.
- **Review mode:** Batch.
- **Approved path:** Direct adjustment within 6.1-P1R, followed by the existing
  P0 and Story 6.1 gate sequence.
- **Primary handoff:** Product Owner / Developer agents for the six approved
  changes and backlog-state reconciliation.
- **Required owner participation:** EventStore Owner, Builds Owner, Solution
  Architect, Test Architect, and—during P0—Platform Owner.
- **Implementation authority:** Approved for implementation handoff, subject to
  repository ownership and every named acceptance gate. This approval does not
  itself execute changes or authorize commits, pushes, releases, or dependency
  updates without a corresponding implementation request.
- **Next action:** Apply the factual state correction, obtain EventStore owner
  selection, align and qualify Builds, accept P1R, then continue the separately
  governed P0 and Story 6.1 prerequisite sequence.
