// <copyright file="ProjectFolderClaimAcquired.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

using System;

using Hexalith.EventStore.Contracts.Events;

/// <summary>Event emitted when an exclusive AD-35 Folder claim is acquired.</summary>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="FolderId">The claimed Folder identifier.</param>
/// <param name="ProjectId">The holding Project identifier.</param>
/// <param name="Generation">The claim stream version at acquisition.</param>
/// <param name="TaskId">The Durable Task identifier.</param>
/// <param name="TaskStepKey">The deterministic task-step key.</param>
/// <param name="ActorPrincipalId">The original actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="ReceiptId">The durable receipt identifier.</param>
/// <param name="OccurredAt">The UTC timestamp.</param>
public sealed record ProjectFolderClaimAcquired(
    string TenantId,
    string FolderId,
    string ProjectId,
    long Generation,
    string TaskId,
    string TaskStepKey,
    string ActorPrincipalId,
    string CorrelationId,
    string ReceiptId,
    DateTimeOffset OccurredAt) : IEventPayload;
