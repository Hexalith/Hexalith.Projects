// <copyright file="ProjectDiagnosticLoadResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.UI.Rendering;

/// <summary>
/// Safe diagnostic load result for composed shell views.
/// </summary>
/// <param name="Diagnostic">The diagnostic DTO when available.</param>
/// <param name="Feedback">The safe feedback when the DTO is unavailable.</param>
/// <param name="TenantScope">The server-derived tenant scope label when available.</param>
/// <param name="Mode">The console mode label.</param>
public sealed record ProjectDiagnosticLoadResult(
    ProjectOperatorDiagnostic? Diagnostic,
    ProjectConsoleFeedback? Feedback,
    string? TenantScope,
    string Mode)
{
    /// <summary>Creates a successful result.</summary>
    public static ProjectDiagnosticLoadResult FromDiagnostic(
        ProjectOperatorDiagnostic diagnostic,
        string? tenantScope,
        string mode)
        => new(diagnostic, null, tenantScope, mode);

    /// <summary>Creates a feedback-only result.</summary>
    public static ProjectDiagnosticLoadResult FromFeedback(ProjectConsoleFeedback feedback)
        => new(null, feedback, null, ProjectConsoleModes.ReadOnly);
}

