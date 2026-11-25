# Validate-intent review (rubric walker): Hexalith.Projects architecture spine

- **Target:** `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md`, 600 lines, 89,566 bytes, SHA-256 `01b89a2f5e076a52b17f253417b7472ae85c0c7d26ae5a3c21c428e5868eb5fe`, re-checked unchanged when this review was written.
- **Repository:** Hexalith.Projects, HEAD `31b35637` (main). EventStore submodule at `208af26a`, Builds at `bf8a28b4`.
- **Lens:** good-spine rubric, items 1 to 9. I did not read earlier reviews or validation reports.

**Verdict: PASS WITH FINDINGS.** Resolve the three high findings before the Story 7.1 first-writer gate opens. Resolve H3 before DW-63 acceptance.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 3 |
| Medium | 8 |
| Low | 4 |

## Rubric walk (summary)

| # | Item | Judgment |
| --- | --- | --- |
| 1 | Fixes the real divergence points and misses none | **Partial.** The spine is strong on authority, task truth, confirmation, cutover and disclosure. It misses four points: where the Projects-owned selection/confirmation component lives and who issues its credential (H2), what the 25 retained v1 mutation routes do after the writer cutover (H1), where the AD-35 claim lives (M2), and where new handlers go while the Server still holds them (M8). |
| 2 | Every Rule is enforceable and prevents its divergence | **Mostly.** Three rules fall short. The AD-19 bounded exception cannot be tested as written (M1). AD-28 says availability is "enforced" with no SLI behind it (M4). AD-20's allow-all prohibition is violated today, and no gate or owner is assigned to fix it (H3). |
| 3 | Nothing Deferred or open lets units diverge now | **Partial.** Two items have no home anywhere. The audit archive (M5) is outside every gate and every Deferred row. The production state-store provider (M3) is split between Redis and PostgreSQL evidence lanes. The machine-surface expansion trigger is vague (L2). |
| 4 | Named technology is current | **No glaring issue.** The Stack matches `global.json`, the Builds `bf8a28b4` catalog and `tools/release-packages.json`. Depth is left to the technology lens. |
| 5 | Ratifies the brownfield codebase | **Holds, with two exceptions.** DW-63 host shape, the 25 v1 routes, human policy on v1 routes, the single keyed processor and the query-response policy are all as stated (see "Checked and held up"). The exceptions: the AD-20 composition violation is recorded but not routed (H3), and query and projection handlers still live in the Server (M8). |
| 6 | Covers FR-1..FR-25 and NFR-1..NFR-11 | **Covered at capability level.** Every FR and NFR has an AD row in the Capability map (lines 562-585). Gaps are below capability level: PRD assumption A-6 (owner: architect) has no home (H2), NFR-5's audit figure is reinterpreted without a gate (M5), and there is no home for FR-3/FR-19 Setup validation ownership or FR-22's "no cross-Project enumeration" (L1). |
| 7 | No AD weakens or contradicts another | **Two contradictions.** After the writer cutover, AD-24 conflicts with AD-13/AD-5 and with AD-17/NFR-10 (H1). AD-35 conflicts with AD-18's literal aggregate identity and AD-17's "one … aggregate" (M2). The AD-19 bounded exception does not contradict AD-32. Its sunset trigger is placed in an unrelated unit (M1). The AD-24 amendment agrees with AD-20 on the human policy. |
| 8 | Every initiative-altitude dimension is decided, deferred or open | **Partial.** Deployment, environments and provider are assigned to the platform and cross-region DR is deferred. Not covered: an SLI/SLO measurement owner, alerting, backup/restore consistency across event, task, confirmation and idempotency stores, and the production state-store provider (M3, M4). |
| 9 | Form: terse build substrate | **No.** Tracker state, CI run links, dated evidence and named people live in the spine, and some were already stale on review day (M6). Size and rule density invite drift for small agents (M7). All 32 relative links resolve, the Section G anchor resolves, and there are no placeholders other than the intended `<fresh-absolute-directory>` command argument. |

---

## High

### H1. The post-cutover rule for retained v1 mutation routes contradicts "no confirm API" and the v1-compatibility promise

