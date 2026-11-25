# Reviewer Gate (Validate intent) — Data-integrity lens

Target: `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` (status final, updated 2026-11-24, SHA-256 `01b89a2f5e076a52b17f253417b7472ae85c0c7d26ae5a3c21c428e5868eb5fe`, 600 lines, verified unchanged at review time). Repository HEAD `31b35637`. Inputs: the spine, `.memlog.md`, the PRD, `epics.md` (Story 7.1), `src/`, and the EventStore source and docs at `references/Hexalith.EventStore` (`208af26a`). I did not read earlier reviews or validation reports.

**Verdict: PASS WITH FINDINGS.** The ownership model holds: single writer, Folder-first creation, platform Durable Tasks and the AD-35 claim are sound. The Rules as written still leave four write-path mechanisms open, and implementers could take them in incompatible or unsafe directions. Close those four High findings in the spine before Story 7.1 or the writer cutover starts implementation. They do not block Epic 6 or DW-63 host work.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 4 |
| Medium | 6 |
| Low | 3 |

---

## High

### DI-H1 — An abandoned reservation must be "fenced", but no fencing mechanism exists or is fixed

- **Severity:** High
- **Anchor:** AD-35 Rule, spine L428–L430. AD-12, L211. AD-8, L185.
- **Evidence:**
  - Spine L430: "Commit the binding against the expected Project version with the winning receipt. Fence the commit so an obsolete generation cannot install a binding after release … Abandoned reservations may be released only after authoritative evidence proves the binding did not commit and its outstanding commit is fenced."
  - EventStore's command contracts and client have no caller-supplied expected version. `grep -rni "expectedversion|ExpectedSequence"` over `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts` and `.Client` finds nothing. `SubmitCommandRequest.cs:16,26` carries only an optional opaque `IdempotencyKey`.
  - `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs:39-95`: `Handle(CreateProject)` accepts any command against `ProjectState.Empty`. `ProjectState.cs:36-48` tracks no version and no claim generation.
  - Epics Story 7.1 (`epics.md:1711,1721`) repeats "against the expected Project version, fenced by the winning claim receipt" without naming a mechanism.
- **Failure scenario:**
  1. Task T1 reserves P1 (AD-18), acquires the claim on supplied Folder F (generation g1) and sends `CreateProject(P1, F, g1)`.
  2. The command waits in the P1 actor or sidecar, and the worker's reply is lost.
  3. Reconciliation takes T1 to `Rejected` (AD-4), or it is cancelled. An implementer treats lease expiry plus "no `ProjectCreated` in P1's stream" as "proved did not commit" and releases g1.
  4. P2 acquires F as g2 and commits.
  5. The in-flight P1 command is then processed against an empty stream. Nothing in a pure `Handle` can see that g1 is obsolete, so P1 becomes Active on F.

  Two Active Projects now share one Folder (AD-3). Because permitted Projects are Folder-derived, F's readers gain P1's context.
- **Proposed resolution:** Make the fence a stream fact in AD-35. Before releasing an abandoned reservation, commit a terminal fence event (for example `ProjectActivationAbandoned{generation}`) to the reserved Project stream. Every later activation command for that Project and generation must be rejected by `Handle`. Name the Project-side guard: an activation command carries the claim generation, and the aggregate rejects one that is older than any fence it has recorded. State that a reserved ProjectId is never reused. Add the fence event to the AD-17 fold-completeness set (DI-H2).
- **Acceptance check:** A persisted two-replica test holds an activation command in flight, releases after the fence commits, acquires F for P2 and then delivers the stale command. The test must show `Rejected` on P1, one Active holder of F, and every fold and projection accepting the fence event.
- **Status:** CONFIRMED. The missing platform primitive and the guard-free aggregate were verified in source. The interleaving is constructed.

### DI-H2 — "Folds every event" and "can replay every new event" are met by code that silently drops the Folder binding

