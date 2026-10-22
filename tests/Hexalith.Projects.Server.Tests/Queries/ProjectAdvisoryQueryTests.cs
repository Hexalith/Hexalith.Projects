namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Projects.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Shouldly;
using Xunit;

public sealed class ProjectAdvisoryQueryTests
{
    [Fact]
    public async Task ConversationLinkAndMetadataProduceDeterministicUnselectedSuggestionsWithoutWrites()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-b"); await f.SeedAsync("project-a");
        var owner = new AdvisoryTestConversationDirectory(); var guard = new AdvisoryGuardedReadStore(f.Store);
        var executor = new ProjectAdvisoryQueryExecutor(guard, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        byte[] before = f.Store.SnapshotRaw("statestore", ProjectReadModelKeys.Detail("tenant-a", "project-a"))!;
        int count = f.Store.Count;
        var result = Read(await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Result.ShouldBe(ResolutionResult.MultipleCandidates); result.Incomplete.ShouldBeFalse();
        result.Candidates.Select(x => x.ProjectId).ShouldBe(["project-a", "project-b"]);
        result.Candidates.Select(x => x.Score).ShouldBe([50, 20]); result.Candidates.Select(x => x.Rank).ShouldBe([1, 2]);
        owner.Calls.ShouldBe(2); f.Store.Count.ShouldBe(count); f.Store.SnapshotRaw("statestore", ProjectReadModelKeys.Detail("tenant-a", "project-a")).ShouldBe(before);
        string json = JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
        foreach (string field in new[] { "displayName", "excluded", "selected", "snapshot", "safeLabel" }) json.ShouldNotContain(field);
    }

    [Fact]
    public async Task DuplicateAndPermutedAttachmentsHaveIdenticalScoresReasonsAndOrdering()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-b"); await f.SeedAsync("project-a");
        var executor = Executor(f);
        ProjectAdvisoryAttachment a = new("folder", "folder-project-a"), b = new("folder", "folder-project-b");
        var first = Read(await executor.FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([b, a, b])), TestContext.Current.CancellationToken));
        var second = Read(await executor.FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([a, b])), TestContext.Current.CancellationToken));
        JsonSerializer.Serialize(first).ShouldBe(JsonSerializer.Serialize(second));
        first.Candidates.Select(x => x.ProjectId).ShouldBe(["project-a", "project-b"]);
        first.Candidates.ShouldAllBe(x => x.Score == 45 && x.ReasonCodes.Single() == ProjectReasonCode.ProjectFolderMatched);
    }

    [Fact]
    public async Task RawOversizeIsRejectedBeforeDeduplication()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); var guard = new AdvisoryGuardedReadStore(f.Store);
        var result = await Executor(f, guard).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery(
            Enumerable.Repeat(new ProjectAdvisoryAttachment("folder", "same"), 33).ToArray())), TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope); guard.Reads.ShouldBeEmpty(); f.Folders.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task UnsupportedFilesAreExplicitAndEmptyIncompleteCannotClaimNoMatch()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        var result = Read(await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("file", "opaque-file")])), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Result.ShouldBeNull(); result.Candidates.ShouldBeEmpty(); result.UnsupportedInputKinds.ShouldBe(["file"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeniedAndUnknownFoldersNeverScore(bool unknown)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string id = unknown ? "missing" : "folder-project-a"; f.Folders.Denied.Add(id);
        var result = Read(await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", id)])), TestContext.Current.CancellationToken));
        result.Candidates.ShouldBeEmpty(); result.Result.ShouldBe(ResolutionResult.NoMatch); result.Incomplete.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ArchivedCandidatesRequireExplicitOptInAndOwnerAccess(bool include)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a"); var model = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        f.Store.SeedRaw("statestore", key, model with { Detail = model.Detail! with { Lifecycle = ProjectLifecycle.Archived } });
        var result = Read(await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")], include)), TestContext.Current.CancellationToken));
        result.Candidates.Count.ShouldBe(include ? 1 : 0);
        f.Folders.Denied.Add("folder-project-a");
        Read(await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")], true)), TestContext.Current.CancellationToken)).Candidates.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(EvidenceFreshnessState.Stale, true)]
    [InlineData(EvidenceFreshnessState.Rebuilding, false)]
    [InlineData(EvidenceFreshnessState.Unavailable, false)]
    public async Task LagCanSuggestWhileUnusableProjectionStatesCannot(EvidenceFreshnessState freshness, bool candidate)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a"); var model = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        f.Store.SeedRaw("statestore", key, model with { Freshness = freshness });
        var result = Read(await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBe(!candidate); result.Candidates.Count.ShouldBe(candidate ? 1 : 0);
    }

    [Fact]
    public async Task MissingViewAndUnavailableOwnerReturnIncomplete()
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        var query = Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")]));
        Read(await Executor(f).FromAttachmentsAsync(query, TestContext.Current.CancellationToken)).Result.ShouldBeNull();
        await f.SeedAsync("project-a"); f.Folders.Outcomes["folder-project-a"] = ProjectFolderValidationOutcome.Unavailable;
        var result = Read(await Executor(f).FromAttachmentsAsync(query, TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Candidates.ShouldBeEmpty(); result.Result.ShouldBeNull();
    }

    [Fact]
    public async Task RevokedInputIsFilteredBeforeMappingEvenWhenCandidateFolderLaterAllows()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a"); int reads = 0;
        f.Folders.OnRead = id => { int n = Interlocked.Increment(ref reads); if (n == 3) f.Folders.Denied.Add(id); if (n == 4) f.Folders.Denied.Remove(id); return Task.CompletedTask; };
        var result = Read(await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])), TestContext.Current.CancellationToken));
        result.Candidates.ShouldBeEmpty(); reads.ShouldBe(3);
    }

    [Fact]
    public async Task ConversationRevocationAndCandidateFolderRevocationDoNotDisclose()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        var owner = new AdvisoryTestConversationDirectory { OnRead = n => n == 2 ? QueryResult.Failure("safe-denial")
            : AdvisoryTestConversationDirectory.Result(new(new("conversation", "project-a", null, "Open"), false)) };
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        (await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
        int reads = 0; f.Folders.OnRead = id => { if (Interlocked.Increment(ref reads) == 2) f.Folders.Denied.Add(id); return Task.CompletedTask; };
        Read(await Executor(f).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken)).Candidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task BoundScanUsesAtMost200DetailsInOrdinalOrderAndReportsTruncation()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string[] ids = Enumerable.Range(0, 205).Select(n => $"p{n:000}").Reverse().ToArray();
        f.Store.SeedRaw("statestore", ProjectReadModelKeys.Index("tenant-a"), new ProjectReadIndex("tenant-a", ids, 1, f.Clock.GetUtcNow()));
        var guard = new AdvisoryGuardedReadStore(f.Store);
        var result = Read(await Executor(f, guard).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])), TestContext.Current.CancellationToken));
        var details = guard.Reads.Where(key => !key.EndsWith(":index", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        details.Length.ShouldBe(200); details.ShouldContain(ProjectReadModelKeys.Detail("tenant-a", "p000"));
        details.ShouldNotContain(ProjectReadModelKeys.Detail("tenant-a", "p200")); result.Incomplete.ShouldBeTrue();
    }

    [Fact]
    public async Task DependencyConcurrencyIsFourAndOneDeadlineBoundsUncooperativeReads()
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        for (int n = 0; n < 8; n++) await f.SeedAsync($"p{n}");
        int active = 0, maximum = 0;
        var guard = new AdvisoryGuardedReadStore(f.Store) { BeforeRead = async (key, token) =>
        {
            if (key.EndsWith(":index", StringComparison.Ordinal)) return;
            int current = Interlocked.Increment(ref active); InterlockedExtensionsMaximum(ref maximum, current);
            await Task.Delay(20, token).ConfigureAwait(false); Interlocked.Decrement(ref active);
        }};
        await Executor(f, guard).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])), TestContext.Current.CancellationToken);
        maximum.ShouldBeInRange(1, 4);
        guard.BeforeRead = (_, _) => Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Stopwatch elapsed = Stopwatch.StartNew();
        var result = Read(await Executor(f, guard).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])), TestContext.Current.CancellationToken));
        elapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(4)); result.Incomplete.ShouldBeTrue(); result.Result.ShouldBeNull();
    }

    [Fact]
    public async Task CallerCancellationAndIdentityMismatchCannotBecomeSuggestions()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var query = Query(f, new ResolveProjectFromAttachmentsQuery([]));
        await Should.ThrowAsync<OperationCanceledException>(() => Executor(f).FromAttachmentsAsync(query, cancelled.Token));
        foreach (var malformed in new[] { query with { TenantId = "foreign" }, query with { OriginalActorId = "other" }, query with { AuthenticatedWorkloadId = null }, query with { DelegationId = "other" } })
            (await Executor(f).FromAttachmentsAsync(malformed, TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
    }

    [Fact]
    public async Task ExactProjectAuthorizationPrecedesMetadataAndIsRepeatedBeforeDisclosure()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        int calls = 0;
        f.Authorization.Decision = request => request.ProjectId == "project-a" && Interlocked.Increment(ref calls) >= 1
            ? EventStoreAuthorizationValidationResult.Denied() : EventStoreAuthorizationValidationResult.Allowed("test-current");
        var guard = new AdvisoryGuardedReadStore(f.Store);
        var query = Query(f, new ResolveProjectFromConversationQuery("conversation"));
        Read(await Executor(f, guard).FromConversationAsync(query, TestContext.Current.CancellationToken)).Candidates.ShouldBeEmpty();
        guard.Reads.ShouldNotContain(ProjectReadModelKeys.Detail("tenant-a", "project-a"));
        calls = 0;
        f.Authorization.Decision = request => request.ProjectId == "project-a" && Interlocked.Increment(ref calls) == 2
            ? EventStoreAuthorizationValidationResult.Denied() : EventStoreAuthorizationValidationResult.Allowed("test-current");
        Read(await Executor(f).FromConversationAsync(query, TestContext.Current.CancellationToken)).Candidates.ShouldBeEmpty();
        calls.ShouldBe(2);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"metadata\":null,\"incomplete\":false}")]
    [InlineData("{\"metadata\":{\"conversationId\":\"wrong\",\"projectId\":\"project-a\",\"lifecycleState\":\"Open\"},\"incomplete\":false}")]
    public async Task MalformedOwnerMetadataCannotBecomeAnExhaustiveNoMatch(string json)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        var owner = new AdvisoryTestConversationDirectory { OnRead = _ => QueryResult.FromPayload(JsonDocument.Parse(json).RootElement) };
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        var result = Read(await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Candidates.ShouldBeEmpty(); result.Result.ShouldBeNull();
    }

    [Fact]
    public async Task AuthorizedConversationHintReservesOneOfThe200DetailSlots()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        f.Store.SeedRaw("statestore", ProjectReadModelKeys.Index("tenant-a"), new ProjectReadIndex("tenant-a",
            Enumerable.Range(0, 205).Select(n => $"p{n:000}").ToArray(), 1, f.Clock.GetUtcNow()));
        var guard = new AdvisoryGuardedReadStore(f.Store);
        var result = Read(await Executor(f, guard).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Candidates.Single().ProjectId.ShouldBe("project-a"); result.Incomplete.ShouldBeTrue();
        guard.Reads.Where(key => !key.EndsWith(":index", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).Count().ShouldBe(200);
    }

    [Fact]
    public async Task SharedDeadlineIsNotRestartedBetweenDependentReads()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        var guard = new AdvisoryGuardedReadStore(f.Store) { BeforeRead = (_, token) => Task.Delay(1200, token) };
        Stopwatch elapsed = Stopwatch.StartNew();
        var result = Read(await Executor(f, guard).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); elapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task FinalDisclosureUsesTheNewCommittedFolderAnchorAndLifecycle()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var before = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        f.Folders.Denied.Add("new-denied-folder");
        f.Folders.OnRead = _ =>
        {
            f.Store.SeedRaw("statestore", key, before with { Detail = before.Detail! with
                { ProjectFolder = before.Detail.ProjectFolder! with { FolderId = "new-denied-folder" } } });
            return Task.CompletedTask;
        };
        var query = Query(f, new ResolveProjectFromConversationQuery("conversation"));
        Read(await Executor(f).FromConversationAsync(query, TestContext.Current.CancellationToken)).Candidates.ShouldBeEmpty();
        f.Store.SeedRaw("statestore", key, before);
        f.Folders.OnRead = _ =>
        {
            f.Store.SeedRaw("statestore", key, before with { Detail = before.Detail! with { Lifecycle = ProjectLifecycle.Archived } });
            return Task.CompletedTask;
        };
        Read(await Executor(f).FromConversationAsync(query, TestContext.Current.CancellationToken)).Candidates.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ReferenceState.Pending)]
    [InlineData(ReferenceState.Conflict)]
    [InlineData((ReferenceState)999)]
    public async Task UnacceptedCommittedFolderBindingCannotBecomeACandidate(ReferenceState state)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var model = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        f.Store.SeedRaw("statestore", key, model with { Detail = model.Detail! with
            { ProjectFolder = model.Detail.ProjectFolder! with { ReferenceState = state } } });
        var result = Read(await Executor(f).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Candidates.ShouldBeEmpty(); result.Incomplete.ShouldBeTrue(); f.Folders.Reads.ShouldBe(0);
    }

    [Theory]
    [InlineData("missing-name")]
    [InlineData("blank-name")]
    [InlineData("untrimmed-name")]
    [InlineData("unsafe-name")]
    [InlineData("oversized-name")]
    [InlineData("folder-id")]
    [InlineData("lifecycle")]
    public async Task MalformedConsumedScoringHeaderCannotBecomeACandidate(string mutation)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var model = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        var detail = model.Detail!;
        detail = mutation switch
        {
            "lifecycle" => detail with { Lifecycle = (ProjectLifecycle)999 },
            "missing-name" => detail with { Name = null! },
            "blank-name" => detail with { Name = " " },
            "untrimmed-name" => detail with { Name = " Project project-a " },
            "unsafe-name" => detail with { Name = "credential payload" },
            "oversized-name" => detail with { Name = new string('x', 161) },
            _ => detail with { ProjectFolder = detail.ProjectFolder! with { FolderId = "/private/path" } },
        };
        f.Store.SeedRaw("statestore", key, model with { Detail = detail });
        var result = Read(await Executor(f).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Candidates.ShouldBeEmpty(); result.Incomplete.ShouldBeTrue(); f.Folders.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task AdvisoryHeaderDoesNotRequireUnconsumedSetupOrContextEvidence()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var model = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        f.Store.SeedRaw("statestore", key, model with { Detail = model.Detail! with
            { Setup = null, SetupMetadata = "unconsumed/legacy", FileReferences = null!, MemoryReferences = null! } });
        var result = Read(await Executor(f).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Candidates.Single().ProjectId.ShouldBe("project-a"); result.Incomplete.ShouldBeFalse();
    }

    [Fact]
    public async Task FailedFinalCandidateReadRetainsSuccessfullyFinalizedPeer()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a"); await f.SeedAsync("project-b");
        ConcurrentDictionary<string, int> reads = new(StringComparer.Ordinal);
        string failedKey = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var guard = new AdvisoryGuardedReadStore(f.Store) { BeforeRead = (key, _) =>
        {
            if (reads.AddOrUpdate(key, 1, (_, count) => count + 1) == 2 && key == failedKey)
                throw new IOException("Final candidate unavailable.");
            return Task.CompletedTask;
        } };
        var result = Read(await Executor(f, guard).FromAttachmentsAsync(Query(f,
            new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a"), new("folder", "folder-project-b")])), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Candidates.Single().ProjectId.ShouldBe("project-b");
        result.Candidates.Single().Score.ShouldBe(45); result.Candidates.Single().Rank.ShouldBe(1);
        reads[failedKey].ShouldBe(2); reads[ProjectReadModelKeys.Detail("tenant-a", "project-b")].ShouldBe(2);
    }

    [Fact]
    public async Task FinalConversationObservationReplacesInitialScoringEvidence()
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        foreach (string id in new[] { "project-a", "project-b", "project-c" }) await f.SeedAsync(id);
        var owner = new AdvisoryTestConversationDirectory { OnRead = n => AdvisoryTestConversationDirectory.Result(
            n == 1 ? new(new("conversation", "project-a", "Project project-b", "Open"), false)
                : new(new("conversation", "project-b", "Project project-c", "Open"), false)) };
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        var result = Read(await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Candidates.Select(candidate => candidate.ProjectId).ShouldBe(["project-b", "project-c"]);
        result.Candidates.Select(candidate => candidate.Score).ShouldBe([50, 20]);
        result.Candidates[0].ReasonCodes.ShouldBe([ProjectReasonCode.ConversationLinked]);
        result.Candidates[1].ReasonCodes.ShouldBe([ProjectReasonCode.MetadataMatched]); owner.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task LargeDuplicateIndexKeepsOrdinalSelectionAndLinkedHintWithin200Slots()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        int enumerated = 0;
        var guard = new AdvisoryGuardedReadStore(f.Store) { IndexIdentities = new AdvisoryIndexIdentities(200000,
            index => $"p{(199999 - index) / 2:000000}", _ => enumerated++) };
        var result = Read(await Executor(f, guard).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        string[] details = guard.Reads.Where(key => !key.EndsWith(":index", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        details.ShouldBe(Enumerable.Range(0, 199).Select(n => ProjectReadModelKeys.Detail("tenant-a", $"p{n:000000}"))
            .Append(ProjectReadModelKeys.Detail("tenant-a", "project-a")).Order(StringComparer.Ordinal));
        enumerated.ShouldBe(200000); result.Incomplete.ShouldBeTrue(); result.Candidates.Single().ProjectId.ShouldBe("project-a");
    }

    [Fact]
    public async Task CancellationDuringIndexScanStopsBeforeAdvancingToAnotherIdentity()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        using var cancellation = new CancellationTokenSource(); int enumerated = 0;
        var guard = new AdvisoryGuardedReadStore(f.Store) { IndexIdentities = new AdvisoryIndexIdentities(200000, index => $"p{index:000000}", _ =>
        {
            enumerated++;
            if (enumerated == 25) cancellation.Cancel();
            if (enumerated > 25) throw new InvalidOperationException("Advanced after cancellation.");
        }) };
        await Should.ThrowAsync<OperationCanceledException>(() => Executor(f, guard).FromAttachmentsAsync(
            Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])), cancellation.Token));
        enumerated.ShouldBe(25); guard.Reads.ShouldBe([ProjectReadModelKeys.Index("tenant-a")]);
    }

    [Theory]
    [InlineData("safe-denial")]
    [InlineData("ActorException")]
    public async Task InitialConversationFailureSeparatesDenialFromUnavailable(string error)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        var owner = new AdvisoryTestConversationDirectory { OnRead = _ => QueryResult.Failure(error) };
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        var result = await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken);
        if (error == "safe-denial") result.ErrorMessage.ShouldBe("safe-denial");
        else { var payload = Read(result); payload.Incomplete.ShouldBeTrue(); payload.Result.ShouldBeNull(); }
        owner.Calls.ShouldBe(1); f.Folders.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task DegradedIndexAndRetryableProjectAuthorizationCannotClaimExhaustiveNoMatch()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        var query = Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "unmatched")]));
        string key = ProjectReadModelKeys.Index("tenant-a");
        var index = f.Store.Snapshot<ProjectReadIndex>("statestore", key)!;
        f.Store.SeedRaw("statestore", key, index with { UnavailableProjectIds = ["missing"] });
        var degraded = Read(await Executor(f).FromAttachmentsAsync(query, TestContext.Current.CancellationToken));
        degraded.Incomplete.ShouldBeTrue(); degraded.Result.ShouldBeNull();
        f.Store.SeedRaw("statestore", key, index); f.Authorization.UnavailableProject = "project-a";
        var unavailable = Read(await Executor(f).FromAttachmentsAsync(query, TestContext.Current.CancellationToken));
        unavailable.Incomplete.ShouldBeTrue(); unavailable.Result.ShouldBeNull();
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("audience")]
    [InlineData("role")]
    public async Task MissingSupportedReadAdmissionNeverCallsOwners(string missing)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        var query = Query(f, new ResolveProjectFromConversationQuery("conversation"));
        string claim = missing == "scope" ? "scope" : missing == "audience" ? "aud" : "projects:role";
        foreach (var item in f.Identity.FindAll(claim).ToArray()) f.Identity.RemoveClaim(item);
        if (missing == "scope") query = query with { Scopes = null };
        if (missing == "audience") query = query with { Audience = null };
        var owner = new AdvisoryTestConversationDirectory();
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        (await executor.FromConversationAsync(query, TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
        owner.Calls.ShouldBe(0); f.Folders.Reads.ShouldBe(0);
    }

    [Theory]
    [InlineData("removed")]
    [InlineData("revoked")]
    [InlineData("stale")]
    [InlineData("unavailable")]
    public async Task TenantAdmissionOrdersOwnerReadsAndRechecksBeforeDisclosure(string state)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        var tenant = (await f.Tenants.GetAsync("tenant-a", TestContext.Current.CancellationToken))!;
        if (state == "removed") tenant.Principals.Clear();
        if (state == "stale") tenant.LastEventTimestamp = f.Clock.Now.AddHours(-1);
        if (state == "unavailable") tenant.ProjectionWatermark = string.Empty;
        await f.Tenants.SaveAsync(tenant, TestContext.Current.CancellationToken);
        var owner = new AdvisoryTestConversationDirectory { OnRead = n =>
        {
            if (state == "revoked" && n == 2)
            {
                tenant = f.Tenants.GetAsync("tenant-a", TestContext.Current.CancellationToken).GetAwaiter().GetResult()!;
                tenant.Principals.Clear();
                f.Tenants.SaveAsync(tenant, TestContext.Current.CancellationToken).GetAwaiter().GetResult();
            }
            return AdvisoryTestConversationDirectory.Result(new(new("conversation", null, null, "Open"), false));
        } };
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        var result = await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken);
        if (state is "removed" or "revoked") result.ErrorMessage.ShouldBe("safe-denial");
        else { var payload = Read(result); payload.Incomplete.ShouldBeTrue(); payload.Result.ShouldBeNull(); }
        if (state == "removed") { owner.Calls.ShouldBe(0); f.Folders.Reads.ShouldBe(0); }
    }

    [Theory]
    [InlineData("referenceKind")]
    [InlineData("unknown")]
    [InlineData("duplicate")]
    [InlineData("aggregate")]
    [InlineData("paging")]
    [InlineData("globalAdmin")]
    [InlineData("blank")]
    [InlineData("untrimmed")]
    [InlineData("payload-size")]
    public async Task InvalidEnvelopesAndInputsAreRejectedBeforeMetadata(string mutation)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); var guard = new AdvisoryGuardedReadStore(f.Store);
        var query = Query(f, new ResolveProjectFromAttachmentsQuery([]));
        query = mutation switch
        {
            "referenceKind" => query with { Payload = "{\"attachments\":[{\"referenceKind\":\"unknown\",\"referenceId\":\"id\"}]}"u8.ToArray() },
            "unknown" => query with { Payload = "{\"attachments\":[],\"extra\":true}"u8.ToArray() },
            "duplicate" => query with { Payload = "{\"attachments\":[],\"attachments\":[]}"u8.ToArray() },
            "aggregate" => query with { AggregateId = "other" },
            "paging" => query with { Paging = new() },
            "globalAdmin" => query with { IsGlobalAdmin = true },
            "blank" => Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", " ")])),
            "untrimmed" => Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", " id ")])),
            _ => query with { Payload = query.Payload.Concat(Enumerable.Repeat((byte)' ', 16385 - query.Payload.Length)).ToArray() },
        };
        (await Executor(f, guard).FromAttachmentsAsync(query, TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        guard.Reads.ShouldBeEmpty(); f.Folders.Reads.ShouldBe(0);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("file")]
    [InlineData("denied")]
    [InlineData("conversation")]
    public async Task InputsWithoutScoringSignalsSkipProjectReadsAndStillRecheckAuthority(string kind)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        var guard = new AdvisoryGuardedReadStore(f.Store) { BeforeRead = (_, _) => throw new InvalidOperationException("Must not scan Projects.") };
        var owner = new AdvisoryTestConversationDirectory { OnRead = _ => AdvisoryTestConversationDirectory.Result(new(new("conversation", null, null, "Open"), false)) };
        var executor = new ProjectAdvisoryQueryExecutor(guard, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        if (kind == "denied") f.Folders.Denied.Add("folder-project-a");
        var result = kind == "conversation"
            ? await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken)
            : await executor.FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery(kind == "empty" ? [] : [new(kind == "file" ? "file" : "folder", "folder-project-a")])), TestContext.Current.CancellationToken);
        var payload = Read(result); payload.Incomplete.ShouldBe(kind == "file");
        payload.Result.ShouldBe(kind == "file" ? null : ResolutionResult.NoMatch); guard.Reads.ShouldBeEmpty();
        if (kind == "conversation") owner.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task UnmatchedCandidateFolderDoesNotCallItsOwner()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a"); await f.SeedAsync("project-b");
        f.Folders.OnRead = id => { id.ShouldBe("folder-project-a"); return Task.CompletedTask; };
        var result = Read(await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])), TestContext.Current.CancellationToken));
        result.Candidates.Single().ProjectId.ShouldBe("project-a"); result.Incomplete.ShouldBeFalse();
    }

    [Fact]
    public async Task SwallowedDependencyFailureLogsOnlyTypeAndCorrelation()
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        var logger = new AdvisoryTestLogger();
        var guard = new AdvisoryGuardedReadStore(f.Store) { BeforeRead = (_, _) => throw new IOException("protected tenant/project/folder label sentinel") };
        var executor = new ProjectAdvisoryQueryExecutor(guard, f.Gate, f.Binding, f.Folders, new AdvisoryTestConversationDirectory(), f.Clock, logger);
        var result = Read(await executor.FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "input")])), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Result.ShouldBeNull();
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(Microsoft.Extensions.Logging.LogLevel.Warning); entry.Exception.ShouldBeNull();
        entry.Message.ShouldContain("IOException"); entry.Message.ShouldContain("read-request"); entry.Message.ShouldNotContain("sentinel");
        entry.Keys.Order(StringComparer.Ordinal).ShouldBe(new[] { "ExceptionType", "CorrelationId", "{OriginalFormat}" }.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public async Task MalformedIndexReceiptsCannotProduceCompleteNoMatch(long position, bool missingObservation)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        f.Store.SeedRaw("statestore", ProjectReadModelKeys.Index("tenant-a"), new ProjectReadIndex("tenant-a", [],
            position, missingObservation ? default : f.Clock.Now.AddDays(-1)));
        var result = Read(await Executor(f).FromAttachmentsAsync(Query(f,
            new ResolveProjectFromAttachmentsQuery([new("folder", "unmatched")])), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Result.ShouldBeNull(); result.Candidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task OldButValidIndexReceiptRemainsUsable()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Index("tenant-a");
        f.Store.SeedRaw("statestore", key, f.Store.Snapshot<ProjectReadIndex>("statestore", key)! with { LastVerifiedAt = f.Clock.Now.AddDays(-1) });
        var result = Read(await Executor(f).FromAttachmentsAsync(Query(f,
            new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])), TestContext.Current.CancellationToken));
        result.Candidates.Single().ProjectId.ShouldBe("project-a"); result.Incomplete.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CallerCancellationDuringNormallyCompletedMissingOwnerMetadataPropagates(int pass)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        using var cancellation = new CancellationTokenSource();
        var owner = new AdvisoryTestConversationDirectory { OnRead = n =>
        {
            if (n != pass) return AdvisoryTestConversationDirectory.Result(new(new("conversation", "project-a", null, "Open"), false));
            cancellation.Cancel();
            return AdvisoryTestConversationDirectory.Result(new(null, true));
        } };
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        await Should.ThrowAsync<OperationCanceledException>(() => executor.FromConversationAsync(
            Query(f, new ResolveProjectFromConversationQuery("conversation")), cancellation.Token));
        owner.Calls.ShouldBe(pass);
    }

    [Theory]
    [InlineData(16384)]
    [InlineData(16385)]
    public async Task ValidJsonPayloadHasAnInclusive16384ByteLimit(int size)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        var query = Query(f, new ResolveProjectFromAttachmentsQuery([]));
        query = query with { Payload = query.Payload.Concat(Enumerable.Repeat((byte)' ', size - query.Payload.Length)).ToArray() };
        query.Payload.Length.ShouldBe(size);
        using var validJson = JsonDocument.Parse(query.Payload);
        validJson.RootElement.GetProperty("attachments").GetArrayLength().ShouldBe(0);
        var result = await Executor(f).FromAttachmentsAsync(query, TestContext.Current.CancellationToken);
        if (size > 16384) result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        else { result.Success.ShouldBeTrue(); Read(result).Result.ShouldBe(ResolutionResult.NoMatch); Read(result).Incomplete.ShouldBeFalse(); }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task AdvisoryRequiresReadAuthorityWithoutRequiringListAuthority(bool conversation, bool listOnly)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        foreach (var claim in f.Identity.Claims.Where(c => c.Type is "scope" or "eventstore:permission").ToArray()) f.Identity.RemoveClaim(claim);
        string scope = listOnly ? "projects.list" : "projects.read";
        f.Identity.AddClaim(new Claim("scope", scope));
        f.Identity.AddClaim(new Claim("eventstore:permission", listOnly ? ProjectAuthorizationGate.ListProjectsAction : ProjectAuthorizationGate.ReadProjectAction));
        var query = Query(f, conversation ? new ResolveProjectFromConversationQuery("conversation")
            : new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")])) with { Scopes = [scope] };
        var result = conversation ? await Executor(f).FromConversationAsync(query, TestContext.Current.CancellationToken)
            : await Executor(f).FromAttachmentsAsync(query, TestContext.Current.CancellationToken);
        if (listOnly) { result.Success.ShouldBeFalse(); result.ErrorMessage.ShouldBe("safe-denial"); f.Folders.Reads.ShouldBe(0); }
        else
        {
            var response = Read(result); response.Incomplete.ShouldBeFalse(); response.Result.ShouldBe(ResolutionResult.SingleCandidate);
            response.Candidates.Single().ProjectId.ShouldBe("project-a"); response.Candidates.Single().Score.ShouldBe(conversation ? 50 : 45);
        }
    }

    private static void InterlockedExtensionsMaximum(ref int target, int value)
    {
        int current; do { current = Volatile.Read(ref target); if (value <= current) return; } while (Interlocked.CompareExchange(ref target, value, current) != current);
    }
    private static ProjectAdvisoryQueryExecutor Executor(SupportedProjectReadFixture f, AdvisoryGuardedReadStore? guard = null)
        => new(guard ?? new(f.Store), f.Gate, f.Binding, f.Folders, new AdvisoryTestConversationDirectory(), f.Clock);
    private static QueryEnvelope Query(SupportedProjectReadFixture f, object input)
        => f.Query(input) with { QueryType = input is ResolveProjectFromConversationQuery ? ResolveProjectFromConversationQuery.QueryType : ResolveProjectFromAttachmentsQuery.QueryType };
    private static ProjectAdvisoryResolutionResponse Read(QueryResult result)
    { result.Success.ShouldBeTrue(result.ErrorMessage); return result.GetPayload().Deserialize<ProjectAdvisoryResolutionResponse>(JsonSerializerOptions.Web)!; }
}
