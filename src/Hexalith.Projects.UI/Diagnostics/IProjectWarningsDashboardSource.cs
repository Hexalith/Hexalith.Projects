// <copyright file="IProjectWarningsDashboardSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Source for the metadata-only warnings queue and operational dashboard.
/// </summary>
public interface IProjectWarningsDashboardSource
{
    /// <summary>Loads tenant-scoped visible projects and bounded warning diagnostics.</summary>
    /// <param name="lifecycle">Optional lifecycle filter delegated to the list endpoint.</param>
    /// <param name="cancellationToken">Cancellation token propagated to generated-client queries.</param>
    /// <returns>Warnings queue and dashboard load result.</returns>
    Task<ProjectWarningsDashboardLoadResult> LoadAsync(ProjectLifecycle? lifecycle, CancellationToken cancellationToken);
}
