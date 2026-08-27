// <copyright file="FixtureProxy.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

using System.Net.Http.Json;

/// <summary>Coordinates graph state across the three role-specific sibling fixtures.</summary>
public sealed class FixtureProxy(HttpClient httpClient, IConfiguration configuration)
{
    private static readonly KeyValuePair<string, string>[] EndpointKeys =
    [
        new("conversations", "FixtureEndpoints:Conversations"),
        new("folders", "FixtureEndpoints:Folders"),
        new("memories", "FixtureEndpoints:Memories"),
    ];

    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <summary>Seeds all sibling roles and compensates already-seeded roles on failure.</summary>
    public async Task SeedAsync(LiveFixtureGraph graph, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);
        List<KeyValuePair<string, Uri>> seeded = [];
        try
        {
            foreach (KeyValuePair<string, Uri> endpoint in Endpoints())
            {
                using HttpResponseMessage response = await _httpClient
                    .PostAsJsonAsync(new Uri(endpoint.Value, "/_fixtures/graphs"), graph, cancellationToken)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Fixture role '{endpoint.Key}' seed failed with status {(int)response.StatusCode}.",
                        inner: null,
                        response.StatusCode);
                }

                seeded.Add(endpoint);
            }
        }
        catch
        {
            foreach (KeyValuePair<string, Uri> endpoint in seeded.AsEnumerable().Reverse())
            {
                _ = await _httpClient
                    .DeleteAsync(new Uri(endpoint.Value, $"/_fixtures/graphs/{Uri.EscapeDataString(graph.GraphId)}"), cancellationToken)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <summary>Removes sibling role state in reverse provisioning order.</summary>
    public async Task<FixtureCleanupResult> RemoveAsync(string graphId, CancellationToken cancellationToken)
    {
        List<FixtureCleanupAttempt> attempts = [];
        foreach (KeyValuePair<string, Uri> endpoint in Endpoints().Reverse())
        {
            try
            {
                using HttpResponseMessage response = await _httpClient
                    .DeleteAsync(new Uri(endpoint.Value, $"/_fixtures/graphs/{Uri.EscapeDataString(graphId)}"), cancellationToken)
                    .ConfigureAwait(false);
                attempts.Add(new FixtureCleanupAttempt(endpoint.Key, (int)response.StatusCode));
            }
            catch (HttpRequestException exception)
            {
                attempts.Add(new FixtureCleanupAttempt(endpoint.Key, (int?)exception.StatusCode));
            }
        }

        return new FixtureCleanupResult(attempts);
    }

    private IReadOnlyList<KeyValuePair<string, Uri>> Endpoints()
        => EndpointKeys.Select(item =>
        {
            string value = _configuration[item.Value]
                ?? throw new InvalidOperationException($"Required fixture role '{item.Key}' is not configured.");
            return new KeyValuePair<string, Uri>(item.Key, new Uri(value, UriKind.Absolute));
        }).ToArray();
}
