# Projects production identity contract

The Projects server must use deployment-managed OIDC configuration for every
Production host. The required keys are:

```text
Authentication__JwtBearer__Authority
Authentication__JwtBearer__Issuer
Authentication__JwtBearer__Audience
Authentication__JwtBearer__RequireHttpsMetadata=true
```

`Authentication__JwtBearer__AllowAnonymousDevelopment=true` is permitted only
for an explicitly named Development diagnostic process. It is rejected outside
Development. Production must not use the symmetric signing-key path, anonymous
startup, or credentials committed to this repository.

The Projects host validates the exact configured issuer, audience, token lifetime,
signature, and HTTPS metadata before serving protected routes. Discovery metadata
cannot add another accepted issuer, and audience comparison is case-sensitive and
preserves trailing slashes. Every bearer token must have one nonblank string `sub`;
a supplied name-identifier alias must equal that subject. Malformed array-shaped
tenant or permission claims grant no evidence, including null or nonstring array
members and unsupported object values. Outside Development, tokens must be signed
with RSA (`RS256`, `RS384`, `RS512`), RSA-PSS (`PS256`, `PS384`, `PS512`), or
ECDSA (`ES256`, `ES384`, `ES512`); HMAC and unsigned tokens are rejected even
when discovery metadata contains a matching key. Authentication challenges omit
token-validation details outside Development. Existing authorization gates
continue to enforce the `projects:*` action claims and safe-denial behavior.
HTTP-derived Projects tenant, actor and permission evidence comes only from the
primary authenticated identity, matching the claims transformer. The callback
envelope binding derives workload, delegation, scopes and audience from that same
identity. Supplementary identities do not fill missing authority or expand it,
regardless of their authentication type. An unauthenticated primary identity
provides no Projects authority. SDK-verified query callers retain their separate
verified-envelope authority path.
The EventStore platform owns the P2 query-envelope mapping: `sub` remains the
original actor, workload and delegation evidence remain separate, and `scope`,
`scp`, and `aud` are preserved only when present and valid. The platform uses
`scope` with `scp` as a fallback; it does not union the two claim spellings.
A declared nonblank `act` must resolve a delegation identifier through the
platform helper or bearer authentication fails. Absent or blank optional
`act` remains unknown and does not independently deny the request. Projects
uses the platform helper without recreating its parser or synthesizing claims.

