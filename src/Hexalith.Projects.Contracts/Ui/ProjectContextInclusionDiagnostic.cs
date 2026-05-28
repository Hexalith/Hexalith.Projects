// <copyright file="ProjectContextInclusionDiagnostic.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The closed vocabulary of safe diagnostic strings allowed on <c>ProjectContextExclusion.Diagnostic</c>
/// (AR-9, Story 3.1). No raw upstream <c>Message</c> / <c>Suggestion</c> / path / token / payload text
/// is ever added to this vocabulary; new values are added only via a follow-up story.
/// </summary>
/// <remarks>
/// The single purpose of this surface is to guarantee that operator troubleshooting metadata (Story
/// 3.3 ExplainContextSelection) can never leak free-form upstream text. Construction-time validation
/// of <c>ProjectContextExclusion</c> and <c>ProjectContextEvaluation</c> rejects any value not in
/// <see cref="Values"/>.
/// </remarks>
public static class ProjectContextInclusionDiagnostic
{
    /// <summary>Diagnostic: tenant-mismatch collapse at the assembly boundary (Memories ACL recheck).</summary>
    public const string TenantMismatch = "tenantMismatch";

    /// <summary>Diagnostic: the project is unknown / not visible to the authoritative tenant.</summary>
    public const string ProjectUnknown = "projectUnknown";

    /// <summary>Diagnostic: the project is archived; all references are excluded by lifecycle.</summary>
    public const string ProjectArchived = "projectArchived";

    /// <summary>Diagnostic: the reference is unauthorized for the caller.</summary>
    public const string ReferenceUnauthorized = "referenceUnauthorized";

    /// <summary>Diagnostic: the reference is unavailable upstream (fail-closed-clean exclusion).</summary>
    public const string ReferenceUnavailable = "referenceUnavailable";

    /// <summary>Diagnostic: the reference is stale relative to the upstream state.</summary>
    public const string ReferenceStale = "referenceStale";

    /// <summary>Diagnostic: the reference points at an archived resource (lifecycle exclusion).</summary>
    public const string ReferenceArchived = "referenceArchived";

    /// <summary>Diagnostic: the reference is in conflict with another reference or project state.</summary>
    public const string ReferenceConflict = "referenceConflict";

    /// <summary>Diagnostic: the reference identifier is structurally malformed.</summary>
    public const string ReferenceInvalidIdentifier = "referenceInvalidIdentifier";

    /// <summary>Diagnostic: the reference kind is not on the four-kind allowlist.</summary>
    public const string ReferenceKindNotAllowlisted = "referenceKindNotAllowlisted";

    /// <summary>Diagnostic: the Project Folder reference is pending external creation (Story 2.4 degraded path).</summary>
    public const string ProjectFolderPending = "projectFolderPending";

    /// <summary>Diagnostic: the reference is ambiguous (multiple candidates could resolve).</summary>
    public const string ReferenceAmbiguous = "referenceAmbiguous";

    /// <summary>Diagnostic: the upstream conversation was policy-redacted.</summary>
    public const string ReferenceRedacted = "referenceRedacted";

    private static readonly IReadOnlyList<string> _values =
    [
        TenantMismatch,
        ProjectUnknown,
        ProjectArchived,
        ReferenceUnauthorized,
        ReferenceUnavailable,
        ReferenceStale,
        ReferenceArchived,
        ReferenceConflict,
        ReferenceInvalidIdentifier,
        ReferenceKindNotAllowlisted,
        ProjectFolderPending,
        ReferenceAmbiguous,
        ReferenceRedacted,
    ];

    /// <summary>Gets the closed list of allowed diagnostic strings (Ordinal, case-sensitive).</summary>
    public static IReadOnlyList<string> Values => _values;

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="value"/> is <see langword="null"/> or a
    /// member of the closed diagnostic vocabulary; otherwise <see langword="false"/>.
    /// </summary>
    /// <param name="value">The diagnostic candidate (Ordinal, case-sensitive).</param>
    /// <returns>Whether the candidate is acceptable for the <c>Diagnostic</c> field.</returns>
    public static bool IsKnown(string? value)
    {
        if (value is null)
        {
            return true;
        }

        return _values.Contains(value, StringComparer.Ordinal);
    }
}
