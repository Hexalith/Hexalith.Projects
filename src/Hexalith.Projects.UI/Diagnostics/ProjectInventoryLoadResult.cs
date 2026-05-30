// <copyright file="ProjectInventoryLoadResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Rendering;

/// <summary>
/// Safe load result for the Projects inventory.
/// </summary>
/// <param name="Rows">The metadata-only inventory rows.</param>
/// <param name="Feedback">The safe feedback when loading failed.</param>
/// <param name="TenantScope">The server-derived tenant scope display label.</param>
public sealed record ProjectInventoryLoadResult(
    IReadOnlyList<ProjectInventoryRowProjection> Rows,
    ProjectConsoleFeedback? Feedback,
    string TenantScope)
{
    /// <summary>Creates a successful inventory result.</summary>
    /// <param name="rows">The inventory rows.</param>
    /// <param name="tenantScope">The server-derived tenant scope label.</param>
    /// <returns>A successful load result.</returns>
    public static ProjectInventoryLoadResult FromRows(
        IReadOnlyList<ProjectInventoryRowProjection> rows,
        string tenantScope = "server-derived tenant")
        => new(rows, null, tenantScope);

    /// <summary>Creates a failed inventory result.</summary>
    /// <param name="feedback">The safe feedback.</param>
    /// <param name="tenantScope">The server-derived tenant scope label.</param>
    /// <returns>A failed load result.</returns>
    public static ProjectInventoryLoadResult FromFeedback(
        ProjectConsoleFeedback feedback,
        string tenantScope = "server-derived tenant")
        => new([], feedback, tenantScope);
}
