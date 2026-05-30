// <copyright file="ProjectDiagnosticRendering.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Rendering;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Metadata-only rendering helpers for Story 5.2 operator diagnostics.
/// </summary>
public static class ProjectDiagnosticRendering
{
    /// <summary>Counts references that need operator attention.</summary>
    /// <param name="diagnostic">The diagnostic DTO.</param>
    /// <returns>The warning count.</returns>
    public static int CountWarnings(ProjectOperatorDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return diagnostic.References.Count(IsWarningReference);
    }

    private static bool IsWarningReference(ProjectOperatorReferenceSummary reference)
        => !Enum.TryParse(reference.ReferenceState, ignoreCase: true, out ReferenceState state)
            || state is not ReferenceState.Included;
}

