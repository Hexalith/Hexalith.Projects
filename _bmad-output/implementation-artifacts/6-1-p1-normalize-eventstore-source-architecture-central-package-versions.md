---
work_package_id: 6.1-P1
story_key: 6-1-p1-normalize-eventstore-source-architecture-central-package-versions
artifact_kind: external-prerequisite-story-handoff
created: 2026-07-17
source_action_status: open
status: blocked
blocked_by:
  - EventStore-owner and Builds-owner repository selection and local story creation
  - Solution-Architect-approved Architecture Spine baseline revision
  - owner-approved compatibility and rollback evidence
repository_authority: "EventStore and Builds repositories; Architecture Spine updated only through Solution-Architect (architecture-owner) approval"
owners: [EventStore Owner, Builds Owner, Solution Architect]
parallel_with: [6.1-P0]
unblocks: [6.1-P2, 6.1-P4]
traceability:
  requirements: [fr-2, fr-5]
  nfrs: [nfr-10]
  supporting_nfrs: [nfr-5]
  architecture: [AD-6, AD-14, AD-16, AD-17]
  findings: []
  enables_evidence_rows:
    - fr-2
    - fr-5
    - nfr-10
    - nfr-5
target_date: uncommitted
estimate: M
risk: high dependency and compatibility risk
projects_baseline_commit: 08e942e
---

# Story 6.1-P1: Normalize EventStore Source, Architecture, and Central Package Versions

Status: blocked

<!--
This is the Projects-side handoff for the external 6.1-P1 enablement work package. It is not an
additional Hexalith.Projects user-value story and does not change the approved 33-story backlog.
No owner repository, revision, or version has been selected or authorized yet; this artifact
records the required outcome, the currently observable discrepancy, and the acceptance contract so
the named owners can act. It grants no implementation authority by itself.
-->

## Story

As an EventStore Owner, Builds Owner, and Solution Architect,
I want one mutually agreed EventStore source revision, package version, Architecture Spine pin, and G-4 runner manifest baseline, backed by a finite normalization record,
so that Story 6.1 and every later story consuming the shared Epic 6 read baseline can build, test, and prove compatibility against a single supported truth instead of three disagreeing pins.

## Acceptance Criteria

1. **Owner authority and finite normalization record.** Given the EventStore Owner, Builds Owner, and Solution Architect, then they select the repository or coordinated repositories that own the normalization decision, name accountable owners, and create the local work item(s) recording the accepted EventStore source revision, package IDs and versions, namespaces, public symbols/signatures Story 6.1 depends on, compatibility results, owner approvals, and a rollback pin. Projects and implementing agents cannot self-approve P1 acceptance.

2. **One agreed source, package, and pin.** Given the accepted decision, then the checked-out `references/Hexalith.EventStore` submodule revision, the `HexalithEventStoreVersion` central package property in `references/Hexalith.Builds/Props/Directory.Packages.props`, and the Architecture Spine `Stack` table's `Hexalith.EventStore package binding` entry name the exact same revision and version. No `latest`, floating branch, local patch, prerelease, or unrecorded compatibility shim remains anywhere in the 6.1 dependency chain.

3. **Compatibility evidence for Story 6.1's required read/query surface.** Given the accepted version, then a build and test pass demonstrates that `IAsyncDomainProjectionHandler`, `IReadModelStore`/`IReadModelBatchStore` with `ReadModelWritePolicy`, `IDomainQueryHandler`, `IQueryCursorCodec`, and `QueryCursorScope` (AD-14) resolve, compile, and behave as Story 6.1 requires. Evidence names the exact public symbols/signatures checked; it does not implement Story 6.1's read models.

4. **Transitive package graph resolves without conflict.** Given the accepted `HexalithEventStoreVersion`, then a clean restore of the affected central package graph produces no version-conflict warning, downgrade, or unresolved transitive reference for packages Story 6.1's read path depends on. Unrelated central catalog entries (Dapr runtime/client tuple alignment, Fluent UI RC, NSubstitute RC, CommunityToolkit preview, Fluxor) remain explicitly out of scope and are not silently touched.

5. **Architecture Spine realigned atomically.** Given the accepted decision, then the Architecture Spine's `Stack` table entry, the `G-1` gate's EventStore-evidence text, and any other spine passage naming the EventStore version change together in the same approved edit as the Builds central property and the EventStore submodule pin — never as an isolated, partially-applied change.

