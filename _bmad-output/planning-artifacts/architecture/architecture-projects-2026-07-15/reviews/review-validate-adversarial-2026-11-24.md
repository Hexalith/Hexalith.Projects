# Reviewer Gate (Validate intent) — Adversarial incompatibility lens

**Target:** `ARCHITECTURE-SPINE.md` (status final, updated 2026-11-24, SHA-256 `01b89a2f5e076a52b17f253417b7472ae85c0c7d26ae5a3c21c428e5868eb5fe`, re-verified before writing). Repository HEAD `31b35637`. Inputs: the spine, `.memlog.md`, epics.md, the DW-63 spec, `src/`, `tests/`, and `references/Hexalith.EventStore` and `references/Hexalith.McpCli`. No earlier review or validation report was read.

**Verdict: FAIL.** Units that each follow the ADs as written can still put two Active Projects on one Folder (ADV-1). The trigger that three ADs use to switch behaviour, "the AD-17 writer cutover", is never defined as one event (ADV-2).

| Severity | Count |
| --- | --- |
| Critical | 1 |
| High | 5 |
| Medium | 4 |
| Low | 1 |

Line numbers refer to the spine unless a file is named.

---

## Critical

### ADV-1: Two mutation paths can install a Folder binding, and the AD-17 equivalence gate keeps the path that has no claim

- **Severity:** Critical
- **Spine anchor:** AD-17 Rule (L245, "`DomainResult` command behavior equivalent to current success/rejection/no-op semantics … command equivalence against the current fold"); AD-35 (L428–L432); AD-3 (L132, "A Folder is the Project Folder of at most one Active Project"); AD-24 (L321); AD-5 (L167, "Prevents: Boolean confirmation").
- **Units:** (A) the Story 7.1 SDK writer. It follows AD-17 command equivalence and registers one keyed `projects` processor for every command type. (B) Any non-task ingress to that processor: the AD-24 v1 `PUT /api/v1/projects/{projectId}/folder` route, which keeps direct submission for any slice it treats as not yet cut over, or a direct EventStore Gateway command from an authenticated human.
- **Evidence:**
  - The current fold replaces the Folder of an Active Project on a Boolean, with no exclusivity check: `if (!command.ReplacementConfirmed) … ProjectFolderReplacementRequiresConfirmation`, then it emits `ProjectFolderSet` (`src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs:267-330`, flag test at :308).
  - The v1 adapter forwards the Boolean: `replacementConfirmed = command.ReplacementConfirmed` (`src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs:156-164`).
  - The processor's per-command EventStore authorization layer is the allow-all validator in the shipped composition (`ProjectsServerServiceCollectionExtensions.cs:240-241`, used at `ProjectsDomainProcessor.cs:197`).
  - The EventStore Gateway accepts human-subject commands for any domain: `User.FindFirst("sub")` (`references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs:212`).
  - The post-cutover ban in AD-24 covers v1 routes only: "No v1 route then sends a command directly to the aggregate" (L321). AD-35 constrains claim *acquisition* and *task* commits: "Before a Project commit can establish an Active binding, the task must hold the exclusive claim" (L428). No AD requires the aggregate to refuse a binding command that carries no claim receipt.
- **Failure scenario:** After the 7.1 cutover, Project Q holds the AD-35 claim on Folder F through the Folder-first workflow. The SDK aggregate faithfully reproduces the current fold, as AD-17 requires. A v1 client, or any Gateway caller, then submits `SetProjectFolder{ProjectId=P, FolderId=F, replacementConfirmed=true}` for Active Project P. The command needs no task, claim or Confirmation Artifact. The aggregate emits `ProjectFolderSet`, and both P and Q are Active on F. `RestoreProject` and `ArchiveProject` show the same gap on the claim acquire and release sides.
- **Proposed resolution (tighten AD-17 and AD-35):**
  - "From writer cutover, the `projects` aggregate itself rejects every command that establishes, replaces, restores or releases a Folder binding unless the command carries an admitted task ID and the current AD-35 claim receipt (Project, task/step, generation) for that `(TenantId, FolderId)`. Ingress does not matter."
  - "AD-17 command equivalence covers historical replay and the commands whose semantics this spine does not change. The equivalence packet lists each intentional divergence (`SetProjectFolder` Boolean replacement, `RestoreProject` without a claim, `ArchiveProject` claim release, `CreateProject` without a Folder) and proves the target behaviour for it instead."
  - "Only the platform workflow workload identity may submit governed commands to the `projects` domain."
