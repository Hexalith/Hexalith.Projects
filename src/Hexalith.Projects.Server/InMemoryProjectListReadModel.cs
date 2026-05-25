// <copyright file="InMemoryProjectListReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>
/// Thread-safe in-memory <see cref="IProjectListReadModel"/> backed by <see cref="ProjectListProjection"/>.
/// </summary>
public sealed class InMemoryProjectListReadModel : IProjectListReadModel
{
    private readonly object _gate = new();
    private ProjectListProjection _projection = ProjectListProjection.Empty;
    private long _sequence;

    /// <summary>Folds a single <c>ProjectCreated</c> event into the list projection.</summary>
    /// <param name="dispatchTenantId">The dispatch tenant the event was delivered for.</param>
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
    public Task<IReadOnlyList<ProjectListItem>> ListAsync(
        string authoritativeTenantId,
        ProjectLifecycle? lifecycleFilter,
        CancellationToken cancellationToken = default)
    {
        ProjectListProjection snapshot;
        lock (_gate)
        {
            snapshot = _projection;
        }

        return Task.FromResult(snapshot.List(authoritativeTenantId, lifecycleFilter));
    }
}
