// <copyright file="ConversationResolutionMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Projects-owned, metadata-only view of the single conversation being resolved (Story 4.2). The host
/// composition reads this through the Pattern-A conversation ACL; the pure
/// <see cref="ConversationResolutionEvidenceMapper"/> consumes it to derive per-project match signals.
/// </summary>
/// <remarks>
/// <para>
/// Every field is safe metadata only — an opaque conversation id, an optional linked/hinted project id,
/// an optional safe display label, and the Projects-vocabulary <see cref="ReferenceState"/> the ACL
/// derived from the upstream conversation trust/freshness posture. It never carries transcript text,
/// prompt fragments, message bodies, paths, tokens, or tenant authority.
/// </para>
/// <para>
/// <see cref="ReferenceState"/> is the fail-closed evidence state for the whole conversation read:
/// <see cref="ReferenceState.Included"/> only when the upstream evidence is current and in-scope;
/// every other value (Stale, Unavailable, Unauthorized, Excluded, …) surfaces downstream as a
/// resolution exclusion rather than a positive match.
/// </para>
/// </remarks>
/// <param name="ConversationId">The opaque conversation identifier being resolved.</param>
/// <param name="LinkedProjectId">An optional opaque project id the conversation metadata records or hints (explicit assignment or response-scoped hydration). <see langword="null"/> for a truly project-less conversation.</param>
/// <param name="SafeLabel">An optional safe display label from the conversation metadata, used for the metadata-derived heuristic match.</param>
/// <param name="ReferenceState">The Projects-vocabulary reference state derived from the upstream conversation trust posture.</param>
public sealed record ConversationResolutionMetadata(
    string ConversationId,
    string? LinkedProjectId,
    string? SafeLabel,
    ReferenceState ReferenceState)
{
    /// <summary>Gets the opaque conversation identifier being resolved.</summary>
    public string ConversationId { get; } = ValidateRequired(ConversationId, nameof(ConversationId));

    /// <summary>Gets the optional linked/hinted project id. Null inputs normalize to null.</summary>
    public string? LinkedProjectId { get; } = Normalize(LinkedProjectId);

    /// <summary>Gets the optional safe display label. Null/whitespace inputs normalize to null.</summary>
    public string? SafeLabel { get; } = Normalize(SafeLabel);

    /// <summary>
    /// Creates a fail-closed metadata record for a conversation the ACL could not read in-scope. No
    /// linked id and no label survive, and the reference state is <paramref name="referenceState"/>
    /// (defaulting to <see cref="ReferenceState.Unavailable"/>).
    /// </summary>
    /// <param name="conversationId">The opaque conversation identifier.</param>
    /// <param name="referenceState">The fail-closed reference state.</param>
    /// <returns>A metadata record carrying no positive match evidence.</returns>
    public static ConversationResolutionMetadata FailClosed(
        string conversationId,
        ReferenceState referenceState = ReferenceState.Unavailable)
        => new(conversationId, LinkedProjectId: null, SafeLabel: null, referenceState);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
