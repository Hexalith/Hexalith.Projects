# Reviewer Gate — Rubric Walk of the 2026-11-25 Spine Update

**Target:** `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` (SHA-256 `865f8eed3622dfcbb67393f917ae4d79778db3a17a3a990fdd84dee4a9df0fb3`, 95,244 bytes, 600 lines, AD-1..AD-35). Compared with the prior revision `01b89a2f` (89,566 bytes) and memlog entries 110–173. Brownfield spot checks ran against `src/` and the root-declared submodule checkouts under `references/` (read-only).

**Verdict: PASS WITH FINDINGS.** The update closes the C1/C2 critical paths in principle and keeps AD IDs, FR/NFR coverage and the 25-route adapter intact. Two High findings remain. First, the writer cutover has an unstated dependency on G-5 deliverables. Second, the AD-35 lost-reply fence contradicts the AD-3/AD-8 rule that pre-activation state lives only in the task. Several of the rules changed today also disagree with one another at the sentence level.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 2 |
| Medium | 12 |
| Low | 6 |
| **Total** | **20** |

---

## High

### R-H1 — The writer cutover silently depends on 8.8-P1/G-5 deliverables that AD-20 schedules later

- **Severity:** High · **Status:** CONFIRMED
- **Spine anchor:** AD-20 line 292; AD-12 line 215; AD-17 line 251 (Writer cutover); G-5 line 552
- **Evidence:**
  - AD-20 (292): "the Projects Server owner moves the allowing EventStore-authorization validator and Dapr policy evidence provider out of `AddProjectsServerRuntimeInfrastructure` before the writer cutover. From 8.8-P1 the real validator runs on G-5 evidence".
  - `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:169-170`: the only non-test default is `TryAddSingleton<IProjectEventStoreAuthorizationValidator, DenyAllProjectEventStoreAuthorizationValidator>` (and DenyAll Dapr evidence). Lines 240-243 replace them with the Allowing types.
  - The validator sits on both paths. `ProjectsDomainProcessor.cs:197-216` rejects every non-ChatBot command unless the validator returns `Allowed`. `ProjectAuthorizationGate.cs:508` runs it in the ordered chain that every v1 and supported read and mutation uses.
  - AD-12 (215): "Later owner steps use task-bounded G-5 delegated credentials, never the interactive token". AD-20's forwarding exception covers only "the retained adapter".
  - `epics.md:1610-1619`: the Epic 7 entry gate requires G-1 and G-2 only. `epics.md:2770`: 8.8-P1 is "blocked by Stories 8.3–8.5, G-4, G-5", which places it late in Epic 8, after the cutover that AD-17 puts "after Stories 7.1–7.15".
- **Failure scenario:** The Server owner applies AD-20(a) before the cutover. Every production-capable host then falls back to DenyAll, so supported reads and the current writer deny everything until 8.8-P1. To keep deployed environments working, the cutover owner runs them in "explicit test or local-development composition". That puts the SDK writer on allow-all, the outcome C1 rejected. After the cutover, SDK-writer saga owner steps have no permitted credential: the interactive token is forbidden and G-5 credentials do not exist yet. Tasks stall in `WaitingForDependency`, or a team forwards the token anyway. Three owners make three different choices.
- **Proposed fix (AD-17, Writer cutover paragraph, after "before the Epic 8 release"):** "The cutover deploy also requires the AD-20 real EventStore-authorization validator and Dapr policy evidence on G-5, and G-5 task-bounded delegated credentials for saga owner steps. These are the 8.8-P1 capabilities, not its evidence package. Until both exist, the cutover does not happen." In AD-20, replace "From 8.8-P1 the real validator runs" with "From the writer cutover, and no later than 8.8-P1, the real validator runs". Add "the Epic 7 writer cutover" to the G-5 entry condition. Alternative if G-5 cannot precede the cutover: name an interim non-allowing validator, specifying its inputs and owner, that the production-capable host registers once the Allowing types leave.

### R-H2 — The AD-35 creation-abandoned tombstone contradicts AD-3/AD-8 and is missing from the AD-17/AD-22 event set

