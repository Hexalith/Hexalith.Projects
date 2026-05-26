// <copyright file="DeterministicActorPartyResolver.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

using System.Security.Cryptography;
using System.Text;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Integration mapping from authenticated Projects principal to Conversations actor Party id.
/// </summary>
/// <remarks>
/// Until a dedicated Parties lookup is introduced, this resolver deliberately derives a stable,
/// non-reversible Party id from the authoritative tenant id and authenticated principal id. The
/// mapping is encapsulated here so a future Parties-backed resolver can replace it without changing
/// endpoint request contracts. Request payloads never provide actor authority.
/// </remarks>
public sealed class DeterministicActorPartyResolver : IActorPartyResolver
{
    /// <inheritdoc />
    public ValueTask<PartyId> ResolveActorPartyAsync(
        TenantId tenantId,
        CallerPrincipalId principalId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(principalId);

        cancellationToken.ThrowIfCancellationRequested();

        byte[] material = Encoding.UTF8.GetBytes(tenantId.Value + "\u001F" + principalId.Value);
        string hash = Convert.ToHexString(SHA256.HashData(material)).ToLowerInvariant();
        return ValueTask.FromResult(new PartyId("projects-actor-" + hash[..32]));
    }
}
