// <copyright file="DockerDaprRedisTwoReplicaFixture.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if SUPPORTED_PROJECT_READ_SOURCE
namespace Hexalith.Projects.Integration.Tests.Story71;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Dapr.Client;

using Hexalith.EventStore.Client.Projections;

using Xunit;

/// <summary>
/// Manages an isolated Docker bridge network running pinned <c>redis:7.4-alpine</c> (verified via
/// <c>redis-server --version</c>) and two independent <c>daprio/daprd:1.19.2</c> sidecars sharing
/// the same Redis state store (<c>keyPrefix: none</c>) for Story 7.1 two-replica claim-race,
/// lost-reply failover, and restart proofs.
/// </summary>
public sealed class DockerDaprRedisTwoReplicaFixture : IAsyncLifetime
{
    /// <summary>The pinned Dapr sidecar image for the Story 7.1 claim-race lane.</summary>
    public const string DaprImage = "daprio/daprd:1.19.2";

    /// <summary>The pinned Redis image tag for the Story 7.1 claim-race lane.</summary>
    public const string RedisImage = "redis:7.4-alpine";

    /// <summary>The shared Dapr state-store component name.</summary>
    public const string StoreName = "statestore";

    private readonly string _runId = Guid.NewGuid().ToString("N")[..10];
    private readonly ConcurrentBag<DaprClient> _extraClients = [];
    private string? _networkName;
    private string? _redisContainerName;
    private string? _daprAContainerName;
    private string? _daprBContainerName;
    private string? _componentsDir;
    private DaprClient? _daprClientA;
    private DaprClient? _daprClientB;
    private string _replicaAGrpcEndpoint = string.Empty;
    private string _replicaBGrpcEndpoint = string.Empty;

    /// <summary>Gets the observed <c>redis-server --version</c> string from the running Redis container.</summary>
    public string ObservedRedisServerVersion { get; private set; } = string.Empty;

    /// <summary>Gets the <see cref="DaprReadModelStore"/> connected to Dapr sidecar Replica A.</summary>
    public DaprReadModelStore ReplicaAStore { get; private set; } = null!;

    /// <summary>Gets the <see cref="DaprReadModelStore"/> connected to Dapr sidecar Replica B.</summary>
    public DaprReadModelStore ReplicaBStore { get; private set; } = null!;

    /// <summary>Gets the HTTP endpoint of Dapr sidecar Replica A.</summary>
    public string ReplicaAHttpEndpoint { get; private set; } = string.Empty;

    /// <summary>Gets the HTTP endpoint of Dapr sidecar Replica B.</summary>
    public string ReplicaBHttpEndpoint { get; private set; } = string.Empty;

    /// <summary>Creates a fresh <see cref="DaprReadModelStore"/> with a new <see cref="DaprClient"/> targeting Replica A (simulating process restart).</summary>
    /// <returns>A new <see cref="DaprReadModelStore"/> instance.</returns>
    public DaprReadModelStore CreateFreshReplicaAStore()
    {
        DaprClient client = new DaprClientBuilder()
            .UseHttpEndpoint(ReplicaAHttpEndpoint)
            .UseGrpcEndpoint(_replicaAGrpcEndpoint)
            .Build();
        _extraClients.Add(client);
        return new DaprReadModelStore(client);
    }

    /// <summary>Creates a fresh <see cref="DaprReadModelStore"/> with a new <see cref="DaprClient"/> targeting Replica B (simulating process restart).</summary>
    /// <returns>A new <see cref="DaprReadModelStore"/> instance.</returns>
    public DaprReadModelStore CreateFreshReplicaBStore()
    {
        DaprClient client = new DaprClientBuilder()
            .UseHttpEndpoint(ReplicaBHttpEndpoint)
            .UseGrpcEndpoint(_replicaBGrpcEndpoint)
            .Build();
        _extraClients.Add(client);
        return new DaprReadModelStore(client);
    }

