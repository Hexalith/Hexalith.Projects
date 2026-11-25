# Reviewer Gate — Decision Fidelity and Regression (Finalize after Update, 2026-11-25)

**Verdict: PASS WITH FINDINGS.** All 64 decision-log entries are placed once in a reasonable spot. Nothing leaked, and no rejected alternative crept in. Six medium findings remain: two contradictions inside the spine that the update introduced, two decided clauses the update dropped, and two pointer or rule regressions.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 0 |
| Medium | 6 |
| Low | 8 |

**Inputs**

- SPINE: `/home/administrator/projects/hexalith/projects/_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` (SHA-256 `865f8eed3622dfcbb67393f917ae4d79778db3a17a3a990fdd84dee4a9df0fb3`, 600 lines, 95,244 bytes, verified)
- MEMLOG: `/home/administrator/projects/hexalith/projects/_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/.memlog.md`, lines 110–173
- PREV: `/tmp/claude-1000/-home-administrator-projects-hexalith-projects/ff52b716-5b73-435a-a827-b58173df2c32/scratchpad/ARCHITECTURE-SPINE.01b89a2f.md`
- Method: I read every entry from line 110 to 173 against the spine, compared PREV with SPINE word by word, opened every pointer target, grepped for leaked or superseded wording, and checked the code facts the entries cite in `src/`.

---

## Medium

### F-1 — AD-31 still accepts the legacy `projectId` throughout v1, which contradicts AD-18's post-cutover `400`

- **Severity:** Medium · **Anchor:** AD-31, SPINE:366, against AD-18, SPINE:278 · **CONFIRMED**
- **Evidence:**
  - SPINE:366 says: "Only the historical unversioned request (`name`, optional `projectId`, `description`, `setupMetadata`) is a v1 compatibility shape, accepted and readable throughout v1".
  - SPINE:278 says: "From the cutover, a supplied `projectId` or `X-Hexalith-Task-Id` on v1 create returns `400` naming the field".
  - MEMLOG:156 (H6) adopts both the post-cutover `400` and the corrected field list. The correction put `projectId` into a sentence that still says "accepted … throughout v1".
- **Failure scenario:** At the cutover, the owner of the AD-24 adapter reads AD-31 as a compatibility promise and keeps accepting a caller-supplied `projectId`. The caller then selects the aggregate identity, which is exactly what AD-18 says it prevents. Story 7.1-P1 fixtures and AD-24 before/after fixtures pin opposite behavior.
- **Proposed fix (AD-31, last sentence):** "Only the historical unversioned request (`name`, optional `projectId`, `description`, `setupMetadata`) is a v1 compatibility shape, readable throughout v1 and accepted under AD-18 (a supplied `projectId` returns `400` from the writer cutover); retirement requires a major version, migration notice, usage evidence, compatibility tests and rollback evidence."

### F-2 — AD-33 says only FR-8 releases quarantine, but M8 adopted Story 7.15 repair as a second exit

- **Severity:** Medium · **Anchor:** AD-33, SPINE:407, against AD-35, SPINE:427 · **CONFIRMED**
- **Evidence:**
  - SPINE:407 says: "Only the FR-8 binding workflow takes a Project out of quarantine."
  - SPINE:427 says: "each Project on a conflicting key is quarantined as `Unavailable` with no context (AD-3) until Story 7.15 repair or the FR-8 Administrator binding workflow (Story 7.6) leaves at most one holder; the key then reopens with that holder’s claim reconstructed from its history".
  - MEMLOG:158 (M8) says the Project is quarantined "reusing the AD-3 folderless-legacy quarantine, and is released only through the FR-8 Administrator binding workflow (Story 7.6) or Story 7.15 repair". MEMLOG:173 confirms the 7.15 exit.
- **Failure scenario:** The Story 7.15 implementer, or the reviewer of the FR-22 inventory, applies AD-33. The surviving holder then stays `Unavailable` after a successful repair, or the 7.15 repair is rejected as a quarantine bypass.
- **Proposed fix (AD-33, SPINE:407):** "Only the FR-8 binding workflow, or Story 7.15 repair of a conflicting AD-35 key, takes a Project out of quarantine."

