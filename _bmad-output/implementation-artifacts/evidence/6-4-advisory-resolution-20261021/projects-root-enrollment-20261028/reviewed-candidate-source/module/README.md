# Projects domain declaration

`hexalith.module-manifest.v2.json` declares the existing Projects executable domain server. It is a normal owner declaration using the supported v2 schema, independent of Story 6.4 verification. The root `projects` module identity lets public Builds `prepare-build` generate a mapping containing `Hexalith.Projects` and its declared direct references. Builds owns the generated mapping and imports.

The declaration inventories the internal EventStore workload surface; it does not complete Platform runtime migration, supply a qualified release, or activate CI, ingress or a new host. The source candidate is based on Projects `6cdf9bbdc0824e59529c9a99c16b7ec02e03e21b`.

## Contract provenance

- `src/Hexalith.Projects.Server/Program.cs` starts the existing executable and registers `AddProjectsServer`, runtime infrastructure and endpoint mapping.
- `ProjectsServerModule.DomainName` is `projects`; runtime registration defaults the projection app identity and workload audience to `projects`, and the allowed workload caller to `eventstore`.
- `ProjectsServerServiceCollectionExtensions.MapProjectsServerEndpoints` maps `POST /process`, `/project`, `/query` and `/admin/operational-index-metadata`, protected in the runtime host by `EventStoreDomainServicePolicies.Process`, `.Project`, `.Query` and `.Metadata` respectively.
- `Projections/ProjectReads/ProjectReadPlatformEndpoints.cs` additionally maps `POST /project/v2` and `/project/rebuild/v1` under the same `.Project` policy. No other SDK callback is declared.
- `src/Hexalith.Projects.ServiceDefaults/ProjectsServiceDefaults.cs` maps `/ready` in the executable host.
- `src/Hexalith.Projects.Infrastructure/ProjectsStateStoreOptions.cs` binds `Projects:StateStore:StateStoreName`. The shared state component uses conservative `authoritative-restore` classification, matching the shared EventStore state capability. Projects projection rows being derived does not make that shared component disposable or establish an implemented restore hook.
- Dependencies follow existing EventStore, Tenants, Parties, Conversations, Folders and Memories contract/client/runtime references and registration. `ProjectsDomainProcessor` delegates grant/revoke bootstrap to `ProjectOwnerBootstrapVerifier`, which requires the Parties authority query through EventStore. This declaration does not invent missing dependency declarations.

`runtime.extensions` is empty because this executable does not declare an extension package. `Hexalith.Projects.Server.csproj` has `IsPackable=false`; it is not represented as a released package. Resource requests/limits, pub/sub subscriptions, workers, lifecycle hooks and external-provider bindings are not invented.

The existing `MapProjectsDomainServiceEndpoints` compatibility routes under `/api/v1/projects` retain their own tenant, resource and action checks. They include list/detail, operator diagnostics, context/setup, advisory resolution, proposal/confirmation and reference/setup/lifecycle mutations. They are outside this selected internal workload surface: no Platform ingress or Dapr invocation permission is declared for those routes. Query operations supported through `/query` keep their handler-side authorization. This declaration does not change endpoint implementations or authorization.

## Public preparation

With the qualified Builds CLI available, run from the Projects repository root, choosing a fresh output directory:

```text
hexalith-module prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace <fresh-absolute-directory> --output json
```

Use every emitted `buildPreparation.environment` value unchanged for local Debug builds. Never reuse Conversations' mapping for Projects or edit emitted imports. Source preparation can warn about Platform identity/release or catalog mismatches; it cannot qualify package mode.

## Remaining runtime prerequisites

Starting a composed runtime additionally requires an owner-supported binding for the actual Projects executable (`projects/domain`), dependency declarations including Folders, and scoped authentication configuration. The current owner executable consumes `Authentication:JwtBearer` (Authority, Issuer, Audience and RequireHttpsMetadata), workload receiver settings, the `Authentication:WorkloadJwtBearer` JWT contract, and outbound `Authentication:WorkloadIssuer` client credentials/token-endpoint settings (authority discovery comes from the workload JWT contract). The sidecar channel consumes `APP_API_TOKEN`; outbound issuer credentials use `Authentication:WorkloadIssuer:ClientSecret`. `ProjectChatBotAuthorityGateway` also requires `Authentication:BoundStreamRead:PrivateKey` for authoritative stream and owner-authority proofs, with its public trust enrolled by EventStore. The declaration records these logical secret names without values and supplies no credentials or fabricated extension package. Identity egress records bearer and workload OIDC metadata/JWKS and the explicit or discovered workload token endpoint as symbolic destinations; deployment must resolve them. Only when the entire `Authentication:WorkloadJwtBearer` section is absent does the SDK select `Authentication:JwtBearer`. A missing Authority value within a present dedicated workload section does not trigger fallback.

The configurable `Projects:StateStore:StateStoreName` binding does not cover every registered projection. `ProjectReadModelKeys.StoreName` fixes named Project reads to `statestore`; `ConversationStartSetupProjectionHandler.StoreName` fixes Conversation-start projections to `projects-conversation-start-setup`. Full runtime enrollment must supply both named components through an owner-supported binding. Changing the Projects option does not redirect them. The current v2 Dapr declaration requires a real configuration key; no key is invented for these fixed names. This remains a runtime prerequisite rather than a claim of complete runtime enrollment.

Gateway/store/runtime proof also needs authenticated EventStore and owner services, Dapr sidecars, enrolled authority grants, current tenant/resource evidence, and measured store end-state. Empty lifecycle hooks/recovery inventory do not establish recovery readiness. Source tests of this candidate are not a qualified release, deployed acceptance or Story completion.
