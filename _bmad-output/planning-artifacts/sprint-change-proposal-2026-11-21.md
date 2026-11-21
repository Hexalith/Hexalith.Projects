---
title: Reconcile the Projects sprint with approved read, writer and release decisions
date: '2026-11-21'
status: applied-to-planning
mode: batch
change_scope: moderate
source_revision: 31835eb8e3826ecd799321e00c7203ff0bb27f88
---

# Projects sprint correction · 21 November 2026

## Why the plan changed

During Story 6.7 review, sprint planning found that a developer could satisfy the epic by opening a Project before requesting an unbound Conversation’s context. The approved read contract requires Conversations to confirm the membership. The same review found older machine-confirmation rules, incomplete Folder concurrency tests and a full-release evidence row that could accept an earlier revision. The PRD and finalized architecture already establish the decisions. [Read amendments](story-6-7-approved-read-cutover-amendments-2026-11-16.md) · [PRD validation findings V08–V10](prds/prd-Hexalith.Projects-2026-05-24/validation-report.md) · [Architecture decision AD-35](architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#ad-35--adopted-projects-owns-the-exclusive-active-project-folder-claim)

## Corrections applied

| Work | Earlier criterion | Acceptance after correction |
| --- | --- | --- |
| Epic 6 reads and the Epic 8 Chatbot companion | An opened Project could stand in for missing membership, and the companion could treat a projected link as an exemption. | The original actor and delegation must have current native membership evidence for the named Conversation, Tenant and Project. Exercise an explicit open on an unbound Conversation and a stale advisory link separately. With readable Project and missing proof, withhold Setup, references and first-response admission. FR-16, FR-17 and FR-20 now point to the Story 6.7 source packet. |
| Machine confirmation and selection | The CLI and MCP stories and UX journey could grant confirmation after an interactive-session claim or release approval. | Both public McpCli heads refuse confirmation-required and UI-only actions and cannot mint Selection Evidence. Exercise the refusal even with an interactive claim and an accepted release. The actor uses the registered confidential component appropriate to the role and action. DW-84’s navigation now names the four enrolled inspection operations. |
| Story 7.1 and dependent writers | A retry within one Project and a check for an occupied Folder could pass without racing two Project streams. | Folders supplies current authorization. Projects serializes the Tenant/Folder claim and fences the Project commit by generation. Race two Projects, interrupt the winner, resume on another replica and replay a stale release. Verify the receipt and both histories. Protect legacy incumbents before enabling a key. Carry the claim through proposal creation, replacement, archive, restore and legacy repair. |
| Stories 8.7 and 8.11-P1 | The canonical smoke row asked for a passing authenticated run without identifying the candidate. | For the full profile, bind source SHA, managed run and job, persisted end state and recovery to the release evidence. Exercise missing, failed, skipped, wrong-SHA and matching passing results. The two Contracts profiles use their own package checks. The historical 19-pass/56-fail smoke result retains its failed disposition. |

The [epics](epics.md), [UX journeys](ux-design-specification.md), [canonical evidence matrix](implementation-readiness-traceability-matrix.yaml) and [human-readable matrix](implementation-readiness-traceability-matrix.md) contain the revised acceptance criteria. The [sprint tracker](../implementation-artifacts/sprint-status.yaml) records the correction and the [deferred-work ledger](../implementation-artifacts/deferred-work.md) closes DW-84 for the wording change.

## Scope and sequencing

Apply the correction to the existing stories. The effort is a planning revision plus review of the affected evidence rows. New epics, runtime changes and changes to the release schedule are outside this correction. The principal implementation risk is a false pass from checking a single Project stream, an advisory link or a green build. Each now has a negative acceptance case. Owner availability and execution of the existing gates determine delivery dates.

## Handoff

- **Projects and the Chatbot owner** review the native membership cases against the Story 6.7 source packet and the separately owned 8.8-P3 companion evidence. Story 6.7 is in review. [Read cutover evidence](../../docs/read-cutover.md)
- **Projects and the Folders owner** supply claim and authorization evidence respectively. The Test Architect verifies the race, recovery, legacy incumbent and fencing cases at the first writer gate. Story 7.1 is blocked.
- **Projects UX and McpCli owners** verify the two public heads refuse confirmation and selection before and after release. DW-84 closure covers the planning text.
- **Release Engineering and the Test Architect** attach the full candidate’s managed journey to 8.11-P1. Jerome and John own the terminal production decision. Stories 8.8 and 8.11 are blocked. [Release evidence handoff](sprint-change-proposal-2026-11-20.md)