- **Severity:** High · **Status:** CONFIRMED (code and text)
- **Spine anchors:** AD-24 Rule (line 321); AD-13 Rule (line 217); AD-5 Rule (line 167); AD-17 read-cutover paragraph (line 262); AD-19 (line 289).
- **Evidence:**
  - AD-24 (line 321): "From that cutover, a v1 mutation admits work only through the selected SDK writer's governed admission, with platform-generated identities and the AD-5/AD-19 Preview, confirmation and task outcomes."
  - AD-13 (line 217): "Consumption occurs only inside the Projects-owned confirmation component (no confirm API)."
  - AD-5 (line 167): a new binding without valid Selection Evidence returns `RequestPreview`.
  - AD-17 (line 262): "Preserve retained v1 REST and client signatures, one-string constructors and deconstruction."
  - Code: the retained adapter maps two confirm APIs, `POST /api/v1/projects/proposals/confirm` (`src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:292`) and `POST /api/v1/projects/{projectId}/conversations/{conversationId}/resolution/confirm` (`:364`). It also maps additive link and folder routes (`:324`, `:406`, `:426`, `:468`) that carry no Selection Evidence. `ConfirmProjectResolutionPayload` binds only a `ResolutionResult` discriminator.
  - The memlog confirms these are Boolean confirmations. Entry 101 rejected a trigger because it "would have required reproducing the Boolean confirmations AD-5 forbids".
- **Failure scenario:** Story 7.11 and 7.12 implementers, and the 7.1 writer cutover, can each read the spine differently:
  - (a) Make the v1 confirm routes consume a Confirmation Artifact. That creates the confirm API AD-13 forbids.
  - (b) Make them, and every unselected v1 link, return `RequestPreview`/`409`. That retires v1 mutation behavior without the NFR-10 major-version, migration-notice, usage and rollback evidence that AD-24 itself requires for retirement.
  - (c) Keep the direct submit path. That violates AD-24.

  Retained `Projects.Client` consumers then get route-dependent behavior across replicas or releases.
- **Proposed resolution:** Add a per-route post-cutover disposition to AD-24, for example:

  > "At writer cutover, the two v1 confirm routes and v1 additive routes without Selection Evidence return `RequestPreview` with no effect. v1 create, Setup, archive and restore return `202` plus task. This is a v1 behavior change: before the cutover, record the consumer inventory and the NFR-10 evidence, or freeze those routes."

  Alternatively, declare v1 mutation retirement part of the writer cutover under NFR-10.
- **Acceptance check:** The spine, or a document it links, has a 25-row table of v1 method/path × {pre-cutover, post-cutover} outcome. Contract tests assert that no v1 route consumes a Confirmation Artifact. The writer-cutover gate cites the NFR-10 evidence or the freeze.

### H2. The Projects-owned selection and confirmation component has no structural home or credential issuer (PRD A-6 unaddressed)

- **Severity:** High · **Status:** CONFIRMED (text against PRD, addendum and epics)
- **Spine anchors:**
  - AD-5 (line 167): "minted only by the actor's pick inside the Projects-owned component"
  - AD-13 (line 217): "Projects-owned confirmation component"
  - AD-20 (line 297): "It uses its own session-bound credential unavailable to the host adapter"
  - Capability map FR-25 (line 575)
  - AD-24 (line 321): "Retire Projects-owned … UI … runtime projects"
  - AD-16 (line 239) and Seed (line 481): `Projects.UI.Contracts` holds "presentation metadata only" / "FrontComposer descriptors only"
  - The spine never mentions A-6.
- **Evidence:**
  - PRD `prd.md:604`, A-6: "a Projects-owned interactive component hosted via FrontComposer on Chatbot and Web … authenticate their own requests with a Projects-issued credential … Owner: Winston".
  - addendum.md:41 and :146: "Projects-owned FrontComposer-hosted selection component", which "consumes the artifact with its own session-bound credential".
  - epics.md:2153 (Story 7.16): "the component authenticates with a Projects-issued credential".
  - Conflict: AD-28 (line 347) gives identity, KMS and secrets to the platform, and AD-3 (line 132) says "Projects issues no Project-level grant".
- **Failure scenario:**
  - Story 7.16 puts the component in the retiring `Hexalith.Projects.UI`, or in `Projects.UI.Contracts` (which then stops being descriptors-only), or in the Chatbot repository (which stops being Projects-owned).
  - Story 7.16 also invents a Projects-issued credential that bypasses G-5, while 8.8-P3-H pins a Chatbot host against a different hosting contract.
  - 7.11 and 7.12 build the confirmation component a third way.
