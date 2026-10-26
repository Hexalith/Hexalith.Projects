#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Xunit;

/// <summary>Focused controls for the distributed fixture's independent evidence checks.</summary>
public sealed class SupportedProjectReadEvidenceTests
{
    /// <summary>New domain keys are observed while permitted audit and operational writes stay separate.</summary>
    [Fact]
    public void DomainFamilyEnumerationIncludesNewKeysAndExcludesAuditAndOperationalKeys()
    {
        foreach (string key in new[] { "projects:reads:v1:5:owned:9:new-entry", "projects:tenant-access:owned",
            "projection:folders:new-domain-family:owned:new-folder", "eventstore||AggregateActor||owned:projects:new-project||journal",
            "eventstore||AggregateActor||system:tenants:owned||journal" })
            Assert.True(SupportedProjectReadDistributedFixture.IsOwnedDomainKey(key, "owned", "foreign"), "Owned domain family was omitted.");
        foreach (string key in new[] { "projects:read-audit:v1:owned", "admin:catalog:owned", "projection:folders:folder-permissions:unrelated:folder",
            "projects:reads:v1:9:unrelated:detail" })
            Assert.False(SupportedProjectReadDistributedFixture.IsOwnedDomainKey(key, "owned", "foreign"), "Unrelated or permitted state entered the domain comparison.");
    }

