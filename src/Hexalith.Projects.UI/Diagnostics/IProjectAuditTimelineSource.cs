// <copyright file="IProjectAuditTimelineSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

/// <summary>
/// Reads bounded audit rows through the existing operator diagnostic endpoint.
/// </summary>
public interface IProjectAuditTimelineSource
{
    /// <summary>Reads audit rows for one Project.</summary>
    Task<ProjectAuditTimelineLoadResult> GetAuditTimelineAsync(
        string projectId,
        int? auditLimit,
        CancellationToken cancellationToken);
}