- **Severity:** High
- **Anchor:** AD-17 Rule, L245 ("the legacy read projection folds every event the selected writer emits"; "Command rollback is permitted only while the old writer can both replay every new event …"). AD-22, L309. Convention "Event evolution", L441.
- **Evidence:**
  - Memlog L102 says the intent is "preventing folderless or pending rendering of Folder-first Projects". The Rule's wording does not carry that intent.
  - `ProjectCreated.cs` has no Folder field today. Every current fold deserializes with tolerant `JsonSerializerDefaults.Web` and ignores unknown properties:
    - `ProjectStateApply.cs:61-75` sets `ProjectFolder = null` on `ProjectCreated`.
    - `ProjectDetailProjection.cs:110-125` sets the folder to `null`.
    - `ProjectReadProjectionHandler.cs:22-29,279` lists `ProjectCreated` as Relevant and deserializes it the same way.
  - All three already "fold" a new `ProjectCreated{Folder=F}` without error, so the Rule's literal test passes with no change.
  - EventStore's own guidance, `references/Hexalith.EventStore/docs/concepts/event-versioning.md:22`: an added property is safe only if "old readers must safely ignore it … Check every Apply, projection, and subscription consumer". Here old readers cannot safely ignore it.
  - Old-writer consequence, `ProjectAggregate.cs:300-315`: `SetProjectFolder` requires `ReplacementConfirmed` only when `state.ProjectFolder?.FolderId` is non-empty.
- **Failure scenarios:**
  1. Under `LegacyRollback` or the AD-24 adapter, a Folder-first Project is folded with no Folder and no pending event. Retained reads show an Active, folderless Project, which AD-3 forbids (or quarantine it, depending on the read path).
  2. After a command rollback that passed the "can replay" test, the legacy writer rehydrates P1 as unbound. It then accepts a Folder change with no Boolean confirmation, no Preview and no AD-35 claim, which breaks AD-5 and AD-35.
- **Proposed resolution:** Reword AD-17 so that "fold" and "replay" mean **state equivalence**, not deserialization without error. Each retained fold must produce, for every event shape the selected writer emits including every populated field, the state the SDK writer's own fold produces. Name the retained folds: the aggregate fold used by the legacy processor and `ProjectProjectionHandler`, `ProjectDetailProjection`/`ProjectListProjection`, and every retained read model behind `LegacyRollback` or the v1 adapter. Add to AD-22 that the legacy folds must read the new `ProjectCreated` Folder binding before the first Folder-first write.
- **Acceptance check:** A golden history containing writer-emitted `ProjectCreated{Folder=F}`, `ProjectFolderSet` and `ProjectRestored` (and the DI-H1 fence) yields equal Folder binding and lifecycle across all named folds. A negative test shows the current folds fail it.
- **Status:** CONFIRMED in source.

### DI-H3 — Aggregate snapshots are outside writer selection and rollback, and they rehydrate silently by property name

- **Severity:** High
- **Anchor:** AD-17 Rule, L245 ("A host build registers exactly one keyed `projects` processor with its matching replay; writer selection and rollback replace both together"). The spine never mentions aggregate snapshots (grep for "snapshot" finds only response snapshots, L303, L373, L381).
- **Evidence:**
  - EventStore stores snapshots at `{tenant}:{domain}:{aggId}:snapshot` with opaque `object State` and only an envelope version (`references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/SnapshotRecord.cs:20-29`). Nothing records which processor or state schema wrote it.
  - The SDK writer must be `EventStoreAggregate<TState> where TState : class, new()` (`EventStoreAggregate.cs:25`). Positional `ProjectState` (`ProjectState.cs:36`) has no parameterless constructor, so the SDK writer necessarily has a different state type.
  - `DomainProcessorStateRehydrator.cs:230-250` maps a JSON-object snapshot onto `TState` property by property and **silently skips** missing names.
  - The legacy processor tolerantly deserializes whatever snapshot it finds (`ProjectsDomainProcessor.cs:901-914,957-968`).
  - The DW-63 spec compares "nested snapshots" for the legacy host only (`spec-dw-63…md:48`).
