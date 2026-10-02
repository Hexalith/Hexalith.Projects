---
title: "Sprint Change Proposal: Scope G-6 to the Current Runtime Tuple"
date: 2026-10-02
status: approved
approved: 2026-10-02
approved_at_utc: 2026-10-02T08:35:08Z
approved_by: Jerome
workflow: bmad-correct-course
review_mode: batch
proposal_approval: approved
handoff_status: routed
implementation_status: prepared-pending-integration-and-qualification
change_scope: moderate
trigger: "Accepted G-6 attempt 16 became inapplicable after routine gitlink movement and a real CommunityToolkit Dapr pin change."
affected_epics: [6, 7, 8]
primary_gate: G-6
supersedes: none
---

# Sprint Change Proposal: Scope G-6 to the Current Runtime Tuple

## 1. Issue Summary

On 2026-10-01, Jérôme Piquot accepted G-6 attempt 16 for its exact committed
source and the `13.6.0-beta.910` CommunityToolkit Dapr tuple. Its isolated
PostgreSQL two-sidecar restart qualifier passed 1/1 and the deterministic support
matrix passed 33/33. That acceptance remains valid historical evidence for the
source it names. On 2026-10-02, the current checkout no longer matches that
scope: the Builds catalog pins `13.6.0-preview.1.261001-0243`, nine packet-bound
submodule gitlinks have advanced, and the CI Builds action still names the old
Builds SHA. Running the status-aware G-6 gate exits 1 with 11 checks reported.

The new Toolkit preview is a substantive tuple change and needs current
qualification and a named owner disposition. The other failures expose a scope
problem: the G-6 packet binds 13 repositories and 12,244 source-file hashes,
including sources outside the runtime/toolchain decision. An unrelated gitlink
advance forces full recapture and new packet acceptance even if the controlled
tuple, runtime path, and focused proof are unchanged. `sprint-status.yaml` says
the historical packet is accepted and usable for its exact scope; it does not
answer whether the current checkout is qualified. These two truths are being
read as one status.

The current release workflow has a manual `allow_stale_g6` exception that can
accept a failed exact-source CI run if only the two G-6 jobs fail. It tests job
outcomes, not whether G-6 failed because of provenance-only drift, a changed
version, a failed runtime test, or an unavailable qualifier. It is therefore a
temporary operator exception, not replacement G-6 evidence.

### Evidence checked

- Accepted packet: `_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/attempt-16/packet.json`, reviewed pending SHA-256 `b7f940e512e17bcad98b021e4a7d4b9acce4f0c9fba4d3dc23f9d160245be139`.
- Current command: `PYTHONDONTWRITEBYTECODE=1 python3 tests/tools/run_g6_candidate_gate.py --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/attempt-16/packet.json` → exit 1; central Toolkit pin drift, nine gitlink mismatches, and CI Builds execution SHA mismatch.
- Baseline: `references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json`; current pin: `references/Hexalith.Builds/Props/Directory.Packages.props`.
- The accepted packet records checkout-source proof, not qualification of published EventStore `3.110.0` archives, G-4, G-5, release, deployment, or Dapr.Workflow.

## 2. Impact Analysis