    /// <summary>Closed audit values, types, hash, count, time and sensitive exclusions are all required.</summary>
    [Fact]
    public void AuditPrivacyRequiresClosedValuesAndSafeMetadata()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Dictionary<string, object> valid = new()
        {
            ["requestSurrogate"] = new string('A', 64), ["outcome"] = "Inspection", ["projectCount"] = 2,
            ["fieldClass"] = "ProjectName", ["recordedAt"] = now,
        };
        bool Safe(Dictionary<string, object> value, params string[] exclusions) => SupportedProjectReadDistributedFixture.IsSafeAudit(
            JsonSerializer.SerializeToElement(value), now.AddMinutes(-1), now, exclusions);
        Assert.True(Safe(valid));
        foreach ((string field, object value) in new (string, object)[]
        {
            ("requestSurrogate", "protected-description"), ("requestSurrogate", new string('a', 64)), ("outcome", "ProtectedAlphabeticDescription"),
            ("fieldClass", "Owned folder label"), ("fieldClass", 4), ("projectCount", -1), ("projectCount", 201), ("projectCount", "2"),
            ("recordedAt", now.AddHours(-1)), ("recordedAt", now.AddHours(1)), ("recordedAt", "actor-1"),
        })
        {
            Dictionary<string, object> invalid = new(valid) { [field] = value };
            Assert.False(Safe(invalid), "Invalid audit metadata was accepted.");
        }
        Assert.False(Safe(valid, new string('A', 64)), "A seeded sensitive value was retained in audit metadata.");
        Dictionary<string, object> extra = new(valid) { ["credential"] = "protected-value" };
        Assert.False(Safe(extra), "An extra audit field was accepted.");
        Dictionary<string, object> denial = new(valid) { ["outcome"] = "SafeDenial", ["fieldClass"] = "None", ["projectCount"] = 0 };
        Assert.True(Safe(denial));
        denial["projectCount"] = 1;
        Assert.False(Safe(denial), "Safe denial retained a disclosure count.");
    }

    /// <summary>Real admission component names remain public while each descriptive response path is omitted.</summary>
    [Fact]
    public void NonDescriptiveFaultControlsValidateContractPathsAndPopulatedCompleteResults()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AdmissionSnapshot snapshot = new(AdmissionResponseState.Complete, now, 2,
            [new("Project", true, EvidenceFreshnessState.Current, "current"), new("Folder", true, EvidenceFreshnessState.Current, "current")], []);
        GetProjectResponse opened = new("project-a", null, null, ProjectLifecycle.Active, now, now, null, null,
            [new("ProjectFolder", "folder-project-a", null, null, null, ReferenceState.Included, null, now)], snapshot);
        JsonElement open = JsonSerializer.SerializeToElement(opened, JsonSerializerOptions.Web);
        Assert.True(open.GetProperty("snapshot").GetProperty("components")[0].TryGetProperty("name", out _), "Positive control omitted public admission component names.");
        Assert.True(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(open, open: true));
        foreach (string field in new[] { "name", "description", "setup", "setupMetadata" })
            foreach (string? value in new string?[] { null, "protected-descriptive-value" })
            {
                JsonObject invalid = JsonNode.Parse(open.GetRawText())!.AsObject();
                invalid[field] = value;
                Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(invalid), open: true), "An open descriptive field was accepted.");
            }
        foreach (string? value in new string?[] { null, "protected-reference-label" })
        {
            JsonObject invalid = JsonNode.Parse(open.GetRawText())!.AsObject();
            invalid["references"]![0]!["displayName"] = value;
            Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(invalid), open: true), "An open reference display name was accepted.");
        }
        ProjectListPage listed = new([new("project-a", null, ProjectLifecycle.Active, ReferenceState.Included, snapshot),
            new("project-b", null, ProjectLifecycle.Active, ReferenceState.Included, snapshot)], null, false, snapshot);
        JsonElement list = JsonSerializer.SerializeToElement(listed, JsonSerializerOptions.Web);
        Assert.True(list.GetProperty("items")[0].GetProperty("snapshot").GetProperty("components")[0].TryGetProperty("name", out _), "Positive list control omitted public admission component names.");
        Assert.True(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(list, open: false));
        foreach (int index in new[] { 0, 1 })
            foreach (string? value in new string?[] { null, "protected-project-name" })
            {
                JsonObject invalid = JsonNode.Parse(list.GetRawText())!.AsObject();
                invalid["items"]![index]!["name"] = value;
                Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(invalid), open: false), "A list row name was accepted.");
            }
        Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(listed with { Items = [] }, JsonSerializerOptions.Web), open: false), "An empty Complete list control qualified.");
        Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(opened with { Snapshot = snapshot with { ResponseState = AdmissionResponseState.Unavailable } }, JsonSerializerOptions.Web), open: true), "An unavailable ordinary open control qualified.");
        Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(listed with { Snapshot = snapshot with { ResponseState = AdmissionResponseState.Unavailable } }, JsonSerializerOptions.Web), open: false), "An unavailable ordinary list control qualified.");
    }

    /// <summary>Restart evidence requires both actual server and launcher identities to change after proven exit.</summary>
    [Fact]
    public void RestartEvidenceRejectsMissingExitAndUnchangedActualProcess()
    {
        object Identity(int pid, int launcher, string started) => new
        {
            resourceName = "projects-owned", pid, startTicks = started, launcherPid = launcher, launcherStartTicks = started,
            binarySha256 = new string('a', 64), assemblySha256 = new string('b', 64),
        };
        JsonObject valid = JsonSerializer.SerializeToNode(new
        {
            resource = "projects", action = "restart", completed = true, oldProcessExited = true, endpointsRediscovered = true,
            before = Identity(101, 201, "100"), after = Identity(102, 202, "200"),
        })!.AsObject();
        bool Accept(JsonObject value) => SupportedProjectReadDistributedFixture.IsRestartProcessEvidence(JsonSerializer.SerializeToElement(value));
        Assert.True(Accept(valid));
        foreach (string field in new[] { "before", "after", "completed", "oldProcessExited", "endpointsRediscovered" })
        {
            JsonObject invalid = valid.DeepClone().AsObject();
            invalid.Remove(field);
            Assert.False(Accept(invalid), "Incomplete restart evidence qualified.");
        }
        foreach ((string pid, string ticks) in new[] { ("pid", "startTicks"), ("launcherPid", "launcherStartTicks") })
        {
            JsonObject invalid = valid.DeepClone().AsObject();
            invalid["after"]![pid] = invalid["before"]![pid]!.DeepClone();
            invalid["after"]![ticks] = invalid["before"]![ticks]!.DeepClone();
            Assert.False(Accept(invalid), "An unchanged process qualified as a restart.");
        }
        foreach (string field in new[] { "pid", "startTicks", "launcherPid", "launcherStartTicks", "binarySha256", "assemblySha256", "resourceName" })
        {
            JsonObject invalid = valid.DeepClone().AsObject();
            invalid["after"]!.AsObject().Remove(field);
            Assert.False(Accept(invalid), "Missing process identity qualified.");
        }
        JsonObject changedBinary = valid.DeepClone().AsObject();
        changedBinary["after"]!["assemblySha256"] = new string('c', 64);
        Assert.False(Accept(changedBinary), "A rebuilt or replaced application qualified as an unchanged restart.");
    }

    /// <summary>Fresh observation timestamps and opaque cursors may change; stable metadata, versions, freshness and order may not.</summary>
    [Fact]
    public void RestartComparisonPreservesStableMetadataVersionsFreshnessAndOrdering()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AdmissionSnapshot snapshot = new(AdmissionResponseState.Complete, now, 2,
            [new("Project", true, EvidenceFreshnessState.Current, "Current") { LastVerifiedAt = now },
             new("Folder", true, EvidenceFreshnessState.Current, "Current") { LastVerifiedAt = now },
             new("Authorization", true, EvidenceFreshnessState.Current, "Current") { LastVerifiedAt = now }], []);
        AdmissionSnapshot observed = snapshot with { AsOf = now.AddSeconds(1), Components = snapshot.Components.Select(component =>
            component.Name == "Project" ? component : component with { LastVerifiedAt = now.AddSeconds(1) }).ToArray() };
        GetProjectResponse opened = new("project-a", "Synthetic", "Description", ProjectLifecycle.Active, now, now, null, null, [], snapshot);
        Assert.True(SupportedProjectReadDistributedFixture.SameStableReadMetadata(opened, opened with { Snapshot = observed }));
        foreach (GetProjectResponse changed in new[]
        {
            opened with { Name = "Changed" }, opened with { Description = "Changed" }, opened with { UpdatedAt = now.AddSeconds(1) },
            opened with { LifecycleState = ProjectLifecycle.Archived }, opened with { SetupMetadata = "Changed" },
            opened with { Snapshot = snapshot with { ProjectVersion = 3 } },
            opened with { Snapshot = snapshot with { Components = [snapshot.Components[0] with { Freshness = EvidenceFreshnessState.Stale }] } },
            opened with { Snapshot = snapshot with { Components = [snapshot.Components[0] with { LastVerifiedAt = now.AddSeconds(1) }, .. snapshot.Components.Skip(1)] } },
            opened with { Snapshot = snapshot with { AsOf = default } },
            opened with { Snapshot = snapshot with { RecoveryActions = ["Retry"] } },
        }) Assert.False(SupportedProjectReadDistributedFixture.SameStableReadMetadata(opened, changed), "Altered stable metadata qualified.");
        ProjectListPage page = new([new("project-a", "A", ProjectLifecycle.Active, ReferenceState.Included, snapshot),
            new("project-b", "B", ProjectLifecycle.Active, ReferenceState.Included, snapshot)], "opaque-before", true, snapshot);
        Assert.True(SupportedProjectReadDistributedFixture.SameStableReadMetadata(page, page with { Cursor = "opaque-after", Snapshot = observed }));
        foreach (ProjectListPage changed in new[] { page with { Items = page.Items.Reverse().ToArray() }, page with { Items = [page.Items[0]] },
            page with { Cursor = null }, page with { HasMore = false }, page with { Items = [page.Items[0] with { Name = "Changed" }, page.Items[1]] } })
            Assert.False(SupportedProjectReadDistributedFixture.SameStableReadMetadata(page, changed), "Altered paging metadata qualified.");
    }

    /// <summary>Neither primary nor foreign domain bytes, exact watermarks, keys or concurrency identities may change.</summary>
    [Fact]
    public void RestartStateComparisonRejectsAlteredStateInEitherTenant()
    {
        SupportedProjectReadStateSnapshot[] before = [new("primary", "{\"globalPosition\":9}"u8.ToArray(), "one"),
            new("foreign", "{\"globalPosition\":17}"u8.ToArray(), "two")];
        Assert.True(SupportedProjectReadDistributedFixture.SameDomainState(before, before.Reverse().ToArray()));
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState([], []));
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, [before[0]]));
        Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, [.. before, new("new-key", [1], "three")]));
        for (int index = 0; index < before.Length; index++)
            foreach (SupportedProjectReadStateSnapshot altered in new[] { before[index] with { ETag = "changed" }, before[index] with { Key = "replaced" },
                before[index] with { Bytes = "{\"globalPosition\":18}"u8.ToArray() } })
            {
                SupportedProjectReadStateSnapshot[] after = before.ToArray();
                after[index] = altered;
                Assert.False(SupportedProjectReadDistributedFixture.SameDomainState(before, after), "Altered domain state qualified.");
            }
    }

    /// <summary>Failure evidence survives mismatched, missing, malformed and unreadable state without protected payloads.</summary>
    [Fact]
    public async Task RestartFailurePersistsSanitizedObservationsBeforeAssertionAsync()
    {
        string[] tenants = ["primary-secret", "foreign-secret"];
        SupportedProjectReadStateSnapshot[] before = tenants.SelectMany(tenant => new[]
        {
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Index(tenant),
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Detail(tenant, "project-a"),
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Detail(tenant, "project-b"),
        }).Select((key, index) => new SupportedProjectReadStateSnapshot(key,
            JsonSerializer.SerializeToUtf8Bytes(new { globalPosition = index + 1, description = "protected-secret-payload" }), "secret-etag")).ToArray();
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-state-control-");
        try
        {
            foreach (string scenario in new[] { "changed", "missing", "malformed", "unreadable" })
            {
                string path = Path.Combine(directory.FullName, scenario + ".json");
                SupportedProjectReadStateSnapshot[] after = before.ToArray();
                if (scenario == "changed") after[0] = after[0] with { ETag = "changed" };
                if (scenario == "missing") after = after.Skip(1).ToArray();
                if (scenario == "malformed") after[0] = after[0] with { Bytes = "["u8.ToArray() };
                await Assert.ThrowsAnyAsync<Exception>(() => SupportedProjectReadDistributedFixture.CaptureRestartStateEvidenceAsync(path, before,
                    () => scenario == "unreadable" ? Task.FromException<SupportedProjectReadStateSnapshot[]>(new IOException("protected-secret-error")) : Task.FromResult(after), tenants));
                string text = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
                Assert.DoesNotContain("secret", text, StringComparison.Ordinal);
                using JsonDocument evidence = JsonDocument.Parse(text);
                Assert.False(evidence.RootElement.GetProperty("domainKeysBytesEtagsAndWatermarksUnchanged").GetBoolean());
                Assert.Equal(6, evidence.RootElement.GetProperty("before").GetArrayLength());
                Assert.Equal(scenario == "changed" ? "observed" : scenario == "missing" ? "missing" : "unreadable",
                    evidence.RootElement.GetProperty("afterObservation").GetString());
                if (scenario == "missing") Assert.Single(evidence.RootElement.GetProperty("missingKeys").EnumerateArray());
                if (scenario == "unreadable") Assert.Equal(JsonValueKind.Null, evidence.RootElement.GetProperty("after").ValueKind);
            }
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>Transient network/request failures recover through the real predicate and never disclose exception details.</summary>
    [Fact]
    public async Task RecoveryRetriesTransportAndRequestFailuresThenRequiresAuthorizedResponseAsync()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-recovery-control-");
        try
        {
            int calls = 0;
            Task<System.Net.Http.HttpResponseMessage> Query(CancellationToken _) => ++calls switch
            {
                1 => Task.FromException<System.Net.Http.HttpResponseMessage>(new HttpRequestException("protected-secret-token")),
                2 => Task.FromException<System.Net.Http.HttpResponseMessage>(new TaskCanceledException("protected-secret-payload")),
                3 => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)),
                _ => Task.FromResult(AuthorizedRecoveryResponse()),
            };
            string path = Path.Combine(directory.FullName, "recovery.log");
            await SupportedProjectReadDistributedFixture.ObserveAuthorizedRecoveryAsync("projects-process", Query, path,
                TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(1), TestContext.Current.CancellationToken);
            string diagnostic = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(4, calls);
            Assert.Contains("transportFailures=2", diagnostic);
            Assert.Contains("authorizedRecovery=True", diagnostic);
            Assert.DoesNotContain("secret", diagnostic, StringComparison.Ordinal);
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>A short test budget bounds persistent failures and a request which ignores cancellation.</summary>
    [Theory]
    [InlineData("transport")]
    [InlineData("request-timeout")]
    [InlineData("unresponsive")]
    [InlineData("server")]
    public async Task RecoveryFailureIsBoundedAndLogsOnlyMetadataAsync(string scenario)
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-recovery-control-");
        try
        {
            TaskCompletionSource<System.Net.Http.HttpResponseMessage> unresponsive = new();
            Task<System.Net.Http.HttpResponseMessage> Query(CancellationToken _) => scenario switch
            {
                "transport" => Task.FromException<System.Net.Http.HttpResponseMessage>(new HttpRequestException("protected-secret-token")),
                "request-timeout" => Task.FromException<System.Net.Http.HttpResponseMessage>(new TaskCanceledException("protected-secret-token")),
                "unresponsive" => unresponsive.Task,
                _ => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                    { Content = new StringContent("protected-secret-payload") }),
            };
            string path = Path.Combine(directory.FullName, "recovery.log");
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
            await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveAuthorizedRecoveryAsync(
                "projects-process", Query, path, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(1), TestContext.Current.CancellationToken));
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), "Recovery exceeded its short control budget.");
            string diagnostic = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains("authorizedRecovery=False", diagnostic);
            Assert.DoesNotContain("secret", diagnostic, StringComparison.Ordinal);
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>Caller cancellation is preserved while recording unsuccessful recovery diagnostics.</summary>
    [Fact]
    public async Task RecoveryCancellationIsNotReclassifiedAsSuccessOrDeadlineAsync()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-recovery-control-");
        try
        {
            using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(50));
            string path = Path.Combine(directory.FullName, "recovery.log");
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SupportedProjectReadDistributedFixture.ObserveAuthorizedRecoveryAsync(
                "projects-process", async token => { await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false); return AuthorizedRecoveryResponse(); }, path,
                TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(1), cancellation.Token));
            Assert.Contains("authorizedRecovery=False", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>Authentication and malformed-request refusals stop immediately rather than retrying until the deadline.</summary>
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    public async Task RecoveryUnexpectedAuthenticationRefusesImmediatelyAsync(int status)
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-recovery-control-");
        try
        {
            int calls = 0;
            string path = Path.Combine(directory.FullName, "recovery.log");
            Task<System.Net.Http.HttpResponseMessage> Query(CancellationToken _)
            {
                calls++;
                return Task.FromResult(new System.Net.Http.HttpResponseMessage((System.Net.HttpStatusCode)status)
                    { Content = new StringContent("protected-secret-payload") });
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveAuthorizedRecoveryAsync(
                "projects-process", Query, path, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(1), TestContext.Current.CancellationToken));
            Assert.Equal(1, calls);
            string diagnostic = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains("authorizedRecovery=False", diagnostic);
            Assert.DoesNotContain("secret", diagnostic, StringComparison.Ordinal);
        }
        finally { directory.Delete(recursive: true); }
    }

    /// <summary>Cancelling a fixture helper terminates and awaits its child while preserving an unrelated process.</summary>
    [Fact]
    public async Task HelperCancellationTerminatesOnlyItsOwnedProcessTreeAsync()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("restart-process-control-");
        using System.Diagnostics.Process unrelated = new() { StartInfo = new("python3") };
        unrelated.StartInfo.ArgumentList.Add("-c");
        unrelated.StartInfo.ArgumentList.Add("import time;time.sleep(30)");
        unrelated.Start();
        try
        {
            string path = Path.Combine(directory.FullName, "pids");
            const string code = "import os,subprocess,sys,time; child=subprocess.Popen([sys.executable,'-c','import time;time.sleep(30)']);open(sys.argv[1],'w').write(str(os.getpid())+' '+str(child.pid));time.sleep(30)";
            using CancellationTokenSource cancellation = new();
            Task<byte[]> helper = SupportedProjectReadDistributedFixture.RunOwnedProcessAsync("python3", ["-c", code, path], cancellation.Token);
            using CancellationTokenSource readyBudget = new(TimeSpan.FromSeconds(5));
            try
            {
                while (!File.Exists(path)) await Task.Delay(10, readyBudget.Token);
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => helper);
                foreach (string pid in (await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).Split(' '))
                {
                    string stat = $"/proc/{pid}/stat";
                    Assert.True(!File.Exists(stat) || File.ReadAllText(stat).Split(')')[1].TrimStart().StartsWith('Z'), "Owned helper remained alive after cancellation.");
                }
                Assert.False(unrelated.HasExited);
            }
            finally
            {
                cancellation.Cancel();
                try { await helper.ConfigureAwait(true); } catch (OperationCanceledException) { }
            }
        }
        finally { unrelated.Kill(); await unrelated.WaitForExitAsync(TestContext.Current.CancellationToken); directory.Delete(recursive: true); }
    }

    /// <summary>The conservative request-start bound cannot accept rejection after the unchanged cursor lifetime.</summary>
    [Fact]
    public void CursorRejectionMustPrecedeItsObservedLifetimeBound()
    {
        DateTimeOffset requested = DateTimeOffset.UtcNow;
        Assert.True(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(requested, requested.AddMinutes(14)));
        Assert.False(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(requested, requested.AddMinutes(15)));
        Assert.False(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(requested, requested.AddMinutes(16)));
        Assert.False(SupportedProjectReadDistributedFixture.CursorIsWithinObservedLifetime(requested, requested.AddSeconds(-1)));
    }

    private static System.Net.Http.HttpResponseMessage AuthorizedRecoveryResponse()
    {
        AdmissionSnapshot snapshot = new(AdmissionResponseState.Complete, DateTimeOffset.UtcNow, 2, [], []);
        GetProjectResponse opened = new("project-a", "protected-secret-name", null, ProjectLifecycle.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], snapshot);
        var response = new Hexalith.EventStore.Contracts.Queries.SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(opened, JsonSerializerOptions.Web));
        return new(System.Net.HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(response) };
    }

    /// <summary>Storage access remains bound to the creation ID even if its reusable name is replaced.</summary>
    [Fact]
    public void RedisIdentityRefusesReplacementAndForeignOwnership()
    {
        string expected = new('a', 64);
        Assert.True(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity(JsonSerializer.Serialize(new { id = expected, owner = "scope" }), expected, "scope"));
        Assert.False(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity(JsonSerializer.Serialize(new { id = new string('b', 64), owner = "scope" }), expected, "scope"));
        Assert.False(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity(JsonSerializer.Serialize(new { id = expected, owner = "foreign" }), expected, "scope"));
        Assert.False(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity("{}", expected, "scope"));
    }
}
#endif
