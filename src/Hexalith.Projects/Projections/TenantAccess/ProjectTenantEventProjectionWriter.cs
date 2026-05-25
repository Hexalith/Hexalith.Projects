// <copyright file="ProjectTenantEventProjectionWriter.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

/// <summary>Selects the host allowed to write tenant-access projections from Tenants events.</summary>
public enum ProjectTenantEventProjectionWriter
{
    Disabled,
    Server,
    Workers,
}
