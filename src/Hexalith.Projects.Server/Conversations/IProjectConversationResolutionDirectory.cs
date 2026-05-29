// <copyright file="IProjectConversationResolutionDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Resolution;

/// <summary>
/// Projects-owned Pattern-A ACL for the single-conversation, metadata-only read the Story 4.2
/// resolution query needs. Unlike <see cref="IProjectConversationDirectory"/> (project-keyed forward
/// listing), this reads exactly one conversation by its opaque identity for a project-less
/// conversation, returning safe metadata the pure resolution mapper can evaluate.
/// </summary>
public interface IProjectConversationResolutionDirectory
{
    /// <summary>
    /// Reads safe, tenant-scoped metadata for one conversation. Fails closed: an unauthorized,
    /// cross-tenant, unknown, or unreachable read returns a fail-closed
    /// <see cref="ConversationResolutionMetadata"/> (no linked id, no label) carrying a degraded
    /// reference state instead of throwing or leaking.
    /// </summary>
    /// <param name="conversationId">The opaque Conversations-owned conversation identity.</param>
    /// <param name="tenantId">The authoritative tenant scope.</param>
    /// <param name="caller">The authenticated caller principal.</param>
    /// <param name="correlationId">The safe request correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Projects-shaped, metadata-only conversation evidence.</returns>
    Task<ConversationResolutionMetadata> ReadConversationMetadataAsync(
        ConversationId conversationId,
        ConversationTenantId tenantId,
        CallerPrincipalId caller,
        string correlationId,
        CancellationToken cancellationToken = default);
}