- **Failure scenario:** At cutover the SDK writer loads a legacy snapshot. A renamed member such as `FolderBinding` versus `ProjectFolder`, or a new `Version` or claim generation, stays at its default, so a bound Active Project rehydrates as unbound or at version 0. Commands then take folderless paths. On rollback the legacy processor drops the SDK state's extra members in the same way. The golden-history and command-equivalence tests in AD-17 replay events only and cannot detect this.
- **Proposed resolution:** Add to AD-17: "Writer selection and rollback also invalidate or re-key aggregate snapshots. A processor ignores, and re-folds from events, any snapshot that it did not write under its own declared state schema. Equivalence evidence includes rehydration from the other writer's snapshot."
- **Acceptance check:** A persisted test snapshots P1 under the legacy processor, selects the SDK writer and issues a command. It must show state equal to a pure event re-fold, and likewise after rollback.
- **Status:** CONFIRMED in EventStore and Projects source.

### DI-H4 — Projects' own commit step has no deterministic idempotency key or expected-version rule, so stale retries can overwrite newer state

- **Severity:** High
- **Anchor:** AD-12, L211 ("Each owner call carries a deterministic task/step idempotency key and expected owner version"). AD-15, L233. AD-18, L283 (message IDs are platform ULIDs). AD-4, L138 (two-instance execution and duplicate delivery converge).
- **Evidence:**
  - The aggregate's idempotency ledger is keyed by the envelope **MessageId**, not by task and step. `ProjectsDomainProcessor.cs:364,422,475` passes `envelope.MessageId` as `IdempotencyKey`. `ProjectStateApply.cs:50-57` and `ProjectAggregate.cs:53-58` dedupe on that key.
  - A redelivery with a fresh ULID message ID is therefore a new command.
  - AD-12's "owner call" wording reads as applying to sibling owners. AD-35 requires an expected Project version only for binding commits, and DI-H1 shows the platform offers none.
- **Failure scenarios:**
  - **Lost update:** Setup task A sends `UpdateProjectSetup(v1)` and loses its reply. A later task B commits v2. A's lease expires and the resumed worker resends v1 with a new MessageId, and v1 overwrites v2.
  - **Wrong terminal state:** A duplicate archive returns `ProjectAlreadyArchived`. The task may record `Rejected` for work that actually succeeded, and AD-4 makes terminal states immutable.
- **Proposed resolution:** State in AD-12 or AD-15 that Projects' own aggregate commit is a saga step like any owner call. Its aggregate idempotency key derives from the task and step, stays stable across replicas and redeliveries, and is independent of MessageId. Every task-driven Project command carries an expected Project version (defined per DI-M3), and `Handle` rejects a mismatch as a non-mutating conflict that the task resolves by authoritative status query.
- **Acceptance check:** In a two-instance test, A's stale v1 resend after B's v2 leaves v2 in place, and A converges to its original outcome without a new event. A duplicate archive converges to `Succeeded`.
- **Status:** CONFIRMED for the code paths. The interleaving is PLAUSIBLE.

---

## Medium

### DI-M1 — The claim's persistence home, generation semantics and authority precedence are unseeded

- **Anchor:** AD-35, L428. Structural Seed, L475–L485. AD-1, L120. AD-9, L191. G-2, L554.
- **Evidence:** AD-35 says "Projects serializes claim acquisition by `(TenantId, FolderId)` … records … claim generation in a durable receipt". The seed table lists no claim stream, aggregate or record. AD-1 forbids Projects-owned persistence, and AD-9 assigns receipts to the platform, so it is unclear whether the claim is a Projects EventStore aggregate or a platform record.
- **Failure scenario:** Story 7.1 implements the claim as an ETag-guarded read-model key, while restore, replace and legacy repair stories (7.6, AD-23) assume an event-sourced claim aggregate. Their generations, rebuild behaviour and audit trail differ. There is also no rule for which side wins when the claim record and a Project history disagree, for example claim "held by P1" while P1 is Archived.
- **Proposed resolution:** Seed it: "The claim is a Projects-owned EventStore aggregate `Tenant/project-folder-claims/FolderId`. Its generation is its stream sequence, and the receipt ID is a platform ULID. Project histories are authoritative for binding. A claim disagreeing with them is closed and reconciled, never auto-released." Alternatively name the platform record and its owner under G-1.
- **Acceptance check:** The Structural Seed names the artifact. One shared claim API is used by create, replace, restore, repair and archive.
- **Status:** CONFIRMED (absence). PLAUSIBLE (divergence).

