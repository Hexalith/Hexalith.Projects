namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Shouldly;
using Xunit;

public sealed partial class ProjectAdvisoryQueryTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task AdvisoryPolicyBindsLiteralReadActionAndRechecksTheExactProject(bool conversation, bool revoke)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        await f.SeedAsync("project-a");
        var unchanged = await CapturePolicyStateAsync(f);
        ConcurrentBag<EventStoreAuthorizationValidationRequest> requests = [];
        int resourceChecks = 0;
        f.Authorization.Decision = request =>
        {
            requests.Add(request);
            return request.ProjectId is not null && Interlocked.Increment(ref resourceChecks) == 2 && revoke
                ? EventStoreAuthorizationValidationResult.Denied()
                : EventStoreAuthorizationValidationResult.Allowed("policy-current");
        };
        var executor = Executor(f);
        var query = Query(f, conversation ? new ResolveProjectFromConversationQuery("conversation")
            : new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")]));
        var response = Read(conversation
            ? await executor.FromConversationAsync(query, TestContext.Current.CancellationToken)
            : await executor.FromAttachmentsAsync(query, TestContext.Current.CancellationToken));

        requests.Count.ShouldBe(4);
        requests.ShouldAllBe(request => request.ActionToken == "projects:read"
            && request.TenantId == "tenant-a" && request.PrincipalId == "actor-1"
            && request.CorrelationId == query.CorrelationId);
        requests.Count(request => request.ProjectId is null).ShouldBe(2);
        requests.Count(request => request.ProjectId == "project-a").ShouldBe(2);
        response.Incomplete.ShouldBeFalse();
        response.Result.ShouldBe(revoke ? ResolutionResult.NoMatch : ResolutionResult.SingleCandidate);
        response.Candidates.Select(candidate => candidate.ProjectId).ShouldBe(revoke ? [] : ["project-a"]);
        await unchanged();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ReadScopeCannotReplaceTheRequiredReadPermission(bool conversation, bool substituteListPermission)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        await f.SeedAsync("project-a");
        var unchanged = await CapturePolicyStateAsync(f);
        foreach (var claim in f.Identity.FindAll("eventstore:permission").ToArray()) f.Identity.RemoveClaim(claim);
        if (substituteListPermission) f.Identity.AddClaim(new Claim("eventstore:permission", "projects:list"));
        var guard = new AdvisoryGuardedReadStore(f.Store);
        var owner = new AdvisoryTestConversationDirectory();
        int validatorCalls = 0;
        f.Authorization.Decision = _ =>
        {
            Interlocked.Increment(ref validatorCalls);
            return EventStoreAuthorizationValidationResult.Allowed("must-not-reach");
        };
        var query = Query(f, conversation ? new ResolveProjectFromConversationQuery("conversation")
            : new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")]));
        query.Scopes.ShouldNotBeNull();
        query.Scopes!.ShouldContain("projects.read");
        var executor = Executor(f, guard, owner);
        var result = conversation
            ? await executor.FromConversationAsync(query, TestContext.Current.CancellationToken)
            : await executor.FromAttachmentsAsync(query, TestContext.Current.CancellationToken);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        result.PayloadBytes.ShouldBeNull();
        validatorCalls.ShouldBe(0);
        guard.Reads.ShouldBeEmpty();
        owner.Calls.ShouldBe(0);
        f.Folders.Reads.ShouldBe(0);
        await unchanged();
    }

    [Fact]
    public async Task DeniedMatchingFolderInputIsNoMatchWhileProtectedConversationIsSafeDenial()
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        await f.SeedAsync("project-a");
        await f.SeedAsync("project-b");
        string deniedKey = ProjectReadModelKeys.Detail("tenant-a", "project-b");
        var deniedModel = f.Store.Snapshot<ProjectReadModel>("statestore", deniedKey)!;
        f.Store.SeedRaw("statestore", deniedKey, deniedModel with { Detail = deniedModel.Detail! with
        {
            ProjectFolder = deniedModel.Detail.ProjectFolder! with { FolderId = "foreign-or-denied-folder" },
        } });
        var unchanged = await CapturePolicyStateAsync(f, "project-a", "project-b");
        f.Folders.Deny("foreign-or-denied-folder");
        var guard = new AdvisoryGuardedReadStore(f.Store);
        var owner = new AdvisoryTestConversationDirectory { OnRead = _ => QueryResult.Failure("safe-denial") };
        var executor = Executor(f, guard, owner);
        var empty = Read(await executor.FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([])), TestContext.Current.CancellationToken));
        var rejected = Read(await executor.FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery(
            [new("folder", "foreign-or-denied-folder")])), TestContext.Current.CancellationToken));

        rejected.Result.ShouldBe(ResolutionResult.NoMatch);
        rejected.Incomplete.ShouldBeFalse();
        JsonSerializer.Serialize(rejected).ShouldBe(JsonSerializer.Serialize(empty));
        guard.Reads.ShouldBeEmpty();
        owner.Calls.ShouldBe(0);
        var protectedResult = await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken);
        protectedResult.Success.ShouldBeFalse();
        protectedResult.ErrorMessage.ShouldBe("safe-denial");
        protectedResult.PayloadBytes.ShouldBeNull();
        guard.Reads.ShouldBeEmpty();

        var allowed = Read(await executor.FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery(
            [new("folder", "foreign-or-denied-folder"), new("folder", "folder-project-a")])), TestContext.Current.CancellationToken));
        allowed.Candidates.Single().ProjectId.ShouldBe("project-a");
        allowed.Candidates.Single().Score.ShouldBe(45);
        allowed.Candidates.Single().ReasonCodes.ShouldBe([ProjectReasonCode.ProjectFolderMatched]);
        JsonSerializer.Serialize(allowed).ShouldNotContain("foreign-or-denied-folder");
        await unchanged();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConversationObservationSuggestsWithoutSelectingOrInferringSessionMembership(bool linked)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        await f.SeedAsync("project-a");
        var unchanged = await CapturePolicyStateAsync(f);
        var guard = new AdvisoryGuardedReadStore(f.Store);
        var owner = new AdvisoryTestConversationDirectory
        {
            OnRead = _ => AdvisoryTestConversationDirectory.Result(new(new("conversation", linked ? "project-a" : null, null, "Open"), false)),
        };
        var result = await Executor(f, guard, owner).FromConversationAsync(
            Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken);
        var response = Read(result);
        response.Incomplete.ShouldBeFalse();
        response.Result.ShouldBe(linked ? ResolutionResult.SingleCandidate : ResolutionResult.NoMatch);
        owner.Calls.ShouldBe(2);
        if (linked)
        {
            response.Candidates.Single().ProjectId.ShouldBe("project-a");
            response.Candidates.Single().ReasonCodes.ShouldBe([ProjectReasonCode.ConversationLinked]);
        }
        else
        {
            response.Candidates.ShouldBeEmpty();
            guard.Reads.ShouldBeEmpty();
        }
        AssertAdvisoryPolicyWire(result);
        await unchanged();
    }

    [Theory]
    [InlineData("sessionProjectId")]
    [InlineData("openedProjectId")]
    [InlineData("selectedProjectId")]
    public async Task SessionFallbackHintsAreNotSupportedAdvisoryInputs(string field)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        var guard = new AdvisoryGuardedReadStore(f.Store);
        var owner = new AdvisoryTestConversationDirectory();
        var query = Query(f, new ResolveProjectFromConversationQuery("conversation")) with
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string>
            {
                ["conversationId"] = "conversation", [field] = "project-a",
            }),
        };
        var result = await Executor(f, guard, owner).FromConversationAsync(query, TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        owner.Calls.ShouldBe(0);
        guard.Reads.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LaggingSuggestionWithMissingSetupNeverBecomesAContextCertificate(bool conversation)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        await f.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var model = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        f.Store.SeedRaw("statestore", key, model with
        {
            Freshness = EvidenceFreshnessState.Stale,
            Detail = model.Detail! with { Setup = null, SetupMetadata = "unconsumed-private-marker" },
        });
        var unchanged = await CapturePolicyStateAsync(f);
        var executor = Executor(f);
        var query = Query(f, conversation ? new ResolveProjectFromConversationQuery("conversation")
            : new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")]));
        var result = conversation
            ? await executor.FromConversationAsync(query, TestContext.Current.CancellationToken)
            : await executor.FromAttachmentsAsync(query, TestContext.Current.CancellationToken);
        var response = Read(result);
        response.Candidates.Single().ProjectId.ShouldBe("project-a");
        response.Incomplete.ShouldBeFalse();
        response.EvaluatedAt.ShouldBe(f.Clock.GetUtcNow());
        AssertAdvisoryPolicyWire(result);
        result.GetPayload().GetRawText().ShouldNotContain("unconsumed-private-marker");
        await unchanged();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task FolderlessSuggestionStillRequiresFinalProjectAuthority(bool pending, bool revoke)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        await f.SeedAsync("project-a", folder: false);
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var model = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        f.Store.SeedRaw("statestore", key, model with { Detail = model.Detail! with
        {
            ProjectFolder = pending ? new ProjectFolderReference(null, null, ReferenceState.Pending, null, f.Clock.GetUtcNow()) : null,
        } });
        var unchanged = await CapturePolicyStateAsync(f);
        int checks = 0;
        f.Authorization.Decision = request => request.ProjectId == "project-a" && Interlocked.Increment(ref checks) == 2 && revoke
            ? EventStoreAuthorizationValidationResult.Denied() : EventStoreAuthorizationValidationResult.Allowed("policy-current");
        var result = await Executor(f).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken);
        var response = Read(result);
        response.Result.ShouldBe(revoke ? ResolutionResult.NoMatch : ResolutionResult.SingleCandidate);
        response.Candidates.Count.ShouldBe(revoke ? 0 : 1);
        checks.ShouldBe(2);
        f.Folders.Reads.ShouldBe(0);
        AssertAdvisoryPolicyWire(result);
        await unchanged();
    }

    private static void AssertAdvisoryPolicyWire(QueryResult result)
    {
        JsonElement payload = result.GetPayload();
        payload.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ShouldBe(
            new[] { "result", "candidates", "evaluatedAt", "incomplete", "unsupportedInputKinds" }.Order(StringComparer.Ordinal));
        foreach (JsonElement candidate in payload.GetProperty("candidates").EnumerateArray())
        {
            candidate.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ShouldBe(
                new[] { "projectId", "reasonCodes", "rank", "score" }.Order(StringComparer.Ordinal));
        }
    }

    private static async Task<Func<Task>> CapturePolicyStateAsync(SupportedProjectReadFixture f, params string[] projectIds)
    {
        if (projectIds.Length == 0) projectIds = ["project-a"];
        string[] keys = [ProjectReadModelKeys.Index("tenant-a"), .. projectIds.Select(id => ProjectReadModelKeys.Detail("tenant-a", id))];
        Dictionary<string, (byte[]? Bytes, string? ETag)> before = [];
        int count = f.Store.Count;
        foreach (string key in keys)
        {
            var entry = await f.Store.GetAsync<object>("statestore", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
            before[key] = (f.Store.SnapshotRaw("statestore", key), entry.ETag);
        }
        return async () =>
        {
            f.Store.Count.ShouldBe(count);
            foreach (string key in keys)
            {
                f.Store.SnapshotRaw("statestore", key).ShouldBe(before[key].Bytes);
                (await f.Store.GetAsync<object>("statestore", key, TestContext.Current.CancellationToken).ConfigureAwait(false)).ETag.ShouldBe(before[key].ETag);
            }
        };
    }
}