6. **G-4 runner manifest agreement.** Given the 6.1-P0 platform composition runner and its manifest contract, then the accepted EventStore revision/version is the same one the runner composes against. P1 records and confirms this agreement; it does not implement or modify the runner itself.

7. **Only recorded, root-declared checkouts are admissible.** Given AD-6, then only the root-declared `references/Hexalith.EventStore` and `references/Hexalith.Builds` submodule checkouts at the accepted, committed revision are admissible evidence. An uncommitted working-tree submodule pointer ahead of what is recorded in the umbrella repository's index, a nested submodule, or a stale local binary is not evidence of the normalized baseline.

8. **Rollback pin and regression evidence.** Given the normalized baseline differs from what is currently checked out or committed anywhere in the chain, then the record names the prior mutually-compatible triple (Architecture Spine pin, Builds central property, EventStore revision) as the rollback pin, and a regression check confirms no currently-published consumer of the prior pinned API surface breaks silently.

9. **Finite, non-extensible completion boundary.** Given all P1 acceptance evidence passes, then the Projects action ledger may record P1 evidence for 6.1-P2 and 6.1-P4. P1 does not implement G-1 durable-task/Confirmation Artifact capability, G-2 query-security/watermark capability, the G-4 runner itself, or non-EventStore G-6 prerelease gates (Fluent UI RC4, CommunityToolkit preview, NSubstitute RC, Fluxor); it does not self-accept 6.1-P2, 6.1-P3, or 6.1-P4; and it does not authorize Story 6.1 implementation.

## Tasks / Subtasks

**Non-implementation entry condition.** Do not begin the tasks below until AC 1 has an
owner-approved repository-local record. The paths in the conditional file map are guidance, not a
repository decision.

- [ ] Establish owner-repository authority and freeze the contract (AC: 1, 9)
  - [ ] Select the repository or coordinated repositories, name accountable owners (EventStore Owner, Builds Owner, Solution Architect), and create the local work item(s).
  - [ ] Record the accepted revision, package IDs/versions, namespaces, public symbols/signatures Story 6.1 depends on, and the acceptance/rollback contract before any code change.

- [ ] Select and pin one EventStore source revision and package version (AC: 2, 7)
  - [ ] Reconcile the current three-way disagreement: Architecture Spine `3.67.3`, Builds `HexalithEventStoreVersion` `3.70.0`, and the checked-out EventStore submodule revision (currently past `3.70.0`).
  - [ ] Record the accepted revision only as a committed, root-declared submodule pointer — never an uncommitted working-tree checkout.

- [ ] Prove compatibility against Story 6.1's required read/query surface (AC: 3)
  - [ ] Build and test the accepted version against the AD-14 seams Story 6.1 needs; record exact symbols/signatures verified.
  - [ ] Do not implement Story 6.1's projections, query handlers, or cursors under P1.

- [ ] Resolve the transitive central-package graph (AC: 4)
  - [ ] Run a clean restore against the accepted `HexalithEventStoreVersion` and record the result; fix only conflicts the accepted version introduces.
  - [ ] Leave unrelated G-6 prerelease/alignment items (Dapr, Fluent UI, NSubstitute, CommunityToolkit, Fluxor) untouched and separately tracked.

- [ ] Atomically realign the Architecture Spine, Builds, and G-4 runner manifest records (AC: 5, 6)
  - [ ] Submit one Solution-Architect-approved edit updating the Spine `Stack` table and `G-1` gate text together with the Builds central property and EventStore pin.
  - [ ] Confirm the 6.1-P0 runner manifest targets the same accepted pin; do not edit the runner itself.

- [ ] Record rollback pin and regression evidence (AC: 8)
  - [ ] Name the prior mutually-compatible triple as the rollback pin.
  - [ ] Run a regression check against the prior pinned API surface and record the result.

- [ ] Hand the accepted normalization record to 6.1-P2 and 6.1-P4 (AC: 9)
  - [ ] Publish the finite normalization record (revision, versions, symbols, compatibility results, approvals, rollback pin).
  - [ ] Explicitly state the completion boundary so P2/P3/P4 and Story 6.1 know what remains their own responsibility.

