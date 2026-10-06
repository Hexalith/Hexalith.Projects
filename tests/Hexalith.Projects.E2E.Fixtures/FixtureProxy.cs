// <copyright file="FixtureProxy.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

using System.Net.Http.Json;

/// <summary>Coordinates graph state across the three role-specific sibling fixtures.</summary>
/// <param name="httpClient">The client used to reach the role hosts.</param>
/// <param name="configuration">The configuration that names each role endpoint.</param>
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

    /// <summary>Seeds all sibling roles in provisioning order and compensates in reverse order on failure.</summary>
    /// <param name="graph">The validated metadata-only graph.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns><see langword="null"/> on success; otherwise the metadata-only failure and compensation.</returns>
    public async Task<FixtureSeedFailure?> SeedAsync(LiveFixtureGraph graph, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);
        IReadOnlyList<KeyValuePair<string, Uri>> endpoints = Endpoints();
        for (int index = 0; index < endpoints.Count; index++)
        {
            KeyValuePair<string, Uri> endpoint = endpoints[index];
            int? statusCode = await SendAsync(
                HttpMethod.Post,
                new Uri(endpoint.Value, "/_fixtures/graphs"),
                graph,
                cancellationToken)
                .ConfigureAwait(false);
            if (statusCode is >= 200 and < 300)
            {
                continue;
            }

            // The failed role may have stored the graph before its response was lost, so it is
            // compensated together with every earlier role. Removal is idempotent (404 succeeds).
            FixtureCleanupResult compensation = await RemoveRolesAsync(
                endpoints.Take(index + 1).Reverse(),
                graph.GraphId,
                cancellationToken)
                .ConfigureAwait(false);
            return new FixtureSeedFailure(endpoint.Key, statusCode, compensation);
        }

        return null;
    }

    /// <summary>Removes sibling role state in reverse provisioning order.</summary>
    /// <param name="graphId">The graph identity.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The typed attempted-role and status results in attempt order.</returns>
    public Task<FixtureCleanupResult> RemoveAsync(string graphId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(graphId);
        return RemoveRolesAsync(Endpoints().Reverse(), graphId, cancellationToken);
    }

    private async Task<FixtureCleanupResult> RemoveRolesAsync(
        IEnumerable<KeyValuePair<string, Uri>> endpoints,
        string graphId,
        CancellationToken cancellationToken)
    {
        List<FixtureCleanupAttempt> attempts = [];
        foreach (KeyValuePair<string, Uri> endpoint in endpoints)
        {
            int? statusCode = await SendAsync(
                HttpMethod.Delete,
                new Uri(endpoint.Value, $"/_fixtures/graphs/{Uri.EscapeDataString(graphId)}"),
                graph: null,
                cancellationToken)
                .ConfigureAwait(false);
            attempts.Add(new FixtureCleanupAttempt(endpoint.Key, statusCode));
        }

        return new FixtureCleanupResult(attempts);
    }

    private async Task<int?> SendAsync(
        HttpMethod method,
        Uri requestUri,
        LiveFixtureGraph? graph,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, requestUri);
        if (graph is not null)
        {
            request.Content = JsonContent.Create(graph);
        }

        try
        {
            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            return (int)response.StatusCode;
        }
        catch (HttpRequestException)
        {
            // Transport details can carry endpoints; only the absence of a status is reported.
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private IReadOnlyList<KeyValuePair<string, Uri>> Endpoints()
        => EndpointKeys.Select(item =>
        {
            string value = _configuration[item.Value]
                ?? throw new InvalidOperationException($"Required fixture role '{item.Key}' is not configured.");
            return new KeyValuePair<string, Uri>(item.Key, new Uri(value, UriKind.Absolute));
        }).ToArray();
}
