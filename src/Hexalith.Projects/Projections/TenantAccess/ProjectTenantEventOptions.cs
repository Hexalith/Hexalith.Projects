// <copyright file="ProjectTenantEventOptions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

/// <summary>Options for consuming Tenants events into the Projects tenant-access projection.</summary>
public sealed class ProjectTenantEventOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Projects:TenantEvents";

    /// <summary>Gets or sets the host that owns projection writes during Server-to-Workers migration.</summary>
    public ProjectTenantEventProjectionWriter ProjectionWriter { get; set; } = ProjectTenantEventProjectionWriter.Workers;
}