### DI-M2 — Claim disposition at task terminal states and at the irreversible checkpoint is unspecified

- **Anchor:** AD-4, L138 ("A confirmed denial … performs no new owner mutation"). AD-12, L211 ("A rejection cannot trigger another owner call"). AD-35, L430.
- **Failure scenario:** A user supplies their existing Folder F. The claim is acquired, then the commit is refused or reconciliation reaches `Rejected`, or the task is cancelled. One implementation reads AD-4 and AD-12 as forbidding release, so F is never claimable again: terminal tasks are immutable and reconciliation starts only from `NeedsAttention`. Another releases at once without the DI-H1 fence.
- **Proposed resolution:** State that claim acquisition is not the irreversible checkpoint. Every transition to `Rejected`, `Failed` or `Cancelled` for a task holding an uncommitted claim must, before it becomes terminal, run the fenced release (DI-H1) as a Projects-internal step. That step is not an owner call or an owner mutation. If the fence cannot be established, the task stays `NeedsAttention`.
- **Acceptance check:** Tests for each terminal path end with F free and claimable by another Project, and with a stale commit rejected.
- **Status:** CONFIRMED (text gap). PLAUSIBLE (impact).

### DI-M3 — "Project version" is undefined, and persisted rejection events advance the stream sequence

- **Anchor:** AD-5, L167. AD-13, L217 ("current versions"). AD-32, L377 (`projectVersion`). AD-35, L430 ("expected Project version"). PRD L257: "Confirmation is invalidated when actor authority or Project version changes."
- **Evidence:**
  - `ProjectsDomainProcessor.cs:54-60`: "EventStore persists rejection results as ordinary stream events."
  - `ProjectReadProjectionHandler.cs:329-331` advances the detail `Sequence` on every event, including rejections.
  - AD-17 L264 compares "supported ProjectVersion independently with the original SequenceNumber".
- **Failure scenario:** Any caller whose command is refused, such as an unauthorized archive attempt, appends a rejection event. If "version" means the stream sequence, every outstanding Preview and Confirmation Artifact for that Project fails (`RenewPreview`) and every expected-version commit (DI-H4) conflicts. That is a cheap denial-of-progress, and implementers will disagree on which number to bind.
- **Proposed resolution:** Define the Project version once, in AD-15 or the Conventions: the count of state-changing events (or the sequence of the last one), excluding `IRejectionEvent`. Use that same value for `projectVersion`, Preview and artifact binding, and expected-version commits.
- **Acceptance check:** An interleaved rejected command leaves an outstanding Confirmation Artifact valid and lets an expected-version commit succeed.
- **Status:** CONFIRMED (rejections persisted, sequence advances). PLAUSIBLE (impact).

### DI-M4 — Conflicting legacy incumbents stay Active and context-usable

- **Anchor:** AD-35, L432 ("Keep the key closed to every claimant while its history is incomplete or holders conflict") versus AD-3, L132 ("A Folder is the Project Folder of at most one Active Project").
- **Evidence:** The legacy writer has no cross-Project uniqueness check. `ProjectAggregate.cs:267-333` and `Folders/*` (grep for "already bound" or "FolderInUse" finds nothing), and legacy restore has no Folder check (`ProjectAggregate.cs:213-258`). Duplicates are therefore possible in history.
- **Failure scenario:** Legacy P1 and P2 are both Active on F. The cutover closes F's key, but both stay Active, listed and usable as context for F's readers, so AD-3 stays violated for an unbounded time.
- **Proposed resolution:** In AD-35 or AD-17, quarantine every Project on a conflicting key (`Unavailable`, no context), exactly like the AD-3 folderless legacy rule. Release it only through the FR-8 Administrator binding workflow (Story 7.6).
- **Acceptance check:** In a fixture with two Active legacy Projects on one Folder, both read as `Unavailable` with no context after cutover, and the FR-8 repair yields one Active holder.
- **Status:** CONFIRMED (no legacy guard). PLAUSIBLE (data exists).

