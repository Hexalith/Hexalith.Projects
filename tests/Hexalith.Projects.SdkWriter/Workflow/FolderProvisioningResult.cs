// <copyright file="FolderProvisioningResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

/// <summary>Result of provisioning, looking up, or validating a Folder for Story 7.1.</summary>
/// <param name="Outcome">The provisioning/validation outcome.</param>
/// <param name="FolderId">The Folder identifier when available.</param>
/// <param name="DisplayName">The Folder's own display name when available.</param>
/// <param name="ReceiptId">The durable Folders provisioning receipt identifier (null for pre-existing supplied Folders).</param>
/// <param name="OwnerVersion">The Folders owner stream version.</param>
/// <param name="SoleManagerActorId">The verified sole-manager actor identifier.</param>
/// <param name="InheritOrganizationPermissions">Whether Tenant-default read inheritance is enabled (must be <see langword="false"/>).</param>
/// <param name="WorkloadHasFolderAuthority">Whether Projects' workload identity holds Folder authority (must be <see langword="false"/>).</param>
/// <param name="ReasonCode">Optional Safe Metadata reason code.</param>
public sealed record FolderProvisioningResult(
    FolderProvisioningOutcome Outcome,
    string? FolderId,
    string? DisplayName,
    string? ReceiptId,
    long OwnerVersion,
    string? SoleManagerActorId,
    bool InheritOrganizationPermissions,
    bool WorkloadHasFolderAuthority,
    string? ReasonCode = null);
