#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Hexalith.Tenants.Contracts.Identity;

/// <summary>Reuses authenticated bootstrap and source commands for the separate owned compatibility lane.</summary>
internal sealed partial class SupportedProjectReadDistributedFixture
{
    /// <summary>Seeds only newly owned source authority, then an archived Project to initialize its public list.</summary>
    internal async Task SeedIsolatedLiveFixturesAsync()
    {
        Require(Environment.GetEnvironmentVariable("PROJECTS_ISOLATED_LIVE_FIXTURES") == "1",
            "Compatibility setup requires the separate private runner.");
        var bootstrap = await AwaitHostedBootstrapAsync().ConfigureAwait(false);
        // Hosted publication and its final cleanup keys can become observable in adjacent Redis
        // turns. Observe the bounded fresh inventory twice before the unchanged cutover helper
        // performs its own complete quiescence, backup and source checks.
        using (var quiet = CancellationTokenSource.CreateLinkedTokenSource(Cancellation))
        {
            quiet.CancelAfter(TimeSpan.FromSeconds(30));
            Dictionary<string, SupportedProjectReadStateSnapshot>? previous = null;
            var stable = new Stopwatch();
            while (true)
            {
                try
                {
                    var current = await ReadFreshCutoverInventoryAsync(quiet.Token).ConfigureAwait(false);
                    if (previous is null || !SameCutoverInventory(previous, current)) stable.Restart();
                    previous = current;
                    if (stable.Elapsed >= TimeSpan.FromSeconds(2)) break;
                }
                catch (Xunit.Sdk.XunitException) when (!quiet.IsCancellationRequested)
                {
                    previous = null;
                    stable.Reset();
                }
                await Task.Delay(100, quiet.Token).ConfigureAwait(false);
            }
        }
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                await ActivateFreshDeliveryWriterProtocolAsync(bootstrap).ConfigureAwait(false);
                break;
            }
            catch (Xunit.Sdk.XunitException error) when (attempt < 3
                && error.Message == "Fresh cutover delivery inventory changed or has missing state."
                && !File.Exists(Path.Combine(Path.GetDirectoryName(Setting("InvocationDirectory"))!, "delivery-cutover.rdb")))
            {
                // This exact refusal precedes backup or activation. A written backup prevents a
                // retry, so a partly advanced protocol can never be re-admitted by this fixture.
                await Task.Delay(100, Cancellation).ConfigureAwait(false);
            }
        }
        await CommandAsync(TenantPlatformScopes.Tenants, "tenants", Tenant, "CreateTenant",
            new { TenantId = Tenant, Name = "Owned live fixture tenant", Description = "Disposable compatibility fixture" }, Tenant).ConfigureAwait(false);
        await CommandAsync(TenantPlatformScopes.Tenants, "tenants", Tenant, "AddUserToTenant",
            new { TenantId = Tenant, UserId = PrimaryActor, Role = "TenantOwner" }, Tenant).ConfigureAwait(false);
        var source = await StreamAsync(TenantPlatformScopes.Tenants, "tenants", Tenant, Tenant).ConfigureAwait(false);
        await WaitForTenantAuthorityAsync("projects:tenant-access:" + Tenant, Tenant, PrimaryActor, source, new Stopwatch()).ConfigureAwait(false);
        string project = Isolation + "-warmup";
        await CommandAsync(Tenant, "projects", project, "Hexalith.Projects.Commands.CreateProject",
            new { Name = "Owned fixture warmup" }, Tenant).ConfigureAwait(false);
        await CommandAsync(Tenant, "projects", project, "Hexalith.Projects.Commands.ArchiveProject",
            new { ArchiveIntent = "archive", RequestSchemaVersion = "v1" }, Tenant).ConfigureAwait(false);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(Cancellation);
        budget.CancelAfter(TimeSpan.FromSeconds(45));
        string token = HumanToken(Tenant, chatbot: false, inspection: true, administrator: true);
        HashSet<int> statuses = [];
        string? lastLifecycle = null;
        bool archived = false;
        Exception? primaryFailure = null;
        try
        {
            while (true)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    _endpoints["projects"] + "/api/v1/projects/" + project + "?inspectDescriptiveMetadata=true");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("X-Hexalith-Tenant-Id", Tenant);
                using var response = await _http.SendAsync(request, budget.Token).ConfigureAwait(false);
                statuses.Add((int)response.StatusCode);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(budget.Token).ConfigureAwait(false));
                    if (body.RootElement.TryGetProperty("lifecycleState", out var lifecycle))
                        lastLifecycle = lifecycle.GetString() is "active" or "archived" ? lifecycle.GetString() : "unexpected";
                    if (lastLifecycle == "archived") { archived = true; break; }
                }
                await Task.Delay(250, budget.Token).ConfigureAwait(false);
            }
        }
        catch (Exception error)
        {
            primaryFailure = error;
            throw;
        }
        finally
        {
            try
            {
                string raw = await RedisAsync("HGET", "projects:projection-journal:" + Tenant, "data").ConfigureAwait(false);
                using JsonDocument journal = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
                var eventNames = journal.RootElement.EnumerateObject().FirstOrDefault(property => property.Name.Equals("events", StringComparison.OrdinalIgnoreCase)).Value;
                string[] types = eventNames.ValueKind == JsonValueKind.Array ? eventNames.EnumerateArray().Select(item =>
                    item.EnumerateObject().Single(property => property.Name.Equals("eventTypeName", StringComparison.OrdinalIgnoreCase)).Value.GetString()!.Split('.').Last()).ToArray() : [];
                await File.WriteAllTextAsync(Path.Combine(Evidence, "warmup-convergence.json"), JsonSerializer.Serialize(new
                {
                    statuses = statuses.Order().ToArray(), lastLifecycle, archived,
                    journalSha256 = Digest(raw), journalEventCounts = types.GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count()),
                }, Json), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception) when (primaryFailure is not null)
            {
                // A diagnostic failure must preserve the actual setup/convergence failure.
            }
        }
        await File.WriteAllTextAsync(Path.Combine(Evidence, "isolated-fixture-seed.json"), JsonSerializer.Serialize(new
        {
            sourceAuthorityCreatedThroughCommands = true, hostedBootstrapVerified = true,
            sharedTenantChanged = false, warmupProjectArchived = true, realOidc = true,
        }), Cancellation).ConfigureAwait(false);
    }
}
#endif
