// <copyright file="ProjectResolutionCandidateEvidence.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Metadata-only evidence for one candidate Project considered by <see cref="ProjectResolutionEngine"/>.
/// The caller pre-fetches and ACL-checks this evidence; the engine only evaluates it.
/// </summary>
/// <param name="ProjectId">The opaque project identifier.</param>
/// <param name="DisplayName">Optional safe display name.</param>
/// <param name="Lifecycle">The candidate Project lifecycle.</param>
/// <param name="Signals">Per-reference match signals already projected into Projects vocabulary.</param>
public sealed record ProjectResolutionCandidateEvidence(
    string ProjectId,
    string? DisplayName,
    ProjectLifecycle Lifecycle,
    IReadOnlyList<ProjectResolutionMatchSignal> Signals)
{
    /// <summary>Gets the opaque project identifier.</summary>
    public string ProjectId { get; } = ValidateRequired(ProjectId, nameof(ProjectId));

    /// <summary>Gets the optional safe display name.</summary>
    public string? DisplayName { get; } = Normalize(DisplayName);

    /// <summary>Gets per-reference match signals. Null inputs normalize to empty.</summary>
    public IReadOnlyList<ProjectResolutionMatchSignal> Signals { get; } = Signals ?? Array.Empty<ProjectResolutionMatchSignal>();

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}

/// <summary>
/// A single Projects-shaped match signal for project resolution. The signal carries only safe opaque
/// metadata and shared vocabulary values; no upstream payload, transcript, path, or body is allowed.
/// </summary>
/// <param name="ReferenceKind">The Projects reference kind: conversation, folder, file, memory, or metadata.</param>
/// <param name="ReferenceId">The opaque reference identifier.</param>
/// <param name="ReasonCode">The shared reason code represented by this signal.</param>
/// <param name="ReferenceState">The ACL/freshness/lifecycle state observed by the host.</param>
/// <param name="ObservedAt">The instant at which this signal was observed.</param>
public sealed record ProjectResolutionMatchSignal(
    string ReferenceKind,
    string ReferenceId,
    ProjectReasonCode ReasonCode,
    ReferenceState ReferenceState,
    DateTimeOffset ObservedAt)
{
    /// <summary>Gets the Projects reference kind.</summary>
    public string ReferenceKind { get; } = ValidateRequired(ReferenceKind, nameof(ReferenceKind));

    /// <summary>Gets the opaque reference identifier.</summary>
    public string ReferenceId { get; } = ValidateRequired(ReferenceId, nameof(ReferenceId));

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
