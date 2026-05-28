// <copyright file="ProjectContextDiagnostics.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Internal helpers that build only closed-vocabulary diagnostic strings for
/// <c>ProjectContextInclusionPolicy</c> (Story 3.1). Every helper asserts the chosen string is a
/// member of <see cref="ProjectContextInclusionDiagnostic.Values"/> — a code/contract bug if not.
/// </summary>
internal static class ProjectContextDiagnostics
{
    public static string For(ProjectContextInclusionCheck failedCheck, ReferenceState surfacedState)
        => failedCheck switch
        {
            ProjectContextInclusionCheck.TenantAuthority => Guard(ProjectContextInclusionDiagnostic.TenantMismatch),
            ProjectContextInclusionCheck.ProjectVisibility => Guard(ProjectContextInclusionDiagnostic.ProjectUnknown),
            ProjectContextInclusionCheck.ProjectLifecycle => Guard(ProjectContextInclusionDiagnostic.ProjectArchived),
            ProjectContextInclusionCheck.ReferenceAuthorization => Guard(ProjectContextInclusionDiagnostic.ReferenceUnauthorized),
            ProjectContextInclusionCheck.ReferenceLifecycle => DiagnosticForLifecycle(surfacedState),
            ProjectContextInclusionCheck.ReferenceFreshness => DiagnosticForFreshness(surfacedState),
            ProjectContextInclusionCheck.ReferenceKindAllowlist => DiagnosticForAllowlist(surfacedState),
            _ => throw new InvalidOperationException($"Unmapped ProjectContextInclusionCheck '{failedCheck}'."),
        };

    public static string Guard(string value)
    {
        if (!ProjectContextInclusionDiagnostic.IsKnown(value))
        {
            throw new InvalidOperationException(
                $"Diagnostic '{value}' is not a member of the closed ProjectContextInclusionDiagnostic vocabulary.");
        }

        return value;
    }

    private static string DiagnosticForLifecycle(ReferenceState surfacedState)
        => surfacedState switch
        {
            ReferenceState.Archived => Guard(ProjectContextInclusionDiagnostic.ReferenceArchived),
            ReferenceState.Ambiguous => Guard(ProjectContextInclusionDiagnostic.ReferenceAmbiguous),
            ReferenceState.Conflict => Guard(ProjectContextInclusionDiagnostic.ReferenceConflict),
            _ => Guard(ProjectContextInclusionDiagnostic.ReferenceArchived),
        };

    private static string DiagnosticForFreshness(ReferenceState surfacedState)
        => surfacedState switch
        {
            ReferenceState.Stale => Guard(ProjectContextInclusionDiagnostic.ReferenceStale),
            ReferenceState.Pending => Guard(ProjectContextInclusionDiagnostic.ProjectFolderPending),
            ReferenceState.Excluded => Guard(ProjectContextInclusionDiagnostic.ReferenceRedacted),
            _ => Guard(ProjectContextInclusionDiagnostic.ReferenceUnavailable),
        };

    private static string DiagnosticForAllowlist(ReferenceState surfacedState)
        => surfacedState == ReferenceState.InvalidReference
            ? Guard(ProjectContextInclusionDiagnostic.ReferenceInvalidIdentifier)
            : Guard(ProjectContextInclusionDiagnostic.ReferenceKindNotAllowlisted);
}
