# Hexalith.Projects architecture validation

November 24–25, 2026 · Standalone BMAD validation · Projects `31b35637` · spine `01b89a2f`

**Not ready to gate the first writer.** FAIL: the spine ratifies the shipped DW-63 host and maps every FR and NFR, but DW-63 acceptance is blocked by C1 and H1, and the Story 7.1 first writer and the AD-17 writer cutover are blocked by C1, C2 and twelve high findings (H2–H13).

Seven independent lenses reviewed the same final spine, and their 83 findings merge into 63. Two critical gaps remain: the shipped writer approves every command through a test-only validator, and a Folder binding can be installed without a current AD-35 claim. Most high findings ask the spine to define the writer cutover as one event and to say what the retained v1 routes do before and after it. Epic 6 read work is not blocked.

| Critical | High | Medium | Low | Lint findings |
| --- | --- | --- | --- | --- |
| 2 | 17 | 26 | 18 | 0 |

Dispositions: autofix changes spine wording only; discuss needs Jerome's decision; defer moves the item to the Deferred table with a revisit condition; route sends it to a story or DW owner and names the gate it blocks. Status marks (CONFIRMED, PLAUSIBLE) are the reviewers' own.

Sections: [Critical](#critical) · [High](#high) · [Medium](#medium) · [Low](#low) · [Gates](#what-blocks-which-gate) · [Checks](#checks) · [Disposition](#disposition) · [HTML report](validation-report-2026-11-24.html)

## Critical

### C1 · The shipped writer authorizes every command through a test-only allow-all validator, and no gate removes it

**Critical** · AD-20 allows allow-all implementations only in explicit test composition, and the Structural Seed itself records that the shipped host breaks that rule. On the shipped path, `AddProjectsDomainService` calls `AddProjectsServerRuntimeInfrastructure`, which replaces the deny-all defaults with `AllowingProjectEventStoreAuthorizationValidator` and `AllowingProjectDaprPolicyEvidenceProvider`, and that validator is the single writer's only per-command authorization check. The spine names no owner, gate or exit, and the memlog routes the fix to 8.8-P1 release evidence, after writers are live. So DW-63 can be accepted and Story 7.1 can qualify a writer that trusts every admitted command, while the spine never says where Projects actor/action/Project policy is enforced for a command that skips the v1 adapter gate, for example one sent to the EventStore human command API, whose default RBAC fails open on tokens without domain claims.

**Spine anchors.** [AD-20 Prevents and Rule · lines 296–297](ARCHITECTURE-SPINE.md) · [Structural Seed · line 487](ARCHITECTURE-SPINE.md) · [Writers boundary row · line 51](ARCHITECTURE-SPINE.md) · [AD-17, DW-63 gates Story 7.1 · line 266](ARCHITECTURE-SPINE.md) · [G-5 · line 557](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:60` calls `AddProjectsServerRuntimeInfrastructure` (defined at `:218`) on the shipped path.
- `ProjectsServerServiceCollectionExtensions.cs:169-170` registers deny-all defaults; `:240-243` removes them and registers the two `Allowing*` types.
- `src/Hexalith.Projects/Authorization/AllowingProjectEventStoreAuthorizationValidator.cs:22` returns `Allowed("eventstore_validator_test_allow")`. Its summary reads “Explicit dev/test opt-in”, and it ships in the packable `Hexalith.Projects` package.
- `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs:194-216`: the only authorization check before command dispatch.
- `references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs:74` and `.../Authorization/ClaimsRbacValidator.cs:40-58,70-82`: the human command API, whose default RBAC fails open without domain or permission claims (security lens).
- `.memlog.md:104` routes the fix to “the Projects Server owner and 8.8-P1 evidence”. The DW-63 spec has no match for “allowing” or “allow-all”. `epics.md:1694` gates Story 7.1 on “accepted DW-63”.

**Proposed resolution.** Make removal a gate, not a note. AD-20, or the line-51 first-writer gate list, should say: no shipped composition registers an `Allowing*` validator or evidence provider, and a production-capable host fails at startup if one is registered. Real or deny-by-default EventStore-authorization and Dapr-policy evidence must be in place before DW-63 acceptance, and at the latest before Story 7.1 first-writer qualification. Owner: the Projects Server owner, with G-5 supplying the evidence source. Add the enforcement point: Projects actor/action/Project policy is evaluated at governed admission, and the writer accepts a Projects command only with that admission's evidence, whatever the ingress. Say whether `Allowing*` types may stay in the packable `Hexalith.Projects` assembly.

**Acceptance check.** A composition test resolves the container built by `AddProjectsDomainService` and finds no `Allowing*` implementation of `IProjectEventStoreAuthorizationValidator` or `IProjectDaprPolicyEvidenceProvider`. Startup with one registered fails. A persisted test submits `ArchiveProject` through the EventStore command API with a token that lacks Folder manage, and sees a rejection with the stream unchanged. Both rows appear in the DW-63 (or Story 7.1 entry) evidence.

**Raised by.** [security](reviews/review-validate-security-2026-11-24.md) (SEC-01, Critical) · [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-6, High) · [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-2, High) · [rubric](reviews/review-validate-rubric-2026-11-24.md) (H3, High)

**Status.** CONFIRMED for the composition (all four lenses; the cited lines were re-read for this report). PLAUSIBLE for exploitation through deployed ingress, which depends on EventStore command-API exposure and IdP claims.

**Settled.** Severity: the security lens rated this Critical and the other three High. Kept Critical, because the writer's only authorization layer approves every command on the shipped path.

**Recommended disposition.** **route**. Projects Server owner. Block DW-63 acceptance. If Jerome prefers to keep DW-63's frozen scope, the same check moves to the Story 7.1 first-writer entry gate. No lens accepts any later point. **Gate:** DW-63 acceptance (latest: Story 7.1 first writer).

### C2 · A Folder binding can be installed without a current AD-35 claim

**Critical** · AD-35 governs how a task acquires the exclusive Folder claim, but no AD requires the `projects` aggregate to refuse a binding command that lacks a current claim receipt. AD-17 meanwhile asks for command equivalence with the current fold, which replaces an Active Project's Folder on a Boolean `ReplacementConfirmed` with no exclusivity check. After cutover, a v1 `PUT …/folder`, a restore or a direct Gateway command can therefore bind Folder F to Project P while Project Q holds F's claim. Separately, an in-flight activation command from an abandoned task can be processed after its claim generation was released and re-acquired, because neither the pure `Handle` nor EventStore (no caller-supplied expected version) can see that the generation is obsolete. Either way two Active Projects share one Folder, AD-3 breaks, and F's readers gain the other Project's context.

**Spine anchors.** [AD-3 · line 132](ARCHITECTURE-SPINE.md) · [AD-17 Rule · line 245](ARCHITECTURE-SPINE.md) · [AD-35 · lines 428–432](ARCHITECTURE-SPINE.md) · [AD-24 · line 321](ARCHITECTURE-SPINE.md) · [AD-5 · line 167](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs:300-315`: replacing a bound Folder needs only `ReplacementConfirmed`, with no cross-Project check (the adversarial lens cites `:267-330`).
- `src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs:156-164` forwards `replacementConfirmed` from v1.
- `references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs:212` accepts human-subject commands for any domain.
- EventStore `SubmitCommandRequest.cs:16,26` carries no expected version. `ProjectAggregate.cs:39-95` handles `CreateProject` against an empty state. `ProjectState.cs:36-48` tracks no version or claim generation.
- Intentional deltas that literal AD-17 equivalence would hide: `ProjectAggregate.cs:65-95` still emits `ProjectFolderCreationPending` (AD-22 stops it); `ProjectState.cs:57,60` caps 100 File and 100 Memory references separately (AD-15: 5,000 combined); `ProjectAggregate.cs:213-258` restores with no Folder check (AD-23 requires one).

**Proposed resolution.** Tighten AD-35 and AD-17. (1) From writer cutover, the `projects` aggregate rejects every command that establishes, replaces, restores or releases a Folder binding unless it carries an admitted task ID and the current claim receipt (Project, task/step, generation), whatever the ingress. (2) Fence abandoned reservations as a stream fact: before release, commit a terminal fence event (for example `ProjectActivationAbandoned{generation}`) to the reserved Project stream. `Handle` rejects later activation commands for that generation, a reserved ProjectId is never reused, and the fence event joins the AD-17 fold set. (3) AD-17 command equivalence covers historical replay and unchanged commands only. List the approved deltas (Folder-first create without Pending, the combined 5,000 cap, artifact-based replacement, restore Folder validity, claim acquisition and release), each with its own golden history, and make the first Folder-first write the AD-22 history boundary. (4) Only the platform workflow identity submits governed commands to the `projects` domain.

**Acceptance check.** At the cutover candidate, persisted tests send `SetProjectFolder`, `RestoreProject` and `CreateProject` (with a Folder and no receipt) through the v1 route and through the raw Gateway command endpoint. Each is rejected with no event appended. A two-replica test holds an activation command in flight, releases after the fence commits, lets P2 acquire F and then delivers the stale command: P1 is rejected and F has one Active holder. The writer-gate evidence lists each delta with its own golden case.

**Raised by.** [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-1, Critical) · [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-H1, High) · [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-M5, Medium)

**Status.** CONFIRMED (aggregate, submitter, Gateway and EventStore contract source; `ProjectAggregate.cs:300-315` re-read here). The interleavings are constructed.

**Recommended disposition.** **discuss**. Decision: where claim enforcement lives (an aggregate-side receipt and generation check, as proposed, or a platform primitive under G-1), and the approved list of AD-17 deltas. Decide together with M1. **Gate:** Story 7.1 first writer; AD-17 writer cutover.

## High

### H1 · CI cannot build any revision: “source mode only” hides a package-mode compile failure

**High** · The spine says the DW-63 Server “builds in source mode only” until EventStore publishes its query-response seam, which reads like a local inconvenience. CI always builds in package mode, so every push fails compilation with CS0246 on `IDomainQueryResponsePolicy`, and every downstream CI job (Aspire tests, performance, generated-artifact gates, managed E2E) is skipped. No CI test evidence can exist for DW-63 or any later revision. The obvious fixes do not work: published EventStore 5.5.2, the newest release, still lacks the type, and project references in CI are forbidden.

**Spine anchors.** [Writers row · line 51](ARCHITECTURE-SPINE.md) · [Stack, EventStore row · line 461](ARCHITECTURE-SPINE.md) · [Build and test convention · line 452](ARCHITECTURE-SPINE.md) · [AD-25 · line 327](ARCHITECTURE-SPINE.md)

**Evidence.**

- CI runs 45477603548 (`0db83209`), 45479334086 (`280b1eed`) and 45488087994 (`31b35637`, HEAD) fail at step “Build” with `ProjectsQueryResponsePolicy.cs(8,53): error CS0246` (technology lens, `gh run view`).
- `Directory.Build.props` sets `UseHexalithProjectReferences=false` when `CI=true`, and `RejectUnsafeHexalithProjectReferenceMode` fails CI builds that use sibling project references.
- In EventStore, `IDomainQueryResponsePolicy` first appears in untagged `208af26a` and is absent at tag `v5.5.2`; 5.5.2 was published on nuget.org on 2026-11-24.

**Proposed resolution.** Writers row and Stack EventStore row: “Published EventStore up to 5.5.2 lacks `IDomainQueryResponsePolicy`, which first appears in untagged `208af26a`. While the Server uses it, CI package-mode build-and-test fails (CS0246) on every revision and downstream CI jobs are skipped, so CI supplies no DW-63 acceptance evidence. The exit needs three steps: EventStore publishes a release after 5.5.2 that carries the type, Builds selects it, and Projects advances its Builds gitlink. Project references in CI remain forbidden.” Name the EventStore release owner as the dependency.

**Acceptance check.** The spine names 5.5.2 as lacking the seam and states that CI build-and-test stays red until the three-step exit. After the exit, a CI run on the new Builds gitlink passes the Build step with no CS0246.

**Raised by.** [technology](reviews/review-validate-technology-2026-11-24.md) (T-1, High)

**Status.** CONFIRMED (EventStore history and tags, nuget.org registration, CI job logs).

**Recommended disposition.** **route**. EventStore release owner, then the Builds owner, then Projects (the three-step exit). The spine sentence itself is an autofix. **Gate:** DW-63 acceptance.

### H2 · ChatBot Project grants contradict AD-3 and have no governing AD

**High** · AD-3 says Projects issues no Project-level grant, yet the shipped writer persists `GrantProjectChatBotAuthority` and `RevokeProjectChatBotAuthority` (“Issues an exact, expiring ChatBot project grant”), admitted on Tenants-owner and Parties evidence rather than Folders, and AD-19 half-ratifies the subsystem by naming `ResolveProjectChatBotAuthority`. A Story 7.1 team building the SDK aggregate from the spine's inventory may omit these commands, which breaks AD-17 replay of existing streams, while another team extends them citing AD-19. The authority query also tells an existing Project (200 with `allowed=false`, stream head and observation) from an absent one (404) before any Folder check, which is the denial-versus-absence oracle AD-19 forbids, and a grant can outlive a Folder revocation.

**Spine anchors.** [AD-3 · line 132](ARCHITECTURE-SPINE.md) · [AD-19 · line 289](ARCHITECTURE-SPINE.md) · [AD-17 · line 245](ARCHITECTURE-SPINE.md) · [AD-33 · lines 387–416](ARCHITECTURE-SPINE.md) · [Action classification · lines 496–507](ARCHITECTURE-SPINE.md) · [PRD §2.1 · line 37](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · [PRD A-3 · line 601](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)

**Evidence.**

- `src/Hexalith.Projects.Contracts/Commands/GrantProjectChatBotAuthority.cs:3` (re-read here); commit `b7ae7273` “feat(auth): persist scoped ChatBot project grants”.
- `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs:171,234`; `src/Hexalith.Projects/Aggregates/Project/ProjectState.cs:51-55`; `ProjectsRuntimeCatalogDeclarations.cs:19,32-33`; `docs/event-catalog.md:185-221`.
- `src/Hexalith.Projects.Server/Authorization/ProjectOwnerBootstrapVerifier.cs:23-26,43-46`: admission on Tenants-owner and Parties evidence.
- `src/Hexalith.Projects.Server/Queries/ProjectChatBotAuthorityQueryHandler.cs:61-65` returns `Forbidden` (mapped to 404) for an absent stream, and `:91-98` returns 200 with `Head`, `ObservationId` and `ObservedAt` for an existing Project without a grant (re-read here).
- `prd.md:37`: “Projects issues no Project-level grants of its own”. `prd.md:601` (A-3): add explicit grant/revoke FRs under a new ID.

**Proposed resolution.** Decide, then record the answer in AD-3 or a dedicated AD. Either (a) a bounded exception: the scoped, expiring ChatBot authority issuance is a delegated-operation record, not Project access. It is necessary but never sufficient: Folder authority is still required and Folder revocation wins immediately. Both commands and their events join the AD-17 replay and equivalence set, the AD-33 matrix and the classification table. Or (b) unapproved: freeze it to replay-only (events stay readable under AD-6/AD-22) and send it to a new PRD FR under A-3. Either way, `ResolveProjectChatBotAuthority` returns the same safe 404 for absent, denied and ungranted, with no head or observation, unless the delegated actor has current Folder read.

**Acceptance check.** AD-17 names the `ProjectChatBotAuthority*` events in the replay set. AD-3 no longer contradicts `ProjectsRuntimeCatalogDeclarations.Commands`. A handler test returns identical status and body for “Project exists, actor has no Folder read” and “Project absent”. A grant whose holder lost Folder read is denied at Chatbot.

**Raised by.** [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-1, High) · [security](reviews/review-validate-security-2026-11-24.md) (SEC-02, High)

**Status.** CONFIRMED (code and PRD). PLAUSIBLE for Chatbot consumer behavior; Chatbot source was not inspected.

**Recommended disposition.** **discuss**. Decision: sanction the ChatBot grant as a bounded AD-3 exception, or freeze it to replay-only and route it to a new PRD FR. **Gate:** Story 7.1 first writer (disposition); Epic 8 release (disclosure fix).

### H3 · Query and projection handlers live in the Server, and the spine's target home cannot hold them

**High** · AD-24 keeps `Projects.Server` “only” as a two-line host plus registration, and the Structural Seed puts handlers and projections in `Hexalith.Projects`. In the code, all ten query handlers, both named projections, the legacy full-replay projection, the Dapr read models, `ProjectReadAudit`, the authorization gate and the owner directories live in the Server, and `Hexalith.Projects` has none. The SDK seams they implement live in `Hexalith.EventStore.DomainService`, which carries a `Microsoft.AspNetCore.App` framework reference, so moving them into the packable domain package changes its dependency shape. New Epic 7 and Epic 8 handlers will land in two assemblies, and the target `AddEventStoreDomainService(typeof(ProjectAggregate).Assembly)` would discover only half of them.

**Spine anchors.** [AD-24 · line 321](ARCHITECTURE-SPINE.md) · [Structural Seed · lines 480, 482, 487](ARCHITECTURE-SPINE.md) · [Diagram Domain node · line 62](ARCHITECTURE-SPINE.md)

**Evidence.**

- 13 handler files under `src/Hexalith.Projects.Server/Queries/` and `src/Hexalith.Projects.Server/Projections/`, registered at `ProjectsServerServiceCollectionExtensions.cs:185-208,231-255`.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/*.csproj:14` (`FrameworkReference Microsoft.AspNetCore.App`); `IDomainQueryHandler.cs:18`; `IAsyncDomainProjectionHandler.cs:8`.
- `src/Hexalith.Projects/Hexalith.Projects.csproj` references only Contracts, and `tools/release-packages.json` lists no EventStore DomainService dependency for it.

**Proposed resolution.** Add one rule to AD-24 or the Seed that names the interim home (the Server, until the AD-17 writer cutover) and the target home for every `IDomainQueryHandler`, named projection, read-model adapter and owner directory, consistent with the dependency rule for `Hexalith.Projects`. Either a named assembly may depend on `Hexalith.EventStore.DomainService`, or the packable domain stays SDK-free and the non-packable Server keeps them. Correct AD-24's “only” to name the interim residents, and name the unit that moves the existing ones.

**Acceptance check.** A new handler's location follows from the spine alone. A build check (for example, no `IDomainQueryHandler` types outside the named assembly) enforces it.

**Raised by.** [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-3, High) · [rubric](reviews/review-validate-rubric-2026-11-24.md) (M8, Medium)

**Status.** CONFIRMED.

**Recommended disposition.** **discuss**. Decision: the target assembly for handlers and projections, and whether `Hexalith.Projects` may depend on `Hexalith.EventStore.DomainService`. **Gate:** Story 7.1 first writer.

### H4 · “The AD-17 writer cutover” is not one defined event, but three ADs switch on it

**High** · AD-19, AD-24 and AD-35 change behavior “at” or “from” the writer cutover. One keyed processor handles every Project command, so selecting the SDK writer selects it for archive, Folder set and links at once, while the governed workflows arrive one story at a time in 7.2–7.14 and Story 7.1 qualifies creation only. One team reads the cutover as global and closes every v1 mutation route, an unstated mutation freeze. Another reads it per slice, keeps v1 archive and `PUT …/folder` on direct submission and scopes claims to creation, which opens C2. A consumer assumes the AD-19 400 mapping applies from Story 7.1.

**Spine anchors.** [AD-17 · line 245](ARCHITECTURE-SPINE.md) · [AD-19 · line 289](ARCHITECTURE-SPINE.md) · [AD-24 · line 321](ARCHITECTURE-SPINE.md) · [AD-35 · line 432](ARCHITECTURE-SPINE.md) · [Writers row · line 51](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects.Server/ProjectsServerModule.cs:24-75` and `ProjectsServerServiceCollectionExtensions.cs:172-177`: one keyed processor and one matching replay (re-read here).
- AD-17 says both “atomically selecting one SDK writer” and “Before switching an affected writer slice”.
- `epics.md:1694`: “Before opening an affected writer slice, resolve its applicable legacy partial-record obligations under AD-17.”

**Proposed resolution.** Amend AD-17: “The writer cutover is one recorded event: the deployed selection of the SDK `projects` processor. At that event, together, every pre-cutover command ingress (including v1 direct submissions to the aggregate or to Conversations) is fenced, AD-35 claims are enforced for every key, and the AD-19 query policy changes (unless H12 decouples it). Workflows not yet delivered are slice enablements after the cutover. Until a slice is enabled, its v1 and canonical routes return a fixed nondisclosing `503` with `Retry`, no task and no command, and the release record lists the frozen routes.” Also state that cutover from the current processor is roll-forward only (L11).

**Acceptance check.** The cutover record names one deployment SHA and the full list of fenced ingress points. A test at that SHA calls each of the 15 non-GET v1 routes and gets governed admission or the declared frozen response.

**Raised by.** [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-2, High)

**Status.** CONFIRMED (spine and epics text, processor registration).

**Settled.** Route count: the adversarial lens wrote “18 v1 mutation routes”. The endpoint file maps 25 pairs, 10 GET and 15 non-GET (`ProjectsDomainServiceEndpoints.cs:84-532`, recounted here), so the acceptance check uses 15.

**Recommended disposition.** **discuss**. Decision: one global cutover with per-slice enablement and a declared mutation freeze, or per-slice cutovers each with its own ingress fence. **Gate:** Story 7.1 first writer; AD-17 writer cutover.

### H5 · Retained v1 confirmation routes accept Boolean confirmations from any human bearer and have no post-cutover outcome

**High** · AD-13 says there is no confirm API and AD-5 forbids Boolean confirmation, yet the retained adapter maps `POST /api/v1/projects/proposals/confirm` and `POST …/resolution/confirm`, carries a Boolean `replacementConfirmed` on `PUT …/folder`, and maps move, unlink, archive and restore. Today any human bearer accepted by the platform human policy can call them, with no interactive-session (A-7) or registered-adapter requirement, so an agent or script holding a user token bypasses AD-29 containment. AD-24 then says only that from cutover a v1 mutation goes through governed admission. Implementers can make the confirm routes consume an artifact (the confirm API AD-13 forbids), return `RequestPreview` (retiring v1 behavior without NFR-10 evidence), or keep direct submission (violating AD-24).

**Spine anchors.** [AD-24 · line 321](ARCHITECTURE-SPINE.md) · [AD-13 · line 217](ARCHITECTURE-SPINE.md) · [AD-5 · line 167](ARCHITECTURE-SPINE.md) · [AD-17 · line 262](ARCHITECTURE-SPINE.md) · [AD-20 · line 297](ARCHITECTURE-SPINE.md) · [AD-29 · line 353](ARCHITECTURE-SPINE.md) · [AD-33 · line 416](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:292,344,364,386,406,521,532` (re-read here).
- `src/Hexalith.Projects.Server/Queries/ConfirmProjectResolutionEndpoint.cs:70` and `ProposeNewProjectEndpoint.cs:205`: claim-permission gates only.
- `references/Hexalith.EventStore/.../EventStoreHttpHostSecurityExtensions.cs:53-69`: a bearer-only human policy with no session claim.
- `ConfirmProjectResolutionPayload` binds only a `ResolutionResult` discriminator. `.memlog.md:101` calls these shapes the Boolean confirmations AD-5 forbids.

**Proposed resolution.** Add to AD-24 a table of the 25 v1 pairs against {pre-cutover, post-cutover}. Proposed classes after cutover: task-only routes (no-Folder create, Setup) return `202` plus task; association and confirmation-required routes, including the Boolean flag, return `409` with `RequestPreview` in the v1 problem shape, no task, no command and one denial audit row. No v1 route ever consumes a Confirmation Artifact. Treat this as a v1 behavior change: record the consumer inventory and NFR-10 evidence before cutover, or freeze those routes. For the pre-cutover window, either restrict confirmation-required routes to the registered confidential Chatbot/Web adapter credential (the A-7 interim rule) or record an accepted risk with an owner.

**Acceptance check.** The spine, or a document it links, has the 25-row table. Contract tests assert that no v1 route consumes a Confirmation Artifact and that each post-cutover route returns its declared status with no effect. If the interim restriction is chosen, a valid human token without session or adapter credential is refused on every confirmation-required route. The cutover gate cites the NFR-10 evidence or the freeze.

**Raised by.** [rubric](reviews/review-validate-rubric-2026-11-24.md) (H1, High) · [security](reviews/review-validate-security-2026-11-24.md) (SEC-03, High) · [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-8, Medium)

**Status.** CONFIRMED (code, EventStore policy source and spine text).

**Recommended disposition.** **discuss**. Decisions: post-cutover v1 behavior (refusal with `RequestPreview` plus NFR-10 evidence, or a freeze), and the pre-cutover interim (adapter-only restriction, or an accepted risk with an owner). **Gate:** Story 7.1 first writer (interim; proposal confirmation shares the 7.1-P1 validator path); AD-17 writer cutover (route table).

### H6 · v1 create takes GUID-shaped or caller-chosen identities, and the post-cutover rule is undefined

**High** · AD-18 forbids GUID-shaped and caller-selected identities, but v1 create mints `Guid.NewGuid().ToString("N")` or accepts `body.ProjectId`, and takes the task and correlation IDs from headers. AD-31 calls the legacy request “name-only”, while the pinned 7.1-P1 legacy row accepts `{name}` with optional `projectId`, `description` and `setupMetadata`. After cutover, one implementer ignores a supplied `projectId`, so retained consumers polling their own ID get 404 and AD-31's “accepted throughout v1” breaks; another honours it and violates AD-18. Before cutover, create outcomes for a guessed existing ProjectId may reveal that it exists.

**Spine anchors.** [AD-18 · lines 282–283](ARCHITECTURE-SPINE.md) · [AD-31 · line 371](ARCHITECTURE-SPINE.md) · [AD-24 · line 321](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs:650-652` (re-read here) and `:554-566` (`X-Hexalith-Task-Id` and `X-Correlation-Id` from headers).
- `_bmad-output/implementation-artifacts/spec-7-1-p1-qualify-projects-creation-contracts.md`, Legacy row; the fixtures are pinned by commit `31b35637`.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:2785-2797`: the canonical request has no `projectId`, so every canonical create gets a GUID.

**Proposed resolution.** In AD-18, add a bounded pre-cutover exception: retained v1 create mints GUID-N or accepts the caller's `projectId`, and those IDs stay readable as persisted legacy IDs. Choose one post-cutover behavior for a supplied `projectId` (reject with `400 projectId`, or accept it as an idempotency alias) and for caller task and correlation IDs. Correct AD-31's “name-only” to the actual legacy field set. State that pre-cutover create outcomes for an existing foreign ProjectId are indistinguishable from other denials.

**Acceptance check.** For a legacy body with `projectId` after cutover, the spine yields exactly one status and one identity. AD-31's legacy field list matches the 7.1-P1 legacy fixture row.

**Raised by.** [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-4, High) · [security](reviews/review-validate-security-2026-11-24.md) (SEC-10, Low)

**Status.** CONFIRMED (code and fixture). PLAUSIBLE for the existence oracle, which depends on the status surface.

**Recommended disposition.** **discuss**. Decision: post-cutover handling of a caller-supplied `projectId` (reject, or alias). The AD-31 wording correction is an autofix. **Gate:** Story 7.1 first writer; AD-17 writer cutover.

### H7 · The retained adapter forwards raw human bearer tokens to EventStore, Conversations and Folders

**High** · AD-20 lists raw-token forwarding among what it prevents, but `EventStoreGatewayTokenForwardingHandler` attaches the caller's bearer token to outbound EventStore, Conversations and Folders calls. One token audience is then honored by four services. A token taken from any of them, or a lower-trust client of that audience, can act directly at EventStore (the open path in C1), Conversations or Folders with the user's full authority, and owners cannot tell Projects-mediated calls from direct ones, which weakens AD-26 attribution. The spine names no bounded exception or retirement trigger.

**Spine anchors.** [AD-20 Prevents and Rule · lines 296–297](ARCHITECTURE-SPINE.md) · [AD-24 · line 321](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects.Server/EventStoreGatewayTokenForwardingHandler.cs:17-22,39-50`.
- `ProjectsServerServiceCollectionExtensions.cs:256-261` (Conversations), `:263-268` and `:270-274` (Folders), `:281-288` (EventStore gateway).
- `src/Hexalith.Projects.Server/Authentication/ProjectsAuthenticationServiceCollectionExtensions.cs:88-90`: one inbound audience.

**Proposed resolution.** AD-20: “Projects never forwards an inbound bearer token. Outbound owner and EventStore calls use platform-issued, audience-scoped delegated credentials carrying the delegation identifier.” If the retained v1 adapter must keep forwarding until cutover, name that as a bounded exception with an owner, a distinct-audience requirement and a retirement trigger that falls before the first writer is exposed.

**Acceptance check.** A captured outbound request from the Release Server to each owner carries a callee-audience credential, never the inbound token bytes. The owner rejects a Projects-audience token presented directly.

**Raised by.** [security](reviews/review-validate-security-2026-11-24.md) (SEC-04, High)

**Status.** CONFIRMED (code). PLAUSIBLE for the shared-audience deployment consequence.

**Recommended disposition.** **discuss**. Decision: accept a bounded pre-cutover exception (owner, audience, retirement trigger), or replace forwarding before the first writer. **Gate:** Story 7.1 first writer; Epic 8 release (audience and delegation evidence).

### H8 · Multi-Tenant principals: v1 routes pick the Tenant by token claim order

**High** · The spine says Tenant comes from authenticated server context, and only the McpCli note says Tenant selection is envelope-owned. On v1 routes the accessor takes the first current-tenant claim, then the first `eventstore:tenant` claim, then `tenantId`, and the claims transformation expands a `tenants` array into several `eventstore:tenant` claims; the SDK query path uses the verified envelope Tenant instead. A consultant in Tenants A and B who creates on a v1 surface lands a confidential Project and Folder in whichever Tenant the IdP listed first, while the same session on the Gateway acts in the intended Tenant. AD-11's acting-session predicate is undefined for such sessions.

**Spine anchors.** [McpCli note · line 43](ARCHITECTURE-SPINE.md) · [AD-18 · line 283](ARCHITECTURE-SPINE.md) · [AD-11 · line 205](ARCHITECTURE-SPINE.md) · [Authentication convention · line 448](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects.Server/HttpContextProjectTenantContextAccessor.cs:46-48` (re-read here).
- `ProjectsClaimsTransformation.cs:58-71`; `ProjectAuthorizationGate.cs:429-437,593-601` (explicit header or query selection is rejected as `tenant_mismatch`); `Queries/VerifiedProjectQueryTenantContextAccessor.cs:9`.

**Proposed resolution.** AD-20 or AD-18: “Each request binds exactly one Tenant: the Gateway envelope Tenant verified against the principal's Tenant set, or, on retained v1 routes, a single current-tenant claim. A principal with more than one Tenant and no verified explicit selection is denied with the safe 404. First-claim selection is forbidden.”

**Acceptance check.** A token with tenants `[A,B]` and no current-tenant claim is denied on v1 create and list. With current-tenant `B`, both v1 and the Gateway act in B only, and a persisted check shows A untouched.

**Raised by.** [security](reviews/review-validate-security-2026-11-24.md) (SEC-05, High)

**Status.** CONFIRMED (code).

**Recommended disposition.** **route**. Story 7.1 owner for the code fix on the v1 Tenant accessor. The spine sentence follows from AD-18 and is an autofix. If Jerome treats denying multi-Tenant tokens on v1 as an NFR-10 behavior change, this becomes a discuss item. **Gate:** Story 7.1 first writer.

### H9 · Retained folds “fold” a Folder-first ProjectCreated by silently dropping the Folder

**High** · AD-17 requires the legacy read projection to fold every event the selected writer emits, and allows command rollback only while the old writer can replay every new event. `ProjectCreated` has no Folder field today, and every retained fold deserializes tolerantly and sets the Folder to null, so the literal test passes with no change. Under `LegacyRollback` or the v1 adapter, a Folder-first Project then reads as Active and folderless, which AD-3 forbids. After a command rollback, the legacy writer rehydrates it as unbound and accepts a Folder change with no Boolean, no Preview and no claim.

**Spine anchors.** [AD-17 · line 245](ARCHITECTURE-SPINE.md) · [AD-22 · line 309](ARCHITECTURE-SPINE.md) · [Event evolution convention · line 441](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs:61-75`; `ProjectDetailProjection.cs:110-125`; `ProjectReadProjectionHandler.cs:22-29,279`.
- `ProjectAggregate.cs:300-315`: the Boolean is required only when a Folder is already bound (re-read here).
- `references/Hexalith.EventStore/docs/concepts/event-versioning.md:22`: an added property is safe only if old readers can safely ignore it.
- `.memlog.md:102` records the intent: no folderless or pending rendering of Folder-first Projects.

**Proposed resolution.** Reword AD-17 so “fold” and “replay” mean state equivalence, not deserialization without error: for every event shape the selected writer emits, including every populated field, each retained fold produces the state the SDK writer's own fold produces. Name the retained folds: the legacy processor's aggregate fold and `ProjectProjectionHandler`, `ProjectDetailProjection`/`ProjectListProjection`, and every read model behind `LegacyRollback` or the v1 adapter. Add to AD-22 that legacy folds read the new `ProjectCreated` Folder binding before the first Folder-first write.

**Acceptance check.** A golden history with writer-emitted `ProjectCreated{Folder=F}`, `ProjectFolderSet` and `ProjectRestored` (and the C2 fence event) yields the same Folder binding and lifecycle in every named fold. A negative test shows the current folds fail it.

**Raised by.** [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-H2, High)

**Status.** CONFIRMED (source).

**Recommended disposition.** **autofix**. The memlog already records the intent; the Rule text needs to carry it. **Gate:** Story 7.1 first writer.

### H10 · Projects' own commit step has no stable idempotency key, expected version or conflict outcome

**High** · AD-12 gives each owner call a deterministic task/step idempotency key and expected version, but the Projects aggregate dedupes on the envelope MessageId, so a redelivery under a fresh ULID is a new command. A resumed Setup task can resend an older value after a newer task committed and overwrite it, and a duplicate archive returns `ProjectAlreadyArchived`, so the task may record `Rejected` for work that succeeded. For a genuine version conflict between concurrent tasks (an archive admitted at version N while Setup commits N+1), the spine gives no outcome, and teams will choose `Rejected`, a retry against a state the actor never previewed, or `NeedsAttention`.

**Spine anchors.** [AD-12 · line 211](ARCHITECTURE-SPINE.md) · [AD-15 · line 233](ARCHITECTURE-SPINE.md) · [AD-18 · line 283](ARCHITECTURE-SPINE.md) · [AD-4 · line 138](ARCHITECTURE-SPINE.md) · [AD-35 · line 430](ARCHITECTURE-SPINE.md) · [AD-5 and AD-13 · lines 167, 217](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs:364,422,475` pass `envelope.MessageId` as the idempotency key.
- `src/Hexalith.Projects/Aggregates/Project/ProjectStateApply.cs:50-57` and `ProjectAggregate.cs:53-58` dedupe on that key.

**Proposed resolution.** AD-12 or AD-15: Projects' own aggregate commit is a saga step like any owner call. Its idempotency key derives from task and step, is stable across replicas and redeliveries, and is independent of MessageId. Every task-driven Project command carries an expected Project version (defined in M2), and `Handle` rejects a mismatch as a non-mutating conflict that the task resolves through an authoritative status query. Add the outcome rule to AD-12: on conflict, re-evaluate from the last checkpoint. A confirmation-bound task whose Preview digest no longer matches ends `Rejected` with `RenewPreview`. A task-only task retries within the AD-27 budget. Neither becomes `NeedsAttention` unless the outcome is unknown.

**Acceptance check.** In a two-instance test, a stale resend after a newer commit leaves the newer value, and the stale task converges to its original outcome with no new event. A duplicate archive converges to `Succeeded`. Race fixtures (archive against Setup, link against link) show the declared terminal states and audit rows.

**Raised by.** [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-H4, High) · [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-10, Medium)

**Status.** CONFIRMED for the code paths. PLAUSIBLE for the interleavings.

**Recommended disposition.** **discuss**. The stable key follows directly from AD-12. Decision: the version-conflict outcome policy (the proposed split between confirmation-bound and task-only tasks). **Gate:** Story 7.1 first writer; AD-17 writer cutover.

### H11 · G-6 and CI run Dapr 1.19.1, which predates the 1.19.2 ETag and first-write fix

**High** · AD-15 serialization, AD-17 fencing and the AD-35 claim race all depend on state-store ETag and first-write concurrency. Dapr 1.19.2 fixes state stores (Redis, PostgreSQL and in-memory) that did not enforce ETags and first-write consistently: on Redis, a transaction with a failed ETag check could still apply its other operations. It also fixes three CVEs. G-6 and CI run 1.19.1 while the Builds catalog selects 1.19.2, so evidence produced now can pass or fail for reasons that change on the production runtime, and a deployment matching the qualified 1.19.1 can silently lose a concurrent update. The approved support-table exception covers SDK/runtime pairing, not this defect.

**Spine anchors.** [Stack, Dapr row · line 464](ARCHITECTURE-SPINE.md) · [AD-15 · line 233](ARCHITECTURE-SPINE.md) · [AD-17 · line 245](ARCHITECTURE-SPINE.md) · [AD-35 · lines 428–432](ARCHITECTURE-SPINE.md) · [G-6 · line 558](ARCHITECTURE-SPINE.md)

**Evidence.**

- `tools/qualification/g6-current-policy-2026-11-23-toolkit-preview.json:9` (`"daprRuntime": "1.19.1"`) and `.github/workflows/ci.yml:123-124,377-378` (re-read here).
- The Builds catalog sets `HexalithDaprRuntimeVersion` to `1.19.2`; dapr/dapr v1.19.2 release notes, 2026-11-19 (technology lens).
- `tools/qualification/run_g6_current.py:223,242`: the Redis and PostgreSQL lanes.

**Proposed resolution.** Dapr row: “Runtime 1.19.2 fixes ETag and first-write enforcement on Redis, PostgreSQL and in-memory stores, plus three CVEs. Evidence for AD-15 serialization, AD-17 fencing and the AD-35 claim must run on runtime 1.19.2 or later, unless the owner records an explicit risk acceptance on each affected evidence row. 1.19.1 results do not qualify those invariants.”

**Acceptance check.** The spine names the 1.19.2 fix and binds AD-15, AD-17 and AD-35 evidence to it. The next G-6 policy tuple uses 1.19.2 or later, or records the acceptance.

**Raised by.** [technology](reviews/review-validate-technology-2026-11-24.md) (T-2, High)

**Status.** CONFIRMED (release notes, repository pins, EventStore source).

**Recommended disposition.** **route**. G-6 policy owner for the policy tuple. `.github/workflows/**` is owned by Hexalith.CiCd, so the CI runtime change is proposed there rather than edited in Projects. The spine sentence is an autofix. **Gate:** Story 7.1 first writer (AD-35 claim-race proof); AD-17 writer cutover.

### H12 · The AD-19 404 exception ends at the writer cutover, but the v4 read packet hard-codes the 404

**High** · AD-19 keeps the DW-63 404 mapping for invalid input on four queries “until the AD-17 writer cutover”, then switches one policy to 400. The 77-cell v4 matrix asserts 404 for invalid cursors on context, explain and Conversation-start. At the cutover SHA, either the v4 packet fails and Primary Web falls back to `Unavailable` just as writes go live, or someone edits the matrix without a new normalization revision. Either way reads change at the writer migration, against AD-17's rule that reads switch independently of writer migration, and Gateway and Chatbot consumers that map 404 to “denied or absent” start receiving 400 with no notice. No unit owns that consumer impact.

**Spine anchors.** [AD-19 · line 289](ARCHITECTURE-SPINE.md) · [AD-17 read cutover · lines 262, 264](ARCHITECTURE-SPINE.md)

**Evidence.**

- `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.CutoverMatrix.cs:579-580`: `expected = operation is "context" or "explain" or "conversation-start" ? 404 : 400`, with legacy `BadRequest` (re-read here).
- `src/Hexalith.Projects.Server/Queries/ProjectsQueryResponsePolicy.cs:21-30`.
- DW-63 spec line 38: the Gateway's `DaprDomainQueryInvoker` maps receiver 404/403 to safe denial.

**Proposed resolution.** Tie the end of the exception to a read event: “The AD-19 exception ends with a new normalization revision (v5) that requalifies the 77 cells and records the consumer notice. It is independent of the writer cutover.” Name the unit that performs the 404-to-400 switch, with Gateway and Chatbot compatibility evidence. List the legacy-400 / supported-404 invalid-input pair as an approved pair under v4, since line 264 admits only cause-specific 503/200 pairs.

**Acceptance check.** The v4 approved-pair list includes the invalid-cursor pair. The cutover SHA either still returns 404 for the three reads or ships with a passing v5 packet. The switch has a named story with consumer evidence.

**Raised by.** [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-3, High) · [rubric](reviews/review-validate-rubric-2026-11-24.md) (M1, Medium)

**Status.** CONFIRMED (matrix and policy source).

**Recommended disposition.** **discuss**. Decision: decouple the end of the exception from the writer cutover (a v5 read revision), or keep the coupling and require a passing v5 packet at the cutover SHA. **Gate:** AD-17 writer cutover.

### H13 · Aggregate snapshots are outside writer selection and rollback

**High** · AD-17 says writer selection and rollback replace the processor and its replay together, but the spine never mentions aggregate snapshots. EventStore stores a snapshot as opaque state with only an envelope version, the SDK writer needs a different state type (positional `ProjectState` has no parameterless constructor), and the SDK rehydrator maps snapshot JSON by property name, silently skipping missing names. At cutover a bound Active Project can rehydrate as unbound or at version 0, and after rollback the legacy processor drops the SDK state's extra members. Event-only golden histories cannot detect either.

**Spine anchors.** [AD-17 · line 245](ARCHITECTURE-SPINE.md)

**Evidence.**

- `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/SnapshotRecord.cs:20-29`; `EventStoreAggregate.cs:25` (`where TState : class, new()`); `DomainProcessorStateRehydrator.cs:230-250`.
- `src/Hexalith.Projects/Aggregates/Project/ProjectState.cs:36`; `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs:901-914,957-968`.
- DW-63 spec line 48 compares nested snapshots for the legacy host only.

**Proposed resolution.** Add to AD-17: “Writer selection and rollback also invalidate or re-key aggregate snapshots. A processor ignores, and re-folds from events, any snapshot it did not write under its own declared state schema. Equivalence evidence includes rehydration from the other writer's snapshot.”

**Acceptance check.** A persisted test snapshots a Project under the legacy processor, selects the SDK writer and issues a command; the resulting state equals a pure event re-fold. The same holds after rollback.

**Raised by.** [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-H3, High)

**Status.** CONFIRMED (EventStore and Projects source).

**Recommended disposition.** **autofix**. One rule; no competing option was raised. **Gate:** AD-17 writer cutover.

### H14 · The Projects-owned selection and confirmation component has no home and no credential issuer

**High** · AD-5, AD-13 and AD-20 put Selection Evidence minting and confirmation consumption inside “the Projects-owned component”, which uses “its own session-bound credential unavailable to the host adapter”. The spine never says which package holds that component (AD-24 retires Projects-owned UI runtime projects, and `Projects.UI.Contracts` is descriptors-only), how FrontComposer hosts it, or who issues its credential. PRD A-6 says “a Projects-issued credential”, which sits uneasily with AD-28, where the platform owns identity, KMS and secrets. Stories 7.11, 7.12 and 7.16 can each place the component differently and invent different credentials, and the A-6 fallback (until the component exists on a surface, additive links and initial Folder set there stay confirmation-required) is not carried.

**Spine anchors.** [AD-5 · line 167](ARCHITECTURE-SPINE.md) · [AD-13 · line 217](ARCHITECTURE-SPINE.md) · [AD-20 · line 297](ARCHITECTURE-SPINE.md) · [AD-24 · line 321](ARCHITECTURE-SPINE.md) · [AD-16 and Seed · lines 239, 481](ARCHITECTURE-SPINE.md) · [AD-28 · line 347](ARCHITECTURE-SPINE.md) · [Capability map FR-25 · line 575](ARCHITECTURE-SPINE.md) · [PRD A-6 · line 604](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)

**Evidence.**

- `prd.md:604` (A-6, owner Winston), re-read here; `addendum.md:41,146`; `epics.md:2153` (Story 7.16: “the component authenticates with a Projects-issued credential”).
- The spine contains no reference to A-6 (grep).

**Proposed resolution.** Add a Structural Seed row for the interactive component: package name, packaging, FrontComposer hosting contract and host repositories. Decide the credential issuer, for example “the platform identity service (G-5) issues a component-scoped, session-bound credential; Projects holds no signing keys”, and reconcile the PRD A-6 wording with it. Cite A-6 and carry its fallback. If this stays undecided, record an open item owned by the architect and gate Stories 7.11, 7.12 and 7.16 on it.

**Acceptance check.** The spine names the artifact and its credential issuer and cites A-6. Story 7.16's entry gate references that row. AD-24's retirement list and the `UI.Contracts` definition no longer exclude the component.

**Raised by.** [rubric](reviews/review-validate-rubric-2026-11-24.md) (H2, High) · [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-10 (A-6 part), Low)

**Status.** CONFIRMED (spine against PRD, addendum and epics).

**Settled.** Contradiction settled. The PRD lens marked A-6 as carried by AD-5, AD-13 and AD-20, while its F-10 and the rubric lens said A-6 is not carried. Reading spine lines 167, 217 and 297 against `prd.md:604`: the issuer rule itself is carried (the Projects-owned component mints Selection Evidence with a session-bound credential), so the PRD lens is right about rule coverage. The rubric lens is right that packaging, hosting contract and credential issuer are undecided. Both agree that the A-6 citation and fallback are missing. The finding stays High for the undecided home and issuer.

**Recommended disposition.** **discuss**. Decision: the component's package and hosting contract, and its credential issuer (platform G-5, or Projects-issued as A-6 says). **Gate:** Stories 7.11, 7.12 and 7.16 entry.

### H15 · AD-29's four-read inventory is not enforced, and two preview operations are already decorated

**High** · AD-29 treats the release inventory as coverage measurement: a cataloged operation without an inventory row only produces a diagnostic. Under McpCli policy, decorating a Contracts type is the owner's consent to expose it without human confirmation. `GetProjectAuditQuery` (`audit`) and `GetProjectTaskStatusQuery` (`task-status`) already carry `[HexalithQuery]` from the blocked Story 8.1 preview. The next Contracts release enrolled for any fix would therefore give agents FR-21 audit and task status, flagged only by a report line, although the spine puts audit and task control outside the four-read inventory.

**Spine anchors.** [AD-29 · lines 353, 355](ARCHITECTURE-SPINE.md) · [McpCli note · line 43](ARCHITECTURE-SPINE.md) · [AD-16 · line 239](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects.Contracts/Queries/GetProjectAuditQuery.cs:10` and `GetProjectTaskStatusQuery.cs:6` (re-read here); commit `95f2984c`.
- `references/Hexalith.McpCli/src/Hexalith.McpCli.Abstractions/HexalithQueryAttribute.cs:4-6`: decoration is consent. `references/Hexalith.McpCli/CLAUDE.md:84`: an operation no inventory row mentions is reported, never failed.

**Proposed resolution.** Tighten AD-29: “Decoration is enrollment. In every Contracts version, the set of `[HexalithQuery]` and `[HexalithCommand]` types equals the approved AD-29 inventory for that version, and a Projects-owned test fails the build on any difference. Adding an operation needs an AD-29 inventory amendment and G-3 evidence before the decoration merges. Preview operations stay undecorated.” This is a build-time check. The Gateway remains the only runtime authorization, as the November 20 report's M6 required.

**Acceptance check.** A Contracts reflection test asserts exactly the approved operation names. It fails on the current tree until the two preview decorations are removed or approved.

**Raised by.** [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-4, High)

**Status.** CONFIRMED (decorations and McpCli policy). Whether discovery is automatic depends on Operation Shape vector coverage.

**Recommended disposition.** **discuss**. Decision: remove the `audit` and `task-status` decorations until Story 8.1 is approved, or approve them into the inventory with G-3 evidence. **Gate:** Next Projects.Contracts publication (G-3 enrollment); at the latest Epic 8 release.

### H16 · Durable inspection audit is primary state in the rebuildable read-model store

**High** · AD-24 lets the adapter write durable state “only as the AD-33 inspection audit through the platform read-model store”, while AD-14 makes that store's views rebuildable and AD-26 requires at least 365 days of audit retention. `ProjectReadAudit` writes inspection facts and audit-index pages to the state store with no event appended, so replay cannot reproduce them. A Story 8.10 generation rebuild either fails its replay-parity check or drops audit rows AD-26 must keep, and because the index keys are not generation-scoped, a “fresh” generation either shares them or loses them. The code matches AD-24, as the rubric and brownfield lenses confirmed; the conflict is between AD-24 and AD-14/AD-26.

**Spine anchors.** [AD-24 · line 321](ARCHITECTURE-SPINE.md) · [AD-14 · line 225](ARCHITECTURE-SPINE.md) · [AD-26 · line 335](ARCHITECTURE-SPINE.md) · [AD-17, Story 8.10 rebuild · line 266](ARCHITECTURE-SPINE.md)

**Evidence.**

- `src/Hexalith.Projects.Server/Queries/ProjectReadAudit.cs:14,34-36,122`; `ProjectReadModelKeys.cs:7`.
- `ProjectOperatorQueryExecutor.cs:65,87` merges these facts with replayed history.
- `epics.md:2888`: Story 8.10 verifies list, detail and audit against authoritative replay.

**Proposed resolution.** Either (a) every durable audit fact, including inspection, is appended to a platform-owned audit record stream with its own retention, and audit views are rebuildable projections of it; or (b) record an explicit carve-out: inspection-audit keys are excluded from rebuilds and generation switches, carry 365-day retention, and are verified by count instead of replay.

**Acceptance check.** The 8.10 rebuild fixture holds inspection facts. After rebuild and switch, the audit query returns the same facts, and replay parity (or, under the carve-out, the count check) passes.

**Raised by.** [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-5, High)

**Status.** CONFIRMED (source and epics).

**Settled.** Not a contradiction: the rubric and brownfield lenses list the audit write as conforming to AD-24, and the adversarial lens agrees that it does. Its finding is that AD-24 conflicts with AD-14 and AD-26.

**Recommended disposition.** **discuss**. Decision: a platform audit stream, or the carve-out. **Gate:** Epic 8 release (Stories 8.1 and 8.10).

### H17 · Supported context reads skip request-time Folder authorization (DW-85), and the spine omits it

**High** · The boundary row says all Epic 6 stories are done, and AD-14 and AD-20 require Folder evidence no older than the interactive dependency timeout. The approved November 22 sprint change records that supported Get and Explain read Project-owned persisted detail with zero owner calls, so a Folder revocation reaches them only when that evidence refreshes; supported Refresh and the Reference Trust Index are open under DW-85. A user whose Folder read was revoked can keep receiving Setup bodies and references on the Supported route. Story 8.8/8.11 reviewers and the Chatbot adoption owner who read the spine would assume the freshness rule already holds.

**Spine anchors.** [Read features row · line 50](ARCHITECTURE-SPINE.md) · [AD-14 · line 225](ARCHITECTURE-SPINE.md) · [AD-20 · line 297](ARCHITECTURE-SPINE.md) · [AD-32 · lines 377, 381](ARCHITECTURE-SPINE.md) · [Deferred RTI row · line 594](ARCHITECTURE-SPINE.md) · [PRD NFR-1 · line 544](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · [PRD FR-16 · line 429](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · [PRD asOf · line 178](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md)

**Evidence.**

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-22.md:48-52,70` and `_bmad-output/implementation-artifacts/deferred-work.md:772` (DW-85), re-read here. The spine never mentions DW-85.
- Story 6.3 spec line 708: “Get/Explain make zero owner calls.”
- `prd.md:544` (NFR-1: authorization is recomputed at every decision), `prd.md:429` (FR-16) and `prd.md:178` (`asOf`).

**Proposed resolution.** Read features row: “Supported Get, Explain and Conversation-start read Project-owned persisted evidence with zero owner calls. Supported Refresh and the Reference Trust Index are open under DW-85.” AD-14, a bounded and named exception: “Until DW-85 closes, Folder revocation reaches supported context only when persisted evidence refreshes. This does not satisfy AD-20 freshness or NFR-1. Production Supported selection and external Chatbot adoption require DW-85 closure or an approved, time-bounded revocation-lag bound.” Point the Deferred RTI row at DW-85, and say whether `asOf` is the computation time (as in the PRD) or the evidence cutoff.

**Acceptance check.** The spine names DW-85 in the boundary row and in AD-14. The fr-16, fr-17 and fr-18 evidence rows cannot record `passed` for supported reads while DW-85 is open. A revocation-then-context test on the supported route returns `Denied` or `Unavailable` within the AD-20 bound.

**Raised by.** [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-1, High)

**Status.** CONFIRMED (proposal, ledger, Story 6.3 spec and spine).

**Recommended disposition.** **autofix**. Records a scope change Jerome already approved on November 22. Delivering DW-85 stays with its owner. **Gate:** Epic 8 release (production Supported selection and Chatbot adoption).

## Medium

### M1 · The AD-35 claim has no home, generation definition or terminal-state release rule

AD-35 says Projects serializes claim acquisition by `(TenantId, FolderId)` and records a claim generation in a durable receipt, but the Structural Seed lists no claim stream, aggregate or record, AD-1 forbids Projects-owned persistence outside EventStore, and AD-17 and AD-18 speak of a single `Tenant/projects/ProjectId` aggregate. Story 7.1 can build the claim as a FolderId-keyed aggregate, a Dapr state lock or a platform primitive, and Stories 7.6, 7.14 and 7.15 then inherit or re-invent it. There is also no rule for a task that holds an uncommitted claim and ends `Rejected`, `Failed` or `Cancelled`: one reading of AD-4 and AD-12 forbids any release, so the Folder can never be claimed again, while another releases at once without the C2 fence. Nor does the spine say which side wins when a claim and a Project history disagree.

**Proposed resolution.** Seed the claim, for example: “a second Projects aggregate type `project-folder-claim` keyed `Tenant/project-folder-claims/FolderId` in the same DomainService; its generation is its stream sequence; the receipt ID is a platform ULID; Project histories are authoritative for binding, and a disagreeing claim is closed and reconciled, never auto-released.” Amend the AD-17 and AD-18 wording to admit it, or add the claim to G-1 if it is a platform primitive. State that claim acquisition is not the irreversible checkpoint, and that every terminal transition of a task holding an uncommitted claim first runs the fenced release as a Projects-internal step, not an owner call. If the fence cannot be established, the task stays `NeedsAttention`.

**Anchors.** [AD-35 · line 428](ARCHITECTURE-SPINE.md) · [AD-18 · line 283](ARCHITECTURE-SPINE.md) · [AD-17 · line 245](ARCHITECTURE-SPINE.md) · [AD-4 · line 138](ARCHITECTURE-SPINE.md) · [AD-12 · line 211](ARCHITECTURE-SPINE.md) · [Structural Seed · lines 475–494](ARCHITECTURE-SPINE.md) · **Raised by.** [rubric](reviews/review-validate-rubric-2026-11-24.md) (M2, Medium) · [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-M1, Medium) · [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-M2, Medium) · **Status.** CONFIRMED (absence); PLAUSIBLE (divergence). · **Disposition.** **discuss**: Claim substrate and owner; decide with C2. · **Gate.** Story 7.1 first writer.

### M2 · “Project version” is undefined, and rejection events advance the stream sequence

AD-5, AD-13, AD-32 and AD-35 bind Previews, Confirmation Artifacts, `projectVersion` and expected-version commits to a Project version that no AD defines. EventStore persists rejection results as ordinary stream events (`ProjectsDomainProcessor.cs:54-60`), and the supported detail projection advances its sequence on every event, including rejections (`ProjectReadProjectionHandler.cs:329-331`). If version means stream sequence, any refused command, such as an unauthorized archive attempt, invalidates every outstanding Preview and artifact for that Project and makes every expected-version commit conflict. That is a cheap denial of progress, and implementers will bind different numbers.

**Proposed resolution.** Define the Project version once, in AD-15 or the Conventions: the count of state-changing events, or the sequence of the last one, excluding `IRejectionEvent`. Use that value for `projectVersion`, Preview and artifact binding, and expected-version commits.

**Anchors.** [AD-5 · line 167](ARCHITECTURE-SPINE.md) · [AD-13 · line 217](ARCHITECTURE-SPINE.md) · [AD-32 · line 377](ARCHITECTURE-SPINE.md) · [AD-35 · line 430](ARCHITECTURE-SPINE.md) · [AD-17 · line 264](ARCHITECTURE-SPINE.md) · **Raised by.** [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-M3, Medium) · **Status.** CONFIRMED (rejections are persisted and advance the sequence); PLAUSIBLE (impact). · **Disposition.** **discuss**: Choose the Project version definition. · **Gate.** Story 7.1 first writer.

### M3 · No rule rechecks the original actor's authority while a task runs

AD-4 rechecks the original actor only from `NeedsAttention`, AD-13 binds authorization evidence at admission, and AD-20 requires fresh evidence in the immutable context. Nothing covers revocation between admission and commit for `Running` or `WaitingForDependency`, or says which credential later owner steps use once the interactive session ends. A creation task can resume after a retry window and commit `ProjectCreated` for a user who has since lost Tenant membership or Folder manage.

**Proposed resolution.** AD-4 or AD-12: before the irreversible checkpoint and at each Project commit, re-evaluate the original actor's current Tenant membership and action authority with the reconciliation predicate. A confirmed denial goes to `Rejected`, or to `NeedsAttention` under AD-8 when a Folder exists, with one audit outcome. An unavailable check means `WaitingForDependency`, never a commit. Owner steps use per-step platform-issued delegated credentials bounded by the task, never the interactive token.

**Anchors.** [AD-4 · line 138](ARCHITECTURE-SPINE.md) · [AD-12 · line 211](ARCHITECTURE-SPINE.md) · [AD-13 · line 217](ARCHITECTURE-SPINE.md) · [AD-20 · line 297](ARCHITECTURE-SPINE.md) · [G-5 · line 557](ARCHITECTURE-SPINE.md) · **Raised by.** [security](reviews/review-validate-security-2026-11-24.md) (SEC-06, Medium) · **Status.** PLAUSIBLE (design gap; the G-1 engine does not exist yet). · **Disposition.** **discuss**: Terminal state on mid-task revocation; credential for later owner steps. · **Gate.** Story 7.1 first writer.

### M4 · AD-31 never names who assigns metadataClass or what it governs

AD-31 says the class is “assigned by authenticated integration policy and never inferred from user text”, but v1 create takes `projectMetadata.metadataClass` from the caller's body (`ProjectsDomainServiceEndpoints.cs:621-625`), the resulting `CreateProject` carries no classification (`:662-671`), and the 7.1-P1 matrix says the classification is not persisted. A human or agent can label credential-bearing metadata `public_metadata`, Chatbot, Web and the Durable Task writer can each derive the class differently, and downstream disclosure rules cannot depend on a value that is never stored.

**Proposed resolution.** Name the assigning authority in AD-31, for example the registered integration adapter's workload identity under platform policy, with any other principal's value rejected with `400`. State whether the class is persisted in the creation event and which disclosure rules consume it, or say plainly “validated only in v1; grants and restricts nothing”.

**Anchors.** [AD-31 · line 371](ARCHITECTURE-SPINE.md) · **Raised by.** [security](reviews/review-validate-security-2026-11-24.md) (SEC-07, Medium) · **Status.** CONFIRMED (code and 7.1-P1 spec). · **Disposition.** **discuss**: Assigning principal and persistence of the class. · **Gate.** Story 7.1 first writer.

### M5 · No system of record for Tenant-role and session-mode evidence

AD-33 says a session exercises either Project User or Tenant-role authority, and AD-10 and G-2 rely on owners recognizing Tenant roles, but the code establishes Tenant and role evidence from three sources: JWT permission claims (`HttpContextProjectTenantContextAccessor.cs:89-90`), a Projects tenant-access projection (`src/Hexalith.Projects/Authorization/TenantAccessAuthorizer.cs:15`) and the Hexalith.Tenants owner query (`ProjectOwnerBootstrapVerifier.cs:23-26`). Projects can treat a principal as Tenant Project Administrator from a cached projection while Conversations or Folders deny it, so a Preview promises an outcome the owners refuse, and a dual-capable Web session can combine both authorities against AD-33.

**Proposed resolution.** Name the Tenant-role system of record. Add versioned Tenant-role evidence and the session mode to the AD-20 immutable context, fixed at admission and passed to every owner. State that any Projects-side projection is a freshness-bounded cache, never mutation authority.

**Anchors.** [AD-33 · line 387](ARCHITECTURE-SPINE.md) · [AD-10 · line 199](ARCHITECTURE-SPINE.md) · [AD-20 · line 297](ARCHITECTURE-SPINE.md) · [G-2 · line 554](ARCHITECTURE-SPINE.md) · **Raised by.** [security](reviews/review-validate-security-2026-11-24.md) (SEC-08, Medium) · **Status.** CONFIRMED (three sources in code); PLAUSIBLE (owner disagreement). · **Disposition.** **discuss**: Name the Tenant-role system of record (likely G-5). · **Gate.** Story 7.1 first writer; Epic 8 release.

### M6 · Retained proposal confirmation parses before it authorizes, and the promised DW entry does not exist

AD-31 says authorization precedes protected metadata parsing, but `POST /api/v1/projects/proposals/confirm` reads the body (`ProposeNewProjectEndpoint.cs:191`) and runs `TryValidateConfirmRequest` (`:199`) before `AuthorizeCreateAsync` (`:205`), so an unauthorized caller gets validation feedback, including the classification `rejectedField`. The 7.1-P1 spec (decision Q2=A) routes the fix to a DW entry for Story 7.12, but `deferred-work.md` has no such entry (grep, this report) and the spine records no exception, so Story 7.12 will not pick it up.

**Proposed resolution.** Write the DW entry (route, deviation, owner Story 7.12) before 7.1-P1 acceptance, and add a one-line bounded exception to AD-31 naming the route and the closing story. Alternatively, fix the order so the endpoint authorizes before reading the body.

**Anchors.** [AD-31 · line 371](ARCHITECTURE-SPINE.md) · [AD-24 · line 321](ARCHITECTURE-SPINE.md) · **Raised by.** [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-5, Medium) · [security](reviews/review-validate-security-2026-11-24.md) (SEC-12, Low) · **Status.** CONFIRMED (endpoint order and ledger absence re-checked here). · **Disposition.** **route**: New DW entry for the Story 7.12 owner. · **Gate.** Story 7.1 first writer (7.1-P1 acceptance).

### M7 · Idempotency has three layers with different scopes, and nothing carries keys across the cutover

AD-5 scopes admission idempotency to `(Tenant, actor, operation, key)` for at least 30 days. Today the v1 submitter sends the caller's `Idempotency-Key` as the EventStore `MessageId` (`EventStoreProjectCommandSubmitter.cs:54,301`), which EventStore documents as identity rather than an idempotency key, with a 24-hour replay tier, and the aggregate keeps a third per-stream copy (`ProjectAggregate.cs:53`). A Chatbot v1 create that loses its reply just before cutover and retries afterwards finds no AD-5 record and admits a second Project.

**Proposed resolution.** State that the platform AD-5 admission record is the only equivalence authority and aggregate fingerprints are defence in depth. At cutover, import every v1 key seen in the previous 30 days as an AD-5 record that resolves to its original outcome, or fence it to the AD-19 nondisclosing `409`. Tie the caller-`projectId` rule to H6.

**Anchors.** [AD-5 · line 167](ARCHITECTURE-SPINE.md) · [AD-18 · line 283](ARCHITECTURE-SPINE.md) · [AD-17 · line 245](ARCHITECTURE-SPINE.md) · [AD-24 · line 321](ARCHITECTURE-SPINE.md) · **Raised by.** [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-7, Medium) · **Status.** PLAUSIBLE (code paths confirmed; cross-aggregate MessageId deduplication in EventStore 5.5.1 not verified). · **Disposition.** **route**: AD-17 cutover inventory owner. · **Gate.** AD-17 writer cutover.

### M8 · Conflicting legacy incumbents stay Active and context-usable

AD-35 keeps a key closed to every claimant while its history is incomplete or its holders conflict. The legacy writer has no cross-Project uniqueness check and legacy restore has no Folder check (`ProjectAggregate.cs:213-258,267-333`), so two Active legacy Projects on one Folder are possible. After cutover both stay Active, listed and usable as context for the Folder's readers, and AD-3 stays broken for an unbounded time.

**Proposed resolution.** In AD-35 or AD-17, quarantine every Project on a conflicting key (`Unavailable`, no context), like the AD-3 rule for folderless legacy Projects, and release it only through the FR-8 Administrator binding workflow (Story 7.6).

**Anchors.** [AD-35 · line 432](ARCHITECTURE-SPINE.md) · [AD-3 · line 132](ARCHITECTURE-SPINE.md) · **Raised by.** [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-M4, Medium) · **Status.** CONFIRMED (no legacy guard); PLAUSIBLE (such data exists). · **Disposition.** **discuss**: Quarantine conflicting legacy holders at cutover, or not. · **Gate.** AD-17 writer cutover.

### M9 · FR-3 and FR-19 Setup-policy invariants have no carrying AD

The capability map cites AD-15, AD-16, AD-18 and the conventions for FR-3 and FR-19, but none of them states the PRD rules: Setup source policy may name only existing Context References and never introduces one; a Setup change never widens an admitted Conversation-start or context snapshot; and FR-19's secret classes (credential-shaped tokens, PEM blocks, credentialed connection strings, secret-reference formats), unrestricted paths and foreign payloads are rejected. AD-31's single-validator rule covers creation metadata class only. At cutover the SDK writer and the retained v1 adapter can validate Setup differently, and Setup that names an unlinked File can bypass FR-9's gates.

**Proposed resolution.** Add to AD-15 or a convention row: “Setup source policy names only existing Context References and never introduces one. A Setup change yields a new `projectVersion` and never widens an admitted snapshot. One shared Setup validator in `Hexalith.Projects` (FR-19 secret classes, unrestricted paths, foreign payloads, control characters) runs before submission on the v1 adapter, the SDK writer and proposal confirmation.” Fix the line-566 citation.

**Anchors.** [Capability map · line 566](ARCHITECTURE-SPINE.md) · [AD-15 · line 233](ARCHITECTURE-SPINE.md) · [AD-31 · line 371](ARCHITECTURE-SPINE.md) · [PRD · lines 151, 245, 248, 467](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · **Raised by.** [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-4, Medium) · [rubric](reviews/review-validate-rubric-2026-11-24.md) (L1 (FR-3/FR-19 part), Low) · **Status.** CONFIRMED. · **Disposition.** **autofix**: Restate the PRD Setup rules. · **Gate.** AD-17 writer cutover.

### M10 · AD-19's “same safe 404” wording does not match receiver bodies

For the four excepted queries the policy returns `NotFound` with the handler's own body: `safe-denial` for denial, `InvalidEnvelope` for bad paging after authorization, and `Forbidden` for every Chatbot-authority failure (`ProjectsQueryResponsePolicy.cs:21-25`; `GetProjectContextQueryHandler.cs:37,45,48`). Only the Gateway normalizes them to one safe 404 (`DaprDomainQueryInvoker.cs:95`). A test author who reads “the same safe 404 as denial” literally asserts identical receiver bodies and fails DW-63, or an implementer “fixes” the bodies and breaks the frozen DW-63 row. The body set lives only in memlog entry 107. Severity: the rubric lens rated this Medium and the brownfield and security lenses Low; it stays Medium because a literal reading breaks a frozen DW-63 expectation.

**Proposed resolution.** Reword: “status `404`; the receiver body is exactly one of `safe-denial` (denial, absence or unparseable target), `InvalidEnvelope` (only after successful authorization) or `Forbidden` (Chatbot authority, see H2), with no protected detail; the Gateway returns the same safe 404 for all.”

**Anchors.** [AD-19 · line 289](ARCHITECTURE-SPINE.md) · **Raised by.** [rubric](reviews/review-validate-rubric-2026-11-24.md) (M1 (wording part), Medium) · [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-9, Low) · [security](reviews/review-validate-security-2026-11-24.md) (SEC-09, Low) · **Status.** CONFIRMED. · **Disposition.** **autofix**: Reword to the actual body set. · **Gate.** None.

### M11 · The AD-19 exception is coded as a deny-list of six types

AD-19 names exactly four excepted queries and gives every other query `400` for invalid input, but `ProjectsQueryResponsePolicy` returns the raw 404 for any type that is not one of six normalized types (`ProjectsQueryResponsePolicy.cs:21-26`, re-read here). Today the four excepted types are exactly the remaining registered handlers, as the brownfield lens found, so behavior matches. The first new query, such as Story 8.2's export or an Epic 7 Preview query, will silently get 404 for malformed input.

**Proposed resolution.** Spine: “The exception is an allow-list of the four named types; every other registered Projects query type uses the AD-19 mapping.” Code: invert the check, and add a test that enumerates the registered `IDomainQueryHandler` query types.

**Anchors.** [AD-19 · line 289](ARCHITECTURE-SPINE.md) · [Structural Seed · line 487](ARCHITECTURE-SPINE.md) · **Raised by.** [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-9, Medium) · **Status.** CONFIRMED. · **Disposition.** **route**: DW-63 owner inverts the check; spine wording is an autofix. · **Gate.** None today; before the first new query type.

### M12 · Event evolution says nothing about payload versions, upcasters or rebuilds after fold changes

The convention forbids `V2` event types, but the EventStore SDK supports `[EventPayloadVersion(n)]` with upcaster chains that the SDK writer would consume, while the legacy processor and projections read raw bytes and ignore the payload version. A version-2 `ProjectCreated` with an upcaster seems to satisfy the convention while the two folds diverge. When an upcaster or fold rule changes, incremental read models and snapshots built under the old rule are never rebuilt.

**Proposed resolution.** Add to the convention: “Projects events stay at payload version 1 while any retained legacy fold exists. A payload version or upcaster requires retiring or upgrading every retained fold first. Any change to fold or upcaster semantics bumps the affected projection's version and forces a rebuild (Story 8.10 mechanics) and snapshot invalidation (H13).”

**Anchors.** [Event evolution convention · line 441](ARCHITECTURE-SPINE.md) · [AD-22 · line 309](ARCHITECTURE-SPINE.md) · [AD-14 · line 225](ARCHITECTURE-SPINE.md) · **Raised by.** [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-M6, Medium) · **Status.** CONFIRMED (the SDK capability exists; the spine is silent). · **Disposition.** **autofix**: Add the payload-version rule. · **Gate.** None (applies at the first event-schema change).

### M13 · The hand-edited OpenAPI “Contract Spine” is an unclassified second contract authority

AD-16 makes versioned .NET contracts the sole editable contract authority and says it prevents editable OpenAPI drift. Yet `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:1-12` calls itself the canonical “Contract Spine”, story commits edit it directly (`9dadd65c`, `f10129c9`, `005c2d5d`), it drives `Projects.Client` generation through NSwag, and `OpenApiContractSpineTests` assert it. When a story changes a retained v1 wire shape, one implementer edits the C# DTOs and another edits the YAML that regenerates the client.

**Proposed resolution.** Add to AD-16 or AD-24: “`hexalith.projects.v1.yaml` is the frozen retained-v1 authority for the 25 AD-24 pairs and `Projects.Client` generation. It accepts compatibility fixes only, never new operations, and retires with the adapter. New operations originate only in C# Contracts.”

**Anchors.** [AD-16 · lines 235–239](ARCHITECTURE-SPINE.md) · [Contracts convention · line 444](ARCHITECTURE-SPINE.md) · **Raised by.** [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-6, Medium) · **Status.** CONFIRMED. · **Disposition.** **discuss**: Which artifact a v1 wire-shape change edits first. · **Gate.** None.

### M14 · The ownership diagram routes all sibling calls through workflow transport, while the Server calls owners directly

The diagram shows owner calls only as dashed workflow-transport edges, but the Server references the Conversations, Folders and Memories clients (`Hexalith.Projects.Server.csproj:18-23`) and registers them for query-time reauthorization and v1 membership writes (`ProjectsServerServiceCollectionExtensions.cs:112-168,256-279`). A writer story implementing mutation-time owner reauthorization can wire a direct client, following the code, while the saga story routes the same call through workflow transport, with different identity, retry and idempotency semantics.

**Proposed resolution.** Add a dashed host edge for delegated owner queries (reauthorization) and one caption sentence: request-scoped owner reads and reauthorization go host-direct under the delegated actor; durable cross-context mutation goes through workflow transport (AD-12).

**Anchors.** [Design Paradigm diagram · lines 58–112](ARCHITECTURE-SPINE.md) · **Raised by.** [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-7, Medium) · **Status.** CONFIRMED. · **Disposition.** **autofix**: Add the host-direct edge and caption. · **Gate.** None.

### M15 · In-repo Projects.Cli and Projects.Mcp have no interim constraint

AD-24 retires the CLI and MCP runtime projects only after replacement, and AD-29 says both public heads refuse confirmation-required actions. But `Projects.Cli` archives, restores, relinks, unlinks and re-evaluates over retained v1 REST, gated by a `--confirm` flag and `--dry-run-evidence`, and sets `ReplacementConfirmed` from that flag (`src/Hexalith.Projects.Cli/ProjectsCliApplication.cs:50,350-352,428`). No spine rule stops a later story from extending it for continuity, creating an agent-capable surface outside AD-29. The Hexalith baseline already limits such packages to safety and continuity fixes.

**Proposed resolution.** One AD-24 or AD-29 sentence: “`Projects.Cli` and `Projects.Mcp` are frozen obsolete migration sources. They take safety and continuity fixes only, gain no operations, are never shipped or enrolled, and their mutation commands are inventoried for retirement.”

**Anchors.** [AD-24 · line 321](ARCHITECTURE-SPINE.md) · [AD-29 · line 353](ARCHITECTURE-SPINE.md) · [AD-33 · line 398](ARCHITECTURE-SPINE.md) · **Raised by.** [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-8, Medium) · **Status.** CONFIRMED. · **Disposition.** **autofix**: Restate the Hexalith MCP/CLI baseline. · **Gate.** None.

### M16 · The spine carries tracker state, CI run links, dated evidence and personal names, some already stale

Lines 43 and 46–52, the P1R measured paragraph, the McpCli `6993abf`/DW-82 note, the Stack revision anchor and the historical qualification paragraph hold story statuses, readiness results, CI run IDs, dated attempts and names. On review day HEAD was two commits past the `280b1eed` anchor. The `280b1eed` run (45479334086) had finished before finalization, with G-6 and P1R passing and build-and-test failing, yet line 469 cites G-6 only on the ancestor `26fed89d`; the run on `31b35637` (45488087994) repeated that result. Every tracker change forces a re-finalization and a new digest. The rubric lens saw run 45488087994 still in progress; the technology lens, written later, saw it finish, and this report uses the later state.

**Proposed resolution.** Move the boundaries table, McpCli note, CI links, P1R measured paragraph and historical qualification to the memlog, `sprint-status.yaml` or `docs/read-cutover.md`. Keep one pointer (“current delivery state: sprint tracker”) and one rule: cite G-6 on the exact candidate SHA, and a red build-and-test blocks qualification. Use role names instead of personal names.

**Anchors.** [Lines 43, 46–52](ARCHITECTURE-SPINE.md) · [AD-17 · lines 264–277](ARCHITECTURE-SPINE.md) · [AD-29 · line 355](ARCHITECTURE-SPINE.md) · [AD-30 · line 363](ARCHITECTURE-SPINE.md) · [Stack · lines 456, 469](ARCHITECTURE-SPINE.md) · **Raised by.** [rubric](reviews/review-validate-rubric-2026-11-24.md) (M6, Medium) · [technology](reviews/review-validate-technology-2026-11-24.md) (T-3, Medium) · **Status.** CONFIRMED (`gh run view`). · **Disposition.** **autofix**: Move delivery state out of the spine. · **Gate.** None.

### M17 · Size and rule density invite drift for small agents

The spine is 600 lines and about 89.6 KB (roughly 22k tokens), with single-bullet rules of 300–450 words (AD-4, AD-5, AD-17, AD-24, AD-33) that mix target invariants, transitional clauses (“Until the AD-17 writer cutover…”) and present-tense state. An agent that loads AD-24 alone misses the AD-19 and AD-20 qualifiers, and one that loads AD-19 alone takes the transitional 404 as permanent.

**Proposed resolution.** Split each AD into a Rule (the target invariant, about 120 words at most) and a labeled “Transitional (until X)” sub-bullet; move read-cutover operating detail to `docs/read-cutover.md`; aim for about 40 KB.

**Anchors.** [AD-4 · line 138](ARCHITECTURE-SPINE.md) · [AD-5 · line 167](ARCHITECTURE-SPINE.md) · [AD-17 · lines 245, 262–277](ARCHITECTURE-SPINE.md) · [AD-24 · line 321](ARCHITECTURE-SPINE.md) · [AD-33 · lines 387–416](ARCHITECTURE-SPINE.md) · **Raised by.** [rubric](reviews/review-validate-rubric-2026-11-24.md) (M7, Medium) · **Status.** PLAUSIBLE (judgment). · **Disposition.** **defer**: Next spine revision, after the C/H fixes land. · **Gate.** None.

### M18 · The production state-store provider is undecided while evidence lanes split between Redis and PostgreSQL

AD-27 measures on persistent Dapr/Redis, the G-6 lane restarts real PostgreSQL sidecars, and AD-28 says local, CI and production bind the same logical resource contract, but no row selects the production provider class; the Deferred rows cover only the Durable Task provider and the cloud provider. Story 8.9 can qualify NFR-5 on Redis while 8.10 and G-6 qualify RPO and restart on PostgreSQL, and production binds a provider neither exercised.

**Proposed resolution.** Require AD-27, AD-28 and AD-30 evidence to run on the state-store and pub/sub provider class selected for production, and record that selection as a G-5 platform decision before Stories 8.9 and 8.10, or add a Deferred row naming the evidence it invalidates.

**Anchors.** [AD-27 · line 341](ARCHITECTURE-SPINE.md) · [AD-28 · line 347](ARCHITECTURE-SPINE.md) · [Stack · line 469](ARCHITECTURE-SPINE.md) · [Deferred · lines 591, 598](ARCHITECTURE-SPINE.md) · **Raised by.** [rubric](reviews/review-validate-rubric-2026-11-24.md) (M3, Medium) · **Status.** CONFIRMED (text). · **Disposition.** **defer**: Deferred row; revisit before Stories 8.9/8.10 evidence (G-5). · **Gate.** Epic 8 release.

### M19 · The operational envelope lacks SLI/SLO measurement, alerting and cross-store restore consistency

AD-28 says platform configuration and evidence “must enforce” 99.9% monthly availability, but configuration cannot enforce availability, and the spine names no SLI, measurement source or alert owner. There is no rule for restoring the event store relative to the task, confirmation, idempotency and audit stores: a point-in-time restore can leave Durable Task records and consumed artifacts ahead of the events, producing duplicate or orphaned owner calls that nothing routes to `NeedsAttention`.

**Proposed resolution.** Add to AD-28 an SLI measured at the Gateway by platform telemetry, platform-owned alerting, and a restore rule (“a restore is cross-store consistent, or mutation freezes and in-flight tasks enter `NeedsAttention`”), plus a restore-drill row in AD-30, or add Deferred rows with owners.

**Anchors.** [AD-28 · line 347](ARCHITECTURE-SPINE.md) · [AD-17 P1R note · line 274](ARCHITECTURE-SPINE.md) · [Deferred · line 598](ARCHITECTURE-SPINE.md) · [PRD NFR-3 · line 546](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · **Raised by.** [rubric](reviews/review-validate-rubric-2026-11-24.md) (M4, Medium) · **Status.** PLAUSIBLE. · **Disposition.** **defer**: Deferred rows owned by the platform; revisit before Story 8.11. · **Gate.** Epic 8 release.

### M20 · AD-27 introduces an ungated platform audit archive and reinterprets NFR-5

AD-27 says the NFR-5 audit figure is not a retention cap and that older records move losslessly to “a bounded, queryable platform archive”. The PRD says only “100,000 retained audit records per Project”. No gate (G-1 to G-6), Deferred row or capability-map row names the archive, and the memlog has no decision for it. Stories 8.1 and 8.9 may assume an archive nobody builds, or Projects may build its own store, against AD-1.

**Proposed resolution.** Record the NFR-5 interpretation as a product decision linked from the PRD or addendum. Then either name the archive owner and entry condition under G-1 or G-5, or restate it as “query shape of 100,000 per Project; retention per AD-26; archive mechanism deferred to platform design” with a revisit condition.

**Anchors.** [AD-27 · line 341](ARCHITECTURE-SPINE.md) · [External gates · lines 553–558](ARCHITECTURE-SPINE.md) · [Capability map FR-21 · line 572](ARCHITECTURE-SPINE.md) · [PRD NFR-5 · line 551](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · **Raised by.** [rubric](reviews/review-validate-rubric-2026-11-24.md) (M5, Medium) · [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-9, Low) · **Status.** CONFIRMED. · **Disposition.** **discuss**: Confirm the NFR-5 reading; name the archive owner or defer it. · **Gate.** Epic 8 release.

### M21 · AD-34 does not make Projects responsible for accessibility of its own components

AD-34 binds FR-14, FR-15 and FR-22 to FR-24 and assigns conformance to “Chatbot and operator presentation owners”, but the confirmation and selection components are Projects-owned (AD-5, AD-13, PRD A-6), and FR-25 is not bound. The rule also drops NFR-9's “visibly focused” and “do not rely on … timing alone”, although the spine fixes hard limits (a 15-minute Confirmation Artifact and 5-minute Selection Evidence). Story 7.16's component can let a screen-reader user's pick lapse at five minutes with no warning, which would surface only in the 8.8-P3 journey.

**Proposed resolution.** AD-34: bind FR-25 and the confirmation flows of FR-4, FR-7, FR-8, FR-11 and FR-23. State that Projects owns WCAG 2.2 AA conformance of its confirmation and selection components, while Chatbot and FrontComposer own their host surfaces. Add visible focus and “no reliance on timing alone: expiry is announced before it lapses and is recoverable through `RequestPreview`/`RenewPreview` without losing entered context”.

**Anchors.** [AD-34 · lines 418–422](ARCHITECTURE-SPINE.md) · [Capability map NFR-9 · line 580](ARCHITECTURE-SPINE.md) · [PRD NFR-9 · line 558](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · **Raised by.** [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-2, Medium) · **Status.** CONFIRMED. · **Disposition.** **autofix**: Bind FR-25; add Projects ownership, focus and timing. · **Gate.** Epic 8 release (8.8-P3); Story 7.16 consumes it.

### M22 · FR-21 audit of the Setup-update credential kind is missing from AD-26

FR-21 requires audit of “Setup updates with the admitting credential kind (interactive or delegated)”, and a Service/Workflow Caller may perform FR-3 Setup updates. AD-26's list of audited content never mentions Setup updates or credential kind, since Setup is task-only rather than a confirmed mutation. Story 8.1's acceptance checks it, but the platform builds the audit schema from AD-26.

**Proposed resolution.** Add to AD-26: “Setup updates with the admitting credential kind (interactive or delegated) taken from the AD-20 context.”

**Anchors.** [AD-26 · line 335](ARCHITECTURE-SPINE.md) · [PRD FR-21 · line 491](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · [PRD · line 116](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · **Raised by.** [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-3, Medium) · **Status.** CONFIRMED. · **Disposition.** **autofix**: Add the Setup credential-kind clause. · **Gate.** Epic 8 release (Story 8.1).

### M23 · NFR-10's additive-contract promise is bound only to events and specific v1 shapes

NFR-10 makes contracts additive and serialization-tolerant unless a breaking change is approved, and retirement needs a major version, migration notice, usage, compatibility and rollback evidence. The spine applies additivity only to events and to retained v1 REST and client signatures. AD-16 has no evolution rule for published Contracts types, although McpCli enrolls Projects.Contracts 2.0.0 and Chatbot consumes it, so an Epic 8 story can rename or retype a published query DTO member and pass spine review.

**Proposed resolution.** Add to AD-16: “Published Projects.Contracts types, operation schemas and wire vocabulary evolve additively and serialization-tolerantly. Removing, renaming or retyping a published member, or changing its semantics, needs explicit approval and the NFR-10 retirement evidence, gated by package API baselines.”

**Anchors.** [AD-16 · line 239](ARCHITECTURE-SPINE.md) · [Event evolution · line 441](ARCHITECTURE-SPINE.md) · [AD-17 · line 262](ARCHITECTURE-SPINE.md) · [PRD NFR-10 · line 559](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · **Raised by.** [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-5, Medium) · **Status.** CONFIRMED (text); PLAUSIBLE (impact). · **Disposition.** **autofix**: Add the Contracts evolution rule. · **Gate.** Next Projects.Contracts publication; Epic 8 release.

### M24 · FR-22's per-Project inspection scope and “no cross-Project enumeration” were dropped

The PRD says inspection is per Project and per task, that reference and audit reads are scoped to one Project and paged, and that v1 offers no cross-Project audit or reference enumeration. The spine scopes only the quarantine inventory per Project, while the epics plan a cross-project operational dashboard with aggregated tiles, and no rule separates that dashboard from forbidden enumeration. Story 8.1 or 8.3 can add a Tenant-wide audit or reference query that satisfies every AD.

**Proposed resolution.** Add to AD-33 or AD-14: “FR-22 reference and audit reads are scoped to one Project or one task and paged. No surface offers cross-Project audit or reference enumeration in v1. Tenant aggregates carry only counts and status codes.”

**Anchors.** [AD-14 · line 225](ARCHITECTURE-SPINE.md) · [AD-33 · lines 405–412](ARCHITECTURE-SPINE.md) · [Capability map FR-22 · line 573](ARCHITECTURE-SPINE.md) · [PRD FR-22 · lines 71, 504](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md) · **Raised by.** [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-6, Medium) · [rubric](reviews/review-validate-rubric-2026-11-24.md) (L1 (FR-22 part), Low) · **Status.** PLAUSIBLE. · **Disposition.** **autofix**: Restate the PRD scope rule. · **Gate.** Epic 8 release.

### M25 · The Builds CLI pin is stale and the evidence CLI is unpinned

The runner contract says Projects selects Module.Cli 6.4.2 (`Directory.Packages.props:6`), and test projects consume `Hexalith.Builds.Tooling.dll` 6.4.2, but CI and G-6 build Module.Cli from the Builds gitlink source (`v6.7.0-7`; `.github/workflows/ci.yml:236-241`, `tools/qualification/run_g6_current.py:35,64`), and nuget.org carries 6.4.3 through 6.8.2. AD-30 makes Builds own `hexalith-evidence validate`, yet `.config/dotnet-tools.json` pins only the FrontComposer CLI, so AD-30 rows are validated with whatever version someone installs.

**Proposed resolution.** State one Builds tooling rule: Module.Cli and Evidence.Cli versions match the pinned Builds release or the published version that Builds selects; CI builds Module.Cli from the gitlink; and `hexalith-evidence` is pinned in `.config/dotnet-tools.json` before any AD-30 row is recorded as validated. Correct 6.4.2, or record why it is intentionally older.

**Anchors.** [Runner contract · line 511](ARCHITECTURE-SPINE.md) · [AD-30 · line 361](ARCHITECTURE-SPINE.md) · **Raised by.** [technology](reviews/review-validate-technology-2026-11-24.md) (T-4, Medium) · **Status.** CONFIRMED. · **Disposition.** **route**: Projects build-tooling owner reconciles pins. · **Gate.** Epic 8 release (AD-30 evidence rows).

### M26 · G-6 coverage is a catalog delta, not only FrontComposer

The Stack says G-6 evidence does not cover FrontComposer 6.1.0 because the platform pins Builds `763fc0e9`. The catalog diff to the committed `bf8a28b4` also changes Memories 4.1.0 to 4.1.1, `Microsoft.Extensions.Http.Resilience` and `ServiceDiscovery` 11.0.0 to 11.0.1 (declared by the shipping `Projects.ServiceDefaults`), and several test packages. Release qualification could treat G-6 as covering everything except FrontComposer.

**Proposed resolution.** “G-6 evidence covers the Builds `763fc0e9` catalog only. Any version that differs in the committed `bf8a28b4` catalog is unqualified by G-6; today that includes FrontComposer 6.1.0, Memories 4.1.1 and Microsoft.Extensions.Http.Resilience/ServiceDiscovery 11.0.1.”

**Anchors.** [Stack G-6 paragraph · line 469](ARCHITECTURE-SPINE.md) · [AD-24 · line 321](ARCHITECTURE-SPINE.md) · **Raised by.** [technology](reviews/review-validate-technology-2026-11-24.md) (T-5, Medium) · **Status.** CONFIRMED (catalog diff, `tools/release-packages.json`). · **Disposition.** **autofix**: State coverage as a catalog delta. · **Gate.** Epic 8 release.

## Low

- **L1 · McpCli-expansion Deferred row reads as deferring FR-22/FR-24 CLI and MCP access.** The McpCli-expansion Deferred row is vague and reads as deferring FR-22 and FR-24 CLI and MCP access, which AD-21 and the PRD make release obligations. Reword it to defer only naming, inventory and presentation, revisited at Story 8.4/8.5 entry with G-3 enrollment (owners: McpCli and Projects). [Deferred · line 596](ARCHITECTURE-SPINE.md) · [AD-21 · line 303](ARCHITECTURE-SPINE.md) · Raised by [rubric](reviews/review-validate-rubric-2026-11-24.md) (L2) · [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-7) · **autofix** (Reword the Deferred row) · Gate: None.
- **L2 · Target host snippet reads as current code.** The target host snippet `builder.AddEventStoreDomainService(typeof(ProjectAggregate).Assembly)` reads as current code. Label it “target, after the AD-17 writer gate”. Settled: the technology and brownfield lenses are right that it compiles (`ProjectAggregate` is a static class, `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs:28`), and the rubric lens is right that today's host passes the Server assembly (`ProjectsServerServiceCollectionExtensions.cs:62`); the label fixes both readings. [Structural Seed · lines 487–492](ARCHITECTURE-SPINE.md) · Raised by [rubric](reviews/review-validate-rubric-2026-11-24.md) (L3) · **autofix** (Label the snippet as target) · Gate: None.
- **L3 · Memlog entries 98 and 103 disagree on the 5f1d955b failure.** Memlog entries 98 and 103 disagree on why run 45430692159 (`5f1d955b`) failed. The technology lens confirmed entry 103 (failure at submodule initialization, as line 52 says); append a memlog correction entry. [Release row · line 52](ARCHITECTURE-SPINE.md) · Raised by [rubric](reviews/review-validate-rubric-2026-11-24.md) (L4) · **autofix** (Memlog correction entry) · Gate: None.
- **L4 · Aspire 17.0.0 SDK deprecation not recorded.** Aspire 17.0.0 deprecates `Aspire.AppHost.Sdk` and `AddProject` (ASPIRE012/CS0618), which the Projects AppHost suppresses. Record this in the Stack row and say new AppHost code uses `Microsoft.NET.Sdk` with path-based registration. [Stack, Aspire row · line 463](ARCHITECTURE-SPINE.md) · [AD-25 · line 327](ARCHITECTURE-SPINE.md) · Raised by [technology](reviews/review-validate-technology-2026-11-24.md) (T-6) · **autofix** (Record the deprecation) · Gate: None.
- **L5 · FrontComposer CLI tool lags the packages.** The FrontComposer CLI tool is pinned at 6.0.1 while the packages are 6.1.0 (tool 6.1.0 was published on 2026-11-23). Bump it, or state that 6.0.1 output is not evidence for 6.1.0 descriptors. [Runner contract · line 511](ARCHITECTURE-SPINE.md) · [Stack · line 461](ARCHITECTURE-SPINE.md) · Raised by [technology](reviews/review-validate-technology-2026-11-24.md) (T-7) · **route** (Projects tooling owner) · Gate: None.
- **L6 · Proposal-ledger orphans missing from the AD-17 inventory.** The per-replica v1 proposal-confirmation ledger can leave orphans (a Project without membership, or a duplicate Project) that the AD-17 partial-record inventory does not list. Add that class with a repair disposition. PLAUSIBLE. [AD-24 · line 321](ARCHITECTURE-SPINE.md) · [AD-17 · line 245](ARCHITECTURE-SPINE.md) · Raised by [adversarial](reviews/review-validate-adversarial-2026-11-24.md) (ADV-11) · **route** (AD-17 cutover inventory owner) · Gate: AD-17 writer cutover.
- **L7 · No test asserts exactly one keyed processor.** No test asserts AD-17's “exactly one keyed `projects` processor”: `GetRequiredKeyedService` returns the last registration. Assert `GetKeyedServices<IDomainProcessor>("projects")` and the replay are each single and refer to the same writer. [AD-17 · line 245](ARCHITECTURE-SPINE.md) · Raised by [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-10) · **route** (DW entry for the cutover owner) · Gate: AD-17 writer cutover.
- **L8 · Route-catalog authority is split.** Route-catalog authority is split between `ProjectsRuntimeCatalogDeclarations` and `module/hexalith.module-manifest.v2.json` with no reconciling test, and the development catalog's retirement owner (DW-63 or AD-25) is contradictory. Name one authority and the check that binds the other. [Structural Seed · line 487](ARCHITECTURE-SPINE.md) · [Runner contract · line 511](ARCHITECTURE-SPINE.md) · Raised by [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-11) · **route** (DW-63 owner) · Gate: None.
- **L9 · One-type-per-file drift in the Server.** Five Server files declare more than one type, against the one-type-per-file convention (for example `IProjectCommandSubmitter.cs`). Route a hygiene DW to the Server owner, or note that existing cases are tolerated until the types move (H3). [Files and types convention · line 445](ARCHITECTURE-SPINE.md) · Raised by [brownfield](reviews/review-validate-brownfield-2026-11-24.md) (BF-12) · **route** (Hygiene DW for the Server owner) · Gate: None.
- **L10 · Raw signing key read from configuration.** `ProjectChatBotAuthorityGateway.cs:74-85` imports a raw PKCS#8 RSA private key from `Authentication:BoundStreamRead:PrivateKey`. AD-28 and G-5 should require platform-issued signing material (a handle or remote signer). [AD-28 · line 347](ARCHITECTURE-SPINE.md) · [Deferred · line 592](ARCHITECTURE-SPINE.md) · Raised by [security](reviews/review-validate-security-2026-11-24.md) (SEC-11) · **route** (G-5 owner) · Gate: Epic 8 release.
- **L11 · Writer cutover is roll-forward only; say so.** Writer cutover from the current processor is roll-forward only, since that processor emits `ProjectFolderCreationPending` and has no claim. Say in AD-17 that “executable rollback” for the writer means mutation freeze plus forward recovery (pairs with H4). [AD-17 · line 245](ARCHITECTURE-SPINE.md) · Raised by [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-L1) · **autofix** (State roll-forward only) · Gate: AD-17 writer cutover.
- **L12 · Reverse-index rebuild source unnamed.** Name the reverse-index rebuild source as the Conversations owner's membership history and receipts under G-2, not Projects task or audit data. PLAUSIBLE. [AD-10 · line 197](ARCHITECTURE-SPINE.md) · Raised by [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-L2) · **autofix** (Name the rebuild source) · Gate: None.
- **L13 · Idempotency retention start for nonterminal tasks.** State that an idempotency record never expires while its task is nonterminal, and that the 30 days count from the terminal transition. PLAUSIBLE. [AD-5 · line 167](ARCHITECTURE-SPINE.md) · Raised by [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) (DI-L3) · **autofix** (Clarify the retention start) · Gate: None.
- **L14 · NFR-3 qualifiers and NFR-7 retry guidance dropped.** Restore NFR-3's qualifiers (“excluding planned maintenance”, “with required dependencies healthy”) in AD-28, and require bounded retry-after guidance on `429`/`503` in AD-19 (NFR-7). [AD-28 · line 347](ARCHITECTURE-SPINE.md) · [AD-19 · line 289](ARCHITECTURE-SPINE.md) · Raised by [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-8) · **autofix** (Restore the PRD qualifiers) · Gate: Epic 8 release.
- **L15 · Assumption fallbacks A-1, A-2 and A-4 not carried.** Carry the PRD fallbacks for A-1 (SM-7/SM-8 “not measurable” without the Chatbot feed), A-2 (pre-check and fail closed on same-name Folders) and A-4 (bulk binding only under FR-8/FR-22) in the conventions or Deferred table. A-6 is handled in H14. [Outcome measurement · line 450](ARCHITECTURE-SPINE.md) · [G-2 · line 554](ARCHITECTURE-SPINE.md) · [Deferred · lines 587–600](ARCHITECTURE-SPINE.md) · Raised by [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-10) · **autofix** (Carry the assumption fallbacks) · Gate: Epic 8 release.
- **L16 · Freshness normalization casing not carried.** Add a convention row for addendum §2 freshness normalization: lower-case machine codes and PascalCase labels via `EvidenceFreshnessStateCode`, preserving producer fields. [AD-32 · line 377](ARCHITECTURE-SPINE.md) · [Vocabulary convention · line 439](ARCHITECTURE-SPINE.md) · Raised by [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-11) · **autofix** (Add the normalization row) · Gate: None.
- **L17 · Frontmatter sources omit cited proposals.** Add the cited planning artifacts missing from the frontmatter `sources`: the 2026-11-21, 2026-11-22 and 2026-11-22-schedule-story-8-1 sprint-change proposals and the 2026-11-24 readiness rerun. [Frontmatter · lines 17–37](ARCHITECTURE-SPINE.md) · [Lines 52, 264](ARCHITECTURE-SPINE.md) · Raised by [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-12) · **autofix** (Complete the sources list) · Gate: None.
- **L18 · Archived Projects' resolution eligibility not carried.** Add to AD-14 that resolution candidates are Active, read-model-confirmed Projects by default, and Archived Projects only on explicit request (FR-4, FR-12). [AD-14 · line 227](ARCHITECTURE-SPINE.md) · [AD-3 · line 132](ARCHITECTURE-SPINE.md) · Raised by [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) (F-13) · **autofix** (Add the Archived rule) · Gate: None.

## What blocks which gate

| Gate | Critical | High | Medium and low | What must happen |
| --- | --- | --- | --- | --- |
| DW-63 acceptance | C1 | H1 | — | Real or deny-by-default authorization in the shipped composition, and a published EventStore release with the query-response seam before CI can produce any evidence. |
| Story 7.1 first writer | C1 (latest point), C2 | H2–H11 | M1–M6 | Decisions on claim enforcement, ChatBot grants, the handler home, the cutover definition, v1 interim behavior, identities and token handling; claim evidence on Dapr 1.19.2 or later. |
| AD-17 writer cutover | C2 | H4, H5, H6, H10, H11, H12, H13 | M7, M8, M9; L6, L7, L11 | One recorded cutover event, a v1 route table, fold and snapshot rules, and a defined end for the AD-19 exception. |
| Stories 7.11, 7.12 and 7.16 entry | — | H14 | M21 (consumed by 7.16) | Component package, hosting contract and credential issuer. |
| Next Projects.Contracts publication (G-3) | — | H15 | M23 | Decorated operations equal the approved inventory, and published contracts evolve additively. |
| Epic 8 release | — | H2 (disclosure), H7 (evidence), H15, H16, H17 | M5, M18–M26; L10, L14, L15 | An audit-storage decision, DW-85 closure or an approved revocation-lag bound, the operational envelope and pinned evidence tooling. |
| None | — | — | M10–M17; L1–L5, L8, L9, L12, L13, L16–L18 | Spine, memlog or hygiene fixes that no gate waits on. |

## Checks

### Checks that held up

- **Host shape.** `Program.cs` is `AddProjectsDomainService()`, `UseEventStoreDomainService()` and one `MapProjectsHumanEndpoints()`. `AddProjectsDomainService` calls the SDK host with the Server assembly, as line 487 states, and refuses the anonymous development bypass. (rubric, adversarial, brownfield, security; re-read here)
- **25 retained v1 routes.** Exactly 25 `/api/v1/projects` method-and-path pairs (10 GET, 15 non-GET), the same at `5f1d955b` and HEAD, all under the platform human policy with no `AllowAnonymous`. Commands go through `IProjectCommandSubmitter` and membership through `IProjectConversationAssignmentDirectory`; the only durable adapter write is the AD-33 inspection audit; the proposal ledger is per-replica. (rubric, adversarial, brownfield; recounted here)
- **Single writer.** One keyed `projects` `IDomainProcessor` and one matching keyed `IAsyncAggregateReplay`; no `EventStoreAggregate<TState>` type exists yet. (all code lenses; re-read here)
- **Query-policy split.** `ProjectsQueryResponsePolicy` returns 400 for `InvalidEnvelope` and `InvalidCursor` and a `safe-denial` 404 on exactly six types. The four AD-19-excepted types are the remaining registered handlers, the SDK resolves one `IDomainQueryResponsePolicy`, and denial precedes validation on the excepted handlers, so unauthorized callers get no validation oracle. (rubric, brownfield, adversarial, security)
- **EventStore seam.** `IDomainQueryResponsePolicy` exists only in untagged EventStore `208af26a` and is absent at `v5.5.1` and `v5.5.2`, as the Stack says. H1 covers the consequence. (technology, brownfield, rubric)
- **Versions against nuget.org.** .NET SDK 11.0.100 is the latest. The Builds `bf8a28b4` catalog matches every Stack value, and every third-party version is the latest stable release. Aspire 17.0.0 is current, the Toolkit Dapr prerelease is a real need, the Dapr support-table exception is accurate, and McpCli 3.0.0 predates Projects enrollment. (technology)
- **CI history.** `26fed89d` was fully green, including P1R and G-6. `5f1d955b` failed at submodule initialization before any build, as line 52 says. (technology)
- **FR and NFR coverage.** FR-1 to FR-25 and NFR-1 to NFR-11 each map to at least one AD. Numeric envelopes match the PRD: retention, export bounds, AD-27 admission rates, RPO and RTO, the AD-33 role matrix and the Recovery Action Code list. (prd-reconcile, rubric)
- **Contracts and seams.** `Hexalith.Projects.Contracts` is dependency-light, AD-14 uses named `IAsyncDomainProjectionHandler`s with one identified legacy full replay, and `tools/release-packages.json` lists five packages. (rubric, brownfield, technology)
- **Security basics.** Inbound JWT validation pins issuer and audience, Tenant and principal headers cannot override claims, v1 create authorizes before reading the body, the human policy rejects workload and mixed credentials, and the SDK query path binds Tenant and actor from the verified envelope. (security, brownfield)
- **Data-integrity rules.** AD-35 keeps the claim through unknown outcomes and makes stale releases no-ops, AD-13 converges lost replies, AD-4 terminal states are immutable, the AD-22 Pending deserializer is retained, and folds fail closed on unknown event types. (data-integrity)
- **Internal consistency and links.** The AD-4, AD-12 and AD-33 reconciliation bounds agree, AD-19 and AD-32 agree, and all 32 relative links, 20 frontmatter sources and the Section G anchor resolve. (rubric)

### How contradictions were settled

- **A-6 coverage (rubric against prd-reconcile).** Spine lines 167, 217 and 297 carry the A-6 issuer rule, but not the component's package, hosting or credential issuer, nor the A-6 citation and fallback. Both lenses were partly right; kept High as H14.
- **Target host snippet (rubric against technology and brownfield).** The snippet compiles, and it is also not today's host. Kept as L2 with a “target” label as the fix.
- **v1 mutation route count (adversarial against brownfield).** The endpoint file maps 10 GET and 15 non-GET routes; H4's acceptance check uses 15, not 18.
- **Allow-all validator severity (security against three lenses).** Kept Critical (C1): the writer's only authorization layer approves everything on the shipped path.
- **AD-19 wording severity (rubric against brownfield and security).** Kept Medium (M10): a literal reading breaks a frozen DW-63 expectation.
- **State of CI run 45488087994 (rubric against technology).** The rubric lens saw it in progress; the later technology lens saw it finish with G-6 passing and build-and-test failing. M16 uses the later state.
- **Inspection audit (rubric and brownfield against adversarial).** Not a contradiction: the code matches AD-24, and ADV-5 says AD-24 conflicts with AD-14 and AD-26 (H16).
- **Memlog entries 98 and 103 (rubric).** The technology lens's `gh run view` confirms entry 103; L3 adds a memlog correction.

### Checks completed

- Deterministic lint: `lint_spine.py` returned 0 findings (reported by the gate; not re-run for this report).
- Digest: SHA-256 recomputed for this report as `01b89a2f5e076a52b17f253417b7472ae85c0c7d26ae5a3c21c428e5868eb5fe`; 600 lines; 35 AD headings, AD-1 to AD-35. Every lens recorded the same digest.
- Seven lenses, 83 findings in total, merged into 63 (table below).
- Light verification in this consolidation: the shipped composition and allow-all validator (C1), the Folder replacement path (C2), the 25 route mappings (H4, H5), the ChatBot grant command and authority handler (H2), v1 create identity (H6), the v1 Tenant accessor (H8), the Dapr pins (H11), the v4 matrix and response policy (H12, M11), the preview decorations (H15), PRD §2.1 and A-6 (H2, H14), the November 22 proposal and DW-85 (H17), the proposal-confirm order and the missing DW entry (M6), and every spine line cited as an anchor.
- Baseline: Projects `31b35637` on `main`. The uncommitted working-tree edits (`docs/runbooks/projects-topology.md`, `WorkloadCredentialTopologyTests.cs`) touch no spine claim, per the brownfield lens.
- Not used: the older reviews in `reviews/`. The November 20 report was read for its visual design and for the AD-29 cross-reference in H15.

| Lens | Focus | Verdict | Critical · High · Medium · Low |
| --- | --- | --- | --- |
| [rubric](reviews/review-validate-rubric-2026-11-24.md) | Good-spine rubric walker | PASS WITH FINDINGS | 0 · 3 · 8 · 4 |
| [technology](reviews/review-validate-technology-2026-11-24.md) | Technology currency and reality check | PASS WITH FINDINGS | 0 · 2 · 3 · 2 |
| [adversarial](reviews/review-validate-adversarial-2026-11-24.md) | Adversarial incompatibility | FAIL | 1 · 5 · 4 · 1 |
| [brownfield](reviews/review-validate-brownfield-2026-11-24.md) | Code conformance | PASS WITH FINDINGS | 0 · 4 · 4 · 4 |
| [security](reviews/review-validate-security-2026-11-24.md) | Security and Tenant isolation | FAIL | 1 · 4 · 3 · 4 |
| [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) | Data integrity and event sourcing | PASS WITH FINDINGS | 0 · 4 · 6 · 3 |
| [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) | PRD FR/NFR reconciliation | PASS WITH FINDINGS | 0 · 1 · 5 · 7 |

## Disposition

| ID | Severity | Title | Recommended disposition | Gate |
| --- | --- | --- | --- | --- |
| C1 | Critical | The shipped writer authorizes every command through a test-only allow-all validator, and no gate removes it | route: Projects Server owner; removal gates DW-63 acceptance | DW-63 acceptance (latest: Story 7.1 first writer) |
| C2 | Critical | A Folder binding can be installed without a current AD-35 claim | discuss: Decide where claim enforcement lives and approve the AD-17 deltas | Story 7.1 first writer; AD-17 writer cutover |
| H1 | High | CI cannot build any revision: “source mode only” hides a package-mode compile failure | route: EventStore release, Builds selection, Projects gitlink bump | DW-63 acceptance |
| H2 | High | ChatBot Project grants contradict AD-3 and have no governing AD | discuss: Sanction as bounded AD-3 exception, or freeze to replay-only | Story 7.1 first writer (disposition); Epic 8 release (disclosure fix) |
| H3 | High | Query and projection handlers live in the Server, and the spine's target home cannot hold them | discuss: Choose the handler/projection assembly and its dependency rule | Story 7.1 first writer |
| H4 | High | “The AD-17 writer cutover” is not one defined event, but three ADs switch on it | discuss: Global cutover with slice enablement, or per-slice cutovers | Story 7.1 first writer; AD-17 writer cutover |
| H5 | High | Retained v1 confirmation routes accept Boolean confirmations from any human bearer and have no post-cutover outcome | discuss: Post-cutover v1 behavior and pre-cutover interim | Story 7.1 first writer (interim; proposal confirmation shares the 7.1-P1 validator path); AD-17 writer cutover (route table) |
| H6 | High | v1 create takes GUID-shaped or caller-chosen identities, and the post-cutover rule is undefined | discuss: Reject or alias a caller-supplied projectId after cutover | Story 7.1 first writer; AD-17 writer cutover |
| H7 | High | The retained adapter forwards raw human bearer tokens to EventStore, Conversations and Folders | discuss: Bounded exception, or replace forwarding before the first writer | Story 7.1 first writer; Epic 8 release (audience and delegation evidence) |
| H8 | High | Multi-Tenant principals: v1 routes pick the Tenant by token claim order | route: Story 7.1 owner fixes the v1 Tenant accessor | Story 7.1 first writer |
| H9 | High | Retained folds “fold” a Folder-first ProjectCreated by silently dropping the Folder | autofix: Rule wording carries the recorded intent | Story 7.1 first writer |
| H10 | High | Projects' own commit step has no stable idempotency key, expected version or conflict outcome | discuss: Confirm the version-conflict outcome policy | Story 7.1 first writer; AD-17 writer cutover |
| H11 | High | G-6 and CI run Dapr 1.19.1, which predates the 1.19.2 ETag and first-write fix | route: G-6 policy owner; CI change proposed to Hexalith.CiCd | Story 7.1 first writer (AD-35 claim-race proof); AD-17 writer cutover |
| H12 | High | The AD-19 404 exception ends at the writer cutover, but the v4 read packet hard-codes the 404 | discuss: Decouple the exception's end from the writer cutover, or require v5 at cutover | AD-17 writer cutover |
| H13 | High | Aggregate snapshots are outside writer selection and rollback | autofix: Add the snapshot rule to AD-17 | AD-17 writer cutover |
| H14 | High | The Projects-owned selection and confirmation component has no home and no credential issuer | discuss: Component package, hosting and credential issuer | Stories 7.11, 7.12 and 7.16 entry |
| H15 | High | AD-29's four-read inventory is not enforced, and two preview operations are already decorated | discuss: Remove the two preview decorations, or approve them with G-3 evidence | Next Projects.Contracts publication (G-3 enrollment); at the latest Epic 8 release |
| H16 | High | Durable inspection audit is primary state in the rebuildable read-model store | discuss: Platform audit stream, or carve-out | Epic 8 release (Stories 8.1 and 8.10) |
| H17 | High | Supported context reads skip request-time Folder authorization (DW-85), and the spine omits it | autofix: Record the approved November 22 scope change | Epic 8 release (production Supported selection and Chatbot adoption) |
| M1 | Medium | The AD-35 claim has no home, generation definition or terminal-state release rule | discuss: Claim substrate and owner; decide with C2 | Story 7.1 first writer |
| M2 | Medium | “Project version” is undefined, and rejection events advance the stream sequence | discuss: Choose the Project version definition | Story 7.1 first writer |
| M3 | Medium | No rule rechecks the original actor's authority while a task runs | discuss: Terminal state on mid-task revocation; credential for later owner steps | Story 7.1 first writer |
| M4 | Medium | AD-31 never names who assigns metadataClass or what it governs | discuss: Assigning principal and persistence of the class | Story 7.1 first writer |
| M5 | Medium | No system of record for Tenant-role and session-mode evidence | discuss: Name the Tenant-role system of record (likely G-5) | Story 7.1 first writer; Epic 8 release |
| M6 | Medium | Retained proposal confirmation parses before it authorizes, and the promised DW entry does not exist | route: New DW entry for the Story 7.12 owner | Story 7.1 first writer (7.1-P1 acceptance) |
| M7 | Medium | Idempotency has three layers with different scopes, and nothing carries keys across the cutover | route: AD-17 cutover inventory owner | AD-17 writer cutover |
| M8 | Medium | Conflicting legacy incumbents stay Active and context-usable | discuss: Quarantine conflicting legacy holders at cutover, or not | AD-17 writer cutover |
| M9 | Medium | FR-3 and FR-19 Setup-policy invariants have no carrying AD | autofix: Restate the PRD Setup rules | AD-17 writer cutover |
| M10 | Medium | AD-19's “same safe 404” wording does not match receiver bodies | autofix: Reword to the actual body set | None |
| M11 | Medium | The AD-19 exception is coded as a deny-list of six types | route: DW-63 owner inverts the check; spine wording is an autofix | None today; before the first new query type |
| M12 | Medium | Event evolution says nothing about payload versions, upcasters or rebuilds after fold changes | autofix: Add the payload-version rule | None (applies at the first event-schema change) |
| M13 | Medium | The hand-edited OpenAPI “Contract Spine” is an unclassified second contract authority | discuss: Which artifact a v1 wire-shape change edits first | None |
| M14 | Medium | The ownership diagram routes all sibling calls through workflow transport, while the Server calls owners directly | autofix: Add the host-direct edge and caption | None |
| M15 | Medium | In-repo Projects.Cli and Projects.Mcp have no interim constraint | autofix: Restate the Hexalith MCP/CLI baseline | None |
| M16 | Medium | The spine carries tracker state, CI run links, dated evidence and personal names, some already stale | autofix: Move delivery state out of the spine | None |
| M17 | Medium | Size and rule density invite drift for small agents | defer: Next spine revision, after the C/H fixes land | None |
| M18 | Medium | The production state-store provider is undecided while evidence lanes split between Redis and PostgreSQL | defer: Deferred row; revisit before Stories 8.9/8.10 evidence (G-5) | Epic 8 release |
| M19 | Medium | The operational envelope lacks SLI/SLO measurement, alerting and cross-store restore consistency | defer: Deferred rows owned by the platform; revisit before Story 8.11 | Epic 8 release |
| M20 | Medium | AD-27 introduces an ungated platform audit archive and reinterprets NFR-5 | discuss: Confirm the NFR-5 reading; name the archive owner or defer it | Epic 8 release |
| M21 | Medium | AD-34 does not make Projects responsible for accessibility of its own components | autofix: Bind FR-25; add Projects ownership, focus and timing | Epic 8 release (8.8-P3); Story 7.16 consumes it |
| M22 | Medium | FR-21 audit of the Setup-update credential kind is missing from AD-26 | autofix: Add the Setup credential-kind clause | Epic 8 release (Story 8.1) |
| M23 | Medium | NFR-10's additive-contract promise is bound only to events and specific v1 shapes | autofix: Add the Contracts evolution rule | Next Projects.Contracts publication; Epic 8 release |
| M24 | Medium | FR-22's per-Project inspection scope and “no cross-Project enumeration” were dropped | autofix: Restate the PRD scope rule | Epic 8 release |
| M25 | Medium | The Builds CLI pin is stale and the evidence CLI is unpinned | route: Projects build-tooling owner reconciles pins | Epic 8 release (AD-30 evidence rows) |
| M26 | Medium | G-6 coverage is a catalog delta, not only FrontComposer | autofix: State coverage as a catalog delta | Epic 8 release |
| L1 | Low | McpCli-expansion Deferred row reads as deferring FR-22/FR-24 CLI and MCP access | autofix: Reword the Deferred row | None |
| L2 | Low | Target host snippet reads as current code | autofix: Label the snippet as target | None |
| L3 | Low | Memlog entries 98 and 103 disagree on the 5f1d955b failure | autofix: Memlog correction entry | None |
| L4 | Low | Aspire 17.0.0 SDK deprecation not recorded | autofix: Record the deprecation | None |
| L5 | Low | FrontComposer CLI tool lags the packages | route: Projects tooling owner | None |
| L6 | Low | Proposal-ledger orphans missing from the AD-17 inventory | route: AD-17 cutover inventory owner | AD-17 writer cutover |
| L7 | Low | No test asserts exactly one keyed processor | route: DW entry for the cutover owner | AD-17 writer cutover |
| L8 | Low | Route-catalog authority is split | route: DW-63 owner | None |
| L9 | Low | One-type-per-file drift in the Server | route: Hygiene DW for the Server owner | None |
| L10 | Low | Raw signing key read from configuration | route: G-5 owner | Epic 8 release |
| L11 | Low | Writer cutover is roll-forward only; say so | autofix: State roll-forward only | AD-17 writer cutover |
| L12 | Low | Reverse-index rebuild source unnamed | autofix: Name the rebuild source | None |
| L13 | Low | Idempotency retention start for nonterminal tasks | autofix: Clarify the retention start | None |
| L14 | Low | NFR-3 qualifiers and NFR-7 retry guidance dropped | autofix: Restore the PRD qualifiers | Epic 8 release |
| L15 | Low | Assumption fallbacks A-1, A-2 and A-4 not carried | autofix: Carry the assumption fallbacks | Epic 8 release |
| L16 | Low | Freshness normalization casing not carried | autofix: Add the normalization row | None |
| L17 | Low | Frontmatter sources omit cited proposals | autofix: Complete the sources list | None |
| L18 | Low | Archived Projects' resolution eligibility not carried | autofix: Add the Archived rule | None |

Reviewed the final [architecture spine](ARCHITECTURE-SPINE.md) at Projects `31b35637` on `main`, SHA-256 `01b89a2f5e076a52b17f253417b7472ae85c0c7d26ae5a3c21c428e5868eb5fe`. This consolidation made no changes to the spine, the decision log or the November 20 report. Sources: [rubric](reviews/review-validate-rubric-2026-11-24.md) · [technology](reviews/review-validate-technology-2026-11-24.md) · [adversarial](reviews/review-validate-adversarial-2026-11-24.md) · [brownfield](reviews/review-validate-brownfield-2026-11-24.md) · [security](reviews/review-validate-security-2026-11-24.md) · [data-integrity](reviews/review-validate-data-integrity-2026-11-24.md) · [prd-reconcile](reviews/review-validate-prd-reconcile-2026-11-24.md) · [PRD](../../prds/prd-Hexalith.Projects-2026-05-24/prd.md).
