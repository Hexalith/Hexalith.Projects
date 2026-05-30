// <copyright file="IProjectProjectionStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReferenceIndex;

/// <summary>
/// Durable project projection store shared by Server reads and Workers projection processing.
/// </summary>
public interface IProjectProjectionStore
{
    /// <summary>Appends a persisted EventStore event to the projection journal.</summary>
    Task<ProjectProjectionAppendResult> AppendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);

    /// <summary>Lists tenant-scoped project rows.</summary>
    Task<IReadOnlyList<ProjectListItem>> ListAsync(
        string tenantId,
        ProjectLifecycle? lifecycleFilter,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a tenant-scoped project detail row.</summary>
    Task<ProjectDetailItem?> GetDetailAsync(string tenantId, string projectId, CancellationToken cancellationToken = default);

    /// <summary>Lists tenant-scoped reference-index rows matching presented folder/file identifiers.</summary>
    Task<IReadOnlyList<ProjectReferenceIndexItem>> ListReferencesByReferenceAsync(
        string tenantId,
        IReadOnlyCollection<string> folderIds,
        IReadOnlyCollection<string> fileReferenceIds,
        CancellationToken cancellationToken = default);

    /// <summary>Gets readiness evidence for a tenant projection journal.</summary>
    Task<ProjectProjectionReadiness> GetReadinessAsync(string tenantId, CancellationToken cancellationToken = default);
}
