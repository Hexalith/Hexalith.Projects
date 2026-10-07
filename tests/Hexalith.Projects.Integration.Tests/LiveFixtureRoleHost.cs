// <copyright file="LiveFixtureRoleHost.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Net.Http.Json;

using Hexalith.Projects.E2E.Fixtures;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>Runs one real fixture role host on a loopback port with one seeded metadata-only graph.</summary>
internal sealed class LiveFixtureRoleHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private LiveFixtureRoleHost(WebApplication app, Uri baseAddress)
    {
        _app = app;
        BaseAddress = baseAddress;
    }

    /// <summary>Gets the role host's dynamically assigned base address, ending with a slash.</summary>
    public Uri BaseAddress { get; }

    /// <summary>Starts the role host exactly as the AppHost profile does and seeds <paramref name="graph"/>.</summary>
    /// <param name="role">The fixture role.</param>
    /// <param name="graph">The graph to seed through the role's metadata-only ingress.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>The started host.</returns>
    public static async Task<LiveFixtureRoleHost> StartAsync(string role, LiveFixtureGraph graph, CancellationToken cancellationToken)
    {
        WebApplication app = LiveFixtureApplication.Build([], builder =>
        {
            _ = builder.WebHost.UseUrls("http://127.0.0.1:0");
            _ = builder.Logging.SetMinimumLevel(LogLevel.Warning);
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [LiveFixtureApplication.RoleConfigurationKey] = role,
            });
        });
        await app.StartAsync(cancellationToken).ConfigureAwait(true);
        string address = app.Services
            .GetRequiredService<IServer>()
            .Features
            .GetRequiredFeature<IServerAddressesFeature>()
            .Addresses
            .First();
        LiveFixtureRoleHost host = new(app, new Uri(address.EndsWith('/') ? address : address + "/"));

        using HttpClient client = new() { BaseAddress = host.BaseAddress };
        using HttpResponseMessage seeded = await client
            .PostAsJsonAsync("_fixtures/graphs", graph, cancellationToken)
            .ConfigureAwait(true);
        _ = seeded.EnsureSuccessStatusCode();
        return host;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync().ConfigureAwait(true);
        await _app.DisposeAsync().ConfigureAwait(true);
    }
}
