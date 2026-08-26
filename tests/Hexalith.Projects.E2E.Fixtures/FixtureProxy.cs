// <copyright file="FixtureProxy.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

using System.Net.Http.Json;

/// <summary>Coordinates graph state across the three role-specific sibling fixtures.</summary>
public sealed class FixtureProxy(HttpClient httpClient, IConfiguration configuration)
{
    private static readonly string[] EndpointKeys =
    [
        "FixtureEndpoints:Conversations",
        "FixtureEndpoints:Folders",
        "FixtureEndpoints:Memories",
    ];

    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <summary>Seeds all sibling roles and compensates already-seeded roles on failure.</summary>
    public async Task SeedAsync(LiveFixtureGraph graph, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);
        List<Uri> seeded = [];
        try
        {
            foreach (Uri endpoint in Endpoints())
            {
                using HttpResponseMessage response = await _httpClient
                    .PostAsJsonAsync(new Uri(endpoint, "/_fixtures/graphs"), graph, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                seeded.Add(endpoint);
            }
        }
        catch
        {
            foreach (Uri endpoint in seeded.AsEnumerable().Reverse())
            {
                _ = await _httpClient
                    .DeleteAsync(new Uri(endpoint, $"/_fixtures/graphs/{Uri.EscapeDataString(graph.GraphId)}"), cancellationToken)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <summary>Removes sibling role state in reverse provisioning order.</summary>
    public async Task<IReadOnlyList<string>> RemoveAsync(string graphId, CancellationToken cancellationToken)
    {
        List<string> failures = [];
        foreach (Uri endpoint in Endpoints().Reverse())
        {
            using HttpResponseMessage response = await _httpClient
                .DeleteAsync(new Uri(endpoint, $"/_fixtures/graphs/{Uri.EscapeDataString(graphId)}"), cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                failures.Add($"{endpoint.Host}:{(int)response.StatusCode}");
            }
        }

        return failures;
    }

    private IReadOnlyList<Uri> Endpoints()
        => EndpointKeys.Select(key =>
        {
            string value = _configuration[key]
                ?? throw new InvalidOperationException($"Required fixture endpoint '{key}' is not configured.");
            return new Uri(value, UriKind.Absolute);
        }).ToArray();
}