- **Acceptance check:** A persisted test at the cutover candidate sends `SetProjectFolder`, `RestoreProject` and `CreateProject` (with a Folder and no receipt) through the v1 route and through the raw Gateway command endpoint. Each is rejected with no event appended. The two-Project race still shows at most one Active holder.
- **Status:** CONFIRMED (aggregate, submitter, composition and Gateway source)

---

## High

### ADV-2: "The AD-17 writer cutover" is not one defined event, but three ADs switch on it

- **Severity:** High
- **Spine anchor:** AD-17 (L245: "atomically selecting one SDK writer", "exactly one keyed `projects` processor", but also "Before switching an affected writer slice"); AD-19 (L289, "Until the AD-17 writer cutover … At that cutover"); AD-24 (L321, "Until the AD-17 writer cutover … From that cutover"); AD-35 (L432, "At writer cutover"); boundary table (L51, "the first writer").
- **Units:** (A) Story 7.1, the "first writer". It qualifies only the create slice: epics.md:1694, "Before opening an affected writer slice, resolve its applicable legacy partial-record obligations". (B) The v1 adapter maintainer, and the later Stories 7.2–7.14, which deliver the other workflows one by one.
- **Evidence:** One keyed processor handles all eleven command types (`ProjectsServerModule.cs:24-75`, `ProjectsServerServiceCollectionExtensions.cs:172-177`), so selecting the SDK writer selects it for archive, Folder set, links and the rest at the same moment. AD-24 then requires that "a v1 mutation admits work only through the selected SDK writer's governed admission … with the AD-5/AD-19 Preview, confirmation and task outcomes". The governed archive, move and Folder-replacement workflows only arrive in Stories 7.4–7.14.
- **Failure scenario:** At 7.1, team A reads the cutover as global. It closes every v1 mutation route, which leaves v1 archive, link and Folder routes with no compliant admission, a mutation freeze the spine never states. Team B reads it per slice. It keeps v1 archive and `PUT /folder` on direct submission and leaves AD-35 claims and the AD-19 exception scoped to creation, so ADV-1 opens. Team C (Chatbot or FrontComposer) assumes the AD-19 400 mapping applies from 7.1. All three cite the same text.
- **Proposed resolution (amend AD-17):** "The writer cutover is one recorded event: the deployed selection of the SDK `projects` processor. At that event, and together: (1) every pre-cutover command ingress, including all v1 direct submissions to the aggregate or to Conversations, is fenced; (2) AD-35 claims are enforced for every key; (3) the AD-19 query policy changes (or see ADV-3). Workflows that are not yet delivered are slice *enablements* after the cutover. Until a slice is enabled, its v1 and canonical routes return a fixed nondisclosing `503` with `Retry`, no task and no command, and the release record lists the frozen routes."
- **Acceptance check:** The cutover record names one deployment SHA and the full list of fenced ingress points. A test at that SHA calls each of the 18 v1 mutation routes and gets either governed admission or the declared frozen response.
- **Status:** CONFIRMED (spine and epics text, processor registration)

### ADV-3: The AD-19 exception ends at the writer cutover, but the v4 read packet hard-codes the 404

- **Severity:** High
- **Spine anchor:** AD-19 bounded exception (L289); AD-17 read cutover (L262, reads "switch together, independently of writer migration"; L264, packet "bound to the application revision being selected" and "Only the approved cause-specific 503/200 pairs are admissible").
- **Units:** (A) the Epic 6 v4 requalification and the Primary Web `Supported` selection. (B) The Story 7.1 writer cutover, which under AD-19 must switch "one registered query-response policy" to 400 for invalid input to `GetProjectContext`, `GetConversationStartSetup` and `ExplainContextSelection`.
- **Evidence:** The 77-cell matrix asserts the exception: `int expected = operation is "context" or "explain" or "conversation-start" ? 404 : 400;` with legacy expected `BadRequest` (`tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.CutoverMatrix.cs:579-580`). The policy is in `ProjectsQueryResponsePolicy.cs:21-30`.
- **Failure scenario:**
  - At the cutover SHA, the new policy returns 400, so the invalid-cursor cells for those three reads fail. AD-17 requires a passing packet bound to the selected revision, so Primary Web drops to its default, `Unavailable`, at the moment writes go live.
  - Alternatively, someone edits the matrix expectation at the cutover SHA without a new normalization revision, which silently changes the active v4 semantics.
  - Either way, the reads change at the writer migration, against L262. Chatbot clients that map 404 to "denied or absent" begin to receive 400 for the same request, with no version or notice.
  - Separately, the matrix already admits a legacy-400 / supported-404 pair, which the literal L264 "only … 503/200 pairs" does not list.
