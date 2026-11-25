# Reviewer Gate (Validate intent) — PRD reconciliation lens

Target: \`ARCHITECTURE-SPINE.md\` (status final, updated 2026-11-24, SHA-256 \`01b89a2f5e076a52…\`, 600 lines, AD-1..AD-35) on Projects \`31b35637\`.
Inputs read: PRD \`prd.md\` (updated 2026-11-20) and \`addendum.md\`; \`.memlog.md\`; sprint-change proposals 2026-11-20, 11-21, 11-22, 11-22-schedule-story-8-1, 11-24, 11-24-readiness-blockers; \`deferred-work.md\` (DW-85); Story 6.3 spec; \`epics.md\`; \`ux-design-specification.md\` (grep); \`src/Hexalith.Projects.Contracts/Models/EvidenceFreshnessStateCode.cs\`.

**Verdict: PASS WITH FINDINGS.** Every FR-1..FR-25 and NFR-1..NFR-11 has a carrying AD, and the numeric envelopes match the PRD. One applied scope change (DW-85) is missing from the spine, and several quiet PRD rules were dropped from the AD text.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 1 |
| Medium | 5 |
| Low | 7 |

## Coverage table

Status: **C** = carried by the spine as written; **P** = partly carried (finding cited); **M** = missing.

| Req | Carried by (spine lines) | Status |
| --- | --- | --- |
| FR-1 Create | AD-3, AD-5, AD-8, AD-12, AD-13, AD-18, AD-22, AD-31, AD-35; admission table l.503–504 | C |
| FR-2 Open | AD-3, AD-14, AD-17, AD-32, AD-33 (v1 names l.414) | C |
| FR-3 Setup update | AD-15, AD-32 l.381 (idempotent, durable), AD-33 l.397, admission table l.503 | P (F-4) |
| FR-4 Archive | AD-5, AD-12, AD-33 l.398, AD-35 l.430 | P (F-13) |
| FR-5 List | AD-32 l.379, AD-33 l.414, AD-27 / conventions l.446 | C |
| FR-6 Link Conversation | AD-5, AD-10, AD-33 l.400 | C |
| FR-7 Move | AD-10 l.199, AD-13, AD-33 l.401 | C |
| FR-8 Set/replace Folder | AD-3, AD-13 l.219, AD-23, AD-33 l.403–404, AD-35, G-2 | C |
| FR-9 Link File | AD-5, AD-11, admission table l.504 (foreign Folder) | C |
| FR-10 Link Memory | AD-5, AD-11 | C |
| FR-11 Unlink | AD-5, AD-10, AD-33 l.402 | C |
| FR-12 Resolve (Conversation) | AD-14 l.227, AD-32 | P (F-13) |
| FR-13 Resolve (attachments) | AD-14 l.227 | C |
| FR-14 Confirm ambiguous | AD-5, AD-13, AD-33 l.393 | C |
| FR-15 Propose new | AD-5, AD-8, AD-31, AD-35 | C |
| FR-16 Context | AD-10, AD-11, AD-14, AD-20, AD-32 | P (F-1) |
| FR-17 Explain | AD-7, AD-32 l.381 | P (F-1) |
| FR-18 Refresh | AD-14 l.225, AD-32 l.381 | P (F-1) |
| FR-19 Validate Setup | AD-31, conventions l.443, l.447 | P (F-4) |
| FR-20 Conversation-start | AD-10, AD-32 l.381 | P (F-1) |
| FR-21 Audit | AD-26 | P (F-3) |
| FR-22 Operator read + reconcile | AD-4, AD-29, AD-33 l.405–412 | P (F-6, F-7) |
| FR-23 Restore | AD-23, AD-33 l.398–399, AD-35 | C |
| FR-24 Export | AD-21 | C (F-7 ambiguity) |
| FR-25 Select target | AD-5, AD-13, AD-20, AD-33 l.395 | P (F-2) |
| NFR-1 Security/privacy | AD-11, AD-20, AD-33, conventions l.447–449 | P (F-1) |
| NFR-2 Encryption/KMS | AD-28, G-5, Deferred l.592 | C |
| NFR-3 Availability/recovery | AD-28 | P (F-8) |
| NFR-4 Durability/idempotency | AD-3, AD-4, AD-5, AD-22, AD-28 | C |
| NFR-5 Scale/latency | AD-27 | C (F-9 reinterpretation) |
| NFR-6 Pages/export bounds | AD-21, AD-27, conventions l.446 | C |
| NFR-7 Back-pressure | AD-27, AD-19 | P (F-8) |
| NFR-8 Retention | AD-4, AD-5, AD-7, AD-13, AD-21, AD-26 | C |
| NFR-9 Accessibility | AD-34 | P (F-2) |
| NFR-10 Compatibility | AD-6, AD-16, AD-17 l.262, AD-22, AD-24, AD-31 | P (F-5) |
| NFR-11 Release evidence | AD-30 | C |
| A-1 Chatbot measurement companion | conventions l.450, AD-26 | P (F-10) |
| A-2 Folders create-only | AD-8 l.185, G-2 l.554 | P (F-10, fallback) |
| A-3 Folder-derived authorization | AD-3, G-2 l.554 | C |
| A-4 Legacy folderless inventory | AD-3, AD-17, AD-33 l.412 | P (F-10, minor) |
| A-5 Descriptive inspection permission | AD-20, AD-33 l.412–414, G-5 | C |
| A-6 Selection Evidence issuer | AD-5, AD-13, AD-20 | C |
| A-7 Interactive-session claim | AD-20, AD-29, AD-33 l.416, G-5 | C |
| §5 Observable contract | AD-19, AD-32 | C (asOf meaning: F-1) |
| Addendum §2 freshness normalization (E-20) | none | M (F-11) |
| §2.4 boundaries and v1 exclusions | Paradigm, AD-2, AD-9, AD-11, Deferred l.598, l.600 | C |
| Safe vs Descriptive Metadata | AD-18, AD-26, AD-33, conventions l.449 | C |
| §8 SM-1..SM-8, SM-C1..C4 | conventions l.450, AD-26, AD-27, AD-28, AD-30, AD-34 | C |
| Localization | PRD sets none; AD-18 keeps reason codes enumerated and name-free | N/A |

Sprint-change proposals vs spine: 10-21 advisory exception → AD-14 l.227 (reflected). 11-15 McpCli surface → AD-29 (reflected). 11-16 cutover decisions and amendments → AD-17 l.262–264, AD-19 l.291 (reflected). 11-20 audit → AD-20, AD-27, AD-28, AD-30 (reflected). 11-21 native membership, machine refusal and claim race → AD-10, AD-29, AD-35 (reflected, but not in \`sources\`). 11-22 v4 normalization → AD-17 l.264 (reflected). **11-22 Story 6.3/DW-85 scope change → not reflected (F-1).** 11-24 (both) → sequencing only, boundary table l.51–52 (reflected). DW-63 → AD-19 exception, AD-24 (reflected).

## Findings

### F-1 — Supported context reads skip request-time Folder authorization (DW-85); spine omits it

- **Severity:** High · **Status:** CONFIRMED
- **Spine anchor:** Implementation boundaries, Read features (l.50); AD-14 Rule (l.225); AD-20 Rule (l.297); AD-32 (l.377, l.381); Deferred RTI row (l.594).
- **Evidence:**
  - Spine l.50: "All eight Epic 6 stories and the Epic 6 retrospective are done." There is no exception.
  - Spine l.297, AD-20: the context includes "Folders read/manage evidence with freshness no older than the interactive dependency timeout". l.225, AD-14: "Normal context includes \`Current\` evidence; explicit refresh uses owner batch APIs".
  - PRD prd.md:544, NFR-1: "Authorization is recomputed at every decision from current Folders and Tenant-role evidence." prd.md:429, FR-16: "its authorization is re-checked at assembly time". prd.md:178: \`asOf\` is "the server timestamp of the authorization and evidence computation".
  - sprint-change-proposal-2026-11-22.md:48–52: "delivered supported Get and Explain against Project-owned persisted detail. Refresh is available on retained v1. A subsequent Folder revocation can affect these context reads only after their persisted evidence refreshes." This is promoted to DW-85.
  - deferred-work.md:777 (DW-85, open): "A later Folder revocation reaches context, setup and explain when their persisted evidence refreshes."
  - Story 6.3 spec l.708: "Get/Explain make zero owner calls." Its review log at l.354 records \`asOf\` as \`detail.UpdatedAt\`, the persisted evidence cutoff.
  - The spine cites the 11-22 proposal only for v4 (l.264). DW-85 appears nowhere in the spine.
- **Failure scenario:**
  1. A Folders owner revokes a user's Folder read.
  2. The supported context route (Supported selection, AD-17) keeps serving that user Setup bodies and references until the persisted detail refreshes.
  3. Chatbot admits a first response on \`Complete\`. That breaks SM-3 (zero unauthorized disclosures).
  4. Story 8.8/8.11 reviewers and the external Chatbot adoption owner read l.50 and AD-14/AD-20 and assume the supported route already meets the freshness rule. Epic 7 writers likewise assume an RTI exists.
- **Proposed resolution:**
  - In the Read features row, add: "Supported Get, Explain and Conversation-start read Project-owned persisted evidence with zero owner calls. Supported Refresh and the Reference Trust Index are open under DW-85."
  - In AD-14, add a bounded, named exception like the AD-19 DW-63 exception: "Until DW-85 closes, Folder revocation reaches supported context only when persisted evidence refreshes. This does not satisfy AD-20 freshness or NFR-1. Production Supported selection and external Chatbot adoption require DW-85 closure or an approved, time-bounded revocation-lag bound."
  - Repoint the Deferred RTI row's revisit condition to DW-85.
  - State whether \`asOf\` is the computation time (PRD) or the evidence cutoff.
- **Acceptance check:** The spine names DW-85 in the boundary row and in AD-14. Rows fr-16, fr-17 and fr-18 cannot record \`passed\` for supported reads while DW-85 is open. A revocation-then-context test on the supported route returns \`Denied\` or \`Unavailable\` within the AD-20 bound.

### F-2 — AD-34 does not make Projects responsible for accessibility of its own components, and drops timing and visible focus

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** AD-34 (l.418–422); capability map NFR-9 row (l.580).
- **Evidence:**
  - Spine l.420 binds "FR-14, FR-15, FR-22 through FR-24; NFR-9; SM-5" but not FR-25.
  - Spine l.422 names "Chatbot and operator presentation owners" and lists "non-color-only state". Map l.580 names "Chatbot and FrontComposer presentation owners".
  - Yet the confirmation and selection components are Projects-owned: AD-5 l.167 ("inside the Projects-owned component"), AD-13 l.217, and PRD A-6 prd.md:604 ("Projects-owned interactive component hosted via FrontComposer").
  - PRD prd.md:558, NFR-9: "visibly focused … do not rely on color or timing alone". The UX keeps "timing" (ux-design-specification.md:1027, l.1145); the spine drops it.
  - The spine fixes hard time limits: a 15-minute Confirmation Artifact and 5-minute Selection Evidence (AD-5, AD-13).
- **Failure scenario:**
  1. Story 7.16 ships the selection component. Projects treats accessibility as the Chatbot owner's job; the Chatbot owner treats the component as Projects'.
  2. A screen-reader user's pick lapses at 5 minutes with no warning or extension (WCAG 2.2.1 Timing Adjustable).
  3. The gap is found only in the full 8.8-P3 journey (epics.md:2772), after delivery.
- **Proposed resolution:**
  - AD-34: bind FR-25 and the confirmation flows of FR-4/7/8/11/23.
  - State: "Projects owns WCAG 2.2 AA conformance of its confirmation and selection components; Chatbot and FrontComposer own their host surfaces."
  - Add "visible focus" and "no reliance on timing alone: expiry is announced before it lapses and recoverable through \`RequestPreview\`/\`RenewPreview\` without losing entered context."
- **Acceptance check:** AD-34 names Projects as an owner, binds FR-25, and contains the timing and visible-focus clauses. The fr-25 and nfr-9 evidence rows cite AD-34.

### F-3 — FR-21 audit of the Setup-update credential kind is missing from AD-26

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** AD-26 Rule (l.335).
- **Evidence:**
  - PRD prd.md:491, FR-21: audit covers "Setup updates with the admitting credential kind (interactive or delegated)".
  - prd.md:116: a Service/Workflow Caller "may perform … FR-3 setup updates".
  - Spine l.335 enumerates what the audit includes. It never mentions Setup updates or credential kind; Setup update is task-only, not a "confirmed Project mutation".
  - Story 8.1 acceptance checks "Setup credential kind" (sprint-change-proposal-2026-11-24-readiness-blockers.md:231). So the story carries the rule but the spine does not.
- **Failure scenario:** The platform owns task and security-event records (AD-9) and builds the audit schema from AD-26. Delegated and interactive Setup updates become indistinguishable in audit, and the fr-21 row fails late or ships a hole in delegated-mutation accountability.
- **Proposed resolution:** Add to AD-26: "Setup updates with the admitting credential kind (interactive or delegated) taken from the AD-20 context."
- **Acceptance check:** AD-26 lists the clause. The 8.1 audit test asserts both credential kinds on persisted audit rows.

### F-4 — FR-3/FR-19 Setup-policy invariants have no carrying AD

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** Capability map FR-3/FR-19 row (l.566 → AD-15, AD-16, AD-18, conventions); AD-15 (l.233); AD-31 (l.371).
- **Evidence:**
  - PRD prd.md:151: "Setup filters Context References; it never introduces one".
  - prd.md:245: "Setup source policy may name only existing Context References … rejected as unsupported".
  - prd.md:248: a Setup change "never widens an already-admitted Conversation-start or context snapshot", and applies only at the next start or refresh.
  - prd.md:467, FR-19 lists the secret classes to reject (credential-shaped tokens, PEM key blocks, credentialed connection strings, secret-reference formats), plus unrestricted paths and foreign payloads.
  - None of the ADs that l.566 cites state these rules. AD-31 makes one validator authoritative only for metadata classification.
- **Failure scenario:**
  1. At the AD-17 writer cutover, the SDK aggregate re-implements Setup validation, while the retained v1 adapter (AD-24) keeps the old validator.
  2. Setup naming an unlinked File becomes an included context reference. That bypasses FR-9's manage, resource-read and Selection Evidence gates.
  3. Or one path accepts a PEM block that the other rejects.
- **Proposed resolution:** Add to AD-15 (or a convention row): "Setup source policy names only existing Context References and never introduces one. A Setup change yields a new \`projectVersion\` and never widens an admitted snapshot. One shared Setup validator (FR-19 secret classes, unrestricted paths, foreign payloads, control characters) runs before command submission on the v1 adapter, the SDK writer and proposal confirmation." Then fix the l.566 citation.
- **Acceptance check:** The rule text exists. An equivalence test proves the same rejection set on both writers. A test asserts that Setup naming a non-member reference is rejected.

### F-5 — NFR-10 additive-contract promise is bound only to events and specific v1 shapes

- **Severity:** Medium · **Status:** CONFIRMED (text); impact PLAUSIBLE
- **Spine anchor:** AD-16 (l.239); conventions Event evolution (l.441); AD-17 (l.262).
- **Evidence:**
  - PRD prd.md:559, NFR-10: "Contracts are additive and serialization-tolerant unless a breaking change is explicitly approved … Retirement requires a major version, migration notice, usage evidence, compatibility tests, and rollback evidence".
  - In the spine, additivity appears only for events (l.441, AD-22) and for retained v1 REST/client signatures (l.262). AD-16 says "compatibility metadata" but gives no evolution rule.
  - McpCli enrolls published Projects.Contracts 2.0.0 (l.43, l.355), and Chatbot consumes the Contracts.
- **Failure scenario:** An Epic 8 story (8.4/8.5 or 8.1) renames or retypes a published query DTO member. That passes spine review because only event V2 types are forbidden. McpCli strict discovery at \`6993abf\`, or Chatbot deserialization, then breaks with no major version or migration notice.
- **Proposed resolution:** Add to AD-16: "Published Projects.Contracts types, operation schemas and wire vocabulary evolve additively and serialization-tolerantly. Removing, renaming or retyping a published member, or changing its semantics, needs explicit approval and the NFR-10 retirement evidence, gated by package API baselines."
- **Acceptance check:** AD-16 carries the rule, and a public-API baseline check in Release CI fails on non-additive change.

### F-6 — FR-22 per-Project inspection scope and "no cross-Project enumeration" dropped

- **Severity:** Medium · **Status:** PLAUSIBLE
- **Spine anchor:** AD-14 Rule (l.225: "bounded list, detail, reference, audit, task … views"); AD-33 (l.405–412); map FR-22 row (l.573).
- **Evidence:**
  - PRD prd.md:504: "Inspection is per Project and per task. Reference and audit reads are scoped to one Project and paged under NFR-6; v1 offers no cross-Project audit or reference enumeration."
  - prd.md:71 calls FR-22 "per-Project and per-task interactive inspection".
  - The spine applies per-Project scoping only to the quarantine inventory (l.406).
  - epics.md:227 and l.1148 plan a "cross-project operational dashboard" with aggregated tiles, and the spine gives no rule separating that from forbidden enumeration.
- **Failure scenario:** Story 8.1 or 8.3 adds a Tenant-wide audit or reference query, for example for the dashboard or a McpCli audit read. It satisfies every AD yet breaches a PRD privacy and operator boundary.
- **Proposed resolution:** Add to AD-33 (or AD-14): "FR-22 reference and audit reads are scoped to one Project or one task and paged. No surface offers cross-Project audit or reference enumeration in v1. Tenant aggregates carry only counts and status codes."
- **Acceptance check:** The rule text exists. A negative test shows the audit and reference queries reject a missing or multi-Project scope.

### F-7 — Deferred McpCli expansion row conflicts with release-blocking FR-22/FR-24 CLI/MCP

- **Severity:** Low · **Status:** CONFIRMED (internal inconsistency)
- **Spine anchor:** Deferred row l.596; AD-21 l.303; AD-29 l.355.
- **Evidence:**
  - Spine l.596 defers "Expansion beyond the four enrolled McpCli reads", revisited when "UX/API artifacts" exist.
  - AD-21 l.303 requires export "through Web, CLI, and MCP". AD-29 l.355 puts "Audit, export, task control" outside the current inventory.
  - PRD prd.md:69: "Temporary disabling of a gated surface does not count as delivering that requirement". prd.md:502: FR-22 applies "across Web, CLI, and MCP".
  - Story 8.2 (epics.md:2263, l.2278) does plan CLI/MCP export.
- **Failure scenario:** An 8.11 reviewer reads l.596 as permission to release with Web-only FR-22 audit and task reads and Web-only FR-24 export.
- **Proposed resolution:** Reword l.596: "Naming, inventory and presentation of further McpCli operations. FR-22 audit and task reads and FR-24 export through CLI and MCP remain release obligations under AD-21 and AD-30."
- **Acceptance check:** The Deferred row no longer reads as deferring a PRD surface.

### F-8 — NFR-3 qualifiers and NFR-7 retry guidance dropped

- **Severity:** Low · **Status:** CONFIRMED
- **Spine anchor:** AD-28 (l.347); AD-19 (l.289).
- **Evidence:**
  - PRD prd.md:546: "99.9% monthly availability excluding planned maintenance. With required dependencies healthy, service RTO … 15 minutes".
  - Spine l.347 states 99.9% and RTO without either qualifier.
  - PRD prd.md:553: "Overload returns structured retry guidance". prd.md:182: "bounded retry-after guidance".
  - AD-19 l.289 gives only "Admission overload returns \`429\`".
- **Failure scenario:** A release-evidence owner can never pass the stricter unqualified target, or adapters return a bare 429 with no retry metadata.
- **Proposed resolution:** Restore both NFR-3 qualifiers in AD-28. In AD-19, require 429/503 to carry bounded retry-after guidance in \`recoveryActions\`.
- **Acceptance check:** The text matches the PRD, and the 8.9 back-pressure test asserts the retry guidance.

### F-9 — AD-27 audit archive is a new platform obligation with no decision record or gate

- **Severity:** Low · **Status:** CONFIRMED
- **Spine anchor:** AD-27 (l.341).
- **Evidence:**
  - Spine l.341: older records "move losslessly to a bounded, queryable platform archive".
  - PRD prd.md:551 says only "100,000 retained audit records per Project".
  - The memlog has no archive decision (its AD-27 entry, \`.memlog.md\`:60, omits it). No G-1..G-6 entry names the archive capability.
- **Failure scenario:** Story 8.1/8.9 assumes an archive that no owner has committed to, or omits it, and both readings are compliant.
- **Proposed resolution:** Record the decision in the memlog and either name the owner and gate (G-1 or G-5) or restate it as "query shape of 100,000 per Project; retention per AD-26; archive mechanism Deferred to platform design".
- **Acceptance check:** A memlog entry exists, and a gate or Deferred row names the owner.

### F-10 — Assumption fallbacks A-1, A-2, A-4 and A-6 are not carried

- **Severity:** Low · **Status:** CONFIRMED
- **Spine anchor:** Conventions Outcome measurement (l.450); G-2 (l.554); Deferred table (l.587–600).
- **Evidence:**
  - PRD prd.md:599, A-1: without the Chatbot feed, "SM-7/SM-8 report 'not measurable' and cannot be represented as passing".
  - prd.md:600, A-2: Projects pre-checks and fails closed on any same-name Folder.
  - prd.md:602, A-4: bulk binding only under FR-8/FR-22.
  - prd.md:604, A-6: Folder targets stay confirmation-required if Folders cannot answer the pick-time check.
  - A spine grep finds A-3, A-5 and A-7 only.
- **Failure scenario:** Release evidence records SM-7/SM-8 as passing on fixture parity alone while the Chatbot companion feed is absent. Or a bulk legacy repair is added outside FR-8.
- **Proposed resolution:** Add one line each, in conventions or Deferred, with the PRD revisit condition.
- **Acceptance check:** A grep for A-1, A-2, A-4 and A-6 hits the spine.

### F-11 — Addendum §2 freshness normalization (E-20) is not carried

- **Severity:** Low · **Status:** CONFIRMED (spine); casing impact PLAUSIBLE
- **Spine anchor:** AD-32 (l.377); conventions Task and response vocabulary (l.439).
- **Evidence:**
  - addendum.md:67 maps producer states onto "lower-case codes" (\`trusted\`/\`fresh\` → \`current\`, \`mixedGeneration\` → \`stale\`, \`forbidden\`/\`redacted\`/\`unknown\` → \`unavailable\`) and says normalization "must not erase the producer fields".
  - Contracts implements it (\`EvidenceFreshnessStateCode.cs\`: lower-case machine codes plus PascalCase labels), but only the legacy UI, CLI and MCP adapters use it.
  - Spine l.377 writes \`Current|Stale|Rebuilding|Unavailable\` with no wire-versus-label distinction.
- **Failure scenario:** The SDK query DTOs and McpCli JSON emit PascalCase while the legacy adapters emit lower-case. Consumers in a cross-surface parity test (addendum §7.2) disagree.
- **Proposed resolution:** Add a conventions row: "Freshness machine codes are lower-case, labels PascalCase. Producer states normalize via the Contracts \`EvidenceFreshnessStateCode\` per addendum §2, preserving producer fields."
- **Acceptance check:** The row exists, and the parity test compares codes across Web, CLI and MCP.

### F-12 — Frontmatter \`sources\` omit cited and requirement-shaping proposals

- **Severity:** Low · **Status:** CONFIRMED
- **Spine anchor:** frontmatter l.17–37; l.52; l.264.
- **Evidence:**
  - l.264 cites \`sprint-change-proposal-2026-11-22.md\` ("v4 decision"). l.52 cites \`implementation-readiness-report-2026-11-24-rerun.md\`. Neither is in \`sources\`.
  - \`sprint-change-proposal-2026-11-21.md\` (native membership, machine refusal, claim race) and \`sprint-change-proposal-2026-11-22-schedule-story-8-1.md\` are also absent.
  - The missing 11-22 source is how the DW-85 change (F-1) went unreconciled.
- **Failure scenario:** The next reconciliation pass checks only the listed sources and misses scope changes again.
- **Proposed resolution:** Add the four paths to \`sources\`.
- **Acceptance check:** Every planning artifact linked from the spine body appears in \`sources\`.

### F-13 — Resolution candidate eligibility for Archived Projects not carried

- **Severity:** Low · **Status:** CONFIRMED
- **Spine anchor:** AD-14 advisory paragraph (l.227); AD-3 (l.132).
- **Evidence:**
  - PRD prd.md:258, FR-4: "Archived Projects are excluded from Project Resolution unless explicitly requested".
  - prd.md:375, FR-12: "Only Active, read-model-confirmed Projects are considered by default".
  - AD-3 excludes only pre-activation Projects from candidacy. AD-14 is silent on Archived.
- **Failure scenario:** Advisory resolution in the SDK read model or McpCli \`trace-*\` returns Archived Projects as candidates. That inflates \`MultipleCandidates\` and SM-7 denominators.
- **Proposed resolution:** Add to AD-14: "Candidates are Active, read-model-confirmed Projects by default. Archived Projects are included only on explicit request."
- **Acceptance check:** The text exists, and the advisory test includes an Archived negative case.

## Checked and held up

- Frontmatter binds FR-1..FR-25 and NFR-1..NFR-11, and the capability map (l.564–585) cites at least one AD for every FR and NFR.
- Lifecycle is exactly \`Active\`/\`Archived\` (AD-3, l.438), matching PRD §2.1 (prd.md:34).
- Task statuses, terminal immutability, cancellation cut-off and \`NeedsAttention\` (AD-4) match prd.md:154 and §5 (prd.md:193).
- The Recovery Action Code list at AD-32 l.381 equals prd.md:145, and the Complete/Partial/Unavailable rules equal prd.md:188–191.
- Retention matches NFR-8: Confirmation 15 min, Selection Evidence 5 min, idempotency 30 days or result lifetime, audit at least 365 days or the event-history obligation (AD-5, AD-13, AD-26).
- AD-21 export bounds (1 MiB, 500 references, 100 audit rows, two leases, no cursor or retention) match FR-24 and addendum §3.
- AD-27 numbers match NFR-5/6/7. AD-28 RPO 0, 5-minute task recovery and readiness 200→503→200 match NFR-3/4 and SCP 11-20 finding 6.
- The AD-33 role matrix matches PRD §2.1 and §3.2: Tenant Operator restores only to the prior Folder; Administrators have no additive link or initial Folder; reconciliation is Administrator-only; export is separately authorized.
- McpCli refusal of confirmation, selection and UI-only actions (AD-29, AD-33 l.416) matches prd.md:13, l.39 and l.94.
- AD-10's prior-membership receipt and FR-7 move rules match prd.md:146 and l.296–297. AD-13's dual reader-set binding matches FR-8 prd.md:311.
- AD-31 matches addendum §4.1. The U+2028/U+2029 convention (l.443) matches FR-19 prd.md:468 and addendum §7.3.
- The AD-19 DW-63 404 exception does not contradict the PRD: transport mapping is delegated (prd.md:175) and \`Denied\` nondisclosure is preserved.
- AD-14 l.227 faithfully carries the 10-21 advisory exception (prd.md:195–201).
- AD-17 l.262 carries the 11-16 cutover decisions: one startup selection, default \`Unavailable\`, no per-request fallback (addendum.md:134).
- The 11-24 proposals change only sequencing, and the boundary rows l.51–52 reflect them. Neither alters an FR or NFR.
- The outcome-measurement convention (l.450) matches PRD §8, including "insufficient volume" and SM-7/SM-8 ≥ 90% and SM-C4 ≤ 5%.
- Deferred cross-region DR (l.598) and multi-Folder or multi-Project membership (l.600) match PRD §2.4 and §2.1 revisit conditions.
