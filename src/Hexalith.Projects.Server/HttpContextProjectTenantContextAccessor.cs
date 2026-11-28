// <copyright file="HttpContextProjectTenantContextAccessor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

using Hexalith.Projects.Authorization;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Minimal <see cref="IProjectTenantContextAccessor"/> that derives the authoritative tenant and
/// principal from the authenticated <see cref="ClaimsPrincipal"/> on the current request (Story 1.4).
/// The tenant comes from a claim only — never from a request payload, header, or query parameter. The
/// full EventStore claim-transform / <c>TenantAccessProjection</c> chain is Story 1.6.
/// Only the primary authenticated identity supplies authority, matching the claims transformer;
/// supplementary identities cannot contribute tenant, principal, or permission claims.
/// </summary>
public sealed class HttpContextProjectTenantContextAccessor(IHttpContextAccessor httpContextAccessor) : IProjectTenantContextAccessor
{
    private const string EventStorePermissionClaimType = "eventstore:permission";

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc/>
    public string? AuthoritativeTenantId
    {
        get
        {
            ClaimsIdentity? identity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
            return identity?.IsAuthenticated == true ? ProjectTenantClaims.Resolve(identity) : null;
        }
    }

    /// <inheritdoc/>
    public string? PrincipalId
    {
        get
        {
            ClaimsIdentity? identity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
            if (identity?.IsAuthenticated != true)
            {
                return null;
            }

            string? principal = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? identity.FindFirst("sub")?.Value;
            return string.IsNullOrWhiteSpace(principal) ? null : principal;
        }
    }

    /// <inheritdoc/>
    public EventStoreClaimTransformEvidence GetClaimTransformEvidence(string actionToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionToken);

        ClaimsIdentity? identity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
        if (identity?.IsAuthenticated != true)
        {
            return EventStoreClaimTransformEvidence.Missing();
        }

        string? tenant = ProjectTenantClaims.Resolve(identity);
        string? principal = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? identity.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(principal))
        {
            return EventStoreClaimTransformEvidence.MalformedEvidence();
        }

        IEnumerable<string> permissions = identity.FindAll(EventStorePermissionClaimType).Select(static claim => claim.Value);
        return EventStoreClaimTransformEvidence.Allowed(tenant, principal, permissions);
    }
}