- **Severity:** High · **Status:** CONFIRMED (textual)
- **Spine anchor:** AD-35 line 425; AD-3 line 136; AD-8 lines 188-189; AD-17 line 249 (approved deltas); AD-22 line 304
- **Evidence:**
  - AD-35 (425): "before releasing an abandoned reservation, the task commits a terminal creation-abandoned event to that Project stream, so a late create is rejected".
  - AD-3 (136): "Pre-activation progress exists only as Durable Task state."
  - AD-8 Prevents (188): "an incomplete Project stream"; Rule (189): "submits one Project creation commit containing the Folder binding and the claim receipt".
  - AD-17 (249): "Approved deltas: Folder-first create without Pending, the combined AD-15 cap, artifact-based replacement, restore Folder validity, claim acquire and release". No new Project-stream event is listed. AD-22 lists the creation-event evolution without it.
  - Code that assumes every Project stream begins with `ProjectCreated`: `ProjectChatBotAuthorityQueryHandler.cs:91` (`state.IsCreated`), plus the list and detail projections.
- **Failure scenario:** The Story 7.1 author follows AD-3/AD-8 and writes nothing to the Project stream before activation. With no tombstone, a delayed create commit lands after the claim is released and activates a second Project on a Folder that another task now holds, which is the C2 failure. A Story 7.1 author who follows AD-35 instead creates streams that the retained folds and read models (AD-17 line 251) were never asked to handle. A rebuild or `LegacyRollback` then renders or rejects them unpredictably.
- **Proposed fix:**
  - AD-3: "Pre-activation progress exists only as Durable Task state, except the AD-35 creation-abandoned tombstone, which carries no Project data and is excluded from every read, candidate set and count."
  - AD-8 Prevents: "an observable Project stream before activation…".
  - AD-17 approved deltas: append "the AD-35 creation-abandoned tombstone (replayed by every retained fold as a no-op)".
  - AD-22: "a creation-abandoned event terminates a reserved, never-activated ProjectId".

---

## Medium

### R-M1 — AD-18 rejects `projectId` from the cutover; AD-31 says the shape carrying it is accepted throughout v1

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** AD-18 line 278; AD-31 line 366
- **Evidence:**
  - AD-18: "From the cutover, a supplied `projectId` or `X-Hexalith-Task-Id` on v1 create returns `400` naming the field".
  - AD-31: "Only the historical unversioned request (`name`, optional `projectId`, `description`, `setupMetadata`) is a v1 compatibility shape, accepted and readable throughout v1; retirement requires a major version…".
  - The frozen YAML declares the task header as a create parameter: `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` CreateProject, `- $ref: "#/components/parameters/TaskId"`.
  - PRD NFR-10 (`prd.md:559`) protects only "unversioned name-only creation".
- **Failure scenario:** The cutover owner ships the 400 under AD-18. The Contracts owner blocks it under AD-31's retirement clause, or keeps accepting `projectId` and leaves the existence-oracle path open.
- **Proposed fix (AD-31):** "Only the historical unversioned request is a v1 compatibility shape. Its `name`, `description` and `setupMetadata` stay accepted throughout v1. Its optional `projectId`, and the `X-Hexalith-Task-Id` header, stop being accepted at the writer cutover under AD-18, as an explicitly approved NFR-10 change recorded in the 25-pair table."

### R-M2 — AD-24's cutover changes to v1 semantics bypass the NFR-10 evidence that AD-16 and AD-31 require

- **Severity:** Medium · **Status:** CONFIRMED (textual)
- **Spine anchor:** AD-24 line 316; AD-16 line 243; AD-17 line 251
- **Evidence:**
  - AD-24: confirmation-requiring v1 routes "return `409 RequestPreview`…; the AD-17 consumer inventory is the NFR-10 behavior-change evidence".
  - AD-16: "changing its semantics, needs explicit approval and NFR-10 retirement evidence", and "Contracts' `openapi/hexalith.projects.v1.yaml` is the frozen retained-v1 authority… taking compatibility fixes only".
  - AD-17: "After the cutover, writer rollback is a mutation freeze plus roll-forward". NFR-10 (`prd.md:559`) requires "major version, migration notice, usage evidence, compatibility tests, and rollback evidence".
  - The YAML declares no `RequestPreview` 409 on the archive, restore, link, move, unlink, folder or confirm operations. Today their 409 is the idempotency conflict (YAML line 2147).
