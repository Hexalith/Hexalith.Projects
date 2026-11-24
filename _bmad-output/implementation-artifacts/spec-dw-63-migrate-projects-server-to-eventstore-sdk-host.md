---
title: 'DW-63 Migrate Projects Server to the EventStore SDK host'
type: 'refactor'
created: '2026-11-24'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '5f1d955b13c69a9c24b6a6c2d185770da2197355'
context: ['{project-root}/module/README.md', '{project-root}/docs/read-cutover.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Projects owns its `/process`, `/project` and `/query` mappers. This leaves its Server outside the SDK’s route validation and canonical catalog dispatch. The current SDK sends HTTP 200 for query failures, which changes both the Projects transport and how the Gateway consumes failures.

**Approach:** Adopt assembly-explicit `AddEventStoreDomainService` and `UseEventStoreDomainService` for the Server. Add opt-in query response mapping in EventStore, then qualify the Projects host with its existing keyed processor, historical fold and supported read behavior.

## Boundaries & Constraints

**Always:** Preserve the v1 REST and client contracts, query and command discriminators, event history, state keys, both projection stores, audit effects and cursor scope. Require operation-specific workload, sidecar and catalog admission on SDK callbacks. Retain human principal and current owner checks. Keep the existing command processor as the single writer during host qualification.

**Cutover authority:** This work covers source changes and isolated verification. Story 7.1 writer cutover requires AD-17 replay and command equivalence, writer drain and fencing, AD-35 exclusive claims and executable rollback. The approved recovery for a writer that loses the retained floor is mutation freeze and forward recovery. AD-25 governs runtime retirement.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Failure Behavior |
|---|---|---|---|
| Query response | Valid call, invalid envelope or cursor, missing or unauthorized resource | Same success payload, 400 invalid input for the six existing failure-normalized queries and indistinguishable 404 denial | Other query failures retain their existing 404 mapping. Unavailable administrator verification retains the SDK 503. |
| Command dispatch | Historical state and compatible command envelope | Existing wire events and metadata for success/rejection, empty events for an idempotent no-op | Unauthorized or unadmitted requests cannot dispatch or append. |
| Projection and replay | Ordered history and named delivery with a valid catalog | Same legacy fold and named read-model contents, including `ProjectFolderCreationPending` history | Wrong tenant/domain, invalid catalog or replay input fails closed. Duplicate delivery preserves the persisted end-state. |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` owns the canonical routes, endpoint policies and query response. Behavior confirmed in source `204cc0eb990e3bfa7d806eeb9a84fcdfd9252d9f` and package `5.5.1`. `DomainServiceRequestRouter` uses keyed `IDomainProcessor` and requires a separate replay registration. `DaprDomainQueryInvoker` maps receiver 404/403 to safe denial and forwards error details from successful HTTP responses.
- `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs` holds the keyed processor, query registrations and failure mapping. Reuse `ProjectQueryEnvelopePrincipalBinding`, `ProjectsDomainProcessor` and `ProjectStateApply`. Preserve checks of the original authenticated query when the SDK normalizes administrator flags.
- `src/Hexalith.Projects.Server/ProjectsDevelopmentCatalogExtensions.cs` registers synthetic discovery. Replace it with SDK discovery plus the existing processor payload inventory. Reuse the named handlers in `Projections/ProjectReads/` and `Projections/ConversationStartSetup/`.

## Tasks & Acceptance

**Execution:**

- [ ] `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` — add an opt-in response policy after authorized query dispatch with unchanged default SDK behavior. Test policy invocation, authorization ordering and administrator-verifier unavailability in the EventStore owner repository. Consume the approved source and package through existing dependency ownership.
- [ ] `src/Hexalith.Projects.Server/Program.cs`, `ProjectsServerServiceCollectionExtensions.cs` and `ProjectsDevelopmentCatalogExtensions.cs` in that directory — register the SDK once, move query response mapping into the compatibility policy, and let the SDK map canonical endpoints and health. Keep the retained HTTP routes under explicit human authorization and qualify both normal and private catalog profiles with real catalog admission.
- [x] `src/Hexalith.Projects.Server/ProjectProjectionHandler.cs` and `ProjectsDomainProcessor.cs` in the Server directory — register the legacy full replay projection as an SDK handler and expose aggregate reconstruction through the SDK replay seam. Compare ordered history, nested snapshots and exact wire outcomes against the baseline.
- [ ] `src/Hexalith.Projects.AppHost/Program.cs`, `module/hexalith.module-manifest.v2.json`, `module/README.md` and `tests/Hexalith.Projects.Server.Tests/` — supply SDK catalog admission and peer binding in the ordinary local topology. Reconcile declared callbacks and catalog metadata with the host. Cover missing or wrong credentials, actor bindings, route ownership, statuses and persisted effects. Enroll additional callbacks only with scoped grants.

**Acceptance Criteria:**

- Given the actual Server, when the approved catalog is admitted, then each canonical receiver has one SDK route and only the intended caller, operation and audience can reach its handler.
- Given golden histories, when both hosts process the same envelopes and deliver or rebuild projections, then replayed state, command wire results and persisted projections agree at their original sequences and global positions.
- Given the authenticated read fixtures, when the Gateway and retained clients read the candidate, then v4 comparison and rollback receipts pass for all seven supported operations and store deltas match the required effects.

## Implementation Notes

- The Projects executable maps the SDK receiver and keeps the existing `projects` keyed command processor. `/replay-state` and legacy `/project` use the same stored-event fold. The private profiles accept only exact POST grants, including where Dapr ACL prefix matching can reach an auxiliary route. Positive admission in the ordinary AppHost awaits verification with owner-held Redis catalog custody and a bound workload issuer. [Local configuration](../../module/README.md)
- Working-tree source evidence includes 7 EventStore response-policy and catalog-fence cases, 252 focused Projects Server passes, the separately executed Dapr restart case, 2 domain history cases and client compatibility. The authenticated v4 comparison passed 77 cells and the separate rollback invocation passed its matrix and all 7 retained reads with zero domain writes. The matrix manifest binds the working-tree files and identifies the Git revision as the baseline. [SDK tests](../../artifacts/dw-63/eventstore-fence-and-response.xml) · [Focused tests](../../artifacts/dw-63/source-focused-scoped-candidate.xml) · [Dapr restart](../../artifacts/dw-63/source-persisted-restart.xml) · [v4 packet](../../artifacts/dw-63/v4-fixture-control/v4-packet.json) · [Rollback receipt](../../artifacts/dw-63/v4-fixture-control/v4-rollback-proof.json)
- The final comparison fixture uses direct Conversation fault-injection transport to prevent circuit state from one cell influencing another. The normal application registers SDK resilience. [Read cutover verification](../../docs/read-cutover.md)
- The live narrow run recorded committed catalog generations for Projects, EventStore, Tenants and Folders. Its distributed build timed out at 600 seconds. The retry, `run-with-temp bash -c 'exec python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 1500 --artifacts "$TMPDIR/distributed-build" --evidence artifacts/dw-63/live-regressions-long'`, built the Integration harness and exited 1. EventStore returned HTTP 500 for `Hexalith.Folders.Commands.CreateFolder` during fixture seeding, before any Projects business callback. The owning invoker logged a domain-service invocation failure. The downstream Dapr detail was unavailable. The runner verified removal of its AppHost, containers and sidecars. [Live catalog commits](../../artifacts/dw-63/live-catalog-commits.log) · [Owner failure trace](../../artifacts/dw-63/live-regressions-long/owner-error-trace.json) · [Native test result](../../artifacts/dw-63/live-regressions-long/duplicate/distributed.xml)
- Release package-mode compilation fails with `CS0246` for `IDomainQueryResponsePolicy` against EventStore 5.5.1. EventStore publication and consumption through the shared package pin remain required before DW-63 acceptance. [Release build output](../../artifacts/dw-63/release-package-server-build.log)

## Spec Change Log

## Review Triage Log

## Verification

- Prepare local Debug source mapping using `hexalith-module prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace "$TMPDIR/source-mapping" --output json`. Run the focused Server tests in `ProjectsRuntimeCatalogTests`, `ProjectsDomainProcessorTests`, `SupportedProjectReadHttpTests` and `ProjectsAuthenticationContractTests`, plus the domain `ReadCutoverHistoryTests`. Use `run-with-temp` and the emitted mapping environment.
- Run `run-with-temp python3 tools/qualification/run_supported_project_reads.py --regressions`, the exact-source 77-cell v4 matrix and seven independent retained rollback reads in `docs/read-cutover.md`. Validate the published SDK dependency through the separate Release package-mode Server lane.
- Run `python3 tools/planning/validate_production_authority.py --validate-index` and record the new route, authorization, projection and read evidence before DW-63 review.
