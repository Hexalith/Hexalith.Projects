// <copyright file="IProjectResolutionTraceSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

/// <summary>
/// Runs explicit, compute-on-demand Project resolution trace queries for the operator workbench.
/// </summary>
public interface IProjectResolutionTraceSource
{
    /// <summary>
    /// Executes the requested trace query.
    /// </summary>
    /// <param name="request">The safe trace request.</param>
    /// <param name="cancellationToken">Cancellation token propagated to the generated client.</param>
    /// <returns>The trace result or safe feedback.</returns>
    Task<ProjectResolutionTraceLoadResult> LoadTraceAsync(
        ProjectResolutionTraceRequest request,
        CancellationToken cancellationToken);
}
