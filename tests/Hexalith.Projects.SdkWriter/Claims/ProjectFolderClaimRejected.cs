// <copyright file="ProjectFolderClaimRejected.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

using Hexalith.EventStore.Contracts.Events;

/// <summary>Metadata-only rejection event emitted by <see cref="ProjectFolderClaimAggregate"/>.</summary>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="FolderId">The Folder identifier.</param>
/// <param name="ReasonCode">The nondisclosing Safe Metadata reason code.</param>
/// <param name="TaskId">Optional task identifier.</param>
/// <param name="TaskStepKey">Optional task-step key.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public sealed record ProjectFolderClaimRejected(
    string TenantId,
    string FolderId,
    string ReasonCode,
    string? TaskId,
    string? TaskStepKey,
    string CorrelationId) : IRejectionEvent;
