// <copyright file="DevelopmentProjectQueryTenantContextAccessor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Queries;

using Hexalith.Projects.Authorization;

/// <summary>Envelope-backed authority used only by the explicit Development diagnostics bypass.</summary>
internal sealed class DevelopmentProjectQueryTenantContextAccessor(
    string tenantId,
    string principalId) : IProjectTenantContextAccessor
{
    /// <inheritdoc/>
    public string? AuthoritativeTenantId { get; } = tenantId;

    /// <inheritdoc/>
    public string? PrincipalId { get; } = principalId;

    /// <inheritdoc/>
    public EventStoreClaimTransformEvidence GetClaimTransformEvidence(string actionToken)
        => string.Equals(actionToken, ProjectAuthorizationGate.ReadProjectAction, StringComparison.Ordinal)
            ? EventStoreClaimTransformEvidence.Allowed(AuthoritativeTenantId!, PrincipalId!, [actionToken])
            : EventStoreClaimTransformEvidence.Missing();
}
