// <copyright file="ProjectContextConversationEvidence.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System;

using Hexalith.Projects.Contracts.Queries;

/// <summary>
/// Projects-shaped, metadata-only conversation evidence consumed by
/// <c>ProjectContextInclusionPolicy</c> (Story 3.1). Built by Story 3.2's host endpoint from
/// <c>ProjectConversationItem</c> — kept Projects-shaped here so the policy never imports any
/// sibling-conversations type (Tier-1 purity / AC 11).
/// </summary>
/// <remarks>
/// Construction eager-validates non-empty <see cref="ConversationId"/> and normalizes whitespace-only
/// <see cref="DisplayLabel"/> to <see langword="null"/>. No transcript text, prompt fragments, or
/// raw upstream payloads are ever carried on this shape.
/// </remarks>
/// <param name="ConversationId">The opaque Conversations-owned conversation identifier (as a string).</param>
/// <param name="DisplayLabel">Optional safe display label.</param>
/// <param name="TrustSignal">The Projects-owned trust signal projected from upstream evidence.</param>
/// <param name="LastCheckedAt">The freshness signal: instant the upstream evidence was last observed.</param>
public sealed record ProjectContextConversationEvidence(
    string ConversationId,
    string? DisplayLabel,
    ProjectConversationTrustSignal TrustSignal,
    DateTimeOffset LastCheckedAt)
{
    /// <summary>Gets the opaque Conversations-owned conversation identifier.</summary>
    public string ConversationId { get; } = ValidateRequired(ConversationId, nameof(ConversationId));

    /// <summary>Gets the optional safe display label.</summary>
    public string? DisplayLabel { get; } = Normalize(DisplayLabel);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
