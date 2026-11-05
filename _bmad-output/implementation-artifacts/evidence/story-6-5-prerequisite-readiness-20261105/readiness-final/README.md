# Story 6.5 readiness completed — 2026-11-06

**Ready for a separate implementation request in this verified source checkout.** The authorized synthetic-fixture migration is applied, the mapped Projects UI test project builds, all 12 authentication composition cases pass, and the spec now uses delivered authentication, SDK transport and independent response semantics. No missing prerequisite was established. **DW-1 remains done**, scoped to the retained uncommitted FrontComposer repair.

The [refreshed spec](../../../spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md) is a complete draft for that separate request; approval/implementation was not requested here, so it was not advanced to an approved implementation state. Sprint status remains byte-identical. Readiness passes do not accept Story 6.5's future browser-to-handler, circuit-isolation, persisted-domain-state or accessibility journeys.

## Applied change and preserved inputs

The only source/test change is exactly [the retained proposal](../repair/projects-test-fixture-proposal.patch) in `tests/Hexalith.Projects.UI.Tests/Authentication/ProjectsUiAuthenticationCompositionTests.cs`: seed `FrontComposerTokenSession("projects-ui-test-session", "user-1", "tenant-a")` and provide the matching authenticated local-session/current-Tenant claims. Existing assertions and production Projects code are unchanged. The reverse-apply check passes. Synthetic values remain synthetic; credential capture, storage, binding and relay remain FrontComposer-owned.

Projects HEAD is `b9a890e38044d23affe6b5d403d4b2a69b6edf34` plus the recorded uncommitted fixture/documentation/evidence changes. FrontComposer base is `4c24c287cde520631e960a322413bee804f7c3d4` plus the unchanged [owner repair patch](../repair/owner-source.patch), SHA-256 `cfca262eb860a4cb99efe6d5e76da401cc0ab58f88eb001d262e1eb9724d4d62`. All 17 owner source/test file hashes and both prior owner test assemblies still match [the verified owner manifest](../repair/source-inputs.json); the prior **114/114** owner result remains retained evidence, not a new run. No repaired owner commit or updated gitlink is claimed. A checkout containing only the unchanged owner gitlink is not qualified by this result.

[source-inputs.json](source-inputs.json) records full canonical revisions/status of every mapped repository, 261 source/configuration fingerprints, 21 assembly fingerprints, emitted mapping values and mapping-file hashes. [verify-inputs.py](verify-inputs.py) checks owner repair preservation and detects input drift. The retained consumer assembly's FrontComposer Shell fingerprint equals the previously tested repaired owner Shell fingerprint.

[preservation-before.json](preservation-before.json) fingerprints the preexisting repair packet, readiness documents and sprint status before this work. All original native receipts, patches and source manifests remain unchanged; README updates explicitly supersede historical readiness assessments. Exact earlier versions remain in [spec-before-refresh.md](spec-before-refresh.md), [repair-readme-before-refresh.md](repair-readme-before-refresh.md) and [prerequisite-readme-before-refresh.md](prerequisite-readme-before-refresh.md).

## Exact execution

[commands.json](commands.json) records actual argv arrays, working directory, UTC start/end times, native exit codes and separate native stdout/stderr filenames. [run-command.py](run-command.py) retains output bytes without rewriting or merging streams. All commands below ran from `/home/administrator/projects/hexalith/projects`. Local date is November 6 in Europe/Paris; the native XML correctly records November 5 at 23:00 UTC.

The established mapping was reused unchanged from [the successful Projects preparation](../repair/prepare-projects-source-mapping.txt), not the earlier rejected Conversations mapping. Active module is `Hexalith.Projects`, mode `source`, mapping hash `18d3e8ef0de4e65bc49da4edec30d74464ede01f49c969156fe277ce0babd5aa`. The emitted files are retained in [source-mapping/mapping.json](source-mapping/mapping.json) and adjacent imports. Existing HXW010/HXW011 preparation diagnostics remain recorded; they did not block this local source lane and do not qualify publication/package mode. No preparation, dependency update or gate relaxation was needed.