### F-3 — H2 put the ChatBot authority commands into the AD-33 matrix, but the spine has no matrix row for them

- **Severity:** Medium · **Anchor:** AD-33, SPINE:382 and the matrix at SPINE:384–403. AD-3 (SPINE:136) and the classification table (SPINE:502) do carry H2. · **CONFIRMED** (the row is absent)
- **Evidence:**
  - MEMLOG:160 (H2) says: "Both commands and their events join the AD-17 replay and equivalence set, the AD-33 matrix and the action classification table."
  - AD-17 (SPINE:249) and the classification table (SPINE:502) carry it.
  - No matrix row covers `GrantProjectChatBotAuthority` or `RevokeProjectChatBotAuthority`. AD-33 only gained the sentence "A ChatBot authority grant never substitutes for a matrix permission." (SPINE:382).
- **Failure scenario:** AD-33 says "Every adapter enforces the same action matrix". With no row for issuance, the Web, CLI and MCP adapters have no cell to enforce, and neither does the 8.8-P1 parity evidence. One adapter lets a Tenant Operator grant while another allows only Folder managers. The damage is bounded because a grant is never sufficient on its own (AD-3), but who may issue a grant is unspecified.
- **Proposed fix:** Add the row "Grant or revoke ChatBot authority (delegated-operation record, AD-3 exception)". Take its cells from the approved H2 requirement (the A-3 PRD requirement offered under H2). If H2 fixed no cell values, record the missing values as an open item owned by the architect instead of inventing them.

### F-4 — AD-24 drops H5's list of routes that return 409, so the classification table invites misreading the v1 link routes

- **Severity:** Medium · **Anchor:** AD-24, SPINE:316; classification table, SPINE:498 · **PLAUSIBLE**
- **Evidence:**
  - MEMLOG:155 (H5) says: "Routes whose operation needs a Preview and confirmation under AD-5 and AD-13 (proposal confirm, resolution confirm, Conversation link/move/unlink, PUT folder including its replacementConfirmed flag, File and Memory link/unlink, archive and restore) return 409 with RequestPreview…"
  - SPINE:316 keeps only "a route whose operation needs AD-5 Preview and confirmation (including `PUT …/folder` with `replacementConfirmed`) returns `409 RequestPreview`".
  - SPINE:498 files `conversation.link`, `file-reference.link` and `memory.link` under "Association admission … Valid Selection Evidence admits actor-selected work as a Durable Task", not under "Confirmation plus Durable Task".
- **Failure scenario:** The adapter implementer maps `POST …/conversations/{id}/link`, `…/files/{id}/link` and `…/memories/{id}/link` as association admission and returns `202` with a task. The 25-pair before/after table then disagrees with H5, and an inferred binding is admitted without a Preview. Reaching H5's answer takes a three-step chain: AD-5, then the A-6 fallback, then "Inferred Association → RequestPreview". The decision itself stated the answer directly.
- **Proposed fix (AD-24):** "a route whose operation needs AD-5 Preview and confirmation (proposal and resolution confirm; Conversation link, move and unlink; `PUT …/folder` including `replacementConfirmed`; File and Memory link and unlink; archive and restore) returns `409 RequestPreview` in the v1 problem shape, …"

### F-5 — Two sentences were replaced by pointers to AD-25, but AD-25 does not carry the startup, teardown and identity proof they required

- **Severity:** Medium · **Anchor:** AD-25, SPINE:322; G-4, SPINE:551; runner contract, SPINE:512 · **CONFIRMED**
- **Evidence:**
  - PREV:556 (G-4) said: "Require clean Debug and CI package-mode startup, teardown, identity, persisted recovery and authenticated Web/CLI/MCP proof before retiring the Projects-owned runtime."
  - PREV:517 said: "AD-25 requires authenticated and persisted parity, recovery, teardown and package-mode evidence before retiring the existing AppHost."
  - Both became pointers: SPINE:551 now reads "AD-25 governs the remaining retirement proof", and the sentence at PREV:517 is gone. The AD-25 rule at SPINE:322 lists "Pure-domain, host-contract, persisted-boundary, restart, two-instance and authenticated browser/CLI/MCP lanes plus clean-checkout and CI package-mode parity". It has no startup, teardown or identity proof.
  - No memlog entry authorizes this. M16 covers delivery state only.
