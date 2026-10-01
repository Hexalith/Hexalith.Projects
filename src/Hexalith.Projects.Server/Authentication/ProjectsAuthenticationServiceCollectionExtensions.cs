// <copyright file="ProjectsAuthenticationServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.EventStore.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

/// <summary>Registers the Projects authentication and authorization composition.</summary>
public static class ProjectsAuthenticationServiceCollectionExtensions
{
    /// <summary>Registers validated bearer authentication, authorization, and the explicit local bypass.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The host environment.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddProjectsAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        IConfigurationSection section = configuration.GetSection(ProjectsAuthenticationOptions.SectionName);
        _ = services
            .AddOptions<ProjectsAuthenticationOptions>()
            .Bind(section)
            .ValidateOnStart();
        _ = services.AddSingleton<IValidateOptions<ProjectsAuthenticationOptions>, ValidateProjectsAuthenticationOptions>();
        _ = services.AddAuthorization();

        ProjectsAuthenticationOptions configured = ReadConfiguration(section);
        if (environment.IsDevelopment() && configured.AllowAnonymousDevelopment)
        {
            return services;
        }

        _ = services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configured.Authority;
                options.RequireHttpsMetadata = configured.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = environment.IsDevelopment();
                options.Events.OnTokenValidated = static context =>
                {
                    ClaimsPrincipal? principal = context.Principal;
                    Claim[] subjects = principal?.FindAll("sub").ToArray() ?? [];
                    string? actor = subjects.Length == 1 ? subjects[0].Value : null;
                    if (principal is null
                        || string.IsNullOrWhiteSpace(actor)
                        || !HasStringSubject(context.SecurityToken)
                        || principal.FindAll(ClaimTypes.NameIdentifier)
                            .Any(claim => !string.Equals(claim.Value, actor, StringComparison.Ordinal))
                        || (principal.FindAll("act").Any(static claim => !string.IsNullOrWhiteSpace(claim.Value))
                            && DualPrincipalClaimsHelper.Extract(principal, actor).DelegationId is null))
                    {
                        context.Fail("The access token identity is invalid.");
                    }

                    return Task.CompletedTask;
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configured.Issuer,
                    // Discovery must not add a second accepted issuer when metadata drifts from
                    // the deployment-owned issuer contract.
                    IssuerValidator = (issuer, _, _) => string.Equals(issuer, configured.Issuer, StringComparison.Ordinal)
                        ? issuer
                        : throw new SecurityTokenInvalidIssuerException("The access token issuer is invalid."),
                    ValidateAudience = true,
                    IgnoreTrailingSlashWhenValidatingAudience = false,
                    ValidAudience = configured.Audience,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = environment.IsDevelopment()
                        ? null
                        :
                        [
                            SecurityAlgorithms.RsaSha256,
                            SecurityAlgorithms.RsaSha384,
                            SecurityAlgorithms.RsaSha512,
                            SecurityAlgorithms.RsaSsaPssSha256,
                            SecurityAlgorithms.RsaSsaPssSha384,
                            SecurityAlgorithms.RsaSsaPssSha512,
                            SecurityAlgorithms.EcdsaSha256,
                            SecurityAlgorithms.EcdsaSha384,
                            SecurityAlgorithms.EcdsaSha512,
                        ],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        return services;
    }

    /// <summary>Determines whether the explicit Development-only anonymous bypass is active.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The host environment.</param>
    /// <returns><see langword="true"/> only when the bypass is explicit and Development is active.</returns>
    public static bool IsAnonymousDevelopmentBypass(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        IConfigurationSection section = configuration.GetSection(ProjectsAuthenticationOptions.SectionName);
        ProjectsAuthenticationOptions options = ReadConfiguration(section);
        return environment.IsDevelopment() && options.AllowAnonymousDevelopment;
    }

    private static bool HasStringSubject(SecurityToken token)
    {
        // IdentityModel normalizes numeric sub values into string claims. Inspect only the
        // already-validated payload so malformed subject types cannot become actor identities.
        string? encodedPayload = token switch
        {
            JsonWebToken jsonToken => jsonToken.EncodedPayload,
            JwtSecurityToken jwtToken => jwtToken.RawPayload,
            _ => null,
        };
        if (encodedPayload is null)
        {
            return false;
        }

        using JsonDocument payload = JsonDocument.Parse(Base64UrlEncoder.Decode(encodedPayload));
        return payload.RootElement.TryGetProperty("sub", out JsonElement subject)
            && subject.ValueKind == JsonValueKind.String;
    }

    private static ProjectsAuthenticationOptions ReadConfiguration(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        // Use the same configuration binder as the registered options pipeline. A hand-written
        // parser can otherwise activate a different authentication mode than ValidateOnStart sees.
        return section.Get<ProjectsAuthenticationOptions>() ?? new ProjectsAuthenticationOptions();
    }
}
