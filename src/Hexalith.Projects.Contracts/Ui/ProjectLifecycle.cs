// <copyright file="ProjectLifecycle.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Lifecycle state of a Project aggregate (AR-18, UX-DR5). Part of the single shared vocabulary —
/// never introduce a parallel <c>ProjectStatus</c> enum or magic strings to spell these states.
/// </summary>
/// <remarks>
/// Name-based JSON is mandatory (see <see cref="JsonStringEnumConverter{TEnum}"/>): the wire shape is
/// the enum member NAME, never the integer ordinal, so members can be inserted/renamed without breaking
/// the contract (NFR-6 schema tolerance). Each member carries a <see cref="ProjectionBadgeAttribute"/>
/// mapping it to a severity slot for FrontComposer rendering.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectLifecycle>))]
public enum ProjectLifecycle
{
    /// <summary>The project is active and usable.</summary>
    [ProjectionBadge(BadgeSlot.Success)]
    Active,

    /// <summary>The project has been archived (read-only, retained for audit/history).</summary>
    [ProjectionBadge(BadgeSlot.Warning)]
    Archived,
}
