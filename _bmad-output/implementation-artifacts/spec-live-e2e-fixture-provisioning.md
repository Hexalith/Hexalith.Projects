---
title: 'Live E2E fixture provisioning'
type: 'feature'
created: '2026-08-26'
status: in-progress
baseline_commit: '311aa85c8e81c7c0b19d5c0004ddf36ceafd5651'
baseline_revision: '1e9f847169c94e275d6c7277fdb5d2d040cefc87'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** AppHost-backed Projects E2E cannot reach application behavior deterministically because the selected tenant may lack a projected access grant, sibling domains have no provisioned reference state, UI scenarios use placeholder project IDs, and several live tests reuse fixed identifiers.

**Approach:** Provision deterministic projected-Tenant access plus sibling reference, proposal, and UI-state fixtures with per-run identifiers and cleanup. Replace fixed live IDs with fixture outputs and add parallel-safety and convergence coverage.

## Boundaries & Constraints

**Always:** Use supported authenticated APIs and observable convergence checks; derive the authoritative tenant and principal from the live token; keep the shared tenant repair idempotent and serialized; generate bounded identifiers from run, worker, retry, repeat, and scenario identity; archive created Projects and wait for archived convergence; keep sibling fixtures metadata-only and accessible only in an explicitly enabled AppHost E2E profile.

Treat unavailable host watcher capacity, including Dapr startup `ENOSPC`, as an environmental prerequisite failure that blocks story completion but neither determines product correctness nor provides acceptance evidence. Do not stop unrelated workloads or modify host limits without explicit operator authorization.

**Block If:** The installed Tenants/EventStore contracts cannot safely converge an existing tenant membership, or a required sibling behavior cannot be represented through its public HTTP/client contract without changing a sibling repository or writing a production projection directly.

**Never:** Edit the deferred-work ledger or bundle intent; allow an implementation subagent to edit this spec or another workflow-owned control artifact, including frontmatter status or baseline fields, task checkboxes, change or triage logs, and auto-run results (the subagent reports completion and its exact changed paths in its handoff only); initialize nested submodules; seed Projects/Tenant projections directly; add production backdoors; hard-delete or disable the shared tenant; use fixed live entity IDs, guessed ports, sleeps, or browser interception as API fixture provisioning.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| First live run | Authorized token but missing/stale Projects tenant projection | Supported tenant commands reach terminal success and Projects list reaches 200 before tests start | Bounded poll reports command/status response and last Projects response |
| Existing tenant | Tenant and principal membership already exist | Provisioning is idempotent and refreshes only when needed | Domain rejection is accepted only when postcondition converges |
| Parallel workers | Same run, different worker/retry/repeat/scenario dimensions | Every Project, conversation, folder, file, workspace, proposal, correlation, task, and idempotency ID is deterministic and disjoint | Duplicate dimensions fail a factory contract test |
| Fixture failure | Setup, test, or teardown fails | Already-created resources are cleaned in reverse order; primary failure is preserved | Cleanup failures are attached as diagnostics |
| E2E profile disabled | Normal AppHost/server startup | No fixture ingress or sibling stub registration is reachable | Fail closed; production clients retain normal sibling addresses |

</intent-contract>

## Code Map