### DI-M5 — The AD-17 writer gate demands equivalence to current semantics, which the spine itself changes

- **Anchor:** AD-17, L245 ("`DomainResult` command behavior equivalent to current success/rejection/no-op semantics"). AD-15, L233. AD-22, L309. AD-23, L315. AD-5, L167. AD-35.
- **Evidence:**
  - Current create emits `ProjectCreated(Active)` plus `ProjectFolderCreationPending` (`ProjectAggregate.cs:65-95`), while AD-22 says stop emitting it.
  - Current caps are 100 File plus 100 Memory references, counted separately (`ProjectState.cs:57,60`), while AD-15 says 5,000 combined.
  - Current replacement uses a Boolean `ReplacementConfirmed` (`ProjectAggregate.cs:308`), while AD-5 forbids Boolean confirmation.
  - Current restore does no Folder check, while AD-23 requires one.
- **Failure scenario:** One team builds an SDK writer that is equivalent and still legacy-shaped in order to pass the gate, and defers Folder-first. Another builds Folder-first and fails the gate. There is no rule for when nullable-Folder `ProjectCreated` becomes illegal, so the history boundary AD-22 relies on is undefined.
- **Proposed resolution:** Enumerate the approved deliberate deltas in AD-17: Folder-first create, no Pending, the combined 5,000 cap, artifact-based replacement, restore Folder validity and claim acquisition. The equivalence gate applies to every other command. Each delta needs its own golden history, and the first Folder-first write position is the historical boundary for AD-22.
- **Acceptance check:** The writer-gate evidence lists each delta with its own golden case. All other commands pass byte-level outcome equivalence.
- **Status:** CONFIRMED in source.

### DI-M6 — Event evolution is silent on EventStore payload versions, upcasters and rebuilds after fold changes

- **Anchor:** Convention "Event evolution", L441 ("Additive and serialization-tolerant; no `V2` event types"). AD-22, L309. AD-14, L225.
- **Evidence:**
  - The EventStore SDK supports `[EventPayloadVersion(n)]` plus `IEventPayloadUpcaster` chains, and `EventStoreAggregate` consumes an `EvolutionRegistry` (`event-versioning.md`, `EventStoreAggregate.cs`).
  - The legacy processor and the legacy projections deserialize raw bytes and ignore `PayloadVersion`.
  - The EventStore docs (L78) allow rollback "only while the old service can read every event". The spine says neither "no payload versions" nor how to use them.
- **Failure scenario:**
  - An implementer adds a version-2 `ProjectCreated` with an upcaster, which is not a "V2 event type", so the convention seems satisfied. The SDK folds see the upcasted shape, while the legacy folds see raw bytes and diverge.
  - Separately, when an upcaster or fold rule changes how history is read, incremental read models and snapshots built under the old rule are never rebuilt, so a rebuilt and an incremental model disagree.
- **Proposed resolution:** Add to the convention: "Projects events stay at payload version 1 while any retained legacy fold exists. Introducing a payload version or upcaster requires retiring or upgrading every retained fold first. Any change to fold or upcaster semantics bumps the affected projection's version and forces a rebuild (Story 8.10 mechanics) and snapshot invalidation (DI-H3)."
- **Acceptance check:** A lint or startup check fails when a Projects event declares a version above 1 while the legacy processor is registered. A projection-version bump triggers a rebuild in tests.
- **Status:** CONFIRMED (capability exists, spine silent).

---

## Low

### DI-L1 — The current legacy processor is never an admissible command-rollback target; say so

- **Anchor:** AD-17, L245 and mermaid L259. Epics 7.1 entry gate, `epics.md:1694` ("executable rollback proof").
- **Evidence:** The legacy processor emits `ProjectFolderCreationPending` (`ProjectAggregate.cs:79-95`) and has no AD-35 claim, so it cannot "emit spine-compliant events". AD-17's conditional rollback is therefore empty for it. The epics still ask for "executable rollback", which invites attempts to re-select it.
- **Resolution:** State: "Writer cutover from the current processor is roll-forward only. 'Executable rollback' for the writer means mutation freeze plus forward recovery. Read-route rollback is unaffected."
- **Acceptance check:** The 7.1 rollback evidence shows freeze and forward recovery, not legacy re-selection.
- **Status:** CONFIRMED.

