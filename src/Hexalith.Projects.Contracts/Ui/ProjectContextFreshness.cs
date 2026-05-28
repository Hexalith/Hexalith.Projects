// <copyright file="ProjectContextFreshness.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Assembly-level freshness signal for <c>ProjectContext</c> (AR-9, Story 3.1). Re-exposes the Story 1.6
/// <c>TenantProjectionFreshnessStatus</c> through the shared rendering vocabulary so Story 3.4
/// (Refresh) can surface degraded reads without changing the policy shape.
/// </summary>
/// <remarks>
/// Name-based JSON is mandatory (see <see cref="JsonStringEnumConverter{TEnum}"/>): the wire shape is
/// the enum member NAME, never the integer ordinal (NFR-6 schema tolerance).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectContextFreshness>))]
public enum ProjectContextFreshness
{
    /// <summary>The assembly inputs are fresh — tenant projection is current.</summary>
    [ProjectionBadge(BadgeSlot.Success)]
    Fresh,

    /// <summary>The assembly inputs are stale — read still allowed but flagged for the caller.</summary>
    [ProjectionBadge(BadgeSlot.Warning)]
    Stale,

    /// <summary>The assembly inputs are unavailable (tenant projection rebuilding or absent).</summary>
    [ProjectionBadge(BadgeSlot.Danger)]
    Unavailable,

    /// <summary>The assembly inputs freshness is unknown (default / no signal observed).</summary>
    [ProjectionBadge(BadgeSlot.Neutral)]
    Unknown,
}
