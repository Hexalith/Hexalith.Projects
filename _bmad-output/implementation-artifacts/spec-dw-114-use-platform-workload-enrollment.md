---
title: 'DW-114 Use Platform workload enrollment for the ordinary Projects AppHost'
type: 'bugfix'
created: '2026-11-28'
status: 'done'
baseline_commit: 'b4a92ba2d9649e716f71d1608a86a6cb4455f9c6'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/module/README.md', '{project-root}/references/Hexalith.Platform/docs/security/workload-binding-enrollment.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The ordinary Projects Debug AppHost needs an HTTPS workload issuer and Redis catalog custody. Platform owns the Keycloak workload-binding provider, but its disposable HTTPS launcher only enrolls the Folders cohort. The live Projects runner currently treats missing ordinary custody as an expected startup refusal.

Add a disposable Projects enrollment to Platform and consume it in the live runner's ordinary phase. Start the ordinary AppHost with fresh catalog custody for a dedicated Redis backend and retain proof of catalog admission, credential enforcement, and persisted delivery.

## Boundaries & Constraints

Keep Platform as issuer owner and use its Keycloak provider, startup realm import and finite lease. Enroll only EventStore, Projects, and Tenants with the exact invocation and source grants for the selected tenant and the Tenants storage scope. Validate HTTPS with process-scoped trust. Preserve the Folders profile and the ordinary AppHost's human authentication, source-mode requirement, and external owner-custody entry point. Keep credentials, raw catalog data and signing material private.