- **Failure scenario:** After the cutover, 14 of the 25 v1 routes can never succeed. The Contracts owner reads AD-16 and requires a v2 major version and a YAML change. The cutover owner reads AD-24 and ships with the consumer inventory alone. Projects.Client regeneration then diverges from the host.
- **Proposed fix (AD-24):** "These cutover behavior changes are an explicitly approved NFR-10 exception. Their evidence is the consumer inventory, a migration notice, compatibility tests of the 25-pair table and the AD-17 roll-forward plan in place of rollback. The frozen YAML records the new `409 RequestPreview` and `400` responses as its one permitted cutover amendment. No major version is required." Also add "and the cutover amendment of AD-24" to AD-16's "compatibility fixes only".

### R-M3 — After the cutover, a classless unversioned v1 create has no defined admission

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** AD-31 line 366; AD-24 line 316
- **Evidence:**
  - AD-31: "Canonical Create Project requires one of … at `projectMetadata.metadataClass` … the v1 adapter passes its caller's value through".
  - AD-24: "From the cutover … create and Setup return `202` with the admitted task".
  - `ProjectsDomainServiceEndpoints.cs:614-620`: "only the historical unversioned shape receives the v1 name-only compatibility treatment" (no class).
- **Failure scenario:** The unversioned caller supplies no class, so the v1 adapter has nothing to pass through. Governed admission (Story 7.1) rejects it with `400`, breaking NFR-10 name-only creation, while the adapter author defaults a class, which AD-31 forbids ("never inferred").
- **Proposed fix (AD-31):** "A v1 unversioned create carries no class. Governed admission accepts it as the v1 compatibility shape with the class recorded as `unclassified-v1`, which grants and restricts nothing. Only canonical creation requires a supplied class." An approved alternative default also works, as long as one is named.

### R-M4 — "Handle rejects a mismatch without effect" cannot hold when rejections are persisted events that advance `projectVersion`

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** AD-12 line 215; AD-15 line 237; Consistency Conventions line 435
- **Evidence:**
  - AD-12: "Task-driven Project commands carry the expected Project version; `Handle` rejects a mismatch without effect, ending the task `Rejected`".
  - AD-15: "Project version is the stream sequence (`projectVersion`), which authorized callers' rejection events also advance". Conventions: "structured rejection events".
  - EventStore `AggregateActor.cs:1445`, "D3: Domain rejections (IRejectionEvent) are normal events", and :1940, "Rejection events ARE published". EventStore commands carry no expected-version precondition (no match in EventStore src outside Content/Admin).
- **Failure scenario:** Story 7.2 emits a `ProjectVersionConflict` rejection event. That advances the version and forces `RenewPreview` on every other open Preview, and a retried stale task bumps it again. Story 7.13 instead returns `NoOp`, which the saga cannot tell apart from success.
- **Proposed fix (AD-12):** "`Handle` answers a version mismatch with `DomainResult.NoOp` plus a typed mismatch outcome that the saga step maps to `Rejected`, so the stream is unchanged." If the user prefers a persisted rejection, state the opposite: "…with one structured rejection event that advances `projectVersion` (AD-15)", and drop "without effect".

### R-M5 — The AD-35 claim release on terminal tasks contradicts AD-4's immutable terminal states

- **Severity:** Medium · **Status:** CONFIRMED (textual)
- **Spine anchor:** AD-35 line 425; AD-4 line 142
- **Evidence:**
  - AD-35: "A task ending `Rejected`, `Failed` or `Cancelled` with an uncommitted claim runs that fenced release as a Projects-internal step or stays `NeedsAttention`."
  - AD-4: "`Succeeded`, `Rejected`, `Failed` and `Cancelled` are immutable … One durable transition authority rejects every transition absent from the state graph".
- **Failure scenario:** An implementer runs the release as cleanup after the terminal transition. That step has no checkpoint, receipt or retry. If the release is lost, the claim leaks, and the Folder can never be claimed by any Project: the task is terminal, and AD-4 reconciliation starts only from `NeedsAttention`.
- **Proposed fix (AD-35):** "Before a task holding an uncommitted claim takes a `Rejected`, `Failed` or `Cancelled` transition, it runs the fenced release as its final Projects-internal step. If the release cannot be confirmed, the task enters `NeedsAttention` instead of the terminal state."

