// <copyright file="ProjectContextEvaluation.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Per-candidate evaluation trace row emitted by <c>ProjectContextInclusionPolicy</c> alongside the
/// assembled <see cref="ProjectContext"/> (AR-9, Story 3.1). The Story 3.3 ExplainContextSelection
/// endpoint surfaces these rows so operators can read the include/exclude verdict for every candidate.
/// </summary>
/// <remarks>
/// Construction eager-validates non-empty <see cref="ReferenceKind"/> and <see cref="ReferenceId"/>,
/// and validates <see cref="Diagnostic"/> against
/// <see cref="ProjectContextInclusionDiagnostic.Values"/> exactly as <see cref="ProjectContextExclusion"/>
/// does, so free-form diagnostic text cannot leak through this field either.
/// </remarks>
/// <param name="ReferenceKind">The candidate reference kind.</param>
/// <param name="ReferenceId">The opaque sibling-owned reference identifier.</param>
/// <param name="ResultState">The surfaced <see cref="Ui.ReferenceState"/> for this candidate.</param>
/// <param name="FailedCheck">The single failed inclusion check, or <see langword="null"/> when included.</param>
/// <param name="ReasonCode">Optional shared-vocabulary reason code (set on inclusion).</param>
/// <param name="Diagnostic">Optional safe diagnostic string from <see cref="ProjectContextInclusionDiagnostic.Values"/>.</param>
/// <param name="ObservedAt">The instant at which the candidate was evaluated.</param>
public sealed record ProjectContextEvaluation(
    string ReferenceKind,
    string ReferenceId,
    ReferenceState ResultState,
    ProjectContextInclusionCheck? FailedCheck,
    ProjectReasonCode? ReasonCode,
    string? Diagnostic,
    DateTimeOffset ObservedAt)
{
    /// <summary>Gets the candidate reference kind.</summary>
    public string ReferenceKind { get; } = ValidateRequired(ReferenceKind, nameof(ReferenceKind));

    /// <summary>Gets the opaque sibling-owned reference identifier.</summary>
    public string ReferenceId { get; } = ValidateRequired(ReferenceId, nameof(ReferenceId));

    /// <summary>Gets the safe diagnostic string from the closed vocabulary, or null.</summary>
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