- **Proposed resolution:** Tie the end of the exception to a read event, not the writer event. "The AD-19 exception ends with a new normalization revision (v5) that requalifies the 77 cells and records the consumer notice. It is independent of the writer cutover." Then list the legacy-400 / supported-404 invalid-input pair as an approved pair under v4.
- **Acceptance check:** The v4 packet's approved-pair list includes the invalid-cursor pair. The cutover SHA either still returns 404 for the three reads, or ships with a v5 packet that passes.
- **Status:** CONFIRMED (matrix and policy source)

### ADV-4: The AD-29 four-read inventory is not enforced, because decoration is the consent and an unlisted operation only produces a report

- **Severity:** High
- **Spine anchor:** AD-29 (L353, "A cataloged operation missing a release-inventory row produces a diagnostic and remains subject to the same runtime checks"; L355, "Audit, export, task control and writes are outside that four-operation inventory"); McpCli disposition note (L43, "Readiness/setup/task-control and audit operations must not be inferred from those reads"); AD-16 (L239).
- **Units:** (A) The Projects Contracts owner, who decorated the blocked Story 8.1 preview. (B) McpCli enrollment of the next Projects.Contracts version (G-3).
- **Evidence:**
  - `[HexalithQuery("Read committed audit facts for one Project…", Name = "audit"…)]` (`src/Hexalith.Projects.Contracts/Queries/GetProjectAuditQuery.cs:10-11`) and `[HexalithQuery(… Name = "task-status" …)]` (`GetProjectTaskStatusQuery.cs:6-7`). Both were added in commit `95f2984c` ("add the Story 8.1 audit and task status preview").
  - McpCli: "Decorating a type is the owning maintainer's consent to expose it … without human confirmation" (`references/Hexalith.McpCli/src/Hexalith.McpCli.Abstractions/HexalithQueryAttribute.cs:4-6`). Also "an operation no inventory row mentions is reported …, never failed" (`references/Hexalith.McpCli/CLAUDE.md:84`).
- **Failure scenario:** The next Contracts release is enrolled to pick up any fix. McpCli then discovers `projects.audit` and `projects.task-status`, assuming their Operation Shapes are already covered by vectors (an uncovered shape fails). Agents get the FR-21 audit read, including durable inspection-audit facts, and task status. Only a stderr "reported" line flags it. Each unit followed its own rules.
- **Proposed resolution (tighten AD-29):** "Decoration is enrollment. In every Contracts version, the set of `[HexalithQuery]` and `[HexalithCommand]` types equals the approved AD-29 inventory for that version. A Projects-owned test fails the build on any difference. Adding an operation needs an AD-29 inventory amendment and G-3 evidence before the decoration merges. Preview operations stay undecorated."
- **Acceptance check:** A Contracts test reflects over the assembly and asserts exactly the approved operation names. It fails on the current tree until the two preview decorations are removed or approved.
- **Status:** CONFIRMED (decorations and McpCli policy). Whether discovery is automatic depends on vector shape coverage.

### ADV-5: Durable inspection audit is stored as primary state in the rebuildable read-model store