### R-M6 — The AD-14 DW-85 exception says "without owner calls" while AD-10/AD-32 and the code still call Conversations

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** AD-14 line 229; AD-10 line 201; AD-32 line 376
- **Evidence:**
  - AD-14: "Until DW-85 closes, supported Get, Explain and Conversation-start read persisted Project evidence without owner calls … short of AD-20 freshness and NFR-1".
  - AD-10: these reads "require the Conversations owner to verify the named Conversation". AD-32: "require AD-10 native membership plus current Project, Folder and actor authority".
  - `ProjectContextQueryExecutor.cs:217-219`: `membership.IsCurrentMemberAsync(...)`, an owner call through `DelegatedOwnerQueryClient`.
  - `deferred-work.md:778` (DW-85) limits the gap to Folder revocation and Refresh.
- **Failure scenario:** The DW-85 test author asserts "zero owner calls" and the membership check fails it, or someone removes the membership check to conform. During DW-85, AD-32's "current … Folder … authority" is also unmet, and AD-32 does not reference the exception.
- **Proposed fix (AD-14):** "…read persisted Project and Folder evidence without Folders or Memories owner calls (Conversations membership is still verified under AD-10), so Folder revocation reaches supported context only on evidence refresh, short of AD-20 freshness, AD-32's current Folder authority and NFR-1…".

### R-M7 — The ChatBot-authority exception in AD-3 is neither ratified by the code nor in the AD-33 matrix

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** AD-3 line 136; AD-33 lines 382-403; classification line 502; G-2 line 549; diagram lines 78-86
- **Evidence:**
  - AD-3: the grant "never suffices without current Folder authority, and Folder revocation wins immediately". Memlog line 160 (H2): "Both commands and their events join … the AD-33 matrix". The matrix has no Grant/Revoke row.
  - `ProjectChatBotAuthorityQueryHandler.cs:91`: `bool allowed = state.IsCreated && state.Lifecycle == ProjectLifecycle.Active && grant is not null && !grant.Revoked && grant.ExpiresAt > …`. There is no Folder check, and the stream head is returned even when access is denied.
  - Grantors are authorized by `ProjectOwnerBootstrapVerifier.cs:25,45`: Tenants `ResolveTenantOwnerAuthority` plus Parties `ResolveChatBotPartyAuthority`. That is a Tenant-owner role outside the four AD-33 roles, and Parties is absent from the diagram and from G-2. The grant path also bypasses the EventStore validator (`ProjectsDomainProcessor.cs:168-171, 238-244`).
  - Only AD-19 sets a deadline ("By the Epic 8 release").
- **Failure scenario:** The Chatbot team relies on AD-3 and treats `allowed=true` as already Folder-gated. A Web or CLI adapter writer finds no matrix row and invents a granting role. A Parties contract change breaks grants without any G-2 pin.
- **Proposed fix:**
  - Add an AD-33 matrix row: "Grant/revoke ChatBot authority | Denied | Denied | Denied | Tenant owner with Parties human evidence (AD-3 exception; never expands matrix authority)". Adjust the role if the user decides otherwise.
  - Qualify AD-3: "…never suffices without current Folder authority (enforced in `ResolveProjectChatBotAuthority` by the Epic 8 release, AD-19)".
  - Add Parties and the Tenants owner query to the G-2 pins.

### R-M8 — The AD-14 inspection-audit carve-out is scoped to "v1", but supported reads write the same facts, and the AD-24 denial rows have no durable home

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** AD-14 line 229; AD-24 line 316; AD-26 line 330
- **Evidence:**
  - AD-14: "Carve-out: v1 list/open inspection audit (`ProjectReadAudit` facts, audit-index pages) is durable primary state … excluded from rebuilds".
  - The supported SDK executors write the same facts: `ProjectReadQueryExecutor.cs:148,155,225` and `ProjectOperatorQueryExecutor.cs:17` (`audit.RecordAsync`).
  - AD-24: "its only durable writes are those denial rows and the AD-14 inspection audit". The store for the denial rows is not named.
- **Failure scenario:** The Story 8.10 rebuild author excludes only keys written by v1. The rebuild then drops supported-read inspection facts and the cutover denial rows, which breaks AD-26's 365-day retention.
- **Proposed fix (AD-14):** "Carve-out: every `ProjectReadAudit` fact and audit-index page, whichever ingress writes it, and the AD-24 cutover denial rows, are durable primary state…".

