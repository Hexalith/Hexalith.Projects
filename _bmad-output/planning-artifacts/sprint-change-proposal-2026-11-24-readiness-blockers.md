---
title: Qualify the remaining implementation-readiness inputs for Epics 6–8
project: Hexalith.Projects
date: 2026-11-24
mode: batch
status: proposed
application_status: pending-approval
classification: moderate
baseline_commit: 5f1d955b13c69a9c24b6a6c2d185770da2197355
---

# Sprint Change Proposal — Remaining readiness blockers for Epics 6–8

## 1. Issue summary

The November 24 readiness rerun returned **FAIL / NOT READY** after the approved Chatbot sequencing correction. The plan still puts both first-writer prerequisite packages behind the readiness freeze they help resolve. This proposal requests a bounded authorization to produce and qualify the missing inputs and asks the two authorized owners for a separate Epic 8 structure decision. [Readiness rerun](implementation-readiness-report-2026-11-24-rerun.md) · [Applied correction](sprint-change-proposal-2026-11-24.md) · [Section G](epics-architecture-conformance-checklist-2026-07-16.md#section-g--human-signoff-packet)

| Finding | Verified working-tree state |
|---|---|
| First-writer prerequisite cycle | The Epic 7 package table explicitly blocks both 7.1-P1 and 7.1-P2 on the global freeze. If applied to producing or qualifying their inputs, readiness waits for the package evidence while the package work waits for readiness. The tracker gives both actions the entire `Epic-7-gate` as a dependency, although 7.1-P2 supplies the relevant G-1/G-2 inputs. 6.7 is already `done`. Its creation-contract baseline is a real P1 input. P2 needs the applicable owner capability records for acceptance. Owners can prepare and independently qualify those records as part of the package work. [7.1 prerequisites](epics.md#story-71-prerequisite-work-packages) · [Tracker](../implementation-artifacts/sprint-status.yaml) |
| DW-63 and first writer | Story 7.1 is `blocked`. The DW-63 ledger is `open` and the spec is `in-progress`. The Server working tree calls the SDK host and EventStore has in-progress changes. The governed integration and SDK acceptance is outstanding. The 7.1 writer capability record is absent at its declared destination. [Ledger](../implementation-artifacts/deferred-work.md) · [DW-63 spec](../implementation-artifacts/spec-dw-63-migrate-projects-server-to-eventstore-sdk-host.md) · [Server entry point](../../src/Hexalith.Projects.Server/Program.cs) |
| Chatbot host and journey | 7.16 remains `backlog`. Both host and full P3 are `blocked-external`, and neither Projects pin is present at its declared destination. The nested host action lists registration, mount/session contract, compatible identity and FrontComposer, and the authenticated probe fixture. The full action additionally requires delivered 7.16 and the applicable journey capabilities. The corrected sequence is already in the epic, UX, matrix, and tracker. [8.8 packages](epics.md#story-88-prerequisite-evidence-packages) · [Chatbot UX](ux-design-specification.md#chatbot-companion-release-input) |
| 8.1 and governed validation | 8.1 is `blocked` at acceptance and 7.15 is `backlog`. The internal preview and local test history are present. The four declared TRX artifacts are absent at their canonical destinations. The Story 8.1 notes report managed-browser failures before Playwright, direct Builds 6.6.1 runner `HXC002` for the prescribed filters, and direct Builds validator exit 6 with seven existing matrix findings. The current Projects manifest exposes only FrontComposer. Running the prescribed validator from Projects again exited 1 because the tool is absent from the manifest. [8.1 spec](../implementation-artifacts/spec-8-1-inspect-task-audit-and-reconciliation-truth.md#implementation-notes) · [Tool manifest](../../.config/dotnet-tools.json) |
| Epic 8 owner decision | Both Section G decision rows and authorized architecture conformance clearance are pending. The listed base commit matches HEAD. Fourteen of the 15 listed artifact SHA-256 digests match, as does the separately listed trigger report digest. The `docs/read-cutover.md` digest in Section G, `fae0a27d76c755a14ec58aa46f20383b509c046c4d29a29e0d83542cf39cf2c3`, matches the HEAD blob. The current raw digest is `809f2b485431d6e1cebc281487282ad25d0ade125dd4ba6cc3defd1a7d402f76`. The working-tree diff adds three lines about fixture injection and the distributed transport lane. The runbook still requires the v4 comparison and separately executed rollback. [Section G baseline](epics-architecture-conformance-checklist-2026-07-16.md#baseline-artifacts) · [Runbook](../../docs/read-cutover.md#selection-and-rollback) |

## 2. Impact analysis

- **Epic 6.** Keep all eight story completions. The epic remains `in-progress` with `epic-6-retro-requalify-v4` open and 6.1-P4 `superseded`. Projects maintainer and Test Architect must bind 77 passing exact-source comparison cells and observed `Supported` selection to the intended revision and its gitlinks. After explicit `LegacyRollback`, execute all seven authenticated retained reads and bind their separate result to the same v4 packet digest. v3 receipts retain their original scope. A compatible creation-contract baseline is a P1 input. The v4 run remains a separate read qualification and part of the applicable DW-63 verification. [Epic 6 qualification](../implementation-artifacts/sprint-status.yaml) · [v4 runbook](../../docs/read-cutover.md#selection-and-rollback) · [DW-63 verification](../implementation-artifacts/spec-dw-63-migrate-projects-server-to-eventstore-sdk-host.md#verification)
- **Epic 7.** 7.1-P1 and P2 can qualify in parallel once their actual inputs are available. DW-63 follows its existing source-integration scope with one retained writer. A disposable, persisted first-writer qualification can then produce AD-17/AD-35 proof with the accepted contracts. 7.15 owns applicable legacy repair and durable recovery. 7.16 consumes the independently accepted writer capability and the host reference on their own merits. Full 7.1 FR-1/FR-25 integrated acceptance follows delivery of the 7.16 component where selection applies. [Epic 7 and writer contract](epics.md#epic-7-users-complete-durable-project-decisions-and-recover-them)
- **Epic 8.** The 8.1 scheduling exception covers early development. Its review and completion need accepted 7.1-P2 and 7.15, the authenticated persisted fixtures and blocking CI, plus the four evidence rows below. 8.8 requires all of 8.8-P1/P2/P3. 8.11 requires every preceding Epic 8 story, the immutable deployment/smoke, KMS and rollback packages, complete AD-30 evidence, and dated terminal decisions from Jerome and John. `release-smoke` remains `failed` (19 passed / 56 failed) until a matching full-release managed journey proves persisted end state and recovery on the exact candidate SHA with profile, run and job identity. G-4/G-5 and exact-source G-6 qualification remain open for the reviewed candidate. The G-6 acceptance recorded for `0f03582b3457a6d9212d60e2f9146a6043af5f7e` is scoped to that checkout. [Story 8.1 scheduling](sprint-change-proposal-2026-11-22-schedule-story-8-1.md) · [Section G gates](epics-architecture-conformance-checklist-2026-07-16.md#evidence-and-open-gates-on-this-baseline) · [Terminal release decision](epics.md#story-811-record-the-terminal-production-release-decision)
- **PRD, architecture and UX.** PRD §2.3 keeps FR-21, FR-22, FR-24 and every NFR release-blocking. The Spine already specifies ownership, additive history and contracts, replay and fencing, the exclusive Folder claim, and release evidence. The UX already specifies the separate host and journey checks. The proposed text implements those requirements through the existing backlog and evidence ownership. Delivery work lies with the source, fixture, runner and CI owners. The planning status references in PRD §2.5 and the compiled Epic 8 context predate the active tracker. [PRD](prds/prd-Hexalith.Projects-2026-05-24/prd.md) · [Architecture Spine](architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md) · [UX design](ux-design-specification.md#chatbot-companion-release-input)

## 3. Recommended approach

**Moderate direct adjustment.** Subject to explicit approval, allow just the two existing prerequisite packages to prepare, exercise, and present their contract proof under the freeze. Allow an isolated Projects first-writer qualification candidate only to produce the already-required AD-17/AD-35 receipts, once the applicable pinned inputs are accepted. Keep DW-63 under its existing authority and require its governed acceptance for the first writer. Cross-repository changes require repository-local authorization. Use immutable coordinates, deterministic persisted fixtures, controlled credentials, and a fenced single-writer topology. Keep all fixture writes inside the isolated test stores. Approval of this proposal authorizes the scoped planning correction and the defined qualification boundary. The named owners record their own acceptance of actual results.

The planning edit is small. The P1 and Chatbot host lanes are medium effort. P2 supply, SDK-host verification, replica/restart qualification and the 8.1 governed CI lane carry the largest effort and schedule uncertainty. Owner availability, source pin stability and fixture/tool reachability determine the critical path. Start independent host and tooling work alongside the first-writer prerequisites. Keep writes closed when a candidate cannot prove fencing or recovery. Rolling back the completed Epic 6 work would force requalification and leave the prerequisite cycle. A smaller MVP would require removing binding product or quality requirements and would leave the sequencing issue.

### Epic 8 decision for the Product Owner and authorized Solution Architect

| Option | Effect on the reviewed baseline |
|---|---|
| **Retain both tracks (recommended)** | 8.1–8.6 deliver internally consumable operator capability after their own entry and acceptance gates. 8.7–8.11 consume accepted outputs and separately owned qualification inputs for the same release. In particular, 8.1 needs Epic 7 capabilities and the Epic 8 shared fixture/CI prerequisites. It has no completion dependency on 8.7–8.11. Retention preserves the authority index, stable story/package/evidence identities and AD-30’s sole 8.11 terminal decision. |
| **Split operator value and release qualification** | Gives governance its own epic and would fit an independently owned qualification cadence. On the reviewed plan it adds an epic authority/index migration and requires reconciling story/package ownership, the Spine, PRD/UX links, CI and matrix references. The prior-only dependency order, complete evidence burden and terminal 8.11 decision still apply. |

The recommendation applies to base `5f1d955b13c69a9c24b6a6c2d185770da2197355`, the 14 matching Section G artifacts and the current runbook bytes identified in §1, with the baseline mismatch explicitly open. Preserve the original digest table as provenance. Before either owner signs, record a superseding exact byte manifest for the candidate they both review. If these planning edits are applied first, include their changed paths and hashes and reverify the affected dependency, traceability, and conformance checks. Both decisions must cite the same base commit and digest set, including the reviewed runbook. [Section G owner decision record](epics-architecture-conformance-checklist-2026-07-16.md#owner-decision-record) · [Epic 8 cohesion exception](epics.md#epic-8-cohesion-exception)

| Owner | Record in Section G |
|---|---|
| Product Owner | Name, authority, `ACCEPT` or `REJECT` retaining both tracks, decision date, exact base commit and digest set, and product rationale or conditions. Confirm internal operator value, sequencing and the common release boundary. |
| Authorized Solution Architect | Name and authorization, independent `ACCEPT` or `REJECT` for retention, decision date, the same base commit and digest set, and architecture rationale or conditions. Record a separate dated conformance verdict with findings or notes for the same bytes, including prior-only edges and AD-30. |

Both must accept to close cohesion. If either rejects, the Product Owner and Solution Architect commission a separate structural course correction and review every affected reference and scheduling guard before any migration. Revisit retention if the tracks gain independent release cadences, unrelated scope, a forward dependency, or a viable platform-owned qualification epic that preserves evidence identity. Owner decisions cover structure and conformance. Readiness, writer operation and release each retain their own acceptance records.

## 4. Proposed planning edits

Apply these exact replacements only after approval.

### 4.1 Epic 7 prerequisite work and first-writer gate

In [epics.md, Story 7.1 prerequisite work packages](epics.md#story-71-prerequisite-work-packages), replace the introductory paragraph with the following. Rationale — define the scope that can produce evidence while readiness remains frozen.

**Before**

```markdown
These packages are independently accepted enablement, not user-value stories or Story 7.1
implementation subtasks. Neither package delivers FR-1 or exposes a Project. Any external repository
change requires its own repository-local authorization and validation.
```

**After**

```markdown
These packages are independently accepted enablement. While implementation readiness is NOT READY,
owners may prepare and qualify only the pinned 7.1-P1 and 7.1-P2 inputs in isolated fixtures. For
7.1-P2, the G-1/G-2 owners may create and qualify the capability records that the package requires
for acceptance. Any external repository change requires its own repository-local authorization and
validation. The work produces prerequisite evidence only. FR-1 delivery, Project exposure,
story scheduling, production routing and release require their existing gates.
```

In the same table, replace just the last cell of each row.

| ID | Before | After |
|---|---|---|
| `7.1-P1` | `open; blocked by Story 6.7 contract cutover and the current implementation-readiness freeze` | `open; qualify against the accepted Story 6.7 creation-contract baseline under the bounded prerequisite scope above` |
| `7.1-P2` | `open; blocked by accepted G-1/G-2 capability records and the current implementation-readiness freeze` | `open; prepare and qualify the relevant G-1/G-2 owner inputs under the bounded prerequisite scope above; package acceptance requires the pinned owner capability records` |

In [Story 7.1, Entry gate](epics.md#story-71-activate-a-project-with-exactly-one-authorized-folder), distinguish the isolated proof from the cutover decision. Rationale — qualify the proof in a disposable candidate so cutover cannot become a prerequisite for its own acceptance.

**Before**

```markdown
- **Entry gate:** Epic 7 gate, Story 6.7, 7.1-P1, and 7.1-P2 accepted at immutable revisions with executable rollback. At the first writer gate, Projects must prove the AD-35 claim against pinned Folders authorization under G-2. Open DW-63 must also be accepted before writer cutover, with SDK-host compatibility and AD-17 evidence as specified in the Epic 7 prerequisite above.
```

**After**

```markdown
- **Entry gate:** Epic 7 gate, Story 6.7, 7.1-P1, and 7.1-P2 accepted at immutable revisions with executable rollback. After those inputs and DW-63 SDK-host acceptance, Projects may qualify the Folder-first writer in an isolated persisted fixture under the bounded prerequisite scope. Cutover requires accepted DW-63, the pinned contracts, AD-17 historical replay and command equivalence with old ingress drained and fenced, and AD-35 persisted exclusive claim, replica recovery, legacy-incumbent fencing and executable rollback proof. Before opening an affected writer slice, resolve its applicable legacy partial-record obligations under AD-17. If the retained writer cannot replay and write compliantly, keep mutations fenced and execute the approved forward recovery. Production routing and release remain governed by their existing approvals.
```

### 4.2 Tracker metadata for the two existing actions

In [sprint-status.yaml, action_items](../implementation-artifacts/sprint-status.yaml), replace the following exact lines within the named action only. Rationale — reflect the actual acceptance dependencies and the owners already required in the epic. This is a targeted metadata reconciliation after approval, with the complete isolated candidate validation in §5. Sprint regeneration has a separate READY requirement.

| Action and field | Before | After |
|---|---|---|
| 7.1-P1 `owner` | `    owner: "Projects Contracts Owner / Projects Server Owner / Test Architect"` | `    owner: "Projects Contracts Owner / Projects Server Owner / Product Owner / Test Architect"` |
| 7.1-P1 `depends_on` | `    depends_on: ["Epic-7-gate"]` | `    depends_on: ["Accepted Story 6.7 creation-contract baseline"]` |
| 7.1-P2 `owner` | `    owner: "EventStore-Platform Owner / Folders Owner / Solution Architect / Test Architect"` | `    owner: "EventStore Owner / Platform Owner / Folders Owner / Solution Architect / Test Architect"` |
| 7.1-P2 `depends_on` | `    depends_on: ["Epic-7-gate"]` | `    depends_on: ["Pinned G-1 Durable Task and applicable G-2 Folders owner records at package acceptance"]` |
| 7.1-P2 `routed_to` | `    routed_to: "Story 7.1"` | `    routed_to: "Story 7.1 first writer and Story 8.1 durable task and reconciliation acceptance"` |

### 4.3 Canonical containment and generated view

In the [canonical matrix](implementation-readiness-traceability-matrix.yaml), replace just the quoted value of `containment.freeze`. In the [Markdown view](implementation-readiness-traceability-matrix.md#status--containment), regenerate the text following `- **Freeze:** ` from that same value, without the YAML quotes. The two literal values below define both replacements. Rationale — expose the bounded authority without changing any evidence row, gate outcome or release disposition.

**Before**

```text
Broader historical NOT READY remains; approved 2026-11-02 E1-E7 permits 6.1 focused capability/spec readiness and 6.5 after accepted 6.1-6.4 plus usable FrontComposer authentication/current-Tenant/per-circuit credentials and DW-1 done. P0/P1R-remediation/P2/P3 remain open under Epic 8; P4 superseded. 6.7 retains exhaustive legacy equivalence and reversible routing; Epic 7 writer and Epic 8 qualification/release obligations remain.
```

**After**

```text
Broader historical NOT READY remains; approved 2026-11-02 E1-E7 permits 6.1 focused capability/spec readiness and 6.5 after accepted 6.1-6.4 plus usable FrontComposer authentication/current-Tenant/per-circuit credentials and DW-1 done. P0/P1R-remediation/P2/P3 remain open under Epic 8; P4 superseded. 6.7 retains exhaustive legacy equivalence and reversible routing. The bounded 7.1 scope permits prerequisite input preparation and qualification for 7.1-P1/P2 and isolated first-writer proof under the Epic 7 conditions. Epic 7 writer cutover, ordinary scheduling, Epic 8 acceptance and terminal release retain their gates.
```

### 4.4 Story 7.15 evidence reference and Story 8.1 acceptance

In [epics.md, Story 7.15 traceability](epics.md#story-715-reconcile-legacy-and-interrupted-workflows), replace the following exact suffix. The canonical `nfr-4` row already identifies 7.15 as a supporting story. This cross-reference correction preserves all 64 canonical keys.

**Before**

```markdown
evidence row `nfr-4-reconcile`.
```

**After**

```markdown
reconciliation evidence contribution to canonical row `nfr-4`.
```

In [epics.md, Story 8.1](epics.md#story-81-inspect-task-audit-and-reconciliation-truth), replace the first paragraph. Rationale — make the approved early-development boundary and the actual acceptance handoff explicit in the story.

**Before**

```markdown
Story 8.1 may enter development before Epic 7 completes. Its prior workflow dependencies, Epic 8 entry fixtures and required evidence still apply at acceptance. [22 November scheduling change](sprint-change-proposal-2026-11-22-schedule-story-8-1.md)
```

**After**

```markdown
Story 8.1 may enter development before Epic 7 completes. Review and completion require accepted 7.1-P2 task admission, committed status and receipt lookups, and Story 7.15's durable reconciliation capability. The Test Architect verifies `fr-21`, `nfr-8`, `finding-test-001` and `release-authenticated-persisted-boundary` with the Epic 8 authenticated fixtures, blocking CI and the governed evidence validator. Verify both eligible heads on the same persisted fixture. The Product Owner records story acceptance after the Test Architect records evidence and the Solution Architect confirms AD/G conformance. [22 November scheduling change](sprint-change-proposal-2026-11-22-schedule-story-8-1.md)
```

### 4.5 Chatbot full-journey compatibility handoff

In [epics.md, 8.8-P3 full Chatbot companion journey](epics.md#88-p3-full-chatbot-companion-journey), replace the recorder clause. Rationale — mirror the Projects compatibility check in the applied proposal and preserve the two stage-specific pins.

**Before**

```markdown
independent acceptance by the Chatbot Presentation and Test Owners, the Product Owner and
Test Architect in Projects record the immutable pin and evidence-row binding at
```

**After**

```markdown
independent acceptance by the Chatbot Presentation and Test Owners, the Projects Owner verifies
compatibility of the tested host, component and journey inputs. The Product Owner and Test Architect
in Projects then record the immutable full-journey pin and evidence-row binding at
```

### 4.6 Section G signing baseline

In the [Section G owner decision record](epics-architecture-conformance-checklist-2026-07-16.md#owner-decision-record), replace the paragraph after the owner table. Rationale — preserve the prior packet and bind both decisions and architecture clearance to the exact common candidate.

**Before**

```markdown
Record the base commit and applied digest set with each decision. The authorized Solution
Architect also records the conformance verdict and date against those bytes. Refresh the
digest set and reverify the affected checks if the planning baseline changes before signing.
Sprint regeneration requires a passing independent implementation-readiness gate.
```

**After**

```markdown
The November 24 follow-up verified fourteen listed digests and identified a working-tree
change in docs/read-cutover.md. The table above preserves the applied Chatbot correction's
baseline. Before signing, record a superseding byte manifest of the exact reviewed candidate,
including the current runbook and any applied planning changes. Reverify the affected checks.
Each owner records name, authority, decision, date, shared base commit and digest set, and
rationale or conditions. The authorized Solution Architect also records a separate conformance
verdict and date against those bytes. Sprint regeneration requires a passing independent
implementation-readiness gate. [Follow-up proposal](sprint-change-proposal-2026-11-24-readiness-blockers.md)
```

## 5. Evidence, approvals and implementation handoff

### 5.1 First-writer and Epic 6 evidence

| Input | Owner handoff, required artifact and acceptance |
|---|---|
| 7.1-P1 | Projects Contracts and Server owners supply an immutable compatibility/rollback packet at the accepted 6.7 creation-contract baseline. Supply exact clean-checkout commands, canonical and legacy/name-only fixtures, expected events or no effects, compatibility fingerprint and exercised rollback. Prove authorization before parsing, the exact FR-19 classification vocabulary and shared-validator parity, and `400 ValidationFailure` with no echo, task admission or command for invalid authorized input. Projects Owner, Product Owner and Test Architect record acceptance against the pin. |
| 7.1-P2 | EventStore and Platform supply G-1 pinned task/admission/confirmation, idempotency, leases, checkpoint, receipt/status and restart/two-replica records. Folders supplies the applicable G-2 expected-version, current actor/resource read/manage authorization, create-only sole-manager/no Tenant-default read, deterministic step keys, authoritative owner receipts/status and explicit compensation records. Provide exact clean-checkout commands, persisted end states, and exercised rollback. Verify equal retry, changed request, lost receipt/recovery and dependency failure without implicit bind or authority fallback. EventStore, Platform and Folders record their approvals. Solution Architect and Test Architect record conformance and evidence and accept the P2 package against those coordinates. Its accepted task contract also feeds 8.1. |
| DW-63 and v4 | Projects Owner accepts integrated SDK route/catalog, authorization, historical command/projection and supported-read regressions. EventStore Owner accepts the SDK behavior and pinned SDK capability. Retain the existing command processor as single writer during host qualification. Projects maintainer and Test Architect accept the separate exact-source v4 packet and seven-read rollback receipt as in §2. Attach compatible source and package verification and record the governed DW-63 disposition in its ledger/spec. |
| 7.1 AD-17/AD-35 and the writer capability | Projects Owner supplies the isolated, authenticated, persisted results. Include historical golden replay and command success/reject/no-op equivalence, old-ingress drain/fence, single writer, compliant rollback drill or mutation freeze with executable forward recovery, and relevant legacy/partial inventory and dispositions. With pinned current Folders authorization, race two Projects for one free Tenant/Folder key, lose the winner's reply, resume on another replica and recover the exact task/step claim and generation. Claim receipts and both authoritative Project histories must show at most one Active holder. Prove stale commit and release refusal, occupied legacy-incumbent fencing, closed ambiguous history, unknown outcome retaining its claim, and generation-safe rollback/recovery. The create-only no-Folder path must complete via read-model-confirmed activation. A supplied Folder without valid Selection Evidence returns `RequestPreview` where applicable. Projects Owner, Solution Architect and Test Architect record the dated qualified capability and all immutable inputs at `evidence/epic7/7.1-folder-first-writer-capability.json`. 7.16 consumes that record independently of 7.1’s whole-story status. |

### 5.2 Chatbot parallel qualification and downstream acceptance

**Preserve `8.8-P3-H → 7.16 → 8.8-P3 → 8.8 → 8.11`.** In parallel with P1/P2 and DW-63, the Chatbot Presentation and Test Owners can qualify the real confidential host using the pinned registration, mount/session contract, compatible FrontComposer and Platform Identity bindings, and a deterministic authenticated inert probe. Bind server-derived Tenant, original actor, workload/audience, interactive session and surface. Exercise refusal on missing, wrong, noninteractive, Tenant-role, revoked or changed bindings and cross-context attempts. Use the Projects fixture authority’s probe-confined session credential. Inspect zero Selection Evidence, Confirmation Artifacts, tasks or Project effects. Contain drift by disabling selection or restoring an independently reverified binding. Exercise rollback and bind the command, results and artifact digests to the immutable owner manifest. FrontComposer and Platform Identity attest their bindings. The Chatbot Presentation and Test Owners record host acceptance in their repository, the Projects Owner checks compatibility, and the Projects Product Owner and Test Architect record the accepted reference at `evidence/epic8/8.8-P3-H-chatbot-host-pin.json`. The host qualification needs neither the qualified writer nor the full journey to run. [Host contract](epics.md#88-p3-h-confidential-chatbot-host-prerequisite)

When the host and writer capability pins and remaining 7.16 entry gates are accepted, Projects delivers 7.16 and the Test Architect records executable `fr-25` component evidence. Keep positive Chatbot selection for 7.1, 7.3, 7.7, 7.9 and 7.14 downstream of the real component, with each consumer’s own authority and acceptance intact. The Chatbot owners can assemble fixtures and expected journey cases during host work. They accept the full journey only after exercising the shipped component, accepted host digest and applicable candidate, proposal, confirmation, task, recovery and response-admission capabilities on compatible pinned revisions. Include current authorization, expiry/replay and lost response, independent unbound-open and stale-link denial cases, and authenticated NFR-9 accessibility. The Chatbot Presentation and Test Owners record full acceptance in their repository, the Projects Owner verifies compatibility, and the Projects Product Owner and Test Architect record the full pin and row bindings at `evidence/epic8/8.8-P3-chatbot-companion-pin.json`. The Test Architect carries the full package into 8.8 with accepted P1/P2 and later 8.11. [Full journey and integration contract](epics.md#88-p3-full-chatbot-companion-journey)

### 5.3 Story 8.1 acceptance matrix

| Dependency or stable row | Accountable owner and artifact | Acceptance check |
|---|---|---|
| 7.1-P2 | EventStore/Platform and Folders owner capability manifests and approvals, reviewed by Solution and Test Architects. Projects pins the accepted task admission, committed status, authoritative receipts/recovery and authorization revisions in its 8.1 fixture. | Start an actual admitted Durable Task. Recover its committed checkpoint and original bindings, status and receipts across restart or a lost reply. The workflow transitions through the pinned authoritative owner. |
| Story 7.15 | Projects workflow owner on the pinned EventStore/Platform engine with applicable Folders evidence. Supply accepted per-record reconciliation inventory and durable task receipts/status, persisted histories, and native 7.15 restart/retry TRX/JSON bound to the candidate. Product Owner records completion with Test Architect evidence and Solution Architect conformance. | Prove quarantined folderless, pending-Folder and interrupted records reach truthful terminal or `NeedsAttention` dispositions without event rewrite, unsafe writers or auto-delete. Use that durable transition capability for 8.1 operator recovery. An authorized Administrator reconciles the existing task with original bindings and original-actor commit-time reauthorization. Denial records `Rejected` and one audit outcome. Unavailable authority has zero effects and changed targets or intent require a new Preview. Assert zero new tasks or Confirmation Artifacts. |
| `fr-21` — `pending` | Projects audit/workflow and security fact owners supply the committed input facts. Projects Web and McpCli owners supply the approved Contracts and enrolled audit read. Test Architect records `evidence/epic8/8.1-task-audit.trx` from `ops` / `Story=8.1`. | On the shared authenticated persisted fixture, verify Web and CLI return identical stable IDs and metadata-only truth. Cover committed admission, outcomes, confirmed mutations, confirmation use/cancel/refusal, denial, reconciliation, export audit and owner receipts. Verify inspection cardinality, Setup credential kind, role-specific re-anchoring disclosure, retry deduplication, safe pre-activation task status and same-Tenant denial. Use the applicable persisted producer facts or governed fixture inputs for the existing event contract. Later producer release proof stays with its owning story. |
| `nfr-8` — `pending` | Projects audit/retention owner and EventStore/Platform task, idempotency and artifact owners. Test Architect records `evidence/epic8/8.1-retention.trx` from `ops` / `Story=8.1&Trait=retention`. | Read persisted audit across restart and time boundaries for ≥365 days and any longer event-history obligation. Keep active tasks pollable until terminal and terminal results and scoped idempotency for ≥30 days or result lifetime, whichever is longer. Check the 15-minute Preview/Confirmation and 5-minute Selection Evidence contracts with their respective owner evidence. Prove Resolution Traces and export bodies are transient, with intermediate states and polls in telemetry. The local inspection-retention case covers only its exercised slice. |
| `finding-test-001` — `blocked-external` | Builds owns the compatible G-4 runner and filter support, Platform owns the real supported persisted fixture and authenticated environment, and Projects CI/Platform owners supply the blocking job with the CI/CD owner where required. Test Architect records `evidence/epic8/8.1-persisted-boundary.trx` from `ci-gates` / `Story=8.1`. | Require a blocking authenticated Story 8.1 run with persisted end-state assertions, restart/recovery, critical E2E, cross-Tenant denial and zero unexpected effects. Verify the browser assertions actually execute. Record candidate revision, fixture/runner version, job identity and native results. |
| `release-authenticated-persisted-boundary` — `blocked-external` | Platform/Identity and Builds owners supply the qualified gate and exact environment, with CI owners supplying the required release job. Test Architect records `evidence/release/persisted-boundary.trx` from `ci-gates` / `Category=persisted-boundary`, bound to the exact candidate. | Run the full authenticated persisted boundary on the real supported platform, including applicable 6.7 inputs, identity and owner authorization, durable read/write end state, restart/recovery and denial. Bind run/job, package or deployment coordinates, exact commands, result totals and artifact hashes. Story-only or historical local receipts do not close the release row. |

**Governed tooling.** Builds and Platform supply a supported immutable runner and evidence-validator pin and the approved Projects invocation, backed by the existing G-4/P0 owner route. Platform and Identity supply G-5 authenticated fixtures and the applicable G-6 owner qualification for the tested tuple. Resolve the native fixture blocker and unsupported filters with their owners. The Test Architect runs the four exact matrix invocations and `dotnet tool run hexalith-evidence validate _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml` through the approved tool binding. Retain validator version, dataset/revision, executed commands, diagnostics and exit status. Resolve reported matrix defects with their row owners. Accept only a successful governed validation at the applicable scope and real passing authenticated CI evidence with each required row bound to its native artifact. Failed, skipped, missing or unavailable cases retain their actual disposition. [Canonical matrix](implementation-readiness-traceability-matrix.yaml) · [Current 8.1 verification commands](../implementation-artifacts/spec-8-1-inspect-task-audit-and-reconciliation-truth.md#verification) · [G-4/P0 contract](../implementation-artifacts/spec-6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md)

### 5.4 Dependency and approval order

1. **Requester approval.** Approve or revise the exact planning edits and bounded qualification scope. Product Owner and Projects developer recheck the before text and apply the approved metadata changes as a coherent candidate. Capture the active and candidate indexes with the production-authority guard. For the candidate supply `--sprint-status`, `--deferred-work`, `--p0-artifact` and `--workspace-root` for one isolated workspace, including the existing P1R record at its fixed path. Revalidate the active index after any approved metadata publication. Preserve all story, action and evidence IDs and statuses, including completed stories. [Project context](../project-context.md)
2. **Parallel owner work.** Projects qualifies P1 against the accepted 6.7 baseline. EventStore/Platform/Folders qualify the relevant G-1/G-2 inputs and P2. Projects/EventStore finish and qualify DW-63 and Projects maintainer/Test Architect complete the applicable v4 proof. The Chatbot owners qualify only the host boundary under the applied correction. Builds/Platform/Identity/CI owners prepare the governed 8.1 lane independently of workflow delivery. The Product Owner and authorized Solution Architect can decide cohesion in parallel once both receive the same verified candidate baseline.
3. **Isolated first-writer proof.** After the required prior inputs, Projects qualifies AD-17/AD-35 in isolation and the three owners record the qualified writer capability. Keep the activation decision separate. Require applicable legacy repairs with their durable dispositions before opening any affected cutover slice.
4. **Independent readiness and sprint tracking.** After the applicable entry evidence and governance decisions are recorded, the independent assessor reruns readiness on the exact candidate. Record entry and scheduling gates separately from story acceptance and production release obligations. For example, passing 8.1 acceptance remains downstream of delivering 7.15. If the assessor returns `NOT READY`, keep ordinary scheduling and regeneration paused and report the remaining findings to the owners. The current guard pins `NOT_READY` and early statuses, including 7.1, 7.16 and 8.1. Each later authorized transition needs scoped guard/test reconciliation with actual accepted evidence, including the distinct host and full P3 pin checks from the applied correction. Regenerate only after independent `READY` and successful validation of the approved candidate.
5. **Downstream story and release gates.** Under the applicable scheduling authority, deliver and accept 7.15 with its prior workflow and legacy inputs. Once 7.1-P2, 7.15 and governed fixtures/CI are accepted, execute and accept the four 8.1 rows before its review/completion transition. With the host pin, qualified writer and other entry gates accepted, deliver 7.16. Then qualify full 8.8-P3, integrate 8.8 after P1/P2/P3, and submit the 8.11 terminal packet under its existing gates. Integrated supplied-Folder acceptance requires the delivered 7.16 component.

### 5.5 Review checks and approval boundary

- [x] Verified the reported blockers, active stable inventory (35 production stories and 64 evidence rows, covering 25 FRs and 11 NFRs), owner inputs and affected planning documents. The matrix currently records 20 pending, 42 blocked-external, one failed and one blocked.
- [x] Compared Section G’s manifest against current bytes and identified the runbook discrepancy for owner review. Both governance decisions remain pending.
- [x] The active index and an isolated copy of the current complete bundle both returned `[6, 7, 8]` from the production-authority guard. All 51 production-authority unit tests passed.
- [x] Matched the proposed before text to the current targets. The staged dependency model orders the Chatbot chain and detects cycles when either prerequisite again waits for readiness or when 7.16 again waits for full P3.
- [!] Governed validation is open. `run-with-temp dotnet tool run hexalith-evidence validate _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml` exited 1 with “Cannot find a tool in the manifest file that has a command named 'hexalith-evidence'.”
- [!] Requester approval, artifact application, evidence execution and acceptance, exact-baseline owner sign-off, readiness rerun and any sprint regeneration are pending. This document is the batch approval packet.
