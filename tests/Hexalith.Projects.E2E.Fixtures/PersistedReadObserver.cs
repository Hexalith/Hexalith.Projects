namespace Hexalith.Projects.E2E.Fixtures;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>Test-only, read-only observer used between completed fixture setup and cleanup.</summary>
internal sealed class PersistedReadObserver(IConfiguration configuration)
{
    // The single Lua invocation observes key membership, serialized bytes and Dapr hash versions
    // atomically. It never mutates state, repairs a projection, or fetches via a production endpoint.
    private const string Script = """
        local result = {}; local cursor = '0';
        repeat
          local page = redis.call('SCAN', cursor); cursor = page[1];
          for _, key in ipairs(page[2]) do
            local audit = string.find(key, 'projects:read-audit:v1:', 1, true) ~= nil;
            local tenant = ARGV[1];
            local function contains(value) return string.find(key, value, 1, true) ~= nil; end;
            local domain = contains('projects:reads:v1:' .. string.len(tenant) .. ':' .. tenant .. ':')
              or contains('projects-conversation-start-setup||' .. tenant .. ':projects:')
              or contains('projects:tenant-access:' .. tenant)
              or contains('projection:folders:') and contains(':' .. tenant)
              or contains('AggregateActor') and (contains(tenant .. ':') or contains('system:tenants:' .. tenant .. '||'))
              or contains('projection:folders:operational-status:') or contains('shared-generation:') or contains('shared-cow-pointer:');
            if audit or domain then
              local bytes = redis.call('DUMP', key);
              if bytes then table.insert(result, {key, redis.sha1hex(bytes), audit}); end;
            end;
          end;
        until cursor == '0';
        return cjson.encode(result);
        """;

    /// <summary>Limits observation to the private launcher's exact registered run, Tenant and real actor bindings.</summary>
    internal bool CanObserve(LiveFixtureGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!configuration.GetValue<bool>("FixtureStateStore:Enabled"))
            return false;
        string? isolation = configuration["FixtureStateStore:IsolationId"];
        if (isolation is null || !isolation.StartsWith("reads-", StringComparison.Ordinal) || isolation.Length < 18
            || graph.RunId != isolation)
            return false;
        return graph.GraphId == isolation && graph.TenantId == configuration["FixtureStateStore:Tenant"]
                && graph.PrincipalId == configuration["FixtureStateStore:PrimaryActor"]
            || graph.GraphId == isolation + "-foreign" && graph.TenantId == configuration["FixtureStateStore:ForeignTenant"]
                && graph.PrincipalId == configuration["FixtureStateStore:ForeignActor"];
    }

    internal async Task<PersistedReadObservation> ObserveAsync(LiveFixtureGraph graph, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!CanObserve(graph))
            throw new InvalidOperationException("The graph is outside the owned observation scope.");
        string address = configuration["FixtureStateStore:RedisHost"] ?? throw new InvalidOperationException("Fixture observer is not configured.");
        if (!Uri.TryCreate("tcp://" + address, UriKind.Absolute, out Uri? endpoint) || endpoint.Port <= 0 || endpoint.Port == 6379 || !endpoint.IsLoopback
            || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new InvalidOperationException("Fixture observer address is invalid.");
        ProcessStartInfo start = new("redis-cli")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in new[] { "--raw", "-h", endpoint.Host, "-p", endpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture), "EVAL", Script, "0", graph.TenantId })
            start.ArgumentList.Add(argument);
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Fixture observer failed to start.");
        try
        {
            Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errors = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            string serialized = await output.ConfigureAwait(false);
            _ = await errors.ConfigureAwait(false);
            if (process.ExitCode != 0)
                throw new InvalidOperationException("Fixture state observation failed.");
            using JsonDocument document = JsonDocument.Parse(serialized);
            SortedDictionary<string, string> domain = new(StringComparer.Ordinal), audit = new(StringComparer.Ordinal);
            foreach (JsonElement row in document.RootElement.EnumerateArray())
            {
                string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(row[0].GetString()!)));
                SortedDictionary<string, string> destination = row[2].GetBoolean() ? audit : domain;
                string fingerprint = row[1].GetString()!;
                if (!destination.TryAdd(key, fingerprint) && destination[key] != fingerprint)
                    throw new InvalidOperationException("Fixture state observation changed within its atomic boundary.");
            }
            if (domain.Count == 0)
                throw new InvalidOperationException("Fixture state observation was empty.");
            return new(domain, audit);
        }
        finally
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: false);
        }
    }
}
