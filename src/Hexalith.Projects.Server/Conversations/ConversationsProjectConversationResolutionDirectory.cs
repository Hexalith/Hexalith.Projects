// <copyright file="ConversationsProjectConversationResolutionDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using System.Net;

using Hexalith.Conversations.Client;
using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;

/// <summary>
/// Projects read ACL adapter over the Conversations typed client for the Story 4.2 single-conversation
/// metadata read. It is the only Projects code allowed to touch <c>Hexalith.Conversations.*</c> types
/// for this read path; it re-checks tenant/conversation scope per the fail-closed precedent set by
/// <c>ConversationsProjectConversationAssignmentDirectory.ReadCurrentProjectAsync</c> and maps the
/// upstream trust/freshness posture onto the shared <see cref="ReferenceState"/> vocabulary.
/// </summary>
public sealed class ConversationsProjectConversationResolutionDirectory(IConversationClient conversationClient)
    : IProjectConversationResolutionDirectory
{
    private readonly IConversationClient _conversationClient = conversationClient ?? throw new ArgumentNullException(nameof(conversationClient));

    /// <inheritdoc />
    public async Task<ConversationResolutionMetadata> ReadConversationMetadataAsync(
        ConversationId conversationId,
        ConversationTenantId tenantId,
        CallerPrincipalId caller,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(caller);

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
                return ConversationResolutionMetadata.FailClosed(conversationId.Value, MapFailure(result.StatusCode));
            }

            ConversationDetailResult detail = result.Value;
            if (detail.Details is null)
            {
                return ConversationResolutionMetadata.FailClosed(conversationId.Value, ToReferenceState(detail.FreshnessState));
            }

            // Per-row scope re-check (Ordinal): never surface evidence for a conversation that escaped
            // the requested tenant/identity scope — collapse to a fail-closed Unavailable record.
            if (detail.Details.TenantId != tenantId || detail.Details.ConversationId != conversationId)
            {
                return ConversationResolutionMetadata.FailClosed(conversationId.Value, ReferenceState.Unavailable);
            }

            // A project-less conversation carries no explicit ProjectId; a response-scoped hydration
            // pointer is the only soft hint and is used only when an explicit assignment is absent.
            string? linkedProjectId = detail.Details.ProjectId?.Value
                ?? detail.Details.ProjectHydration?.ProjectId.Value;

            return new ConversationResolutionMetadata(
                conversationId.Value,
                linkedProjectId,
                detail.Details.Label,
                ToReferenceState(detail.Details.Freshness.FreshnessState));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return ConversationResolutionMetadata.FailClosed(conversationId.Value, ReferenceState.Unavailable);
        }
    }

    // Maps an unsuccessful upstream HTTP status onto a fail-closed reference state. Authentication /
    // authorization / not-found all collapse to Unauthorized (externally indistinguishable); anything
    // else is treated as a transient Unavailable.
    private static ReferenceState MapFailure(HttpStatusCode? statusCode)
        => statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound => ReferenceState.Unauthorized,
            _ => ReferenceState.Unavailable,
        };

    // Translates the upstream Conversations projection trust posture onto the shared Projects
    // ReferenceState vocabulary. Only a fully current posture yields an Included (positive) signal;
    // every degraded posture fails closed to a non-Included state the engine surfaces as an exclusion.
    private static ReferenceState ToReferenceState(ProjectionTrustState state)
    {
        if (state == ProjectionTrustState.Current)
        {
            return ReferenceState.Included;
        }

        if (state == ProjectionTrustState.Stale)
        {
            return ReferenceState.Stale;
        }

        if (state == ProjectionTrustState.Forbidden)
        {
            return ReferenceState.Unauthorized;
        }

        if (state == ProjectionTrustState.Redacted)
        {
            return ReferenceState.Excluded;
        }

        // Rebuilding / Unavailable / any unknown future state all fail closed to Unavailable.
        return ReferenceState.Unavailable;
    }
}
