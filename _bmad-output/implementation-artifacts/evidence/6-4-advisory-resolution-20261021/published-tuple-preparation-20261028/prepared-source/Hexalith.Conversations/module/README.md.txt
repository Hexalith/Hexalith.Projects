# Conversations Platform enrollment prerequisite source slice

For Story 6.4's local published-tuple preparation on 2026-10-28, the tool
manifest now prepares `hexalith.builds.module.cli` **4.38.2**, matching the
prepared direct Platform range floor. The published tool identifies Builds
`2574bf2f3da5367d8801f0d7661d66155e5d90b0`; its official qualification,
signed archive and exact source were verified. This local pin prepares the
consumer for that floor. It does not record or qualify a new Platform identity,
adopt a package composition, change a .NET dependency, or refresh the historical
observations below. Platform still needs its exact Builds gitlink recorded,
integration qualification and publication before consumer adoption. Current
original-checkout public package preparation remains subject to those guards.

The approved 2026-10-27 remediation corrects active source/build/configuration
consumers and prepares the pending [Platform owner handoff](platform-handoff.md).
It uses only mapped Debug builds and isolated composition/test stores under the
user-approved pre-edit AppHost exception. No product runtime was launched.
The Epic 16 hold remains ACTIVE; Story 16.1 stays draft/backlog, entry null and
trust empty. Source corrections confer no owner acceptance, live qualification,
deployment, host retirement, release or push authority.

Current entry investigation at `26298b31eb4d362f88e008246ac3acb160128330` is
recorded in [the 2026-10-27 report](../_bmad-output/implementation-artifacts/story-16-1-entry-investigation-2026-10-27.md).
Both native adoption formats still fail with six PASS and ten FAIL rows.
All six Story 16.1 commands now fail `SOURCE_DRIFT` on `.gitattributes` before
entry evaluation; the earlier BLOCKED observations below remain historical.
CI preparation exists, but accepted CLI 4.35.2 rejects its `prepare-build`
command with HXC001. Source implementation does not establish accepted-artifact
qualification or successful CI execution.

Fresh observations are retained in
[the remediation validation directory](validation/epic-16-enrollment-remediation-2026-10-27/).
The native scanner reads tracked sources from the working checkout at baseline
`dc5793db17fd6974200e80eb2b853187bc5f1669`; new untracked source remains outside
its inventory. Owner integration must rerun both formats on the intended reviewed
tracked candidate. The historical observations below keep their original
candidate and scope; they do not describe a newly qualified runtime.

The accepted direct Platform tool is `hexalith.builds.module.cli` **4.35.2**, with
roll-forward disabled. Its restored identity is
`4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`. The root tool manifest copies
only that accepted entry. No dependency or gitlink was updated.

Retained first-implementation [source-check summary](validation/epic-16-enrollment-remediation-2026-10-27/summary.json):
all fourteen targeted finding identities are absent from both native reports;
the reports remain FAIL with 6 PASS and 10 excluded FAIL rows, and no unexpected
findings. Full offline Server tests pass 1369/1369; both in-process population
checks pass. These results do not close the pending CI mapping integration gate
or any owner/live qualification gate.

