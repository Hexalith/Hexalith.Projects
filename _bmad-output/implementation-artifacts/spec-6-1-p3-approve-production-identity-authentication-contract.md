---
title: 'Approve the production identity and authentication contract'
type: 'feature'
created: '2026-07-19'
status: 'in-review'
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
| Valid token | Trusted issuer/audience, lifetime, signature, and required Projects scope/permission | Existing authorization gate receives normalized identity; accepted P2 envelope keeps actor/workload/delegation distinct | Missing required authorization fails closed with safe denial |
| Invalid identity | Wrong issuer/audience, expired token, malformed delegation, or absent scope | No protected Project data is returned; malformed optional delegation remains unknown | Authentication failure or safe authorization denial; no claim detail disclosure |

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
- [x] `tests/Hexalith.Projects.Server.Tests/Authentication/ProjectsAuthenticationContractTests.cs` and related existing tests -- cover startup/configuration, valid/invalid token claims, delegated/non-delegated mapping, missing scope, and safe-denial behavior -- create the P3 verification fixture set.
- [x] `docs/runbooks/projects-production-identity-contract.md` -- record configuration keys, owner responsibilities, fixture commands, accepted P2 pin, and rollback procedure -- make the approval and deployment boundary reviewable.

**Acceptance Criteria:**
- Given a Production host, when required OIDC configuration is absent or insecure, then startup fails before protected endpoints can serve anonymously.
- Given an explicit Development bypass, when the environment is not Development, then validation rejects it and no bypass is activated.
- Given valid non-delegated or delegated token claims, when Projects forwards the authenticated request, then the P2 envelope preserves original actor, workload, delegation, scopes, and audience separately; malformed optional delegation remains unknown.
- Given invalid issuer/audience, expired credentials, absent required scope/permission, or cross-Tenant access, when a protected read is attempted, then no protected metadata is disclosed and the existing safe-denial contract remains intact.
- Given local and production configuration review, when the owner-approved fixture and rollback checks run, then no production secret is stored in the repository, the local Keycloak fixture is clearly development-only, and the accepted P2 revision/configuration can be reverted deterministically.

## Design Notes

The Projects host owns adoption and fail-closed startup policy; EventStore owns the dual-principal
query-envelope implementation. The AppHost may inject authority/issuer/audience and clear any signing-key
override for local OIDC, but production values must come from deployment-managed configuration or secret
references. A missing scope is an authorization failure, not a token-parser excuse to expose claim data.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release` -- expected: authentication and existing server tests pass.
- `dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --configuration Release` -- expected: AppHost/access-control contract tests pass.

**Manual checks (if no CLI):**
- Inspect the production configuration/secret references and rollback record; they must contain no committed credential and must reject the Development bypass outside Development.
