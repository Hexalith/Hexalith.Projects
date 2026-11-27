// <copyright file="ProjectFolderClaimReleased.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

using System;

using Hexalith.EventStore.Contracts.Events;

/// <summary>Event emitted when an active AD-35 Folder claim is released after abandonment fencing.</summary>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="FolderId">The Folder identifier.</param>
/// <param name="ProjectId">The Project whose claim was released.</param>
/// <param name="ReleasedGeneration">The exact generation that was released.</param>
/// <param name="TaskId">The Durable Task identifier.</param>
/// <param name="TaskStepKey">The deterministic release task-step key.</param>
/// <param name="ReasonCode">The Safe Metadata reason code.</param>
/// <param name="ActorPrincipalId">The actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="OccurredAt">The UTC timestamp.</param>
public sealed record ProjectFolderClaimReleased(
    string TenantId,
    string FolderId,
    string ProjectId,
    long ReleasedGeneration,
    string TaskId,
    string TaskStepKey,
    string ReasonCode,
    string ActorPrincipalId,
    string CorrelationId,
    DateTimeOffset OccurredAt) : IEventPayload;
