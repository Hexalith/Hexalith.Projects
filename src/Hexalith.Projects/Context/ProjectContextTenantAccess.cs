// <copyright file="ProjectContextTenantAccess.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System;

using Hexalith.Projects.Authorization;

/// <summary>
/// Typed wrapper around the Story 1.6 <see cref="TenantAccessAuthorizationResult"/> consumed by
/// <c>ProjectContextInclusionPolicy</c> (Story 3.1). The policy never re-evaluates tenant access —
/// it inspects this result and maps it onto the assembly-level outcome and freshness per the
/// AC 6 decision matrix.
/// </summary>
/// <param name="Result">The Story 1.6 tenant-access authorization decision.</param>
public sealed record ProjectContextTenantAccess(TenantAccessAuthorizationResult Result)
{
    /// <summary>Gets the Story 1.6 tenant-access authorization decision.</summary>
    public TenantAccessAuthorizationResult Result { get; } = Result ?? throw new ArgumentNullException(nameof(Result));
}
