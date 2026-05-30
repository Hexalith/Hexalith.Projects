// <copyright file="ProjectAuditTimelineLoadResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.UI.Rendering;

/// <summary>
/// Safe load result for bounded audit timeline reloads.
/// </summary>
public sealed record ProjectAuditTimelineLoadResult(
    IReadOnlyList<ProjectOperatorAuditTimelineItem> Rows,
    ProjectOperatorFreshnessMetadata? Freshness,
    ProjectConsoleFeedback? Feedback)
{
    /// <summary>Creates a successful audit timeline result.</summary>
    public static ProjectAuditTimelineLoadResult FromRows(
        IReadOnlyList<ProjectOperatorAuditTimelineItem> rows,
        ProjectOperatorFreshnessMetadata freshness)
        => new(rows, freshness, null);

    /// <summary>Creates a feedback-only audit timeline result.</summary>
    public static ProjectAuditTimelineLoadResult FromFeedback(ProjectConsoleFeedback feedback)
        => new([], null, feedback);
}
