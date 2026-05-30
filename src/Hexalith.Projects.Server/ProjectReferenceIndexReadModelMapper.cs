// <copyright file="ProjectReferenceIndexReadModelMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReferenceIndex;

internal static class ProjectReferenceIndexReadModelMapper
{
    public static IReadOnlyList<ProjectReferenceIndexCandidateRow> ToCandidateRows(
        string authoritativeTenantId,
        IEnumerable<ProjectListItem> listRows,
        IEnumerable<ProjectReferenceIndexItem> references)
    {
        ArgumentNullException.ThrowIfNull(listRows);
        ArgumentNullException.ThrowIfNull(references);

        IReadOnlyList<ProjectListItem> tenantRows = ProjectQueryTenantFilter.FilterList(authoritativeTenantId, listRows);
        Dictionary<string, ProjectListItem> projects = tenantRows
            .GroupBy(static row => row.ProjectId, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

        return references
            .Where(reference => !string.IsNullOrWhiteSpace(reference.ProjectId))
            .Where(reference => projects.ContainsKey(reference.ProjectId))
            .GroupBy(reference => reference.ProjectId, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                ProjectListItem project = projects[group.Key];
                ProjectReferenceIndexItem[] matchedReferences = group
                    .OrderBy(static reference => reference.ReferenceKind, StringComparer.Ordinal)
                    .ThenBy(static reference => reference.ReferenceId, StringComparer.Ordinal)
                    .ToArray();
                return new ProjectReferenceIndexCandidateRow(
                    project.TenantId,
                    project.ProjectId,
                    project.Name,
                    project.Lifecycle,
                    matchedReferences);
            })
            .ToArray();
    }
}
