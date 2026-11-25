# Reviewer Gate (Finalize after Update): adversarial incompatibility review, 2026-11-25

**Target:** `ARCHITECTURE-SPINE.md` (SHA-256 865f8eed3622dfcb…, updated 2026-11-25, AD-1 to AD-35), read against `.memlog.md` lines 110 to 173, `src/` on Projects HEAD 31b35637, and root submodule sources (`references/Hexalith.EventStore` working tree at ae1143bdc, `references/Hexalith.Folders` at d56e868f, `references/Hexalith.McpCli` at c07d09cc). Earlier review and validation reports were not read.

**Lens:** build two units one level down that each obey every AD to the letter and still come out incompatible.

**Verdict: PASS WITH FINDINGS.** The four HIGH holes must be closed before re-finalization. Each one sits in a rule added today.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 4 |
| Medium | 5 |
| Low | 3 |

Status key: **CONFIRMED** means the clash follows from the spine text, and from code where code exists. **PLAUSIBLE** means it depends on an implementation choice the spine leaves open.

---

## HIGH

### ADV-1: The writer cutover is not ordered against 8.8-P1 and G-5, but the post-cutover writer needs both

- **Anchor:** AD-17 line 251; AD-20 line 292; AD-35 line 425; AD-12 line 215
- **Status:** CONFIRMED

**Evidence**

- AD-17 (line 251): the cutover happens "after Stories 7.1–7.15 each qualify on the SDK writer in test composition … and before the Epic 8 release."
- AD-20 (line 292): allow-all leaves "before the writer cutover." The other clauses start later: "From 8.8-P1 the real validator runs on G-5 evidence … and the writer accepts a Projects command only with that admission's evidence, whatever the ingress." Token forwarding continues "Until G-5 supplies platform-issued … delegated credentials …, at the latest by 8.8-P1, the retained adapter may forward the inbound human bearer token."
- AD-35 (line 425): "From the writer cutover, the `projects` aggregate accepts a command that establishes, replaces, restores or releases a binding only through governed admission with the current receipt, whatever the ingress."
- AD-12 (line 215): "Later owner steps use task-bounded G-5 delegated credentials, never the interactive token."
- `_bmad-output/planning-artifacts/epics.md:2770`: 8.8-P1 is "blocked by Stories 8.3–8.5, G-4, G-5". Nothing orders it before the cutover.
- In code:
  - `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:169` registers `DenyAllProjectEventStoreAuthorizationValidator` as the fallback.
  - Lines 240–241 of the same file override it with `AllowingProjectEventStoreAuthorizationValidator`.
  - `ProjectsDomainProcessor.cs:197-215` rejects every command unless the validator returns `Allowed`.
  - The SDK `EventStoreAggregate<TState>` (`references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs`) has no authorization hook. Conventions line 435 requires a pure `Handle`.
  - EventStore's command API only checks claims-based RBAC, and enforces even that only when the caller carries the claims: `references/Hexalith.EventStore/src/Hexalith.EventStore/Authorization/ClaimsRbacValidator.cs:40` "only enforce if user has domain claims" and `:52` "only enforce if user has permission claims".

**Failure scenario**

Three units are built:

1. The Server owner moves allow-all out before the cutover, as AD-20 says.
2. The cutover owner deploys the SDK writer once Stories 7.1–7.15 qualify, as AD-17 says.
3. The 8.8-P1 owner delivers admission evidence and G-5 credentials later, as epics.md says.

In the window between the cutover and 8.8-P1, each of the following happens:

- **The binding fence has no mechanism.** "Only through governed admission with the current receipt" has nothing to verify before 8.8-P1's admission evidence exists. The Story 7.1 implementer must either reject every binding command, so the writer cannot create, archive or replace, or invent a Projects-local admission marker that 8.8-P1 later replaces.
- **Other commands are open.** Setup, File and Memory link/unlink, and Grant/Revoke are not binding commands. Submitted straight to the EventStore command API, they reach a pure `Handle` that has only claims-RBAC in front of it. That bypasses Folder-derived policy, Selection Evidence and confirmation (AD-5, AD-33).
- **Durable owner steps have no credential.** Folders provisioning for every create needs G-5 delegated credentials. Forwarding is allowed only to "the retained adapter", which has no inbound request at worker retry time. Every create therefore either waits forever in `WaitingForDependency` or reuses the interactive token, against AD-12.

