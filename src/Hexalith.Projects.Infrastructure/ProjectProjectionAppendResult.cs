// <copyright file="ProjectProjectionAppendResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

/// <summary>
/// Metadata-only result from durable project projection processing.
/// </summary>
/// <param name="Status">The append status.</param>
/// <param name="TenantId">The tenant id from the event envelope.</param>
/// <param name="MessageId">The event message id.</param>
/// <param name="Sequence">The projection sequence/watermark, backed by EventStore global position.</param>
public sealed record ProjectProjectionAppendResult(
    ProjectProjectionAppendStatus Status,
    string TenantId,
    string MessageId,
    long Sequence);
