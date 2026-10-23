namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Shouldly;
using Xunit;

public sealed partial class ProjectAdvisoryQueryTests
{
    [Theory]
    [InlineData("lifecycle")]
    [InlineData("label")]
    [InlineData("blank-project")]
    [InlineData("untrimmed-project")]
    [InlineData("invalid-project")]
    [InlineData("incomplete")]
    public async Task MalformedConsumedOwnerMetadataIsIncomplete(string mutation)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        var metadata = new ConversationResolutionMetadataV1("conversation", "project-a", null, "Open");
        metadata = mutation switch
        {
            "lifecycle" => metadata with { LifecycleState = "Purged" },
            "label" => metadata with { SafeLabel = new string('x', ConversationResolutionScoringLabel.MaximumLength + 1) },
            "blank-project" => metadata with { ProjectId = " " },
            "untrimmed-project" => metadata with { ProjectId = " project-a " },
            "invalid-project" => metadata with { ProjectId = "/private/path" },
            _ => metadata,
        };
        var payload = JsonSerializer.SerializeToElement(new { metadata, incomplete = mutation == "incomplete" ? (object)"false" : false }, JsonSerializerOptions.Web);
        var owner = new AdvisoryTestConversationDirectory { OnRead = _ => QueryResult.FromPayload(payload) };
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        var result = Read(await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Result.ShouldBeNull(); result.Incomplete.ShouldBeTrue(); result.Candidates.ShouldBeEmpty();
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FolderlessProjectsUseExactProjectAuthorityWithoutOwnerCalls(bool pending)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a", folder: false);
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var model = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        f.Store.SeedRaw("statestore", key, model with { Detail = model.Detail! with
            { ProjectFolder = pending ? new ProjectFolderReference(null, null, ReferenceState.Pending, null, f.Clock.GetUtcNow()) : null } });
        var conversation = Read(await Executor(f).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        conversation.Result.ShouldBe(ResolutionResult.SingleCandidate); conversation.Incomplete.ShouldBeFalse();
        conversation.Candidates.Single().ProjectId.ShouldBe("project-a"); f.Folders.Reads.ShouldBe(0);
        var attachments = Read(await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "unmatched")])), TestContext.Current.CancellationToken));
        attachments.Incomplete.ShouldBeFalse(); attachments.Result.ShouldBe(ResolutionResult.NoMatch);
        f.Folders.Arguments.ShouldAllBe(argument => argument.ProjectId == "projects" && argument.FolderId == "unmatched");
    }

    [Fact]
    public async Task ConversationOnlyChecksOwnersForProjectsWithAnObservedSignal()
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        for (int n = 0; n < 20; n++) await f.SeedAsync($"unmatched-{n}");
        await f.SeedAsync("project-a");
        var response = Read(await Executor(f).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        response.Result.ShouldBe(ResolutionResult.SingleCandidate); response.Incomplete.ShouldBeFalse();
        f.Folders.Reads.ShouldBe(2);
        f.Folders.Arguments.ShouldAllBe(argument => argument.ProjectId == "project-a" && argument.FolderId == "folder-project-a");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IncompleteOwnerMetadataRetainsAnyAuthorizedSuggestion(bool linked)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        var owner = new AdvisoryTestConversationDirectory { OnRead = _ => AdvisoryTestConversationDirectory.Result(new(new("conversation", linked ? "project-a" : null, null, "Open"), true)) };
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        var result = Read(await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Result.ShouldBe(linked ? ResolutionResult.SingleCandidate : null);
        result.Candidates.Count.ShouldBe(linked ? 1 : 0);
    }

    [Theory]
    [InlineData(false, 200)]
    [InlineData(false, 201)]
    [InlineData(true, 199)]
    [InlineData(true, 200)]
    public async Task ValidNonmatchingRowsProveTheEnumerationTruncationBoundary(bool conversation, int count)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        for (int n = 0; n < count; n++) await f.SeedAsync($"p{n:000}");
        f.Authorization.Decision = request => request.ProjectId == "unreadable-hint"
            ? EventStoreAuthorizationValidationResult.Denied() : EventStoreAuthorizationValidationResult.Allowed("test");
        var owner = new AdvisoryTestConversationDirectory { OnRead = _ => AdvisoryTestConversationDirectory.Result(new(new("conversation", "unreadable-hint", null, "Open"), false)) };
        var guard = new AdvisoryGuardedReadStore(f.Store);
        var executor = new ProjectAdvisoryQueryExecutor(guard, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        var result = Read(conversation
            ? await executor.FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken)
            : await executor.FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "unmatched")])), TestContext.Current.CancellationToken));
        bool truncated = count + (conversation ? 1 : 0) > 200;
        result.Incomplete.ShouldBe(truncated); result.Result.ShouldBe(truncated ? null : ResolutionResult.NoMatch);
        guard.Reads.Where(key => !key.EndsWith(":index", StringComparison.Ordinal)).Order(StringComparer.Ordinal)
            .ShouldBe(Enumerable.Range(0, conversation ? 199 : 200).Select(n => ProjectReadModelKeys.Detail("tenant-a", $"p{n:000}")).Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("model-tenant")]
    [InlineData("model-project")]
    [InlineData("detail-tenant")]
    [InlineData("detail-project")]
    [InlineData("sequence")]
    [InlineData("freshness-default")]
    [InlineData("freshness-undefined")]
    public async Task MalformedReadIdentityAndSequenceNeverDiscloseForeignIds(string mutation)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        string key = ProjectReadModelKeys.Detail("tenant-a", "project-a");
        var model = f.Store.Snapshot<ProjectReadModel>("statestore", key)!;
        model = mutation switch
        {
            "model-tenant" => model with { TenantId = "foreign-tenant" },
            "model-project" => model with { ProjectId = "foreign-project" },
            "detail-tenant" => model with { Detail = model.Detail! with { TenantId = "foreign-tenant" } },
            "detail-project" => model with { Detail = model.Detail! with { ProjectId = "foreign-project" } },
            "sequence" => model with { Sequence = model.Sequence + 1 },
            "freshness-default" => model with { Freshness = default },
            _ => model with { Freshness = (EvidenceFreshnessState)999 },
        };
        f.Store.SeedRaw("statestore", key, model);
        var result = Read(await Executor(f).FromConversationAsync(Query(f, new ResolveProjectFromConversationQuery("conversation")), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Candidates.ShouldBeEmpty();
        JsonSerializer.Serialize(result).ShouldNotContain("foreign-");
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("etag")]
    [InlineData("projects")]
    [InlineData("unavailable")]
    public async Task MalformedIndexStructureCannotClaimCompleteNoMatch(string mutation)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        var index = new ProjectReadIndex("tenant-a", [], 1, f.Clock.GetUtcNow());
        index = mutation switch { "tenant" => index with { TenantId = "foreign" }, "projects" => index with { ProjectIds = null! },
            "unavailable" => index with { UnavailableProjectIds = null! }, _ => index };
        f.Store.SeedRaw("statestore", ProjectReadModelKeys.Index("tenant-a"), index);
        var guard = new AdvisoryGuardedReadStore(f.Store) { EmptyETag = mutation == "etag" };
        var result = Read(await Executor(f, guard).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery([new("folder", "unmatched")])), TestContext.Current.CancellationToken));
        result.Incomplete.ShouldBeTrue(); result.Result.ShouldBeNull(); result.Candidates.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("{\"conversationId\":\"\"}")]
    [InlineData("{\"conversationId\":\" conversation \"}")]
    [InlineData("{\"conversationId\":null}")]
    [InlineData("{\"conversationId\":\"conversation\",\"extra\":1}")]
    [InlineData("{\"conversationId\":\"conversation\",\"conversationId\":\"conversation\"}")]
    [InlineData("{\"ConversationId\":\"conversation\"}")]
    public async Task InvalidConversationPayloadsNeverReadOwnerMetadata(string payload)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); var owner = new AdvisoryTestConversationDirectory();
        var executor = new ProjectAdvisoryQueryExecutor(f.Store, f.Gate, f.Binding, f.Folders, owner, f.Clock);
        var query = Query(f, new ResolveProjectFromConversationQuery("conversation")) with { Payload = System.Text.Encoding.UTF8.GetBytes(payload) };
        (await executor.FromConversationAsync(query, TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        owner.Calls.ShouldBe(0); f.Folders.Reads.ShouldBe(0);
    }

    [Theory]
    [InlineData("{\"attachments\":null}")]
    [InlineData("{\"attachments\":[null]}")]
    [InlineData("{\"attachments\":[],\"includeArchived\":\"true\"}")]
    [InlineData("{\"Attachments\":[]}")]
    public async Task InvalidAttachmentPayloadsNeverReadMetadata(string payload)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); var guard = new AdvisoryGuardedReadStore(f.Store);
        var query = Query(f, new ResolveProjectFromAttachmentsQuery([])) with { Payload = System.Text.Encoding.UTF8.GetBytes(payload) };
        (await Executor(f, guard).FromAttachmentsAsync(query, TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        guard.Reads.ShouldBeEmpty(); f.Folders.Reads.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MismatchedQueryTypeAndEntityAreRejectedBeforeOwnerReads(bool wrongType)
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); var guard = new AdvisoryGuardedReadStore(f.Store);
        var query = Query(f, new ResolveProjectFromAttachmentsQuery([]));
        query = wrongType ? query with { QueryType = ResolveProjectFromConversationQuery.QueryType } : query with { EntityId = "other" };
        (await Executor(f, guard).FromAttachmentsAsync(query, TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(wrongType ? "safe-denial" : QueryAdapterFailureReason.InvalidEnvelope);
        guard.Reads.ShouldBeEmpty(); f.Folders.Reads.ShouldBe(0);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(33)]
    public async Task RawAttachmentLimitIncludes32(int count)
    {
        var f = await SupportedProjectReadFixture.CreateAsync();
        var result = await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery(
            Enumerable.Repeat(new ProjectAdvisoryAttachment("folder", "unmatched"), count).ToArray())), TestContext.Current.CancellationToken);
        if (count == 33) result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        else { result.Success.ShouldBeTrue(); f.Folders.Reads.ShouldBe(2); }
    }

    [Fact]
    public async Task MixedFolderAndFileInputsRetainSuggestionsWithExplicitIncompleteCoverage()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); await f.SeedAsync("project-a");
        var result = Read(await Executor(f).FromAttachmentsAsync(Query(f, new ResolveProjectFromAttachmentsQuery(
            [new("file", "unmapped-file"), new("folder", "folder-project-a")])), TestContext.Current.CancellationToken));
        result.Result.ShouldBe(ResolutionResult.SingleCandidate); result.Incomplete.ShouldBeTrue(); result.UnsupportedInputKinds.ShouldBe(["file"]);
        result.Candidates.Single().ProjectId.ShouldBe("project-a"); f.Folders.Arguments.ShouldAllBe(argument => argument.FolderId == "folder-project-a");
    }

    [Fact]
    public async Task UnauthorizedMalformedRequestDeniesBeforeValidationOrOwnerReads()
    {
        var f = await SupportedProjectReadFixture.CreateAsync(); var guard = new AdvisoryGuardedReadStore(f.Store);
        foreach (var claim in f.Identity.FindAll("scope").ToArray()) f.Identity.RemoveClaim(claim);
        var query = Query(f, new ResolveProjectFromAttachmentsQuery([])) with { Scopes = null, Payload = "invalid"u8.ToArray() };
        (await Executor(f, guard).FromAttachmentsAsync(query, TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe("safe-denial");
        guard.Reads.ShouldBeEmpty(); f.Folders.Reads.ShouldBe(0);
    }
}