The C1 fix (moving allow-all out) protects only the legacy processor, which this same cutover retires.

**Proposed fix.** Append to AD-17 **Writer cutover**:

> "The cutover also requires, in the target environment, the AD-20 admission-evidence check on every Projects command (8.8-P1's validator and evidence binding) and G-5 task-bounded delegated credentials for durable owner steps; without both the cutover does not run."

In AD-20, change "From 8.8-P1 the real validator runs" to:

> "From the writer cutover, and at the latest by 8.8-P1, the real validator runs"

### ADV-2: A claim taken by a create that is abandoned for actor denial can never be released

- **Anchor:** AD-35 line 425; AD-4 line 142; AD-12 line 215; AD-8 line 189
- **Status:** CONFIRMED (textual)

**Evidence**

- AD-35: "before releasing an abandoned reservation, the task commits a terminal creation-abandoned event to that Project stream" and "A task ending `Rejected`, `Failed` or `Cancelled` with an uncommitted claim runs that fenced release as a Projects-internal step or stays `NeedsAttention`." Also: "neither a worker timeout nor a read-model observation authorizes release."
- AD-12: "Before each Project commit, Projects rechecks the original actor's current Tenant membership and action authority: a confirmed denial ends the task `Rejected` (`NeedsAttention` under AD-8 once a Folder exists)."
- AD-4: from `NeedsAttention`, "a confirmed denial takes the task directly to terminal `Rejected` with one durable audit outcome and no new owner mutation". Terminal states "are immutable" and "Reconciliation admits no new intent, task or Confirmation Artifact."

**Failure scenario**

1. A create task provisions or validates Folder F and acquires F's claim.
2. Before the creation commit, the actor loses Tenant membership. The AD-12 recheck denies, and because a Folder exists the task goes to `NeedsAttention`.
3. The Administrator reconciles. AD-4 takes the task "directly" to terminal `Rejected`.

The AD-35 release would need the creation-abandoned commit first. That is a Project commit, so AD-12 rechecks the same denied actor and blocks it.

Two units each follow the letter:

- The reconciliation implementer (Stories 7.15 and 8.1) goes directly to terminal `Rejected`.
- The claim implementer (Story 7.1) expects the release step to run.

The result: F's claim is held forever by a Project that never existed. No task can be admitted to clear it. For a supplied existing Folder, that Folder is permanently unbindable, even through the FR-8 Administrator workflow.

**Proposed fix.** Add to AD-35:

> "The creation-abandoned commit and the conditional claim release are Projects-internal compensation steps. They are exempt from the AD-12 original-actor recheck and run before any terminal transition, including AD-4 reconciliation to `Rejected`. A task cannot become `Rejected`, `Failed` or `Cancelled` while it holds an uncommitted claim."

### ADV-3: The expected-version conflict cannot be expressed on EventStore and collides with commit-step dedupe

- **Anchor:** AD-12 line 215; AD-15 line 237; AD-35 line 425
- **Status:** CONFIRMED for the platform semantics; PLAUSIBLE for the double-holder outcome

**Evidence**

- AD-12: "Projects' own commit is a saga step keyed by task and step, independent of MessageId, so a redelivery or duplicate returns its original outcome." Also: "`Handle` rejects a mismatch without effect, ending the task `Rejected` … with no automatic retry."
- AD-15: "Project version is the stream sequence (`projectVersion`), which authorized callers' rejection events also advance."
- In EventStore:
  - `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs:54,57,80,98`: a rejection is "Events.Count > 0 && Events[0] is IRejectionEvent", and the only zero-event result is `NoOp`.
  - `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:1743-1756` stores rejection events and advances `NewSequenceNumber`.

**Failure scenario**

No EventStore result is both a "rejection" and "without effect":

- A `Rejection` persists an event and advances `projectVersion`. That invalidates every outstanding Preview and conflicts every other in-flight task.
- A `NoOp` looks exactly like an idempotent duplicate.

The spine also never says where the task/step key lives, or that it is checked before the version. For a duplicate of a committed step the expected version is stale by definition, so the two AD-12 rules give opposite answers.

A concrete path:

1. A create commit's reply is lost and the commit is redelivered.
2. `Handle` applies the version check (as the letter says) and returns a conflict.
3. The task ends `Rejected`.
4. AD-35 says a `Rejected` task "with an uncommitted claim runs that fenced release". The conditional release names the right Project and generation, so it succeeds.

