// <copyright file="ProjectWarningsDashboardLoadResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Rendering;

/// <summary>
/// Safe load result for the Projects warning queue and operational dashboard.
/// </summary>
/// <param name="InventoryRows">The loaded visible Project rows.</param>
/// <param name="QueueItems">The metadata-only warning queue items.</param>
/// <param name="Dashboard">The aggregate dashboard counts.</param>
/// <param name="Feedback">The safe feedback when the base load failed.</param>
/// <param name="TenantScope">The server-derived tenant scope display label.</param>
public sealed record ProjectWarningsDashboardLoadResult(
    IReadOnlyList<ProjectInventoryRowProjection> InventoryRows,
    IReadOnlyList<ProjectWarningQueueItemProjection> QueueItems,
    ProjectOperationalDashboardProjection Dashboard,
    ProjectConsoleFeedback? Feedback,
    string TenantScope)
{
    /// <summary>Creates a successful warnings/dashboard result.</summary>
    public static ProjectWarningsDashboardLoadResult FromRows(
        IReadOnlyList<ProjectInventoryRowProjection> inventoryRows,
        IReadOnlyList<ProjectWarningQueueItemProjection> queueItems,
        ProjectOperationalDashboardProjection dashboard,
        string tenantScope = "server-derived tenant")
        => new(inventoryRows, queueItems, dashboard, null, tenantScope);

    /// <summary>Creates a failed warnings/dashboard result.</summary>
    public static ProjectWarningsDashboardLoadResult FromFeedback(
        ProjectConsoleFeedback feedback,
        string tenantScope = "server-derived tenant")
        => new([], [], new ProjectOperationalDashboardProjection { TenantScope = tenantScope }, feedback, tenantScope);
}
