// <copyright file="ReferenceState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

/// <summary>
/// Inclusion / reference state of a sibling reference (conversation, folder, file, memory) within a
/// Project (AR-18, UX-DR5). Part of the single shared vocabulary — never introduce parallel enums or
/// magic strings to spell these states.
/// </summary>
/// <remarks>
/// Name-based JSON is mandatory (see <see cref="JsonStringEnumConverter{TEnum}"/>): the wire shape is the
/// enum member NAME, never the integer ordinal (NFR-6 schema tolerance). Badge severity slots are
/// presentation metadata owned by the UI-only <c>ProjectVocabularyDescriptors</c> table.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ReferenceState>))]
public enum ReferenceState
{
    /// <summary>The reference is queued or awaiting upstream confirmation.</summary>
    Pending,

    /// <summary>The reference is included in the project context.</summary>
    Included,

    /// <summary>The reference is explicitly excluded from the project context.</summary>
    Excluded,

    /// <summary>Access to the referenced resource was denied (fail-closed authorization).</summary>
    Unauthorized,

    /// <summary>The referenced resource is currently unavailable (upstream context unreachable).</summary>
    Unavailable,

    /// <summary>The reference is stale relative to the current upstream state.</summary>
    Stale,

    /// <summary>The reference points at an archived resource.</summary>
    Archived,

    /// <summary>The reference is ambiguous and could resolve to multiple candidates.</summary>
    Ambiguous,

    /// <summary>The reference belongs to a different tenant than the owning project.</summary>
    TenantMismatch,

    /// <summary>The reference conflicts with another reference or the current project state.</summary>
    Conflict,

    /// <summary>The reference is structurally invalid (malformed identifier or owner context).</summary>
    InvalidReference,
}
