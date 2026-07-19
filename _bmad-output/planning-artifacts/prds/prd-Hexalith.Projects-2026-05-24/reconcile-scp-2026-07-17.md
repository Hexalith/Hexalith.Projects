---
input: sprint-change-proposal-2026-07-17.md ("Externalize Story 6.1 Platform Prerequisites", approved 2026-07-17 by Jerome, scope major, handoff routed)
date: 2026-07-19
verdict: no-prd-edits-required
---

# Reconciliation Extract: SCP 2026-07-17 vs PRD prd-Hexalith.Projects-2026-05-24

## A. Does the proposal decide the PRD/addendum question itself?

Yes. The proposal decides — in its own body, not only in frontmatter — that neither
`prd.md` nor `addendum.md` changes.

**Frontmatter** (proposal lines 15–18): `preserves:` lists `prd.md`, `addendum.md`, and
`ARCHITECTURE-SPINE.md`.

**Body confirmation, deciding language:**

- §2 Impact Analysis → "PRD Impact" (lines 71–74):
  > "No PRD change is required. FR-2, FR-5, NFR-1, NFR-5, and NFR-10 remain in scope without
  > weakening, deferral, or renumbering."
- §6 checklist, Section 3 (lines 477, 479):
  > "[x] 3.1 PRD remains valid without edits." and "[x] 3.3 UX remains valid without edits."
  Item 3.4 enumerates the artifacts that DO require correction — "Story/spec, epics, sprint
  tracking, context, traceability, and evidence artifacts" — and the addendum is not among them.
- §2 "Secondary Artifact Impact" (lines 134–149) lists every artifact requiring coordinated
  updates (epics.md, story/spec, sprint-status.yaml, epic-6-context.md, traceability matrix,
  deferred-work.md, Architecture Spine + central package files conditionally). Neither `prd.md`
  nor `addendum.md` appears.
- §7 (lines 508–513): the approved local application aligns "Epic 6, Story 6.1, its
  specification, sprint status, Epic 6 context, and the readiness matrix" — again no PRD/addendum.

**Structural note:** unlike prior proposals that carried an explicit §4.x "PRD modification"
decision, this proposal has no §4 entry targeting the PRD at all (§4.1–§4.8 cover story,
work packages, epic gate text, version normalization, spec, sprint status, traceability,
containment). The absence is deliberate and consistent with §2 and checklist 3.1: the body
treats the PRD/addendum as authority it operationalizes, e.g. Evidence bullet (lines 57–58):
> "The PRD addendum requires dependency and version gates to be verified before affected
> stories start."
which paraphrases prd.md §2.5 (line 90) accurately.

**Conclusion A:** The proposal itself decides "no PRD/addendum modification"; frontmatter and
body agree.

## B. Cross-check of every PRD-level artifact the proposal cites

| Cited | Where in proposal | Exists in PRD/addendum? | Characterization match |
| --- | --- | --- | --- |
| FR-2 Open Project | §1 Classification (l.63), §2 PRD Impact (l.72), §4.7 (l.390) | prd.md §6.1, l.197–206 | Match. Story 6.1 "list and open projects" = FR-5 + FR-2; proposal treats them as the value contract, unweakened. |
| FR-5 List Projects | same | prd.md §6.1, l.231–241 | Match. |
| NFR-1 Security and privacy | §2 (l.72), §4.7 (l.391) | prd.md §7, l.477 | Match. Fail-closed, tenant/actor/action scoping is exactly what P2 (query security) and P3 (production identity) supply evidence for. |
| NFR-5 Performance and scale | same | prd.md §7, l.484 | Match. List/open latency envelope; linking it to P2/P4 + G-4 runner evidence is coherent. |
| NFR-10 Compatibility | same | prd.md §7, l.492 | Match. Version normalization (P1) preserves the additive-contract obligation. |
| "PRD addendum requires dependency and version gates verified before affected stories start" | Evidence, l.57–58 | prd.md §2.5 l.90 (routing sentence); addendum §4.3, §7.1, §8 (the tracked gates) | Match — accurate paraphrase of §2.5. |
| AD-6, AD-14, AD-19, AD-20, AD-25, AD-30 | §1 Evidence, §2 Architecture Impact | Not in PRD/addendum — Architecture Spine artifacts | Correctly downstream. Their PRD-level shadows exist and agree: AD-19 safe denial ↔ prd.md §5 `Denied` "discloses no protected Project/component metadata" (l.173) and addendum l.21 safe-denial 404 note; AD-20 dual principal ↔ prd.md §2.1 "service callers act only with delegated actor authority" (l.32) + Service/Workflow Caller row (l.105) + addendum §1.1 "carry real credentials and delegated service identity"; AD-25/AD-30 evidence tooling ↔ NFR-11 + addendum §7. |
| G-2, G-3, G-4, G-5, G-6 gates | §1, §2 Epic Impact, §4.3 ledger | **Not defined in prd.md or addendum.md.** Defined downstream in epics.md (confirmed at epics.md l.1249–1250, l.1291, l.1759–1760). | Not drift: the PRD/addendum never used G-x vocabulary, and the addendum's routing section assigns "repository-local slices and dependency order" to epics/stories. Noted as a terminology boundary, not an inconsistency. |
| SM-x metrics | — | — | The proposal cites no SM IDs. Nothing to verify; no drift possible. |
| E-x evidence rows | — | — | The proposal cites no E-x rows. Addendum §8 index ends at E-9; see section D. |

