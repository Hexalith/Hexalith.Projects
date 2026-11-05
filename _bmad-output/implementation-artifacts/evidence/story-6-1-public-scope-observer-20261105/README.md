# Story 6.1 public scopes and observer completion — 2026-11-05

Story 6.1 and its approved spec remain **in-progress**. The authorized public-credential/status alignment and observer race repair are implemented and locally verified. Fresh native bootstrap now passes original-receipt validation and explicit platform-scoped durable-status reading, then fails closed at the existing SDK's documented missing platform-workload source-read capability. The [owner handoff](owner-handoff.md) records the required shared repair. No sibling source, dependency pin, production enrollment, legacy routing or Git state was changed.

## Changes

- The exact `actor-1` / `supported-read-bootstrap` password credential grants only `global-administrators:bootstrap`, with no Tenant/storage grants. Its RS256 signature and unchanged credential rejection remain verified.
- Fixture Administrator credentials use canonical public Tenant grants and finite `tenants:create`, `tenants:add-user`, `tenants:read` and `global-administrators:read` permissions. Tenants seed commands/stream requests use owner-declared `platform:tenants`; public platform requests carry explicit scope, and command status carries exactly one Tenant or platform header. Historical envelopes, event identities and persisted `system` assertions are retained. Twelve new controls cover finite grants and invalid/reserved/mixed scopes.
- The observer samples independently and enrolls root completion only after the actual gateway provider initializes. A bounded blocked-export control cannot finish early; a suppressed root completes with an empty export set. The existing unbound-span control now installs the provider/gate and proves the nested export was actually observed. No privacy or exported-span assertion was relaxed.
- Bootstrap source proof stops with an explicit unsupported-capability failure. It issues no invalid public `system` read, custom transport, response remapping or authorization bypass. Source/causation/correlation assertions remain in place for the future owner-supported path.

## Actual verification before review corrections

| Lane | Result |
| --- | --- |
| Signed issuer / all launcher controls | 1/1 and 119/119 passed |
| Mapped Debug Integration and final Server builds | 0 warnings / 0 errors |
| Local Integration, live distributed class excluded | 208/208 passed, including all 95 evidence controls |
| Final focused gateway controls | 8/8 passed, including both new completion controls |
| Final local Server, existing owned-Dapr selectors excluded | 1,330/1,330 passed |
| Fresh duplicate Integration build | 0 warnings / 0 errors |
| Fresh native duplicate | 0 passed / 1 failed / 0 errors, skipped or not-run |

Native results are [local-integration.xml](local-integration.xml), [observer-final.xml](observer-final.xml), [local-server-final.xml](local-server-final.xml) and [duplicate/distributed.xml](duplicate/distributed.xml). [commands.json](commands.json) records exact commands and outcomes; source-mapping environment is retained in [prepare-source.json](prepare-source.json). The initial [build-server output](retained-output/build-server.txt) xUnit1051 analyzer failure is preserved separately from the corrected passing builds.

The native invocation was:

```bash
MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --scenario duplicate --timeout 900 --evidence _bmad-output/implementation-artifacts/evidence/story-6-1-public-scope-observer-20261105/duplicate
```

[bootstrap-command-status.json](duplicate/bootstrap-command-status.json) records `hostedReceiptVerified=true`, actual status `Completed`, HTTP `200`, and explicit scope `platform:global-administrators`. It explicitly records `persistedGlobalAdministratorSetVerified=false`: receipt/status success is not persisted source proof. The native failure is `Owner capability required: the EventStore bound-workload SDK does not support platform:global-administrators source reads.` The matrix was not completed; no later provider or full runtime regression ran after this known owner blocker. No Project seeding or supported Project reads were reached.

The exact prior package-mode command/output remains retained in [the bootstrap-authority packet](../story-6-1-bootstrap-authority-20261105/README.md) and its [byte-identical retained package output](retained-output/prior-package-restore-nu1109.txt): `CI=true MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Release -m:1 -p:NuGetAudit=false` fails restore with three NU1109 errors, because Tenants.Client 7.0.0 requires EventStore Client/Contracts >=4.0.4 while Projects selects 3.133.0. It was not rerun or overridden in this continuation; local Debug passing does not certify that package build.

## Source and cleanup

Projects HEAD is `a6c415f38053cbdb257b12d196be0bfa70aadc2d` plus preserved and current uncommitted changes; [revisions.json](revisions.json) contains every full tested direct dependency commit. The fresh native manifest is **41df764d7b1a222455c81f74258f5b819021a87604fd0fdb5928f8aa30c723e0**, with 17,029 inputs. Source matched it exactly at native cleanup. Native test assembly SHA256 is `7eb121c25135414d7bc80f16d49794d59893b0bde1088276b7ae84a4662e7668`.

After native cleanup, one Server-test-only change made the existing unbound-span control exercise the newly installed completion gate. That source was rebuilt and passed 8/8 focused and 1,330/1,330 local Server tests. The pre-review local manifest is **a56fe2d12ab5c1077217ebb02e1f67a9c879e3e73fce6e06098dd622b609099b**; that checkpoint’s Server assembly SHA256 is `42a0123084b60fb8cbb9602ec5c02c2483f2ead2abdbf25a14217131ee97fff9`. [final-source-comparison.json](final-source-comparison.json) records the sole changed path and both hashes. At that checkpoint Integration/runtime source was unchanged and source was **not** claimed equal to the native manifest. Later review corrections are recorded separately below. The native scenario was not repeated for this test-only correction. [local-tested-assemblies.json](local-tested-assemblies.json) preserves the earlier local assembly bindings.

