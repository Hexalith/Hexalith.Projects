namespace Hexalith.Projects.UI.Tests.Components;

using Bunit;
using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Components.Pages;
using Hexalith.Projects.UI.Components.Shared;
using Hexalith.Projects.UI.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using Shouldly;
using Xunit;

/// <summary>Supported presentation preserves each observation without invented admission or metadata.</summary>
public sealed class SupportedProjectReadPageTests : FrontComposerTestBase
{
    [Fact]
    public void InventoryRendersIndependentEnumerationAndRowEvidenceWithoutInventedDatesOrWarnings()
    {
        IProjectWarningsDashboardSource source = Substitute.For<IProjectWarningsDashboardSource>();
        ProjectListPage delivered = new([new("project-opaque", null, ProjectLifecycle.Active, ReferenceState.Unavailable, Snapshot(AdmissionResponseState.Unavailable, 12))], "opaque", true, Snapshot(AdmissionResponseState.Complete, 0));
        ProjectInventoryLoadResult inventory = ProjectInventoryLoadResult.FromRows([new() { ProjectId = "project-opaque", Lifecycle = ProjectLifecycle.Active }]) with
        {
            Page = delivered,
            Snapshot = delivered.Snapshot,
            Cursor = delivered.Cursor,
            HasMore = true
        };
        source.LoadPageAsync(Arg.Any<ListProjectsQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(ProjectWarningsDashboardLoadResult.FromRows(inventory.Rows, [], new()) with
        {
            Inventory = inventory
        }));
        Services.AddSingleton(source);
        IRenderedComponent<Home> cut = Render<Home>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("project-opaque"));
        cut.Find("[data-testid='project-inventory-row-snapshot']").TextContent.ShouldContain("Unavailable");
        cut.Find("[data-testid='project-inventory-page-snapshot']").TextContent.ShouldContain("Complete");
        cut.Markup.ShouldContain("Warning history is unavailable");
        cut.Markup.ShouldNotContain("0001-01-01");
        cut.Find("[data-testid='project-inventory-next']").Click();
        source.Received().LoadPageAsync(Arg.Is<ListProjectsQuery>(query => query.Cursor == "opaque"), Arg.Any<CancellationToken>());
        cut.Find("[data-testid='project-inventory-filter-archived']").Click();
        source.Received().LoadPageAsync(Arg.Is<ListProjectsQuery>(query => query.Cursor == null && query.LifecycleState == "Archived"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DetailLabelsThreeIndependentStatesAndVersionsAndOffersNoUnsupportedControls()
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        GetProjectResponse opened = new("p1", null, null, ProjectLifecycle.Archived, default, default, null, null, [], Snapshot(AdmissionResponseState.Complete, 11));
        ProjectDetailLoadResult delivered = new(null, null, null, [], "tenant-a", "Read only")
        {
            Open = opened,
            Context = new("p1", ProjectLifecycle.Archived, null, null, [], [], [], [], Snapshot(AdmissionResponseState.Partial, 22)),
            Start = new(null, new(ConversationStartResponseState.Complete, DateTimeOffset.UnixEpoch, 33, [], [])),
        };
        source.InspectAsync("p1", false, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult(delivered));
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters.Add(p => p.ProjectId, "p1"));
        cut.WaitForAssertion(() => cut.Find("[data-testid='project-open-snapshot']").TextContent.ShouldContain("11"));
        cut.Find("[data-testid='project-context-snapshot']").TextContent.ShouldContain("Partial");
        cut.Find("[data-testid='project-context-snapshot']").TextContent.ShouldContain("22");
        cut.Find("[data-testid='project-start-snapshot']").TextContent.ShouldContain("Complete");
        cut.Find("[data-testid='project-start-snapshot']").TextContent.ShouldContain("33");
        cut.Markup.ShouldContain("Not disclosed");
        cut.Markup.ShouldContain("Audit history is unavailable");
        cut.FindAll("[data-testid='maintenance-action-panel']").ShouldBeEmpty();
        cut.FindAll("[data-testid='audit-timeline']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Context activation");
    }

