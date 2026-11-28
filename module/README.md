# Projects domain declaration

`hexalith.module-manifest.v2.json` declares the existing Projects executable domain server. It is a normal owner declaration using the supported v2 schema, independent of Story 6.4 verification. The root `projects` module identity lets public Builds `prepare-build` generate a mapping containing `Hexalith.Projects` and its declared direct references. Builds owns the generated mapping and imports.

The declaration inventories the internal EventStore workload surface of the Projects executable.

## Contract provenance

- `src/Hexalith.Projects.Server/Program.cs` starts the executable with the EventStore domain-service SDK. Projects registers its existing keyed processor, aggregate replay, full-replay projection, named projections and queries before SDK discovery.
- `ProjectsServerModule.DomainName` is `projects`; runtime registration defaults the projection app identity and workload audience to `projects`, and the allowed workload caller to `eventstore`.
- The SDK maps `POST /process`, `/replay-state`, `/query`, `/project`, `/project/v2`, `/project/v2/reconcile`, `/project/rebuild/v1`, `/project/rebuild/part/v1` and `/admin/operational-index-metadata`. The declaration grants EventStore these exact callbacks. Each requires its workload policy, app-channel token, and a peer binding to the admitted catalog. Additional SDK projection protocols carry no invocation grant in this declaration.
- The SDK maps `/health`, `/alive` and `/ready`. Catalog readiness requires the committed authority.
- `src/Hexalith.Projects.Infrastructure/ProjectsStateStoreOptions.cs` binds `Projects:StateStore:StateStoreName`. The shared state component uses conservative `authoritative-restore` classification, matching the shared EventStore state capability. Projects projection rows being derived does not make that shared component disposable or establish an implemented restore hook.
- Dependencies follow existing EventStore, Tenants, Parties, Conversations, Folders and Memories contract/client/runtime references and registration. `ProjectsDomainProcessor` delegates grant/revoke bootstrap to `ProjectOwnerBootstrapVerifier`, which requires the Parties authority query through EventStore. This declaration does not invent missing dependency declarations.

`runtime.extensions` is empty because this executable does not declare an extension package. `Hexalith.Projects.Server.csproj` has `IsPackable=false`; it is not represented as a released package. Resource requests/limits, pub/sub subscriptions, workers, lifecycle hooks and external-provider bindings are not invented.

The retained `/api/v1/projects` routes use the SDK's explicit human bearer policy and keep their tenant, resource and action checks. The internal `/query` receiver keeps handler-side authorization and uses the Projects response policy for its existing 400 and 404 outcomes.

## Public preparation

With the qualified Builds CLI available, run from the Projects repository root, choosing a fresh output directory:

```text
hexalith-module prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace <fresh-absolute-directory> --output json
```

Use every emitted `buildPreparation.environment` value unchanged for local Debug builds. Never reuse Conversations' mapping for Projects or edit emitted imports. Source preparation can warn about Platform identity/release or catalog mismatches; it cannot qualify package mode.

## Ordinary local catalog

Set `Projects:DevelopmentRouteCatalog` in AppHost user secrets. Supply the owner-held `CustodyDirectory`, `AuthorityComponentName`, `AuthorityStateKey`, `ActiveDigestKeyGeneration`, `DigestKeys` and `SecretMapGeneration` for the selected Redis backend. A newly provisioned digest ring is appropriate only for a new isolated business backend. The EventStore [development route catalog reference](../references/Hexalith.EventStore/docs/reference/development-route-catalog.md) describes how to provision and retain this custody. The ordinary topology composes its catalog only from a local Debug source build; a package-mode run refuses at startup. The ordinary AppHost admits the compiled Projects and Tenants declarations through the SDK and uses the Platform's local Dapr mTLS control plane to bind peer identities.

Supply `Projects:DevelopmentRouteCatalog:Workload` settings `Authority`, `Issuer` and `TokenEndpoint` for the bound workload issuer, and its client credentials `GatewayClientId`, `GatewayClientSecret`, `ProjectsClientId`, `ProjectsClientSecret`, `TenantsClientId` and `TenantsClientSecret`. Supply `HumanAuthorityReceiptSigningKey` in the same section for the shared human command receipt configuration. The workload issuer grants EventStore the exact operation and audience required by each callback and signs the authenticated originating caller binding for Projects queries. It grants Projects `eventstore:stream-read` and `eventstore:owner-authority-query`, and Tenants `eventstore:stream-read`, for the `eventstore` audience. The AppHost generates run-scoped owner proof keys for Projects and Tenants. EventStore trusts their public keys for the `projects` domain and for the `tenants` and `global-administrators` domains respectively, and admits both as internal callers. Keycloak supplies the separate local human bearer contract. The private read and isolated live profiles continue to use their own custody and issuer.

