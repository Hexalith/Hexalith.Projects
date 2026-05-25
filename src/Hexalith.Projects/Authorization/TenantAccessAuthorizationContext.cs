// <copyright file="TenantAccessAuthorizationContext.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

/// <summary>Inputs for pure tenant-access authorization.</summary>
public sealed record TenantAccessAuthorizationContext(
    string? AuthoritativeTenantId,
    string? PrincipalId,
    string? RequestedTenantId);
