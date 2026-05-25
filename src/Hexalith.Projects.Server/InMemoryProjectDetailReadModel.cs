// <copyright file="InMemoryProjectDetailReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>
/// A minimal, thread-safe in-memory <see cref="IProjectDetailReadModel"/> backed by a
/// <see cref="ProjectDetailProjection"/> (Story 1.4 tracer bullet). It folds <c>ProjectCreated</c>
/// events into the deterministic projection and serves the tenant-scoped minimal read. Reads are
/// tenant-scoped (the projection's tenant guard and canonical keying ensure a foreign-tenant event can
/// never satisfy a query for another tenant). Story 1.9 replaces this with the Dapr-backed projection
/// store; the projection itself is the reusable, rebuildable read model.
/// </summary>
public sealed class InMemoryProjectDetailReadModel : IProjectDetailReadModel
{
    private readonly object _gate = new();
    private ProjectDetailProjection _projection = ProjectDetailProjection.Empty;
    private long _sequence;

    /// <summary>
    /// Folds a single <c>ProjectCreated</c> event into the projection (the dispatch path the Workers /
    /// projection subscriber drives in Story 1.9). Pure projection update; tenant-guarded by the
    /// envelope/event tenant agreement enforced inside the projection.
    /// </summary>
    /// <param name="dispatchTenantId">The dispatch (envelope) tenant the event was delivered for.</param>
    /// <param name="created">The project-created event.</param>
    public void Project(string dispatchTenantId, ProjectCreated created)
    {
        ArgumentNullException.ThrowIfNull(created);

        lock (_gate)
        {
            long sequence = ++_sequence;
            _projection = _projection.Apply([new ProjectProjectionEnvelope(dispatchTenantId, sequence, created)]);
        }
    }

    /// <inheritdoc/>
    public Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default)
    {
        ProjectDetailProjection snapshot;
        lock (_gate)
        {
            snapshot = _projection;
        }

        return Task.FromResult(snapshot.Get(authoritativeTenantId, projectId));
    }
}
