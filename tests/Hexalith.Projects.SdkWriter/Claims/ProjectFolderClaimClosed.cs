// <copyright file="ProjectFolderClaimClosed.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

using System;

using Hexalith.EventStore.Contracts.Events;

/// <summary>Event emitted when an AD-35 Folder claim key is closed/quarantined.</summary>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="FolderId">The Folder identifier.</param>
/// <param name="ReasonCode">The Safe Metadata reason code.</param>
/// <param name="TaskId">Optional task identifier.</param>
/// <param name="TaskStepKey">Optional task-step key.</param>
/// <param name="ActorPrincipalId">The actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="OccurredAt">The UTC timestamp.</param>
public sealed record ProjectFolderClaimClosed(
    string TenantId,
    string FolderId,
    string ReasonCode,
    string? TaskId,
    string? TaskStepKey,
    string ActorPrincipalId,
    string CorrelationId,
    DateTimeOffset OccurredAt) : IEventPayload;