- `src/Hexalith.Projects.AppHost/Program.cs` -- current EventStore, Tenants, Projects, workers, UI topology and E2E-profile composition point.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` -- production sibling client registrations that the E2E profile must satisfy without weakening defaults.
- `tests/e2e/global-setup.ts` -- live credential validation/token warm-up and serialized tenant readiness gate.
- `tests/e2e/playwright.config.ts` -- live environment contract and current single-worker restriction.
- `tests/e2e/support/merged-fixtures.ts` -- composed tenant and seeded-Project fixtures consumed by specs.
- `tests/e2e/support/fixtures/projects-fixtures.ts` -- Project create/convergence/archive lifecycle.
- `tests/e2e/support/helpers/readiness.ts` -- bounded polling primitive.
- `tests/e2e/specs/projects-{file-reference,resolution,proposal,resolution-trace,reference-health,console-shell}.spec.ts` -- fixed IDs and placeholder states to replace.
- `tests/e2e/README.md`, `docs/runbooks/projects-topology.md`, `tests/e2e/.env.example` -- live runner discovery and fixture-profile operator contract.
- `.bmad-loop/runs/20260826-164827-cb61/bundles/live-e2e-fixture-provisioning/intent.md` and `_bmad-output/implementation-artifacts/spec-5-12-live-apphost-operational-console-verification.md` -- read-only source evidence.

## Tasks & Acceptance

**Execution:**
- `tests/e2e/support/factories/live-fixture-identities.ts` and `tests/e2e/specs/live-fixture-identities.spec.ts` -- add a pure bounded identity factory and verify deterministic, collision-free run/worker/retry/repeat/scenario dimensions.
- `tests/e2e/support/helpers/eventstore-api-client.ts`, `tests/e2e/support/helpers/tenant-access-readiness.ts`, and `tests/e2e/global-setup.ts` -- submit/poll authenticated tenant commands and gate live execution on the outer Projects authorization response.
- `tests/Hexalith.Projects.E2E.Fixtures/Hexalith.Projects.E2E.Fixtures.csproj` and its one-type-per-file C# sources -- implement metadata-only Conversations/Folders/Memories public contracts plus a run-scoped control API.
- `src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj`, `src/Hexalith.Projects.AppHost/Program.cs`, `Hexalith.Projects.slnx`, and `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` -- compose and verify the sibling fixture roles only under the explicit E2E profile, fail closed otherwise.
- `tests/e2e/support/helpers/live-fixtures-api-client.ts`, `tests/e2e/support/fixtures/live-fixtures.ts`, `tests/e2e/support/fixtures/projects-fixtures.ts`, and `tests/e2e/support/merged-fixtures.ts` -- seed the per-test fixture graph, expose its IDs/states, and clean in reverse order while waiting for Project archival.
- `tests/e2e/specs/projects-file-reference.spec.ts`, `tests/e2e/specs/projects-resolution.spec.ts`, `tests/e2e/specs/projects-proposal.spec.ts`, `tests/e2e/specs/projects-resolution-trace.spec.ts`, `tests/e2e/specs/projects-reference-health.spec.ts`, and `tests/e2e/specs/projects-console-shell.spec.ts` -- replace fixed IDs and placeholder routes with fixture outputs and observable API/UI scenarios.
- `tests/e2e/specs/live-fixtures-lifecycle.spec.ts` and `tests/e2e/playwright.config.ts` -- verify convergence, reverse cleanup/failure diagnostics, and enable two-worker live isolation.
- `tests/e2e/.env.example`, `tests/e2e/README.md`, and `docs/runbooks/projects-topology.md` -- document dynamic EventStore/fixture endpoint discovery, explicit profile startup, and cleanup.

**Acceptance Criteria:**
- Given a clean supported AppHost and valid live credentials, when global setup completes, then the token tenant/principal is present and fresh in Projects authorization without manual database or projection edits.
- Given two live workers and retries/repeats, when fixture graphs run concurrently, then their identifiers and sibling state are disjoint and each graph tears down without affecting another.
- Given reference, resolution, proposal, trace, warning, empty, and feedback scenarios, when live specs execute, then state comes from fixture outputs and assertions observe real Projects API/UI behavior.
- Given the E2E profile is absent, when AppHost starts, then no fixture control surface or stub sibling binding is exposed.

## Spec Change Log

- 2026-10-06: Resumed build and captured the current full baseline commit. Implementation halted under the existing tenant-contract Block If condition; no source or sibling changes were made. Live acceptance remains unsatisfied.
- 2026-11-14: Rechecked the tenant-contract prerequisite against the current workspace. The Block If condition still applies; no implementation changes were made. Recorded focused passes and the current solution-build failures below. Status and existing baseline identifiers are preserved.
- 2026-11-14: Implemented the separately authorized tenant-access recovery prerequisite across the root-declared EventStore, Tenants, and Projects sources. The supported contract and verification results are recorded below. Full fixture provisioning remains a subsequent build; this spec's existing status and baseline identifiers are preserved.

## Review Triage Log

## Design Notes

The test host is a runner-owned compatibility fixture, not a replacement sibling implementation. AppHost may launch role-specific instances under the sibling app identities only when the explicit live-fixture profile is enabled; seed/reset operations remain scoped by run identity and expose metadata required by Projects, never content payloads.

## Verification

**Commands:**
- `dotnet build Hexalith.Projects.slnx --no-restore` -- expected: all projects compile with warnings as errors.
- `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --no-build --no-restore` -- expected: fixture-profile and fail-closed coverage passes.
- `npm --prefix tests/e2e run typecheck` -- expected: strict TypeScript checks pass.
- `npm --prefix tests/e2e test -- --workers=2` with the documented AppHost fixture profile -- expected: live suite uses disjoint fixtures, converges, and cleans up.

**Live environment prerequisite:** Before live test collection, the explicit fixture profile must reach authenticated outer Projects readiness. If Dapr startup fails because host watcher capacity is unavailable, restore sufficient capacity or use an isolated host, then rerun every live matrix covering test; each must collect and pass, and skipped or uncollected tests remain unsatisfied.

## Build Attempt — 2026-10-06

**Outcome:** Blocked by the existing intent contract. Status remains `in-progress`;
no implementation tasks or acceptance criteria are newly marked complete.

**Contract evidence:** Existing membership causes `AddUserToTenant` to reject;
a same-role `ChangeUserRole` is a no-op; `UpdateTenant` emits only `TenantUpdated`.
Projects requires lifecycle and membership events to restore its authorization
projection. Installed EventStore projection replay rebuilds the owning Tenants
read model through `/project`, rather than Projects' independent tenant-event
subscription projection. No supported safe refresh/republication contract was found.

- [Tenants membership rejection](../../references/Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#L112)
- [Tenants same-role no-op](../../references/Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#L215)
- [Projects authorization event requirements](../../src/Hexalith.Projects/Projections/TenantAccess/ProjectTenantAccessHandler.cs#L119)
- [EventStore owning-domain projection delivery](../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/ProjectionUpdateOrchestrator.cs#L990)

**Checks run against existing code:**

| Command | Result |
| --- | --- |
| `npm --prefix tests/e2e run typecheck` | Exit 0. |
| `npm --prefix tests/e2e test -- --workers=2 --project=chromium specs/live-fixture-identities.spec.ts` | Exit 0; 3 passed. |
| `dotnet build Hexalith.Projects.slnx --no-restore` | Exit 1; CS1704 duplicate FrontComposer Shell/Contracts assemblies. |
| `Projects__E2E__LiveFixtures=true HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --format Json --non-interactive` | Exit 2; CS1501 at `FoldersProjectFolderDirectory.cs:66`: no `GetEffectivePermissionsAsync` overload takes 5 arguments. |
| `aspire ps --non-interactive` | No running AppHost. |

Integration and live matrix verification did not complete. No watcher-capacity
failure was observed. These focused passes do not establish acceptance.
The referenced `.bmad-loop/runs/20260826-164827-cb61/bundles/live-e2e-fixture-provisioning/intent.md`
is absent. Existing readiness still probes create-validation HTTP 400 instead of
the required list HTTP 200, and some cleanup failure paths suppress diagnostics;
these remain implementation gaps after the prerequisite contract is resolved.

## Build Attempt — 2026-11-14

**Outcome:** Blocked by the existing tenant-contract prerequisite. Status remains
`in-progress`; no task or acceptance criterion is newly marked complete. Current
HEAD is `9c804569031eee647207fa3b0d88407dadca38a0`; the workspace contains
pre-existing changes. Existing baseline identifiers are unchanged. Only this spec
was updated during this attempt; no implementation or sibling files were changed.

**Current contract evidence:** Existing membership still rejects `AddUserToTenant`,
an unchanged `ChangeUserRole` still returns `NoOp`, and `EnableTenant` rejects an
already active tenant. `UpdateTenant` emits only `TenantUpdated`, which restores
neither lifecycle nor membership in Projects. Projects receives these events
through an independent Workers subscription. Tenants' declared projection outputs
are `tenants`, `tenant-index`, and `tenant-audit`; EventStore projection replay
resolves owning-domain `project`/`project/v2` targets rather than republishing to
the Projects subscriber. Command replay also rejects completed commands.

- [Current Tenants command behavior](../../references/Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs)
- [Projects authorization event application](../../src/Hexalith.Projects/Projections/TenantAccess/ProjectTenantAccessHandler.cs)
- [Independent tenant-event subscription](../../src/Hexalith.Projects.Workers/ProjectsWorkersModule.cs)
- [Tenants projection declarations](../../references/Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Routing/TenantCatalogDeclarations.cs)
- [EventStore projection target resolution](../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/ProjectionUpdateOrchestrator.cs)
- [EventStore command replay restrictions](../../references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/ReplayController.cs)

**Checks run against existing code:**

| Command | Result |
| --- | --- |
| `npm --prefix tests/e2e run typecheck` | Exit 0. |
| `npm --prefix tests/e2e test -- --workers=2 --project=chromium specs/live-fixture-identities.spec.ts` | Exit 0; 5 passed, 0 skipped. These pure tests do not establish live isolation. |
| `dotnet build tests/Hexalith.Projects.E2E.Fixtures/Hexalith.Projects.E2E.Fixtures.csproj --no-restore` | Exit 0; 0 warnings, 0 errors. |
| `dotnet build Hexalith.Projects.slnx --no-restore` | Exit 1; 281 errors. First failures include Conversations MSB4019 for missing `references/Hexalith.Conversations/references/Hexalith.Builds/Props/Directory.Packages.props`, EventStore.Contracts CS0234 for `Hexalith.Commons` in `Events/EventIdentityValidator.cs:1`, and Tenants.Contracts CS0246 for `HexalithModuleAttribute` / `HexalithModule` in `Module.cs:5`. |

The focused fixture-host build passed; no gate was weakened and no nested
submodule was initialized. No AppHost was
started or shared tenant mutated. Integration and live matrix acceptance remain
unverified. No watcher-capacity failure was observed. Existing readiness still
accepts list HTTP 503 `read_model_unavailable` plus invalid-create HTTP 400 instead
of requiring list HTTP 200. The referenced bundle `intent.md` remains absent.

**Required prerequisite:** A supported safe Tenants/EventStore refresh or
republication contract must converge existing lifecycle and membership evidence
into Projects without direct projection writes or temporary authority changes.
Once that contract is available, finish readiness and cleanup gaps and rerun the
integration and complete live matrix gates.

## Supported tenant-access recovery contract — 2026-11-14

The coordinated source implementation now provides `RefreshTenantAccess` and
`TenantAccessRefreshed`. The command reads the existing authoritative Tenant
aggregate and emits a new complete access snapshot at the next source sequence.
It preserves status, exact membership and roles, names, descriptions, configuration
values and membership history. The event carries status, memberships/roles and
configuration-key names only. Applying it to Tenants is a business-state no-op.

**Admission:** Use a host enrolled with the compiled Tenants catalog and intent
adapters, including `tenants:refresh-access`, and the updated Projects worker.
Require a real authenticated human with that operation permission and a current
TenantOwner role or source-verified global-administrator authority. Claiming
global-administrator status alone is insufficient. Recovery works for disabled
tenants while preserving their disabled status and denial. Payload TenantId must
match the authenticated command target. The ordinary legacy AppHost's healthy
resource state alone does not establish catalog enrollment.

**Supported HTTP ingress:** Submit `POST /api/v1/commands` with bearer credentials
and `X-Hexalith-Scope: platform:tenants`. Use this body, taking the tenant and
principal from the live token rather than a fixture constant:

```json
{
  "messageId": "<recovery-operation-ulid>",
  "tenant": "platform:tenants",
  "domain": "tenants",
  "aggregateId": "<token-tenant>",
  "commandType": "RefreshTenantAccess",
  "payload": { "TenantId": "<token-tenant>" },
  "correlationId": "<recovery-correlation-ulid>"
}
```

The canonical wire alias is `refresh-tenant-access`; the decorated Tenants REST
surface is `POST /api/tenants/{tenantId}/refresh-access`. Reserved storage tenant
`system` is never a public request scope. Poll authenticated
`GET /api/v1/commands/status/{messageId}` with the same platform-scope header and
the required `tenants:read` permission until terminal completion. A duplicate or
conflict response requires observing the original command's status, rather than
assuming success. An authenticated domain permission denial can be returned
synchronously as HTTP 422 with `InsufficientPermissionsRejection`; anonymous
requests return HTTP 401. Neither response is recovery success.

**Retry and convergence semantics:** Retain one operation/message ID across
concurrent retries of one recovery. EventStore serializes aggregate commands,
deduplicates that ID and durably publishes the single resulting evidence event.
For a later recovery, including projection loss after a prior completed recovery,
use a fresh operation ID: completed subscriber markers and command deduplication
remain intact. Bound total attempts and the overall deadline in the subsequent
fixture helper. A snapshot is limited to 4,096 membership/configuration entries
and 262,144 serialized UTF-8 bytes; oversized snapshots reject without partial
evidence. Reusable snapshot budgeting and conditional persistence belong to
EventStore, not fixture or domain-specific Dapr plumbing.

Projects replaces the complete access snapshot atomically through the supported
EventStore read-model store/write policy. It removes obsolete principals, retains
exact roles and lifecycle, replaces configuration-key evidence, advances source
and snapshot watermarks, and resets superseded conflict/malformed evidence.
Identical messages remain idempotent; contradictory equal-sequence evidence fails
closed. Older evidence cannot overwrite a later removal or disable. If a later
live event overtakes a refresh before its incomplete base can be repaired,
Projects fails closed and a fresh bounded recovery operation is required.
After a complete snapshot, forward gaps and unseen delayed deltas newer than
that snapshot also fail closed until fresh complete evidence. Events already
covered by the snapshot remain safely ignored. Legacy non-checkpointed Tenants
caches skip this access-only event so it cannot hide delayed business events;
durable Tenants consumers retain contiguous checkpoint recovery.

Command `Completed` therefore proves source publication, not outer authorization
convergence. Require authenticated `GET /api/v1/projects` with the unchanged token
tenant/principal to return **HTTP 200**. HTTP 400, safe denial, and HTTP 503
`read_model_unavailable` do not satisfy this postcondition. Recovery cannot create
a missing Project list read model or grant authority absent from the source.

**Subsequent fixture build:** Replace the current duplicate-membership/rename
repair path with this contract, correct the helper's reserved `system` request
scope, and enforce the outer 200 postcondition. Keep shared-tenant setup serialized
and bounded. Preserve existing roles and status; never promote a member or enable
a disabled shared tenant to make fixtures run. Identity generation, sibling
fixtures, cleanup, parallel browser execution and their acceptance matrix remain
pending under the original tasks above.

**Delivery boundary:** These are coordinated Debug/source changes. Package-mode
consumers require an EventStore Client release containing snapshot limits, then a
Tenants release containing the new contract and handler, followed by the Projects
consumer update and the owning package-cohort qualification. No dependency pin or
package gate was bypassed in this build.

### Recovery verification evidence

The final local proof used real OIDC, the actual EventStore/Tenants/Projects
owners and worker, and persisted state in a fresh owned backend. The source
tenant and membership already existed before recovery. The same pre-recovery
token and principal were reused; no status or membership changes were used as a
repair. This is local source verification, not migration of a shared deployment
or production/package qualification.

| Verification | Result |
| --- | --- |
| Observable stale authorization | Unchanged persisted evidence reached age 90.60 s under the recovery-only 90 s policy; Projects list returned HTTP 404 before recovery. Both actual runtime freshness settings were observed. |
| Authenticated recovery | Completed through `platform:tenants`; one new `TenantAccessRefreshed` event. |
| Anonymous request | HTTP 401. |
| Authenticated foreign principal | HTTP 422 with `InsufficientPermissionsRejection`; durable `Rejected` status and unchanged Projects access evidence. |
| Eight concurrent retries of one operation ID | Eight HTTP 202 responses, one source snapshot event at sequence 4. |
| Later fresh operation without resetting subscriber/store state | HTTP 202 and `Completed`; exactly one additional snapshot at sequence 5. |
| Physical persisted projection rereads | Logical versions 2 → 3 → 4; source, projection and snapshot watermarks 4 then 5. Exact membership, roles, lifecycle and configuration-key evidence preserved. |
| Same token/principal after recovery | `GET /api/v1/projects` returned HTTP **200**. |
| Missing/stale projection, disabled source, obsolete roles/members, delivery gaps, delayed revocation and conditional first-create races | Focused tests passed, including fresh-store rereads, bounded retry exhaustion, real payload conflicts and malformed receiver input. No projection was directly seeded in the live proof. |
| Owned cleanup | Exact AppHost, sidecars and private containers removed; private credentials removed; pre-existing resources preserved. |

**Executed commands and outputs:**

- `python3 tools/qualification/run_supported_project_reads.py --scenario tenant-access-recovery --timeout 600 --evidence /tmp/tenant-access-recovery-live-20261114-g` — exit 0; 1 passed, 0 failed/skipped. The recovery scenario does not claim the full read or browser matrices.
- `python3 -m unittest discover -s tests/tools -p test_supported_project_reads_launcher.py` — exit 0; 150 passed.
- Individual mapped Debug/source builds and native xUnit runs — 351 passed, 0 failed/skipped across EventStore snapshot limits (4), Projects handler/authorization (30), Projects persisted recovery/worker (21), Tenants recovery/authority/validation/projections (72), complete Tenants Client (101), affected Contracts (118), and deadline/topology controls (5). Builds passed with zero warnings/errors.
- Full Tenants Contracts — 1,054 executed, 849 passed, 205 failed, 0 skipped. Remaining failures are release/governance tests reading unavailable nested Builds fixtures (`references/Hexalith.Builds/Github/governed-provenance` and `.github/workflows/domain-ci.yml`). No nested submodule was initialized. This broad gate remains blocked independently of the passing recovery checks.

Retained evidence: [summary and boundaries](evidence/tenant-access-projection-recovery-20261114/verification-summary.json),
[exact command records](evidence/tenant-access-projection-recovery-20261114/commands.json),
[live native XML](evidence/tenant-access-projection-recovery-20261114/live-recovery.xml),
[HTTP observations](evidence/tenant-access-projection-recovery-20261114/tenant-access-recovery-observations.jsonl),
[actual freshness settings](evidence/tenant-access-projection-recovery-20261114/freshness-environment.json),
[source input fingerprints](evidence/tenant-access-projection-recovery-20261114/source-manifest.json.gz),
and [broader gate diagnostics](evidence/tenant-access-projection-recovery-20261114/broad-gate.json).
The directory also retains each focused suite's XML. Tokens, passwords and
private key material are excluded.

Review fixes cover legacy consumer checkpoints, post-snapshot ordering gaps,
actual-payload fingerprint conflicts, malformed inputs, propagated polling
deadlines and positive advancing watermark/version evidence. Earlier attempts
were not accepted as final evidence: adapter CA2007, unrelated advisory readiness,
and synchronous permission-denial handling were corrected before the first proof;
rerun `e` stopped before collection with `OSError` while the filesystem was full;
rerun `f` exposed the diagnostic freshness setting and timed out before recovery.
Only task-created temporary build outputs were removed to recover disk space.
Final rerun `g` passed against stable corrected source. Full fixture tasks and
their live matrix remain pending.