| Area | Impact and disposition |
| --- | --- |
| PRD | FR-1–FR-25 and NFR-1–NFR-11 remain unchanged. NFR-11 still blocks release on failed critical cases, unexplained skips, or unavailable environments. Addendum §8 should index the approved G-6 policy change after approval; no main PRD semantic edit. |
| Epic 6 | Story 6.1 and package 6.1-P0 continue to require applicable G-6 qualification. Replace the implied all-repository packet applicability test with current tuple and tested-runtime-path applicability. P1R package/archive limitations, P0 stages 2–7, P2–P4, and implementation readiness remain separate blockers. |
| Epic 7 | No story, order, or product scope change. Its affected persisted/restart lanes consume the current G-6 result; an unavailable or failed lane is not passed. |
| Epic 8 | The Epic 8 entry rule and 8.3-P1/8.8-P2 Web prerequisites need a current G-6 result for the actual Fluent/Toolkit/Fluxor composition. Story 8.11 still requires the complete AD-30 and NFR-11 terminal evidence and Jerome + John acceptance. No story ID or evidence row key changes. |
| Architecture | AD-30 remains fail-closed and machine-checkable. Revise the G-6 row, Stack observations, and qualification note to distinguish immutable historical attempt-16 acceptance from current applicability. Do not silently transfer owner acceptance to the new Toolkit version. |
| UX | No screen, interaction, accessibility, or companion UX requirement changes. Existing FrontComposer/Fluent requirements remain. Only the 8.3-P1 dependency wording that still says Fluent V5 RC needs correction to the selected stable version. |
| Code and CI | Builds owns tuple audit and focused qualifier; EventStore owns the OQ8 fixture; Projects owns CI selection and release source checks. Replace fixed packet recapture on every root gitlink advance with current-source CI proof. Record all current gitlinks as provenance without comparing them to a historical packet. |
| Historical records | Preserve all G-6 packets, failed attempts, reviewed bytes, named decisions, and prior P1R dispositions unchanged. A new result cannot rewrite their scope. |

### Required safety boundary

G-6 must continue to check effective package/runtime versions, the approved
support-table exception, and the actual isolated two-sidecar persisted restart
behavior. The output must distinguish `qualified`, `failed`, and `not verified`.
The selected source-mode test must not claim published EventStore archive
compatibility. AD-30 readiness, G-4/G-5, P1R, and release acceptance remain
independent.

## 3. Recommended Approach

Choose **direct adjustment inside the existing G-6 prerequisite**. No new epic,
user-value story, FR/NFR, or rollback of completed work is needed. The MVP and
Story 8.11 terminal release rule remain unchanged.

1. Define one small versioned G-6 policy manifest for controlled versions and
   approved exceptions. Audit effective resolved versions, including transitive
   Toolkit and Dapr packages, against the manifest. A tuple or exception change
   needs a new named Builds/Platform/FrontComposer-Web owner decision. The
   accepted attempt-16 decision does not approve the current Toolkit preview.
2. Run the focused PostgreSQL two-sidecar restart/authority qualifier and its
   necessary deterministic support checks against the **current CI checkout**
   when the tuple or tested runtime/fixture path changes, and again on the exact
   release source. Build the affected AppHosts/consumers in the supported
   source and package modes. A failed, skipped, or unavailable critical run is
   not qualified. Keep safe isolated-resource cleanup and source/package
   distinction.
3. Emit a compact immutable CI evidence artifact: tested root SHA and root
   gitlinks, a reviewed fingerprint of material runtime inputs, controlled
   effective versions and resolved graph, runner/fixture
   identity, test commands and results, environment, cleanup outcome,
   limitations, approval reference, and artifact digest. Treat gitlinks as
   provenance of this run. Reuse tuple approval and non-release runtime evidence
   only while their material-input fingerprint is unchanged; do not require
   every gitlink to equal an older packet or hash every repository source file.
   Exact-source release qualification checks the current main SHA and its fresh
   run artifact.
4. Keep ordinary build, contract, and project gates running for every change.
   G-6 reports `not required by this change` when its controlled tuple and
   tested runtime path are unchanged; this is not a `passed` qualification
   claim. A release always requires a current-source focused G-6 run. Retire
   the broad `allow_stale_g6` path when the replacement is in use; it must
   never count as G-6 qualification during transition.
5. Make the status model explicit: `historical accepted packet` versus `current
   checkout qualification`. Update only the latter from a current CI run.

