// <copyright file="IProjectDetailSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

/// <summary>
/// Generated-client backed source for the read-only Project detail inspector.
/// </summary>
public interface IProjectDetailSource
{
    /// <summary>Loads Project detail using canonical generated query clients.</summary>
    /// <param name="projectId">The Project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The detail load result.</returns>
    Task<ProjectDetailLoadResult> GetProjectDetailAsync(string projectId, CancellationToken cancellationToken);
}
