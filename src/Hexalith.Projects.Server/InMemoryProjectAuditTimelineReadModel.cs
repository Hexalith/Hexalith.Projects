// <copyright file="InMemoryProjectAuditTimelineReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Projections.ProjectAuditTimeline;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>Thread-safe in-memory audit timeline read model for pre-runtime hosts and tests.</summary>
public sealed class InMemoryProjectAuditTimelineReadModel : IProjectAuditTimelineReadModel
{
    private readonly object _gate = new();
    private ProjectAuditTimelineProjection _projection = ProjectAuditTimelineProjection.Empty;
    private long _sequence;

    /// <summary>Folds a single project event into the audit timeline projection.</summary>
    /// <param name="dispatchTenantId">The dispatch tenant the event was delivered for.</param>
    /// <param name="projectEvent">The project event.</param>
    public void Project(string dispatchTenantId, IProjectEvent projectEvent)
    {
        ArgumentNullException.ThrowIfNull(projectEvent);

        lock (_gate)
        {
            long sequence = ++_sequence;
            _projection = _projection.Apply([new ProjectProjectionEnvelope(dispatchTenantId, sequence, projectEvent)]);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ProjectAuditTimelineItem>> ListAsync(
        string authoritativeTenantId,
        string? projectId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        ProjectAuditTimelineProjection snapshot;
        lock (_gate)
        {
            snapshot = _projection;
        }

        return Task.FromResult(snapshot.List(authoritativeTenantId, projectId, limit));
    }
}
