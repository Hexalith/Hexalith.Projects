// <copyright file="IProjectOperatorDiagnosticSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

/// <summary>
/// Reads the Story 5.2 operator diagnostic model for shell primitives.
/// </summary>
public interface IProjectOperatorDiagnosticSource
{
    /// <summary>Reads project diagnostics.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The safe load result.</returns>
    Task<ProjectDiagnosticLoadResult> GetProjectDiagnosticsAsync(
        string projectId,
        CancellationToken cancellationToken);
}

