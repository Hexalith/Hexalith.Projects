// <copyright file="ProjectReasonCode.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Explanatory reason code describing why a reference was included or why a resolution matched
/// (AR-18, UX-DR5). Part of the single shared vocabulary — never introduce parallel enums, magic
/// strings, or free-text reasons as the canonical signal to spell these codes.
/// </summary>
/// <remarks>
/// Name-based JSON is mandatory (see <see cref="JsonStringEnumConverter{TEnum}"/>): the wire shape is the
/// enum member NAME, never the integer ordinal (NFR-6 schema tolerance). Reason codes are explanatory,
/// so each member maps to the informational <see cref="BadgeSlot.Info"/> severity slot.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectReasonCode>))]
public enum ProjectReasonCode
{
    /// <summary>A conversation was explicitly linked to the project.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    ConversationLinked,

    /// <summary>The project folder matched the reference.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    ProjectFolderMatched,

    /// <summary>A file reference matched the project.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    FileReferenceMatched,

    /// <summary>A memory matched the project.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    MemoryMatched,

    /// <summary>Project metadata matched the resolution inputs.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    MetadataMatched,
}
