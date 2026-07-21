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

The Projects host validates issuer, audience, token lifetime, signature, and
HTTPS metadata before serving protected routes. Existing authorization gates
continue to enforce the `projects:*` action claims and safe-denial behavior.
The EventStore platform owns the P2 query-envelope mapping: `sub` remains the
original actor, workload and delegation evidence remain separate, and `scope`,
`scp`, and `aud` are preserved only when present and valid. Projects does not
recreate that helper or synthesize missing claims.

## Ownership and fixtures

- Identity/Security Owner: owns the issuer, audience, token-claim mapping, and
  production identity provider.
- Projects Owner: owns host adoption, action permissions, and safe-denial tests.
- Solution Architect: approves the contract and the rollback boundary.
- The local Keycloak realm at
  `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` is a
  Development fixture. Its realm attributes explicitly identify production
  configuration as external; its sample credentials are not deployment secrets.
- The P2 dependency is complete in EventStore. The current root-declared
  EventStore revision is `5c123ccbce2515a618134382d6181c2ec1a5cbbf`; it
  contains the P2 implementation commits `58236cf3` and `b904322b`.

The supported fixture matrix covers missing production configuration, valid and
invalid issuer/audience, expired credentials, delegated and non-delegated claims,
missing Projects permissions, cross-Tenant access, and malformed optional
delegation. Tests must assert that no protected Project metadata is returned.

## Reproducible Development fixtures

Run the deterministic host-startup, JWT middleware, safe-denial, forwarding,
and P2-envelope contract fixtures from the repository root:

```bash
dotnet test tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj \
  --configuration Release \
  --filter 'FullyQualifiedName~ProjectsAuthenticationContractTests|FullyQualifiedName~ProjectsClaimsTransformationTests'

dotnet test tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj \
  --configuration Release \
  --filter FullyQualifiedName~AspireTopologyTests
```

The default `http` server launch profile expects OIDC configuration. Anonymous
startup is available only through the explicitly named Development diagnostic
profile:

```bash
dotnet run --project src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj \
  --launch-profile anonymous-diagnostics
```

For a live local Keycloak fixture, copy the Keycloak and Projects HTTP endpoint
values shown by the Aspire dashboard, then request fixture tokens. These sample
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
