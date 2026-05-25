// <copyright file="ReferenceState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Inclusion / reference state of a sibling reference (conversation, folder, file, memory) within a
/// Project (AR-18, UX-DR5). Part of the single shared vocabulary — never introduce parallel enums or
/// magic strings to spell these states.
/// </summary>
/// <remarks>
/// Name-based JSON is mandatory (see <see cref="JsonStringEnumConverter{TEnum}"/>): the wire shape is the
/// enum member NAME, never the integer ordinal (NFR-6 schema tolerance). Each member carries a
/// <see cref="ProjectionBadgeAttribute"/> mapping it to a severity slot for FrontComposer rendering.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ReferenceState>))]
public enum ReferenceState
{
    /// <summary>The reference is included in the project context.</summary>
    [ProjectionBadge(BadgeSlot.Success)]
    Included,

    /// <summary>The reference is explicitly excluded from the project context.</summary>
    [ProjectionBadge(BadgeSlot.Neutral)]
    Excluded,

    /// <summary>Access to the referenced resource was denied (fail-closed authorization).</summary>
    [ProjectionBadge(BadgeSlot.Danger)]
    Unauthorized,

    /// <summary>The referenced resource is currently unavailable (upstream context unreachable).</summary>
    [ProjectionBadge(BadgeSlot.Danger)]
    Unavailable,

    /// <summary>The reference is stale relative to the current upstream state.</summary>
    [ProjectionBadge(BadgeSlot.Warning)]
    Stale,

    /// <summary>The reference points at an archived resource.</summary>
    [ProjectionBadge(BadgeSlot.Warning)]
    Archived,

    /// <summary>The reference is ambiguous and could resolve to multiple candidates.</summary>
    [ProjectionBadge(BadgeSlot.Warning)]
    Ambiguous,

    /// <summary>The reference belongs to a different tenant than the owning project.</summary>
    [ProjectionBadge(BadgeSlot.Danger)]
    TenantMismatch,

    /// <summary>The reference conflicts with another reference or the current project state.</summary>
    [ProjectionBadge(BadgeSlot.Danger)]
    Conflict,

    /// <summary>The reference is structurally invalid (malformed identifier or owner context).</summary>
    [ProjectionBadge(BadgeSlot.Danger)]
    InvalidReference,
}
