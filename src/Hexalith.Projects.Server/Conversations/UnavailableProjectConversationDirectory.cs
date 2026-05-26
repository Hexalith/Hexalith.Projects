// <copyright file="UnavailableProjectConversationDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;

/// <summary>
/// Fail-closed conversation directory used until a host configures the Conversations typed client.
/// </summary>
internal sealed class UnavailableProjectConversationDirectory : IProjectConversationDirectory
{
    /// <inheritdoc />
    public Task<ProjectConversationsPage> ListForProjectAsync(
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

        return Task.FromResult(ProjectConversationsPage.Empty(projectId, ProjectConversationTrustSignal.Unavailable));
    }
}
