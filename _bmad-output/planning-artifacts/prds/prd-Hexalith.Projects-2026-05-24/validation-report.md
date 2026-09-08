# Validation Report — Hexalith.Projects

- **PRD:** `_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/prd.md`
- **Rubric:** `.agents/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at:** 2026-09-08T11:29:00+02:00
- **Grade:** Poor

## Overall verdict

This PRD holds up as a chain-top product contract: §2.1 names real trade-offs, §2.4 makes omissions explicit, and most FRs carry testable consequences under a shared §5 recovery contract. What is at risk is silent product invention — UX, architecture, and stories must still decide what makes a match exclusive enough to skip confirmation, which evidence is required for `Complete` versus `Partial`, and how to treat brownfield folderless Projects that the addendum admits and the PRD's present-tense invariant denies. `status: final` is earned for direction; it is not earned as a claim that no product questions remain for downstream work.

The isolation review materially shifts the gate: the confirmation boundary and Conversation membership can be satisfied on paper while still silently attaching work or moving membership without a move. Planning-drift does not remumber FRs, but the folder is stale as a current baseline — §2.5 still reads phase-unblocked while addendum E-17 is `NOT READY` and later approved SCPs were never reconciled here.

## Dimension verdicts
- Decision-readiness — strong
- Substance over theater — strong
- Strategic coherence — strong
- Done-ness clarity — adequate
- Scope honesty — adequate
- Downstream usability — adequate
- Shape fit — strong

## Findings by severity

### Critical (2)
**[Adversarial isolation]** — Actor-selected vs inferred is not a server fact; SingleCandidate can durable-attach without FR-14 (§1, FR-6, FR-9, FR-10, FR-12, FR-14, SM-C1)
Vision asks for confirmation only when intent is ambiguous. FR-14 binds Preview solely to multiple candidates. Nothing defines how the server distinguishes actor-selected from inferred, so a `SingleCandidate` or authorization-collapsed hit can be submitted as a silent link.
Fix: Treat any association whose Project id originated in Resolution, proposal, or attachment match as inferred. Require FR-14-class confirmation for every inferred bind, including `SingleCandidate`. Reject `conversation.link` unless the request carries an actor picker artifact or a consumed confirmation.

**[Adversarial isolation]** — Conversation unlink plus actor-selected link bypasses FR-7 and breaks exactly-one membership (FR-6, FR-7, FR-11, §2.1, §4)
FR-11 allows a legal zero-Project Conversation. The next actor-selected FR-6 link then skips FR-7’s dual-Project Preview and audit. Memlog intent that membership change is an auditable move is not an FR.
Fix: Forbid Conversation unlink, or classify any link to a previously membered Conversation as a move with the same dual-target Confirmation Artifact and audit action.

### High (11)
**[Done-ness clarity]** — Required versus optional evidence for Complete / Partial is not enumerable (§5; FR-2, FR-16, FR-20)
`Complete` requires “other required evidence”; `Partial` treats “optional references” as excludable. The required set is never listed per purpose, so stories will disagree on whether a missing Memory or File blocks admission.
Fix: Per response purpose, name the required components and state that File References and Memories are optional unless Project Setup marks them required.

**[Done-ness clarity]** — SingleCandidate exclusivity is unspecified, so confirmation can be skipped on a weak match (§1; FR-12, FR-13, FR-14; SM-C1)
FR-14 applies only when resolution returns multiple candidates. A sole `MetadataMatched` hit can skip Preview. This is the common path, not an edge, and is the product-side twin of the first critical finding.
Fix: State the exclusivity rule in §2.1 / FR-12 (for example: `SingleCandidate` requires `ConversationLinked` or `ProjectFolderMatched`, or any sole metadata/file/memory hit must use FR-14).

**[Adversarial isolation]** — Same-Tenant is not an invariant on links, moves, or resolution inputs (NFR-1, FR-6, FR-7, FR-9, FR-10, FR-13)
Authorization on both ends can still link Tenant A’s Conversation or Folder into Tenant B’s Project.
Fix: Add a fail-closed same-Tenant predicate on every reference. Authorization on both ends is necessary and not sufficient.

**[Adversarial isolation]** — “Permitted Projects” has no grant, share, or deny model (§3.2, FR-2, FR-5, FR-16, NFR-1, SM-3)
No FR creates, lists, or revokes actor permission. Implementers will default to Tenant-wide Project User access.
Fix: Specify the v1 actor-authorization model and grant/revoke operations, or declare Tenant-wide access and drop the actor-leakage claim.

**[Adversarial isolation]** — Same-name Folder creation can attach a foreign workspace (FR-1, FR-19, FR-23, addendum §5)
Neither create nor restore says create-only, nor forbids binding an already-existing same-name Folder or another Project’s anchor.
Fix: Same-name means a create that fails closed on name collision. Binding an existing Folder requires an actor-selected Folder id plus confirmation.

**[Adversarial isolation]** — The Folder is a cardinality rule, not an exclusive authorization anchor (§2.1, FR-8, FR-16, UJ-4)
Nothing says a Folder is bound to at most one Active Project. Two Active Projects can share the same Folders ACL.
Fix: Require exclusive Folder binding for Active Projects, or declare shared-Folder as an explicit multi-Project workspace.

**[Adversarial isolation]** — Delegated callers can confirm and mutate if they hold actor credentials plus the artifact (§2.1, §3.2, FR-14, addendum §1.1)
A workflow that requests Preview as the actor can consume the artifact in the same hop.
Fix: Bind every action decision to the original actor. Do not return a consumable confirmation token to Service/Workflow Callers.

**[Adversarial isolation]** — Tenant Project Administrator reconciliation is an unbound authority expansion (FR-22, FR-1, FR-7, FR-14, FR-15, FR-23)
No FR lists what reconciliation may change. An Administrator can finish membership without the original Confirmation Artifact bindings.
Fix: Reconciliation may only retry or compensate an already-admitted task against its original bound targets. New intent requires a new Preview.

**[Adversarial isolation]** — “Metadata-only” still ships Setup, names, and titles through operator read, traces, and export (FR-3, FR-17, FR-21, FR-22, FR-24, §2.4, NFR-1)
FR-24 has size/row caps but no field allow-list. Operators get prompt-adjacent Setup and human-readable titles.
Fix: Publish an allow-list of identifiers, lifecycle, versions, and safe codes. Exclude Setup bodies, names, titles, paths, and Preview bodies unless a separate content-inspection permission exists.

**[Planning-drift]** — prd.md §2.5 readiness wording conflicts with addendum current disposition (`prd.md` §2.5; addendum E-17)
§2.5 says no phase-blocking product questions remain for UX, architecture, or epic decomposition. Addendum E-17 is `NOT READY` with live Playwright 19/56 and a production freeze.
Fix: Keep FR/NFR text; revise §2.5 so planning-status language matches E-17 / Story 8.11 and later unreconciled SCPs.

**[Planning-drift]** — Later approved SCPs were never reconciled into this PRD folder (`.memlog.md`; addendum §8)
Memlog last reconciles 2026-07-17. Addendum index stops at E-18 and omits 2026-07-31 through 2026-09-02 proposals that still bind planning (Story 6.8, freshness map, export retry, AD-33).
Fix: Run an addendum-only reconcile pass and update `.memlog.md`. Do not edit FR/NFR bodies unless a later reconcile proves a contract delta.

### Medium (17)
**[Substance over theater]** — Success metrics that only echo NFRs (§8 SM-1, SM-4, SM-5, SM-6)
Fix: Keep SM-2, SM-3, SM-7, and SM-8 as primaries; treat NFR restatements as release-acceptance checks under NFR-11.

**[Done-ness clarity]** — File and Memory link FRs omit the recovery contract used everywhere else (FR-9, FR-10 vs FR-6, FR-8, FR-11)
Fix: Give FR-9 and FR-10 the same task, idempotency, completion, and recovery consequences as FR-6.

**[Done-ness clarity]** — “Relink” and “manual reconciliation” are authorized and audited without a mutation FR (§3.2; FR-21, FR-22)
Fix: Define FR-level relink and reconciliation, or delete those words and map the permission onto existing FRs.

**[Done-ness clarity]** — SM-7’s unit of measure is not defined (§8 SM-7)
Fix: Define an episode and reuse SM-8’s context-correction definition or define Project correction explicitly.

**[Scope honesty]** — Present-tense folderless invariant contradicts acknowledged brownfield (NFR-4; FR-1; addendum §5)
Fix: State that legacy Active folderless Projects may exist and must be reconciled before list/resolution/context admission.

**[Scope honesty]** — Zero tagged assumptions on a green-light chain-top PRD (§2.5; §8; FR-1)
Fix: Tag the Chatbot measurement-companion and Folders same-name-create dependencies as `[ASSUMPTION]`, and downgrade §2.5.

**[Downstream usability]** — Domain nouns do not round-trip through the glossary (§4; §6.2; FR-17; NFR-5; §8)
Fix: Add Context Reference to §4; rename FR-17’s artifact or widen Resolution Trace; use one correction term.

**[Adversarial isolation]** — FR-6 does not inherit FR-7’s dual-membership fail-closed (FR-6, FR-7, NFR-4)
Fix: State the same atomic invariant on every membership mutation.

**[Adversarial isolation]** — FR-3 silently changes the context-assembly policy (FR-3, FR-16, §2.1)
Fix: Classify inclusion/exclusion policy as confirmation-required, or snapshot it into Conversation-start confirmation.

**[Adversarial isolation]** — “Relink” is in the role matrix and audit list but is not an operation (§3.2, FR-7, FR-8, FR-11, FR-21)
Fix: Strike “relink” or define it as FR-7/FR-8-class Preview for every reference kind.

**[Adversarial isolation]** — FR-24 permission is not a disclosure control; FR-22 pagination already dumps the tenant (§2.3, FR-5, FR-22, FR-24, NFR-5, NFR-6)
Fix: Cap or purpose-limit FR-22, or retract the “alternatives to unbounded troubleshooting” claim.

**[Adversarial isolation]** — Pre-activation “safe status” can leak reserved workspace intent (FR-1, FR-2, FR-5, FR-22)
Fix: Define the pre-activation allow-list: task id, Task Status, safe reason/recovery codes, expiry.

**[Adversarial isolation]** — FR-12 Denied vs NoMatch can distinguish a foreign Conversation (§5, FR-12, NFR-1)
Fix: Collapse Denied, NoMatch, and nonexistence for unauthorized or cross-Tenant Conversation inputs.

**[Adversarial isolation]** — File/Memory links escape the Folder authorization anchor (FR-8, FR-9, FR-10, FR-16)
Fix: Require File References to be descendants of the bound Project Folder, or require confirmation that names the foreign Folder/Project.

**[Planning-drift]** — 2026-08-02 bound export as synchronous/non-idempotent retry-may-resnapshot while claiming no PRD change (FR-24, NFR-4, NFR-6)
Fix: Add one FR-24/NFR-6 consequence, or record that implication in the addendum.

**[Planning-drift]** — Addendum still routes UI.Contracts through Story 6.2 / “33 stories” after 2026-09-02 created Story 6.8 (addendum §4.2, E-3)
Fix: Update addendum §4.2, E-3, and the evidence index to Story 6.8 and the post-09-02 story count.

**[Planning-drift]** — AD-33 role×surface matrix can be read as widening confirmation surfaces and tightening TPA additive-link authority (`prd.md` §3.2)
Fix: Tighten §3.2 one sentence each, or put that gloss in the addendum and cite AD-33 as non-widening.

### Low (6)
**[Done-ness clarity]** — Residual adjectives on otherwise testable FRs (FR-15, FR-19)
Fix: Require a suggested name when attachments or Conversation metadata can supply one; point “unsafe” characters at the addendum rule.

**[Downstream usability]** — SM-7’s source pointer does not resolve (§8 SM-7)
Fix: Cite “§8 Outcome measurement contract.”

**[Adversarial isolation]** — FR-19 “rejects secrets” has no detection rule (FR-3, FR-19, NFR-1)
Fix: Define rejectable classes and treat residual Setup as sensitive.

**[Adversarial isolation]** — Idempotency scope can cross Projects for the same actor (glossary, NFR-4, FR-1)
Fix: Include action targets in the idempotency scope, or treat target mismatch as a conflict.

**[Planning-drift]** — 2026-07-31 freshness canonicalization elaborates observable wire values the PRD glossary does not tabulate
Fix: Index the freshness SCP in the addendum; optionally add the producer-normalization table there or in architecture.

**[Planning-drift]** — Folder-internal provenance disagrees with itself (`.memlog.md` vs addendum E-17/E-18)
Fix: Append memlog events for the E-17/E-18 addendum patch and for this drift review; do not back-date `prd.md` `updated`.

## Mechanical notes
- Frontmatter is `status: final` with `updated: 2026-07-15`. The addendum’s controlling readiness row is E-17 / E-18 (2026-08-02 / 2026-08-03).
- FR-1–FR-24, NFR-1–NFR-11, UJ-1–UJ-5, SM-1–SM-8, and SM-C1–SM-C4 are contiguous and unique.
- Cross-references that do not resolve: SM-7 “defined above”; “relink” in §3.2/FR-21 (no FR); “Context Reference” (no glossary entry).
- No inline `[ASSUMPTION]` or `[NOTE FOR PM]` markers — a scope-honesty gap, not a clean bill of health.
- Every UJ has a named protagonist. Recovery Action Code `RequestPreview` is defined and never assigned a binding trigger.
- Glossary drift to watch: “read-model-confirmed” variants; “relink” vs “link” vs “move.”

## Reviewer files
- `review-rubric.md`
- `review-adversarial-isolation.md`
- `review-planning-drift.md`
