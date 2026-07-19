// <copyright file="ProjectsAuthenticationOptions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Authentication;

/// <summary>
/// Defines the bearer authentication configuration consumed by the Projects host.
/// </summary>
public sealed class ProjectsAuthenticationOptions
{
    /// <summary>
    /// The configuration section containing the Projects bearer settings.
    /// </summary>
    public const string SectionName = "Authentication:JwtBearer";

    /// <summary>Gets the OIDC authority used to discover signing keys.</summary>
    public string? Authority { get; init; }

    /// <summary>Gets the issuer required in validated access tokens.</summary>
    public string? Issuer { get; init; }

    /// <summary>Gets the audience required in validated access tokens.</summary>
    public string? Audience { get; init; }

    /// <summary>Gets a value indicating whether OIDC metadata may be fetched without HTTPS.</summary>
    public bool RequireHttpsMetadata { get; init; } = true;

    /// <summary>Gets a value indicating whether anonymous startup is allowed for Development diagnostics.</summary>
    public bool AllowAnonymousDevelopment { get; init; }
}
