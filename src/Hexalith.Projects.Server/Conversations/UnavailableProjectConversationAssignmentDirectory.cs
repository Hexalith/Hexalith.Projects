// <copyright file="UnavailableProjectConversationAssignmentDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Identifiers;

using ProjectId = Hexalith.Projects.Contracts.Identifiers.ProjectId;

/// <summary>Fail-closed write directory used until a host configures the Conversations client.</summary>
internal sealed class UnavailableProjectConversationAssignmentDirectory : IProjectConversationAssignmentDirectory
{
    /// <inheritdoc />
    public Task<ProjectConversationAssignmentResult> LinkAsync(
        ProjectId projectId,
        ConversationId conversationId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        ProjectId? expectedCurrentProjectId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Unavailable(metadata));

    /// <inheritdoc />
    public Task<ProjectConversationAssignmentResult> MoveAsync(
        ProjectId targetProjectId,
        ConversationId conversationId,
        ProjectId sourceProjectId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Unavailable(metadata));

    /// <inheritdoc />
    public Task<ProjectConversationAssignmentResult> UnlinkAsync(
        ProjectId projectId,
        ConversationId conversationId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Unavailable(metadata));

    /// <inheritdoc />
    public Task<ProjectConversationAssignmentResult> ConfirmResolutionAssignmentAsync(
        ProjectId targetProjectId,
        ConversationId conversationId,
        ProjectId? expectedSourceProjectId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Unavailable(metadata));

    private static ProjectConversationAssignmentResult Unavailable(ProjectConversationCommandMetadata metadata)
        => new(ProjectConversationAssignmentOutcome.Unavailable, metadata.CorrelationId);
}