- **Proposed resolution:**
  - Add a Structural Seed row for the interactive component: package name, packaging, FrontComposer hosting contract, and which repositories host it.
  - Decide the credential issuer, for example: "the platform identity service (G-5) issues a component-scoped, session-bound credential; Projects never holds signing keys".
  - Map A-6 to an AD. If it is not decided, record it as an open question with the architect as owner, and gate 7.11, 7.12 and 7.16 on it.
- **Acceptance check:** The spine names the artifact and its credential issuer and cites A-6. Story 7.16's entry gate references that row. AD-24's retirement list and the `UI.Contracts` definition no longer exclude the component.

### H3. AD-20's allow-all prohibition is violated in the shipped host, with no gate or owner in the spine

- **Severity:** High · **Status:** CONFIRMED (code)
- **Spine anchors:** AD-20 Rule (line 297): "Allow-all implementations exist only in explicit test composition." Structural Seed (line 487) records the violation. The first-writer gate list (line 51) and G-5 (line 557) do not mention it.
- **Evidence:**
  - `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:240-243` replaces the deny-all defaults (`:169-170`) with `AllowingProjectEventStoreAuthorizationValidator` and `AllowingProjectDaprPolicyEvidenceProvider`.
  - `AllowingProjectEventStoreAuthorizationValidator` returns `Allowed("eventstore_validator_test_allow")` (`src/Hexalith.Projects/Authorization/AllowingProjectEventStoreAuthorizationValidator.cs:22`).
  - The swap happens in `AddProjectsServerRuntimeInfrastructure`, which `AddProjectsDomainService` calls on the shipped path (`:60`).
  - The single writer consumes the validator (`ProjectsDomainProcessor.cs:41,63`), and `ProjectAuthorizationGate` consumes both (`Authorization/ProjectAuthorizationGate.cs:18-19`).
  - The DW-63 spec never mentions it (no match for "allowing/allow-all/8.8").
  - Memlog entry 104 routes it only to "8.8-P1 evidence", a release-stage package.
- **Failure scenario:** DW-63 is accepted as behavior-preserving, and Story 7.1 activates the first writer on a host whose Projects-layer EventStore-authorization validator approves every request. That leaves only the SDK route policy in front of the single writer. The fix is deferred to the 8.8 release evidence, after writers have been live in managed environments.
- **Proposed resolution:** Add to AD-20 or to the line 51 first-writer gates: "The shipped composition registers no `Allowing*` validator or evidence provider. Removing them, together with a real EventStore-authorization validator and Dapr-policy provider, gates DW-63 acceptance (or at the latest the first writer). Owner: Projects Server owner."
- **Acceptance check:** A host-composition test resolves the shipped `AddProjectsDomainService` container and finds no `Allowing*` implementation. The gate row names the owner and the unit.

---

## Medium

### M1. The AD-19 bounded exception cannot be tested as written, and its sunset sits in an unrelated unit

- **Severity:** Medium · **Status:** CONFIRMED (code)
- **Spine anchor:** AD-19 Rule (line 289): "invalid input to those queries returns the same safe `404` as denial, with no protected detail … At that cutover, one registered query-response policy applies this mapping to every Projects query type."
- **Evidence:**
  - `src/Hexalith.Projects.Server/Queries/ProjectsQueryResponsePolicy.cs:21-25` returns `Results.NotFound(result)`, the raw result, for the four exception types.
  - For those types the handlers emit different bodies:
    - `InvalidEnvelope` for invalid input (`GetProjectContextQueryHandler.cs:48`, `GetConversationStartSetupQueryHandler.cs:69`, `ExplainContextSelectionQueryHandler.cs:48`)
    - `safe-denial` for denial (`:37,45`)
    - `Forbidden` for the Chatbot authority query (`ProjectChatBotAuthorityQueryHandler.cs:46-86`)
  - The body rule exists only in memlog entry 107 ("bodies carry only safe-denial, InvalidEnvelope or Forbidden").
  - The DW-63 spec (line 38) notes that the Gateway's `DaprDomainQueryInvoker` maps receiver 404/403 to safe denial. The 404→400 change therefore also changes what the Gateway and Chatbot see.
