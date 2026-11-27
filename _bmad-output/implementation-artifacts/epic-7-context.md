# Epic 7 Context: Users Complete Durable Project Decisions and Recover Them

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Run consequential Project decisions as versioned Projects workflows on platform Durable Tasks. Users can complete setup, associations, creation, and lifecycle changes through the same governed admission and recovery model. Operators can reconcile interrupted work from persisted checkpoints, and users see an Active Project only after its authorized Folder binding and read model are current.

## Stories

- Story 7.1: Activate a Project with exactly one authorized Folder
- Story 7.2: Update Project Setup idempotently
- Story 7.3: Link an unassigned Conversation
- Story 7.4: Move a Conversation between Projects
- Story 7.5: Unlink a Conversation
- Story 7.6: Replace a Project Folder
- Story 7.7: Link an authorized File Reference
- Story 7.8: Unlink a File Reference
- Story 7.9: Link an authorized Memory
- Story 7.10: Unlink a Memory
- Story 7.11: Confirm an ambiguous Project choice
- Story 7.12: Confirm a proposed new Project
- Story 7.13: Archive an Active Project
- Story 7.14: Restore an Archived Project
- Story 7.15: Reconcile legacy and interrupted workflows
- Story 7.16: Select an association target
- Story 7.17: Cut over Project writes to the SDK writer in one fenced deploy

## Requirements & Constraints

- Check same-Tenant scope and current original-actor authority at admission and before each Project commit. Owner calls use task-bounded delegated credentials for later steps. A workload identity grants no Folder or Project authority. Unknown authorization delays work, and denied cases expose no protected details.
- Admission returns a pollable task. The durable record governs completion, cancellation before the irreversible checkpoint, recoverable dependency delays and `NeedsAttention`, and immutable terminal outcomes. Success requires authoritative state and the relevant read model to agree.
- Scope idempotency by Tenant, actor, operation and key. Equivalent retries resolve the original task before spent evidence is checked again. Changed requests conflict. Keep the record through execution and at least 30 days after terminal transition or for the longer result lifetime.
- Preserve historical events and unversioned name-only creation throughout v1. Advance contracts additively. Audit admissions, mutations, confirmation decisions, reconciliation and terminal outcomes with Safe Metadata. Retain foreign resource payloads in their owning bounded contexts.
- Verify persisted state with authenticated fixtures on the governed G-4 runner, including restart, two-instance, duplicate-delivery and lost-response cases. Keep supported and historical read behavior covered during migration.

## Technical Decisions

- EventStore/platform owns task admission, state transitions, leases, checkpoints, retries, cancellation and retention. Projects owns workflow definitions, domain guards, completion conditions and Recovery Action Codes.
- Each saga owner step uses a deterministic task-step key and expected version. Persist owner receipts before advancing, query authoritative status before retrying an unknown response, and use explicit idempotent compensation or retain the work for reconciliation. Reconciliation rechecks the original actor and resumes only the original task bindings. Projects never automatically deletes a foreign resource.
- Folder provisioning or validation precedes Project activation. Projects owns the exclusive Active Project claim per Tenant and Folder. The claim generation fences commitments and releases. Recover unknown outcomes from persisted receipts and preserve the reservation while the Project commit is uncertain. Conversations owns native membership, including durable prior-membership evidence used to govern later moves. Folders and Memories own their resources, authorization and lifecycle.
- Qualify each writer slice in SDK test composition. Keep one deployed writer and preserve replay, command equivalence, legacy fencing and rollback evidence. Ship the writer change in one drained, fenced cutover, with mutation freeze and roll-forward as recovery after cutover.

## UX & Interaction Patterns

- Separate accepted work, waiting, recovery and verified completion in user-visible feedback. Keep status and recovery accessible across approved surfaces. Expired or stale confirmation returns `RenewPreview`. Lost admission responses can be recovered with task polling or the same idempotency key.
- Use server-issued opaque, single-use Confirmation Artifacts for governed confirmation and consume them atomically with task admission inside the Projects-owned confidential component. Bind them to the actor, Tenant, interactive session, surface, action, targets, request, Preview, authorization and current versions. They expire after 15 minutes. A qualifying additive pick can use five-minute Selection Evidence. A missing or unusable pick follows `RequestPreview`.

## Cross-Story Dependencies

- Epic 7 requires owner-approved, immutable G-1 Durable Task and Confirmation Artifact capabilities and G-2 sibling contracts for authorization, expected versions, step idempotency, status receipts and compensation. It builds on the applicable Epic 6 reads.
- The first writer requires accepted creation contracts, Durable Task and Folder inputs, and DW-63 SDK-host integration. The [accepted 7.1-P2 capability pin](evidence/epic7/7.1-P2-capability-pin.json) records the pinned Durable Task and Folder provisioning scope. The general `NOT_READY` allowance covers isolated prerequisite qualification. Sibling work needs repository-local authorization.
- Association target selection consumes the independently accepted Folder-first writer capability. Positive actor-selected Folder integration depends on the delivered selection component. Story 7.17 requires G-5 delegated credentials and the real EventStore authorization validator ahead of Epic 8 release.