The Active Project now holds F with no claim. A second Project acquires F, giving two Active holders. That is the AD-35 invariant broken.

**Proposed fix.** Replace the AD-12 sentence with:

> "Each Project commit carries its task/step key in the command and its events. Before the expected-version check, `Handle` returns the recorded outcome of a step already committed in the stream. A version mismatch is a persisted rejection event (it advances `projectVersion`, AD-15) mapped to `RenewPreview` or a retryable conflict code. A task never treats a commit outcome as uncommitted until it has queried the Project stream for its task/step."

### ADV-4: The ChatBot authority exception has no admission path, no matrix row and no pure-`Handle` form, and non-owners can advance `projectVersion`

- **Anchor:** AD-3 line 136; classification line 502; AD-33 lines 384–403; AD-17 line 249; AD-15 line 237; AD-20 line 292
- **Status:** CONFIRMED

**Evidence**

- Classification row (line 502) gives only "AD-3 exception; never sufficient without current Folder authority". It names no admission contract.
- The AD-33 matrix has no Grant/Revoke row, although the memlog H2 decision (memlog line 160) says both commands "join … the AD-33 matrix".
- AD-17 (line 249) requires "`DomainResult` behavior equivalent to current success/rejection/no-op semantics … (including … the ChatBot authority events)". Conventions (line 435) require "pure `Handle` and `Apply`".
- Today's processor:
  - `src/Hexalith.Projects.Server/ProjectsDomainProcessor.cs:168-171` routes Grant/Revoke around the authorization validator.
  - Lines 240–243 of the same file make a network owner check (`ProjectOwnerBootstrapVerifier.cs:15-55`, which queries Tenants and Parties with a 30 s timeout). On failure it returns `DomainResult.Rejection([new ProjectChatBotAuthorityRejected("owner_unavailable")])`, which is persisted on the Project stream.
- The H10 rationale (memlog line 157) depends on "unauthorized commands stop at governed admission".

**Failure scenario**

- **The SDK writer cannot reproduce the owner check.** A pure `Handle` cannot make that network call, so the Story 7.x writer either:
  - drops the check, letting any principal with EventStore command access issue grants, or
  - keeps I/O in `Handle`, against the conventions.
- **Non-owners advance the version.** Each failed grant from a non-owner appends a rejection event. Anyone can therefore keep `projectVersion` moving: every Preview on that Project needs `RenewPreview` and every in-flight task ends `Rejected` on conflict. The "authorized callers'" premise in AD-15 is false for this family today.
- **The Chatbot path breaks at 8.8-P1.** AD-20 says "the writer accepts a Projects command only with that admission's evidence, whatever the ingress". There is no admission class for Grant/Revoke, so the external Chatbot's direct grant path breaks at that point, unless someone adds an undocumented bypass.

**Proposed fix.** Classification row for Grant/Revoke becomes:

> "Delegated-operation admission: synchronous governed admission (no task, no artifact) that verifies current Tenants owner and Parties human evidence before submission; the writer accepts it only with that admission's evidence; `Handle` is pure."

Add an AD-33 row naming the issuing role (Tenant owner, human, interactive) and "denied for every other column".

Add to AD-15:

> "A command that fails admission appends nothing to the Project stream."

---

## MEDIUM

### ADV-5: AD-31 promises the historical v1 create shape "throughout v1", but AD-18 and canonical create reject it after the cutover

- **Anchor:** AD-31 line 366; AD-18 line 278; AD-5 line 171
- **Status:** CONFIRMED

**Evidence**

- AD-31 makes the historical request (`name`, optional `projectId`, …) "a v1 compatibility shape, accepted and readable throughout v1". It also says canonical create "requires" `projectMetadata.metadataClass` and "the v1 adapter passes its caller's value through".
- AD-18: "From the cutover, a supplied `projectId` or `X-Hexalith-Task-Id` on v1 create returns `400` naming the field."
- AD-5: "v1 keys seen in the prior 30 days are imported as records resolving to their original outcome."
- In code (`src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`):
  - Lines 614–626: the historical shape carries no class and skips class validation.
  - Lines 648–653: the server mints GUID-N or takes the caller's `projectId`.
  - Line 556: `X-Hexalith-Task-Id` is read from the caller.
- The generated client always exposes that header: `src/Hexalith.Projects.Client/Generated/HexalithProjectsClient.g.cs:58`.