**Drift found:** none. Every PRD-level ID cited exists at the same ID with a matching
characterization. The only observation is that the G-x gate vocabulary and the 6.1-P0..P4
packages exist solely in epics.md/sprint artifacts, which is where the addendum's routing
section says dependency order belongs.

## C. Conflict scan against .memlog.md decisions

Checked every recorded decision/override; **no conflicts found**.

- **FR-22→FR-24 supersession** (memlog l.19, l.42; addendum "Supersession trace"): proposal
  never touches FR-22/FR-24, export placement, or FR count. No conflict.
- **Folder-gated creation override** (memlog l.41: Active only after exactly one authorized
  Folder + read-model confirmation): proposal does not touch creation semantics. No conflict.
- **Production identity fail-closed** (memlog l.40 "production identity"; addendum §1.1
  "Production-capable hosts must not register allow-all development stubs..."): proposal P3
  ("production authentication and authorization composition mandatory and fail-closed;
  development-only bypass behavior... cannot activate in Production", l.294–303) **reinforces**
  the recorded decision — it externalizes delivery ownership, it does not alter the contract.
- **Release-gate story location** (memlog l.55: addendum names Story 8.9; 07-16 rerun moved the
  terminal gate to Story 8.11): the 07-17 proposal moves only the **Epic 6 entry gate** (into
  the 6.1-P ledger); it leaves Epics 7–8 "in sequence and unchanged in product scope" (l.110)
  and never names 8.9/8.11. No new conflict introduced; the pre-existing addendum staleness is
  handled in section D.
- **Release containment / freeze** (addendum "Corrective development and production release
  remain frozen..."): proposal §4.8 Immediate Containment and §7 ("does not approve any P0-P3
  capability... or sibling repository mutation") preserve containment. Consistent.
- **Epics 1–5 immutable history** (addendum; E-3): proposal l.114–115 "No completed Epic 1–5
  story should be rolled back... comparison and regression authority" — consistent.
- **No sibling-repository mutation by implication** (addendum §5, memlog): proposal §5
  "Repository-local implementation stories or issues must be created in each owning repository.
  This proposal authorizes none of those mutations by itself" — consistent.
- **Prior 07-16 reconciliations** (memlog l.53–54: both no-PRD-change, PRD preserved): this
  proposal continues the same pattern and reaffirms rather than contradicts.

## D. Deferred-items check (from the 2026-07-16 run, memlog l.55–56)

**Item 1 — discretionary E-10 evidence row for the two 2026-07-16 proposals.**
Status: **still open, still not applied.** Addendum §8 still ends at E-9 (addendum l.179).
Effect of this proposal: the backlog of unindexed approved evidence grows — the 07-16 proposal,
the 07-16 readiness rerun, the 07-17 readiness report, and now the approved Major 07-17
proposal (which redefines Epic 6 entry gating) are all absent from the index. The case for a
batched E-10+ addition is stronger, but it remains discretionary and non-blocking, and this
proposal's `preserves:` frontmatter forbids making that edit as part of applying *this*
proposal. It stays a separately approved addendum-hygiene revision.

**Item 2 — addendum names Story 8.9 as terminal release gate; rerun moved it to Story 8.11;
deferred "until epics materialize."**
Status: **revisit condition now plausibly MET, edit still deferred by this run's charter.**
- Addendum still names Story 8.9 as release gate in four places: the containment sentence
  ("Story 8.9 release evidence (E-8) passes", addendum l.18) and the E-3, E-4, E-8 index rows
  (l.173, l.174, l.178).
- The 07-17 proposal does **not** move or rename the terminal gate. It externalizes the Epic 6
  entry gates (G-4 runner → 6.1-P0; query security/watermark → 6.1-P2; G-5 production identity
  → 6.1-P3), which is orthogonal to the Epic 8 terminal gate.
- However, epics have now materialized: epics.md l.13 `releaseBlockedUntil: 'Story 8.11 passes
  with dated Jerome + John acceptance'`; l.1971 "supersedes the prior Story 8.9 gate"; and
  Story 8.9 has been **repurposed** as "Meet bounded performance and back-pressure objectives"
  (epics.md l.1917, owner of NFR-5/6/7). The addendum's "Story 8.9 release evidence" sentence
  therefore now points at a performance story, which is actively misleading rather than merely
  stale.
- Mitigation already in place: addendum §8 preamble ("A later artifact supersedes a row only
  when it explicitly identifies the earlier evidence...") plus the materialized epics.md keep
  the truth recoverable, so this stays non-blocking.

**Recommendation:** bundle both items into one future addendum revision (outside this run):
refresh the Story 8.9 → Story 8.11 gate naming in the containment sentence and E-3/E-4/E-8
rows, and add E-10+ rows for the 2026-07-16 proposals, the 07-16/07-17 readiness reports, and
the 2026-07-17 proposal.

## E. New-gap scan: new product-level requirement, constraint, or vocabulary?

Tested every construct the proposal introduces against this PRD's discipline
(capabilities-not-implementation; AD-x and mechanisms live downstream):

| Proposal construct | PRD-level counterpart | Verdict |
| --- | --- | --- |
| Prerequisite work packages 6.1-P0..P4, `blocked` story state, `blocked_by`, action ledger | None needed — planning/process artifacts | Must NOT enter PRD. Lives in epics.md/sprint-status (already applied per proposal §6.4). |
| Gate ledger G-2/G-3/G-6 applicability table | prd.md §2.5 already delegates gate verification "before affected stories start" | No PRD change. Epic-level dependency order is routed to epics/stories by the addendum's routing section. |
| Dual-principal identity (immutable actor + workload principal, delegation, scopes/audience) | prd.md §2.1 l.32 "service callers act only with delegated actor authority"; §3.2 Service/Workflow Caller row l.105; NFR-1; addendum §1.1 "real credentials and delegated service identity" | Product-level essence already present; envelope mechanics are AD-20/architecture. No gap. |
| Safe-denial 404 / forbidden-vs-nonexistent indistinguishability | prd.md §5 `Denied` l.173 "discloses no protected Project/component metadata" (existence IS protected metadata, so a distinguishable 403 already violates the PRD as written); SM-3 zero unauthorized disclosure; addendum l.21 safe-denial 404 note | Adequately covered. HTTP status mechanics are AD-19/contract-level. No required edit; at most an optional clarifying clause, not recommended now. |
| Global-position/projection watermark | prd.md read-model-confirmed completion, `asOf`, Evidence Freshness State; addendum §1.1 assigns cursors/read-model stores to the EventStore platform | Pure implementation seam (AD-14). Must NOT enter PRD. |
| G-4 persisted runner + machine-checkable evidence tooling | NFR-11 (authenticated evidence, "not verified" never "passed"); SM-6; addendum §7 | Verification mechanism, correctly downstream. No gap. |
| Production auth fail-closed at startup (P3/G-5) | NFR-1 fail-closed; addendum §1.1 no allow-all stubs in production-capable hosts | Covered at product level; the approved contract is platform-owned. No gap. |
| `blocked-external` readiness state, "planning coverage ≠ implementation readiness" | NFR-11 / SM-6 spirit (no misrepresented evidence) | Traceability-matrix vocabulary, not product contract. No gap. |

**Observation (no edit required):** prd.md §2.5's sentence that dependency/version gates are
"tracked in the addendum" remains true for the gates it referred to (addendum §4.3, §7.1, §8);
the new 6.1-P entry-gate ledger is epic-level dependency order, which the addendum routing
section already assigns to epics and stories. No accuracy correction is needed.

**Conclusion E:** the proposal introduces no product-level requirement, constraint, or
vocabulary missing from the PRD/addendum. Everything new is enablement sequencing, ownership,
and verification machinery that this PRD's discipline deliberately keeps downstream.

## Required PRD edits

**None.** The proposal explicitly requires none (§2 "No PRD change is required", checklist 3.1),
preserves both files in frontmatter, cross-checks clean against every cited FR/NFR, conflicts
with no recorded decision, and introduces no product-level gap. `prd.md` remains `status: final`,
`updated: 2026-07-15`, byte-for-byte unchanged; `addendum.md` unchanged.

## Recommended optional items

Deferred to a single future, separately approved addendum revision (not this run; this
proposal's `preserves:` forbids applying them here):

1. **Gate-name refresh (revisit condition now met):** update "Story 8.9 release evidence (E-8)"
   in the addendum containment sentence and the Story 8.9 references in E-3/E-4/E-8 rows to the
   materialized terminal gate Story 8.11, since epics.md now repurposes 8.9 as the
   performance/back-pressure story.
2. **Evidence-index extension:** add E-10+ rows covering the two 2026-07-16 proposals, the
   2026-07-16 readiness rerun, the 2026-07-17 implementation-readiness report, and the approved
   2026-07-17 externalization proposal (which now governs Epic 6 entry gating and the 6.1-P0..P4
   prerequisite ledger).