- **Failure scenario:**
  - A test author reads "the same safe 404 as denial" and asserts identical responses at the receiver, so DW-63 fails.
  - An implementer "fixes" the body and breaks the frozen DW-63 row.
  - At the 7.1 writer cutover, a writer story must change four read transports that Chatbot consumes. No unit owns the consumer impact or a refresh of the v4 comparison.
- **Proposed resolution:** Reword to: "same `404` status; the body is exactly one of `safe-denial`, `Forbidden`, `InvalidEnvelope` and carries no protected detail." Name the unit that performs the 404→400 switch, with its Gateway and Chatbot compatibility evidence and the read-comparison impact. Alternatively, decouple the switch from the writer cutover.
- **Acceptance check:** A receiver-level test enumerates the allowed body set for the four types. The switch has a named story with consumer evidence.

### M2. The AD-35 exclusive claim has no structural home, and AD-18 and AD-17 wording forbid the obvious one

- **Severity:** Medium · **Status:** PLAUSIBLE (text-level)
- **Spine anchors:**
  - AD-35 (line 428): "Projects serializes claim acquisition by `(TenantId, FolderId)` across all writers"
  - AD-18 (line 283): "aggregate identity is `Tenant/projects/ProjectId`"
  - AD-17 (line 245): "one convention-discovered `projects` aggregate"
  - AD-15 (line 233)
  - The Structural Seed (lines 475-494) and the Deferred table (lines 587-600) say nothing about where the claim lives.
- **Failure scenario:** Story 7.1, which carries the first writer and 7.1-P2, picks one of three designs:
  - a FolderId-keyed aggregate in the `projects` domain, which violates AD-18's literal identity and "one aggregate";
  - a Projects-owned Dapr state lock, which violates AD-1;
  - a platform exclusive-key primitive requested under G-1.

  Stories 7.6, 7.14 and 7.15 then inherit or re-invent the choice, and the legacy-incumbent reconstruction (line 432) is written against different stores.
- **Proposed resolution:** Decide the substrate. For example: "a second Projects aggregate type `project-folder-claim` keyed `Tenant/project-folder-claims/FolderId` in the same DomainService; AD-18's identity rule applies to Project aggregates." Amend AD-17's "one … aggregate" accordingly. If the claim is a platform primitive instead, add it to G-1.
- **Acceptance check:** The Seed lists the claim artifact and its key. AD-18 and AD-17 wording admit it.

### M3. The production persistence provider is undecided while evidence lanes split between Redis and PostgreSQL

- **Severity:** Medium · **Status:** CONFIRMED (text)
- **Spine anchors:**
  - AD-27 (line 341): "Measure … on persistent Dapr/Redis"
  - Stack (line 469): "real PostgreSQL two-sidecar restart"
  - AD-28 (line 347): "Local, CI, and production bind the same logical resource contract"
  - Deferred (line 591) covers only the Durable Task provider; line 598 covers the cloud provider.
- **Failure scenario:** Story 8.9 qualifies NFR-5 performance on Redis, while 8.10 and G-6 qualify RPO and restart on PostgreSQL, and production binds a provider neither lane exercised. AD-30 then accepts evidence that does not describe production.
- **Proposed resolution:** Require AD-27, AD-28 and AD-30 evidence (performance, resilience, RPO) to run on the state-store and pub/sub provider class selected for production. Record that selection as a G-5 platform decision before 8.9 and 8.10, or add a Deferred row that names which evidence it invalidates.
- **Acceptance check:** One provider class is named for production-qualifying evidence. The AD-27 and G-6 wording agree.

### M4. The operational envelope is missing SLI/SLO measurement, alerting and backup/restore consistency

- **Severity:** Medium · **Status:** PLAUSIBLE
- **Spine anchors:** AD-28 Rule (line 347): "The platform configuration and evidence must enforce 99.9% monthly availability, 15-minute service RTO … committed-event RPO 0". Deferred (line 598): cross-region DR only. AD-17 P1R note (line 274) is the spine's only restore statement.
- **Evidence:**
  - PRD NFR-3 (`prd.md:546`) sets a 99.9% availability *target*. Configuration cannot enforce availability, and the spine names no SLI, measurement source or alert owner.
  - The spine has no rule for restoring the event store relative to the platform task, confirmation, idempotency and audit-archive stores.
  - A search for "backup", "SLO", "alert" and "runbook" finds only line 274.