The JWT parser uses the final member when a raw payload repeats a claim name,
as permitted by [RFC 7519 section 4](https://www.rfc-editor.org/rfc/rfc7519#section-4).
Projects and EventStore consume that same decoded identity; Projects does not
introduce a different raw-member or delegation mapping policy.

Projects forwards the original bearer token unchanged to EventStore. Deployment
owners must ensure that token satisfies both hosts' exact audience contracts,
using an approved shared audience or a token containing both approved audiences.
Configuring two individually valid hosts does not establish downstream token
compatibility. Before release acceptance, run an authorized Projects request that
reaches EventStore and verify downstream authentication and successful completion;
retain the request/result evidence with the accepted configuration coordinates.
This requirement does not select a new audience or authorize a deployment.

## Supported query workload authority — 2026-10-25

The legacy Projects-to-EventStore bearer-forwarding contract above remains applicable to those direct routes. Supported list/open queries enter the authenticated EventStore gateway and use the separate platform workload-authority path: gateway-observed actor, workload/delegation and permissions travel in an exact signed query envelope. The gateway issues its own short-lived workload assertion; it does not forward the user's bearer to Projects or Folders. Owner reads additionally retain the signed requester proof, original assertion verification, exact owner allowlist and current Folder authorization.

EventStore supports asymmetric per-request bindings using its opt-in client-credentials `authorization_details` profile. See the [authoritative platform contract](../../references/Hexalith.EventStore/docs/security/workload-binding-authority.md) and [Projects configuration categories and local proof](../supported-project-reads.md#asymmetric-local-proof-and-configuration-boundary--2026-10-25). Ordinary OIDC without this profile cannot add the required digest. The technical capability is available; production enrollment and acceptance remain absent.

Keep public user `Authentication:JwtBearer` configuration separate from the complete dedicated `Authentication:WorkloadJwtBearer` contract (workload authority and identical issuer, receiver audience, `AllowedAlgorithms=[RS256]`, HTTPS metadata). Each outbound `Authentication:WorkloadIssuer` needs its own server-owned workload/client identity, protected secret, exact token endpoint and `EnableResourceBindingRequests=true`. The enabled broker needs approved protected signer references and exact current grants: identity/revision, credential digest, workload, audience, operation, tenant/domain allowlists, validity and revocation state. Query grants explicitly enable `AllowQueryAuthorityDigest`; owner-gateway grants separately authorize the existing requester-proof operation. Do not infer authority from user roles, OAuth scopes, client-provided digests, network reachability or a public issuer's signing keys.

The controlled `ProjectReadAuthorityFixture` uses disposable synthetic signer/client material, actual broker/issuer/RS256 receiver code, gateway/Projects/Folder handlers and isolated Dapr persistence. Its tests cover exact binding faults, permission/audit enforcement, owner failures, grant revocation and public/workload trust separation while checking unchanged domain bytes/ETags. It substitutes local service discovery and deterministic current-head observations; it supplies no live identity/provider, protected production key custody, immutable deployment tuple or accepted gateway/owner equivalence. Required Dapr endpoints are `STORY61_DAPR_HTTP_ENDPOINT` and `STORY61_DAPR_GRPC_ENDPOINT`; their absence fails the new qualification cases. Build Server.Tests into a fresh `--artifacts-path` with Debug source references, run the built xUnit `-class '*SupportedProjectAuthorityHttpTests'` selection, then the full Server assembly with the same owned endpoints.

Before live enrollment the accountable owners still need the selected environment, exact HTTPS/TLS route and client trust, immutable accepted Projects/EventStore artifacts, signer/JWKS fingerprint and approval, protected signer/password/client-secret delivery references, exact scoped grant and receiver configuration, all other required workload enrollments, read-only verification observations and executable rollback. Revoking a currently loaded grant stops new issuance; the broker issues assertions with `exp - iat` at most 120 seconds, clipped by grant/certificate expiry. The current workload receivers independently apply 60 seconds of clock skew, so otherwise valid broker assertions may be accepted until `exp + 60 seconds`, approximately 180 seconds after issuance at the longest broker duration. The receiver maximum assertion duration (300 seconds by default) is separate from the broker and outbound client profile duration limit. Receiver tests retain that real policy and prove acceptance inside, and rejection outside, the expiry/skew window. Record configuration reload behavior, the actual receiver expiry/skew settings and emergency receiver/network/trust retirement. Disabling broker mapping requires the next host rollout and never requires deleting domain state. Local proof changes no P0/P1R/P2/P3/P4, conformance, independent READY, sprint acceptance or deployment gate, and the dated evidence below stays historical.

## Ownership and fixtures

- Identity/Security Owner: owns the issuer, audience, token-claim mapping, and
  production identity provider.
- Projects Owner: owns host adoption, action permissions, and safe-denial tests.
- Solution Architect: approves the contract and the rollback boundary.
- The local Keycloak realm at
  `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` is a
  Development fixture. Its realm attributes explicitly identify production
  configuration as external; its sample credentials are not deployment secrets.
- The historical P3 implementation referenced EventStore revision
  `5c123ccbce2515a618134382d6181c2ec1a5cbbf`. At Projects HEAD
  `4d8dcf65803792f7def3b10ed21227329536154b`, the root-declared EventStore
  revision is `6dededdecd62dd6dc6d1f15810108d860ec70c8f`, and Builds is
  `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`. The current
  [P2 owner handoff](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/6-1-p2-query-security-projection-capability-acceptance-record.md)
  remains draft and unaccepted. Local P3 continuation was explicitly authorized
  on 2026-10-01; it does not accept P2, P3, P4, a package/revision tuple, or a
  production deployment. Owners must supply accepted immutable coordinates,
  configuration/secret references, and executable rollback selection separately.

The supported fixture matrix covers missing production configuration, valid and
invalid issuer/audience, expired credentials, delegated and non-delegated claims,
missing Projects permissions, cross-Tenant access, and malformed optional
delegation. Rejection fixtures assert that no protected Project metadata is
returned, after a valid-token control proves the seeded Project is accessible.

## Reproducible Development fixtures

Run the deterministic host-startup, JWT middleware, safe-denial, forwarding,
and P2-envelope contract fixtures from the repository root:

```bash
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj \
  --configuration Debug -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0

dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll \
  -class Hexalith.Projects.Server.Tests.Authentication.ProjectsAuthenticationContractTests \
  -class Hexalith.Projects.Server.Tests.ProjectsClaimsTransformationTests

dotnet build tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj \
  --configuration Debug -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0

dotnet tests/Hexalith.Projects.Integration.Tests/bin/Debug/net10.0/Hexalith.Projects.Integration.Tests.dll \
  -class Hexalith.Projects.Integration.Tests.AspireTopologyTests
```

Local Debug builds use the root-declared sibling source projects. Invoke the
built xUnit v3 assembly directly for focused filters; `global.json` selects
Microsoft.Testing.Platform, so project-level `--filter` is not the fixture path.
For complete regression verification, invoke both assemblies without `-class`.
The two MSBuild properties above are local build environment pins used for this
verification; they do not change centrally managed package versions or CI gates.
CI/release validation must separately use Release and the centrally pinned NuGet
packages.

On 2026-10-01, after resumed independent review and repairs, local verification
passed 133 focused authentication/identity tests, 808 server tests, and 27
integration tests with no failures or skips. Both Debug test-project builds
passed with zero warnings/errors. The signed-token forwarding fixture exercises
the real EventStore query controller and claims validators over HTTP; its
mediator handler captures identity without persistence. The retained local
[evidence manifest](../../_bmad-output/implementation-artifacts/evidence/6-1-p3-review-2026-10-01/manifest.json)
binds tested source/assembly hashes, compressed command logs, and the latest
review repairs. The [previous local packet](../../_bmad-output/implementation-artifacts/evidence/6-1-p3-local-2026-10-01/manifest.json)
retains the earlier 117/792/27 results.
These results use the current source working trees, including concurrent sibling
edits; they do not establish an immutable baseline or Release package acceptance.
A fresh isolated Aspire start with `--no-build` succeeded and reported Projects,
EventStore, Tenants, and security healthy; the AppHost was then stopped. This
replaces the earlier restore-timeout observation for startup only. Neither
startup health nor in-process fixtures constitutes authenticated persisted
Keycloak/G-4 acceptance evidence.

The default `http` server launch profile expects OIDC configuration. Anonymous
startup is available only through the explicitly named Development diagnostic
profile:

```bash
dotnet run --configuration Debug --project src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj \
  --launch-profile anonymous-diagnostics
```

For executable live setup, use the [managed AppHost fixture procedure](../../tests/e2e/README.md#live-apphost-route).
The managed runner derives tenant/principal identity from a real signed Keycloak
token and provisions tenant access through supported APIs; its
[Project fixtures](../../tests/e2e/support/fixtures/projects-fixtures.ts) create
Projects, wait for projection convergence, and archive them during cleanup.
The runner stops its own AppHost when finished. This is the supported automated
route for seeding and authorized controls; it still requires separately accepted
G-4 and owner evidence for production approval.

The curl snippets below are manual diagnostics for a separately running,
seeded local topology. If the authorized control denies, treat setup as failed
and do not count the subsequent negative responses as verification.
Copy the Keycloak and Projects HTTP endpoint values shown by `aspire describe`,
then request fixture tokens. These sample
passwords exist only in the Development realm import and are not production
credentials:

```bash
export KEYCLOAK_FIXTURE_URL='http://localhost:<keycloak-port>'
export PROJECTS_FIXTURE_URL='http://localhost:<projects-port>'

PROJECTS_FIXTURE_TOKEN="$({ curl -fsS \
  -X POST "$KEYCLOAK_FIXTURE_URL/realms/hexalith/protocol/openid-connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=password' \
  --data-urlencode 'client_id=hexalith-eventstore' \
  --data-urlencode 'username=tenant-a-user' \
  --data-urlencode 'password=tenant-a-pass'; } | jq -er '.access_token')"

READONLY_FIXTURE_TOKEN="$({ curl -fsS \
  -X POST "$KEYCLOAK_FIXTURE_URL/realms/hexalith/protocol/openid-connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=password' \
  --data-urlencode 'client_id=hexalith-eventstore' \
  --data-urlencode 'username=readonly-user' \
  --data-urlencode 'password=readonly-pass'; } | jq -er '.access_token')"

curl -i "$PROJECTS_FIXTURE_URL/api/v1/projects" \
  -H "Authorization: Bearer $PROJECTS_FIXTURE_TOKEN"

curl -i "$PROJECTS_FIXTURE_URL/api/v1/projects" \
  -H "Authorization: Bearer $READONLY_FIXTURE_TOKEN"

curl -i "$PROJECTS_FIXTURE_URL/api/v1/projects" \
  -H "Authorization: Bearer $PROJECTS_FIXTURE_TOKEN" \
  -H 'X-Hexalith-Tenant-Id: tenant-b'
```

After the tenant-a fixture has been seeded, the first request is the authorized
control. The read-only and cross-Tenant requests must both return the same
metadata-only `404` safe-denial shape (`tenant_access_denied`,
`resource_unavailable`, `details.visibility=redacted`).

## Rollback

Rollback restores the last owner-approved Projects host revision and its
deployment-managed OIDC configuration as one unit. It must retain issuer,
audience, HTTPS metadata, and fail-closed startup requirements. Rolling back to
anonymous production behavior or committing a signing key is not an approved
recovery path. Revalidate the P2 revision and run the server authentication and
integration contract suites before reopening protected traffic.

Before deployment, the accountable owners must record the exact accepted
Projects/EventStore/Builds revisions, package versions, deployment configuration
revision, secret references, rollback trigger, and executable restore procedure.
The prior historical pin and local passing tests cannot supply that approval.
