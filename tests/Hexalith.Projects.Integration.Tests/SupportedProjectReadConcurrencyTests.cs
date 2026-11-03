#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Collections.Concurrent;
using System.Text.Json;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Xunit;

/// <summary>Pins sequential truth observations and concurrent diagnostic integrity without dropping reads or dependencies.</summary>
public sealed class SupportedProjectReadConcurrencyTests
{
    /// <summary>All eight observations are sequential in exact Tenant and paging/open order for either scope order.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllEightTruthReadsRemainSequentialInExactTenantAndDependencyOrder(bool reversed)
    {
        string[] tenants = reversed ? ["foreign", "primary"] : ["primary", "foreign"];
        ConcurrentDictionary<string, int> steps = new(StringComparer.Ordinal);
        ConcurrentQueue<string> observationOrder = new();
        int active = 0, maximum = 0, calls = 0, completed = 0;
        async Task EnterAsync(string tenant, int step)
        {
            Assert.Equal(step, steps.AddOrUpdate(tenant, 1, (_, previous) => previous + 1) - 1);
            int current = Interlocked.Increment(ref active);
            int prior;
            do { prior = Volatile.Read(ref maximum); }
            while (prior < current && Interlocked.CompareExchange(ref maximum, current, prior) != prior);
            Interlocked.Increment(ref calls);
            try
            {
                Assert.Equal(1, current);
                Assert.Equal(Array.IndexOf(tenants, tenant) * 4 + step, Volatile.Read(ref completed));
                observationOrder.Enqueue(tenant + ":" + step);
                await Task.Yield();
            }
            finally
            {
                Interlocked.Decrement(ref active);
                Interlocked.Increment(ref completed);
            }
        }
        var observed = await SupportedProjectReadDistributedFixture.ObserveBothTenantTruthAsync(tenants,
            async (tenant, input) =>
            {
                bool next = input.Cursor is not null;
                Assert.Equal(1, input.PageSize);
                if (next) Assert.Equal(tenant + "-cursor", input.Cursor);
                await EnterAsync(tenant, next ? 1 : 0).ConfigureAwait(true);
                return Page(tenant, next);
            },
            async (tenant, project) =>
            {
                await EnterAsync(tenant, project == "project-a" ? 2 : 3).ConfigureAwait(true);
                return Open(tenant, project);
            });
        Assert.Equal(1, maximum);
        Assert.Equal(8, calls);
        Assert.Equal(0, active);
        Assert.Equal(8, completed);
        Assert.Equal(8, observed.Count);
        Assert.Equal(tenants.SelectMany(tenant => Enumerable.Range(0, 4).Select(step => tenant + ":" + step)), observationOrder);
        Assert.All(new[] { "primary", "foreign" }, tenant =>
        {
            Assert.Equal(4, steps[tenant]);
            Assert.Equal(tenant + ":project-a", Assert.IsType<GetProjectResponse>(observed[tenant + ":project-a"]).Name);
            Assert.Equal(tenant + ":project-b", Assert.IsType<GetProjectResponse>(observed[tenant + ":project-b"]).Name);
        });
    }