- **Failure scenario:** The Projects-owned AppHost and runtime projects are retired after the listed lanes pass, with no proof that the runner tears down or injects identity. Local operability regresses: sidecars are orphaned or credentials are missing.
- **Proposed fix (AD-25, last sentence):** "Pure-domain, host-contract, persisted-boundary, restart, two-instance and authenticated browser/CLI/MCP lanes, startup, teardown and identity proof, plus clean-checkout and CI package-mode parity, must pass before old runtime projects are removed; Debug-only source references are insufficient."

### F-6 — AD-27 still names Redis, but M18 defers the provider class and requires AD-27 evidence on the selected class

- **Severity:** Medium · **Anchor:** AD-27, SPINE:336, against the Deferred table, SPINE:595 · **CONFIRMED**
- **Evidence:**
  - SPINE:336 says: "Measure authenticated supported and rollback reads under concurrent appends on persistent Dapr/Redis at median and maximum Tenant sizes".
  - SPINE:595 says: "G-5 decision before Story 8.9 and 8.10 evidence; AD-27, AD-28 and AD-30 evidence runs on the selected class".
  - MEMLOG:149 (M18) says: "the production state-store and pub/sub provider class is undecided while evidence lanes split between Redis and PostgreSQL. AD-27, AD-28 and AD-30 evidence must run on the selected class".
  - The G-6 qualifier already runs PostgreSQL (SPINE:465).
- **Failure scenario:** Story 8.9 performance evidence is captured on Redis because AD-27 says so, then rejected when G-5 selects PostgreSQL. Alternatively, evidence captured on PostgreSQL is challenged as not conforming to AD-27.
- **Proposed fix (AD-27):** "…under concurrent appends on the persistent Dapr state store of the selected production provider class (Deferred; G-5) at median and maximum Tenant sizes, …"

---

## Low

### F-7 — The audit-archive Deferred row lost the binding qualifiers of the removed archive sentence

- **Severity:** Low · **Anchor:** AD-27, SPINE:336; Deferred, SPINE:594 · **PLAUSIBLE**
- **Evidence:**
  - PREV:341 said: "older records within the full AD-26 retention period move losslessly to a bounded, queryable platform archive."
  - SPINE:594 now says: "Audit archive beyond the AD-27 online shape | Platform design before Story 8.1 and 8.9 entry".
  - MEMLOG:170 (M20) defers only "The archive mechanism for older records".
  - Other Deferred rows (SPINE:586, 587, 589) say what "remain[s] binding".
- **Failure scenario:** Platform design treats records beyond the online shape as retained but not readable, for example cold storage with no FR-22 read path. Records stay lossless through AD-26 retention and AD-27 "audit pressure never discards required records", but the spine no longer says anywhere that they stay queryable.
- **Proposed fix (revisit cell):** "Platform design before Story 8.1 and 8.9 entry; AD-26 retention and AD-27 no-discard remain binding". Ask the owner whether queryability is part of the deferred mechanism; do not add it unilaterally.

### F-8 — "selection and rollback replace both" survives next to H4's "writer rollback is a mutation freeze plus roll-forward"

- **Severity:** Low · **Anchor:** AD-17, SPINE:251 · **PLAUSIBLE**
- **Evidence:**
  - SPINE:251 says: "selection and rollback replace both and invalidate or re-key aggregate snapshots … After the cutover, writer rollback is a mutation freeze plus roll-forward."
  - MEMLOG:112 (H13) says "writer selection and rollback also invalidate or re-key aggregate snapshots".
  - MEMLOG:151 (H4) says "writer rollback means freezing mutation and rolling forward … because the current processor cannot replay or emit spine-compliant Folder-first behavior".
