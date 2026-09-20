// <copyright file="ConversationStartAdmissionSnapshot.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

/// <summary>AD-32 response snapshot governing Conversation-start setup usability.</summary>
/// <param name="ResponseState">The aggregate response state.</param>
/// <param name="AsOf">The persisted read-model observation instant.</param>
/// <param name="ProjectVersion">The authorized Project read-model version.</param>
/// <param name="Components">Metadata-only component evidence.</param>
/// <param name="RecoveryActions">Bounded recovery actions.</param>
public sealed record ConversationStartAdmissionSnapshot(
    ConversationStartResponseState ResponseState,
    DateTimeOffset AsOf,
    long ProjectVersion,
    IReadOnlyList<ConversationStartComponent> Components,
    IReadOnlyList<string> RecoveryActions)
{
    /// <summary>Projects the shared AD-32 snapshot into the legacy Story 6.2 CLR contract.</summary>
    /// <param name="snapshot">The shared snapshot.</param>
    /// <returns>The wire-compatible Conversation-start snapshot.</returns>
    public static ConversationStartAdmissionSnapshot FromShared(AdmissionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new ConversationStartAdmissionSnapshot(
            snapshot.ResponseState switch
            {
                AdmissionResponseState.Complete => ConversationStartResponseState.Complete,
                AdmissionResponseState.Partial => ConversationStartResponseState.Partial,
                AdmissionResponseState.Unavailable => ConversationStartResponseState.Unavailable,
                AdmissionResponseState.Denied => ConversationStartResponseState.Denied,
                _ => throw new ArgumentOutOfRangeException(nameof(snapshot)),
            },
            snapshot.AsOf,
            snapshot.ProjectVersion,
            snapshot.Components
                .Select(static component => new ConversationStartComponent(
                    component.Name,
                    component.Included,
                    component.Freshness,
                    component.Reason))
                .ToArray(),
            snapshot.RecoveryActions.ToArray());
    }
}