**Effort:** Medium, provisionally 3–5 engineering days plus the isolated live
qualification and owner review. **Risk:** Medium to high until trigger coverage
and release negative controls pass. The main risk is excluding a behavior-changing
source path; mitigate it with a reviewed affected-path inventory, effective
resolved-package audit, exact-source release run, and negative tests that mutate
the tuple, fixture, runtime configuration, failed result, missing result, and
approval. Schedule impact is limited to gate implementation and one fresh run;
no epic sequencing change is proposed.

Rollback of the policy change restores the previous G-6 jobs and packet
selection. Runtime/package rollback restores prior pinned versions and reruns
restore, build, and qualification; no domain data rewrite is authorized.

## 4. Detailed Change Proposals

These are proposed edits for approval, not applied authority. Historical
accepted records remain immutable.

### Change 1 — Revise the frozen G-6 intent and evidence contract

**Artifact:** `_bmad-output/implementation-artifacts/spec-g-6-current-runtime-toolchain-qualification.md` (frozen intent, Boundaries & Constraints), followed by the Builds baseline/evidence contract and Projects CI scripts after approval.

**OLD:** “Dirty source remains pending; immutable acceptance requires committed closure and exact gitlinks.” The current gate compares every packet-bound gitlink to the root index and validates a 12,244-file source manifest.

**NEW:** “Historical acceptance stays bound to its recorded tuple and execution source. Current G-6 applicability requires an approved effective tuple and passing runtime proof whose reviewed material-input fingerprint still matches. Gitlinks identify the tested checkout; a change to an unrelated gitlink alone does not revoke tuple approval or matching runtime proof. A changed controlled tuple, tested runtime path, critical result, or exception requires a new current run and, for a new tuple/exception, named owner disposition. Release runs the proof again at its exact current source SHA and gitlinks. A dirty local checkout cannot claim current qualification.”

**Rationale:** Retains reproducible current-source proof while removing whole-workspace recapture churn. This edit renegotiates a human-owned frozen instruction; implementation must not quietly change the validator first.

### Change 2 — Update Architecture Spine G-6 and AD-30 interpretation

**Artifact:** `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` (§Stack, §External capability entry gates, current G-6 observation).

**OLD:** G-6 is described through the September packet and the October attempt-16 exact gitlinks; Stack rows say “current G-6 acceptance pending” despite a later accepted historical packet.

**NEW:** G-6 is a versioned, current-source tuple qualification with explicit `qualified`/`failed`/`not verified` states and a separate historical acceptance record. The October attempt-16 result remains accepted only for its named beta.910 checkout. The current `.261001-0243` Toolkit pin is unqualified until a fresh focused run and named exception/tuple disposition. AD-30 continues to reject unavailable or failed critical evidence and does not infer downstream readiness from G-6.

**Rationale:** Removes contradictory current/historical status language and keeps release evidence truthful.

### Change 3 — Adjust Epic 6/8 dependency language and tracking

**Artifacts:** `_bmad-output/planning-artifacts/epics.md` (§External entry gates, §Epic 6 gate and 6.1-P0 ledger, §Epic 8 gate, 8.3-P1 entry), `_bmad-output/implementation-artifacts/sprint-status.yaml`, and affected story/package entry sections.

**OLD:** “G-6 applies before any affected build, runner, or evidence lane is claimed passing”; 8.3-P1 requires G-6 “accepted at immutable revisions, including the approved Fluent UI V5 RC and Fluxor governance”; tracking shows `G-6.status: accepted` and `current_accepted_usable: true` without a current-checkout field.

**NEW:** “An affected lane may claim G-6 qualification only from an approved controlled tuple and passing runtime proof for its material inputs; release additionally reruns on the exact current source. Historical packet acceptance does not supply current applicability after relevant drift. Unrelated gitlink movement remains governed by ordinary repository gates.” 8.3-P1 names the selected Fluent UI V5 stable version and current Toolkit/Fluxor composition. Tracking preserves historical attempt-16 acceptance and separately records the current tuple as `not verified` until a new qualifying run; downstream Story 6.1 and Epic 8 states do not advance on the planning edit.