- **Failure scenario:** The author of the cutover runbook reads "rollback replace both" as permission to swap back to the legacy processor after the SDK writer has written. H4 forbids that. Alternatively, the author skips snapshot invalidation when a roll-forward writer replaces the processor, because that replacement is not called "rollback".
- **Proposed fix:** "every processor selection or replacement (including an abort of the cutover deploy before mutation reopens, or a roll-forward writer) replaces both and invalidates or re-keys aggregate snapshots".

### F-9 — The scope of H9's fold-equivalence rule narrowed to the read models of the listed folds

- **Severity:** Low · **Anchor:** AD-17, SPINE:251 · **PLAUSIBLE**
- **Evidence:**
  - MEMLOG:111 (H9) says: "(the legacy processor's aggregate fold, ProjectProjectionHandler, ProjectDetailProjection, ProjectListProjection and every read model behind LegacyRollback or the AD-24 adapter)".
  - SPINE:251 says: "(legacy aggregate fold, `ProjectProjectionHandler`, `ProjectDetailProjection`, `ProjectListProjection` and their read models)".
- **Failure scenario:** A retained read model fed by some other handler (for example a tenant-access, reverse-membership or audit-index view that serves the adapter) is left out of the state-equivalence evidence. It then shows Folder-first Projects as folderless, which is the exact failure H9 exists to prevent.
- **Proposed fix:** "… `ProjectListProjection` and every read model behind `LegacyRollback` or the AD-24 adapter) …".

### F-10 — Gate and scope clauses from three entries were dropped

- **Severity:** Low · **Anchor:** Writers row, SPINE:52; AD-18, SPINE:278; AD-31, SPINE:366 · **CONFIRMED** (absent from the spine)
- **Evidence:**
  - MEMLOG:136 (H8) says "route to the Story 7.1 owner; gates the first writer". The Writers row's list of first-writer gates (SPINE:52) omits the AD-18 single-Tenant binding.
  - MEMLOG:138 (M6) says "gates 7.1-P1 acceptance". This is not in the spine.
  - MEMLOG:154 (C1) says "DW-63 stays behavior-preserving as frozen". The Writers row says nothing about it.
- **Failure scenario:** The first-writer entry check does not list Tenant binding. DW-63 grows to absorb the validator move that C1 routed to a separate deferred-work item.
- **Proposed fix (Writers row):** "Accepted 7.1-P1/P2 packages, single-Tenant request binding (AD-18), one authorized Folder before activation, the AD-35 claim, durable owner receipts and single-writer fencing gate the first writer; DW-63 stays behavior-preserving." Under M16, the M6 acceptance gate can stay in the tracker; confirm that with the owner.

### F-11 — The removal of the measured P1R paragraph (authorized by M16) took one durable rule with it

- **Severity:** Low · **Anchor:** AD-17 (the removed paragraph, PREV:268–277); AD-6, SPINE:177; AD-28, SPINE:342 · **PLAUSIBLE**
- **Evidence:**
  - PREV:273–274 said: "Restoring a pre-upgrade backup does not authorize loss of later committed events."
  - MEMLOG:171 (M16) authorizes removing "the measured P1R paragraph", and MEMLOG:172 records it as kept in `sprint-change-proposal-2026-10-06.md`.
  - No AD now states this rule. AD-6 ("Existing committed events remain readable and unchanged") and the AD-28 RPO 0 commitment only cover adjacent ground. The M19 Deferred row (SPINE:596) covers restore consistency across stores, not using a backup as a rollback.
- **Failure scenario:** During an incident after the cutover, someone restores a pre-cutover backup as a "rollback" and discards events the SDK writer committed, against H4's roll-forward rule.
- **Proposed fix:** No change is required, because the removal was authorized. If the owner wants the rule kept, add one clause to AD-17's writer-cutover paragraph: "restoring a pre-cutover backup never discards later committed events."

### F-12 — Bookkeeping drift: an unlogged Binds widening, two restatements and a stale map row