- **Failure scenario:**
  - Story 8.6 (health) and 8.11 (release) each define availability differently.
  - A point-in-time restore of the event store leaves Durable Task records and consumed Confirmation Artifacts ahead of the events, which yields duplicate or orphaned owner calls that nothing routes to `NeedsAttention`.
- **Proposed resolution:** Add to AD-28:
  - an SLI definition: the success ratio of authenticated metadata APIs and task admission, measured at the Gateway by platform telemetry;
  - platform-owned alerting;
  - a restore rule: "a restore is cross-store consistent, or mutation freezes and in-flight tasks enter `NeedsAttention`";
  - a restore-drill row in AD-30.

  Alternatively, add Deferred rows with owners.
- **Acceptance check:** AD-28 names the SLI and its source, the restore-consistency rule and the evidence row.

### M5. AD-27 introduces an ungated platform audit archive and reinterprets NFR-5

- **Severity:** Medium · **Status:** CONFIRMED (text)
- **Spine anchor:** AD-27 (line 341): "the audit figure is not a retention cap, and older records within the full AD-26 retention period move losslessly to a bounded, queryable platform archive".
- **Evidence:** PRD NFR-5 (`prd.md:551`) says "100,000 retained audit records per Project". No external gate (G-1..G-6, lines 553-558), Deferred row or Capability-map row (line 572, FR-21) names the archive.
- **Failure scenario:** Stories 8.1 (audit reads) and 8.9 (performance) assume an archive nobody builds. Or Projects builds its own archive store, which violates AD-1.
- **Proposed resolution:** Add the archive capability to G-1 or G-5 with an owner and entry condition, or move it to Deferred with a revisit condition. Record the NFR-5 interpretation as a product decision with a PRD or addendum link.
- **Acceptance check:** A gate or Deferred row names the archive owner, and FR-21's map row cites it.

### M6. The spine carries tracker state, CI links, dated evidence and names, some already stale

- **Severity:** Medium · **Status:** CONFIRMED
- **Spine anchors:**
  - line 43: McpCli disposition note
  - lines 46-52: "Implementation boundaries on November 24, 2026" (story statuses, readiness `FAIL / NOT_READY`, CI run IDs)
  - lines 264-277: v3/v4 receipt scope; "Measured P1R application (approved 2026-10-06): Attempt 21 …"
  - line 355: McpCli `6993abf`, DW-82
  - line 363: "Jerome and John record …"
  - lines 456, 469, 471: revision anchors, CI job link, historical qualification
- **Evidence of staleness on review day:**
  - HEAD is `31b35637`, two commits past the `280b1eed` anchor (lines 51, 456). One of them is `31b35637 test(server): pin 7.1-P1 creation-contract fixtures`.
  - CI run `45479334086` on the anchor `280b1eed` concluded `failure` at `ci / build-and-test` (from `gh run view`). Line 52 lists only the `5f1d955b` and `0db83209` failures.
  - CI run `45488087994` on `31b35637` is in progress.
  - The memlog shows three finalizations on 2026-11-24 (entries 106 and 108), each forcing new SHA and Section G churn.
- **Failure scenario:** A small agent reads "Story 7.1 is blocked" or "builds in source mode only" as an invariant and refuses or schedules work wrongly. Every tracker change forces a spine re-finalization and new SHA, plus rework of anything bound to the digest.
- **Proposed resolution:**
  - Move the boundaries table, McpCli note, CI links, P1R measured paragraph and Historical qualification paragraph to the memlog, `sprint-status.yaml` or `docs/read-cutover.md`.
  - Keep one pointer: "Current delivery state: sprint tracker."
  - Use role names ("Release Owner", "Product Owner") instead of personal names.
- **Acceptance check:** The spine contains no `actions/runs` URLs, story statuses or dated attempt numbers. Commit SHAs appear only as pinned dependency coordinates in Stack.

### M7. Size and rule density invite drift for small agents

- **Severity:** Medium · **Status:** PLAUSIBLE (judgment)
- **Spine anchors:**
  - The spine is 600 lines and 89,566 bytes, roughly 22k tokens.
  - Single-bullet rules of about 300-450 words: AD-4 (line 138), AD-5 (line 167), AD-17 (line 245, plus four paragraphs at lines 262-277), AD-24 (line 321), AD-33 (lines 387-416).
  - These rules mix target invariants, transitional clauses ("Until the AD-17 writer cutover…", lines 289 and 321) and present-tense state ("Today `AddProjectsDomainService` …", line 487).
