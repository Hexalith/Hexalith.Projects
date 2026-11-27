// <copyright file="FolderStepReceiptPayload.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

/// <summary>Durable receipt persisted at Step 1 (<c>Receipts[0]</c>) before advancing to claim acquisition.</summary>
/// <param name="FolderId">The provisioned or validated Folder identifier.</param>
/// <param name="FolderDisplayName">The Folder's own display name.</param>
/// <param name="ProvisioningReceiptId">The durable Folders provisioning receipt identifier, or null for supplied Folders.</param>
/// <param name="IsSupplied">Whether the Folder was supplied by the actor rather than newly provisioned.</param>
public sealed record FolderStepReceiptPayload(
    string FolderId,
    string? FolderDisplayName,
    string? ProvisioningReceiptId,
    bool IsSupplied);
