---
title: 'Approve the production identity and authentication contract'
type: 'feature'
created: '2026-07-19'
status: 'in-progress'
baseline_commit: 'd4a69ad9a640294e849444a60d7ddfbd0468f91a'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/project-context.md'
  - '{project-root}/src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `Hexalith.Projects.Server` currently enables bearer authentication only when an
authority happens to be configured. Missing production identity configuration therefore leaves an
implicit anonymous host path, and the local claim shim does not document or prove the P2 dual-principal
identity contract.

**Approach:** Add a validated Projects authentication contract that requires OIDC authority, issuer,
audience, and secure metadata in Production, while allowing only an explicit Development bypass.
Adopt the platform-owned P2 identity mapping and provide deterministic configuration/token fixtures,
ownership documentation, and rollback evidence without duplicating EventStore query-envelope logic.

## Boundaries & Constraints

**Always:** Bind `Authentication:JwtBearer` through options validation before serving endpoints; derive
actor, workload, delegation, scopes, and audience only from validated token claims and the accepted P2
platform contract; keep authorization fail-closed and metadata-only; keep production secrets outside
source control; preserve the existing local Keycloak fixture and safe-denial behavior.

**Ask First:** Stop if the accepted P2 public mapping or revision changes, if an Identity/Security Owner
or Solution Architect rejects the claim/configuration contract, or if production deployment ownership,
secret references, or rollback pins are not available. Do not select a new identity provider or audience
without owner approval.

**Never:** Do not permit anonymous or symmetric-key authentication in Production; do not infer identity
from headers, query/body fields, Dapr metadata, or client-supplied tenant values; do not copy the P2
query envelope/helper into Projects; do not commit production credentials or switch the public read route.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Production startup | Missing/blank authority, issuer, audience, or insecure metadata setting | Host fails validation before serving protected endpoints | Deterministic startup/configuration failure; no anonymous fallback |
| Development bypass | `Development` environment and explicit bypass flag | Host may start without OIDC only for local diagnostics | Same flag in Production is rejected |
| Valid token | Trusted issuer/audience, lifetime, signature, and required Projects permission | Existing authorization gate receives normalized identity; accepted P2 envelope keeps actor/workload/delegation distinct and preserves scopes as routing metadata | Missing required permission fails closed with safe denial |
| Invalid identity | Wrong issuer/audience, expired token, malformed delegation, or absent required permission | No protected Project data is returned; malformed optional delegation remains unknown | Authentication failure or safe authorization denial; no claim detail disclosure |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/Program.cs` -- current conditional JWT registration and middleware pipeline.
- `src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs` -- existing tenant, principal, and permission normalization.
- `src/Hexalith.Projects.AppHost/Program.cs` -- local security resource wiring and Projects environment injection.
- `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` -- development-only identity fixtures.
- `src/Hexalith.Projects.AppHost/DaprComponents/accesscontrol.yaml` -- local versus production Dapr access-control boundary.
- `tests/Hexalith.Projects.Server.Tests/ProjectsClaimsTransformationTests.cs` -- claim normalization tests.
- `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` -- AppHost security wiring contract tests.
- `tests/Hexalith.Projects.Integration.Tests/DaprConfigurationTests.cs` -- local/production access-control assertions.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.Server/Authentication/ProjectsAuthenticationOptions.cs` -- add environment-aware required OIDC settings and startup validation -- prevent implicit anonymous production startup.
- [x] `src/Hexalith.Projects.Server/Authentication/ProjectsAuthenticationServiceCollectionExtensions.cs` and `src/Hexalith.Projects.Server/Program.cs` -- register validated bearer authentication/authorization and an explicit Development-only bypass -- preserve the existing endpoint gate and safe denial.
- [x] `src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs` -- align normalized claims with the accepted P2 mapping without synthesizing missing actor, workload, delegation, scope, or audience evidence -- keep identity provenance authoritative.
- [x] `src/Hexalith.Projects.AppHost/Program.cs` and `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` -- make local fixture wiring explicit and keep production secret/config ownership external -- prevent dev credentials from becoming deployment defaults.
- [x] `tests/Hexalith.Projects.Server.Tests/Authentication/ProjectsAuthenticationContractTests.cs` and related existing tests -- cover startup/configuration, valid/invalid token claims, delegated/non-delegated mapping, scope preservation, missing permission, and safe-denial behavior -- create the P3 verification fixture set.
- [x] `docs/runbooks/projects-production-identity-contract.md` -- record configuration keys, owner responsibilities, fixture commands, accepted P2 pin, and rollback procedure -- make the approval and deployment boundary reviewable.