### R-M9 — Deferring the production provider class conflicts with AD-27's fixed Redis lane and with Epic 7 claim evidence

- **Severity:** Medium · **Status:** CONFIRMED (textual)
- **Spine anchor:** Deferred line 595; AD-27 line 336; Stack line 460; Stack line 465
- **Evidence:**
  - Deferred: "Production state-store and pub/sub provider class | G-5 decision before Story 8.9 and 8.10 evidence; AD-27, AD-28 and AD-30 evidence runs on the selected class".
  - AD-27: "Measure … on persistent Dapr/Redis". Stack 460: "AD-15 serialization, AD-17 fencing and AD-35 claim evidence needs runtime `1.19.2`", and that runtime's fix is provider-specific (Redis, PostgreSQL, in-memory). The G-6 qualifier uses "real PostgreSQL".
- **Failure scenario:** Stories 7.1–7.15 produce two-instance claim and fencing evidence on whatever provider each lane happens to use, before the class is chosen. AD-27 measures on Redis. Production then selects PostgreSQL, and the concurrency evidence does not transfer.
- **Proposed fix:**
  - Deferred revisit condition: "G-5 decision before Story 7.1 two-instance evidence. Until then AD-15/AD-17/AD-35 concurrency evidence runs on every candidate class."
  - AD-27: replace "on persistent Dapr/Redis" with "on the selected production provider class".

### R-M10 — "First writer" stays undefined next to the single "writer cutover", and the epics still speak in per-slice terms

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** Implementation boundaries line 52; G-1 line 548; G-2 line 549; AD-17 line 251
- **Evidence:**
  - Writers row: "…single-writer fencing gate the first writer". G-1: "Before the first writer". G-2: "Before the first affected writer".
  - AD-17: "every writer-cutover rule means this event". Memlog line 151 (H4): "no per-slice enablement".
  - `epics.md:1694`: "Before opening an affected writer slice". `epics.md:2118`: "before their value slice cuts over".
- **Failure scenario:** One owner reads "first writer" as the cutover deploy and starts Story 7.1 without G-1 pins. Another reads it as Story 7.1 and also demands cutover-only gates there, such as single-writer fencing. Story authors keep per-slice opening language.
- **Proposed fix:**
  - Writers row: "G-1, G-2, accepted 7.1-P1/P2, one authorized Folder before activation, the AD-35 claim and durable owner receipts gate Story 7.1 SDK-writer qualification. Single-writer fencing gates the writer cutover."
  - G-1 and G-2: "Before Story 7.1 entry".
  - Route an epics correct-course to remove the per-slice wording.

### R-M11 — The spine departs from the PRD in three places, with no open item tracking the PRD amendments

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchor:** AD-3 line 136; AD-20 line 292; AD-27 line 336; Deferred table lines 582-600
- **Evidence:**
  - PRD A-6 (`prd.md:604`): "a Projects-issued credential". AD-20: "its component-scoped, session-bound credential comes from the G-5 platform identity service".
  - The PRD has no requirement for `GrantProjectChatBotAuthority` (no PRD match), while AD-3 adopts it.
  - NFR-5 (`prd.md:551`): "100,000 retained audit records per Project". AD-27 reads this as "not a retention cap".
  - Memlog lines 160, 166 and 170 say each PRD update is "offered", but no Deferred or open row records them.
- **Failure scenario:** The Story 7.16 author follows the product authority (memlog line 9), builds a Projects-issued signing credential, and violates AD-28.
- **Proposed fix (Deferred row):** "PRD amendments pending Product Owner approval: (a) ChatBot delegated-operation record under A-3; (b) A-6 platform-issued component credential; (c) NFR-5 audit count as online query shape | Before Story 7.11, 7.12, 7.16 and 8.1 entry; until then AD-3, AD-20 and AD-27 govern."

### R-M12 — The operational envelope sets no durability objective for durable state outside the event store

- **Severity:** Medium · **Status:** PLAUSIBLE
- **Spine anchor:** AD-28 line 342; AD-14 line 229; Deferred line 596
- **Evidence:**
  - AD-28: "committed-event RPO 0 inside the configured primary-region durability domain". Only events are covered.
  - AD-14 makes inspection audit "durable primary state in the read-model store". AD-4 and AD-5 put task, idempotency and confirmation records in platform stores. Deferred line 596 defers only "restore consistency across stores".
  - NFR-4: "Accepted tasks are never silently dropped". NFR-8 sets audit retention at 365 days or more.
