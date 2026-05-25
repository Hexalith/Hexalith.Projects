// <copyright file="ResolutionResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Outcome of a project-resolution attempt (AR-18, UX-DR5). Part of the single shared vocabulary —
/// never introduce parallel enums or magic strings to spell these results.
/// </summary>
/// <remarks>
/// Name-based JSON is mandatory (see <see cref="JsonStringEnumConverter{TEnum}"/>): the wire shape is the
/// enum member NAME, never the integer ordinal (NFR-6 schema tolerance). Each member carries a
/// <see cref="ProjectionBadgeAttribute"/> mapping it to a severity slot for FrontComposer rendering.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ResolutionResult>))]
public enum ResolutionResult
{
    /// <summary>No candidate project matched the resolution inputs.</summary>
    [ProjectionBadge(BadgeSlot.Neutral)]
    NoMatch,

    /// <summary>Exactly one candidate project matched (unambiguous resolution).</summary>
    [ProjectionBadge(BadgeSlot.Success)]
    SingleCandidate,

    /// <summary>Multiple candidate projects matched (ambiguous — requires confirmation).</summary>
    [ProjectionBadge(BadgeSlot.Warning)]
    MultipleCandidates,
}
