---
title: 'Complete the next actionable Projects deferred work items'
type: 'chore'
created: '2026-11-24'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '6b841c3a8ecea461f7791b3afffc224d04fcb13a'
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete ten independent items from the Projects deferred work list in its current priority order. Skip blocked or superseded items with reasons, verify each completion, mark it done, and commit it separately. Run the full Projects build and test suite before pushing to main.

## Boundaries & Constraints

Keep existing authority checks, package policy and compatibility guarantees. Implement an item in the repository that owns its affected behavior. Resolve a verification item only with evidence from its actual test path. Use the ledger's existing order among entries without explicit priority. Preserve the historical records and note the relevant current proof.

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/deferred-work.md` records numbered and unnumbered items. `spec-next-ten-deferred-work-items.md` records the previous batch and known blockers at the old baseline.
- `references/Hexalith.Builds/Tools/{G4PackageQualification.functions,test-g4-tool-package-contracts,test-g4-tool-package-artifact-validator}.ps1` implement and test the qualification validator. The producer-publication verification evidence includes an unapplied patch for hidden files. DW-75 also requires the current full qualification run.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.DomainService.Tests/EventStoreDomainServiceExtensionsTests.cs` tests diagnostics registration. Give synchronous and asynchronous projections distinct projection-only domains and verify the resolved diagnostics set.
- `references/Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/FoldersServerHostCompositionTests.cs` contains owner host/provider startup coverage. Use its actual loopback collector to test delivery of OTLP HTTP/protobuf through the current EventStore wrapper.
- Tenants readiness and command transport controls already exist in `references/Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Bootstrap/`. The packed consumer runner is `references/Hexalith.Tenants/scripts/verify-catalog-package-consumer.py`. Reuse the existing retained UI-flow disposal tests.
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` exercises actual native HTTP admission. Existing alias-conflict controls cover refusal. Exercise unambiguous `sub` acceptance if it is still outstanding.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/Middleware/CorrelationIdMiddlewareTests.cs` is the target for real header clearing at exception handling and response start.
- The Projects retained list lives in `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`. Retained Conversation and attachment resolution live in `src/Hexalith.Projects.Server/Queries/`. Reuse their HTTP test fixtures and the 200-row / existing shared deadline safety bounds.

## Tasks & Acceptance

- [x] Walk `deferred-work.md` by priority. Use its exact acceptance requirements and keep blocked or obsolete items out of the ten completed count.
- [x] Implement and verify each actionable item in the owning files above. Use the next viable entry after any blocker.
- [x] Given applicable regression suites, when they run, then the changed behavior is exercised with the cases described by each item and existing cases pass.
- [ ] Given ten verified entries, when the completion gates run, then each entry has an individual commit and a done disposition, the full Projects build and suite pass, and main receives the verified commits.

## Implementation Notes

The completion target of ten follows the request's repeated total and final-report requirement.

Focused verification passed in ten candidates so far. EventStore projection diagnostics passed 105 cases and its correlation middleware passed seven. The Folders owner host sent all three emitted signals to its loopback OTLP collector. Tenants ran 365 bootstrap cases, including the exact transient and body-bound controls, 15 SDK consumer script tests plus a published package consumer, and 30 retained-flow cases. Conversations passed 41 HTTP cases. Projects passed 94 focused retained-route and list cases after review. Builds passed six focused readiness cases and all three hidden-file controls. Review found that the capture fixture discards the command outcome. The full run did not yield a qualification receipt before the candidate was withdrawn.

The first full .NET pass used local pre-existing owner Release outputs. Two metadata tests refused those hashes. The readiness test also exposed its short wall-clock race, so the test now triggers an explicit deadline on its second poll. The managed browser run found an in-flight Tenants compile error, since corrected. The package owner artifacts rebuilt successfully. All seven readiness controls passed with an explicitly triggered deadline and a separate caller-cancellation check. The browser retry brought up the AppHost and authenticated authority but failed the active folderless Project smoke case.

## Skipped entries

