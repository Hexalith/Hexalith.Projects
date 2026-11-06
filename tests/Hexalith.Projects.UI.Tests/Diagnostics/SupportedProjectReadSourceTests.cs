namespace Hexalith.Projects.UI.Tests.Diagnostics;

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Diagnostics;
using NSubstitute;
using Shouldly;
using Xunit;

/// <summary>Frozen supported-read matrix: independent authority, safe absence and bounded advisory coverage.</summary>
public sealed class SupportedProjectReadSourceTests
{
    [Fact]
    public async Task InventoryPreservesIndependentPageAndDegradedRowEvidenceWithoutDiagnosticFanout()
    {
        SupportedReadTestContext scope = new();
        AdmissionSnapshot pageSnapshot = Snapshot(AdmissionResponseState.Complete, 0);
        AdmissionSnapshot rowSnapshot = Snapshot(AdmissionResponseState.Unavailable, 7);
        ProjectListPage delivered = new([new("p1", null, ProjectLifecycle.Active, ReferenceState.Unavailable, rowSnapshot)], "opaque+cursor", true, pageSnapshot);
        scope.Client.SubmitQueryAsync<ProjectListPage>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(new EventStoreQueryResult<ProjectListPage>(null, delivered, false, null));
        ProjectInventorySource inventory = new(scope.Queries);
        ProjectWarningsDashboardLoadResult result = await new ProjectWarningsDashboardSource(inventory).LoadPageAsync(new(), TestContext.Current.CancellationToken);
        result.Inventory!.Snapshot.ShouldBeSameAs(pageSnapshot);
        result.Inventory.Page!.Items[0].Snapshot.ShouldBeSameAs(rowSnapshot);
        result.Inventory.Cursor.ShouldBe("opaque+cursor");
        result.Inventory.HasMore.ShouldBeTrue();
        result.QueueItems.ShouldBeEmpty();
        result.InventoryRows[0].Name.ShouldBeEmpty();
        scope.Client.ReceivedCalls().Count().ShouldBe(1);
        SubmitQueryRequest sent = scope.Client.ReceivedCalls().Single().GetArguments()[0].ShouldBeOfType<SubmitQueryRequest>();
        sent.Tenant.ShouldBe("tenant-a");
        sent.Domain.ShouldBe("projects");
        sent.AggregateId.ShouldBe("projects");
        sent.QueryType.ShouldBe(ListProjectsQuery.QueryType);
        sent.Payload!.Value.GetProperty("inspectDescriptiveMetadata").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task ContinuationResetsOnTenantOrFilterChangesAndRemainsOpaque()
    {
        SupportedReadTestContext scope = new();
        List<SubmitQueryRequest> requests = [];
        scope.Client.SubmitQueryAsync<ProjectListPage>(Arg.Do<SubmitQueryRequest>(requests.Add), null, Arg.Any<CancellationToken>())
            .Returns(new EventStoreQueryResult<ProjectListPage>(null, new([], "opaque+cursor", true, Snapshot(AdmissionResponseState.Complete, 0)), false, null));
        ProjectInventorySource inventory = new(scope.Queries);
        await inventory.LoadPageAsync(new(), TestContext.Current.CancellationToken);
        await inventory.LoadPageAsync(new(Cursor: "opaque+cursor"), TestContext.Current.CancellationToken);
        await inventory.LoadPageAsync(new("Archived", "opaque+cursor"), TestContext.Current.CancellationToken);
        scope.SetTenant("tenant-b");
        await inventory.LoadPageAsync(new("Archived", "opaque+cursor"), TestContext.Current.CancellationToken);
        requests[1].Payload!.Value.GetProperty("cursor").GetString().ShouldBe("opaque+cursor");
        requests[2].Payload!.Value.GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
        requests[3].Payload!.Value.GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
        requests[3].Tenant.ShouldBe("tenant-b");
    }

    [Fact]
    public async Task DetailPreservesThreeStatesVersionsAndConversationStartComponentType()
    {
        SupportedReadTestContext scope = new();
        GetProjectResponse open = new("p1", null, null, ProjectLifecycle.Archived, default, default, null, null, [], Snapshot(AdmissionResponseState.Complete, 1));
        ProjectContextReadResponse context = new("p1", ProjectLifecycle.Archived, null, null, [], [], [], [], Snapshot(AdmissionResponseState.Partial, 2));
        ConversationStartSetupResponse start = new(null, new(ConversationStartResponseState.Unavailable, DateTimeOffset.Parse("2026-11-03T00:00:00Z"), 3, [], [AdmissionRecoveryAction.Retry]));
        scope.Client.SubmitQueryAsync<GetProjectResponse>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>()).Returns(new EventStoreQueryResult<GetProjectResponse>(null, open, false, null));
        scope.Client.SubmitQueryAsync<ProjectContextReadResponse>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>()).Returns(new EventStoreQueryResult<ProjectContextReadResponse>(null, context, false, null));
        scope.Client.SubmitQueryAsync<ConversationStartSetupResponse>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>()).Returns(new EventStoreQueryResult<ConversationStartSetupResponse>(null, start, false, null));
        ProjectDetailLoadResult result = await new ProjectDetailSource(scope.Queries).GetProjectDetailAsync("p1", TestContext.Current.CancellationToken);
        result.Open.ShouldBeSameAs(open);
        result.Context.ShouldBeSameAs(context);
        result.Start.ShouldBeSameAs(start);
        scope.Client.ReceivedCalls().Count().ShouldBe(3);
        scope.Client.ReceivedCalls().All(call => ((SubmitQueryRequest)call.GetArguments()[0]!).AggregateId == "p1").ShouldBeTrue();
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(503)]
    public async Task DeniedAbsentForeignAndUnverifiableOpenReturnIdenticalSafeAbsence(int status)
    {
        SupportedReadTestContext scope = new();
        scope.Client.SubmitQueryAsync<GetProjectResponse>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>())
            .Returns<Task<EventStoreQueryResult<GetProjectResponse>>>(_ => throw new EventStoreGatewayException(status, "private-title", detail: "private-detail", correlationId: "private-id"));
        ProjectDetailLoadResult result = await new ProjectDetailSource(scope.Queries).GetProjectDetailAsync("protected-target", TestContext.Current.CancellationToken);
        result.Open.ShouldBeNull();
        result.Context.ShouldBeNull();
        result.Start.ShouldBeNull();
        result.Feedback!.SafeReasonCode.ShouldBe("safe_denial");
        JsonSerializer.Serialize(result).ShouldNotContain("private");
        scope.Client.ReceivedCalls().Count().ShouldBe(1);
    }

    [Fact]
    public async Task MissingServerAuthenticationFailsBeforeGatewayIncludingAuthDisabledStartup()
    {
        SupportedReadTestContext scope = new();
        ProjectReadQueryClient disabled = new(scope.Client, [], []);
        ProjectReadResult<ProjectListPage> result = await disabled.QueryAsync<ProjectListPage>("projects", ListProjectsQuery.QueryType, new ListProjectsQuery(), TestContext.Current.CancellationToken);
        result.Value.ShouldBeNull();
        result.Feedback!.SafeReasonCode.ShouldBe("safe_denial");
        scope.Client.ReceivedCalls().ShouldBeEmpty();
        // An accessor may retain a prior snapshot for diagnostics while explicitly failing authority.
        scope.Authority.TryGetContext(Arg.Any<string?>(), Arg.Any<string>()).Returns(
            new Hexalith.FrontComposer.Shell.Infrastructure.Tenancy.TenantContextResult(false,
                new("tenant-a", "user-1", true, "prior-observation"),
                Hexalith.FrontComposer.Shell.Infrastructure.Tenancy.TenantContextFailureCategory.StaleTenantContext, "prior-observation"));
        ProjectReadResult<ProjectListPage> stale = await scope.Queries.QueryAsync<ProjectListPage>("projects", ListProjectsQuery.QueryType, new ListProjectsQuery(), TestContext.Current.CancellationToken);
        stale.Feedback!.SafeReasonCode.ShouldBe("safe_denial");
        scope.Client.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task CallerCancellationPropagatesWithoutSafeFailureSubstitution()
    {
        SupportedReadTestContext scope = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => new ProjectInventorySource(scope.Queries).ListProjectsAsync(null, cancellation.Token));
        scope.Client.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task AdvisoryPreservesNullIncompleteFileCoverageAndComputationTime()
    {
        SupportedReadTestContext scope = new();
        DateTimeOffset evaluated = DateTimeOffset.Parse("2026-11-05T00:00:00Z");
        scope.Client.SubmitQueryAsync<ProjectAdvisoryResolutionResponse>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(new EventStoreQueryResult<ProjectAdvisoryResolutionResponse>(null, new(null, [], evaluated, true, ["file"]), false, null));
        ProjectResolutionTraceLoadResult result = await new ProjectResolutionTraceSource(scope.Queries).LoadTraceAsync(ProjectResolutionTraceRequest.ForAttachments([], ["f1"], false), TestContext.Current.CancellationToken);
        result.Trace!.Result.ShouldBeNull();
        result.Trace.Incomplete.ShouldBeTrue();
        result.Trace.UnsupportedInputKinds.ShouldBe("file");
        result.Trace.EvaluatedAt.ShouldBe(evaluated);
        result.Trace.ObservedAt.ShouldBe(default);
        ProjectResolutionTraceMapper.DeriveOutcomeLabel(result.Trace, []).ShouldNotContain("NoMatch");
        result.Candidates.ShouldBeEmpty();
        result.Exclusions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(32, true)]
    [InlineData(33, false)]
    public async Task AttachmentsAreBoundedBeforeDeduplication(int count, bool permitted)
    {
        SupportedReadTestContext scope = new();
        scope.Client.SubmitQueryAsync<ProjectAdvisoryResolutionResponse>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(new EventStoreQueryResult<ProjectAdvisoryResolutionResponse>(null, new(ResolutionResult.NoMatch, [], DateTimeOffset.UnixEpoch, false, []), false, null));
        ProjectResolutionTraceLoadResult result = await new ProjectResolutionTraceSource(scope.Queries).LoadTraceAsync(ProjectResolutionTraceRequest.ForAttachments(Enumerable.Repeat("folder-1", count), [], false), TestContext.Current.CancellationToken);
        if (!permitted)
        {
            result.Feedback!.SafeReasonCode.ShouldBe("attachment_limit_exceeded");
            scope.Client.ReceivedCalls().ShouldBeEmpty();
            return;
        }
        SubmitQueryRequest sent = scope.Client.ReceivedCalls().Single().GetArguments()[0].ShouldBeOfType<SubmitQueryRequest>();
        sent.Payload!.Value.GetProperty("attachments").GetArrayLength().ShouldBe(32);
        sent.Paging.ShouldBeNull();
        sent.AggregateId.ShouldBe("projects");
    }

    [Fact]
    public async Task NavigationRemountCannotOverwriteAnotherConsumersContinuation()
    {
        string program = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Hexalith.Projects.UI", "Program.cs"));
        program.ShouldContain("AddTransient<IProjectInventorySource, ProjectInventorySource>()");
        SupportedReadTestContext context = new();
        List<SubmitQueryRequest> requests = [];
        int page = 0;
        context.Client.SubmitQueryAsync<ProjectListPage>(Arg.Do<SubmitQueryRequest>(requests.Add), null, Arg.Any<CancellationToken>())
            .Returns(_ => new EventStoreQueryResult<ProjectListPage>(null,
                new([], "opaque-" + ++page, true, Snapshot(AdmissionResponseState.Complete, 0)), false, null));
        ServiceCollection services = new();
        services.AddSingleton(context.Queries);
        services.AddTransient<IProjectInventorySource, ProjectInventorySource>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectInventorySource home = provider.GetRequiredService<IProjectInventorySource>();
        ProjectInventoryLoadResult first = await home.LoadPageAsync(new(), TestContext.Current.CancellationToken);
        await provider.GetRequiredService<IProjectInventorySource>().ListProjectsAsync(null, TestContext.Current.CancellationToken);
        await provider.GetRequiredService<IProjectInventorySource>().ListProjectsAsync(null, TestContext.Current.CancellationToken);
        await home.LoadPageAsync(new(Cursor: first.Cursor), TestContext.Current.CancellationToken);
        requests[3].Payload!.Value.GetProperty("cursor").GetString().ShouldBe(first.Cursor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdvisoryPreservesNonemptyUnselectedCandidates(bool multiple)
    {
        SupportedReadTestContext scope = new();
        ProjectAdvisoryCandidate[] candidates = multiple
            ? [new("p1", [ProjectReasonCode.ProjectFolderMatched], 1, 100), new("p2", [ProjectReasonCode.ConversationLinked], 2, 90)]
            : [new("p1", [ProjectReasonCode.ProjectFolderMatched], 1, 100)];
        scope.Client.SubmitQueryAsync<ProjectAdvisoryResolutionResponse>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(new EventStoreQueryResult<ProjectAdvisoryResolutionResponse>(null,
                new(multiple ? ResolutionResult.MultipleCandidates : ResolutionResult.SingleCandidate, candidates, DateTimeOffset.UnixEpoch, false, []), false, null));
        ProjectResolutionTraceLoadResult result = await new ProjectResolutionTraceSource(scope.Queries)
            .LoadTraceAsync(ProjectResolutionTraceRequest.ForAttachments(["folder-p1"], [], false), TestContext.Current.CancellationToken);
        result.Candidates.Count.ShouldBe(candidates.Length);
        for (int index = 0; index < candidates.Length; index++)
        {
            result.Candidates[index].ProjectId.ShouldBe(candidates[index].ProjectId);
            result.Candidates[index].Rank.ShouldBe(candidates[index].Rank);
            result.Candidates[index].Score.ShouldBe(candidates[index].Score);
            result.Candidates[index].ReasonCodes.ShouldBe(string.Join(", ", candidates[index].ReasonCodes));
        }
        ProjectResolutionTraceMapper.DeriveOutcomeLabel(result.Trace!, []).ShouldContain("unselected");
        result.Exclusions.ShouldBeEmpty();
    }

    private static AdmissionSnapshot Snapshot(AdmissionResponseState state, long version)
        => new(state, DateTimeOffset.Parse("2026-11-01T00:00:00Z").AddDays(version), version, [], [AdmissionRecoveryAction.Retry]);
}