- **Severity:** High
- **Spine anchor:** AD-24 (L321, "writes durable state only as the AD-33 inspection audit through the platform read-model store"); AD-14 (L225, "EventStore storage owns bounded list, detail, reference, audit … views", "Persisted end-state, … rebuild … evidence are mandatory"); AD-26 (L335, 365-day retention, "Descriptive-metadata inspection events are durable audit"); AD-17 / Story 8.10 (L266, "rebuild a quarantined Tenant into a fresh generation, verify list/detail/audit against authoritative replay").
- **Units:** (A) The AD-24 v1 adapter and the Epic 6 supported reads, which write inspection audit facts directly. (B) Story 8.10 generation rebuild and Story 8.1 audit retention.
- **Evidence:**
  - `ProjectReadAudit` writes `projects:read-audit:v1:{surrogate}` and `projects:audit-index:v1:{hash(tenant,project)}:{page}` into `statestore` through `ReadModelWritePolicy.UpdateAsync`. No event is appended (`src/Hexalith.Projects.Server/Queries/ProjectReadAudit.cs:14,34-36,122`; `ProjectReadModelKeys.cs:7`).
  - The audit query merges these facts with replayed history (`ProjectOperatorQueryExecutor.cs:65,87`).
  - Story 8.10 requires parity against replay: epics.md:2888.
- **Failure scenario:** An operator runs the 8.10 runbook. Inspection facts cannot be derived from replay, so the parity check fails, or the fresh generation omits them and the reader switch drops audit rows that AD-26 must keep for 365 days. The index keys are not scoped to a generation, so a "fresh" generation either shares them, which is not fresh, or loses them. A routine AD-14 rebuild that clears the store has the same effect.
- **Proposed resolution (tighten AD-26 and AD-14):** "Every durable audit fact, including inspection, is appended to a platform-owned audit record stream with its own retention. Audit views are projections of that stream and are rebuildable. Neither the adapter nor a query handler writes primary audit state into the read-model store." If that is not possible before cutover, state an explicit carve-out: inspection-audit keys are excluded from rebuild and generation switches, carry 365-day retention, and are verified by count instead of replay.
- **Acceptance check:** The 8.10 rebuild fixture holds inspection facts. After the rebuild and switch, the audit query returns the same inspection facts, and replay parity passes.
- **Status:** CONFIRMED (source and epics)

### ADV-6: The shipped single writer runs with allow-all EventStore authorization, and no gate keeps that out of the first writer

- **Severity:** High
- **Spine anchor:** AD-20 (L297, "Allow-all implementations exist only in explicit test composition"); Structural Seed (L487, which records the violation as current state); boundary table (L51); G-5 (L557).
- **Units:** (A) The DW-63 host, accepted under its own frozen criteria. (B) The Story 7.1 writer cutover, whose gate requires only "accepted DW-63" (epics.md:1694).
- **Evidence:** `AddProjectsDomainService` calls `AddProjectsServerRuntimeInfrastructure`, which registers `AllowingProjectEventStoreAuthorizationValidator` and `AllowingProjectDaprPolicyEvidenceProvider` (`ProjectsServerServiceCollectionExtensions.cs:60,240-243`). The validator returns `Allowed("eventstore_validator_test_allow")` (`src/Hexalith.Projects/Authorization/AllowingProjectEventStoreAuthorizationValidator.cs:22`) and is the writer's authorization layer (`ProjectsDomainProcessor.cs:197`). The memlog routes the fix to 8.8-P1, after the first writer.
- **Failure scenario:** DW-63 is accepted and 7.1 cuts over. The writer's EventStore authorization layer approves every command, so ADV-1's direct Gateway path is limited only by Gateway policy, and the AD-33 matrix is not enforced at the writer.
- **Proposed resolution (tighten AD-20 and the 7.1 gate):** "Neither DW-63 acceptance nor writer cutover is admissible while any `Allowing*` validator or evidence provider can be resolved outside explicit test composition. A production-capable host fails at startup if one is registered."
- **Acceptance check:** A host-composition test resolves `IProjectEventStoreAuthorizationValidator` and `IProjectDaprPolicyEvidenceProvider` from `Program`'s builder and asserts they are not allow-all types. Startup with an allow-all registration fails.
- **Status:** CONFIRMED

---

## Medium

### ADV-7: Three idempotency layers with different scopes, and nothing carries keys across the cutover

