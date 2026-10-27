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

    /// <summary>Replay cannot use an incomplete prefix, a substituted identity or inconsistent source metadata.</summary>
    [Fact]
    public void CompleteReplaySourceRejectsMissingTruncatedAndInconsistentHistory()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        Hexalith.EventStore.Contracts.Streams.StreamReadEvent first = new(1, "Created", "{}"u8.ToArray(), "json", 1, "first", "correlation", null, timestamp, "actor") { GlobalPosition = 11 };
        Hexalith.EventStore.Contracts.Streams.StreamReadEvent second = first with { SequenceNumber = 2, MessageId = "second", GlobalPosition = 15, Timestamp = timestamp.AddSeconds(1) };
        Hexalith.EventStore.Contracts.Streams.StreamReadRequest request = new("owned", "projects", "project-a");
        Hexalith.EventStore.Contracts.Streams.StreamReadPage source = new("owned", "projects", "project-a", [first, second], new(0, null, 2, 2, 2, false, null));
        Assert.True(SupportedProjectReadDistributedFixture.IsCompleteProjectStream(request, source));
        foreach (Hexalith.EventStore.Contracts.Streams.StreamReadPage invalid in new[]
        {
            source with { Tenant = "foreign" }, source with { Domain = "folders" }, source with { AggregateId = "project-b" },
            source with { Events = [] }, source with { Events = [first] }, source with { Events = [second] },
            source with { Events = [first, second with { SequenceNumber = 3 }] },
            source with { Events = [first, second with { GlobalPosition = 0 }] }, source with { Events = [first, second with { GlobalPosition = 11 }] },
            source with { Events = [first, second with { Timestamp = default }] }, source with { Events = [first, second with { MessageId = first.MessageId }] },
            source with { Metadata = source.Metadata with { FromSequence = 1 } }, source with { Metadata = source.Metadata with { ToSequence = 2 } },
            source with { Metadata = source.Metadata with { LastSequenceReturned = 1 } }, source with { Metadata = source.Metadata with { LatestSequence = 3 } },
            source with { Metadata = source.Metadata with { LatestSequence = 1 } }, source with { Metadata = source.Metadata with { EventCount = 1 } },
            source with { Metadata = source.Metadata with { IsTruncated = true } },
        }) Assert.False(SupportedProjectReadDistributedFixture.IsCompleteProjectStream(request, invalid), "Incomplete or inconsistent source evidence qualified.");
        Assert.False(SupportedProjectReadDistributedFixture.IsCompleteProjectStream(request with { FromSequence = 1 }, source));
    }

    /// <summary>Only the named target detail and tenant index ETags may change; both tenants' bytes and watermarks remain exact.</summary>
    [Fact]
    public void ReplayComparisonRejectsForbiddenWritesAndAlteredWatermarksInBothTenants()
    {
        string[] tenants = ["primary-secret", "foreign-secret"];
        SupportedProjectReadStateSnapshot[] before = ReplayState(tenants);
        string[] expected = [before[0].Key, before[1].Key];
        SupportedProjectReadStateSnapshot[] after = before.Select((item, index) => index < 2 ? item with { ETag = "new-secret-etag" } : item).ToArray();
        bool Valid(SupportedProjectReadStateSnapshot[]? values, string[]? allowed = null) => SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, values, tenants, allowed ?? expected, before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean();
        Assert.True(Valid(after));
        Assert.True(SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, before, tenants, [], before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean());
        Assert.False(Valid(before), "A rebuild with no ETag change qualified.");
        Assert.False(Valid(null));
        Assert.False(Valid([]));
        Assert.False(Valid(after.Skip(1).ToArray()));
        Assert.False(Valid([.. after, new("new-secret-key", [1], "secret-etag")]));
        Assert.False(Valid([.. after.Skip(1), after[1]]), "Duplicate state observations qualified.");
        Assert.False(Valid(after, [before[0].Key]));
        SupportedProjectReadStateSnapshot[] projectionsOnly = before.Take(6).ToArray();
        Assert.False(SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(projectionsOnly, projectionsOnly, tenants, [],
            before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean(), "Consistently omitted non-projection domain keys qualified.");
        for (int index = 0; index < before.Length; index++)
            foreach (SupportedProjectReadStateSnapshot changed in new[]
            {
                after[index] with { Bytes = "{\"globalPosition\":999,\"description\":\"protected-secret\"}"u8.ToArray() },
                after[index] with { Bytes = "{\"globalPosition\":0}"u8.ToArray() }, after[index] with { Bytes = "["u8.ToArray() },
                after[index] with { Bytes = [] }, after[index] with { ETag = "" }, after[index] with { Key = "replaced-secret-key" },
            })
            {
                SupportedProjectReadStateSnapshot[] invalid = after.ToArray();
                invalid[index] = changed;
                Assert.False(Valid(invalid), "Forbidden state changes or missing watermarks qualified.");
            }
        foreach (int index in Enumerable.Range(2, before.Length - 2))
        {
            SupportedProjectReadStateSnapshot[] invalid = after.ToArray();
            invalid[index] = invalid[index] with { ETag = "unexpected-secret-etag" };
            Assert.False(Valid(invalid), "A sibling or foreign tenant ETag write qualified.");
        }
        Assert.False(SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, after, [tenants[0]], expected, before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean());
        Assert.False(SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, after, [tenants[0], tenants[0]], expected, before.Select(item => item.Key).ToArray()).GetProperty("valid").GetBoolean());
    }

    /// <summary>Incomplete replay evidence remains explicit and sanitized without retaining keys, ETags, metadata or error bodies.</summary>
    [Fact]
    public void ReplayStateEvidenceRetainsExactWatermarksAndSanitizedFailureObservations()
    {
        string[] tenants = ["primary-secret", "foreign-secret"];
        SupportedProjectReadStateSnapshot[] before = ReplayState(tenants);
        foreach (SupportedProjectReadStateSnapshot[]? after in new SupportedProjectReadStateSnapshot[]?[] { before, null, before.Skip(1).ToArray() })
        {
            JsonElement evidence = SupportedProjectReadDistributedFixture.CreateReplayStateEvidence(before, after, tenants, [], before.Select(item => item.Key).ToArray());
            Assert.DoesNotContain("secret", evidence.GetRawText(), StringComparison.Ordinal);
            Assert.Equal(after == before, evidence.GetProperty("valid").GetBoolean());
            Assert.Equal(6, evidence.GetProperty("beforeWatermarks").GetArrayLength());
            Assert.Equal(6, evidence.GetProperty("afterWatermarks").GetArrayLength());
            Assert.Equal(Enumerable.Range(1, 6).Select(value => (long)value), evidence.GetProperty("beforeWatermarks").EnumerateArray().Select(value => value.GetProperty("globalPosition").GetInt64()));
        }
    }

    /// <summary>A failed cursor page retains AD-32 diagnostics without payloads, identifiers, token bytes or unrecognized metadata.</summary>
    [Fact]
    public void CursorControlDiagnosticsRetainUnavailableStateWithoutProtectedPayloads()
    {
        AdmissionSnapshot snapshot = new(AdmissionResponseState.Unavailable, DateTimeOffset.UtcNow, 0,
            [new("FolderVisibility", false, EvidenceFreshnessState.Unavailable, "FolderUnavailable"),
             new("secret-component", false, EvidenceFreshnessState.Unavailable, "secret-reason")], []);
        ProjectListPage page = new([new("secret-project", "secret-name", ProjectLifecycle.Active, ReferenceState.Included, snapshot)], "secret-token", false, snapshot);
        JsonElement evidence = SupportedProjectReadDistributedFixture.CreateCursorControlEvidence(page);
        Assert.DoesNotContain("secret", evidence.GetRawText(), StringComparison.Ordinal);
        Assert.Equal("Unavailable", evidence.GetProperty("responseState").GetString());
        Assert.False(evidence.GetProperty("hasMore").GetBoolean());
        Assert.True(evidence.GetProperty("cursorPresent").GetBoolean());
        Assert.Equal("FolderVisibility", evidence.GetProperty("components")[0].GetProperty("name").GetString());
        Assert.Equal("FolderUnavailable", evidence.GetProperty("components")[0].GetProperty("reason").GetString());
        Assert.Equal("Other", evidence.GetProperty("components")[1].GetProperty("name").GetString());
        Assert.Equal("Other", evidence.GetProperty("components")[1].GetProperty("reason").GetString());
    }

    /// <summary>Honest empty Unavailable observations are retained until a definitive refusal; they never establish acceptance.</summary>
    [Fact]
    public async Task CursorObserverRetainsUnavailableAttemptsBeforeDefinitiveRefusalAsync()
    {
        List<JsonElement> observations = [];
        int calls = 0;
        await SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(_ => Task.FromResult(++calls < 3
            ? CursorResponse(AdmissionResponseState.Unavailable)
            : InvalidCursorResponse()),
            (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Equal(3, calls);
        Assert.Equal("query_invalid_page", observations[^1].GetProperty("problem").GetProperty("reasonCode").GetString());
        Assert.Equal("invalid-cursor", observations[^1].GetProperty("problem").GetProperty("reason").GetString());
        Assert.True(observations[^1].GetProperty("observerDeadline").GetDateTimeOffset()
            - observations[^1].GetProperty("observerStartedAt").GetDateTimeOffset() <= TimeSpan.FromSeconds(90));
        Assert.Equal(new[] { 200, 200, 400 }, observations.Select(value => value.GetProperty("httpStatus").GetInt32()));
        Assert.All(observations.Take(2), value => Assert.True(value.GetProperty("honestEmptyUnavailable").GetBoolean()));
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(observations), StringComparison.Ordinal);
    }

    /// <summary>Complete/Partial continuations and populated or cursor-bearing Unavailable pages fail immediately.</summary>
    [Theory]
    [InlineData("complete")]
    [InlineData("partial")]
    [InlineData("populated")]
    [InlineData("cursor")]
    [InlineData("has-more")]
    [InlineData("transport")]
    public async Task CursorObserverNeverRetriesAcceptedOrDishonestResponsesAsync(string scenario)
    {
        List<JsonElement> observations = [];
        int calls = 0;
        Task<System.Net.Http.HttpResponseMessage> Query(CancellationToken _)
        {
            calls++;
            return scenario == "transport" ? Task.FromException<System.Net.Http.HttpResponseMessage>(new HttpRequestException("secret-error"))
                : Task.FromResult(CursorResponse(scenario == "complete" ? AdmissionResponseState.Complete : scenario == "partial" ? AdmissionResponseState.Partial : AdmissionResponseState.Unavailable,
                    scenario == "populated", scenario == "cursor", scenario == "has-more"));
        }
        await Assert.ThrowsAnyAsync<Exception>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(Query,
            (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.Equal(1, calls);
        Assert.Single(observations);
        Assert.False(observations[0].GetProperty("honestEmptyUnavailable").GetBoolean());
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(observations), StringComparison.Ordinal);
    }

    /// <summary>Unavailable-only observations cannot pass on deadline or after the original cursor lifetime.</summary>
    [Fact]
    public async Task CursorObserverRequiresRefusalWithinBothBudgetsAsync()
    {
        foreach (bool expired in new[] { false, true })
        {
            List<JsonElement> observations = [];
            await Assert.ThrowsAnyAsync<Exception>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
                _ => Task.FromResult(expired ? InvalidCursorResponse() : CursorResponse(AdmissionResponseState.Unavailable)),
                (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow.AddMinutes(expired ? -16 : 0),
                TimeSpan.FromMilliseconds(30), TestContext.Current.CancellationToken));
            Assert.NotEmpty(observations);
        }
    }

    /// <summary>The new observer propagates caller cancellation and retains the failed attempt without error payloads.</summary>
    [Fact]
    public async Task CursorObserverPreservesCallerCancellationAsync()
    {
        List<JsonElement> observations = [];
        using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            async token => { await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false); return CursorResponse(AdmissionResponseState.Unavailable); },
            (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5), cancellation.Token));
        Assert.Single(observations);
        Assert.Equal(JsonValueKind.Null, observations[0].GetProperty("httpStatus").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, observations[0].GetProperty("failureType").ValueKind);
    }

    /// <summary>A stalled query which ignores cancellation cannot extend the observer's whole-operation budget.</summary>
    [Fact]
    public async Task CursorObserverBoundsAStalledQueryAsync()
    {
        List<JsonElement> observations = [];
        TaskCompletionSource<System.Net.Http.HttpResponseMessage> stalled = new();
        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            _ => stalled.Task, (value, _) => { observations.Add(value); return Task.CompletedTask; }, DateTimeOffset.UtcNow,
            TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken));
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), "The stalled cursor query exceeded its short control budget.");
        Assert.Single(observations);
        Assert.Equal(JsonValueKind.Null, observations[0].GetProperty("httpStatus").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, observations[0].GetProperty("failureType").ValueKind);
    }

    /// <summary>Only the actual safe platform Problem Details classification proves cursor invalidation.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("[")]
    [InlineData("{}")]
    [InlineData("{\"status\":400,\"reasonCode\":\"query_invalid_page\",\"reason\":\"invalid-page\"}")]
    [InlineData("{\"status\":400,\"reasonCode\":\"query_malformed_request\",\"reason\":\"invalid-cursor\"}")]
    [InlineData("{\"status\":\"400\",\"reasonCode\":\"query_invalid_page\",\"reason\":\"invalid-cursor\"}")]
    public async Task CursorObserverRejectsUnclassifiedAndMalformedBadRequestsAsync(string body)
    {
        List<JsonElement> observations = [];
        await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            _ => Task.FromResult(InvalidCursorResponse(body)), (value, _) => { observations.Add(value); return Task.CompletedTask; },
            DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.Single(observations);
        Assert.Equal(JsonValueKind.Null, observations[0].GetProperty("problem").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, observations[0].GetProperty("failureType").ValueKind);
    }

    /// <summary>Even a valid refusal cannot qualify when asynchronous evidence recording stalls beyond the observer budget.</summary>
    [Fact]
    public async Task CursorObserverBoundsStalledEvidenceRecordingAsync()
    {
        TaskCompletionSource stalled = new();
        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            _ => Task.FromResult(InvalidCursorResponse()), (_, token) => { Assert.True(token.CanBeCanceled); return stalled.Task; },
            DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken));
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), "Stalled evidence recording extended the observer budget.");
    }

    /// <summary>Cancellation or elapsed observer time during recording is rechecked before reporting success.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CursorObserverRejectsLateOrCancelledSuccessAsync(bool cancelled)
    {
        using CancellationTokenSource cancellation = new();
        Task Observe() => SupportedProjectReadDistributedFixture.ObserveRebuildCursorInvalidationAsync(
            _ => Task.FromResult(InvalidCursorResponse()), (_, _) =>
            {
                if (cancelled) cancellation.Cancel();
                else Thread.Sleep(100);
                return Task.CompletedTask;
            }, DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(50), cancellation.Token);
        if (cancelled) await Assert.ThrowsAnyAsync<OperationCanceledException>(Observe);
        else await Assert.ThrowsAsync<InvalidOperationException>(Observe);
    }

    private static System.Net.Http.HttpResponseMessage InvalidCursorResponse(string body = "{\"status\":400,\"reasonCode\":\"query_invalid_page\",\"reason\":\"invalid-cursor\",\"detail\":\"secret-not-retained\"}")
        => new(System.Net.HttpStatusCode.BadRequest) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/problem+json") };

    private static System.Net.Http.HttpResponseMessage CursorResponse(AdmissionResponseState state, bool populated = false, bool cursor = false, bool hasMore = false)
    {
        AdmissionSnapshot snapshot = new(state, DateTimeOffset.UtcNow, 0,
            [new("FolderVisibility", false, EvidenceFreshnessState.Unavailable, "FolderUnavailable")], []);
        ProjectListPage page = new(populated ? [new("secret-project", "secret-name", ProjectLifecycle.Active, ReferenceState.Included, snapshot)] : [],
            cursor ? "secret-cursor" : null, hasMore, snapshot);
        return new(System.Net.HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(
            new Hexalith.EventStore.Contracts.Queries.SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(page, JsonSerializerOptions.Web))) };
    }

    /// <summary>Batch decoding preserves binary/escaping boundaries and refuses missing, duplicate or malformed provider observations.</summary>
    [Fact]
    public void ProviderBatchDecodingPreservesExactBytesAndVersionsAndRejectsMissingEvidence()
    {
        string[] keys = ["owned||first", "owned||second"];
        byte[] binary = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        JsonArray rows = JsonSerializer.SerializeToNode(new[]
        {
            new { key = keys[0], exists = true, dataHex = Convert.ToHexString(binary), version = "9223372036854775807" },
            new { key = keys[1], exists = true, dataHex = "", version = "0002" },
        })!.AsArray();
        SupportedProjectReadStateSnapshot[] Parse(JsonNode value) => SupportedProjectReadDistributedFixture.ParseRawStateBatch(JsonSerializer.SerializeToUtf8Bytes(value), keys);
        SupportedProjectReadStateSnapshot[] actual = Parse(rows);
        Assert.Equal(binary, actual[0].Bytes);
        Assert.Equal("9223372036854775807", actual[0].ETag);
        Assert.Empty(actual[1].Bytes);
        Assert.Equal("0002", actual[1].ETag);
        foreach ((string field, object value) in new (string, object)[]
        {
            ("key", "foreign||key"), ("key", keys[1]), ("exists", false), ("dataHex", false), ("dataHex", "0"),
            ("dataHex", "GG"), ("version", false), ("version", "0"), ("version", "-1"), ("version", "1.5"), ("version", "9223372036854775808"),
        })
        {
            JsonArray invalid = rows.DeepClone().AsArray();
            invalid[0]![field] = JsonSerializer.SerializeToNode(value);
            Assert.ThrowsAny<Exception>(() => Parse(invalid));
        }
        foreach (string field in new[] { "key", "exists", "dataHex", "version" })
        {
            JsonArray invalid = rows.DeepClone().AsArray();
            invalid[0]!.AsObject().Remove(field);
            Assert.ThrowsAny<Exception>(() => Parse(invalid));
        }
        Assert.ThrowsAny<Exception>(() => Parse(new JsonArray(rows[0]!.DeepClone())));
    }

    private static SupportedProjectReadStateSnapshot[] ReplayState(string[] tenants)
        => tenants.SelectMany(tenant => new[]
        {
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Index(tenant),
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Detail(tenant, "project-a"),
            Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Detail(tenant, "project-b"),
        }).Select((key, index) => new SupportedProjectReadStateSnapshot(key,
            JsonSerializer.SerializeToUtf8Bytes(new { globalPosition = index + 1, description = "protected-secret-payload" }), "secret-etag"))
            .Concat(tenants.Select(tenant => new SupportedProjectReadStateSnapshot("journal:" + tenant, "protected-secret-journal"u8.ToArray(), "secret-etag"))).ToArray();

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
