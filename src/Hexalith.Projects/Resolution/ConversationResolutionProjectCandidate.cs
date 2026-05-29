// <copyright file="ConversationResolutionProjectCandidate.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// One tenant-authorized candidate Project enumerated by the Story 4.2 host composition (typically a
/// row from the tenant-scoped project list read model) and handed to
/// <see cref="ConversationResolutionEvidenceMapper"/> for per-conversation match-signal derivation.
/// </summary>
/// <remarks>
/// The host is responsible for tenant-scoping (only authoritative-tenant rows ever become candidates).
/// Archived candidates are included here; the pure engine applies the archived-exclusion rule from
/// <see cref="ReferenceState"/>/lifecycle and the request's IncludeArchived flag.
/// </remarks>
/// <param name="ProjectId">The opaque project identifier.</param>
/// <param name="DisplayName">The optional safe project name used for the metadata-derived heuristic match.</param>
/// <param name="Lifecycle">The candidate Project lifecycle.</param>
public sealed record ConversationResolutionProjectCandidate(
    string ProjectId,
    string? DisplayName,
    ProjectLifecycle Lifecycle)
{
    /// <summary>Gets the opaque project identifier.</summary>
    public string ProjectId { get; } = ValidateRequired(ProjectId, nameof(ProjectId));

    /// <summary>Gets the optional safe project name. Null/whitespace inputs normalize to null.</summary>
    public string? DisplayName { get; } = Normalize(DisplayName);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
