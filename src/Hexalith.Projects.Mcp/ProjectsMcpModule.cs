// <copyright file="ProjectsMcpModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Mcp;
using Hexalith.FrontComposer.Mcp.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Hexalith.Projects MCP adapter registration.
/// MCP tools translate intent to commands/queries over the typed client; they must never
/// reference domain event types or Dapr.
/// </summary>
public static class ProjectsMcpModule
{
    /// <summary>
    /// Gets the module name used in MCP diagnostics.
    /// </summary>
    public static string Name => "Hexalith.Projects.Mcp";

    /// <summary>
    /// Registers Projects MCP descriptors, query readers, command dispatch, and the FrontComposer MCP runtime.
    /// Hosts must register tenant/resource gates before calling this method.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddProjectsMcp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ProjectsMcpResourceReader>();
        services.TryAddScoped<ProjectsMcpCommandService>();
        services.TryAddScoped<IQueryService>(static sp => sp.GetRequiredService<ProjectsMcpResourceReader>());
        services.TryAddScoped<ICommandService>(static sp => sp.GetRequiredService<ProjectsMcpCommandService>());
        services.Configure<FrontComposerMcpOptions>(static options =>
        {
            if (!options.Manifests.Any(static manifest => string.Equals(manifest.SchemaVersion, ProjectsMcpDescriptors.SchemaVersion, StringComparison.Ordinal)))
            {
                options.Manifests.Add(ProjectsMcpDescriptors.Manifest);
            }
        });

        return services.AddFrontComposerMcp();
    }

    /// <summary>
    /// Maps the Projects MCP endpoint through the FrontComposer MCP endpoint adapter.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapProjectsMcp(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapFrontComposerMcp();
        return endpoints;
    }
}
