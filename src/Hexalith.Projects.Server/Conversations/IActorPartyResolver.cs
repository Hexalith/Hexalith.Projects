// <copyright file="IActorPartyResolver.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Resolves the authenticated Projects caller to the stable Conversations Party identity used for command attribution.
/// </summary>
public interface IActorPartyResolver
{
    /// <summary>
    /// Resolves an actor Party id from server-derived tenant and principal context.
    /// </summary>
    /// <param name="tenantId">The authoritative tenant selected by Projects authentication and authorization.</param>
    /// <param name="principalId">The authenticated principal identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stable Conversations Party id to place in command metadata.</returns>
    ValueTask<PartyId> ResolveActorPartyAsync(
        TenantId tenantId,
        CallerPrincipalId principalId,
        CancellationToken cancellationToken = default);
}
