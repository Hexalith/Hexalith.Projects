// <copyright file="ProjectContextExclusion.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Assembled-result row describing a candidate reference that <c>ProjectContextInclusionPolicy</c>
/// excluded from the <see cref="ProjectContext"/> (AR-9, Story 3.1). Story 3.3 ExplainContextSelection
/// surfaces these rows to operators to explain why a reference was left out.
/// </summary>
/// <remarks>
/// Construction eager-validates non-empty <see cref="ReferenceKind"/> and <see cref="ReferenceId"/>,
/// and validates <see cref="Diagnostic"/> against
/// <see cref="ProjectContextInclusionDiagnostic.Values"/>. A free-form diagnostic value throws
/// <see cref="ArgumentException"/> at construction so no upstream <c>Message</c>, <c>Suggestion</c>,
/// path, token, or payload string can ever leak through this field.
/// </remarks>
/// <param name="ReferenceKind">The allowlisted reference kind, or the candidate kind for non-allowlisted cases.</param>
/// <param name="ReferenceId">The opaque sibling-owned reference identifier.</param>
/// <param name="ReferenceState">The shared <see cref="Ui.ReferenceState"/> surfaced for the excluded reference.</param>
/// <param name="ReasonCode">Optional shared-vocabulary <see cref="ProjectReasonCode"/> (typically null for exclusions).</param>
/// <param name="FailedCheck">The single inclusion check whose verdict excluded the candidate.</param>
/// <param name="Diagnostic">Optional safe diagnostic string from <see cref="ProjectContextInclusionDiagnostic.Values"/>.</param>
public sealed record ProjectContextExclusion(
    string ReferenceKind,
    string ReferenceId,
    ReferenceState ReferenceState,
    ProjectReasonCode? ReasonCode,
    ProjectContextInclusionCheck FailedCheck,
    string? Diagnostic)
{
    /// <summary>Gets the allowlisted reference kind, or the candidate kind for non-allowlisted cases.</summary>
    public string ReferenceKind { get; } = ValidateRequired(ReferenceKind, nameof(ReferenceKind));

    /// <summary>Gets the opaque sibling-owned reference identifier.</summary>
    public string ReferenceId { get; } = ValidateRequired(ReferenceId, nameof(ReferenceId));

    /// <summary>Gets the safe diagnostic string from the closed <c>ProjectContextInclusionDiagnostic</c> vocabulary, or null.</summary>
    public string? Diagnostic { get; } = ValidateDiagnostic(Diagnostic);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string? ValidateDiagnostic(string? value)
    {
        if (!ProjectContextInclusionDiagnostic.IsKnown(value))
        {
            throw new ArgumentException(
                $"Diagnostic value is not a member of the closed ProjectContextInclusionDiagnostic vocabulary.",
                nameof(Diagnostic));
        }

        return value;
    }
}