**Acceptance Criteria:**
- Given a Production host, when required OIDC configuration is absent or insecure, then startup fails before protected endpoints can serve anonymously.
- Given an explicit Development bypass, when the environment is not Development, then validation rejects it and no bypass is activated.
- Given valid non-delegated or delegated token claims, when Projects forwards the authenticated request, then the P2 envelope preserves original actor, workload, delegation, scopes, and audience separately; malformed optional delegation remains unknown.
- Given invalid issuer/audience, expired credentials, absent required permission, or cross-Tenant access, when a protected read is attempted, then no protected metadata is disclosed and the existing safe-denial contract remains intact.
- Given local and production configuration review, when the owner-approved fixture and rollback checks run, then no production secret is stored in the repository, the local Keycloak fixture is clearly development-only, and the accepted P2 revision/configuration can be reverted deterministically.

### Review Findings

- [x] [Review][Decision] HIGH — P3 advances while its required P2 dependency remains unaccepted — Resolved 2026-07-20: the EventStore-owned P2 spec is `done`; its implementation commits `58236cf3` and `b904322b` are contained by the current root-pinned EventStore revision `5c123ccb`. The remaining Projects-root P2 evidence bookkeeping is stale cross-repository planning, not an active EventStore dependency or a P3 code-review blocker.
- [x] [Review][Decision] HIGH — The required-scope authorization policy contradicts the current P2 contract — Resolved 2026-07-20: `eventstore:permission` remains the Projects authorization input. Validated `scope`/`scp` claims are preserved as routing metadata in the accepted P2 envelope, but their absence does not independently authorize or deny a request.
- [x] [Review][Decision] HIGH — Deterministic rollback inputs and approvals are unavailable — Resolved 2026-07-20: exact deployment release/package/configuration pins, approval evidence, and executable rollback selection belong to P4 release acceptance. P3 documents and verifies the authentication contract and rollback invariants without blocking its code review on artifacts that do not exist until release assembly.
- [x] [Review][Patch] HIGH — Default launch profile keeps anonymous bypass active when Aspire injects OIDC [src/Hexalith.Projects.Server/Properties/launchSettings.json:11]
- [x] [Review][Patch] MEDIUM — Disabling bundled Keycloak also forces anonymous mode instead of honoring external Development OIDC [src/Hexalith.Projects.AppHost/Program.cs:57]
- [x] [Review][Patch] MEDIUM — Options binding and manual configuration parsing can select different authentication modes [src/Hexalith.Projects.Server/Authentication/ProjectsAuthenticationServiceCollectionExtensions.cs:32]
- [x] [Review][Patch] MEDIUM — Claim normalization can read authorization evidence from a different identity than the authenticated identity [src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs:29]
- [x] [Review][Patch] HIGH — Startup fail-closed verification never starts the host and does not isolate all required-field and HTTPS guards [tests/Hexalith.Projects.Server.Tests/Authentication/ProjectsAuthenticationContractTests.cs:31]
- [x] [Review][Patch] HIGH — JWT middleware, protected safe-denial scenarios, and reproducible fixture commands are not exercised [tests/Hexalith.Projects.Server.Tests/Authentication/ProjectsAuthenticationContractTests.cs:149]
- [x] [Review][Patch] HIGH — P2 actor/workload/delegation mapping and token forwarding are not verified at the EventStore envelope boundary [tests/Hexalith.Projects.Server.Tests/ProjectsClaimsTransformationTests.cs:65]
- [x] [Review][Patch] MEDIUM — Development OIDC validation accepts unusable authority and metadata combinations [src/Hexalith.Projects.Server/Authentication/ValidateProjectsAuthenticationOptions.cs:40]
- [x] [Review][Patch] MEDIUM — Runbook misidentifies the root-pinned EventStore revision [docs/runbooks/projects-production-identity-contract.md:36]
- [ ] [Review][Patch] MEDIUM — Unrelated FrontComposer submodule pointer bump is included in the P3 change [references/Hexalith.FrontComposer:1] — Not applied: removing it now requires rewriting committed P3 history or moving the user-owned FrontComposer checkout from `6a00bd95` back to `b0254994`; this remediation preserves both states.

## Design Notes

The Projects host owns adoption and fail-closed startup policy; EventStore owns the dual-principal
query-envelope implementation. The AppHost may inject authority/issuer/audience and clear any signing-key
override for local OIDC, but production values must come from deployment-managed configuration or secret
references. `scope`/`scp` values remain forwarded routing metadata; action-specific
`eventstore:permission` evidence is the fail-closed Projects authorization input.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release` -- expected: authentication and existing server tests pass.
- `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --configuration Release` -- expected: AppHost/access-control contract tests pass.

**Results (2026-07-20):** 582/582 server tests and 20/20 integration tests passed in Release configuration with `--no-restore`.

**Manual checks (if no CLI):**
- Inspect the production configuration/secret references and rollback record; they must contain no committed credential and must reject the Development bypass outside Development.
