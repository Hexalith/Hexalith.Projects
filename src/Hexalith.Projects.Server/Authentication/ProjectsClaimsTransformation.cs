// <copyright file="ProjectsClaimsTransformation.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;

/// <summary>Normalizes authenticated claims into the EventStore evidence claim names used by Projects.</summary>
public sealed class ProjectsClaimsTransformation : IClaimsTransformation
{
    private const string TenantClaimType = "tenantId";
    private const string TenantsClaimType = "tenants";
    private const string TenantIdClaimType = "tenant_id";
    private const string TidClaimType = "tid";
    private const string PermissionsClaimType = "permissions";
    private const string EventStoreTenantClaimType = "eventstore:tenant";
    private const string EventStorePermissionClaimType = "eventstore:permission";

    /// <inheritdoc/>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(principal);
        }

        if (!identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        if (identity.FindFirst(EventStoreTenantClaimType) is null)
        {
            AddTenantClaims(identity);
        }

        string? principalId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? identity.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(principalId) && identity.FindFirst(ClaimTypes.NameIdentifier) is null)
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principalId));
        }

        if (identity.FindFirst(EventStorePermissionClaimType) is null)
        {
            AddClaimsFromJwt(identity, PermissionsClaimType, EventStorePermissionClaimType);
        }

        return Task.FromResult(principal);
    }

    private static void AddTenantClaims(ClaimsIdentity identity)
    {
        string? tenant = identity.FindFirst(TenantClaimType)?.Value
            ?? identity.FindFirst(TenantIdClaimType)?.Value
            ?? identity.FindFirst(TidClaimType)?.Value;

        if (!string.IsNullOrWhiteSpace(tenant))
        {
            identity.AddClaim(new Claim(EventStoreTenantClaimType, tenant));
            return;
        }

        AddClaimsFromJwt(identity, TenantsClaimType, EventStoreTenantClaimType);
    }

    private static void AddClaimsFromJwt(
        ClaimsIdentity identity,
        string sourceClaimType,
        string targetClaimType)
    {
        foreach (Claim sourceClaim in identity.FindAll(sourceClaimType).ToArray())
        {
            foreach (string value in SplitClaimValues(sourceClaim.Value))
            {
                if (!identity.HasClaim(targetClaimType, value))
                {
                    identity.AddClaim(new Claim(targetClaimType, value));
                }
            }
        }
    }

    private static IEnumerable<string> SplitClaimValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        string trimmed = value.Trim();
        if (trimmed.StartsWith('['))
        {
            string[]? items = null;
            try
            {
                items = JsonSerializer.Deserialize<string[]>(trimmed);
            }
            catch (JsonException)
            {
                items = null;
            }

            if (items is not null)
            {
                foreach (string item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        yield return item.Trim();
                    }
                }

                yield break;
            }
        }

        foreach (string part in trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return part;
        }
    }
}