| Entry | Reason |
| --- | --- |
| DW-34 | The accepted historical baseline is EventStore 3.110.0 with Builds 4.29.1. The proposed 3.90.0 propagation was superseded. |
| DW-36 | The installed cleanup scripts already quarantine the batch and roll back failed staging. The recovery limit belongs to the separately tracked durable-deletion item. |
| DW-48 | The setup projection needs current source-head enrollment and rebuild provenance tied to its actual row. The SDK metadata reader supplies a stream head and requires owner enrollment. |
| DW-54 | The later cutover agreement preserves v1 REST and generated signatures. An additive normalized successor requires a contract-version decision. |
| DW-63 | The approved sequence places the whole-host migration with Story 7.1 and its replay, fencing and rollback proof. Story 7.1 is blocked. |
| DW-64 | Both concurrent commits are visible in published history. The ledger entry describes a past event with no remaining defect. |
| Durable legacy cleanup | Recovery after physical deletion needs a retained backup and an approved deletion and recovery policy. |
| DW-72 | The Test Architect owns the command-evidence retention threshold. |
| DW-74 | The pinned Builds graph removed the bundled npm publisher chain. The previous audit recorded zero advisories. |
| DW-75 | The readiness and hidden-fixture controls passed individually. The command fixture still discards the outcome, so those checks do not establish the intended qualification. The full attempt was stopped and the unqualified patch was kept with the local test logs. |
| DW-77 | The Platform pin lacks an exact release tag and resolves to a different committed Builds pin. |
| DW-78 | The existing Folders owner read requires a native Folder/file pair. A standalone File identifier has no authoritative binding to that pair. |
| DW-79 | The Projects lock already uses undici 6.29.1. The preceding batch verified the three tracked advisories were absent. |
| DW-80 and the actual-owner advisory probe | Browser qualification stopped before the unchanged positive Folder probes. The mapper returned HXW004, HXW011, HXW009 and HXW010. The Platform release tag and Builds tuple also block DW-77. |
| DW-82 | Published Projects Contracts and the McpCli enrollment are present. McpCli Stage 2 remains red in [main CI](https://github.com/Hexalith/Hexalith.McpCli/actions/runs/45387738457/job/138422995157). |
| DW-85 and the related context admission entry | Supported Refresh and the Reference Trust Index require the G-2 owner batches and an approved index schema. |
| Historical G-4 live lane, cleanup and status diagnostics | The old Dapr cohort is superseded. The Builds owner already implemented cleanup and diagnostics. |
| G-6 pin wording, old OQ8 selector and 14-package replay | The Builds wording was corrected. EventStore retired the old controller and verifier. |
| EventStore 3.108.1/3.109.0 images, containment, log archive and smoke | The newer accepted tuple superseded these attempts. The old immutable image inputs and expired GitHub evidence are unavailable. |
| Duplicate Builds xUnit consumer | The owner made the package and consumer identities explicit. The old rows represent distinct packages. |
| P3 production identity and no-build receipt | The owner deployment graph is absent, and the exact historic command output and binary images were never retained. |
| Folders metadata v2 package parity and old Platform preview pair | The published Folders client covers v2. The qualified G-6 tuple has superseded the EventStore.Aspire 3.110.0 preview pair. |
| G-6 EOL reproducibility | Current Builds qualification binds the executed bytes and Projects disables reuse. A change to cross-checkout canonicalization needs the Builds protocol owner. |
| PostgreSQL backup command provenance | The historical receipt links one command to one artifact. Full historical linkage needs a compatible owner receipt format and exact source transfer evidence. |
| Story 6.1 callbacks, Development catalog and source graph | The accepted gateway and owner proof supersedes the old callback and mapping complaints. |
| Story 6.4 snapshot, latency, cold inspection and mutation dispatch | The advisory reset superseded the snapshot condition. Story 8.9 owns scale proof. The cold-inspection and mutation items require owner protocol work. |
| EventStore authoring path and Admin source edges | The pinned source includes sibling Tenants resolution and producer mapping guards. |
| Managed browser coverage and axe/Playwright qualification | The smoke suite completed seven of eight cases. Its live fixture seeds an active Project without a Folder, which the retained Open path quarantines. The legacy browser matrix also needs startup evidence for the exact rollback revision. |
| Direct advisory 404 and production owner activation | Production requires verified gateway identity. Deployment and a distinct direct-call policy both need owner artifacts or approval. |
| Tenants packed consumer ranges and package closure | The owner verifier already rejects exclusive and incorrect resolved cohorts. Fourteen tests passed before the SDK-pin change. |
| Tenants standalone invocation budget | Owner commit ca93fe19847d3535353c8b4850497350aaba47d3 already qualified the 30-second production transport policy. |
| Intermittent proposal confirmation | The cited failure did not reproduce in the full Server runs. Its original safe-404 guard is still unidentified. |
| Shared gateway body, ambiguous actor commits and no-catchup | These require owner transport or recovery protocol decisions, or a supported reproduction of a noncooperative source. |
| McpCli signer, live list and service topology | Native deployment and ownership proof remain with McpCli's failing main integration. |
| Released cutover fingerprints and per-cell attestation | The exact release binaries and an approved evidence identity format are pending. |
| Folders generated v1 client and Projects CLI/MCP retirement | The Folders owner needs to ship its supported generated transport. Retirement remains bound to the McpCli migration acceptance. |
| Advisory limit equivalence | The approved comparison rules lack a decision for materially different coverage above the 200-Project cap. |

## Spec Change Log

## Review Triage Log

- Medium, patched. Explicitly linked Conversation Projects enter bounded discovery independently of the list projection. HTTP controls cover missing discovery plus authorized detail, and both sides of the 200-item cap.
- False for the specified coverage contract. A retryable exact authority result cannot establish that the current name fails to match. Returning incomplete evidence preserves the exact-read boundary.
- Medium, deferred consistency follow-up. A rename during the request can still change a nonmatch or a scored result. The current fix addresses agreement between discovery and the authorized detail observed at each pass. A distributed rename or authority fence needs consistent owner revisions and a latency budget.
- Medium, patched and bounded. Attachment resolution rereads only disclosure candidates after the final authorization sequence and refuses changed inputs under the original deadline. A deliberately noncooperative last read verifies the route can still finish. Cross-store atomicity and revocation during the final read remain outside this observation window.
- Medium, pending qualification for DW-75. The three inventory enumerators needed an independent assertion about the known hidden fixture. The fixture-only readiness test also needs to distinguish plan capture from readiness failure. DW-75 remains open pending the assertion and a complete qualification.
- Medium, patched. The collector requires a marker carried by each real guarded named OTLP client, proving the traffic traverses the selected pipeline.
- Medium, patched. The command SocketsHttpHandler disables response draining and retains the credential and general defaults. Exact factory assertions passed in both registration orders.
- Medium, patched. Create Tenant attempts both callbacks before detaching and preserves the first failure. The combined active callback regression passed.
- Medium, patched. The readiness deadline test refuses an unexpected third poll. Its companion verifies caller cancellation while the deadline remains active.
- Medium, patched. Conversations `sub` admission succeeds without a caller header. Header-only input cannot reach an owner or projection read.

## Verification

The ten entries have distinct local implementation commits in their owning repositories and done entries in Projects. The Projects ledger records 15 numbered and 49 legacy entries still open. One historical entry remains locally resolved with a production blocker.

- `CI=true dotnet build Hexalith.Projects.CI.slnx --configuration Release --force -m:1 --disable-build-servers --nologo -warnaserror` passed with zero warnings and errors.
- The full native .NET run passed seven assemblies. The Server suite exposed wall-clock sensitivity in the 200,000-entry advisory ordering fixture. The fixture now controls its timer, and the request still uses one two-second deadline with separate active timing tests. The affected Release rebuild and full Server rerun passed all 1,560 cases. Across the full run and final Server rerun, all 3,211 cases passed with zero failures or skips.
- `python3 -m unittest discover -s tests/tools -p 'test_*.py'` passed all 493 checks. The three separate BMAD tooling suites passed 88 additional checks. The browser framework passed seven self-checks and the TypeScript check passed.
- `npm --prefix tests/e2e run test:live:managed` passed seven of eight authenticated smoke cases with zero skips. It stopped at the fixture's active Project convergence check. The owned AppHost and sidecars were removed. Release publication remains pending while the managed suite is blocked.
- `python3 tools/qualification/run_supported_project_reads.py --browser --timeout 600 --evidence <private-result-directory>` stopped before any Folder latency probe. The source-preparation command returned `HXW004`, `HXW011`, `HXW009` and `HXW010`.

The Projects ledger commits and the owner implementation commits remain local. Owner publication and Projects gitlink recording are pending the managed browser gate.