- **Severity:** Low · **Anchor:** AD-14 Binds, SPINE:227; AD-18, SPINE:278; classification table, SPINE:502; capability map NFR-9, SPINE:575 · **CONFIRMED**
- **Evidence:**
  - AD-14 Binds changed from "FR-16 through FR-20, FR-22" (PREV:223) to "FR-16 through FR-22", which adds FR-21. The additions of FR-4 (L18) and NFR-1 (H17) are logged; FR-21 is not.
  - AD-18 restates AD-35's claim-stream identity ("and the AD-35 claim streams by `(TenantId, FolderId)`").
  - The classification row restates AD-3 ("never sufficient without current Folder authority").
  - The NFR-9 map row, under "Lives in", still says "Chatbot and FrontComposer presentation owners", although M21 (MEMLOG:119) made the confirmation and selection components Projects-owned.
- **Failure scenario:** Minor. Traceability tools attribute FR-21 to AD-14 without a decision behind it, and the NFR-9 owner lookup misses Projects.
- **Proposed fix:** Return AD-14 Binds to "FR-16 through FR-20, FR-22", or log FR-21 if it is intended. Set NFR-9 "Lives in" to "Projects confirmation and selection components; Chatbot and FrontComposer host surfaces". The two restatements are optional to trim.

### F-13 — The ordering between a cutover before Epic 8 and the 8.8-P1 validator is not stated (a decision-level question)

- **Severity:** Low · **Anchor:** AD-17, SPINE:251; AD-20, SPINE:292 · **PLAUSIBLE**
- **Evidence:**
  - SPINE:251 places the cutover "after Stories 7.1–7.15 … and before the Epic 8 release".
  - SPINE:292 says "a production-capable host refuses to start with one [allow-all] … before the writer cutover. From 8.8-P1 the real validator runs on G-5 evidence".
  - `_bmad-output/planning-artifacts/epics.md:2770` lists 8.8-P1 as "blocked by Stories 8.3–8.5, G-4, G-5".
  - H7's token forwarding also ends "at the latest by 8.8-P1".
- **Failure scenario:** Between a cutover before Epic 8 and 8.8-P1, a Release-built SDK host can use neither allow-all (it refuses to start) nor the validator backed by G-5. The spine does not say which validator the post-cutover host runs. Either the cutover silently slips to 8.8-P1, or someone brings allow-all back under a "local-development" composition.
- **Proposed fix:** This is not a transcription error. Ask the AD-20 owner which admission validator runs after the cutover and before 8.8-P1 (for example, fail-closed), and record the answer in the memlog. Change the spine wording only after that.

### F-14 — Three wording slips that shift meaning

- **Severity:** Low · **Anchor:** AD-14, SPINE:229; AD-19, SPINE:284; Conventions, SPINE:436 · **CONFIRMED**
- **Evidence and fixes:**
  - **AD-14, SPINE:229.** The spine says "`asOf` is the authorization and evidence computation time." H17 (MEMLOG:113) keeps the PRD meaning, and `prd.md:178` defines `asOf` as "the server timestamp of the authorization and evidence computation". Fix: "`asOf` is the server time of the authorization and evidence computation."
  - **AD-19, SPINE:284.** The spine says "…no protected detail, and one safe Gateway `404`". M10 (MEMLOG:115) says "the Gateway returns the same safe 404 for all of them". Fix: "…no protected detail; the Gateway returns the same safe `404` for all three."
  - **Conventions, SPINE:436.** The spine says "a payload version or upcaster first retires or upgrades every retained fold". That makes the payload version the thing that retires folds. M12 (MEMLOG:116) says it "first requires retiring or upgrading every retained fold". Fix: "introducing a payload version or upcaster first requires retiring or upgrading every retained fold."

---

## What held up

- **Coverage.** Every entry from MEMLOG:110 to 173 is traced. Each decided rule appears once, in a sensible home. Examples:
  - H4 is in the AD-17 writer-cutover paragraph, with pointers from the Writers row and the diagram.
  - C2 and M1 are in AD-35, with consequences in AD-8, AD-15 and AD-17, the Seed and the diagram.
  - H12 is in AD-19.
  - H5 is in AD-24 (apart from F-4), H6 in AD-18, H3 in AD-24 and the Seed, and H10/M2/M3 in AD-12 and AD-15.
  - C1 and M5 are in AD-20 and G-5, and H7 and H14 in AD-20 and AD-28 plus a Deferred row.
  - H15, M15 and the McpCli rule moved by M16 are in AD-29. H16 is in AD-14, AD-26 and Story 8.10, and M20 is in AD-27 plus a Deferred row.
  - The fallbacks from M9, M12, M13, M21–M24, L12–L18 and A-6 are placed.