**Failure scenario**

After the cutover, the adapter passes a historical request through with no class. Governed admission (Story 7.1) rejects it with `400 metadataClass`, so "accepted throughout v1" is false. The only alternative, the adapter supplying a class, is inference, which AD-31 forbids.

Retries break as well. An exact retry of a pre-cutover create that carried `projectId` or `X-Hexalith-Task-Id` hits the AD-18 `400` before the AD-5 import can return "its original outcome". Imported v1 keys also have no platform task for the AD-19 "`202` with that task location".

**Proposed fix.** Pick one rule and write it into AD-31 and AD-18. For example:

> "From the writer cutover the historical shape is accepted only without `projectId`; the v1 adapter submits it with no class, and governed admission accepts a classless v1-adapter create (the class stays unpersisted). An equivalent retry under an imported v1 key resolves before field validation and returns the original v1 `202` body."

Otherwise, retire the historical shape at the cutover through the 25-pair table and drop "throughout v1".

### ADV-6: The post-cutover v1 `202` is task-only, but a v1 caller can neither poll the task nor bind a version

- **Anchor:** AD-24 line 316; AD-33 lines 400 and 405; AD-12 line 215; AD-19 line 284
- **Status:** CONFIRMED

**Evidence**

- AD-24: "create and Setup return `202` with the admitted task" and "The adapter never … gains a route".
- None of the 25 mapped pairs is a task route (`ProjectsDomainServiceEndpoints.cs:84-532`).
- The v1 `AcceptedCommand` is `additionalProperties: false` with required `acceptedAt`, `correlationId`, `taskId` and `status`, and no `projectId` (`src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml:2702-2710`). Today's `202` sends no `Location` (`ProjectsDomainServiceEndpoints.cs:1913-1922`).
- AD-33 (line 400) lets Project Users see pre-activation task status only "through Chatbot", and line 405 withholds the reserved Project identity until completion.
- The v1 contract has no `If-Match` or expected-version input anywhere in the YAML (zero matches).

**Failure scenario**

- **Create is unobservable.** A v1 Project User's create returns a `taskId` they cannot poll on v1 and are denied on Gateway task status, which is Chatbot-only for that role. They also never learn the ProjectId.
- **Setup can fail silently.** A v1 Setup task's expected version is whatever the adapter reads at admission. Any concurrent append (a ChatBot grant, or any rejection event, see ADV-3 and ADV-4) ends it `Rejected` under AD-12 with no automatic retry. The v1 caller holds a `202` and has no way to learn that the update did not apply.

**Proposed fix.** Add to AD-24:

> "From the cutover, the v1 `202` carries the platform `taskId` and a `Location` to the platform task-status query; the admitting actor may poll its own task through that query on any authenticated surface (AD-33 pre-activation row: 'own tasks through Chatbot or the retained v1 adapter's task location'). A v1 Setup task takes its expected version from the authoritative stream head at admission, and a conflict outcome is visible on that task."

### ADV-7: "`409 RequestPreview` in the v1 problem shape" has no v1 vocabulary

- **Anchor:** AD-24 line 316; AD-16 line 243
- **Status:** CONFIRMED

**Evidence**

The frozen v1 YAML documents `409` on every mutation route only as `IdempotencyConflict` ("Same idempotency key with non-equivalent tenant-scoped payload semantics"). Locations in `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml`:

- the File link route, from line 1011
- the response at lines 2071–2079
- the example at lines 2146–2157, with `clientAction: revise_request`

The closed enums have no entry that fits a Preview requirement:

- `CanonicalErrorCategory` (lines 3611–3626)
- `clientAction` (lines 2673–2681)

AD-16 freezes the YAML to "compatibility fixes only".

**Failure scenario**

Two implementations both satisfy "the v1 problem shape":

1. **Adapter A reuses `idempotency_conflict`.** Generated `Projects.Client` consumers follow `revise_request` and retry with a fresh key. Every retry writes another AD-24 denial row, and equivalent-retry dedupe (AD-26) does not apply to new keys.
2. **Adapter B adds a category.** That breaks strict enum deserialization in the generated client and violates the frozen YAML.

**Proposed fix.** Add to AD-24:

> "The `409` body is a v1 `ProblemDetails` with `category: validation_error`, `code: request_preview`, `clientAction: revise_request`, `retryable: false` and `details.recoveryAction: RequestPreview`; the frozen YAML gains this documented `409` as a compatibility fix, adding no enum value."

