// <copyright file="CreateBoundProject.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

/// <summary>
/// Story 7.1 Folder-first creation command that commits a single <c>ProjectCreated</c> event
/// carrying an already-authorized, AD-35 claim-fenced Folder binding.
/// </summary>
/// <param name="Name">The Project name.</param>
/// <param name="Description">Optional safe description.</param>
/// <param name="SetupMetadata">Optional safe setup-metadata reference.</param>
/// <param name="FolderId">The exclusively claimed authorized Folder identifier.</param>
/// <param name="FolderDisplayName">The optional safe metadata display name of the bound Folder.</param>
/// <param name="FolderProvisioningReceiptId">The optional Folders provisioning receipt identifier.</param>
/// <param name="FolderClaimGeneration">The winning AD-35 claim generation fencing this commit.</param>
/// <param name="TaskStepKey">The deterministic Durable Task step key.</param>
public sealed record CreateBoundProject(
    string Name,
    string? Description,
    string? SetupMetadata,
    string FolderId,
    string? FolderDisplayName,
    string? FolderProvisioningReceiptId,
    long FolderClaimGeneration,
    string TaskStepKey);
