#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.Queries;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Folders.Contracts;
using Hexalith.Folders.Server.Queries;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Queries;

/// <summary>Commits only the query routes exercised by an isolated gateway fixture.</summary>
/// <param name="tenant">The only tenant served by this fixture's committed routes.</param>
/// <param name="includeAdvisoryQueries">Whether to include the advisory slice and its Conversation owner hop.</param>
internal sealed class GatewayFixtureRouteCatalog(string tenant, bool includeAdvisoryQueries = false) : IRouteCatalogAuthority
{
    private readonly CommittedRouteCatalogSnapshot _snapshot = CreateSnapshot(tenant, includeAdvisoryQueries);

    private int _acquisitionCount;

    /// <summary>Gets the number of dispatch leases acquired by the real gateway components.</summary>
    internal int AcquisitionCount => Volatile.Read(ref _acquisitionCount);

    /// <inheritdoc/>
    public Task<RouteCatalogDispatchLease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = Interlocked.Increment(ref _acquisitionCount);
        return Task.FromResult(new RouteCatalogDispatchLease(_snapshot, static () => Task.CompletedTask));
    }

    /// <inheritdoc/>
    public Task<RouteCatalogGenerationReadback> ReadbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RouteCatalogEnvelope envelope = _snapshot.Envelope;
        return Task.FromResult(new RouteCatalogGenerationReadback(
            _snapshot.Epoch, envelope.Generation, _snapshot.RootDigest,
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

    private static CommittedRouteCatalogSnapshot CreateSnapshot(string tenant, bool includeAdvisoryQueries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenant);
        List<(string RouteId, string Domain, string QueryType, Type Handler, Type Request, Type Result, bool Internal)> queries =
        [
            ("projects-list", "projects", ListProjectsQuery.QueryType, typeof(ListProjectsQueryHandler), typeof(ListProjectsQuery), typeof(ProjectListPage), false),
            ("projects-open", "projects", GetProjectQuery.QueryType, typeof(GetProjectQueryHandler), typeof(GetProjectQuery), typeof(GetProjectResponse), false),
            ("folders-owner-read", "folders", FolderOwnerReadQuery.QueryType, typeof(FolderOwnerReadQueryHandler), typeof(FolderOwnerReadQuery), typeof(FolderOwnerReadResult), true),
        ];
        if (includeAdvisoryQueries)
        {
            queries.Add(("projects-suggest-conversation", "projects", ResolveProjectFromConversationQuery.QueryType, typeof(ResolveProjectFromConversationQueryHandler), typeof(ResolveProjectFromConversationQuery), typeof(ProjectAdvisoryResolutionResponse), false));
            queries.Add(("projects-suggest-attachments", "projects", ResolveProjectFromAttachmentsQuery.QueryType, typeof(ResolveProjectFromAttachmentsQueryHandler), typeof(ResolveProjectFromAttachmentsQuery), typeof(ProjectAdvisoryResolutionResponse), false));
            queries.Add(("conversations-advisory-metadata", "conversations", GetConversationAdvisoryMetadataQuery.QueryType, typeof(GetConversationAdvisoryMetadataDomainQueryHandler), typeof(GetConversationAdvisoryMetadataQuery), typeof(ConversationAdvisoryMetadataResult), true));
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
            RouteOperationContract operation = RouteOperationSchema.Create(
                declaration, query.Request, query.Result,
                query.Internal ? "internal" : "public", "agent-eligible", JsonSerializerOptions.Web);
            declarations.Add(declaration);
            operations.Add(operation);
            bindings.Add(new RouteCatalogEntry(declaration.RouteId, 1,
                RouteCatalogCodec.ComputeDeclarationDigest(declaration), query.Domain, "query", "v1",
                declaration.CredentialKind, declaration.Operation, false, tenant, operation.Digest));
        }

        RouteCatalogEnvelope envelope = RouteCatalogCodec.Seal(new RouteCatalogEnvelope(
            3, 1, "Development", false, declarations, bindings, [], string.Empty, string.Empty, operations, []));
        return new CommittedRouteCatalogSnapshot(
            RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope), 1);
    }

    private static RouteCatalogUnavailableException Immutable()
        => new("The isolated gateway fixture's committed query catalog is immutable.");
}
#endif