### Disposable ordinary Debug qualification

Build the Platform Keycloak provider with `mvn -B -ntp package` in
`references/Hexalith.Platform/infrastructure/keycloak-workload-binding`. Install
the locked Playwright dependencies with `CI=1 npm ci --ignore-scripts` in `tests/e2e`.
The launcher runs a Playwright focus check even for `--ordinary-only`. From the
Projects root, launch the runner as the direct child of Platform's
[run-owned enrollment launcher](../references/Hexalith.Platform/docs/security/workload-binding-enrollment.md#disposable-run-owned-enrollment).
Choose a new evidence directory outside the temporary workspace.

```bash
run-with-temp python3 -B references/Hexalith.Platform/infrastructure/keycloak-workload-binding/run_owned.py \
  --owner disposable-projects-acceptance --lifetime-seconds 1200 -- \
  python3 -B tools/qualification/run_live_project_fixtures.py --ordinary-only \
  --evidence '<new-absolute-evidence-directory>'
```

If the Aspire dashboard container tunnel times out, add
`env ASPIRE_ENABLE_CONTAINER_TUNNEL=false` immediately after `run-with-temp` for
that invocation. The native checks and cleanup still run against the owned topology.
[Aspire container networking](https://aspire.dev/fundamentals/container-networking/#disable-the-container-tunnel)

The runner builds Debug sources and verifies a single-use lease for EventStore,
Projects, and Tenants over HTTPS with process-scoped trust. It creates a fresh
Redis backend and private catalog custody for the ordinary phase. The ordinary
local Keycloak realm supplies its scoped human account and Tenants bootstrap.
The native probe records the SDK's shared committed generation and persisted
command history. It dispatches that history through the enrolled named
projection callback and compares the resulting projection with the Project
query. It also checks that credential refusals leave persisted state unchanged.
The runner reports cleanup and verifies preservation of preexisting processes
and containers. Omit `--ordinary-only` to run the compatibility matrix first
and choose a lease long enough for both phases.

Use the owner-held configuration above for shared or persistent installations.
Projects workers continue to receive tenant and project events through the
ordinary pub/sub configuration. DW-93 tracks their workload enrollment scope.
DW-97 tracks reconcile and staged rebuild qualification.

## Remaining runtime prerequisites

Starting a composed runtime additionally requires an owner-supported binding for the actual Projects executable (`projects/domain`), dependency declarations including Folders, and scoped authentication configuration. The current owner executable consumes `Authentication:JwtBearer` (Authority, Issuer, Audience and RequireHttpsMetadata), workload receiver settings, the `Authentication:WorkloadJwtBearer` JWT contract, and outbound `Authentication:WorkloadIssuer` client credentials/token-endpoint settings (authority discovery comes from the workload JWT contract). The sidecar channel consumes `APP_API_TOKEN`; outbound issuer credentials use `Authentication:WorkloadIssuer:ClientSecret`. `ProjectChatBotAuthorityGateway` also requires `Authentication:BoundStreamRead:PrivateKey` for authoritative stream and owner-authority proofs, with its public trust enrolled by EventStore. The declaration records these logical secret names without values and supplies no credentials or fabricated extension package. Identity egress records bearer and workload OIDC metadata/JWKS and the explicit or discovered workload token endpoint as symbolic destinations; deployment must resolve them. Only when the entire `Authentication:WorkloadJwtBearer` section is absent does the SDK select `Authentication:JwtBearer`. A missing Authority value within a present dedicated workload section does not trigger fallback.

The configurable `Projects:StateStore:StateStoreName` binding does not cover every registered projection. `ProjectReadModelKeys.StoreName` fixes named Project reads to `statestore`; `ConversationStartSetupProjectionHandler.StoreName` fixes Conversation-start projections to `projects-conversation-start-setup`. Full runtime enrollment must supply both named components through an owner-supported binding. Changing the Projects option does not redirect them. The current v2 Dapr declaration requires a real configuration key; no key is invented for these fixed names. This remains a runtime prerequisite rather than a claim of complete runtime enrollment.

Gateway/store/runtime proof also needs authenticated EventStore and owner services, Dapr sidecars, enrolled authority grants, current tenant/resource evidence, and measured store end-state. Empty lifecycle hooks/recovery inventory do not establish recovery readiness. Source tests of this candidate are not a qualified release, deployed acceptance or Story completion.
