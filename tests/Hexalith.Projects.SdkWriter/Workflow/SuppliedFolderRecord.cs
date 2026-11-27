// <copyright file="SuppliedFolderRecord.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

/// <summary>Shared-store record describing an existing Tenant-scoped supplied Folder for testing.</summary>
/// <param name="TenantId">The owning Tenant identifier.</param>
/// <param name="FolderId">The Folder identifier.</param>
/// <param name="DisplayName">The Folder's own display name (preserved on bound creation).</param>
/// <param name="ManagerActorId">The actor holding Folder manage authority.</param>
/// <param name="IsOrphanedOrReserved">Whether the Folder is orphaned or reserved (<c>NeedsAttention</c>).</param>
/// <param name="InheritOrganizationPermissions">Whether Tenant-default read inheritance is enabled.</param>
/// <param name="WorkloadHasFolderAuthority">Whether Projects' workload identity holds Folder authority.</param>
/// <param name="OwnerVersion">The current Folder stream version.</param>
public sealed record SuppliedFolderRecord(
    string TenantId,
    string FolderId,
    string DisplayName,
    string ManagerActorId,
    bool IsOrphanedOrReserved = false,
    bool InheritOrganizationPermissions = false,
    bool WorkloadHasFolderAuthority = false,
    long OwnerVersion = 3);