Limit provisioning to a freshly owned Redis backend and an owned disposable HTTPS realm. Persistent or shared installations require their existing owner configuration. DW-63 owner acceptance and deployment remain separate decisions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Failure Behavior |
| --- | --- | --- | --- |
| Enroll | Ordinary Projects cohort and selected tenant | Finite HTTPS lease with scoped credentials and normal TLS validation | Reject missing, foreign or changed cohort, scope, trust or lease |
| Start | Fresh owned Redis, matching custody and enrollment | Ordinary Debug AppHost commits a catalog with fixture profiles disabled | Fail qualification on startup refusal or an uncommitted generation |
| Verify | Authorized workload and adverse credential controls | Prove authorized command, query and projection delivery in persisted state and refuse wrong or missing credentials | Exit nonzero on incomplete proof or persisted effects on refusal |
| Tear down | Completion, failure, cancellation or expiry | Remove owned processes, containers and private material. Verify preexisting resources survive | Report leftovers and preserve recovery information |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Platform/infrastructure/keycloak-workload-binding/run_owned.py` owns lease preparation, cohort validation, realm rendering and teardown. Reuse the provider and protected manifest protocol. `validate_inventory` currently enforces the exact Folders inventory.
- `references/Hexalith.Platform/src/Hexalith.Platform.Composition/PlatformRunOwnedWorkloadEnrollment.cs` implements the single-use adoption handshake. Bind the enrollment to the Projects runner as the direct child of the Platform launcher. Aspire starts the AppHost as a descendant. Keep the Folders consumer's own manifest and probes intact.
- `src/Hexalith.Projects.AppHost/ProjectsLocalDevelopmentCatalog.cs` consumes `Projects:DevelopmentRouteCatalog`, configures the dedicated issuer and owns the ordinary callback inventory. `IsolatedLiveE2EFixtureProfile.cs` provides reference gateway and Tenants grant definitions. Use the ordinary callback inventory when deriving the Projects profile.
- `tools/qualification/run_live_project_fixtures.py` owns the private-to-ordinary transition, `prepare_private_inputs`, exact process tracking and cleanup. `tools/qualification/run_supported_project_reads.py` provides the owned Aspire launch and receiver receipt helpers.
- `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs` shows the real Gateway command, status, source stream and named-delivery observations. The retained `ProjectsDomainProcessor` writer supplies the creation path.

## Tasks & Acceptance

**Execution**

- [x] `references/Hexalith.Platform/infrastructure/keycloak-workload-binding/run_owned.py`, `adopt_run_owned.py` in that directory and `test_run_owned.py` — add the Projects profile and a verified Python adoption path. Require the bound digest on Projects queries and preserve unbound Tenants queries, distinct bootstrap source scopes and the Folders profile. Exercise real HTTPS and cross-profile refusal.
- [x] `tools/qualification/run_live_project_fixtures.py` and `tests/tools/test_live_project_fixtures_launcher.py` — adopt Platform's verified enrollment for the ordinary phase. Provision a second owned Redis and private custody. Supply trust and protected issuer, credential and receipt-key settings only to the ordinary AppHost. Treat custody refusal as a failure.
- [x] `src/Hexalith.Projects.AppHost/Program.cs` and `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` — provide the disposable run's scoped human and Tenants bootstrap inputs through the existing local Keycloak and owner bootstrap paths. Seed the selected tenant through real bootstrap, create-tenant and membership commands.
- [x] `tests/Hexalith.Projects.Integration.Tests/OrdinaryRunModeLiveTests.cs` — exercise the actual gateway, receivers and sidecars. Match the EventStore, Projects and Tenants SDK commit readbacks and assert allowed and refused credentials, persisted history and projection effects. Update `deferred-work.md` after the live criteria pass.
- [x] `module/README.md` — document the Platform-owned disposable enrollment entry point and describe its use with the ordinary Debug runner.

**Acceptance Criteria**

- Given one fresh lease and Redis backend, when the runner starts the ordinary AppHost, then EventStore, Projects, and Tenants admit the catalog and the receipt records the shared committed generation.
- Given that committed catalog, when the probe exercises commands, queries, projections and wrong or missing credentials, then authorized delivery matches stored state and refusals leave stored state intact.
- Given a finished or interrupted run, when cleanup completes, then the receipt accounts for process, container, and private-material cleanup and preexisting resource preservation.

## Implementation Notes

- Platform enrolled the exact three-workload cohort and verified 15 HTTPS assertions. The live ordinary probe used the authorized named SDK projection callback because automatic named delivery did not converge in the earlier probe.
- The retained two-phase run verified the private-to-ordinary transition, 27 catalog guards, native ordinary checks, and resource preservation. The fresh ordinary run passed with a per-run dashboard tunnel opt-out after an Aspire tunnel timeout. The final managed shell working-directory behavior was verified separately by its invocation test.
- Evidence is under `~/.local/state/hexalith-projects/dw-114-entrypoints-disabled-transition-2/` and `~/.local/state/hexalith-projects/dw-114-entrypoints-ordinary-refresh-2/`. The run JSON SHA-256 values are `922bcddd60866c264f4dff1abe3791971428d148d431fa9067887cd3a6a5deba` and `7a241ef950cf15b9eb2742c1ed82b0f421a0ec9e57dab3c53e5474b19e9eae7f`.
- Passing focused checks include 23 provider tests, 51 Platform enrollment tests, 52 fixture launcher tests, 169 supported-read launcher tests, and 7 Debug source and 1 Release package AppHost model tests.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| BH-1 Platform worktree is ahead of the recorded gitlink | medium | Fresh clones still receive the pinned Platform source. Publication and the resulting gitlink advance require a separate version-control action. Defer. |
| BH-2 Browser phases use the original checkout | medium | The process helper binds the default cwd at import. Patch the caller to pass the disposable checkout explicitly. |
| BH-3 Full Chromium evidence and tunnel setting are absent | medium | Both retained native lanes deliberately omit the full browser matrix. It remains a separate verification scope. Document the actual tunnel recovery setting for managed use. |
| BH-4 Ordinary launcher tests require installed Playwright dependencies | low | The launcher control runs a real focused-test refusal through the Node CLI. Document the prerequisite. |
| BH-5 Qualification bootstrap can target unrelated storage | high | The AppHost accepted independent Redis and custody values with only a private-directory gate. Patch the backend binding and model refusal evidence. |
| BH-6 Log capture precedes bounded cancellation teardown | high | Nine serial diagnostic commands can exhaust Platform's 75-second drain interval. Patch the interrupt path. |
| BH-7 Partial ordinary starts can lose container ownership evidence | high | The exact-id teardown path receives no ordinary fallback IDs when the Aspire graph is gone. Patch the run-label census and exact cleanup. |
| BH-8 Ordinary material identities are captured late | medium | The runner observes both locators after readiness waits. Patch early capture and retain available partial metadata. |
| BH-9 Recovery scans only the isolated-phase locator directory | medium | Ordinary locators are written under the ordinary directory. Patch recovery to report either phase. |
| BH-10 Unadopted lease cleanup is marked verified early | medium | Platform creates its CA and trust before issuing a manifest. Patch pre-adoption release and require verified absence. |
| BH-11 Stopped preexisting containers are absent from the census | medium | The shared helper uses running short IDs. Patch this runner's snapshots to use the existing all-container full-ID reader. |
| BH-12 Completed dispatch can mask work during credential refusal | high | The native probe sends the same dispatch after successful projection. Patch both negative cases to run before first delivery and assert absence. |
| BH-13 Automatic projection delivery remains unverified | false | The accepted scope is persisted delivery through the real SDK callback. Evidence explicitly identifies the delivery path and records the earlier automatic convergence limitation. |
| EC-1 Browser test cwd points at original files | medium | The default process-helper argument captures the original root. Patch with BH-2. |
| EC-2 Platform can terminate the runner during failure diagnostics | high | The bounded drain interval is shorter than serial diagnostic waits. Patch with BH-6. |
| EC-3 Failed startup receipt can omit owned containers | high | Process absence alone cannot prove Docker absence. Patch with BH-7. |
| EC-4 Recovery omits ordinary catalog and mTLS locators | medium | The ordinary profile uses its own private child directory. Patch with BH-9. |
| EC-5 Bootstrap polling can read the initial partial receipt | medium | Tenants writes the initial attempt file in place, then atomically replaces it on completion. Patch bounded polling for the matching completed receipt. |
| EC-6 Credential refusals use already-applied source | high | Idempotency can mask effects before rejection. Patch with BH-12. |
| EC-7 A slow native run can outlive its initial workload tokens | low | Token expiry makes the probe fail visibly under the existing time budget. Separate issuance plumbing would add another credential path. Reject for this pass. |
| EC-8 Interrupted runs can lose cleanup evidence | high | The same serial-log delay can exhaust the supervising lease's grace period before cleanup starts. Patch with BH-6. |
| VG-1 The projection refusal proof can pass after a no-op dispatch | high | The existing duplicate test confirms completed deliveries may return AlreadyCompleted without a store change. Patch with BH-12. |
| VG-2 The persistent Keycloak refusal has no model control | medium | Existing model and live tests exercise the two settings separately. Patch a test with otherwise valid disposable inputs. |
| VG-3 The adopter signature verification lacks a wrong-key probe | medium | The HTTPS controls test valid signatures and bad requests, which fail upstream. Patch a direct invalid-signature check. |
| VG-4 Partial ordinary failure skips Docker census | high | The preexisting fallback was keyed to the private phase. Patch with BH-7. |
| VG-5 Recovery metadata omits ordinary locators | medium | The locator scan is at the private root only. Patch with BH-9. |

## Verification

- Run Platform's `mvn -B -ntp package` and `python3 -B -m unittest test_run_owned` with the built provider. Run the Projects launcher tests in `tests/tools/test_live_project_fixtures_launcher.py`.
- Run the ordinary phase with disposable resources under `run-with-temp`. Retain source and Platform revision identifiers, native persisted-effect results, a credential-free issuer and catalog receipt, and verified cleanup.

- Post-review verification passed on the complete patched source. The two-phase `--disabled-only` run passed 27 catalog guards and the ordinary native probe with zero skips. Its receipt at `/home/administrator/.local/state/hexalith-projects/dw-114-review-final/run.json` has SHA-256 `85dd2eeb45774ee6e4b87ea66ac1cbfb4e27875f14a0c1aa980c32de8c8d2187`, its source manifest has SHA-256 `25ab55c0c995c3088dff5f8d1b93d4b052eaf88cf071f9fe328d107aab65876f`, and its native XML has SHA-256 `3e9b10a5ba8013e42a7f44b0858ed46387add8a322c13c1d8bf5e696d4443cdd`. The post-review check passed all 23 Java provider tests, 52 Platform enrollment tests, 223 Projects launcher tests, and 12 mapped Debug AppHost model tests. The live run used the documented per-run Aspire container-tunnel opt-out. Owned resource cleanup and preservation of the full preexisting container IDs were verified.