- **Failure scenario:** An agent that loads AD-24 alone misses the AD-19 query exception and the AD-20 human-policy clause that qualify it. An agent that loads AD-19 alone takes the transitional 404 as permanent.
- **Proposed resolution:** Split each AD into a **Rule**, the target invariant in at most ~120 words, and a **Transitional (until X)** sub-bullet. Move read-cutover operating detail to `docs/read-cutover.md`. Aim for no more than ~40 KB.
- **Acceptance check:** Each AD Rule is at most ~120 words, transitional clauses are labeled, and spine lint passes.

### M8. New handlers have no defined home while the Server still holds handlers and projections

- **Severity:** Medium · **Status:** CONFIRMED (code)
- **Spine anchors:**
  - AD-24 (line 321): "retain non-packable `Projects.Server` only as the assembly-explicit two-line DomainService host plus module registration and one `MapProjectsHumanEndpoints` call"
  - Seed (line 480): `Hexalith.Projects` holds "handlers, projections, workflow definitions"
  - line 487 describes the current registrations but not the handlers.
- **Evidence:**
  - Query handlers, executors, read models and projection handlers live in the Server: `src/Hexalith.Projects.Server/Queries/*` (for example `GetProjectContextQueryHandler.cs`, `ListProjectsQueryHandler.cs`), `Projections/ProjectReads`, `Projections/ConversationStartSetup`, and `Dapr*ReadModel.cs`.
  - They are registered in `ProjectsServerServiceCollectionExtensions.cs:185-208,231-255`.
- **Failure scenario:** Epic 7 and 8 stories add new query, projection and workflow handlers next to the existing ones in the Server, growing the part AD-24 says to shrink. Other stories put them in `Hexalith.Projects`. The domain package and the host then split the same concern.
- **Proposed resolution:** Add to AD-24: "New handlers, projections and workflow definitions are added to `Hexalith.Projects`; existing Server-resident handlers move there before AD-24 retirement (named unit)."
- **Acceptance check:** The spine names the target package for new handlers and the unit that moves existing ones. A build check (for example, no new `IDomainQueryHandler` types in the Server assembly) enforces it.

---

## Low

### L1. Some FR consequences have no spine home

- **Severity:** Low · **Status:** CONFIRMED (text)
- **Anchor:** Capability map, FR-3 and FR-19 row (line 566), "AD-15, AD-16, AD-18, conventions".
- **Evidence:**
  - FR-3 (`prd.md:248`): a Setup change "never widens an already-admitted … snapshot".
  - FR-19 (`prd.md:467`): the secret-class rejection list.
  - FR-22 (`prd.md:504`): "v1 offers no cross-Project audit or reference enumeration".
  - None of these has an AD, convention or validator-ownership rule. AD-31's single-validator rule covers only creation metadata class.
- **Failure scenario:** Story 7.2 (Setup) and Chatbot-side validation diverge on which validator rejects secrets. An operator story adds cross-Project audit paging.
- **Resolution:** Add a one-line convention: "One shared Setup validator in `Hexalith.Projects` governs FR-3/FR-19 on every surface; v1 operator reads are per-Project only."
- **Acceptance check:** The FR-3/FR-19 row cites a named rule.

### L2. The machine-surface expansion revisit trigger is vague

- **Severity:** Low · **Status:** CONFIRMED (text)
- **Anchor:** Deferred (line 596): "Expansion beyond the four enrolled McpCli reads … | UX/API artifacts".
- **Evidence:** AD-21 (line 303) promises export on CLI and MCP, and FR-22/FR-24 require CLI/MCP operator access. Stories 8.4 and 8.5 exist (epics.md:2675, :2696).
- **Resolution:** Set the revisit condition to "Story 8.4/8.5 entry with G-3 enrollment and Gateway-authorization evidence, owner McpCli owner + Projects owner".
- **Acceptance check:** The Deferred row names the gate and the owner.

### L3. The target host snippet reads as working code today

