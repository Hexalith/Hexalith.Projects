// <copyright file="UnavailableProjectConversationResolutionDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;

/// <summary>
/// Fail-closed default used when no <c>IConversationClient</c> is registered: every single-conversation
/// metadata read returns an <see cref="ReferenceState.Unavailable"/> evidence record so resolution can
/// never depend on an unverified conversation.
/// </summary>
public sealed class UnavailableProjectConversationResolutionDirectory : IProjectConversationResolutionDirectory
{
    /// <inheritdoc />
    public Task<ConversationResolutionMetadata> ReadConversationMetadataAsync(
        ConversationId conversationId,
        ConversationTenantId tenantId,
        CallerPrincipalId caller,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        return Task.FromResult(ConversationResolutionMetadata.FailClosed(conversationId.Value, ReferenceState.Unavailable));
    }
}
