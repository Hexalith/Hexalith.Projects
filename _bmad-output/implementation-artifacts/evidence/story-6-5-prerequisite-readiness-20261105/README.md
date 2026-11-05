# Story 6.5 prerequisite readiness — 2026-11-05

**Current result (2026-11-06): ready for a separate Story 6.5 implementation request in the verified source checkout. DW-1 remains closed for the unchanged owner repair.** The authorized synthetic-fixture migration builds and passes 12/12 authentication composition cases; the spec is refreshed against delivered contracts. [Final readiness, exact commands, native XML and source references](readiness-final/README.md). Spec approval/dispatch and sprint status are unchanged; no Story 6.5 implementation was performed.

**Initial result (historical, before the repair below): blocked by FrontComposer session credential isolation.** DW-1 remained open. Story 6.1 was accepted; its absence was no longer the blocker. The following original reproduction and limitations are preserved.

## Source and accepted inputs (initial assessment)

Projects is clean at `fb538a23201b419d1329d06faf2cd2b290ff8991` before this documentation/evidence update. FrontComposer is clean at `4c24c287cde520631e960a322413bee804f7c3d4`. Its locally known `origin/main` contains four further commits; none changes the authentication sources under investigation. No fetch, dependency update, branch change, staging, commit or push was performed. SDK: `10.0.403`. Exact linked source hashes are in [source-inputs.json](source-inputs.json).