- **Severity:** Medium
- **Spine anchor:** AD-5 (L167, scope `(Tenant, actor, operation, key)`, at least 30 days); AD-18 (L283, platform-generated, "caller-selected aggregate identity" prevented); AD-17 (L245, "inventory … in-flight work"); AD-24 (L321).
- **Units:** (A) The v1 create and mutation routes before the cutover. (B) Governed admission after the cutover (Story 7.1 and later).
- **Evidence:**
  - v1 create mints `new ProjectId(Guid.NewGuid().ToString("N"))` or accepts `body.ProjectId` (`ProjectsDomainServiceEndpoints.cs:650-652`), and `X-Hexalith-Task-Id` is caller-supplied (:556).
  - The submitter sends the caller's `Idempotency-Key` as `MessageId` (`EventStoreProjectCommandSubmitter.cs:54,301`). EventStore says "messageId is command/status/event identity, not an opaque idempotency key" (`references/Hexalith.EventStore/src/Hexalith.EventStore/OpenApi/CommandExampleTransformer.cs:38`), and its own replay tier `Mutation` keeps results for 24 hours (`IdempotencyReplayRetentionTier.cs:11`).
  - The aggregate keeps a third copy of idempotency state, per stream (`ProjectAggregate.cs:53,116,170,224,278`).
- **Failure scenario:** A Chatbot v1 create with key K loses its reply just before the cutover. After the fence, its retry with K reaches governed admission, finds no `(Tenant, actor, project.create, K)` record, and admits a new task with a new ULID: two Projects. A v1 client that supplied `projectId` then polls `GET /api/v1/projects/{its id}` and gets 404 indefinitely.
- **Proposed resolution:**
  - "The platform AD-5 admission record is the only authority for equivalence. Aggregate fingerprints are defence in depth only."
  - "At writer cutover, every v1 key seen in the previous 30 days is imported as an AD-5 record that resolves to its original outcome, or is fenced to return the AD-19 nondisclosing 409."
  - "After the cutover, v1 create rejects a caller-supplied `projectId` and `X-Hexalith-Task-Id` with `400`, and returns the platform task location."
- **Acceptance check:** A cross-cutover retry fixture with a lost reply produces exactly one Project. A v1 create with `projectId` returns 400 after the cutover.
- **Status:** PLAUSIBLE. The code paths are confirmed. Whether EventStore 5.5.1 deduplicates on `MessageId` across aggregates was not verified.

### ADV-8: No defined post-cutover outcome for v1 routes that are confirmation APIs

- **Severity:** Medium
- **Spine anchor:** AD-13 (L217, "Consumption occurs only inside the Projects-owned confirmation component (no confirm API)"); AD-24 (L321); AD-17 (L262, "Preserve retained v1 REST and client signatures"); AD-19 (L289); AD-31 (L371).
- **Units:** (A) The v1 adapter after the cutover. (B) Stories 7.4, 7.6, 7.11, 7.12, 7.13 and 7.14 (confirmation-required workflows).
- **Evidence:** v1 maps `POST …/proposals/confirm`, `POST …/conversations/{id}/resolution/confirm`, `POST …/move`, `DELETE …/conversations/{id}`, `PUT …/folder` (Boolean `replacementConfirmed`), `POST …/archive` and `POST …/restore` (`ProjectsDomainServiceEndpoints.cs:292,344,364,386,406,521,532`). A v1 request can never carry a Confirmation Artifact. The memlog itself notes that v1 shapes "would have required reproducing the Boolean confirmations AD-5 forbids" (memlog L101).
- **Failure scenario:** After the cutover, one implementer returns `409/RenewPreview`, another `400`, and a third admits a task that ends `Rejected`. Chatbot and Web clients branch on three different shapes for the same v1 call, and audit rows differ.
- **Proposed resolution (amend AD-24):** Give a per-class table for v1 after the cutover:
  - task-only routes (no-Folder create, Setup): `202` plus task;
  - association routes: `409` with `RequestPreview` in the v1 problem shape, no task and no command;
  - confirmation-required routes and the Boolean flag: the same `409` with `RequestPreview`. A Boolean never admits work.
  - Audit: one denial audit row in each refusal case.
- **Acceptance check:** A contract test after the cutover hits each of the seven routes and asserts the declared status, body and no-effect behaviour.
- **Status:** CONFIRMED (routes); the gap is in the spine text.

### ADV-9: The AD-19 exception is coded as a deny-list of six types, so every new query defaults to 404