**Rationale:** Makes prerequisite decisions usable without giving an old packet new authority.

### Change 4 — Index the decision without changing product scope or UX

**Artifact:** `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md` §8 after approval. The main PRD and UX specification need no semantic edit.

**OLD:** The evidence index records earlier G-6 dispositions and NFR-11 containment, but not this change in applicability policy.

**NEW:** Add a dated evidence-index row for this approved proposal, naming the revised G-6 scope, decision owner, implementation state, and unchanged NFR-11/Story 8.11 boundaries. Do not mark a new tuple qualified in that row.

**Rationale:** Maintains the PRD-to-gate audit trail without revising product requirements.

### Change 5 — Replace the CI/release path with measured current proof

**Artifacts after approval:** Builds G-6 validator/baseline/evidence schema; EventStore OQ8 qualifier fixture as needed; Projects `tools/qualification/run_g6_qualification.py`, `tests/tools/run_g6_candidate_gate.py`, `tests/tools/check_g6_packet_gitlinks.py`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`, related focused tests and runbook.

**OLD:** CI validates a fixed attempt-16 packet against every current root gitlink; release can use `allow_stale_g6` if both G-6 jobs fail while named non-G-6 jobs pass.

**NEW:** CI validates effective tuple/policy and produces a small G-6 result bound to tested source and material inputs. Relevant changes and release run the focused live qualifier. Release accepts only a fresh qualifying artifact for its exact source SHA and current approved tuple. The broad stale-job exception is removed when replacement coverage is operational.

**Rationale:** Makes the gate evaluate the current risk and prevents an administrative bypass from covering a real version change.

## 5. Implementation Handoff

**Scope:** Moderate direct adjustment. Product Manager/Product Owner keep Epic 6–8 IDs and sequencing; Solution Architect approves the AD-30/G-6 interpretation and the frozen-intent revision; Builds Owner implements the policy/effective-version validator and evidence format; EventStore Owner owns the isolated persisted qualifier; Projects CI/Release Engineering integrates exact-source checks and retires the broad bypass; FrontComposer/Web Owner reviews Fluent/Toolkit consumer coverage; Test Architect verifies negative controls and the current live result. Jérôme Piquot gives the explicit decision on this proposal and, separately, on any newly selected prerelease or unsupported tuple.

**Order:** (1) approve this policy and frozen-intent replacement; (2) implement focused evidence and regression controls; (3) update architecture/epics/addendum/tracking without rewriting attempt 16; (4) run the new Toolkit tuple on current committed source; (5) obtain named tuple/exception disposition; (6) switch CI/release to the new check and remove the broad exception; (7) run independent readiness assessment as required by PRD §2.5. No G-4/G-5, P1R, Story 6.1, or Story 8.11 status advances merely from this proposal.

**Acceptance criteria:**

- A controlled package or Dapr/Aspire/Fluent/Toolkit version change is detected from effective resolved versions and cannot reuse old approval.
- A changed qualifier/fixture/runtime configuration, a failed or skipped critical test, missing cleanup, unavailable environment, stale artifact SHA, or missing owner decision cannot yield `qualified` or release authorization.
- An unrelated gitlink advance does not invalidate an otherwise applicable tuple decision; normal build/test gates still run, and release uses a fresh exact-source qualifier.
- Current root SHA, gitlinks, package graph, test command/results, environment, cleanup, limitations, and approval are readable from one compact result; historical attempt-16 bytes and decisions are unchanged.
- Current Toolkit `.261001-0243` remains `not verified` until it passes the fresh runtime suite and receives its own named disposition. The result makes no claim about published EventStore archives, G-4/G-5, P1R, readiness, or Story 8.11.

## 6. Checklist Disposition

| Item | Status | Finding or follow-up |
| --- | --- | --- |
| 1.1–1.3 Trigger, problem, evidence | [x] | Story 6.1 prerequisite G-6; exact command and version/gitlink failures recorded above. |
| 2.1–2.5 Epic scope, dependencies, order | [x] | Epics 6–8 affected through entry rules; no new epic, renumbering, or resequencing. |
| 3.1 PRD | [x] | NFR-11 remains; addendum index after approval; main PRD semantics unchanged. |
| 3.2 Architecture | [!] | Revise G-6/Stack/current observation and frozen spec after approval. |
| 3.3 UX | [N/A] | No UX flow or component change; 8.3-P1 dependency wording updated in epics. |
| 3.4 Other artifacts | [!] | Builds/EventStore/Projects CI and release code, tracking, tests, runbook after approval. |
| 4.1 Direct adjustment | [x] | Viable; medium effort, medium-to-high transition risk. |
| 4.2 Rollback | [N/A] | Reverting accepted history would not solve applicability scope. |
| 4.3 MVP review | [N/A] | No FR/NFR release scope reduction. |
| 4.4, 5.1–5.5 Approach and proposal | [x] | Direct adjustment, explicit edits, owners, criteria and sequence above. |
| 6.1–6.2 Review and accuracy | [x] | Proposal reconciled with PRD NFR-11, AD-30, epics, UX, packet and current gate. |
| 6.3 Approval | [x] | Jerome approved the complete proposal on 2026-10-02. |
| 6.4 Sprint tracking | [x] | Historical acceptance retained and current checkout marked `not-verified`; a later qualifying run alone can advance it. |
| 6.5 Handoff | [x] | Routed to Solution Architect, Product Owner, Builds/EventStore/Projects implementers, FrontComposer/Web Owner, and Test Architect under §5. |

## 7. Approval and Handoff Record

**Proposal decision:** Jerome explicitly replied “approve” on 2026-10-02 after
review of this complete proposal. Approval authorizes Changes 1–5 and the
replacement of the frozen G-6 applicability policy. It does not approve the
new Toolkit preview tuple, transfer attempt-16 acceptance, authorize a release,
or advance a downstream prerequisite. The §5 handoff is routed for
implementation to Builds, EventStore, Projects CI/Release Engineering,
Solution Architect, Product Owner, FrontComposer/Web Owner, and Test Architect.
Sprint tracking now distinguishes historical acceptance from the current
`not-verified` tuple; historical attempt-16 acceptance remains intact.

## 8. Implementation record (2026-10-02)

The Builds v3 policy and validator, Projects current qualifier and CI selector,
release workflow, focused tests, runbooks, and planning references are prepared
in the working trees. CI audits every checkout. It runs the live qualifier for
material changes, runtime-owning gitlink changes, uncertain change ranges, and
scheduled runs; unrelated documentation or AI Tools changes report `not required
by this change` without claiming qualification. Release always runs and validates
a fresh proof before NuGet login. The broad `allow_stale_g6` dispatch input has
been removed. Historical packet bytes and the v1/v2 validator remain unchanged.

The current preview is recorded as a **pending** candidate, not an approved
tuple or exception. Material-input reuse is disabled until Platform's transitive
file-based restore graph is covered; a qualified non-release result must still
match its exact source. This conservative implementation limits the planned
reuse benefit but does not reintroduce packet reacceptance on unrelated changes.
Focused Projects tests (32), Builds validator controls (3), workflow policy
gate, production-authority index, YAML parsing, and whitespace checks passed.

Current G-6 remains `not verified`: Platform directly pins Toolkit beta.910,
the preview has no named tuple decision or live qualification, and the local
Builds and Conversations checkouts differ from root gitlinks. Code is not
staged or committed; root gitlinks were not changed. A pending-preview candidate
can collect technical live evidence after source integration, but its result
cannot become `qualified` until the Platform pin, proof, and owner decision are
resolved. No release or downstream readiness status is advanced.
