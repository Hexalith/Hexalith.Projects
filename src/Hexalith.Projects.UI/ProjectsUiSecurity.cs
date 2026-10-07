// <copyright file="ProjectsUiSecurity.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI;

using Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Composes Projects UI interactive security exclusively through FrontComposer seams: the confidential
/// Keycloak authorization-code recipe, the HttpOnly cookie session with server authentication state,
/// and per-user token relay on the Projects client. No Projects-owned authentication plumbing exists.
/// </summary>
public static class ProjectsUiSecurity
{
    /// <summary>Gets the OpenID Connect authority configuration key.</summary>
    public const string AuthoritySettingKey = "Authentication:OpenIdConnect:Authority";

    /// <summary>Gets the confidential OpenID Connect client identifier configuration key.</summary>
    public const string ClientIdSettingKey = "Authentication:OpenIdConnect:ClientId";

    /// <summary>Gets the confidential OpenID Connect client secret configuration key.</summary>
    public const string ClientSecretSettingKey = "Authentication:OpenIdConnect:ClientSecret";

    /// <summary>Gets the single-valued claim that carries the signed-in user's current tenant.</summary>
    public const string TenantClaimType = "eventstore:current-tenant";

    /// <summary>Gets the stable user identifier claim.</summary>
    public const string UserClaimType = "sub";

    /// <summary>
    /// Adds FrontComposer server security and Projects token relay when OpenID Connect is configured.
    /// Absent settings keep the explicit auth-disabled local startup; partial settings fail closed.
    /// </summary>
    /// <param name="services">The UI service collection.</param>
    /// <param name="configuration">The UI configuration.</param>
    /// <param name="projectsClient">The Projects API client builder that must relay the user's token.</param>
    /// <returns><see langword="true"/> when authentication was composed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the settings are incomplete or invalid.</exception>
    public static bool AddProjectsUiSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHttpClientBuilder projectsClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(projectsClient);

        string? authority = Read(configuration, AuthoritySettingKey);
        string? clientId = Read(configuration, ClientIdSettingKey);
        string? clientSecret = Read(configuration, ClientSecretSettingKey);
        if (authority is null && clientId is null && clientSecret is null)
        {
            return false;
        }

        // Name only the missing keys; configured values (including the secret) are never echoed.
        string[] missing = [.. new (string Key, string? Value)[]
            {
                (AuthoritySettingKey, authority),
                (ClientIdSettingKey, clientId),
                (ClientSecretSettingKey, clientSecret),
            }
            .Where(static setting => setting.Value is null)
            .Select(static setting => setting.Key)];
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Projects UI OpenID Connect configuration is incomplete; missing: {string.Join(", ", missing)}.");
        }

        // On Unix a rooted path such as "/realms/x" parses as an absolute file:// URI, so the scheme is
        // checked explicitly: only an HTTP(S) authority can host the OpenID Connect metadata.
        if (!Uri.TryCreate(authority, UriKind.Absolute, out Uri? authorityUri)
            || (authorityUri.Scheme != Uri.UriSchemeHttps && authorityUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException($"{AuthoritySettingKey} must be an absolute HTTP or HTTPS URI.");
        }

        _ = services.AddHexalithFrontComposerServerSecurity(options => options.UseKeycloak(
            authorityUri,
            clientId!,
            clientSecret!,
            tenantClaimType: TenantClaimType,
            userClaimType: UserClaimType));
        _ = projectsClient.AddFrontComposerGatewayAuthorization();
        return true;
    }

    private static string? Read(IConfiguration configuration, string key)
    {
        string? value = configuration[key]?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
