// <copyright file="ProjectsQueryRouteCatalog.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Authentication;

using System.Text.Json;

using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Server.Queries;
using Hexalith.EventStore.ServiceDefaults.Routing;

/// <summary>Supplies the immutable committed query authority the real EventStore query controller requires.</summary>
/// <remarks>
/// Only the Projects <c>GetProject</c> query for one tenant is committed, as a public UI-only operation.
/// Every mutating catalog operation is refused.
/// </remarks>
/// <param name="tenant">The only tenant the committed query route resolves.</param>
internal sealed class ProjectsQueryRouteCatalog(string tenant) : IRouteCatalogAuthority
{
    private const string QueryType = "GetProject";

    private readonly CommittedRouteCatalogSnapshot _snapshot = CreateSnapshot(tenant);

    private int _acquisitionCount;

    /// <summary>Gets the number of committed dispatch leases the controller acquired.</summary>
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
            _snapshot.Epoch,
            envelope.Generation,
            _snapshot.RootDigest,
            envelope.RouteFacetDigest,
            envelope.IdempotencyFacetDigest,
            envelope.Profile,
            [],
            true));
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

    private static CommittedRouteCatalogSnapshot CreateSnapshot(string tenant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenant);
        RouteContractDeclaration declaration = new(
            "projects-get-project-query",
            "query",
            "projects",
            QueryType,
            "projects",
            "v1",
            null,
            "workload",
            EventStoreOperationIdentifiers.DomainServiceQuery,
            null,
            [],
            new RouteExecutionDeclaration(
                typeof(QueryRouter).FullName!,
                "exact",
                [QueryType],
                "domain-service",
                "projection",
                []));
        RouteOperationContract operation = RouteOperationSchema.Create(
            declaration,
            typeof(SubmitQueryRequest),
            typeof(SubmitQueryResponse),
            "public",
            "UI-only",
            JsonSerializerOptions.Web);
        RouteCatalogEntry binding = new(
            declaration.RouteId,
            1,
            RouteCatalogCodec.ComputeDeclarationDigest(declaration),
            "projects",
            "query",
            "v1",
            declaration.CredentialKind,
            declaration.Operation,
            false,
            tenant,
            operation.Digest);
        RouteCatalogEnvelope envelope = RouteCatalogCodec.Seal(new RouteCatalogEnvelope(
            3,
            1,
            "Development",
            false,
            [declaration],
            [binding],
            [],
            string.Empty,
            string.Empty,
            [operation],
            []));
        return new CommittedRouteCatalogSnapshot(
            RouteCatalogCodec.Encode(envelope),
            RouteCatalogCodec.ComputeRootDigest(envelope),
            1);
    }

    private static RouteCatalogUnavailableException Immutable()
        => new("The Projects gateway contract fixture accepts only its immutable committed query route.");
}
