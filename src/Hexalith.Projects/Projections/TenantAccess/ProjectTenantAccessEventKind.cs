// <copyright file="ProjectTenantAccessEventKind.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System.Text.Json.Serialization;

/// <summary>Normalized Tenants event kinds consumed by the Projects tenant-access projection.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectTenantAccessEventKind>))]
public enum ProjectTenantAccessEventKind
{
    TenantCreated,
    TenantUpdated,
    TenantDisabled,
    TenantEnabled,
    UserAddedToTenant,
    UserRemovedFromTenant,
    UserRoleChanged,
    TenantConfigurationSet,
    TenantConfigurationRemoved,
}
