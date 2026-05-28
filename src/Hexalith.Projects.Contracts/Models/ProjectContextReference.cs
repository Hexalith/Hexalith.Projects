// <copyright file="ProjectContextReference.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Assembled, metadata-only reference inside the typed <see cref="ProjectContext"/> shape (AR-9,
/// Story 3.1). Mirrors the Story 2.7 <see cref="ProjectMemoryReference"/> shape — opaque identifier
/// plus safe display label plus shared-vocabulary state — and adds a typed
/// <see cref="ProjectReasonCode"/> reason code consumed by Story 3.3 ExplainContextSelection.
/// </summary>
/// <remarks>
/// Construction eager-validates non-empty <see cref="ReferenceKind"/> and <see cref="ReferenceId"/>
/// and normalizes whitespace-only <see cref="DisplayName"/> to <see langword="null"/>. The reference
/// never carries transcripts, file contents, memory bodies, prompts, paths, tokens, or secrets.
/// </remarks>
/// <param name="ReferenceKind">The allowlisted reference kind: <c>folder</c>, <c>file</c>, <c>memory</c>, or <c>conversation</c>.</param>
/// <param name="ReferenceId">The opaque sibling-owned reference identifier.</param>
/// <param name="DisplayName">Safe display metadata; <see langword="null"/> when missing or whitespace.</param>
/// <param name="ReferenceState">The shared <see cref="Ui.ReferenceState"/> surfaced for this reference.</param>
/// <param name="ReasonCode">Optional shared-vocabulary <see cref="ProjectReasonCode"/> explaining inclusion.</param>
/// <param name="ObservedAt">The instant at which the reference state was observed.</param>
public sealed record ProjectContextReference(
    string ReferenceKind,
    string ReferenceId,
    string? DisplayName,
    ReferenceState ReferenceState,
    ProjectReasonCode? ReasonCode,
    DateTimeOffset ObservedAt)
{
    /// <summary>Gets the allowlisted reference kind (<c>folder</c>, <c>file</c>, <c>memory</c>, or <c>conversation</c>).</summary>
    public string ReferenceKind { get; } = ValidateRequired(ReferenceKind, nameof(ReferenceKind));

    /// <summary>Gets the opaque sibling-owned reference identifier.</summary>
    public string ReferenceId { get; } = ValidateRequired(ReferenceId, nameof(ReferenceId));

    /// <summary>Gets the safe display metadata, or <see langword="null"/>.</summary>
    public string? DisplayName { get; } = Normalize(DisplayName);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
