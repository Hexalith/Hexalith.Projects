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

        if (!TryCreateOidcUri(options.Authority, out Uri? authority))
        {
            return ValidateOptionsResult.Fail(
                "Authentication:JwtBearer:Authority must be an absolute HTTP or HTTPS URI without user info, query, or fragment.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail("Authentication:JwtBearer:Issuer must be configured.");
        }

        if (!TryCreateOidcUri(options.Issuer, out Uri? issuer))
        {
            return ValidateOptionsResult.Fail(
                "Authentication:JwtBearer:Issuer must be an absolute HTTP or HTTPS URI without user info, query, or fragment.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Authentication:JwtBearer:Audience must be configured.");
        }

        if (options.RequireHttpsMetadata
            && !string.Equals(authority!.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                "HTTPS metadata discovery requires an HTTPS Authentication:JwtBearer:Authority.");
        }

        if (!isDevelopment
            && (!options.RequireHttpsMetadata
                || !string.Equals(authority!.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(issuer!.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidateOptionsResult.Fail(
                "Production authentication requires HTTPS OIDC authority, issuer, and metadata discovery.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool TryCreateOidcUri(string? value, out Uri? uri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out uri)
            || uri is null
            || string.IsNullOrWhiteSpace(uri.Host)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            uri = null;
            return false;
        }

        return true;
    }
}
