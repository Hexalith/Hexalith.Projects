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
- The P2 dependency is the current root-declared EventStore checkout
  `b904322b` (`feat: add delegated query identity support`) pending P2 owner
  acceptance. P3 cannot self-accept that dependency.

The supported fixture matrix covers missing production configuration, valid and
invalid issuer/audience, expired credentials, delegated and non-delegated claims,
missing Projects permissions, cross-Tenant access, and malformed optional
delegation. Tests must assert that no protected Project metadata is returned.

## Rollback

Rollback restores the last owner-approved Projects host revision and its
deployment-managed OIDC configuration as one unit. It must retain issuer,
audience, HTTPS metadata, and fail-closed startup requirements. Rolling back to
anonymous production behavior or committing a signing key is not an approved
recovery path. Revalidate the P2 revision and run the server authentication and
integration contract suites before reopening protected traffic.
