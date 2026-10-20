# Closing commands

These are exact argv rendered with shell quoting, in the recorded working directory. Debug source references were used; dependency versions were not changed. Raw records and XML are in the archive.

## review-client-build-06

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore`

```sh
dotnet build tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false
```

Result: exit 0

## review-server-build-01

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore`

```sh
dotnet build tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false
```

Result: exit 0

## review-domainservice-build-01

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore`

```sh
dotnet build tests/Hexalith.EventStore.DomainService.Tests/Hexalith.EventStore.DomainService.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false
```

Result: exit 0

## review-conversations-build-02

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.Conversations`

```sh
dotnet build tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false
```

Result: exit 0

## review-tenants-build-01

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.Tenants`

```sh
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false
```

Result: exit 0

## final-folders-core-build

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.Folders`

```sh
dotnet build tests/Hexalith.Folders.Tests/Hexalith.Folders.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons -p:HexalithTenantsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Tenants -p:NuGetAudit=false
```

Result: exit 0

## final-folders-server-build

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.Folders`

```sh
dotnet build tests/Hexalith.Folders.Server.Tests/Hexalith.Folders.Server.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons -p:HexalithTenantsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Tenants -p:NuGetAudit=false
```

Result: exit 0

## final-projects-build

Working directory: `/home/administrator/projects/hexalith/projects`

```sh
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons -p:HexalithTenantsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Tenants -p:NuGetAudit=false
```

Result: exit 0

## final-eventstore-client

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore`

```sh
dotnet tests/Hexalith.EventStore.Client.Tests/bin/Debug/net10.0/Hexalith.EventStore.Client.Tests.dll -class Hexalith.EventStore.Client.Tests.Streams.SourcePublicationNamespaceTests -class Hexalith.EventStore.Client.Tests.Streams.SourceNamespaceSnapshotReaderTests -class Hexalith.EventStore.Client.Tests.Streams.SourcePublicationFeedTests -class Hexalith.EventStore.Client.Tests.Streams.DaprSourcePublicationIndexStoreTests -class Hexalith.EventStore.Client.Tests.Streams.AuthoritativeEventStreamReaderTests -class Hexalith.EventStore.Client.Tests.Projections.DaprReadModelBatchTests -result-xml /tmp/6-4-owner-delivery/final-eventstore-client.xml
```

Result: exit 0; {"total": 362, "passed": 362, "failed": 0, "skipped": 0, "errors": 0}

## final-eventstore-server

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore`

```sh
dotnet tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll -class Hexalith.EventStore.Server.Tests.Streams.NativeSourceMetadataReaderTests -class Hexalith.EventStore.Server.Tests.Streams.NativeSourcePrefixTests -class Hexalith.EventStore.Server.Tests.Controllers.ProjectionCurrentAuthorityTests -class Hexalith.EventStore.Server.Tests.Controllers.BoundOwnerQueryAuthorityTests -class Hexalith.EventStore.Server.Tests.Controllers.BoundOwnerQueryControllerTests -class Hexalith.EventStore.Server.Tests.Queries.QueryRouterSourceGuardTests -class Hexalith.EventStore.Server.Tests.Actors.AggregateActorGetEventsTests -result-xml /tmp/6-4-owner-delivery/final-eventstore-server.xml
```

Result: exit 0; {"total": 104, "passed": 104, "failed": 0, "skipped": 0, "errors": 0}

## final-eventstore-routes

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore`

```sh
dotnet tests/Hexalith.EventStore.DomainService.Tests/bin/Debug/net10.0/Hexalith.EventStore.DomainService.Tests.dll -class Hexalith.EventStore.DomainService.Tests.EventStoreDomainServiceExtensionsTests -xml /tmp/6-4-owner-delivery/final-eventstore-routes.xml
```

Result: exit 0; {"total": 92, "passed": 92, "failed": 0, "skipped": 0, "errors": 0}

## review-domainservice-focused-01

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore`

```sh
dotnet tests/Hexalith.EventStore.DomainService.Tests/bin/Debug/net10.0/Hexalith.EventStore.DomainService.Tests.dll -class Hexalith.EventStore.DomainService.Tests.QueryAuthorityCorrectionTests -result-xml /tmp/6-4-owner-delivery/review-domainservice-focused-01.xml
```

Result: exit 0; {"total": 9, "passed": 9, "failed": 0, "skipped": 0, "errors": 0}

## final-tenants-authority

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.Tenants`

```sh
dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Queries.TenantReadAuthorityQueryHandlerTests -class Hexalith.Tenants.Server.Tests.Queries.TenantOwnerAuthorityQueryHandlerTests -result-xml /tmp/6-4-owner-delivery/final-tenants-authority.xml
```

Result: exit 0; {"total": 19, "passed": 19, "failed": 0, "skipped": 0, "errors": 0}

## final-conversations-full

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.Conversations`

```sh
dotnet tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0/Hexalith.Conversations.Server.Tests.dll -result-xml /tmp/6-4-owner-delivery/final-conversations-full.xml
```

Result: exit 0; {"total": 1013, "passed": 1013, "failed": 0, "skipped": 0, "errors": 0}

## final-folders-core-tests

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.Folders`

```sh
dotnet tests/Hexalith.Folders.Tests/bin/Debug/net10.0/Hexalith.Folders.Tests.dll -class Hexalith.Folders.Tests.Aggregates.Folder.FolderWorkspaceFileMutationAggregateTests -class Hexalith.Folders.Tests.Aggregates.Folder.FolderWorkspaceFileMutationServiceTests -class Hexalith.Folders.Tests.Authorization.EffectivePermissionsRevocationFreshnessTests -class Hexalith.Folders.Tests.Authorization.EffectivePermissionsReadModelFreshnessTests -class Hexalith.Folders.Tests.Authorization.EffectivePermissionsCrossPrincipalTests -class Hexalith.Folders.Tests.Authorization.EffectivePermissionsMetadataLeakageTests -result-xml /tmp/6-4-owner-delivery/final-folders-core-tests.xml
```

Result: exit 0; {"total": 93, "passed": 93, "failed": 0, "skipped": 0, "errors": 0}

## final-folders-server-full

Working directory: `/home/administrator/projects/hexalith/projects/references/Hexalith.Folders`

```sh
dotnet tests/Hexalith.Folders.Server.Tests/bin/Debug/net10.0/Hexalith.Folders.Server.Tests.dll -result-xml /tmp/6-4-owner-delivery/final-folders-server-full.xml
```

Result: exit 0; {"total": 1055, "passed": 1055, "failed": 0, "skipped": 0, "errors": 0}

## final-projects-owner-integration

Working directory: `/home/administrator/projects/hexalith/projects`

```sh
dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests -class Hexalith.Projects.Server.Tests.Queries.ProjectQueryEnvelopePrincipalBindingTests -class Hexalith.Projects.Server.Tests.Queries.SupportedProjectProjectionTests -class Hexalith.Projects.Server.Tests.Queries.GetProjectContextQueryHandlerTests -xml /tmp/6-4-owner-delivery/final-projects-owner-integration.xml
```

Result: exit 0; {"total": 90, "passed": 89, "failed": 0, "skipped": 1, "errors": 0}
