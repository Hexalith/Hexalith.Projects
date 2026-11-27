// <copyright file="ProjectReadModelKeys.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectReads;

using System;

/// <summary>Collision-safe logical keys for supported list/open read models.</summary>
public static class ProjectReadModelKeys
{
    /// <summary>The existing configured platform state-store component.</summary>
    public const string StoreName = "statestore";

    /// <summary>Returns the tenant's supported index key.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <returns>The collision-safe tenant index key.</returns>
    public static string Index(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return $"projects:reads:v1:{tenantId.Length}:{tenantId}:index";
    }

    /// <summary>Returns the supported detail key.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="projectId">The owning project identifier.</param>
    /// <returns>The collision-safe project detail key.</returns>
    public static string Detail(string tenantId, string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        return $"projects:reads:v1:{tenantId.Length}:{tenantId}:{projectId.Length}:{projectId}";
    }
}