- **Routed and deferred entries appear only as the entries say.**
  - H1 is in the Stack EventStore row, and the Writers row points to it. "source mode only" is gone.
  - H11 is in the Stack Dapr row, M25 in the runner contract, L5 in the Stack, and L10 in AD-28 and G-5.
  - M18, M19, M20, H14 and L1 have Deferred rows with the stated revisit conditions.
  - M17 correctly has no Deferred row, because it is an editorial deferral of spine format.
  - L3, Q9 and line 110 correctly make no spine change. L7, L8 and L9 are routed to code only, and the spine adds nothing beyond its existing conventions.
- **No leakage.**
  - Grepping for `\b[HMLC][0-9]{1,2}\b` matches only SM-7, SM-8 and SM-C4.
  - There is no "Rejected:" text. "Rejected" appears only as a task status.
  - There is no reviewer or user provenance, no "option A" or "recommended", and no personal names: "Jerome and John" became "The Release Owners", and the component owner is "architect".
- **No rejected alternatives.**
  - Absent: frozen-slice early cutover, platform reservation primitive, Dapr first-write key, v5 requalification, projectId alias table, automatic retry of task-only work, keeping the earliest binding active, a Projects-issued component credential, a YAML generator, a Projects audit event stream, a literal retention cap, and an API-baseline mechanism.
  - The spine says "no … frozen route" and "no automatic retry" explicitly.
- **Superseded wording is gone.**
  - AD-19's "at that cutover, one registered query-response policy…".
  - AD-17's "Before switching an affected writer slice…" and "Command rollback is permitted only while…".
  - "source mode only", AD-31's "name-only" and "assigned by authenticated integration policy", "AD-33 inspection audit", the platform-archive sentence, and the old "abandoned reservations … authoritative evidence proves" clause.
  - The removals M16 authorized are recorded in MEMLOG:172: boundary statuses, CI run IDs, the Stack revision anchor, the McpCli note, and the P1R and historical-qualification paragraphs.
- **These pointer targets do carry the removed rules:**
  - Conventions **Identifiers → AD-18**: ULIDs, server-derived Tenant, actor and aggregate identity, opaque foreign IDs, and readable legacy IDs.
  - **Project lifecycle → AD-3**: pre-activation progress exists only as Durable Task state.
  - **Query ordering → AD-14** for cursor codec, authenticated scope and tamper evidence, and **AD-27** for page size 50 and maximum 200.
  - **Authentication → AD-20**: both credentials validated, each owner reauthorizes, queries filter again.
  - AD-12 `task.reconcile` → AD-4.
  - AD-13 → AD-19 outcomes, and Selection Evidence's five-minute lifetime and bindings → AD-5.
  - AD-30's McpCli bar and the tail of AD-33 → AD-29.
  - G-3 → AD-29, and G-6 → AD-30 (exact-SHA qualifier, blocked on missing or failed runs).
  - The Seed's `Client.Generation`/`.Shared` note → AD-24.
  - The one exception is F-5.
- **Code facts the entries cite are confirmed:**
  - `src/Hexalith.Projects.Server/Queries/ProjectsQueryResponsePolicy.cs:21-23` holds today's six-type deny-list (M11).
  - `GetProjectAuditQuery` and `GetProjectTaskStatusQuery` are still decorated, so the H15 removal is pending as routed.
  - `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` exists (M13).
  - ASPIRE012 is in `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj:8`, and CS0618 is in `Directory.Build.props:65` (L4).
  - The v1 create route reads `X-Hexalith-Task-Id` and `X-Correlation-Id` as fallbacks and accepts `projectMetadata.metadataClass` (`ProjectsDomainServiceEndpoints.cs:553-560, 591-625`). This matches AD-18's exception before the cutover and AD-31's pass-through.
