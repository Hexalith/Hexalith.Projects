// <copyright file="ResolutionResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

/// <summary>
/// Outcome of a project-resolution attempt (AR-18, UX-DR5). Part of the single shared vocabulary —
/// never introduce parallel enums or magic strings to spell these results.
/// </summary>
/// <remarks>
/// Name-based JSON is mandatory (see <see cref="JsonStringEnumConverter{TEnum}"/>): the wire shape is the
/// enum member NAME, never the integer ordinal (NFR-6 schema tolerance). Badge severity slots are
/// presentation metadata owned by the UI-only <c>ProjectVocabularyDescriptors</c> table.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ResolutionResult>))]
public enum ResolutionResult
{
    /// <summary>No candidate project matched the resolution inputs.</summary>
    NoMatch,

    /// <summary>Exactly one candidate project matched (unambiguous resolution).</summary>
    SingleCandidate,

    /// <summary>Multiple candidate projects matched (ambiguous — requires confirmation).</summary>
    MultipleCandidates,
}
