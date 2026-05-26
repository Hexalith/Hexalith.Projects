// <copyright file="ConversationsProjectConversationAssignmentDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using System.Net;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Projects.Contracts.Identifiers;

using ConversationProjectId = Hexalith.Conversations.Contracts.Identifiers.ProjectId;
using ProjectId = Hexalith.Projects.Contracts.Identifiers.ProjectId;

/// <summary>Projects write ACL adapter over the Conversations reassignment command client.</summary>
public sealed class ConversationsProjectConversationAssignmentDirectory(
    IConversationClient conversationClient,
    IActorPartyResolver actorPartyResolver) : IProjectConversationAssignmentDirectory
{
    private readonly IActorPartyResolver _actorPartyResolver = actorPartyResolver ?? throw new ArgumentNullException(nameof(actorPartyResolver));
    private readonly IConversationClient _conversationClient = conversationClient ?? throw new ArgumentNullException(nameof(conversationClient));

    /// <inheritdoc />
    public async Task<ProjectConversationAssignmentResult> LinkAsync(
        ProjectId projectId,
        ConversationId conversationId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        ProjectId? expectedCurrentProjectId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(metadata);

        if (expectedCurrentProjectId is not null
            && !string.Equals(expectedCurrentProjectId.Value, projectId.Value, StringComparison.Ordinal))
        {
            return new(ProjectConversationAssignmentOutcome.ValidationFailed, metadata.CorrelationId);
        }

        CurrentProjectReadResult current = await ReadCurrentProjectAsync(
            conversationId,
            tenantId,
            caller,
            metadata.CorrelationId,
            cancellationToken).ConfigureAwait(false);
        if (current.Outcome != ProjectConversationAssignmentOutcome.Accepted)
        {
            return new(current.Outcome, metadata.CorrelationId);
        }

        if (current.ProjectId is not null
            && !string.Equals(current.ProjectId.Value, projectId.Value, StringComparison.Ordinal))
        {
            return new(ProjectConversationAssignmentOutcome.ValidationFailed, metadata.CorrelationId);
        }

        ConversationProjectId? expected = expectedCurrentProjectId is null
            ? null
            : new ConversationProjectId(expectedCurrentProjectId.Value);
        return await DispatchAsync(
            projectId,
            conversationId,
            tenantId,
            caller,
            metadata,
            new ConversationProjectAssignment(
                ConversationProjectAssignmentOperation.Assign,
                new ConversationProjectId(projectId.Value)),
            expected,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<ProjectConversationAssignmentResult> MoveAsync(
        ProjectId targetProjectId,
        ConversationId conversationId,
        ProjectId sourceProjectId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetProjectId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(sourceProjectId);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(metadata);

        return DispatchAsync(
            targetProjectId,
            conversationId,
            tenantId,
            caller,
            metadata,
            new ConversationProjectAssignment(
                ConversationProjectAssignmentOperation.Assign,
                new ConversationProjectId(targetProjectId.Value)),
            new ConversationProjectId(sourceProjectId.Value),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProjectConversationAssignmentResult> UnlinkAsync(
        ProjectId projectId,
        ConversationId conversationId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(metadata);

        return DispatchAsync(
            projectId,
            conversationId,
            tenantId,
            caller,
            metadata,
            new ConversationProjectAssignment(ConversationProjectAssignmentOperation.Clear),
            new ConversationProjectId(projectId.Value),
            cancellationToken);
    }

    private async Task<ProjectConversationAssignmentResult> DispatchAsync(
        ProjectId projectId,
        ConversationId conversationId,
        TenantId tenantId,
        CallerPrincipalId caller,
        ProjectConversationCommandMetadata metadata,
        ConversationProjectAssignment assignment,
        ConversationProjectId? expectedCurrentProjectId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(assignment);

        PartyId actorPartyId;
        try
        {
            actorPartyId = await _actorPartyResolver
                .ResolveActorPartyAsync(tenantId, caller, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new(ProjectConversationAssignmentOutcome.Unavailable, metadata.CorrelationId);
        }

        ReassignConversationProjectCommand command = new(
            new ConversationCommandMetadata(
                SchemaVersion.Current,
                tenantId,
                actorPartyId,
                metadata.CorrelationId,
                metadata.TaskId,
                metadata.IdempotencyKey),
            conversationId,
            assignment,
            expectedCurrentProjectId);

        try
        {
            ConversationClientResult<ConversationCommandAcceptedResult> result = await _conversationClient
                .ReassignConversationProjectAsync(command, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return new(MapFailure(result.StatusCode), metadata.CorrelationId);
            }

            if (result.Value.TenantId != tenantId
                || result.Value.ConversationId != conversationId
                || result.Value.CommandType != ConversationCommandType.ReassignConversationProjectCommand
                || !string.Equals(result.Value.IdempotencyKey, metadata.IdempotencyKey, StringComparison.Ordinal))
            {
                return new(ProjectConversationAssignmentOutcome.Unavailable, metadata.CorrelationId);
            }

            return ProjectConversationAssignmentResult.Accepted(result.Value.CorrelationId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new(ProjectConversationAssignmentOutcome.Unavailable, metadata.CorrelationId);
        }
    }

    private async Task<CurrentProjectReadResult> ReadCurrentProjectAsync(
        ConversationId conversationId,
        TenantId tenantId,
        CallerPrincipalId caller,
        string correlationId,
        CancellationToken cancellationToken)
    {
        GetConversationQuery query = new(
            SchemaVersion.Current,
            tenantId,
            caller.Value,
            correlationId,
            conversationId);

        try
        {
            ConversationClientResult<ConversationDetailResult> result = await _conversationClient
                .GetConversationAsync(query, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return new(MapFailure(result.StatusCode), null);
            }

            if (result.Value.Details is null)
            {
                return result.Value.FreshnessState == ProjectionTrustState.Unavailable
                    ? new(ProjectConversationAssignmentOutcome.Unavailable, null)
                    : new(ProjectConversationAssignmentOutcome.Denied, null);
            }

            if (result.Value.Details.TenantId != tenantId || result.Value.Details.ConversationId != conversationId)
            {
                return new(ProjectConversationAssignmentOutcome.Unavailable, null);
            }

            return new(ProjectConversationAssignmentOutcome.Accepted, result.Value.Details.ProjectId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new(ProjectConversationAssignmentOutcome.Unavailable, null);
        }
    }

    private static ProjectConversationAssignmentOutcome MapFailure(HttpStatusCode? statusCode)
        => statusCode switch
        {
            HttpStatusCode.BadRequest => ProjectConversationAssignmentOutcome.ValidationFailed,
            HttpStatusCode.Conflict => ProjectConversationAssignmentOutcome.Conflict,
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound =>
                ProjectConversationAssignmentOutcome.Denied,
            _ => ProjectConversationAssignmentOutcome.Unavailable,
        };

    private sealed record CurrentProjectReadResult(
        ProjectConversationAssignmentOutcome Outcome,
        ConversationProjectId? ProjectId);
}
