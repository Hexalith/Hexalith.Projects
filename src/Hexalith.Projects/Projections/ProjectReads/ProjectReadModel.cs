// <copyright file="ProjectReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectReads;

using System;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Projections.ProjectDetail;

/// <summary>Supported persisted detail and its authoritative delivery evidence.</summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="ProjectId">The owning aggregate.</param>
/// <param name="Detail">The existing pure fold, or null before creation.</param>
/// <param name="Sequence">The last applied aggregate sequence, including catalogued no-ops.</param>
/// <param name="GlobalPosition">The exact persisted global position, never an aggregate sequence.</param>
/// <param name="LastVerifiedAt">The last persisted event observation.</param>
/// <param name="Freshness">Whether the supplied history could be interpreted completely.</param>
public sealed record ProjectReadModel(
    string TenantId,
    string ProjectId,
    ProjectDetailItem? Detail,
    long Sequence,
    long GlobalPosition,
    DateTimeOffset LastVerifiedAt,
    EvidenceFreshnessState Freshness)
{
    /// <summary>Gets the highest observed failed sequence that recovery must cover. Null preserves conservative handling of older degraded records.</summary>
    public long? RecoveryFloor { get; init; }

    /// <summary>Gets a value indicating whether creation on this reserved stream was terminally abandoned (Story 7.1).</summary>
    public bool IsCreationAbandoned { get; init; }
}
