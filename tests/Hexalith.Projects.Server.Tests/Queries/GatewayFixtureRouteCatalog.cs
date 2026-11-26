namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
#if HEXALITH_SOURCE_FIXTURE
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.Queries;
#endif
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
#if HEXALITH_SOURCE_FIXTURE
using Hexalith.Folders.Contracts;
using Hexalith.Folders.Server.Queries;
#endif
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Queries;
using Hexalith.Projects.Contracts.Routing;
using Hexalith.Projects.Server;

/// <summary>Commits the routes exercised by an isolated gateway and domain service fixture.</summary>
/// <param name="tenant">The only tenant served by this fixture's committed routes.</param>
/// <param name="includeAdvisoryQueries">Whether to include the advisory slice and its Conversation owner hop.</param>
/// <param name="capabilities">The installed named projection capabilities for the Projects receiver.</param>
internal sealed class GatewayFixtureRouteCatalog(string tenant, bool includeAdvisoryQueries = false,
    Func<IReadOnlyList<RouteCatalogProjectionCapability>>? capabilities = null) : IRouteCatalogAuthority
{
    private readonly Lazy<CommittedRouteCatalogSnapshot> _snapshot = new(() => CreateSnapshot(tenant, includeAdvisoryQueries, capabilities?.Invoke()));

    private int _acquisitionCount;

    /// <summary>Gets the number of dispatch leases acquired by the real gateway components.</summary>
    internal int AcquisitionCount => Volatile.Read(ref _acquisitionCount);

    /// <inheritdoc/>
    public Task<RouteCatalogDispatchLease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = Interlocked.Increment(ref _acquisitionCount);
        return Task.FromResult(new RouteCatalogDispatchLease(_snapshot.Value, static () => Task.CompletedTask));
    }

    /// <inheritdoc/>
    public Task<RouteCatalogGenerationReadback> ReadbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommittedRouteCatalogSnapshot snapshot = _snapshot.Value;
        RouteCatalogEnvelope envelope = snapshot.Envelope;
        return Task.FromResult(new RouteCatalogGenerationReadback(
            snapshot.Epoch, envelope.Generation, snapshot.RootDigest,
            envelope.RouteFacetDigest, envelope.IdempotencyFacetDigest, envelope.Profile, [], true));
    }

    /// <inheritdoc/>
    public Task PrepareAsync(RouteCatalogActivation candidate, CancellationToken cancellationToken = default)
        => Task.FromException(Immutable());

    /// <inheritdoc/>
    public Task<RouteCatalogHostAttestation> ReadyAsync(string attemptId, CancellationToken cancellationToken = default)
        => Task.FromException<RouteCatalogHostAttestation>(Immutable());

    /// <inheritdoc/>
    public Task CommitAsync(string attemptId, CancellationToken cancellationToken = default)
        => Task.FromException(Immutable());

    /// <inheritdoc/>
    public Task RollbackAsync(RouteCatalogRollbackRequest request, CancellationToken cancellationToken = default)
        => Task.FromException(Immutable());

    /// <inheritdoc/>
    public Task ReconcileAsync(RouteCatalogQuiescenceRequest request, CancellationToken cancellationToken = default)
        => Task.FromException(Immutable());

    private static CommittedRouteCatalogSnapshot CreateSnapshot(string tenant, bool includeAdvisoryQueries,
        IReadOnlyList<RouteCatalogProjectionCapability>? capabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenant);
        List<(string RouteId, string Domain, string QueryType, Type Handler, Type Request, Type Result, bool Internal)> queries =
        [
            ("projects-list", "projects", ListProjectsQuery.QueryType, typeof(ListProjectsQueryHandler), typeof(ListProjectsQuery), typeof(ProjectListPage), false),
            ("projects-open", "projects", GetProjectQuery.QueryType, typeof(GetProjectQueryHandler), typeof(GetProjectQuery), typeof(GetProjectResponse), false),
#if HEXALITH_SOURCE_FIXTURE
            ("folders-owner-read", "folders", FolderOwnerReadQuery.QueryType, typeof(FolderOwnerReadQueryHandler), typeof(FolderOwnerReadQuery), typeof(FolderOwnerReadResult), true),
#endif
        ];
        if (includeAdvisoryQueries)
        {
            queries.Add(("projects-audit", "projects", GetProjectAuditQuery.QueryType, typeof(GetProjectAuditQueryHandler), typeof(GetProjectAuditQuery), typeof(ProjectAuditPage), false));
            queries.Add(("projects-task-status", "projects", GetProjectTaskStatusQuery.QueryType, typeof(GetProjectTaskStatusQueryHandler), typeof(GetProjectTaskStatusQuery), typeof(ProjectTaskStatusResponse), false));
            queries.Add(("projects-context", "projects", ProjectsServerModule.GetProjectContextQueryType, typeof(GetProjectContextQueryHandler), typeof(GetProjectContextQuery), typeof(ProjectContextReadResponse), false));
            queries.Add(("projects-start", "projects", ProjectsServerModule.GetConversationStartSetupQueryType, typeof(GetConversationStartSetupQueryHandler), typeof(GetConversationStartSetupQuery), typeof(ConversationStartSetupResponse), false));
            queries.Add(("projects-explain", "projects", ProjectsServerModule.ExplainContextSelectionQueryType, typeof(ExplainContextSelectionQueryHandler), typeof(ExplainContextSelectionQuery), typeof(ExplainContextSelectionResponse), false));
#if HEXALITH_SOURCE_FIXTURE
            queries.Add(("conversations-native-detail", "conversations", GetConversationQuery.QueryType, typeof(GetConversationDomainQueryHandler),
                typeof(GetConversationQuery).Assembly.GetType("Hexalith.Conversations.Contracts.Queries.GetConversationPayload")!, typeof(ConversationDetailResult), false));
#endif
            queries.Add(("projects-suggest-conversation", "projects", ResolveProjectFromConversationQuery.QueryType, typeof(ResolveProjectFromConversationQueryHandler), typeof(ResolveProjectFromConversationQuery), typeof(ProjectAdvisoryResolutionResponse), false));
            queries.Add(("projects-suggest-attachments", "projects", ResolveProjectFromAttachmentsQuery.QueryType, typeof(ResolveProjectFromAttachmentsQueryHandler), typeof(ResolveProjectFromAttachmentsQuery), typeof(ProjectAdvisoryResolutionResponse), false));
#if HEXALITH_SOURCE_FIXTURE
            queries.Add(("conversations-advisory-metadata", "conversations", GetConversationAdvisoryMetadataQuery.QueryType, typeof(GetConversationAdvisoryMetadataDomainQueryHandler), typeof(GetConversationAdvisoryMetadataQuery), typeof(ConversationAdvisoryMetadataResult), true));
#endif
        }

        List<RouteContractDeclaration> declarations = [];
        List<RouteCatalogEntry> bindings = [];
        List<RouteOperationContract> operations = [];
        foreach (var query in queries)
        {
            RouteContractDeclaration declaration = new(
                query.RouteId, "query", query.Domain, query.QueryType, null,
                "v1", null, "workload", EventStoreOperationIdentifiers.DomainServiceQuery, null, [],
                new RouteExecutionDeclaration(query.Handler.FullName!, "exact", [query.QueryType],
                    "domain-service", "computed", [], RequiresDelegatedContext: query.Internal));
            bool nativeOwnerQuery = query.Domain == "conversations" && !query.Internal;
            RouteOperationContract operation = RouteOperationSchema.Create(
                declaration, query.Request, query.Result,
                query.Internal || nativeOwnerQuery ? "internal" : "public",
                nativeOwnerQuery ? "UI-only" : "agent-eligible", JsonSerializerOptions.Web);
            declarations.Add(declaration);
            operations.Add(operation);
            bindings.Add(new RouteCatalogEntry(declaration.RouteId, 1,
                RouteCatalogCodec.ComputeDeclarationDigest(declaration), query.Domain, "query", "v1",
                declaration.CredentialKind, declaration.Operation, false, tenant, operation.Digest));
        }

        foreach (RouteContractDeclaration route in ProjectsRuntimeCatalogDeclarations.Routes.Where(route => route.Kind != "query"))
        {
            RouteOperationContract operation = ProjectsRuntimeCatalogDeclarations.Operations.Single(operation => operation.RouteId == route.RouteId);
            declarations.Add(route);
            operations.Add(operation);
            bindings.Add(new(route.RouteId, 1, RouteCatalogCodec.ComputeDeclarationDigest(route), "projects",
                ProjectsRuntimeCatalogDeclarations.ReceiverMethods[route.RouteId], "v1", route.CredentialKind, route.Operation,
                false, route.Kind == "projection" ? "*" : tenant, operation.Digest));
        }
        var policies = ProjectsRuntimeCatalogDeclarations.Commands.Select(command => DevelopmentIdempotencyBindings.Bind(command, 1)).ToArray();
        RouteCatalogEnvelope envelope = RouteCatalogCodec.Seal(new RouteCatalogEnvelope(
            3, 1, "Development", false, declarations, bindings, policies, string.Empty, string.Empty, operations, []));
        RouteCatalogAuxiliaryOperation[] auxiliary =
        [
            new("projects", "admin/operational-index-metadata", EventStoreOperationIdentifiers.DomainServiceMetadata, "projects"),
            new("projects", "project/rebuild/v1", EventStoreOperationIdentifiers.DomainServiceProject, "projects"),
            new("projects", "replay-state", EventStoreWorkloadOperations.DomainServiceReplayState, "projects"),
            new("eventstore", "api/v1/queries/owner-authority/workload", EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery, "projects"),
        ];
        return new CommittedRouteCatalogSnapshot(
            RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope), 1, auxiliary, capabilities);
    }

    private static RouteCatalogUnavailableException Immutable()
        => new("The isolated gateway fixture's committed query catalog is immutable.");
}
