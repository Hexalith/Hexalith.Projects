---
title: 'Complete five deferred Conversation-start safety items'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: 'aa676462d450e7b3861d5b124544516b2940e9d5'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred supported-read safety obligations lack closure: dual-principal Conversation-start authorization (DW-47), persisted zero-write checks (DW-51), structural equality and collection ownership of the supported setup response (DW-56), distinct Folder recovery outcomes (DW-67), and the P3 concern that supplementary identities can contribute unauthenticated authority claims.

**Approach:** Complete this bounded root-owned safety slice on main and push the verified commit as requested. Retain the existing legacy setup DTO and supported response constructor, wire fields, and snapshot compatibility. Give the supported response wrapper defensive collection ownership and explicit structural equality/hashing, including its snapshot. Limit HTTP-derived authority to the primary authenticated identity consistently with the existing claims transformer; supplementary identities must never supply tenant, actor, permission or delegated-envelope claims. Add unconditional Conversation-start regressions for exact actor/workload/delegation/scope/audience binding, Folder-state-specific recovery without protected identifiers, and persisted bytes/ETags/key counts before and after successful, unavailable and denied reads. Record exactly these five ledger resolutions. Preserve legacy routes, DW-48 freshness markers, DW-66 cutover semantics, dependency pins, story scheduling and qualification/acceptance decisions.

</frozen-after-approval>

## Implementation Notes

- No unresolved user intent or irreversible data action; the user authorized five deferred items and a main push. This small local safety change adds no public API. The supported response itself is the dedicated value boundary: retaining its legacy CLR Setup property and constructor avoids an overload ambiguity or client break while giving the wrapper structural value semantics.
- Code map: ConversationStartSetupResponse.cs owns defensive copies and structural equality/hash; legacy ConversationStartSetup.cs remains unchanged. HttpContextProjectTenantContextAccessor.cs and ProjectQueryEnvelopePrincipalBinding.cs own primary-identity isolation. GetConversationStartSetupQueryHandlerTests.cs exercises the actual supported executor; SDK InMemoryReadModelStore provides physical bytes, ETags and key count. AdmissionSnapshotTests.cs preserves the original constructor and wire contract.
- Investigation confirms supported dual-principal admission and cause-specific Folder recovery already exist. New regressions close their verification gaps. Full lag/rebuild freshness proof requires missing platform evidence and remains DW-48. Current Story 6.2 intentionally maps shared Partial to Complete pending cutover; preserve it.
- Required baseline and Git instructions, root EditorConfig/build/test configuration, project context, relevant AD-20/AD-32 and existing supported-query specs were inspected. Exact Projects AppHost start/describe/stop succeeded; baseline is /tmp/five-setup-aspire-baseline.json. Resource state is startup evidence only, not live qualification. Unrelated Tenants AppHost was untouched.

- Implementation: retained the existing response constructor/property types; the response wrapper is the supported structural value boundary, including read-only snapshot copies. Primary-identity isolation now covers both the HTTP accessor and callback envelope derivation, consistent with the claims transformer. Added 49 unconditional cases: 19 Contracts, 23 Conversation-start handler, five accessor and two binding cases.
- Baseline resource inspection observed 11 Healthy Running resources and one Finished resource. The exact AppHost was stopped before builds; no production qualification is inferred.
- Initial verification: Debug Server build and Debug Contracts build succeeded with zero warnings/errors. Full Debug Contracts: 221 passed, zero failures/skips. Full Debug Server: 1213 total, 1211 passed, zero failures, two existing opt-in live skips. Release/package Server build succeeded with zero warnings/errors.

## Verification

- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — zero warnings/errors.
- `dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — zero warnings/errors.
- Both project builds with `--configuration Release -p:UseHexalithProjectReferences=false` and the same remaining options — zero warnings/errors.
- `dotnet tests/Hexalith.Projects.Contracts.Tests/bin/Debug/net10.0/Hexalith.Projects.Contracts.Tests.dll -noColor` — 221/221 passed, zero failures/skips. The equivalent Release invocation also passed 221/221.
- `dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -noColor` — 1213 total, 1211 passed, zero failures, two existing live-infrastructure skips. Equivalent Release invocation — 1212 total, 1211 passed, zero failures, one existing live-infrastructure skip. The extra Debug-only skipped test is compiled only under HEXALITH_SOURCE_FIXTURE; the persisted-restart test skips in both modes without isolated Dapr endpoints. All 49 new cases run unconditionally.
- `python3 tools/planning/validate_production_authority.py --validate-index` — passed; authority remains [6, 7, 8].
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1` — passed across all five workflow files.
- `git diff --check` — passed. Existing line-ending conventions preserved. Main and origin/main have zero divergence after fetch.

- Additional own-check correction: effective callback HttpContext now projects the primary identity too, preserving its roles while excluding supplementary roles. This keeps later role checks consistent with the narrowed accessor/envelope binding; both authenticated and unauthenticated supplementary-identity controls verify it.

- Commit message validated with the installed commitlint and repository configuration. Main push is explicitly authorized by the user.

## Review Triage Log

- BH1 — low, patched: supplemental-only callback authority lacked controls. Four omission cases now reject borrowed workload/delegation/scope/audience and retain the platform helper's permitted omission semantics.
- BH2 — medium verification gap, patched: two exact-action cases remove primary projects:read permission, with/without an injected supplemental grant. Both deny without protected reads using the existing guarded-store fixture.
- BH3 — medium verification gap, patched: a controlled validator allows the first authorization and revokes the second, asserts literal projects:read and exact Tenant/actor/Project, and verifies safe denial plus unchanged persisted bytes/ETag/count.
- BH4 — medium verification gap, patched: zero-write cases now also compare the actual Tenant-access projection's serialized state and Version before/after both reads.
- BH5 — low, patched: protected Folder display-name and reason-code canaries are absent in every Folder failure case.
- BH6 — medium verification gap, patched: every Folder failure asserts Included=false, Freshness=Unavailable and the existing bounded missing reason as well as cause-specific recovery.
- BH7 — low, patched: populated and null-Setup golden strings pin the complete serialized response, names and enum casing. Both pass with the retained published wire shape.
- BH8 — low, patched: component Name/Freshness/Reason and component/recovery/preferred/excluded ordering now have independent unequal-value controls.
- The independent reviewer rechecked all eight corrections and reported no unresolved findings. No new finding is deferred. The review identified verification improvements, not a confirmed runtime defect after the effective-role correction.

## Final verification after review

- Debug Contracts: 230 total, 230 passed, zero failures, 0 existing live-infrastructure skips; `/tmp/five-setup-final-contracts-debug-tests.log`.
- Release Contracts: 230 total, 230 passed, zero failures, 0 existing live-infrastructure skips; `/tmp/five-setup-final-contracts-release-tests.log`.
- Debug Server: 1220 total, 1218 passed, zero failures, 2 existing live-infrastructure skips; `/tmp/five-setup-final-server-debug-tests.log`.
- Release Server: 1219 total, 1218 passed, zero failures, 1 existing live-infrastructure skips; `/tmp/five-setup-final-server-release-tests.log`.
- All four final individual-project builds passed with zero warnings/errors. Commands and flags are the same as the Verification section above. This final run supersedes earlier counts: 65 new unconditional regressions, 28 in Contracts and 37 in Server; 1,448 passes per build mode.
- The independent reviewer confirmed every correction, with no unresolved findings or new deferrals. The commitlint check, whitespace check, workflow gate and production-authority index check passed. A byte reconstruction check proves exactly five ledger closures and no other historical-byte changes. Fetch confirmed main/origin/main divergence 0/0 before commit.
