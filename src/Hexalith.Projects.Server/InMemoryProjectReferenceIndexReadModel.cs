// <copyright file="InMemoryProjectReferenceIndexReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReferenceIndex;

/// <summary>Thread-safe in-memory reverse reference-index read model for pre-runtime hosts and tests.</summary>
public sealed class InMemoryProjectReferenceIndexReadModel : IProjectReferenceIndexReadModel
{
    private readonly object _gate = new();
    private ProjectListProjection _listProjection = ProjectListProjection.Empty;
    private ProjectReferenceIndexProjection _referenceProjection = ProjectReferenceIndexProjection.Empty;
    private long _sequence;

    /// <summary>Folds a single project event into the list and reference-index projections.</summary>
    /// <param name="dispatchTenantId">The dispatch tenant the event was delivered for.</param>
    /// <param name="projectEvent">The project event.</param>
    public void Project(string dispatchTenantId, IProjectEvent projectEvent)
    {
        ArgumentNullException.ThrowIfNull(projectEvent);

        lock (_gate)
        {
            long sequence = ++_sequence;
            ProjectProjectionEnvelope envelope = new(dispatchTenantId, sequence, projectEvent);
            _listProjection = _listProjection.Apply([envelope]);
            _referenceProjection = _referenceProjection.Apply([envelope]);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ProjectReferenceIndexCandidateRow>> ListByReferenceAsync(
        string authoritativeTenantId,
        IReadOnlyCollection<string> folderIds,
        IReadOnlyCollection<string> fileReferenceIds,
        CancellationToken cancellationToken = default)
    {
        ProjectListProjection listSnapshot;
        ProjectReferenceIndexProjection referenceSnapshot;
        lock (_gate)
        {
            listSnapshot = _listProjection;
            referenceSnapshot = _referenceProjection;
        }

        IReadOnlyList<ProjectListItem> listRows = listSnapshot.List(authoritativeTenantId, lifecycleFilter: null);
        IReadOnlyList<ProjectReferenceIndexItem> references = referenceSnapshot.ListByReference(authoritativeTenantId, folderIds, fileReferenceIds);
        return Task.FromResult(ProjectReferenceIndexReadModelMapper.ToCandidateRows(authoritativeTenantId, listRows, references));
    }
}