    /// <summary>Sequential observations retain every original ordering, continuation, Complete, metadata and version refusal.</summary>
    [Theory]
    [InlineData("first-order")]
    [InlineData("first-cursor")]
    [InlineData("first-unavailable")]
    [InlineData("next-duplicate")]
    [InlineData("next-cursor")]
    [InlineData("open-identity")]
    [InlineData("open-name")]
    [InlineData("open-version")]
    [InlineData("open-unavailable")]
    public async Task MalformedReadCannotQualifyAcrossTenantScopes(string changed)
    {
        await Assert.ThrowsAnyAsync<Exception>(() => SupportedProjectReadDistributedFixture.ObserveBothTenantTruthAsync(["primary", "foreign"],
            (tenant, input) =>
            {
                bool next = input.Cursor is not null;
                var page = Page(tenant, next);
                if (tenant == "foreign") page = changed switch
                {
                    "first-order" when !next => page with { Items = [page.Items[0] with { ProjectId = "project-b" }] },
                    "first-cursor" when !next => page with { Cursor = null },
                    "first-unavailable" when !next => page with { Snapshot = page.Snapshot with { ResponseState = AdmissionResponseState.Unavailable } },
                    "next-duplicate" when next => page with { Items = [page.Items[0] with { ProjectId = "project-a" }] },
                    "next-cursor" when next => page with { Cursor = "retained" },
                    _ => page,
                };
                return Task.FromResult(page);
            },
            (tenant, project) =>
            {
                var opened = Open(tenant, project);
                if (tenant == "foreign") opened = changed switch
                {
                    "open-identity" => opened with { ProjectId = "foreign-project" },
                    "open-name" => opened with { Name = "changed" },
                    "open-version" => opened with { Snapshot = opened.Snapshot with { ProjectVersion = 3 } },
                    "open-unavailable" => opened with { Snapshot = opened.Snapshot with { ResponseState = AdmissionResponseState.Unavailable } },
                    _ => opened,
                };
                return Task.FromResult(opened);
            }));
    }

    /// <summary>Malformed scope inventory refuses before issuing any request and cannot widen the cap.</summary>
    [Theory]
    [InlineData("duplicate")]
    [InlineData("empty")]
    [InlineData("extra")]
    public async Task ExactlyTwoDistinctScopesAreRequiredBeforeAnyRead(string mode)
    {
        string[] scopes = mode switch { "duplicate" => ["primary", "primary"], "empty" => ["primary", ""], _ => ["primary", "foreign", "extra"] };
        int calls = 0;
        await Assert.ThrowsAnyAsync<Exception>(() => SupportedProjectReadDistributedFixture.ObserveBothTenantTruthAsync(scopes,
            (tenant, input) => { calls++; return Task.FromResult(Page(tenant, false)); },
            (tenant, project) => { calls++; return Task.FromResult(Open(tenant, project)); }));
        Assert.Equal(0, calls);
    }

    /// <summary>A concurrent burst retains each complete diagnostic record exactly once.</summary>
    [Fact]
    public async Task ConcurrentReadEvidenceAppendsRemainCompleteAndExact()
    {
        string directory = Directory.CreateTempSubdirectory("supported-read-evidence-").FullName;
        try
        {
            string path = Path.Combine(directory, "observations.jsonl");
            using SemaphoreSlim gate = new(1, 1);
            await Task.WhenAll(Enumerable.Range(0, 128).Select(index => SupportedProjectReadDistributedFixture.AppendReadEvidenceAsync(
                gate, path, JsonSerializer.Serialize(new { index, marker = new string('x', 1024) }) + Environment.NewLine, TestContext.Current.CancellationToken)));
            string[] rows = await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(128, rows.Length);
            int[] ids = rows.Select(row => { using JsonDocument value = JsonDocument.Parse(row); Assert.Equal(1024, value.RootElement.GetProperty("marker").GetString()!.Length); return value.RootElement.GetProperty("index").GetInt32(); }).Order().ToArray();
            Assert.Equal(Enumerable.Range(0, 128), ids);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static readonly DateTimeOffset Instant = new(2026, 11, 3, 8, 0, 0, TimeSpan.Zero);
    private static AdmissionSnapshot Snapshot() => new(AdmissionResponseState.Complete, Instant, 2, [], []);
    private static ProjectListPage Page(string tenant, bool next)
    {
        string id = next ? "project-b" : "project-a";
        return new([new(id, tenant + ":" + id, ProjectLifecycle.Active, ReferenceState.Included, Snapshot())], next ? null : tenant + "-cursor", !next, Snapshot());
    }
    private static GetProjectResponse Open(string tenant, string project)
        => new(project, tenant + ":" + project, "description", ProjectLifecycle.Active, Instant, Instant, null, null, [], Snapshot());
}
#endif
