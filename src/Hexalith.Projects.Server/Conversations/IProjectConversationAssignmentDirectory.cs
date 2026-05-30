// <copyright file="IProjectConversationAssignmentDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Identifiers;

using ProjectId = Hexalith.Projects.Contracts.Identifiers.ProjectId;

/// <summary>Projects-owned write ACL for conversation project assignment changes.</summary>
public interface IProjectConversationAssignmentDirectory
{
    /// <summary>Links a conversation to the target Project when it is unassigned or already assigned to that Project.</summary>
    Task<ProjectConversationAssignmentResult> LinkAsync(
        ProjectId projectId,
        ConversationId conversationId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        ProjectId? expectedCurrentProjectId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Moves a conversation from the expected source Project to the target Project.</summary>
    Task<ProjectConversationAssignmentResult> MoveAsync(
        ProjectId targetProjectId,
        ConversationId conversationId,
        ProjectId sourceProjectId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>Clears a conversation assignment only when the current Project is the requested Project.</summary>
    Task<ProjectConversationAssignmentResult> UnlinkAsync(
        ProjectId projectId,
        ConversationId conversationId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotently confirms a resolution assignment by reading the current Conversation assignment
    /// before dispatching a write.
    /// </summary>
    Task<ProjectConversationAssignmentResult> ConfirmResolutionAssignmentAsync(
        ProjectId targetProjectId,
        ConversationId conversationId,
        ProjectId? expectedSourceProjectId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        CancellationToken cancellationToken = default);
}