    /// <summary>Executes <c>redis-cli</c> inside the fixture Redis container and returns stdout.</summary>
    /// <param name="args">The redis-cli arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The trimmed stdout output.</returns>
    public async Task<string> ExecRedisCliAsync(string args, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(args);
        return await RunDockerAsync($"exec {_redisContainerName} redis-cli {args}", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        CancellationToken ct = CancellationToken.None;
        _networkName = $"story71-net-{_runId}";
        _redisContainerName = $"story71-redis-{_runId}";
        _daprAContainerName = $"story71-daprd-a-{_runId}";
        _daprBContainerName = $"story71-daprd-b-{_runId}";
        _componentsDir = Path.Combine(Path.GetTempPath(), $"story71-dapr-components-{_runId}");

        try
        {
            _ = await WriteStatestoreComponentAsync(_componentsDir, ct).ConfigureAwait(false);

            await RunDockerAsync($"network create {_networkName}", ct).ConfigureAwait(false);
            await RunDockerAsync(
                $"run -d --name {_redisContainerName} --network {_networkName} --network-alias redis {RedisImage} redis-server --appendonly yes",
                ct).ConfigureAwait(false);

            // Verify actual Redis server version inside the running container (7.4.x).
            ObservedRedisServerVersion = (await RunDockerAsync($"exec {_redisContainerName} redis-server --version", ct).ConfigureAwait(false)).Trim();
            if (!ObservedRedisServerVersion.Contains("Redis server v=7.4.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected Redis server v=7.4.x, observed '{ObservedRedisServerVersion}'.");
            }

            await WaitForRedisReadyAsync(ct).ConfigureAwait(false);

            int[] ports = ReserveTcpPorts(4);
            int httpPortA = ports[0];
            int grpcPortA = ports[1];
            int httpPortB = ports[2];
            int grpcPortB = ports[3];

            await RunDockerAsync(
                $"run -d --name {_daprAContainerName} --network {_networkName} "
                + $"-p 127.0.0.1:{httpPortA}:3500 -p 127.0.0.1:{grpcPortA}:50001 "
                + $"-v \"{_componentsDir}:/components:ro\" {DaprImage} "
                + "./daprd --app-id projects-replica-a --dapr-http-port 3500 --dapr-grpc-port 50001 "
                + "--resources-path /components --log-level warn --enable-api-logging=false",
                ct).ConfigureAwait(false);

            await RunDockerAsync(
                $"run -d --name {_daprBContainerName} --network {_networkName} "
                + $"-p 127.0.0.1:{httpPortB}:3500 -p 127.0.0.1:{grpcPortB}:50001 "
                + $"-v \"{_componentsDir}:/components:ro\" {DaprImage} "
                + "./daprd --app-id projects-replica-b --dapr-http-port 3500 --dapr-grpc-port 50001 "
                + "--resources-path /components --log-level warn --enable-api-logging=false",
                ct).ConfigureAwait(false);

            ReplicaAHttpEndpoint = $"http://127.0.0.1:{httpPortA}";
            ReplicaBHttpEndpoint = $"http://127.0.0.1:{httpPortB}";
            _replicaAGrpcEndpoint = $"http://127.0.0.1:{grpcPortA}";
            _replicaBGrpcEndpoint = $"http://127.0.0.1:{grpcPortB}";

            await WaitForDaprReadyAsync(ReplicaAHttpEndpoint, ct).ConfigureAwait(false);
            await WaitForDaprReadyAsync(ReplicaBHttpEndpoint, ct).ConfigureAwait(false);

            _daprClientA = new DaprClientBuilder()
                .UseHttpEndpoint(ReplicaAHttpEndpoint)
                .UseGrpcEndpoint(_replicaAGrpcEndpoint)
                .Build();
            _daprClientB = new DaprClientBuilder()
                .UseHttpEndpoint(ReplicaBHttpEndpoint)
                .UseGrpcEndpoint(_replicaBGrpcEndpoint)
                .Build();

            ReplicaAStore = new DaprReadModelStore(_daprClientA);
            ReplicaBStore = new DaprReadModelStore(_daprClientB);
        }
        catch
        {
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        while (_extraClients.TryTake(out DaprClient? extra))
        {
            extra.Dispose();
        }

        _daprClientA?.Dispose();
        _daprClientB?.Dispose();

        foreach (string? container in new[] { _daprAContainerName, _daprBContainerName, _redisContainerName })
        {
            if (!string.IsNullOrWhiteSpace(container))
            {
                await TryRunDockerAsync($"rm -f {container}").ConfigureAwait(false);
            }
        }

        if (!string.IsNullOrWhiteSpace(_networkName))
        {
            await TryRunDockerAsync($"network rm {_networkName}").ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(_componentsDir) && Directory.Exists(_componentsDir))
        {
            try
            {
                Directory.Delete(_componentsDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort temp cleanup.
            }
        }
    }

    /// <summary>
    /// Creates the bind-mounted Dapr component directory and non-secret <c>statestore.yaml</c> file,
    /// ensuring non-Windows permissions remain readable by the nonroot <c>daprio/daprd</c> container user.
    /// </summary>
    /// <param name="componentsDir">The target directory to create and populate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The full path to the written <c>statestore.yaml</c> file.</returns>
    internal static async Task<string> WriteStatestoreComponentAsync(string componentsDir, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentsDir);
        _ = Directory.CreateDirectory(componentsDir);
        const string componentYaml = """
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: statestore
            spec:
              type: state.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "redis:6379"
                - name: redisPassword
                  value: ""
                - name: actorStateStore
                  value: "true"
                - name: keyPrefix
                  value: "none"
            """;
        string componentPath = Path.Combine(componentsDir, "statestore.yaml");
        await File.WriteAllTextAsync(componentPath, componentYaml, cancellationToken).ConfigureAwait(false);
        if (!OperatingSystem.IsWindows())
        {
            // daprio/daprd runs as nonroot (65532:65532), so the non-secret component file
            // must remain readable even when the test runner inherits a restrictive umask (0077).
            File.SetUnixFileMode(
                componentsDir,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            File.SetUnixFileMode(
                componentPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.GroupRead
                | UnixFileMode.OtherRead);
        }

        return componentPath;
    }

    private async Task WaitForRedisReadyAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                string pong = await RunDockerAsync($"exec {_redisContainerName} redis-cli ping", cancellationToken).ConfigureAwait(false);
                if (pong.Trim().Equals("PONG", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // Retry until ready.
            }

            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Redis container '{_redisContainerName}' did not respond to PING within 30s.");
    }

    private static async Task WaitForDaprReadyAsync(string httpEndpoint, CancellationToken cancellationToken)
    {
        using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(2) };
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using HttpResponseMessage response = await http
                    .GetAsync($"{httpEndpoint}/v1.0/healthz/outbound", cancellationToken)
                    .ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NoContent || response.IsSuccessStatusCode)
                {
                    // Also verify statestore component is loaded.
                    using HttpResponseMessage meta = await http
                        .GetAsync($"{httpEndpoint}/v1.0/metadata", cancellationToken)
                        .ConfigureAwait(false);
                    string body = await meta.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (meta.IsSuccessStatusCode && body.Contains(StoreName, StringComparison.Ordinal))
                    {
                        return;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Sidecar still starting.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Per-request HTTP timeout while sidecar boots — retry cleanly.
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Dapr sidecar at '{httpEndpoint}' did not become ready within 45s.");
    }

    private static int[] ReserveTcpPorts(int count)
    {
        TcpListener[] listeners = new TcpListener[count];
        int[] ports = new int[count];
        try
        {
            for (int index = 0; index < count; index++)
            {
                listeners[index] = new TcpListener(IPAddress.Loopback, 0);
                listeners[index].Start();
                ports[index] = ((IPEndPoint)listeners[index].LocalEndpoint).Port;
            }

            return ports;
        }
        finally
        {
            foreach (TcpListener? listener in listeners)
            {
                listener?.Stop();
            }
        }
    }

    private static async Task<string> RunDockerAsync(string arguments, CancellationToken cancellationToken)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.Start();
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        string stdout = await stdoutTask.ConfigureAwait(false);
        string stderr = await stderrTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"docker {arguments} exited with {process.ExitCode}: {stderr.Trim()}"));
        }

        return stdout;
    }

    private static async Task TryRunDockerAsync(string arguments)
    {
        try
        {
            _ = await RunDockerAsync(arguments, CancellationToken.None).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Ignore cleanup errors for containers/networks that were never created.
        }
    }
}
#endif
