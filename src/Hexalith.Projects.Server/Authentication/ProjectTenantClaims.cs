// <copyright file="ProjectTenantClaims.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System.Security.Claims;
using System.Text.Json;

/// <summary>Normalizes Tenant claims and resolves the single Tenant bound to a retained v1 request.</summary>
internal static class ProjectTenantClaims
{
    private const string CurrentTenantClaimType = "eventstore:current-tenant";
    private const string TenantMembershipClaimType = "eventstore:tenant";
    private const string RawMembershipClaimType = "tenants";
    private const string RawTenantClaimType = "tenantId";
    private const string RawTenantIdClaimType = "tenant_id";
    private const string RawTidClaimType = "tid";
    private const string MalformedTenantClaimType = "hexalith:projects:malformed-tenant-evidence";
    private const string PromotedMembershipProperty = "Hexalith.Projects.PromotedTenantMembership";

    private static readonly HashSet<string> TenantClaimTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        CurrentTenantClaimType,
        TenantMembershipClaimType,
        RawMembershipClaimType,
        RawTenantClaimType,
        RawTenantIdClaimType,
        RawTidClaimType,
    };

    /// <summary>Retains malformed Tenant evidence that JWT claim materialization can omit.</summary>
    internal static void PreserveMalformedJwtClaims(ClaimsIdentity identity, JsonElement payload)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in payload.EnumerateObject())
        {
            if (TenantClaimTypes.Contains(property.Name) && (!seen.Add(property.Name) || !IsJwtTenantClaim(property.Value)))
            {
                identity.AddClaim(new Claim(MalformedTenantClaimType, string.Empty));
                return;
            }
        }
    }

    /// <summary>Promotes all distinct raw Tenant values when the identity has no canonical memberships.</summary>
    internal static void AddTenantClaims(ClaimsIdentity identity)
    {
        if (identity.HasClaim(MalformedTenantClaimType, string.Empty) || identity.FindAll(TenantMembershipClaimType).Any())
        {
            return;
        }

        _ = ReadRawClaims(identity, out HashSet<string> aliases, out HashSet<string> memberships);
        aliases.UnionWith(memberships);
        foreach (string tenant in aliases)
        {
            Claim claim = new(TenantMembershipClaimType, tenant);
            claim.Properties[PromotedMembershipProperty] = string.Empty;
            identity.AddClaim(claim);
        }
    }

    /// <summary>Returns the unique concrete Tenant, or <see langword="null"/> when the evidence is ambiguous or malformed.</summary>
    internal static string? Resolve(ClaimsIdentity identity)
    {
        Claim[] currentClaims = identity.FindAll(CurrentTenantClaimType).ToArray();
        Claim[] canonicalClaims = identity.FindAll(TenantMembershipClaimType).ToArray();
        HashSet<string> current = new(StringComparer.Ordinal);
        HashSet<string> canonical = new(StringComparer.Ordinal);
        if (identity.HasClaim(MalformedTenantClaimType, string.Empty)
            || !ReadScalarClaims(currentClaims, current)
            || !ReadScalarClaims(canonicalClaims, canonical)
            || !ReadRawClaims(identity, out HashSet<string> aliases, out HashSet<string> memberships))
        {
            return null;
        }

        if (currentClaims.Length == 0)
        {
            aliases.UnionWith(memberships);
            aliases.UnionWith(canonical);
            return GetConcreteTenant(aliases);
        }

        string? tenant = GetConcreteTenant(current);
        if (tenant is null || aliases.Count > 1)
        {
            return null;
        }

        if (canonicalClaims.Any(static claim => !claim.Properties.ContainsKey(PromotedMembershipProperty)))
        {
            return canonical.Contains(tenant) ? tenant : null;
        }

        // Promotion combines the raw sources. Their original values still govern an explicit selection.
        return (aliases.Count == 0 || aliases.Contains(tenant)) && (memberships.Count == 0 || memberships.Contains(tenant))
            ? tenant
            : null;
    }

    private static bool IsJwtTenantClaim(JsonElement value)
        => value.ValueKind == JsonValueKind.String
            || value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0
                && value.EnumerateArray().All(static item => item.ValueKind == JsonValueKind.String);

    private static bool ReadRawClaims(ClaimsIdentity identity, out HashSet<string> aliases, out HashSet<string> memberships)
    {
        aliases = new(StringComparer.Ordinal);
        memberships = new(StringComparer.Ordinal);
        bool valid = ReadScalarClaims(identity.FindAll(RawTenantClaimType), aliases);
        valid &= ReadScalarClaims(identity.FindAll(RawTenantIdClaimType), aliases);
        valid &= ReadScalarClaims(identity.FindAll(RawTidClaimType), aliases);
        foreach (Claim claim in identity.FindAll(RawMembershipClaimType))
        {
            valid &= ReadMembershipClaim(claim, memberships);
        }

        return valid;
    }

    private static bool ReadScalarClaims(IEnumerable<Claim> claims, HashSet<string> values)
    {
        bool valid = true;
        foreach (Claim claim in claims)
        {
            valid &= claim.ValueType == ClaimValueTypes.String && AddTenantId(claim.Value, values);
        }

        return valid;
    }

    private static bool ReadMembershipClaim(Claim claim, HashSet<string> memberships)
    {
        if (claim.ValueType != ClaimValueTypes.String)
        {
            return false;
        }

        string value = claim.Value.Trim();
        if (value.StartsWith('{') || value.StartsWith('"'))
        {
            return false;
        }

        string?[]? values;
        if (value.StartsWith('['))
        {
            try
            {
                values = JsonSerializer.Deserialize<string?[]>(value);
            }
            catch (JsonException)
            {
                return false;
            }
        }
        else
        {
            values = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        if (values is not { Length: > 0 } || values.Any(static item => item is null))
        {
            return false;
        }

        bool valid = true;
        foreach (string? item in values)
        {
            valid &= AddTenantId(item, memberships);
        }

        return valid;
    }

    private static bool AddTenantId(string? value, HashSet<string> tenants)
    {
        string? tenant = value?.Trim();
        if (string.IsNullOrEmpty(tenant) || tenant[0] is '[' or '{' or '"' || tenant.Any(char.IsWhiteSpace))
        {
            return false;
        }

        tenants.Add(tenant);
        return true;
    }

    private static string? GetConcreteTenant(HashSet<string> tenants)
    {
        if (tenants.Count != 1)
        {
            return null;
        }

        string tenant = tenants.Single();
        return tenant is "*" or ProjectsServerModule.ReservedSystemTenant || tenant.StartsWith("platform:", StringComparison.Ordinal)
            ? null
            : tenant;
    }
}
