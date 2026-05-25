// <copyright file="HttpContextProjectTenantContextAccessor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Minimal <see cref="IProjectTenantContextAccessor"/> that derives the authoritative tenant and
/// principal from the authenticated <see cref="ClaimsPrincipal"/> on the current request (Story 1.4).
/// The tenant comes from a claim only — never from a request payload, header, or query parameter. The
/// full EventStore claim-transform / <c>TenantAccessProjection</c> chain is Story 1.6.
/// </summary>
public sealed class HttpContextProjectTenantContextAccessor(IHttpContextAccessor httpContextAccessor) : IProjectTenantContextAccessor
{
    // Canonical tenant claim type carried after claim transformation; matches the Hexalith convention.
    private const string TenantClaimType = "tenantId";

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc/>
    public string? AuthoritativeTenantId
    {
        get
        {
            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            string? tenant = user.FindFirstValue(TenantClaimType);
            return string.IsNullOrWhiteSpace(tenant) ? null : tenant;
        }
    }

    /// <inheritdoc/>
    public string? PrincipalId
    {
        get
        {
            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            string? principal = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            return string.IsNullOrWhiteSpace(principal) ? null : principal;
        }
    }
}