- **Failure scenario:** The platform provisions the read-model store as cache-grade, which is reasonable because read models are rebuildable. A node loss then drops primary audit facts or idempotency records with no stated objective breached.
- **Proposed fix (AD-28):** "RPO 0 within the primary-region durability domain also binds Durable Task, idempotency and confirmation records and the AD-14 audit carve-out keys. Their stores are durable classes, never cache-grade." Alternatively, add it explicitly to the Deferred line 596 row.

---

## Low

### R-L1 — The AD-35 claim aggregate-ID derivation is unspecified
- **Status:** PLAUSIBLE · **Anchor:** AD-35 line 423; AD-18 line 278
- **Evidence:** "one stream per `(TenantId, FolderId)`". EventStore aggregate IDs must match `^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$` (`references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs:11`). Folders allows `^[A-Za-z0-9][A-Za-z0-9_-]{15,127}$` (`references/Hexalith.Folders/src/Hexalith.Folders.Contracts/Queries/GetEffectivePermissions.cs:16`), so a trailing `-` or `_` is valid in Folders but not in EventStore. Legacy Folder IDs are unconstrained.
- **Failure scenario:** Stories 7.1, 7.6 and 7.14 and the cutover reconstruction each derive the key differently, raw or encoded. One Folder then gets two claim streams, and exclusivity is lost.
- **Fix:** "One `Projects.Contracts` helper derives the claim aggregate ID from the FolderId. It uses the FolderId verbatim when it matches the EventStore pattern and a fixed, versioned encoding otherwise. Every workflow and the cutover reconstruction use that helper."

### R-L2 — AD-19 gives `RequestPreview` a status code only for v1
- **Status:** CONFIRMED · **Anchor:** AD-19 line 284; AD-24 line 316
- **Evidence:** AD-19: "unusable Selection Evidence on a new binding returns `RequestPreview` (AD-5/13)", with no HTTP status. AD-24 gives v1 "`409 RequestPreview`".
- **Failure scenario:** The Web, Chatbot and Gateway bindings pick `200`, `409` or `422` independently.
- **Fix (AD-19):** "…returns `409` with `RequestPreview`, admits no task (AD-5/13) on every surface."

### R-L3 — AD-35 does not say what Story 7.15 may decide on a key with conflicting holders
- **Status:** PLAUSIBLE · **Anchor:** AD-35 line 427
- **Evidence:** "until Story 7.15 repair or the FR-8 Administrator binding workflow (Story 7.6) leaves at most one holder". Memlog line 158 rejected "keeping the earliest binding Active (auto-resolution code that guesses)". `epics.md:2129` says: "resolve the incumbent from authoritative history".
- **Failure scenario:** Story 7.15 reintroduces earliest-binding resolution as "history".
- **Fix:** "Story 7.15 may remove a holder only on authoritative facts (a later archive or a superseding binding in its history). It never chooses between two valid Active holders; that choice belongs to FR-8."

### R-L4 — The allow-all validator type ships in the packable domain package
- **Status:** CONFIRMED · **Anchor:** AD-20 line 292; AD-24 line 316
- **Evidence:** `src/Hexalith.Projects/Authorization/AllowingProjectEventStoreAuthorizationValidator.cs:13` in `Hexalith.Projects.csproj` (`<IsPackable>true</IsPackable>`). AD-20's remediation moves only the registration out of `AddProjectsServerRuntimeInfrastructure`.
- **Fix (AD-20):** "…moves the allowing validator and provider types into `Projects.Testing` or explicit local-development composition, so no shipped package contains them…".

### R-L5 — Form: transitional work and run details remain inside Rules, and the spine grew
- **Status:** CONFIRMED · **Anchor:** AD-20 line 292; AD-29 line 352; AD-17 line 270; Stack line 457; Runner contract line 506
- **Evidence:**
  - Rules still hold dated work and remediation tasks: "`GetProjectAuditQuery` and `GetProjectTaskStatusQuery` lose theirs before the next Contracts publication"; "absent from published EventStore through `5.5.2`"; "the Module.Cli `6.4.2` pin is corrected or explained"; "FrontComposer CLI `6.0.1` output…"; the v4 normalization ID and the 77-cell and v3-receipt operating detail.
  - Size went from 89,566 to 95,244 bytes. Rule word counts roughly doubled for AD-12 (85→170), AD-18 (90→196) and AD-20 (175→311). M17 (the Rule/Transitional split, about 40 KB) is deferred only in the memlog.