Review corrections and focused checks are recorded in
[the additive review observation](validation/epic-16-enrollment-remediation-2026-10-27/review-fix-1/README.md).
The earlier observations retain their original bytes and source scope. A separate
complete implementation-source digest snapshot includes the three untracked C#
additions; it does not change native scanner coverage or claim tracked-candidate
qualification. The later [attribute-placement correction](validation/epic-16-enrollment-remediation-2026-10-27/review-fix-2/README.md)
restores the accepted root attributes bytes and scopes raw-byte protection inside
the observation directory; no root-binding refresh is required for that rule.
The completed parent source-slice verification is recorded in the
[remediation spec](../_bmad-output/implementation-artifacts/spec-epic-16-enrollment-adoption-remediation.md#verification).
Those retained test results are not new executions against the later candidate.

## Mapped validation workflow

1. A qualified Platform/Builds caller must select the workspace/dependencies and
   mode and materialize its mapping with Builds'
   `SourceMappingMaterializer.WriteAsync`. It must pass the complete generated
   `SourceMappingMaterializer.Environment` to every restore/build/test child.
   `validate` does not resolve mappings. Current Builds source implements the
   runtime-free `prepare-build` CLI, but accepted 4.35.2 lacks it; using the
   public command requires a separately qualified published successor.
   Do not set dependency roots/modes by hand or probe sibling layouts.
2. In source mode, build the affected test projects individually in Debug under
   that environment, then invoke their built native xUnit assemblies with
   `-class`/`-method`. The prerequisite slice used the public materializer with a
   synthetic direct-root mapping, not an ordinary accepted Platform run.
   `PlatformEnrollmentMappingTest` also evaluates all eight consumer project
   files in source/Debug and package/Release, rejects missing/mismatched inputs,
   and checks competing layouts. Package evaluation compiles no Release assets.
3. Keep package/Release and deployed acceptance separate, with exact tool,
   mapping/catalog, source/package/image and provider identities. The pending
   [Platform handoff](platform-handoff.md) lists those owner gates.
4. Current root `.github/workflows/ci.yml` and Builds
   `.github/workflows/domain-ci.yml` invoke the producer's
   `Github/prepare-module-build` action before affected restores/builds. That
   action calls `prepare-build --mode package` and exports verified mapping
   imports. Accepted 4.35.2 rejects this command; qualify the exact successor
   artifact, accepted pin and Platform/catalog tuple before claiming CI
   integration. Standalone package/property evaluation and source tests do not
   prove a successful CI build. The earlier prerequisite slice did not change
   workflows; subsequent commits added this preparation path.

## Declaration and runtime ownership

`hexalith.module-manifest.v2.json` declares the Conversations domain server:
module `conversations`, server `domain`, Dapr app ID `conversation`, Platform
resource `conversations`, readiness `/ready`, and direct server dependencies
`eventstore`, `tenants`, `memories`. The root already declares those repositories
along with Platform, Builds and McpCli. The declaration uses logical `state` and
`events` roles and injected configuration keys. Shared `state/state` component
classification remains `authoritative-restore`; the Conversations and Tenants
projection inventories remain derived (`rebuild-only`). Recovery hooks and
qualified restore/rebuild demonstrations are pending; none are invented here.

The protected HTTP surface admits the `eventstore` workload for the exact SDK
operational POST route catalog, including `/project/v2/reconcile`. Operation
policies match `EventStoreDomainServicePolicies` and
`EventStoreWorkloadOperations`. The SDK requires both the receiver's Dapr app
channel token (`APP_API_TOKEN`, matching `DaprAppChannelToken.ConfigurationKey`)
and a valid short-lived workload JWT with the receiving audience,
caller and operation grants. The declaration does not turn caller inventory,
ACLs or transport identity into authorization. Platform must supply the
executable/image and gateway mappings, workload/JWT configuration, injected
state bindings, ACL/mTLS configuration, and private credentials. No secrets are
stored here. Dedicated `Authentication:WorkloadJwtBearer:Authority` discovery
and JWKS egress are declared independently of the public JWT issuer. Probe paths
remain SDK-owned.

The Tenants feed is declarative at `/tenants/events`, with logical
`tenants-events` and `tenants-dead-letter` topics. Its physical feed topic is
`system.tenants.events`, following Platform's enrolled Parties pattern;
Platform must inject the matching `Tenants:PubSubName` and `Tenants:TopicName`.
The enrolled `AddConversationTenantAccess(builder.Configuration)` path passes the
SDK's `configureOptions` delegate and binds both options from those required
inputs. Missing/blank delivery inputs or a declarative-subscription value other
than true fail composition. The parameterless compatibility overload retains SDK
defaults for existing direct callers; production uses the configuration overload.
Unsupported `Tenants:Enabled`, `Tenants:ServiceName` and `Tenants:CommandApiAppId`
knobs have no current consumer and are not declared.

The declaration also names `Dapr:DeclarativeSubscriptions` through the
`dapr-declarative-subscriptions-true` configuration reference. Platform must
resolve this input to **true**. In the current SDK that exact boolean switch
suppresses `/dapr/subscribe` discovery while retaining the configured event
POST route. Enrolled composition requires that injected true value; this source
slice provides no deployed runtime configuration. Future qualification
must prove protected `/tenants/events` delivery at the injected pub/sub/topic,
dead-letter routing and absent discovery, plus the production reconciliation
route; source declarations alone do not prove those effects.

The enrolled source composition now binds Tenants pub/sub/topic inputs via the
existing SDK delegate, requires `Dapr:DeclarativeSubscriptions=true`, and calls
SDK `AddEventStoreDataProtection(IConfiguration, "Hexalith.Conversations")`.
Read-model reads/writes, batch/recovery scopes and native proof binding share one
immutable configured component name. Required missing/blank enrollment inputs
fail before store/delivery use. Legacy direct-construction APIs retain the SDK
component default for compatibility; the configured production path cannot select
it when required configuration is absent. The cursor purpose is unchanged.

Isolated tests inspect protected POST route metadata and absent discovery,
execute SDK key-ring persistence and cursor operations against a bounded SDK
Dapr fake, and assert configured projection-store contents. They do not
qualify live delivery, persistent key-ring restart behavior, workload middleware,
provider parity or multi-replica tenant access. Explicit ephemeral SDK test mode
is not production restart evidence. Those owner gates remain pending in the
handoff, and the single-replica/restart warning remains unchanged.

## Retained additive refresh (2026-10-25)

The [v2 review package](../_bmad-output/planning-artifacts/epic-16-adoption-v2/README.md)
independently binds current sources and the exact preserved v1 predecessor.
Its read-only structural check is:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/verify_epic_16_adoption_v2.py --repository .
```

The five native commands below were rerun with accepted **4.35.2** into the fresh
[v2 observation directory](validation/epic-16-adoption-v2/results.json). Restore
and version exit **0**. Ordinary declaration validation and both adoption modes
still exit **1** (`UsageOrManifest`). The published binary still reports both
HXP002 `APP_API_TOKEN` fields. The current direct Builds schema accepts the real
SDK token key and the unchanged manifest; that source-schema PASS does not
establish compatibility of the published accepted binary. The token-format
entry blocker remains **BLOCKED** pending an accepted compatible tool and new
native observations. No alias is introduced.

The [JSON report](validation/epic-16-adoption-v2/adoption.json) and
[text report](validation/epic-16-adoption-v2/adoption-text.txt) retain **24 rows:
six PASS and eighteen FAIL**. These include declaration failure, fourteen
dependency-selector findings and the same three Dapr findings. In addition to
the historical eleven selector paths below, the current report names
`Directory.Packages.props`,
`tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`
and `tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props`.
All failures remain entry blockers; no runtime code was changed to suppress them.

Exact commands, exits, separate stdout/stderr hashes and timestamps are retained.
[provenance.json](validation/epic-16-adoption-v2/provenance.json) records source
HEAD, observation-time working-tree changes, all twelve unchanged direct root
gitlinks/checkouts, and hashes of 1,047 tracked supported-source candidates.
The scanner filters those candidates by its documented supported types, 1 MiB
limit and frozen-host Dapr exclusion. It does not scan new untracked files or
certify unsupported/generated constructs. The captured working tree precedes
the completed v2 package; owner integration must rerun both adoption modes on
the intended reviewed/tracked candidate. These observations establish no
source/package/hosted parity, AD-5/AD-6 or SM-2 qualification.

The historical v1 checkers remain unchanged. They truthfully fail current source
drift; the adoption fault suite now reproduces their exact historical root/tool
and reference bytes without Git history or initialized dependencies.

## Historical 4.35.1 static validation

Run from the Conversations Git root:

```bash
dotnet tool restore
dotnet tool run hexalith-module --version
dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json
dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json --adoption
dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json --adoption --output json
python3 _bmad/scripts/verify_epic_16_adoption.py --repository .
```

These are the recorded v1 commands and outcomes. The current root manifest runs
4.35.2; historical checker reproduction uses pinned portable test fixtures.

Restore and version checks exited **0**. Ordinary declaration validation and
both adoption modes exited **1** (`UsageOrManifest`). The correct SDK
`APP_API_TOKEN` key is rejected by accepted 4.35.1 with HXP002 at
`modules[0].integration.secrets[1].configurationKey` and
`modules[0].runtime.extensions[0].configurationInputs[12].key`: its key format
permits alphanumerics and colon segments, but no underscore. Fixing that
upstream accepted schema/tool compatibility is **BLOCKED**; no alias or tool-pin
change is introduced. Exact command, exit and output
bindings are in [validation/results.json](validation/results.json); the complete
[readable report](validation/adoption-text.txt) and [JSON report](validation/adoption.json)
retain all 21 rows: six PASS and fifteen FAIL (the declaration plus fourteen
source findings). The native ordinary/adoption failures remain failures.

Eleven dependency-selector failures identify `Directory.Build.props`, the
AppHost, Client, Contracts, Server and Domain project files, and AppHost,
Contracts, Integration, Server and Domain test project files. Three
`dapr-subscriptions` failures identify:

- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs`
- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs`
- `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs`

Every exact path and reason remains in the reports. The tool pin,
workspace, direct references, gitlinks, absent nested references and hosting
inventory passed the accepted tool's bounded static checks. Those rows establish
no runtime proof. The accepted 4.35.1 scanner has documented false-positive
limits; no failure is waived or converted to PASS here.

The scanner reads tracked files. During this preparation the additive files are
untracked because staging is prohibited. No source was hidden, renamed, removed,
or excluded to change scanning. The owner must rerun both adoption modes against
the intended reviewed/tracked candidate and review unsupported/generated
constructs. Stored reports are a working-tree snapshot, not qualification of a
later checkout or certification of all new Python files.

## Remaining work and owner procedure

Review the additive [Epic 16 v2 adoption package](../_bmad-output/planning-artifacts/epic-16-adoption-v2/README.md),
with the [v1 package](../_bmad-output/planning-artifacts/epic-16-adoption-v1/README.md) retained as history,
and its read-only checker. A structural PASS has `entryReadiness=BLOCKED` and
four false authority flags. Retained scanner FAIL rows are entry blockers;
they do not make the package structurally inconsistent merely by being recorded.

1. Conversations and Platform owners review the declaration against source,
   agree executable/image/gateway mappings, physical topic injection and scoped
   state/configuration/identity inputs. Active source consumers now use mapped
   project/package pairs and required configured Dapr names; enrolled Tenants,
   declarative delivery and SDK Data Protection wiring are implemented. Review
   the ten retained excluded scanner failures and new/untracked source, qualify
   the producer CI mapping path, and resolve those owner gates separately.
   Preserve public contracts, receiver authorization and durable semantics.
2. Qualify source/Debug, package/Release and hosted operation with exact runtime,
   provider, catalog, tool and mapping identities. Establish the AD-5 operational
   envelope, operator/recovery-deputy coverage, recovery targets/drills, resource
   bounds and any Release-owned scoped/expiring waiver. No run/debug/deploy
   commands were executed for this preparation.
3. Complete [sm-2-checklist.md](sm-2-checklist.md) through observed human evidence:
   real active-checkout breakpoint and edit, direct-only dependency checks,
   readiness, stop/cleanup and Story 3.11 owner acceptance. It remains unverified.
4. The Epic 16 successor schema, generator and measured scenario verifiers exist;
   the earlier six Story 16.1 observations returned BLOCKED with
   ENTRY_QUALIFICATION_PENDING. The current candidate instead fails SOURCE_DRIFT
   on `.gitattributes`, before checking the still-null entry. Resolve source
   binding through a separately scoped additive refresh. Runtime execution and publication require the
   separate trusted owner entry decision after prerequisites pass. Keep all
   eighteen scenarios, thirty-two requirements, graph edges and rollback/final-
   record obligations. The raw-byte rule now lives in the observation directory's
   own `.gitattributes`; that remediation restored the accepted root bytes.
   A later committed mapping-test attributes rule changed the bound root file.
   The old review-fix-1 note remains historical; the current refresh requirement
   follows the new measured drift and does not authorize overwriting accepted
   bindings or removing the legitimate attributes rule.
5. Keep both AppHosts in [hosting-inventory.json](hosting-inventory.json) frozen.
   The inventory names tracked consumers, including solution/test/CI and fixture
   tooling; immutable evidence also records these paths as historical references.
   `DomainModuleAuthoringTemplateValidationTest.cs` actively reads both hosts;
   `check_package_refresh_environment.py` actively reads the main host.
   `PackageEnvironmentAuthorityV18ValidationTest.cs` is a historical Git-blob
   reference, not an active filesystem dependency.
   No module-owned Aspire or ServiceDefaults production project exists. Neither
   AppHost is packable or publishable. Baseline owner with Architect/Runtime must
   explicitly decide exemption or relocation before Story 16.3. Retirement also
   requires producer adoption, owner-reviewed source/package/deployed parity,
   consumer migration and applicable EventStore AD-22 exact-subject removal
   authority (or an explicit owner-reviewed not-applicable disposition).

Scoped evidence status is **not-applicable to runtime publication**: preparation
publishes no runtime receipts or final records. Historical authority publishers
and the Story 16.1 lifecycle are not resumed by this package.
