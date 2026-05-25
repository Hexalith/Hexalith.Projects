// <copyright file="ProjectQueryTenantFilter.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>Filters query-side records to the authoritative tenant before any response is built.</summary>
public static class ProjectQueryTenantFilter
{
    /// <summary>Returns the detail item only when it belongs to the authoritative tenant.</summary>
    public static ProjectDetailItem? Filter(string? authoritativeTenantId, ProjectDetailItem? item)
        => !string.IsNullOrWhiteSpace(authoritativeTenantId)
            && item is not null
            && string.Equals(item.TenantId, authoritativeTenantId.Trim(), StringComparison.Ordinal)
            ? item
            : null;

    /// <summary>Filters a detail collection to the authoritative tenant.</summary>
    public static IReadOnlyList<ProjectDetailItem> FilterDetails(
        string? authoritativeTenantId,
        IEnumerable<ProjectDetailItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (string.IsNullOrWhiteSpace(authoritativeTenantId))
        {
            return [];
        }

        string tenant = authoritativeTenantId.Trim();
        return items.Where(item => string.Equals(item.TenantId, tenant, StringComparison.Ordinal)).ToArray();
    }

    /// <summary>Filters a list collection to the authoritative tenant.</summary>
    public static IReadOnlyList<ProjectListItem> FilterList(
        string? authoritativeTenantId,
        IEnumerable<ProjectListItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (string.IsNullOrWhiteSpace(authoritativeTenantId))
        {
            return [];
        }

        string tenant = authoritativeTenantId.Trim();
        return items.Where(item => string.Equals(item.TenantId, tenant, StringComparison.Ordinal)).ToArray();
    }
}