### ADV-8: The inspection-audit carve-out names only v1, but supported SDK handlers write the same durable keys, and the denial rows have no named home

- **Anchor:** AD-14 line 229; AD-24 line 316; AD-26 line 330; AD-33 line 407
- **Status:** CONFIRMED

**Evidence**

- AD-14: "Carve-out: v1 list/open inspection audit (`ProjectReadAudit` facts, audit-index pages) is durable primary state."
- AD-24 says the handler home is `Hexalith.Projects` for "every `IDomainQueryHandler` … all before the writer cutover", and that the adapter's "only durable writes are those denial rows and the AD-14 inspection audit".
- In code, the supported SDK handlers write the same audit through `ProjectReadAudit`, under prefix `projects:read-audit:v1:` (`src/Hexalith.Projects.Server/Queries/ProjectReadAudit.cs:11-14,26-36`):
  - `ListProjectsQueryHandler` and `GetProjectQueryHandler` (`Queries/ListProjectsQueryHandler.cs:8`, `Queries/GetProjectQueryHandler.cs:8`) do it via `ProjectReadQueryExecutor.cs:29,148,155,225,451`.
  - The operator and quarantine path does it via `ProjectOperatorQueryExecutor.cs:17,205`.
- Memlog H16 (line 169) records that no platform audit stream exists. The adapter "never appends events".

**Failure scenario**

- **The writer has no legal home.** The handler move pulls `ProjectReadAudit` into `Hexalith.Projects`, so an SDK query handler becomes a durable writer, which AD-24 attributes to the adapter alone. When the adapter retires under NFR-10, one reading retires the supported path's mandatory audit along with it (AD-33 line 409, FR-22 quarantine at line 407).
- **Denial rows can be lost.** The post-cutover `409` denial rows have no specified store. Written to the read-model store under any other prefix, they fall outside the carve-out, Story 8.10's generation switch drops them, and AD-26's 365-day retention of authorization denials fails.

**Proposed fix.** Replace the AD-14 carve-out with:

> "Carve-out: every durable Projects audit fact the host writes to the read-model store (inspection, quarantined-inventory inspection and safe-denial facts from any query handler, and the AD-24 adapter's mutation-denial rows) uses one Projects audit key namespace written by one `Hexalith.Projects` audit writer, is excluded from rebuilds and generation switches, retained under AD-26 and verified by count."

In AD-24, change "its only durable writes are …" to "it writes durably only through that audit writer".

### ADV-9: Claim "generation" and "holder" each have two readings

- **Anchor:** AD-35 lines 423, 425 and 427
- **Status:** PLAUSIBLE

**Evidence**

- AD-35 (line 423): "its generation is the stream version, and the durable receipt names Project, task/step and generation". It also says: "A Project commit establishes an Active binding only while its task holds that Folder's claim."
- Line 425: "A conditional release names the exact Project and generation."
- Line 427: at the cutover, "reserve each occupied key for its incumbent". A reconstructed incumbent has no task and no receipt in its pre-cutover Project stream.
- EventStore persists rejection events, so each competing acquire advances the claim stream's version (see ADV-3 evidence).

**Failure scenario**

- **Generation.** The claim aggregate (Story 7.1) can read "generation is the stream version" as the current version. Then any competitor's failed acquire makes the holder's own archive release (Story 7.13) mismatch, and the claim sticks. A squatter can do this deliberately.
- **Holder.** For a legacy incumbent, a Story 7.13 or 7.6 implementer checking "the current receipt" against Project state finds none. They must either reject every archive or replacement of that incumbent forever, or skip the check.

**Proposed fix.** Replace the first AD-35 sentence's tail with:

> "its generation is the claim-stream version of the acquiring event, kept in claim state; rejection events never change it. The holder is (Project, generation); task/step is recorded for recovery. The receipt check compares the command's receipt with the claim's current holder, and a reconstructed incumbent claim carries a cutover receipt that the incumbent's first post-cutover binding command names."

---

## LOW

### ADV-10: The claim stream key is not pinned

- **Anchor:** AD-18 line 278; AD-35 line 423
- **Status:** PLAUSIBLE

**Evidence**

