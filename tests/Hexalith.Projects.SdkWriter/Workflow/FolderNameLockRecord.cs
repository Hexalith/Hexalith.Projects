// <copyright file="FolderNameLockRecord.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

/// <summary>Shared-store create-only name lock enforcing A-2 same-name collision rejection per Tenant.</summary>
/// <param name="TenantId">The owning Tenant identifier.</param>
/// <param name="NormalizedDisplayName">The normalized trimmed display name.</param>
/// <param name="FolderId">The Folder identifier holding the name.</param>
/// <param name="TaskId">The Durable Task identifier that created the Folder.</param>
/// <param name="StepKey">The canonical task-step key.</param>
public sealed record FolderNameLockRecord(
    string TenantId,
    string NormalizedDisplayName,
    string FolderId,
    string TaskId,
    string StepKey);
