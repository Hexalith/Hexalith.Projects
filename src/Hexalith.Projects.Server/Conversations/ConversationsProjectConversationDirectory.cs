// <copyright file="ConversationsProjectConversationDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using System;
using System.Net;

using Hexalith.Conversations.Client;
using ConversationProjectId = Hexalith.Conversations.Contracts.Identifiers.ProjectId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;

/// <summary>
/// Projects ACL adapter over the Conversations supported typed client.
/// </summary>
public sealed class ConversationsProjectConversationDirectory(IConversationClient conversationClient) : IProjectConversationDirectory
{
    private readonly IConversationClient _conversationClient = conversationClient ?? throw new ArgumentNullException(nameof(conversationClient));

    /// <inheritdoc />
    public async Task<ProjectConversationsPage> ListForProjectAsync(
        ProjectId projectId,
        ConversationTenantId tenantId,
        CallerPrincipalId caller,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(page);

        ListConversationsQuery query = new(
            SchemaVersion.Current,
            tenantId,
            caller.Value,
            Guid.NewGuid().ToString("N"),
            new ConversationListFilterV1(ProjectId: new ConversationProjectId(projectId.Value)),
            new ConversationPageRequest(page.PageSize, page.ContinuationCursor));

        try
        {
            ConversationClientResult<ConversationListResult> result = await _conversationClient
                .ListConversationsAsync(query, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return ProjectConversationsPage.Empty(projectId, ToFailureSignal(result.StatusCode));
            }

            return ProjectConversationTranslator.ToPage(projectId, tenantId, result.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProjectConversationsPage.Empty(projectId, ProjectConversationTrustSignal.Unavailable);
        }
    }

    private static ProjectConversationTrustSignal ToFailureSignal(HttpStatusCode? statusCode)
        => statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound
            ? ProjectConversationTrustSignal.Forbidden
            : ProjectConversationTrustSignal.Unavailable;
}
