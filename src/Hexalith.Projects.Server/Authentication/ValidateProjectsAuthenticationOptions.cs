// <copyright file="ValidateProjectsAuthenticationOptions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Authentication;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>Validates the Projects bearer authentication contract before the host serves requests.</summary>
public sealed class ValidateProjectsAuthenticationOptions(IHostEnvironment environment)
    : IValidateOptions<ProjectsAuthenticationOptions>
{
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ProjectsAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        bool isDevelopment = _environment.IsDevelopment();
        if (options.AllowAnonymousDevelopment && !isDevelopment)
        {
            return ValidateOptionsResult.Fail(
                "Authentication:JwtBearer:AllowAnonymousDevelopment is valid only in the Development environment.");
        }

        if (isDevelopment && options.AllowAnonymousDevelopment)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            return ValidateOptionsResult.Fail(
                "Authentication:JwtBearer:Authority must be configured unless an explicit Development bypass is enabled.");
        }

        if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out Uri? authority)
            || authority is null
            || string.IsNullOrWhiteSpace(authority.Host))
        {
            return ValidateOptionsResult.Fail("Authentication:JwtBearer:Authority must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail("Authentication:JwtBearer:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Authentication:JwtBearer:Audience must be configured.");
        }

        if (!isDevelopment
            && (!options.RequireHttpsMetadata
                || !string.Equals(authority.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidateOptionsResult.Fail(
                "Production authentication requires an HTTPS OIDC authority and HTTPS metadata discovery.");
        }

        return ValidateOptionsResult.Success;
    }
}