[Native cleanup verification](native-cleanup-verification.json) confirms exact owned AppHost removal, all four captured containers absent, six owned sidecars absent, private material removed, no remaining Aspire session and preservation of all five preexisting containers. The original [run.json](duplicate/run.json) remains untouched, including its failed result, actual commands and cleanup.

## Acceptance against all seven criteria

| Criterion | Current disposition |
| --- | --- |
| AC1 supported authenticated list/open | Local controls pass; fresh persisted Project path not reached. |
| AC2 safe denial/privacy/timing | Observer race and local denial controls pass; fresh whole-chain Project proof not reached. |
| AC3 honest response state | Local controls pass; fresh persisted authenticated proof not reached. |
| AC4 additive compatibility/retained routing | Current local legacy comparator passes; routing unchanged. Full cutover equivalence remains 6.7. |
| AC5 response completeness | Local controls pass; fresh authenticated response proof not reached. |
| AC6 read-only/persistence | Existing local persistence/no-write controls pass; fresh distributed no-write/replay/restart proof not reached. |
| AC7 authenticated persisted verification | Incomplete: native duplicate fails at the documented owner SDK capability; remaining matrix/regressions unrun. |

Story/sprint acceptance stays in progress. Public seed/status/source alignment cannot be fully accepted until the owner SDK capability exists and the strict current distributed lane passes. Parent workflow review remains pending this implementation handoff. Historical receipts retain their original source limits.


## Checkout-visible raw output retention

The ignored original logs are unchanged. [retained-output-map.json](retained-output-map.json) maps every original log to its checkout-visible, byte-identical `.txt` copy with SHA256 and byte count, including issuer, launcher, referenced build/test outputs, native duplicate outputs and the prior NU1109 package output. [commands.json](commands.json) points its current output references to those retained copies. The tightly scoped `.gitattributes` rule for this packet’s `retained-output/` directory preserves raw bytes and native whitespace; `.gitignore` and staging are unchanged.

## Later review corrections

The early bootstrap progress receipt now records only observed receipt/status progress and `persistedGlobalAdministratorSetVerified=false`; it no longer sets `sourceProofBlockedByOwnerSdk` before projection readiness can succeed. The explicit SDK exception remains at its existing later source-read boundary. The original native receipt, logs, XML, manifest and `run.json` remain immutable. Its old causal flag is historical schema, not evidence that projection waiting cannot fail.

The Folder replacement regression now persists creation/binding/link as a prefix, checks its stored sequence, then delivers replacement separately. Its Complete enumeration, Partial open, unchanged post-read bytes/ETag and full-history rebuild-readiness checks remain. The added ordinary HTTP export control starts the actual gateway provider, registers an exact-trace raw server-stop signal after that provider, sends a non-query request, awaits the bounded stop signal and verifies the exporter observed that exact server span. It uses no Dapr, sleep or flush assumption.

[Focused correction commands/results and later source differences](review-corrections/README.md) record zero-warning/error builds, 8/8 selected Server controls and 2/2 Integration receipt/event controls; no broader run was made. The earlier full-suite totals above remain bound to their original source. Full post-correction verification belongs to the parent run; these corrections do not accept AC7 or change the owner SDK/package blockers.


## Parent review and final verification — 2026-11-05

All three configured review layers ran. Edge Case Hunter and Verification Gap reported no findings. The Blind Hunter's ten findings were individually triaged in the spec: four direct corrections are complete, five low-impact suggestions and one false finding were rejected with evidence, and none was deferred. [Review corrections](review-corrections/README.md) preserve every original native record.

The final parent run passes **208/208 Integration** and **1,331/1,331 Server** tests, with zero failures/errors/skips/not-run. Required live selectors retain the documented exclusions. The new positive actual HTTP export control and two-delivery Folder regression ran in the Server suite. [Exact parent commands, totals, assembly hashes and source binding](parent-verification/results.json), [Integration XML](parent-verification/integration.xml), [Server XML](parent-verification/server.xml), and retained raw [Integration](retained-output/parent/integration.txt) / [Server](retained-output/parent/server.txt) outputs cover the post-review source. Source manifest `0454152151df5bf58fa74343bc59a0d660bb4c4df56dc96cdc25ddf3ad82943d` remained unchanged during both runs. The earlier tables and native `41df...` evidence retain their earlier source coordinates; no fresh native acceptance is inferred from these later checks.

All 17 checkout-visible copied command outputs match their original bytes, digests and sizes, and none is ignored; [the parent check](parent-verification/retained-output-validation.json) records each result. Only the exact retained-output directory has the immutable-output whitespace exception. The [owned mapping workspace was removed](parent-verification/mapping-cleanup.json) after parent verification. Native resource/private-material cleanup remains verified, with unrelated resources preserved.

The early status receipt now omits premature SDK-failure attribution. The original native receipt stays immutable; that run's actual exception independently establishes the observed SDK boundary. Story and spec remain **in-progress** because the owner-dependent source-proof task and current AC7 are incomplete. The package-mode NU1109 result remains unchanged. No completion/release gate was waived, and no staging, commit or push occurred.