## Dev Notes

### Authority, Readiness, and Scope

- This artifact translates the approved `6.1-P1` action from the 2026-07-17 Sprint Change Proposal. No owner repository, accountable-owner acceptance, or baseline revision has been authorized yet — unlike 6.1-P0, which Jerome authorized to `Hexalith/Hexalith.Builds` on 2026-07-17. Keep this artifact `blocked` and the Projects P1 action `open` until owner-approved evidence exists.
- P1 is an enablement work package, not one of the 33 approved Hexalith.Projects user-value stories. Keep Story 6.1 and its specification `blocked`; P1 alone does not unblock Story 6.1, satisfy 6.1-P2/P3, create `6.1-entry-gate.yaml`, or authorize Projects implementation.
- P1 enables FR-2/FR-5 verification and traces to NFR-10 (compatibility), with supporting NFR-5 (performance, since 6.1-P0/P1/P2/P4 all gate the `nfr-5` evidence row). NFR-11 (release evidence) is a separate terminal Epic 8 gate keyed to `Epic-8-gate` and `G-4` only — it is not directly gated by 6.1-P1 in the traceability matrix and is not claimed as P1 scope here. It does not implement Projects read behavior, supply P2's dual-principal query/safe-denial/watermark capability, approve P3's production identity contract, implement the P0 G-4 runner, or self-accept P4.
- P1 has no UI, query-handler, or projection implementation scope. It selects and proves a platform version baseline; Story 6.1 later consumes that baseline to implement Projects-owned read models.
- G-6 (runtime/toolchain alignment) is a related but broader gate owned by Builds Owner + Test Architect; it also covers Dapr runtime/client tuple alignment and prerelease packages (Fluent UI RC4, CommunityToolkit preview, NSubstitute RC, Fluxor) that are explicitly **not** P1's scope. P1 must not silently absorb or defer those unrelated G-6 items.

### Current Reality: The EventStore Version Discrepancy

This is the concrete disagreement P1 exists to resolve, observed directly in this checkout:

- The Architecture Spine's `Stack` table (verified 2026-07-16) pins `Hexalith.EventStore package binding: 3.67.3`, with the note "API evidence only from published 3.67.3 packages or a clean 3.67.3 build."
- `references/Hexalith.Builds/Props/Directory.Packages.props` currently sets `HexalithEventStoreVersion = 3.70.0` (Builds commit `edbaeae`, `chore(deps): update HexalithEventStoreVersion to 3.70.0`, preceded by `e9f8b19` bumping `3.69.0` — the property has moved twice since the Spine's verification date without a corresponding Spine update).
- The checked-out `references/Hexalith.EventStore` submodule working tree sits at `ba203bde` / `v3.70.0-3-gba203bde` — three commits past even the `3.70.0` tag Builds currently pins.
- `Hexalith.Projects`'s own `Directory.Packages.props` sets no direct EventStore package version; it inherits the central property from Builds. A clean build today therefore resolves EventStore packages to `3.70.0`-line versions, not the Spine's recorded `3.67.3` — the Spine's "verified" pin already understates what a clean build actually restores.
- The `G-1` entry gate explicitly records that the durable-task/Confirmation Artifact API evidence later work packages need is "absent from current published/clean EventStore 3.67.3 API evidence" — nobody has verified the officially recorded version actually exposes what downstream work needs. The correct resolution may be a version bump, not just documentation hygiene, but P1 must not choose it unilaterally (per the Sprint Change Proposal: "The selected version is deliberately not specified by this proposal. Choosing it without the named owners would repeat the original blocking condition.").
- The checked-out `references/Hexalith.Builds` submodule working tree is separately at `a625ded` / `v4.19.2-18-ga625ded`, ahead of the `edbaeaed` (`v4.19.2-13`) baseline recorded as 6.1-P0's `owner_repository_baseline`. This is uncommitted local drift in the umbrella repository's submodule pointer, not yet part of any recorded pin. Per AD-6/AC-7, P1 must treat only a committed, root-declared checkout as admissible evidence — not the current uncommitted working-tree state.
- `Aspire.Hosting` in Builds is `13.4.6`, matching the Spine's adopted pin exactly — there is no discrepancy on that axis; the disagreement is specific to EventStore.

### Recent Git and Prior-Evidence Intelligence

- Root commit `08e942e` (current HEAD) and `0dc835f` established the Story 6.1 `blocked` state, the 6.1-P0..P4 work-package ledger in `epics.md` and `sprint-status.yaml`, and the canonical readiness-matrix rows. The `nfr-10` row (`architecture_decisions: [AD-6, AD-16, AD-17, AD-22]`) explicitly gates on `6.1-P1` and `6.1-P4` and blames "Story 6.1 P1/P4 version/source normalization and accepted compatibility baseline" — this is the authoritative existing mapping this story's `traceability.architecture` reuses.
- Root commits `ec21c7e` (AD-30 evidence matrix, corrective Epics 6-8 rebaseline) and `5e32ece` (Epic 6 test design) are planning authority establishing the gate structure, not capability proof.
- The sibling `6.1-P0` handoff (`_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md`) originally deferred the EventStore version/architecture-pin mismatch to 6.1-P1 in its pre-authorization task list (git history, commit `0dc835f`: "disposition any mismatch between the adopted architecture pins and current Builds central properties through 6.1-P1"). That specific line was superseded when Jerome authorized P0's routing on 2026-07-17 and its task list was rewritten to "Routing condition satisfied," but the deferred concern itself was never separately resolved — this artifact is that still-open item.
- Builds commit history (`e9f8b19` → `edbaeae`) shows the `HexalithEventStoreVersion` property has already moved twice past the Spine's recorded pin since the 2026-07-16 verification; treat this as an active, moving target, not a one-time drift to reconcile once.

### Version and Compatibility Boundary

- The specific AD-14 seams Story 6.1 (FR-2, FR-5) depends on are `IAsyncDomainProjectionHandler`, `IReadModelStore`/`IReadModelBatchStore` with explicit `ReadModelWritePolicy`, `IDomainQueryHandler`, `IQueryCursorCodec`, and authenticated `QueryCursorScope`. P1's compatibility evidence must name the exact public symbols/signatures it checked against the accepted version, per AD-6's "public symbols/signatures used by 6.1" requirement — a checked-out revision or stale Release output is not version evidence by itself.
- AD-17 permits command rollback only while the prior writer can both replay every new event and emit spine-compliant behavior; a version change that alters serialized event shapes or query contracts must be evaluated against that same replay-and-emit bar before it can be treated as safe, even though P1 itself performs no cutover.
- AD-16 establishes `Hexalith.Builds` as the sole version owner for NSwag and Fluxor as a precedent for centralized version ownership; P1 extends the same discipline to the EventStore central property without claiming NSwag/Fluxor authority.

### Conditional Project Structure Notes

The EventStore/Builds/architecture owners have not selected the final revision, so these are
conditional impact areas, not approved filenames.

Likely **UPDATE** areas once accepted:

- `references/Hexalith.EventStore` submodule pointer — advance or pin to the accepted committed revision.
- `references/Hexalith.Builds/Props/Directory.Packages.props` — `HexalithEventStoreVersion` property to match the accepted, recorded revision.
- `_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md` — `Stack` table `Hexalith.EventStore package binding` row and the `G-1` gate's EventStore-evidence text, edited together and only through Solution-Architect approval.
- `_bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml` and `.md` — `nfr-10`, `fr-2`, `fr-5` rows' `pinned_revision`/`blocker` fields, once the normalization record is accepted (AD-30 evidence).

Likely **NEW** artifacts:

- The owner-repository local story/issue recording the normalization decision.
- A finite normalization record (revision, package IDs/versions, namespaces, public symbols/signatures, compatibility results, owner approvals, rollback pin) referenced by this handoff and by 6.1-P4.

This Projects handoff itself requires no source-code change; do not put EventStore/Builds version-selection logic or normalization tooling in Projects.

### Verification and Evidence Contract

The owner-repository record must bind final commands, but qualification includes at least:

```text
dotnet restore <EventStore solution, at the accepted revision>
dotnet build <EventStore solution> --configuration Release
dotnet test <EventStore API/compatibility test project(s)> --configuration Release
dotnet restore <a Projects-consuming build against the updated Builds central property>
```

Required P1 handoff evidence:

- owner-approved EventStore repository revision and package version, agreeing across the EventStore checkout, the Builds central property, and the Architecture Spine `Stack` table;
- named public symbols/signatures Story 6.1 depends on, with compatibility results against the accepted version;
- clean transitive-package-graph restore result for the affected dependency chain;
- confirmation that the 6.1-P0 G-4 runner manifest targets the same accepted pin;
- rollback pin naming the prior mutually-compatible triple, with a regression result proving no silent break of the prior pinned surface;
- owner approvals from the EventStore Owner, Builds Owner, and Solution Architect.

### Hard Stops

- Stop if EventStore Owner, Builds Owner, or Solution Architect approval for the selected revision is missing.
- Stop if the selected revision is a `latest`, floating branch, local patch, prerelease, or uncommitted working-tree checkout rather than a recorded, root-declared submodule revision.
- Stop if the Architecture Spine, Builds central property, EventStore checkout, and 6.1-P0 G-4 runner manifest do not end up agreeing after the change.
- Stop if compatibility evidence for Story 6.1's required AD-14 read/query seams is missing or unnamed.
- Stop if P1 attempts to implement G-1/G-2 capabilities, the G-4 runner, or non-EventStore G-6 prerelease gates (Fluent UI RC4, CommunityToolkit preview, NSubstitute RC, Fluxor) — those remain separately owned.
- Stop if P1 self-approves 6.1-P2, 6.1-P3, or 6.1-P4, or marks Story 6.1 ready.
- Stop if a rollback pin is not recorded when the normalized baseline changes what is currently checked out or committed.

### References

- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md#61-p1--normalize-eventstore-source-architecture-and-central-package-versions]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md#44-architecture-and-version-normalization]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-6]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-14]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-16]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-17]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#Target-and-compatibility-bindings]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#External-capability-entry-gates]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml] (rows `nfr-10`, `fr-2`, `fr-5`, `nfr-5`)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-6-Authorized-Project-Reads-on-the-Supported-Platform]
- [Source: _bmad-output/implementation-artifacts/epic-6-context.md]
- [Source: _bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md]
- [Source: _bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths.md]
- [Source: _bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml] (action_items `6.1-P1`, `6.1-P2`)
- [Source: _bmad-output/project-context.md]
- [Source: references/Hexalith.Builds/Props/Directory.Packages.props]
- [Source: references/Hexalith.AI.Tools/hexalith-llm-instructions.md]
- [Official NuGet central package management: https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management]
- [Official .NET dependency version ranges: https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution]
- [Official Git submodules: https://git-scm.com/book/en/v2/Git-Tools-Submodules]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 5

### Debug Log References

- Handoff creation and source analysis only; no implementation build, test, or version change was performed. Observed live discrepancy: Architecture Spine `Stack` table pins `Hexalith.EventStore` `3.67.3` (verified 2026-07-16); `references/Hexalith.Builds/Props/Directory.Packages.props` `HexalithEventStoreVersion=3.70.0` (Builds commit `edbaeae`); checked-out `references/Hexalith.EventStore` working tree at `ba203bde` / `v3.70.0-3-gba203bde`; checked-out `references/Hexalith.Builds` working tree at `a625ded` / `v4.19.2-18-ga625ded` (ahead of any recorded pin, uncommitted in the superproject index).

### Completion Notes List

- Classified this artifact as an external prerequisite handoff, not a Hexalith.Projects user-value story, mirroring the accepted 6.1-P0 handoff pattern.
- No owner-repository routing has been authorized for 6.1-P1; status remains `blocked` pending EventStore Owner, Builds Owner, and Solution Architect selection — unlike 6.1-P0, which Jerome has already authorized to `Hexalith/Hexalith.Builds`.
- Documented the concrete, currently observable three-way EventStore version/source discrepancy (Architecture Spine, Builds central property, checked-out submodule revision) so the accountable owners start from a real normalization record rather than an abstract mandate.
- Reused the canonical `nfr-10` traceability row's architecture-decision mapping (`AD-6, AD-16, AD-17`, plus `AD-14` for the concrete AD-14 read/query seams Story 6.1 depends on) rather than inventing a new one, for consistency with `implementation-readiness-traceability-matrix.yaml`.

### File List

- `_bmad-output/implementation-artifacts/6-1-p1-normalize-eventstore-source-architecture-central-package-versions.md` (new)
