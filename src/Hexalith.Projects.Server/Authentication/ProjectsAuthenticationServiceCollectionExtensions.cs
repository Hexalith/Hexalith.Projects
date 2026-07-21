// <copyright file="ProjectsAuthenticationServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configured.Issuer,
                    ValidateAudience = true,
                    ValidAudience = configured.Audience,
                    ValidateIssuerSigningKey = true,
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

    private static ProjectsAuthenticationOptions ReadConfiguration(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        // Use the same configuration binder as the registered options pipeline. A hand-written
        // parser can otherwise activate a different authentication mode than ValidateOnStart sees.
        return section.Get<ProjectsAuthenticationOptions>() ?? new ProjectsAuthenticationOptions();
    }
}
