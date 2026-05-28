// <copyright file="ProjectContextAssemblyOutcome.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Outer assembly outcome of <c>ProjectContextInclusionPolicy</c> (AR-9, Story 3.1). Indicates whether
/// the assembled <c>ProjectContext</c> was produced, or whether the request collapsed to a safe-denial
/// outcome at the boundary.
/// </summary>
/// <remarks>
/// <para>
/// Name-based JSON is mandatory (see <see cref="JsonStringEnumConverter{TEnum}"/>); the wire shape is
/// the enum member NAME, never the integer ordinal (NFR-6 schema tolerance).
/// </para>
/// <para>
/// <see cref="ProjectUnavailable"/> is intentionally <see cref="BadgeSlot.Neutral"/>: it is the
/// safe-denial 404 contract surface and must never signal existence by colour. Tenant-mismatch /
/// cross-tenant access collapses to <see cref="ProjectUnavailable"/> (never <c>Unauthorized</c>) at
/// the boundary.
/// </para>
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectContextAssemblyOutcome>))]
public enum ProjectContextAssemblyOutcome
{
    /// <summary>The assembly succeeded and produced a Project Context (possibly with excluded refs).</summary>
    [ProjectionBadge(BadgeSlot.Success)]
    Assembled,

    /// <summary>The project is not visible to the authoritative tenant (safe-denial 404 contract).</summary>
    [ProjectionBadge(BadgeSlot.Neutral)]
    ProjectUnavailable,

    /// <summary>Tenant authority is missing, mismatched, disabled, malformed, or otherwise denied.</summary>
    [ProjectionBadge(BadgeSlot.Danger)]
    Unauthorized,
}
