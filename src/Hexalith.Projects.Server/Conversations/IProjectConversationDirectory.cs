// <copyright file="IProjectConversationDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;

/// <summary>
/// Projects-owned ACL for metadata-only conversation references belonging to a project.
/// </summary>
public interface IProjectConversationDirectory
{
    /// <summary>
    /// Lists visible conversation references for the requested project and tenant scope.
    /// </summary>
    /// <param name="projectId">The requested project identifier.</param>
    /// <param name="tenantId">The authoritative tenant scope.</param>
    /// <param name="caller">The authenticated caller principal.</param>
    /// <param name="page">The bounded page request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Projects-shaped page of metadata-only conversation references.</returns>
    Task<ProjectConversationsPage> ListForProjectAsync(
        ProjectId projectId,
        ConversationTenantId tenantId,
        CallerPrincipalId caller,
        PageRequest page,
        CancellationToken cancellationToken = default);
}