| Prerequisite | Existing acceptance / usable source |
| --- | --- |
| 6.1 list/open | Accepted in [the story](../../6-1-list-and-open-projects-through-supported-authenticated-paths.md), final supported-read acceptance and second-pass reconciliation. Implementation commit `39aa17078be156aa06cd4f9cd0a5266545d59711`; dependency reconciliation `19a0548d0e84584092d9ca50693cedfa8805cb5c`; current correction `fb538a23201b419d1329d06faf2cd2b290ff8991`. [Final parent evidence](../story-6-1-platform-maintenance-20261105/parent-review/README.md): 11 distributed cases across three fresh providers; full Integration 222/222 and Server 1,378/1,378. [Second-pass evidence](../story-6-1-review-pass2-20261105/README.md): focused local Server 1,335/1,335 and Integration 222/222; no distributed rerun claimed for that correction. |
| 6.2 Conversation-start | Done [spec](../../spec-6-2-retrieve-conversation-start-setup-with-admission-truth-2.md); feature commit `d3a115df3c7243204ce9fe76757d39937761477f`. Its post-review verification records Server 607/607, domain 657/657 and Contracts 188/188. These historical receipts are not current reruns or new persisted acceptance. |
| 6.3 context | Done [spec](../../spec-6-3-retrieve-assembled-project-context-through-supported-read-models.md); review completion `97c4bd9227cb93af916d83566f8a29e39210809b`. Keep its accepted honest Unavailable/owner-trust limitations and unregistered Refresh behavior. |
| 6.4 advisory resolution | Done [spec closure](../../spec-6-4-resolve-projects-with-transient-current-explanations.md#closure--2026-11-01) at `353cfd33e54270a1acb028c1c66ecb7cd548f3e7`, CI 41229091248. Keep authorized unselected advisory semantics and incomplete File coverage. |
| FrontComposer authentication/current Tenant | Existing Projects `ProjectsUiSecurity`, `Program.cs`, `Routes.razor`, AppHost OIDC wiring and realm fixture compose owner APIs. FrontComposer provides `ServerCircuitUserContextAccessor` and `IFrontComposerTenantContextAccessor`. Their presence does not establish safe credential isolation. |
| Per-circuit gateway credential | **Fails** the isolated reproduction below. The default relay shares a token slot across all sessions with the same subject, independent of signed current Tenant. |

The approved E1–E7 November correction governs readiness. Package publication, production enrollment, rollback qualification and a duplicate hash inventory are not added as dispatch gates.

## Executed credential-isolation reproduction

The retained harness compiles the exact, unchanged owner `FrontComposerGatewayAuthorizationHandler`, `FrontComposerUserTokenStore` and `CircuitServicesAccessor` source files into a temporary Debug console program. It exercises the real handler with two controlled authenticated circuit principals, the same `sub`, different `eventstore:current-tenant` values and absent HttpContext. An in-memory terminal handler captures only synthetic credentials and performs no network operation.

1. Store the first session's credential; circuit A relays it correctly.
2. Simulate the second OIDC sign-in by storing its credential under the same subject; circuit B relays it correctly.
3. A different user's circuit still relays its own credential correctly.
4. Resume the original Tenant-A circuit: it relays session B's credential.

[Native result](run.txt): all three controls are true; `sameSubjectTenantIsolation` is false and `firstCircuitReceivedSecondSessionCredential` is true. The [build](build.txt) exits 0 with zero warnings/errors. The invariant-checking executable exits **1**; this is a reproduced failing prerequisite, not a passing test suite. [Exact command receipts](commands.json) retain both exit codes. The earlier direct scratch run is retained separately in `initial-run.txt` and `initial-build.txt`.

Reproduce from any checkout with these same owner sources, using a new output directory:

```bash
python3 _bmad-output/implementation-artifacts/evidence/story-6-5-prerequisite-readiness-20261105/reproduce.py --workspace /home/administrator/projects/hexalith/projects --output /tmp/story65-auth-isolation-rerun
```

Expected on the recorded owner revision: build exit 0, executable/script exit 1, the same booleans. `repro/` contains the complete harness. The scratch build avoids Projects' dependency graph and does not edit owner sources.

Source cause: `FrontComposerUserTokenStore.Set` overwrites a singleton dictionary indexed only by subject. `FrontComposerTokenRelayServiceExtensions` captures OIDC access tokens under that key. `FrontComposerGatewayAuthorizationHandler.ResolveUserIdAsync` ignores session and Tenant when recovering the current circuit's token. Source inspection shows the same lookup in `FrontComposerAccessTokenProvider`, including the captured SignalR provider; only the gateway handler path was executed here.

This proves cross-session credential substitution at the relay boundary. It does not claim a live browser exploit or unauthorized gateway disclosure: the supported gateway may independently reject a mismatched Tenant. Those later checks cannot satisfy 6.5's requirement that each circuit relay its own credential.

## Other verification and limits

- `python3 tools/planning/validate_production_authority.py --story-id 6.5`: exit 0. This permits the story's authority scope while 6.5 stays blocked; it is not proof that DW-1 is done.
- `dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false`: exit 1 before compilation. Conversations requires the Platform tool-selected source mapping. Exact output: [ui-build.txt](ui-build.txt). No UI xUnit run is claimed. The narrower unchanged-source reproduction above bypasses that unrelated dependency graph without weakening its build gate.
- No AppHost, browser, production provider, real credentials, domain reads or domain writes were used. No live or accessibility acceptance is claimed.

## Required owner repair and implementation handoff

FrontComposer must provide credentials bound to the authenticated session/circuit and current Tenant, covering same-subject concurrent sessions, distinct users, missing/expired credentials, sign-out and the captured provider. Reuse its existing authentication/relay tests, including `FrontComposerAccessTokenProviderTests` and `FrontComposerServerSecurityServiceExtensionsTests`; extend them for the failing isolation case. A Projects-owned token store/provider would duplicate platform authentication plumbing and is outside the approved story boundary. No sibling-module edits were authorized by this 6.5 dispatch.

After that owner repair is available and focused tests prove the seam, record its accepting revision/tests in DW-1 and the 6.5 prerequisites, close DW-1, and finish Build planning/readiness before dispatch. The eventual adapter plan must use actual `src/Hexalith.Projects.UI/Diagnostics/` paths (the old `Services/` map is stale), SDK `IEventStoreGatewayClient.SubmitQueryAsync<T>` and the delivered response DTOs. List enumeration and each row have separate snapshots; open/context/start are separate supported results and must not be merged into invented coherence. Advisory results have their own evaluation/incomplete fields, no AD-32 context admission authority. Existing audit/maintenance and legacy detail fan-out must not become the supported path.

## Verified owner repair and current readiness — 2026-11-05

The explicitly authorized FrontComposer repair supersedes the initial credential blocker above. The owner Debug/source suite passes 114/114 with no failures/errors/skips, including concurrent same-subject sessions across and within Tenants, distinct users, missing/expired authority, protected-cookie round trips, sign-out and captured SignalR reconnect providers. Projects UI source also builds with zero warnings/errors under the correct Platform-selected Projects source mapping. Exact source baseline plus uncommitted repair, commands, native XML, seven resolved review findings and limits are retained in [repair/README.md](repair/README.md).

DW-1 closes for that verified checkout; the unchanged FrontComposer base/gitlink alone is not repaired and no clean repaired commit is claimed. Story 6.5 stays blocked: its stale adapter map and combined-snapshot assumptions require planning correction, and its old subject-only authentication test fixture fails compilation until the prepared scope-limited migration is authorized/applied. No 6.5 implementation or sprint-status edit occurred. The parent failing harness and all original receipts remain historical and unchanged.


## Completed consumer/spec readiness — 2026-11-06

The final readiness result supersedes the earlier planning/fixture blockers while preserving their historical receipts. The retained fixture proposal was applied exactly; the existing UI test project now builds in Debug/source mode with the established Platform-selected Projects mapping, with zero warnings/errors. Its built xUnit assembly passes all 12 authentication composition cases, with zero failures/errors/skips/not-run. All 17 owner repair source/test files, owner patch and prior tested owner assemblies retain their verified hashes; DW-1 is not reopened.

The refreshed spec uses actual Diagnostics adapters and existing authentication, the SDK query client and delivered contracts. List/page, row, open, context and Conversation-start authority remain separate; advisory evaluation/incompleteness never supplies admission. [Final packet](readiness-final/README.md) distinguishes readiness from future browser, supported-handler, independent-admission, persisted no-domain-write and accessibility acceptance. The spec is a complete draft for a separate approval/implementation request, and the blocked sprint entry remains untouched. No runtime feature work, sibling source edit, commit, push, branch or dependency update occurred.


## Subsequent publication — 2026-11-06

The later user request to push all changes authorized publication of this completed work. FrontComposer repair is now committed and pushed as `f577f958ed1c043eec7feabc4885f59d48428818`; fresh owner 114/114 and Projects authentication composition 12/12 checks pass, with clean Debug/source builds. Earlier uncommitted-checkout statements remain historical. See [the publication record](publication/README.md). DW-1 remains done; no Story 6.5 implementation or sprint-status edit occurred.
