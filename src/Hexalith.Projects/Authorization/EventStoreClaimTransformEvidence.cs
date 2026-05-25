// <copyright file="EventStoreClaimTransformEvidence.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Metadata-only evidence produced by the EventStore claim-transform layer.</summary>
public sealed record EventStoreClaimTransformEvidence(
    string? TenantId,
    string? PrincipalId,
    IReadOnlySet<string> PermissionTokens,
    bool IsPresent,
    bool Malformed)
{
    /// <summary>Creates present, non-malformed evidence.</summary>
    public static EventStoreClaimTransformEvidence Allowed(
        string? tenantId,
        string? principalId,
        IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        return new(
            tenantId,
            principalId,
            permissions
                .Where(static permission => !string.IsNullOrWhiteSpace(permission))
                .Select(static permission => permission.Trim())
                .ToHashSet(StringComparer.Ordinal),
            IsPresent: true,
            Malformed: false);
    }

    /// <summary>Creates absent evidence.</summary>
    public static EventStoreClaimTransformEvidence Missing()
        => new(null, null, new HashSet<string>(StringComparer.Ordinal), IsPresent: false, Malformed: false);

    /// <summary>Creates malformed evidence.</summary>
    public static EventStoreClaimTransformEvidence MalformedEvidence()
        => new(null, null, new HashSet<string>(StringComparer.Ordinal), IsPresent: true, Malformed: true);

    /// <summary>Checks whether this evidence includes the required action token.</summary>
    public bool HasPermissionFor(string actionToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionToken);
        return PermissionTokens.Contains(actionToken.Trim());
    }
}