- **Fix:** At the M17 revision, move each remediation clause to a labeled "Transitional (until X; owner)" sub-bullet, or to the sprint tracker, and move read-cutover operating detail into `docs/read-cutover.md`. No rule change is needed now.

### R-L6 — The AD-24 build check exempts "adapter-only code" with no mechanical marker
- **Status:** CONFIRMED (textual) · **Anchor:** AD-24 line 316
- **Evidence:** "a build check rejects them elsewhere except adapter-only code". All query handlers live in Server today (`ProjectsServerServiceCollectionExtensions.cs:185-208, 231`), so the check cannot tell a v1-only adapter handler from a misplaced domain handler.
- **Fix:** "…except types in the `Hexalith.Projects.Server.V1Adapter` namespace (or carrying a named marker attribute), which retire with the adapter."

---

## What held up

- **Coverage.** FR-1..FR-25 and NFR-1..NFR-11 each map to governing ADs in the Capability → Architecture Map (lines 559-577). The frontmatter scope and binds are unchanged. All 22 relative links resolve, and no personal names remain.
- **Brownfield ratification:**
  - `Program.cs` is the stated interim host (`AddProjectsDomainService` → `AddEventStoreDomainService(typeof(ProjectsDomainProcessor).Assembly)`, the Server assembly).
  - `MapProjectsHumanEndpoints` maps exactly 25 method-and-path pairs under `HumanPolicy`.
  - The allowing validator and Dapr provider are registered at `ProjectsServerServiceCollectionExtensions.cs:240-243`.
  - The six decorated Contracts types are the four Story 6.6 reads plus audit and task status, which matches AD-29's removal instruction.
  - `tools/release-packages.json` lists five packages, including `ServiceDefaults`.
  - `UI.Contracts` and `Server` are non-packable. v1 create mints GUID-N or takes `projectId` (`ProjectsDomainServiceEndpoints.cs:651-653`).
  - Direct create authorizes before parsing (lines 568-575), while proposal confirm parses first (`ProposeNewProjectEndpoint.cs:191` before 205), matching the bounded AD-31 exception.
  - The inbound token is forwarded through `EventStoreGatewayTokenForwardingHandler`, matching the AD-20 exception.
- **Technology (light touch).** Every Stack version matches the committed Builds catalog `bf8a28b4`: EventStore 5.5.1, FrontComposer 6.1.0, McpCli.Abstractions 3.0.0, Dapr packages 1.19.1 with catalog runtime 1.19.2, Aspire SDK 17.0.0 and Toolkit `17.0.0-preview.1.261120-0454`, Fluent UI 5.1.0, Fluxor 6.11.0, Ulid 1.4.2, NSwag 14.8.0, xunit.v3 4.0.3, Shouldly 4.3.0, NSubstitute 6.2.0. `global.json` pins 11.0.100 with `latestPatch`. The EventStore gitlink is `208af26a`, and the FrontComposer CLI tool is pinned at 6.0.1, both as stated. The McpCli README at `6993abf` confirms the four enrolled reads. Upstream release currency after June 2026 was not independently web-verified.
- **Changed rules that are sound:**
  - AD-5: scoped idempotency, nonterminal-plus-30-day retention, and the v1 key import.
  - AD-12: the commit step keyed by task and step, and the pre-commit authority recheck.
  - AD-17: one fenced cutover, one keyed processor with its replay, snapshot invalidation, and fold-state equivalence.
  - AD-19: the four-query allow-list for the life of v1, decoupled from the writer cutover.
  - AD-29: decoration as enrollment, enforced by a reflection test.
  - AD-35: the claim as a second aggregate type with generation equal to stream version, and conflicting keys quarantined.
  - Each of these is enforceable and prevents its stated divergence. R-H1, R-H2 and R-M1–M5 are interaction defects between these rules, not defects within them.
- **Form.** M16 succeeded: the boundaries table now holds only durable gate conditions with a tracker pointer, and CI run IDs, story statuses and dated attempts are gone from the spine.
