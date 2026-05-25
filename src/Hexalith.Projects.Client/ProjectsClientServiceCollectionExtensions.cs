// <copyright file="ProjectsClientServiceCollectionExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client;

using Hexalith.Projects.Client.Generated;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// Disambiguate the generated type from the enclosing "Hexalith.Projects.Client" namespace.
using GeneratedProjectsClient = Hexalith.Projects.Client.Generated.Client;

/// <summary>
/// Extension methods that register the Hexalith.Projects typed SDK client (<see cref="IClient"/>) as a
/// typed <see cref="System.Net.Http.HttpClient"/> in the dependency injection container.
/// </summary>
/// <remarks>
/// Authentication is deliberately out of scope: the returned <see cref="IHttpClientBuilder"/> lets callers
/// attach a bearer-token <see cref="System.Net.Http.DelegatingHandler"/> (recommended) without this module
/// taking a dependency on any particular token-acquisition strategy.
/// </remarks>
public static class ProjectsClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Projects typed client, binding <see cref="ProjectsClientOptions"/> from the
    /// <see cref="ProjectsClientOptions.DefaultConfigurationSectionName"/> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> so callers can chain message handlers (for example, a bearer-token handler).</returns>
    public static IHttpClientBuilder AddProjectsClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddOptions<ProjectsClientOptions>()
            .BindConfiguration(ProjectsClientOptions.DefaultConfigurationSectionName);

        return services.AddConfiguredProjectsClient();
    }

    /// <summary>
    /// Registers the Projects typed client with explicit <see cref="ProjectsClientOptions"/> configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">A delegate that configures <see cref="ProjectsClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> so callers can chain message handlers (for example, a bearer-token handler).</returns>
    public static IHttpClientBuilder AddProjectsClient(
        this IServiceCollection services,
        Action<ProjectsClientOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<ProjectsClientOptions>();
        _ = services.Configure(configureOptions);

        return services.AddConfiguredProjectsClient();
    }

    private static IHttpClientBuilder AddConfiguredProjectsClient(this IServiceCollection services)
    {
        // Fail fast (at first resolve) when the transport endpoint is missing or relative.
        services
            .AddOptions<ProjectsClientOptions>()
            .Validate(
                static options => options.BaseAddress is not null,
                $"{nameof(ProjectsClientOptions)}.{nameof(ProjectsClientOptions.BaseAddress)} must be configured.")
            .Validate(
                static options => options.BaseAddress is null || options.BaseAddress.IsAbsoluteUri,
                $"{nameof(ProjectsClientOptions)}.{nameof(ProjectsClientOptions.BaseAddress)} must be an absolute URI.");

        return services.AddHttpClient<IClient, GeneratedProjectsClient>(static (serviceProvider, httpClient) =>
        {
            ProjectsClientOptions options = serviceProvider
                .GetRequiredService<IOptions<ProjectsClientOptions>>()
                .Value;
            httpClient.BaseAddress = options.BaseAddress;
        });
    }
}