```bash
git apply --check _bmad-output/implementation-artifacts/evidence/story-6-5-prerequisite-readiness-20261105/repair/projects-test-fixture-proposal.patch
git apply _bmad-output/implementation-artifacts/evidence/story-6-5-prerequisite-readiness-20261105/repair/projects-test-fixture-proposal.patch

env \
  AfterMicrosoftNETSdkTargets=/tmp/story65-projects-credential-repair-source-mapping/source-mapping/SourceMapping.final.targets \
  Configuration=Debug \
  CustomAfterDirectoryBuildTargets=/tmp/story65-projects-credential-repair-source-mapping/source-mapping/SourceMapping.late.props \
  CustomAfterMicrosoftCommonProps=/tmp/story65-projects-credential-repair-source-mapping/source-mapping/SourceMapping.props \
  CustomBeforeDirectoryBuildProps=/tmp/story65-projects-credential-repair-source-mapping/source-mapping/SourceMapping.targets \
  CustomBeforeDirectoryBuildTargets=/tmp/story65-projects-credential-repair-source-mapping/source-mapping/SourceMapping.before-targets.props \
  DOTNET_CLI_USE_MSBUILD_SERVER=0 \
  HEXALITH_SOURCE_MAPPING_HASH=18d3e8ef0de4e65bc49da4edec30d74464ede01f49c969156fe277ce0babd5aa \
  MSBUILDDISABLENODEREUSE=1 \
  dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers

dotnet tests/Hexalith.Projects.UI.Tests/bin/Debug/net10.0/Hexalith.Projects.UI.Tests.dll \
  -class '*ProjectsUiAuthenticationCompositionTests' \
  -xml /home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/story-6-5-prerequisite-readiness-20261105/readiness-final/projects-auth-composition.xml
```

| Check | Actual native result |
| --- | --- |
| Proposal check, apply, reverse-check | Each exit 0; the retained proposal is unchanged. |
| [Mapped Debug/source build](projects-ui-tests-build.stdout) | Exit 0; zero warnings, zero errors. All restore inputs already up to date. |
| [Built-assembly focused execution](projects-auth-composition.stdout) / [native XML](projects-auth-composition.xml) | Exit 0; **12 passed, 0 failed, 0 errors, 0 skipped, 0 not-run**. xUnit v3 runner 4.0.2, .NET 10.0.14, SDK 10.0.403. The runner's `-xml` deprecation notice is retained verbatim. |
| Source preservation | All 17 owner files, repair patch and prior tested owner assemblies unchanged; correct Projects mapping retained. |
| Authority/index and final consistency | Exact commands/results are recorded in commands.json and the final validation receipt. Authority is scheduling validation, not feature acceptance. |

The 12 cases cover existing configured confidential OIDC/cookie/server state, real HTTP relay composition with the updated synthetic binding, anonymous non-relay, absent local configuration, partial/invalid configuration failures and existing Program/Routes composition. They do not newly prove browser login, concurrent circuits, supported SDK adapter behavior, real owner authorization or persisted no-domain-write behavior. Those remain Story 6.5 implementation acceptance; prior owner isolation coverage remains independently available.

## Spec readiness disposition

[contract-map.md](contract-map.md) records the delivered API and concrete future edit/test map. The refreshed spec:

- Uses actual `Diagnostics/` sources and existing OIDC/authentication/challenge composition; plans the remaining SDK registration and gateway wiring.
- Preserves independent list enumeration, row, open, context and Conversation-start snapshots, including their distinct CLR state/component shapes and existing unavailable/owner-trust limits.
- Uses existing `IEventStoreGatewayClient.SubmitQueryAsync<T>` and delivered Contracts/Queries, exact targets/discriminators and failure wrappers; introduces no substitute transport or composite contract.
- Keeps advisory evaluation time distinct from source freshness, incompleteness distinct from lag, unsupported File coverage explicit and all candidates unselected. No candidate name/component/context authority is invented.
- Leaves authenticated browser-to-handler, two-circuit Tenant isolation, safe absence, independent admission, required metadata inspection audit, persisted zero-domain-write and WCAG acceptance as future feature work.

No unresolved product question or missing prerequisite was found. The prior consumer fixture compilation failure is resolved. Feature tasks remain unchecked, spec approval/dispatch belongs to a separate request, and the existing blocked sprint entry is intentionally untouched. No Story 6.5 runtime/feature-test implementation, sibling source edit, new AppHost/browser run, domain operation, commit, push, branch switch or dependency update occurred.
