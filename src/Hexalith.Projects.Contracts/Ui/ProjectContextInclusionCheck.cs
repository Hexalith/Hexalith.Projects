// <copyright file="ProjectContextInclusionCheck.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Inclusion-policy check identity (AR-9, Story 3.1). Names the single failed check whose verdict
/// excluded a candidate reference from the assembled Project Context, or — when the failed check is
/// <see cref="TenantAuthority"/> or <see cref="ProjectVisibility"/> — collapsed the entire assembly.
/// Part of the single shared vocabulary for context-assembly explanations consumed by Stories 3.2
/// (Get), 3.3 (Explain), 3.4 (Refresh), and 3.5 (Conversation Start Setup).
/// </summary>
/// <remarks>
/// Name-based JSON is mandatory (see <see cref="JsonStringEnumConverter{TEnum}"/>): the wire shape is
/// the enum member NAME, never the integer ordinal (NFR-6 schema tolerance). Every member maps to
/// <see cref="BadgeSlot.Info"/> — the failed-check is purely diagnostic / explanatory, never a
/// severity signal on its own.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectContextInclusionCheck>))]
public enum ProjectContextInclusionCheck
{
    /// <summary>Tenant-authority check: authoritative tenant claim present and matches the request tenant.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    TenantAuthority,

    /// <summary>Project-visibility check: the project exists and belongs to the authoritative tenant.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    ProjectVisibility,

    /// <summary>Project-lifecycle check: the owning project is Active (not Archived).</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    ProjectLifecycle,

    /// <summary>Reference-authorization check: the candidate reference is authorized for the caller.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    ReferenceAuthorization,

    /// <summary>Reference-lifecycle check: the candidate reference is in an includable lifecycle state.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    ReferenceLifecycle,

    /// <summary>Reference-freshness check: the candidate reference is fresh enough to include.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    ReferenceFreshness,

    /// <summary>Reference-kind allowlist check: the reference kind is one of the four allowlisted kinds.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    ReferenceKindAllowlist,
}
