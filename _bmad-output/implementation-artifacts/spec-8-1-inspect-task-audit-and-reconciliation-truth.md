---
title: Inspect task, audit, and reconciliation truth
type: feature
created: '2026-11-22'
status: in-progress
baseline_commit: baf6fa066d973eb50bfbb2d53168146da01968d6
route: dispatch
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-8-context.md'
---

<frozen-after-approval reason="human-owned intent">

## Intent

Give Tenant Operators and Tenant Project Administrators an authenticated view of one Project’s audit history and one permitted Durable Task’s current state and reconciliation outcome. Web and CLI users must see the same committed facts and stable identifiers.

## Boundaries & Constraints

Authorize every read against the current Tenant, actor and action, including continuation pages. Return only Safe Metadata with opaque or Tenant-salted identifiers. Persist bounded audit facts, keep Resolution Traces and export bodies transient, and keep polls, retries and intermediate states in telemetry. Preserve the reversible legacy read route. The 22 November scheduling change permits early development. The existing Epic 7 dependencies, Epic 8 fixtures and evidence still govern acceptance.

## I/O & Edge-Case Matrix

| Given | When | Then |
| --- | --- | --- |
| Authorized operator | Pages one Project’s audit | Return stable IDs, safe outcomes, timestamps and opaque receipt references. A repeated idempotent event appears once. |
| Missing, forbidden or other-Tenant target | Caller opens a single target, uses Preview or polls a task | Return generalized `Denied` without identifying the target. A cursor used outside its authorized scope discloses no rows. |
| Pre-activation task | Any permitted role polls | Return only task identity, status, safe reason and recovery codes, timestamps and expiry. Keep the reserved Project and resource identities private. |
| Admitted task in `NeedsAttention` | An authorized Administrator reconciles | Recheck the original actor at commit against the bound Tenant, intent, targets and Preview. Resume the original checkpoint if authorized. Confirmed denial produces terminal `Rejected` and one audit outcome. Unavailable authority permits no effect. |
| Unauthorized requester or changed targets, proposal, resolution or intent | Reconciliation is attempted | Preserve the original task. A changed intent requires a new Preview. Reconciliation issues no Confirmation Artifact and creates no task. |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/Queries/ProjectReadAudit.cs` persists request-keyed inspection and denial facts. The record currently lacks a Project timeline index.
- `src/Hexalith.Projects.Server/DaprProjectAuditTimelineReadModel.cs` replays historical Project events. The legacy diagnostics endpoint also returns descriptive fields. Keep the new read contract independently allow-listed.
- `src/Hexalith.Projects.UI/Diagnostics/ProjectReadQueryClient.cs` binds the FrontComposer session and Tenant before and after the gateway call. The supported diagnostics page currently withholds audit.
- `references/Hexalith.McpCli/tools/inventory-coverage/v1/projects-approved.json` enrolls four existing reads. The new CLI operation needs the owning module’s catalog, versioned Contracts pin and conformance vector.

## Tasks & Acceptance

- [x] Define decorated, typed Project audit and task-status queries and bounded results in `src/Hexalith.Projects.Contracts/Queries/`. Register them in `src/Hexalith.Projects.Server/ProjectsRuntimeCatalogDeclarations.cs` and implement their authorization, cursor binding and disclosure in `src/Hexalith.Projects.Server/Queries/`.
- [ ] Connect the audit timeline to committed domain and security facts. Extend the inspection writer and projection under `src/Hexalith.Projects/Projections/ProjectAuditTimeline/`. Support existing retained facts, restart and concurrent redelivery. Bind task reads and reconciliation to the platform’s durable transition authority once the Epic 7 owner supplies it.
- [ ] Connect `src/Hexalith.Projects.Client/ProjectsSupportedReadClient.cs` and `src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor`. Enable the equivalent CLI read through McpCli enrollment after the supported capability is accepted. Give unavailable capabilities an explicit state.
- [ ] Exercise both authenticated heads against the same persisted fixture. Extend `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.McpCli.cs`, `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs` and `tests/e2e/specs/projects-supported-read-browser.spec.ts`.

Given consequential activity, when reading the timeline, then admission, terminal outcome, confirmation use/cancellation/rejection, authorization denial, confirmed mutations, reconciliation, export attempts/outcomes and upstream receipts follow FR-21. Selection Evidence alone creates no durable event. Setup updates identify the admitting credential kind. Folder re-anchoring records permitted-set delta counts for a Project User and only `NoLoss` or `Loss` for an Administrator.

Given descriptive inspection, when one request returns multiple Projects, then persist one event with the count and field class. Quarantine inventory name disclosure requires one event per returned Project.

Given time advancing across retention boundaries and a restarted store, when the operator reads, then audit remains available for the longer of 365 days and applicable event-history retention. Active tasks remain pollable until terminal. Preserve terminal results and scoped idempotency records for the longer of 30 days and result lifetime.

## Verification

Run the matrix commands using the qualified tools and authenticated persisted fixtures. Capture the four evidence artifacts and compare the Web and CLI results, including denied and recovery cases.

```sh
dotnet tool run hexalith-module test --profile ops --filter Story=8.1
dotnet tool run hexalith-module test --profile ops --filter 'Story=8.1&Trait=retention'
dotnet tool run hexalith-module test --profile ci-gates --filter Story=8.1
dotnet tool run hexalith-module test --profile ci-gates --filter Category=persisted-boundary
dotnet tool run hexalith-evidence validate _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml
```

## Implementation Notes

On 22 November the requester approved implementation from `baf6fa066d973eb50bfbb2d53168146da01968d6`. The sprint tracker records Story 8.1 as `blocked` at the acceptance boundary and Epic 8 as `in-progress`.

The audit query merges confirmed domain mutations with indexed inspection and authorization-denial facts. Results carry stable opaque IDs. Ordinary multi-Project inspection writes one fact shared by the affected Projects. Quarantine inventory writes one per disclosed Project. The writer repairs an interrupted index update on replay. Historical security facts that lack a Project binding cannot be attributed to a timeline. The API reports partial coverage. Pages return at most 200 rows. Each page still reads the retained journal and index, and large-history performance remains unqualified.

The task query accepts committed observations from an explicitly authorized workflow owner and returns a target-independent unavailable result when the owner is absent. The Web preview is off by default and enabled in the managed browser fixture. McpCli's released catalog continues to expose the four previously accepted operations.

| Remaining acceptance work | Dependency or evidence |
| --- | --- |
| Execute original-actor reauthorization and terminal reconciliation against an admitted task. Verify no new task or Confirmation Artifact, and no effect on unavailable authority. | EventStore/Platform Durable Task admission (7.1-P2) and the 7.15 reconciliation workflow. |
| Verify the complete FR-21 event families and metadata. | Committed workflow, confirmation and export outcomes, upstream receipts, admitting credential kind and role-specific Folder re-anchoring facts from their producers. |
| Qualify audit, terminal task and scoped idempotency lifetimes. | Governed persisted-store and restart evidence for the 365-day and 30-day minimums and their longer applicable lifetimes. |
| Run both shipped heads on one authenticated fixture. | Accepted Contracts and McpCli enrollment, qualified Epic 8 fixtures and the four evidence rows named in the scheduling change. |

Local verification passed 207 Contracts, 154 Client, 693 domain, 1,502 Server and 297 UI tests. The full Server run used isolated Dapr and Redis. One regression reads the inspection across a Projects host and store-client restart and verifies its original ID after retry. The source-referenced gateway fixture compared independently built Web and CLI-shaped requests against persisted facts. All 35 AppHost topology tests, the TypeScript type check and all 50 sprint-guard tests passed. The simulated 730-day retention test covers the inspection record.

The managed OIDC browser launcher passed 164 startup-control tests, started Keycloak and Redis, and built the AppHost. It timed out after 600 seconds waiting for Tenants. Dapr placement and scheduler both returned HTTP 503 while fetching their initial identities. The browser assertions did not execute. The launcher stopped the AppHost, and a separate cleanup check verified the original container set and removed the private fixture files.

The repository tool manifest exposes only FrontComposer. All four prescribed module-runner commands and the evidence-validator command exited 1 because their commands were absent from the manifest.

## Spec Change Log

## Review Triage Log
