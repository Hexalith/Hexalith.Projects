---
title: Schedule early development of Story 8.1
date: '2026-11-22'
scope: Story 8.1 development
---

# Schedule early development of Story 8.1

Story 8.1 can start against the accepted Epic 6 read adapters while the Epic 7 writers are being built. On 22 November the requester selected “Prepare the scheduling change” with the existing acceptance criteria intact. Before this change, the index validator rejected a candidate that moved only 8.1 to development.

## Scheduling

The sprint guard admits `ready-for-dev`, `in-progress` and `blocked` for 8.1 alongside its existing `backlog` state. Starting implementation moves Epic 8 to `in-progress`. A blocked story can retain the parent state it had when the dependency failed. The backlog baseline remains valid for earlier sprint snapshots.

Projects can develop the safe response contracts, authorization, durable read integration and surface tests in dependency order. Keep operations unavailable on public surfaces until their authoritative sources and authorized enrollment are available. Resolve cross-repository work with its owning module. Existing wire contracts and the reversible legacy read route remain available.

## Acceptance

The early development exception ends at implementation. Moving to review or completion still requires the Epic 7 durable task and reconciliation capabilities, Epic 8 entry fixtures and blocking CI, and the existing Story 8.1 evidence. The Test Architect, Product Owner and Solution Architect retain the evidence and conformance responsibilities recorded in the epic. When these inputs are available, reconcile the guarded statuses against the actual evidence. Story 8.1 development grants no release decision.

The four required evidence rows are `fr-21`, `nfr-8`, `finding-test-001` and `release-authenticated-persisted-boundary`. The root tool manifest contains only FrontComposer. The ops runner and evidence validator each exited 1 on the current checkout because their commands were absent. Track those checks as unverified until the supported tools, fixtures and blocking CI produce their recorded artifacts.

[Story criteria](epics.md#story-81-inspect-task-audit-and-reconciliation-truth) · [Evidence matrix](implementation-readiness-traceability-matrix.yaml) · [Scheduling guard](../../tools/planning/validate_production_authority.py)
