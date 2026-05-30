// <copyright file="ProjectDetailLoadResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.UI.Rendering;

/// <summary>
/// Safe load result for the read-only Project detail inspector.
/// </summary>
/// <param name="Detail">The metadata-only detail DTO when available.</param>
/// <param name="Feedback">The safe blocking feedback when base detail cannot load.</param>
/// <param name="DiagnosticFeedback">The safe non-blocking diagnostic feedback when audit/reference enrichment cannot load.</param>
/// <param name="TenantScope">The server-derived tenant scope display label.</param>
/// <param name="Mode">The console mode.</param>
public sealed record ProjectDetailLoadResult(
    ProjectOperatorDiagnostic? Detail,
    ProjectConsoleFeedback? Feedback,
    ProjectConsoleFeedback? DiagnosticFeedback,
    string TenantScope,
    string Mode)
{
    /// <summary>Creates a successful detail result.</summary>
    /// <param name="detail">The detail DTO.</param>
    /// <param name="diagnosticFeedback">Optional non-blocking diagnostic feedback.</param>
    /// <param name="tenantScope">The server-derived tenant scope label.</param>
    /// <param name="mode">The console mode.</param>
    /// <returns>A successful detail load result.</returns>
    public static ProjectDetailLoadResult FromDetail(
        ProjectOperatorDiagnostic detail,
        ProjectConsoleFeedback? diagnosticFeedback = null,
        string tenantScope = "server-derived tenant",
        string mode = ProjectConsoleModes.ReadOnly)
        => new(detail, null, diagnosticFeedback, tenantScope, mode);

    /// <summary>Creates a failed detail result.</summary>
    /// <param name="feedback">The safe blocking feedback.</param>
    /// <returns>A failed detail result.</returns>
    public static ProjectDetailLoadResult FromFeedback(ProjectConsoleFeedback feedback)
        => new(null, feedback, null, "server-derived tenant", ProjectConsoleModes.ReadOnly);
}
