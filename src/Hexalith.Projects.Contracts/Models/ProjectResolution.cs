// <copyright file="ProjectResolution.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Metadata-only Project resolution output emitted by the pure Story 4.1 resolution engine. The wire
/// shape intentionally declares no TenantId field.
/// </summary>
/// <param name="Result">The top-level resolution outcome.</param>
/// <param name="Candidates">Ranked qualifying candidates.</param>
/// <param name="Excluded">Per-candidate/per-signal exclusion evidence for failed-closed inputs.</param>
/// <param name="ObservedAt">The evaluation instant supplied to the engine.</param>
public sealed record ProjectResolution(
    ResolutionResult Result,
    IReadOnlyList<ResolutionCandidate> Candidates,
    IReadOnlyList<ResolutionExclusion> Excluded,
    DateTimeOffset ObservedAt)
{
    /// <summary>Gets ranked qualifying candidates.</summary>
    public IReadOnlyList<ResolutionCandidate> Candidates { get; } = Candidates ?? Array.Empty<ResolutionCandidate>();

    /// <summary>Gets per-candidate/per-signal exclusion evidence.</summary>
    public IReadOnlyList<ResolutionExclusion> Excluded { get; } = Excluded ?? Array.Empty<ResolutionExclusion>();

    /// <summary>Creates a no-match resolution result.</summary>
    /// <param name="excluded">Failed-closed exclusion rows.</param>
    /// <param name="observedAt">The evaluation instant.</param>
    /// <returns>A no-match result.</returns>
    public static ProjectResolution NoMatch(
        IReadOnlyList<ResolutionExclusion>? excluded,
        DateTimeOffset observedAt)
        => new(ResolutionResult.NoMatch, Array.Empty<ResolutionCandidate>(), excluded ?? Array.Empty<ResolutionExclusion>(), observedAt);

    /// <summary>Creates a single-candidate resolution result.</summary>
    /// <param name="candidate">The sole qualifying candidate.</param>
    /// <param name="excluded">Failed-closed exclusion rows.</param>
    /// <param name="observedAt">The evaluation instant.</param>
    /// <returns>A single-candidate result.</returns>
    public static ProjectResolution SingleCandidate(
        ResolutionCandidate candidate,
        IReadOnlyList<ResolutionExclusion>? excluded,
        DateTimeOffset observedAt)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return new(ResolutionResult.SingleCandidate, [candidate], excluded ?? Array.Empty<ResolutionExclusion>(), observedAt);
    }

    /// <summary>Creates a multiple-candidates resolution result.</summary>
    /// <param name="candidates">The ranked qualifying candidates.</param>
    /// <param name="excluded">Failed-closed exclusion rows.</param>
    /// <param name="observedAt">The evaluation instant.</param>
    /// <returns>A multiple-candidates result.</returns>
    public static ProjectResolution MultipleCandidates(
        IReadOnlyList<ResolutionCandidate> candidates,
        IReadOnlyList<ResolutionExclusion>? excluded,
        DateTimeOffset observedAt)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return new(ResolutionResult.MultipleCandidates, candidates, excluded ?? Array.Empty<ResolutionExclusion>(), observedAt);
    }
}

/// <summary>One qualifying project candidate in a resolution result.</summary>
/// <param name="ProjectId">The opaque project identifier.</param>
/// <param name="DisplayName">Optional safe display name.</param>
/// <param name="ReasonCodes">Distinct shared-vocabulary reason codes that contributed to score.</param>
/// <param name="Rank">One-based deterministic rank.</param>
/// <param name="Score">Metadata-only relative score from the documented heuristic.</param>
public sealed record ResolutionCandidate(
    string ProjectId,
    string? DisplayName,
    IReadOnlyList<ProjectReasonCode> ReasonCodes,
    int Rank,
    int Score)
{
    /// <summary>Gets the opaque project identifier.</summary>
    public string ProjectId { get; } = ValidateRequired(ProjectId, nameof(ProjectId));

    /// <summary>Gets the optional safe display name.</summary>
    public string? DisplayName { get; } = Normalize(DisplayName);

    /// <summary>Gets distinct shared-vocabulary reason codes.</summary>
    public IReadOnlyList<ProjectReasonCode> ReasonCodes { get; } = ValidateReasonCodes(ReasonCodes);

    /// <summary>Gets the one-based deterministic rank.</summary>
    public int Rank { get; } = Rank > 0 ? Rank : throw new ArgumentOutOfRangeException(nameof(Rank), "Rank must be greater than zero.");

    /// <summary>Gets the relative numeric score.</summary>
    public int Score { get; } = Score >= 0 ? Score : throw new ArgumentOutOfRangeException(nameof(Score), "Score must be zero or greater.");

    private static IReadOnlyList<ProjectReasonCode> ValidateReasonCodes(IReadOnlyList<ProjectReasonCode>? value)
    {
        if (value is null || value.Count == 0)
        {
            throw new ArgumentException("At least one reason code is required.", nameof(ReasonCodes));
        }

        return value.Distinct().ToArray();
    }

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}

/// <summary>
/// Metadata-only exclusion evidence explaining why a candidate or candidate signal did not
/// contribute to a positive resolution match.
/// </summary>
/// <param name="ProjectId">The opaque project identifier.</param>
/// <param name="DisplayName">Optional safe display name.</param>
/// <param name="ReferenceState">The surfaced fail-closed state.</param>
/// <param name="ReasonCode">Optional reason code associated with the failed signal.</param>
/// <param name="Diagnostic">Optional safe diagnostic from <see cref="ProjectContextInclusionDiagnostic"/>.</param>
public sealed record ResolutionExclusion(
    string ProjectId,
    string? DisplayName,
    ReferenceState ReferenceState,
    ProjectReasonCode? ReasonCode,
    string? Diagnostic)
{
    /// <summary>Gets the opaque project identifier.</summary>
    public string ProjectId { get; } = ValidateRequired(ProjectId, nameof(ProjectId));

    /// <summary>Gets the optional safe display name.</summary>
    public string? DisplayName { get; } = Normalize(DisplayName);

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
                "Diagnostic value is not a member of the closed ProjectContextInclusionDiagnostic vocabulary.",
                nameof(Diagnostic));
        }

        return value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
