// <copyright file="IProjectTenantContextAccessor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Authorization;

/// <summary>
/// Minimal authenticated-tenant context accessor (Story 1.4). The authoritative tenant and principal
/// come from authenticated claims / EventStore claim-transform <b>only</b> — never from a request
/// payload, header, or query parameter. Mirrors the Folders <c>ITenantContextAccessor</c> shape but
/// stripped to the minimum the create tracer bullet needs; the full claim-transform/projection chain
/// is Story 1.6.
/// </summary>
public interface IProjectTenantContextAccessor
{
    /// <summary>Gets the authoritative tenant identifier derived from authenticated claims, or null when unauthenticated.</summary>
    string? AuthoritativeTenantId { get; }

    /// <summary>Gets the authenticated principal identifier, or null when unauthenticated.</summary>
    string? PrincipalId { get; }

    /// <summary>Gets metadata-only EventStore claim-transform evidence for the required action.</summary>
    /// <param name="actionToken">The action token being authorized.</param>
    /// <returns>The claim-transform evidence, or missing/malformed evidence when unavailable.</returns>
    EventStoreClaimTransformEvidence GetClaimTransformEvidence(string actionToken);
}
