// <copyright file="TenantAccessAuthorizationResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System;
using System.Text.Json.Serialization;

/// <summary>Result of tenant-access authorization with metadata-only diagnostic evidence.</summary>
public sealed record TenantAccessAuthorizationResult(
    TenantAccessOutcome Outcome,
    string Code,
    string? TenantId,
    string? ProjectionWatermark,
    DateTimeOffset? LastEventTimestamp,
    TimeSpan? ProjectionAge,
    TenantProjectionFreshnessStatus FreshnessStatus,
    string Source)
{
    /// <summary>Gets a value indicating whether the tenant-access decision allowed the operation.</summary>
    [JsonIgnore]
    public bool IsAllowed => Outcome == TenantAccessOutcome.Allowed;
}
