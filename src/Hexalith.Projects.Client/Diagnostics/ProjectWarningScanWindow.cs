// <copyright file="ProjectWarningScanWindow.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Diagnostics;

using Hexalith.Projects.Client.Generated;

/// <summary>
/// Selects the deterministic visible-project window used for warning diagnostics.
/// </summary>
public static class ProjectWarningScanWindow
{
    /// <summary>The maximum number of visible projects diagnosed by warning surfaces.</summary>
    public const int ProjectLimit = 25;

    /// <summary>
    /// Orders visible projects by ordinal project identifier and returns the shared diagnostic window.
    /// </summary>
    /// <param name="projects">The visible, server-authorized project rows.</param>
    /// <returns>The deterministic warning diagnostic window.</returns>
    public static IReadOnlyList<ProjectListItem> Select(IEnumerable<ProjectListItem> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        return projects
            .OrderBy(static project => project.ProjectId, StringComparer.Ordinal)
            .Take(ProjectLimit)
            .ToArray();
    }
}
