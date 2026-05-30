// <copyright file="IProjectInventorySource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Generated-client backed source for the Projects inventory.
/// </summary>
public interface IProjectInventorySource
{
    /// <summary>Loads metadata-only inventory rows.</summary>
    /// <param name="lifecycle">The optional lifecycle filter backed by the list query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The inventory load result.</returns>
    Task<ProjectInventoryLoadResult> ListProjectsAsync(ProjectLifecycle? lifecycle, CancellationToken cancellationToken);
}