### DI-L2 — The reverse-index rebuild source is unnamed

- **Anchor:** AD-10, L197 ("rebuildable Tenant-scoped reverse index … must be able to reconstruct that receipt").
- **Evidence:** The Project aggregate stores no membership (AD-10). Task records retain only 30 or more days (AD-5), so a rebuild from Projects data alone cannot reconstruct older memberships or prior-membership receipts.
- **Resolution:** Name the rebuild source as the Conversations owner's membership history and receipts under the G-2 pinned contract, not Projects task or audit data.
- **Acceptance check:** Rebuilding the index from an empty store matches the Conversations-authoritative membership and receipts.
- **Status:** PLAUSIBLE.

### DI-L3 — When idempotency retention starts for nonterminal tasks is ambiguous

- **Anchor:** AD-5, L167 ("the terminal task result plus idempotency record remain together for at least 30 days or the longer result lifetime").
- **Failure scenario:** A task stays in `NeedsAttention` for more than 30 days and an admission-dated purge drops its idempotency record. An equivalent retry then admits a second task with a new ProjectId: either a second created Folder, or a second claim attempt on a supplied Folder that the first task still holds.
- **Resolution:** State that an idempotency record never expires while its task is nonterminal, and that the 30 days count from the terminal transition.
- **Acceptance check:** A retention test with a long-running `NeedsAttention` task returns the original task on an equivalent retry.
- **Status:** PLAUSIBLE.

---

## Checked and held up

- **AD-35 retention through uncertainty:** "While the outcome is unknown, retain the claim … Neither a worker timeout nor a read-model observation authorizes release" (L430) correctly blocks the naive unsafe release.
- **AD-35 legacy incumbents:** reconstruction comes from authoritative history rather than read models, with fence-then-reserve-then-open ordering and closed keys for incomplete or conflicting history (L432).
- **AD-35 stale releases:** "A conditional release names the exact Project and claim generation" makes a replayed stale release a no-op against a later holder.
- **AD-13 lost replies:** atomic validation, consumption and admission, plus "recover the original admitted task before revalidating spent evidence", make a lost reply after consumption converge on any replica (L217).
- **AD-4 terminal states:** terminal states are immutable, an unknown partial outcome becomes `NeedsAttention`, and reconciliation reuses the original bindings with an original-actor recheck. The state graph is consistent with the Rule.
- **AD-12 compensation:** compensation runs only through an explicit idempotent owner command, there is a re-query before retry, and Projects never deletes Folders resources (also AD-8, L185).
- **AD-17 registration:** exactly one keyed processor with its matching replay, replaced together, prevents last-registration-wins writer selection.
- **AD-22 history:** the `ProjectFolderCreationPending` deserializer and Apply are retained. They are present in `ProjectStateApply.cs:94-108` and in the Relevant catalog of the supported projection.
- **AD-23 restore with replacement:** `ProjectFolderSet` before `ProjectRestored` in one commit is atomic within a single stream.
- **Fail-closed folds:** the supported read projection uses a finite event catalog and fails closed on unknown types (`ProjectReadProjectionHandler.cs:22-38`). The aggregate Apply enforces identity on every event and throws on unknown types (`ProjectStateApply.cs:39-48,170-173`).
- **AD-6 history preservation:** no rewrite of history. EventStore keeps the original bytes even when upcasting (`event-versioning.md`).
- **AD-18 identifiers:** the `ProjectId` constructor accepts any non-whitespace legacy value and is never GUID-parsed (`ProjectId.cs`), which matches "Persisted legacy IDs remain readable".
- **AD-15 per-Project serialization:** one stream with commands serialized per Project is a sound home for the cap, uniqueness and lifecycle invariants, apart from the cap value conflict in DI-M5.
