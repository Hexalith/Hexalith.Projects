// <copyright file="IProjectDetailSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Queries;

/// <summary>
/// Generated-client backed source for the read-only Project detail inspector.
/// </summary>
public interface IProjectDetailSource
{
    /// <summary>Reads the requested page of committed safe Project facts.</summary>
    Task<ProjectReadResult<ProjectAuditPage>> ReadAuditAsync(string projectId, GetProjectAuditQuery query, CancellationToken cancellationToken);

    /// <summary>Requests explicit separately authorized descriptive or quarantine inspection.</summary>
    Task<ProjectDetailLoadResult> InspectAsync(string projectId, bool descriptive, bool quarantined, CancellationToken cancellationToken);

    /// <summary>Loads context bound to the named Conversation; old callers remain safely unbound.</summary>
    Task<ProjectDetailLoadResult> InspectAsync(string projectId, bool descriptive, bool quarantined,
        string? conversationId, CancellationToken cancellationToken)
        => InspectAsync(projectId, descriptive, quarantined, cancellationToken);

    /// <summary>Loads Project detail using canonical generated query clients.</summary>
    /// <param name="projectId">The Project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The detail load result.</returns>
    Task<ProjectDetailLoadResult> GetProjectDetailAsync(string projectId, CancellationToken cancellationToken);
}