- **Severity:** Low · **Status:** CONFIRMED (code)
- **Anchor:** Structural Seed (lines 489-492): `builder.AddEventStoreDomainService(typeof(ProjectAggregate).Assembly);`.
- **Evidence:** `ProjectAggregate` is a `public static partial class` (`src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs:28`), not an `EventStoreAggregate<TState>`. The live host passes `typeof(ProjectsDomainProcessor).Assembly` (`ProjectsServerServiceCollectionExtensions.cs:62`).
- **Resolution:** Label the snippet "target, after the AD-17 SDK writer gate".
- **Acceptance check:** The label is present.

### L4. Memlog evidence entries disagree on the `5f1d955b` CI failure

- **Severity:** Low · **Status:** CONFIRMED (memlog)
- **Anchor:** Implementation boundaries (line 52).
- **Evidence:** Memlog entry 98 says run 45430692159 "failed build-and-test, P1R replay and G-6". Entry 103 says it "failed at submodule initialization". The spine follows entry 103, but entry 103 is not marked as a correction of 98.
- **Resolution:** Append a memlog `(correction)` entry. Once M6 is applied, this leaves the spine entirely.
- **Acceptance check:** The memlog has an explicit correction entry.

---

## Checked and held up

- **DW-63 host shape.** `Program.cs` is `AddProjectsDomainService()` + `UseEventStoreDomainService()` + `MapProjectsHumanEndpoints()`. `AddProjectsDomainService` calls `AddEventStoreDomainService(typeof(ProjectsDomainProcessor).Assembly)` with the Server assembly, as line 487 states.
- **Retained v1 adapter.** It maps exactly 25 `/api/v1/projects` method-and-path pairs, the same at `5f1d955b` and HEAD, under `RequireAuthorization(EventStoreHttpHostSecurityExtensions.HumanPolicy)`. That matches AD-24 and the AD-20 human-policy clause.
- **Adapter write paths.** v1 commands go through `IProjectCommandSubmitter` and membership commands through `IProjectConversationAssignmentDirectory`. `ProjectReadAudit` writes through `IReadModelStore`. The proposal ledger is in-memory per replica (`InMemoryProjectProposalConfirmationIdempotencyLedger`), as AD-24 says.
- **Single writer.** Exactly one keyed `projects` `IDomainProcessor` is registered, with a matching keyed `IAsyncAggregateReplay` (`ProjectsServerServiceCollectionExtensions.cs:172-177`; `DomainName = "projects"`). That matches AD-17.
- **Query-response policy.** `ProjectsQueryResponsePolicy` gives 400 to `InvalidEnvelope`/`InvalidCursor` and `safe-denial` 404 on exactly the six named types, and a raw 404 for the others. The SDK resolves a single `IDomainQueryResponsePolicy` (`EventStoreDomainServiceExtensions.cs:446`). `IDomainQueryResponsePolicy` exists in EventStore source `208af26a`, which matches the "source mode only" Stack claim.
- **AD-14 seams.** Named `IAsyncDomainProjectionHandler`s are registered, and the single `IDomainProjectionHandler` is the identified legacy full replay.
- **AD-16 Contracts dependencies.** `Hexalith.Projects.Contracts` has no framework references and no FrontComposer, Fluxor, Dapr or Aspire dependency (`tools/release-packages.json`).
- **Shipping packages.** `tools/release-packages.json` lists five packages, including `Projects.ServiceDefaults`.
- **Stack pins.** `global.json` is `11.0.100` with `latestPatch`. The Builds gitlink is `bf8a28b4` at `280b1eed` and HEAD. The Builds catalog has Dapr runtime 1.19.2 / CLI 1.19.1, Toolkit `17.0.0-preview.1.261120-0454` and McpCli 3.0.0. Module.Cli 6.4.2 is in `Directory.Packages.props`, and the FrontComposer CLI tool is 6.0.1.
- **Links and sources.** All 32 relative links and all 20 frontmatter sources resolve, and the Section G anchor resolves.
- **PRD coverage.** FR-1..FR-25 and NFR-1..NFR-11 each appear in the Capability map.
- **AD-24 amendment.** It is consistent with AD-20, AD-6 and NFR-10's retirement criteria, apart from the post-cutover gap in H1.
- **AD-4 / AD-12 / AD-33 reconciliation.** The bounds agree: Administrator-only, original bindings, original-actor check at commit, one audited outcome.
- **AD-19 vs AD-32.** Denial and absence handling agree, and the bounded exception does not weaken AD-32.
- **SHA re-check.** The spine SHA-256 was re-checked unchanged at write time.