- AD-18 says only "the AD-35 claim streams by `(TenantId, FolderId)`". It names no domain and no mapping from FolderId to aggregate ID.
- EventStore requires aggregate IDs to match `^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$` (`references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs:11`).
- Folders admits `^[A-Za-z0-9][A-Za-z0-9_-]{15,127}$`, which allows a trailing `-` or `_` (`references/Hexalith.Folders/src/Hexalith.Folders.Contracts/Commands/RevokeFolderAccess.cs:24`).
- v1 accepted `^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$` (`ProjectsDomainServiceEndpoints.cs:74`).

**Failure scenario.** Story 7.1's acquisition uses the raw FolderId, while Story 7.15's reconstruction escapes or hashes non-conforming IDs, or the two choose different domain names. One Folder then gets two claim streams and two Active holders.

**Proposed fix.** Add to AD-35:

> "The claim aggregate's domain is one fixed name declared in Contracts; its aggregate ID is the Folders-issued FolderId byte for byte. An ID EventStore cannot accept fails closed and is never encoded, hashed or case-folded."

### ADV-11: The AD-17 approved deltas omit the new creation-abandoned event

- **Anchor:** AD-17 line 249; AD-35 line 425
- **Status:** CONFIRMED (textual)

**Evidence.** AD-35 introduces a new Project-stream event, "a terminal creation-abandoned event". AD-17's approved-delta list is "Folder-first create without Pending, the combined AD-15 cap, artifact-based replacement, restore Folder validity, claim acquire and release".

**Failure scenario.** The equivalence-gate owner fails the gate on that event, or the retained-fold owner (`ProjectListProjection`, the legacy fold) does not know a stream that has no `ProjectCreated`.

**Proposed fix.** Add "creation abandonment" to the approved deltas. Add to AD-22: "a creation-abandoned stream is never listed, opened or counted by any fold."

### ADV-12: Epic and story text still encodes per-slice cutover and a Projects-issued credential

- **Anchor:** AD-17 line 251; AD-20 line 292
- **Status:** CONFIRMED

**Evidence.** In `_bmad-output/planning-artifacts/epics.md`:

- Line 1694 (Story 7.1 entry gate): "Before opening an affected writer slice".
- Line 1723 (Story 7.1): "when enabling a Tenant/Folder key".
- Line 2119 (Story 7.15): "before their value slice cuts over".
- Line 2137: Story 7.15's completion boundary is "single-writer command cutover complete", although AD-17 requires 7.15 to qualify before the cutover.
- Line 2153 (Story 7.16): "authenticates with a Projects-issued credential", although AD-20 requires the credential to come from the G-5 platform identity service.

**Failure scenario.** Story authors build to epics.md, so the per-slice and Projects-issued wording reappears in story specs.

**Proposed fix.** Route a correct-course edit to epics.md. Until then, add to the AD-17 Writer cutover paragraph: "Epic or story text describing per-slice or per-key writer enablement is superseded by this event."

---

## What held up

- **AD-17 single-writer shape.** There is one deploy, no per-slice enablement, rollback is a freeze plus roll-forward, exactly one keyed processor per aggregate type, and foreign snapshots are invalidated or re-folded. No pair of units could produce two writers on one stream under these rules (except through ADV-3).
- **AD-24 route surface.** The 25 v1 method-and-path pairs match `ProjectsDomainServiceEndpoints.cs:84-532`. The rule against gaining a route and the `409` that admits no task, sends no command and consumes no artifact are internally consistent.
- **AD-19 query allow-list.** Decoupling the four-query `404` from the cutover removes the earlier read/write coupling.
- **AD-29 decoration is enrollment.** The six decorated Contracts types (`GetProjectQuery`, `ListProjectsQuery`, the two `Resolve*` queries, `GetProjectAuditQuery`, `GetProjectTaskStatusQuery`) reduce to the four-read catalog once the two named decorations are removed. `[HexalithCommand]` and `[HexalithQuery]` come from `McpCli.Abstractions` only. Neither EventStore nor FrontComposer source uses them, so decoration cannot double as Gateway or Web enrollment. This agrees with the McpCli README (`references/Hexalith.McpCli/README.md:493`).
- **AD-35's core choice of a per-Folder aggregate stream.** It gives real atomic acquisition on EventStore's per-aggregate serialization, and the lost-reply recovery by task/step is coherent once ADV-3 and ADV-9 are fixed.
- **AD-20 allow-all removal target.** The removal is correctly aimed at `AddProjectsServerRuntimeInfrastructure` (lines 240–241). The fallback is deny-all, so nothing silently allows.
