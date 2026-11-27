// <copyright file="ProvisionedFolderRecord.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

using System;

/// <summary>Shared-store record of a Folder provisioned via <c>FolderAggregate</c> for Story 7.1.</summary>
/// <param name="TenantId">The owning Tenant identifier.</param>
/// <param name="FolderId">The deterministic derived Folder identifier.</param>
/// <param name="DisplayName">The validated display name (1..160 characters).</param>
/// <param name="ReceiptId">The deterministic Folders provisioning receipt identifier.</param>
/// <param name="OriginalActorId">The sole-manager actor identifier.</param>
/// <param name="TaskId">The Durable Task identifier.</param>
/// <param name="StepKey">The canonical step key (<c>{TaskId}/step/{n}</c>).</param>
/// <param name="AdmissionOperation">The versioned admission operation.</param>
/// <param name="RequestSha256">The normalized provisioning request digest.</param>
/// <param name="OwnerVersion">The Folders stream version (3 = create + read grant + manage grant).</param>
/// <param name="InheritOrganizationPermissions">Whether Tenant-default read inheritance is enabled (false).</param>
/// <param name="WorkloadHasFolderAuthority">Whether Projects' workload identity holds Folder authority (false).</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
public sealed record ProvisionedFolderRecord(
    string TenantId,
    string FolderId,
    string DisplayName,
    string ReceiptId,
    string OriginalActorId,
    string TaskId,
    string StepKey,
    string AdmissionOperation,
    string RequestSha256,
    long OwnerVersion,
    bool InheritOrganizationPermissions,
    bool WorkloadHasFolderAuthority,
    DateTimeOffset CreatedAt);