    [Fact]
    public void EmptyIncompleteAdvisoryHasNoExhaustiveMatchOrSelectionClaim()
    {
        IProjectResolutionTraceSource source = Substitute.For<IProjectResolutionTraceSource>();
        source.LoadTraceAsync(Arg.Any<ProjectResolutionTraceRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(
            ProjectResolutionTraceLoadResult.FromTrace(new()
            {
                Result = null,
                Incomplete = true,
                UnsupportedInputKinds = "file",
                EvaluatedAt = DateTimeOffset.UnixEpoch
            }, [], [])));
        Services.AddSingleton(source);
        IRenderedComponent<ProjectResolutionTraceWorkbench> cut = Render<ProjectResolutionTraceWorkbench>();
        cut.Find("[data-testid='project-resolution-trace-run']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='project-resolution-trace-outcome']").TextContent.ShouldContain("coverage incomplete"));
        cut.Markup.ShouldContain("File identity mapping is unsupported");
        cut.Markup.ShouldNotContain("Resolved");
        cut.Markup.ShouldNotContain("NoMatch");
        cut.FindAll("[data-testid='project-resolution-trace-candidate']").ShouldBeEmpty();
    }

    [Fact]
    public async Task InventoryIgnoresLateDescriptiveResponseAfterReturningToSafeMetadata()
    {
        IProjectWarningsDashboardSource source = Substitute.For<IProjectWarningsDashboardSource>();
        TaskCompletionSource<ProjectWarningsDashboardLoadResult> late = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken previous = default;
        source.LoadPageAsync(Arg.Is<ListProjectsQuery>(q => !q.InspectDescriptiveMetadata), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Inventory("safe-current", null)));
        source.LoadPageAsync(Arg.Is<ListProjectsQuery>(q => q.InspectDescriptiveMetadata), Arg.Do<CancellationToken>(token => previous = token))
            .Returns(late.Task);
        Services.AddSingleton(source);
        IRenderedComponent<Home> cut = Render<Home>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("safe-current"));
        Task pendingInspection = cut.InvokeAsync(() => cut.FindComponents<FluentCheckbox>()[0].Instance.ValueChanged.InvokeAsync(true));
        await cut.InvokeAsync(() => cut.FindComponents<FluentCheckbox>()[0].Instance.ValueChanged.InvokeAsync(false));
        previous.IsCancellationRequested.ShouldBeTrue();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("safe-current"));
        late.SetResult(Inventory("safe-current", "protected-late-name"));
        await pendingInspection.ConfigureAwait(true);
        cut.Markup.ShouldContain("safe-current");
        cut.Markup.ShouldNotContain("protected-late-name");
    }

    [Theory]
    [InlineData("p1")]
    [InlineData("p2")]
    public async Task DetailIgnoresLateInspectionAcrossModeAndRouteChanges(string currentProject)
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        TaskCompletionSource<ProjectDetailLoadResult> late = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken previous = default;
        source.InspectAsync("p1", true, false, Arg.Do<CancellationToken>(token => previous = token)).Returns(late.Task);
        source.InspectAsync(Arg.Any<string>(), false, false, Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Detail(call.ArgAt<string>(0), null)));
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/inspect/projects/p1?descriptive=true");
        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters.Add(p => p.ProjectId, "p1"));
        navigation.NavigateTo($"/inspect/projects/{currentProject}?descriptive=false");
        cut.Render(parameters => parameters.Add(p => p.ProjectId, currentProject));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain($"Project {currentProject}"));
        previous.IsCancellationRequested.ShouldBeTrue();
        int renders = cut.RenderCount;
        await cut.InvokeAsync(() => late.SetResult(Detail("p1", "protected-late-name")));
        cut.WaitForState(() => cut.RenderCount > renders);
        cut.Markup.ShouldContain($"Project {currentProject}");
        cut.Markup.ShouldNotContain("protected-late-name");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuarantineInventoryShowsEffectiveInspectionAndRestoresOrdinarySelection(bool ordinaryInspection)
    {
        IProjectWarningsDashboardSource source = Substitute.For<IProjectWarningsDashboardSource>();
        source.LoadPageAsync(Arg.Any<ListProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Inventory("project-opaque", null)));
        Services.AddSingleton(source);
        IRenderedComponent<Home> cut = Render<Home>();
        if (ordinaryInspection)
            await cut.InvokeAsync(() => cut.FindComponents<FluentCheckbox>()[0].Instance.ValueChanged.InvokeAsync(true));
        await cut.InvokeAsync(() => cut.FindComponents<FluentCheckbox>()[1].Instance.ValueChanged.InvokeAsync(true));
        FluentCheckbox descriptive = cut.FindComponents<FluentCheckbox>()[0].Instance;
        descriptive.Value.ShouldBeTrue();
        descriptive.Disabled.ShouldBe(true);
        cut.Find("[data-testid='project-inventory-inspection']").GetAttribute("aria-checked").ShouldBe("true");
        cut.Find("[data-testid='project-inventory-inspection']").GetAttribute("aria-disabled").ShouldBe("true");
        await source.Received().LoadPageAsync(Arg.Is<ListProjectsQuery>(query => query.IncludeQuarantined
            && query.InspectDescriptiveMetadata == ordinaryInspection), Arg.Any<CancellationToken>());
        await cut.InvokeAsync(() => cut.FindComponents<FluentCheckbox>()[1].Instance.ValueChanged.InvokeAsync(false));
        cut.FindComponents<FluentCheckbox>()[0].Instance.Value.ShouldBe(ordinaryInspection);
        cut.FindComponents<FluentCheckbox>()[0].Instance.Disabled.ShouldBe(false);
        cut.Find("[data-testid='project-inventory-inspection']").GetAttribute("aria-checked").ShouldBe(ordinaryInspection.ToString().ToLowerInvariant());
        cut.Find("[data-testid='project-inventory-inspection']").GetAttribute("aria-disabled").ShouldBe("false");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QuarantineDetailShowsEffectiveInspectionAndRestoresOrdinaryQuerySelection(bool ordinaryInspection)
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        source.InspectAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Detail("p1", null)));
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        string selection = ordinaryInspection.ToString().ToLowerInvariant();
        navigation.NavigateTo($"/inspect/projects/p1?descriptive={selection}&quarantined=true");
        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters.Add(p => p.ProjectId, "p1"));
        cut.WaitForAssertion(() => cut.Find("[data-testid='project-detail-inspection']").GetAttribute("aria-checked").ShouldBe("true"));
        cut.FindComponents<FluentCheckbox>()[0].Instance.Disabled.ShouldBe(true);
        cut.Find("[data-testid='project-detail-inspection']").GetAttribute("aria-disabled").ShouldBe("true");
        navigation.NavigateTo($"/inspect/projects/p1?descriptive={selection}&quarantined=false");
        cut.Render(parameters => parameters.Add(p => p.ProjectId, "p1"));
        cut.WaitForAssertion(() => cut.FindComponents<FluentCheckbox>()[0].Instance.Value.ShouldBe(ordinaryInspection));
        cut.FindComponents<FluentCheckbox>()[0].Instance.Disabled.ShouldBe(false);
        cut.Find("[data-testid='project-detail-inspection']").GetAttribute("aria-checked").ShouldBe(selection);
        cut.Find("[data-testid='project-detail-inspection']").GetAttribute("aria-disabled").ShouldBe("false");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdvisoryRendersNonemptyCandidatesWithoutSelection(bool multiple)
    {
        IProjectResolutionTraceSource source = Substitute.For<IProjectResolutionTraceSource>();
        source.LoadTraceAsync(Arg.Any<ProjectResolutionTraceRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Advisory(multiple)));
        Services.AddSingleton(source);
        IRenderedComponent<ProjectResolutionTraceWorkbench> cut = Render<ProjectResolutionTraceWorkbench>();
        cut.Find("[data-testid='project-resolution-trace-run']").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='project-resolution-trace-candidate']").Count.ShouldBe(multiple ? 2 : 1));
        cut.Find("[data-testid='project-resolution-trace-outcome']").TextContent.ShouldContain("unselected");
        cut.Markup.ShouldContain("Project ID: p1; Rank: 1; Score: 100; Safe reasons: ProjectFolderMatched");
        if (multiple) cut.Markup.ShouldContain("Project ID: p2; Rank: 2; Score: 90; Safe reasons: ConversationLinked");
        cut.Markup.ShouldNotContain("Resolved");
        cut.FindAll("[data-testid*='select-candidate']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("conversation")]
    [InlineData("folders")]
    [InlineData("files")]
    [InlineData("mode")]
    [InlineData("archived")]
    public async Task EditingAdvisoryInputsClearsCompletedAndLateResults(string input)
    {
        IProjectResolutionTraceSource source = Substitute.For<IProjectResolutionTraceSource>();
        TaskCompletionSource<ProjectResolutionTraceLoadResult> late = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken cancelled = default;
        source.LoadTraceAsync(Arg.Any<ProjectResolutionTraceRequest>(), Arg.Do<CancellationToken>(token => cancelled = token))
            .Returns(Task.FromResult(Advisory(false)), late.Task);
        Services.AddSingleton(source);
        IRenderedComponent<ProjectResolutionTraceWorkbench> cut = Render<ProjectResolutionTraceWorkbench>();
        if (input is "folders" or "files")
            await cut.InvokeAsync(() => cut.FindComponents<FluentCheckbox>()[0].Instance.ValueChanged.InvokeAsync(true));
        cut.Find("[data-testid='project-resolution-trace-run']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Project ID: p1"));
        await EditAdvisoryAsync(cut, input, "first-edit");
        cut.FindAll("[data-testid='project-resolution-trace-candidate']").ShouldBeEmpty();
        cut.FindAll("[data-testid='project-resolution-trace-coverage']").ShouldBeEmpty();
        Task pending = cut.InvokeAsync(() => cut.Find("[data-testid='project-resolution-trace-run']").Click());
        await EditAdvisoryAsync(cut, input, "second-edit");
        cancelled.IsCancellationRequested.ShouldBeTrue();
        late.SetResult(Advisory(true));
        await pending.ConfigureAwait(true);
        cut.FindAll("[data-testid='project-resolution-trace-candidate']").ShouldBeEmpty();
        cut.FindAll("[data-testid='project-resolution-trace-coverage']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Authorized reference label")]
    public void OpenReferenceRendersOnlyTheDeliveredOptionalLabel(string? label)
    {
        IProjectDetailSource source = Substitute.For<IProjectDetailSource>();
        ProjectDetailLoadResult result = Detail("p1", null) with
        {
            Open = Detail("p1", null).Open! with
            {
                References = [new("ProjectFolder", "folder-p1", null, null, label, ReferenceState.Included, "ProjectFolderMatched", DateTimeOffset.UnixEpoch)]
            }
        };
        source.InspectAsync("p1", false, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));
        Services.AddSingleton(source);
        Services.AddSingleton(Substitute.For<IProjectResolutionTraceSource>());
        IRenderedComponent<ProjectDiagnostics> cut = Render<ProjectDiagnostics>(parameters => parameters.Add(p => p.ProjectId, "p1"));
        cut.WaitForAssertion(() => cut.Find("[data-testid='project-open-reference']").TextContent.ShouldContain("folder-p1"));
        if (label is null) cut.FindAll("[data-testid='project-open-reference-label']").ShouldBeEmpty();
        else cut.Find("[data-testid='project-open-reference-label']").TextContent.ShouldContain(label);
    }

    private static Task EditAdvisoryAsync(IRenderedComponent<ProjectResolutionTraceWorkbench> cut, string input, string value)
        => cut.InvokeAsync(() => input switch
        {
            "conversation" => cut.FindComponent<FluentTextInput>().Instance.ValueChanged.InvokeAsync(value),
            "folders" => cut.FindComponents<FluentTextArea>()[0].Instance.ValueChanged.InvokeAsync(value),
            "files" => cut.FindComponents<FluentTextArea>()[1].Instance.ValueChanged.InvokeAsync(value),
            "mode" => cut.FindComponents<FluentCheckbox>()[0].Instance.ValueChanged.InvokeAsync(value == "first-edit"),
            _ => cut.FindComponents<FluentCheckbox>()[1].Instance.ValueChanged.InvokeAsync(value == "first-edit"),
        });

    private static ProjectResolutionTraceLoadResult Advisory(bool multiple)
        => ProjectResolutionTraceLoadResult.FromTrace(new()
        {
            Result = multiple ? ResolutionResult.MultipleCandidates : ResolutionResult.SingleCandidate,
            EvaluatedAt = DateTimeOffset.UnixEpoch,
        }, multiple
            ? [new() { ProjectId = "p1", Rank = 1, Score = 100, ReasonCodes = "ProjectFolderMatched" }, new() { ProjectId = "p2", Rank = 2, Score = 90, ReasonCodes = "ConversationLinked" }]
            : [new() { ProjectId = "p1", Rank = 1, Score = 100, ReasonCodes = "ProjectFolderMatched" }], []);

    private static ProjectWarningsDashboardLoadResult Inventory(string projectId, string? name)
    {
        ProjectInventoryLoadResult page = ProjectInventoryLoadResult.FromRows([new() { ProjectId = projectId, Name = name ?? string.Empty }]);
        return ProjectWarningsDashboardLoadResult.FromRows(page.Rows, [], new()) with
        {
            Inventory = page
        };
    }

    private static ProjectDetailLoadResult Detail(string id, string? name)
        => new(null, null, null, [], "tenant-a", "Read only")
        {
            Open = new(id, name, null, ProjectLifecycle.Active, default, default, null, null, [], Snapshot(AdmissionResponseState.Complete, 1)),
        };

    private static AdmissionSnapshot Snapshot(AdmissionResponseState state, long version)
        => new(state, DateTimeOffset.UnixEpoch.AddDays(version), version, [], [AdmissionRecoveryAction.Retry]);
}
