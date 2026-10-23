---
title: 'Close five deferred Projects authentication verification items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: 'ffd96c694839d83e6977da9cac3628a2cb3ecfa1'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred authentication obligations lack current closure evidence: raw numeric JWT subject rejection, forwarding through the registered EventStore gateway pipeline, a signed foreign-Tenant principal against existing Project data, rejection of tokens without expiration, and duplicate-subject rejection independent of a conflicting identity alias. The numeric-subject runtime fix and signed controls already exist, but its ledger entry remains open.

**Approach:** Close this single authentication-verification slice on main and push the verified commit as requested. Verify numeric subjects remain numeric on the signed wire and are rejected by middleware. Exercise the real registered gateway client from an authenticated HTTP request through the real downstream query controller, preserving delegated and nondelegated identity assertions. Add signed-token negative controls for absent expiration and multiple subjects without an alias. Prove that valid actors in two Tenants can read their own seeded Projects while a Tenant-b principal receives the same safe denial for an existing Tenant-a Project and a nonexistent Project, with and without a conflicting hint. Record evidence against exactly these five ledger entries. Preserve existing authority policy, public contracts, dependency pins, historical acceptance and production deployment decisions.

</frozen-after-approval>

## Implementation Notes

- No unresolved intent questions; changes are reversible root-owned fixture and bookkeeping edits. No new production API or authority policy is needed. The user explicitly authorized committing and pushing main; use the one-shot route.
- Code map: tests/Hexalith.Projects.Server.Tests/Authentication/ProjectsAuthenticationContractTests.cs owns signed-token creation, authenticated Kestrel fixtures, Project/tenant-access seed data, safe-denial checks, and the real EventStore query-controller hop. Reuse these helpers; the registered gateway fixture must not install its own forwarding handler or typed client. Only redirect its options BaseAddress to the temporary host.
- Existing production implementation: ProjectsAuthenticationServiceCollectionExtensions.HasStringSubject inspects the validated JWT payload; lifetime and duplicate-subject validation are already strict. ProjectsServerServiceCollectionExtensions.AddProjectsServerRuntimeInfrastructure registers IEventStoreGatewayClient with EventStoreGatewayTokenForwardingHandler. No production changes anticipated.
- Relevant constraints: root AGENTS, required Hexalith baseline and Git instructions, EditorConfig and attributes, Server test project/build props, AD-19/AD-20, and docs/runbooks/projects-production-identity-contract.md. This is deterministic local verification, not production identity acceptance.
- Baseline: Debug Server test-project build passed with zero warnings/errors; existing authentication class passed 125/125. Aspire start succeeded; describe observed security Unhealthy and dependent resources Waiting, so full topology health is not established. Exact start/describe/stop outputs are in /tmp/five-deferred-auth-aspire-*. The selected Projects AppHost was stopped after inspection; the unrelated Tenants AppHost is untouched.

- First implementation run: 139/139 authentication and claims-transformation tests passed with zero failures/skips. Added an HTTP observer so the registered-client test also asserts the literal bearer bytes received by the downstream controller.
- The five selected ledger entries now identify their concrete regression evidence; numeric-subject closure explicitly verifies the existing runtime fix.

## Verification

- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — passed, zero warnings/errors.
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -m:1 -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — passed, zero warnings/errors.
- `dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class Hexalith.Projects.Server.Tests.Authentication.ProjectsAuthenticationContractTests -class Hexalith.Projects.Server.Tests.ProjectsClaimsTransformationTests -noLogo` — 139/139 passed, zero errors/failures/skips/not-run, 26.702 seconds.
- `dotnet tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests.dll -class Hexalith.Projects.Server.Tests.Authentication.ProjectsAuthenticationContractTests -class Hexalith.Projects.Server.Tests.ProjectsClaimsTransformationTests -noLogo` — 139/139 passed, zero errors/failures/skips/not-run, 21.934 seconds. This verifies the fixtures compile and execute against pinned packages as well as sibling source.
- `git diff --check` — passed; authentication source and new spec retain CRLF; historical mixed ledger line endings remain unchanged outside the five additions.
- Commit message validated with commitlint 21.1.0 and the repository config.
- AppHost baseline commands: `timeout 55s aspire start --non-interactive --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` — exit 0; `timeout 40s aspire describe --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --format Json` — exit 0, security Unhealthy and dependent resources Waiting; exact-target `aspire stop --non-interactive --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` — exit 0. No healthy distributed-topology or production acceptance claim is made by these deterministic authentication fixtures.

## Review Triage Log

- BH1 — medium, patched: the nonexistent Project was requested only without a hint. The two existence cases now use identical headers inside each hint iteration; both bodies also match the unhinted baseline.
- BH2 — low, patched: alias-independent duplicate-subject construction was implicit in the helper. The signed payload now explicitly contains the two expected subjects and contains the name-identifier alias exactly when requested. Numeric wire-type verification remains.
- BH3 — low, patched: the new registered pipeline fixture initially sent one token per host. Each of its four combinations now sends two distinct actors, workloads and delegation identities through one host and pooled handler chain, asserting the literal current token and downstream identity each time. This is additional fixture hardening, not a demonstrated production isolation defect.
- BH4 — low, patched: the initial spec recorded only intermediate local results. Verification now records exact commands, build/test results and the runtime baseline limitation; final post-review results will supersede the initial runs below. No production or distributed-runtime acceptance is claimed.

## Post-review verification

- The exact Debug/source build command above passed on the reviewed implementation with zero warnings/errors (29.42 seconds).
- The exact Release/package build command above passed on the reviewed implementation with zero warnings/errors (6.73 seconds).
- The exact Debug test command above passed 139/139, with zero errors/failures/skips/not-run (10.599 seconds).
- The exact Release test command above passed 139/139, with zero errors/failures/skips/not-run (11.142 seconds).
- These final runs include matching-header existence controls, signed duplicate-subject payload assertions, the exact bearer observer and two callers sharing each registered gateway handler pool. All four review findings are addressed; none deferred.
- A byte comparison against the baseline confirms exactly five status/resolution additions to deferred-work.md and preservation of every other historical byte. Final whitespace and CRLF checks passed. Both commits and dependency revisions remain under normal validation; no qualification or acceptance status was changed.
