// <copyright file="AuthorizationOrder.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System.Collections.Generic;

/// <summary>Single source of truth for the layered Projects authorization order.</summary>
public static class AuthorizationOrder
{
    /// <summary>Gets the ordered Projects authorization chain.</summary>
    public static IReadOnlyList<AuthorizationLayer> LayeredProjectAuthorization { get; } =
    [
        AuthorizationLayer.JwtValidation,
        AuthorizationLayer.EventStoreClaimTransform,
        AuthorizationLayer.TenantAccessFreshness,
        AuthorizationLayer.ProjectAcl,
        AuthorizationLayer.EventStoreValidator,
        AuthorizationLayer.DaprDenyByDefaultPolicy,
    ];

    /// <summary>Gets the evidence sources used to explain effective authorization.</summary>
    public static IReadOnlyList<string> EffectivePermissions { get; } =
    [
        "authoritative_tenant_context",
        "client_tenant_comparison",
        "tenant_access_projection",
        "project_detail_projection",
        "eventstore_authorization_validator",
        "dapr_deny_by_default_policy",
    ];
}