- **Severity:** Medium
- **Spine anchor:** AD-19 (L289, the exception names exactly four queries; "invalid input returns `400`" for the rest); Structural Seed (L487).
- **Units:** (A) `ProjectsQueryResponsePolicy`, from DW-63. (B) Any new query added before the cutover, for example Story 8.2's `safe-diagnostic-export` or an Epic 7 Preview query.
- **Evidence:** `if (query.QueryType is not (ListProjects … ResolveProjectFromAttachments)) return Results.NotFound(result);` (`src/Hexalith.Projects.Server/Queries/ProjectsQueryResponsePolicy.cs:21-26`).
- **Failure scenario:** The Story 8.2 author follows AD-19 and expects 400 for a malformed export request. The shared policy returns 404, so a CLI usage error looks like "not found", and two parity suites disagree.
- **Proposed resolution:** "The exception is an allow-list of the four named types. Every other registered Projects query type uses the AD-19 mapping."
- **Acceptance check:** A test enumerates the registered `IDomainQueryHandler` query types. Every type outside the four returns 400 for `InvalidEnvelope` or `InvalidCursor`.
- **Status:** CONFIRMED

### ADV-10: Concurrent tasks on one Project have no defined outcome for a Project-version conflict

- **Severity:** Medium
- **Spine anchor:** AD-15 (L233, "Commands serialize per Project"); AD-12 (L211); AD-35 (L430, "Commit the binding against the expected Project version"); AD-5 and AD-13 (Preview bound to "current versions").
- **Units:** (A) The Story 7.13 archive task, admitted against Preview version N. (B) The Story 7.2 Setup task, which commits N+1 first.
- **Failure scenario:** The archive commit meets an expected-version conflict. One team ends the task `Rejected`, one retries from the checkpoint and archives against a state the actor never previewed, and one goes to `NeedsAttention`. Task truth and audit diverge across Epic 7.
- **Proposed resolution (add to AD-12):** "On a Project-commit version conflict, re-evaluate from the last checkpoint. A confirmation-bound task whose Preview digest no longer matches ends `Rejected` with `RenewPreview`. A task-only task retries within the AD-27 budget. Neither becomes `NeedsAttention` unless the outcome is unknown."
- **Acceptance check:** Persisted race fixtures for archive versus Setup and link versus link show the declared terminal states and audit rows.
- **Status:** PLAUSIBLE

---

## Low

### ADV-11: The per-replica proposal-confirmation ledger creates partial records that AD-17 does not list

- **Severity:** Low
- **Spine anchor:** AD-24 (L321, "per-replica proposal-confirmation idempotency ledger is retained v1 behavior"); AD-17 (L245, partial-record inventory).
- **Evidence:** `POST /api/v1/projects/proposals/confirm` uses both `IProjectCommandSubmitter` and `IProjectConversationAssignmentDirectory` with an in-memory ledger (`ProjectsDomainServiceEndpoints.cs:292-322`; `ProjectsServerServiceCollectionExtensions.cs:110`).
- **Failure scenario:** A retry that lands on another replica creates a second Project, or links membership without the create. These records never appear in the cutover partial-record inventory.
- **Proposed resolution:** Add "v1 proposal-confirm orphans (Project without membership, or a duplicate Project for one proposal)" to the AD-17 inventory classes, with a repair disposition.
- **Acceptance check:** The cutover inventory report has that class with a count.
- **Status:** PLAUSIBLE

---

## Checked and held up

- AD-24 route count: exactly 25 `/api/v1/projects` method-and-path pairs are mapped (`ProjectsDomainServiceEndpoints.cs:84-541`).
- AD-24 composition: `Program.cs` is the SDK host plus one `MapProjectsHumanEndpoints` call, which applies `HumanPolicy` (`ProjectsServerServiceCollectionExtensions.cs:73-79`).
- AD-24 pre-cutover paths: Project commands go through `EventStoreProjectCommandSubmitter`, and membership commands go through `ConversationsProjectConversationAssignmentDirectory`, as the amendment states.
- AD-19 exception bodies: the three context handlers return `safe-denial` for every denial, and the Chatbot authority query returns `Forbidden` uniformly, so the 404 bodies are no existence oracle.
- AD-17 single keyed processor: one keyed `IDomainProcessor` and one matching `IAsyncAggregateReplay` are registered (`ProjectsServerServiceCollectionExtensions.cs:172-177`).
- Stack claim: DW-63 builds in source mode only. The DW-63 spec records the `CS0246` failure against 5.5.1 and the required EventStore release and Builds pin.
- AD-10 and Folders: Folders currently holds no Project binding (no `ProjectId` in `references/Hexalith.Folders/src`), so it is not a second owner of the binding today.
