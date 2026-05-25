// <copyright file="ProjectTenantPrincipalEvidence.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

/// <summary>Projected tenant membership evidence for a principal.</summary>
/// <param name="PrincipalId">The authenticated principal identifier.</param>
/// <param name="Role">The Tenants role name.</param>
public sealed record ProjectTenantPrincipalEvidence(string PrincipalId, string Role);
