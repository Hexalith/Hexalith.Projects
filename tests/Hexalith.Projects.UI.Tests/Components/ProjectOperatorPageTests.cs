namespace Hexalith.Projects.UI.Tests.Components;

using Bunit;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.UI.Components.Pages;
using Hexalith.Projects.UI.Components.Shared;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Tests.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

[Trait("Story", "8.1")]
public sealed class ProjectOperatorPageTests : FrontComposerTestBase
{
    [Fact]
    public void AuditContinuationDisplaysOnlyTheNewAuthorizedPage()
    {
        Services.Configure<ProjectOperatorPreviewOptions>(options => options.Enabled = true);
        var source = Substitute.For<IProjectDetailSource>();
        ProjectAuditFact first = new("stable-a", "metadata.inspected", "Inspection", DateTimeOffset.UnixEpoch, ProjectCount: 1, FieldClass: "ProjectName");
        ProjectAuditFact second = new("stable-b", "project.created", "Active", DateTimeOffset.UnixEpoch, "opaque-receipt");
        source.ReadAuditAsync("project-a", Arg.Any<GetProjectAuditQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new ProjectReadResult<ProjectAuditPage>(
                call.Arg<GetProjectAuditQuery>().Cursor == "bound-token" ? new("Partial", [second], null) : new("Partial", [first], "bound-token"), null, "tenant-a")));
        Services.AddSingleton(source);
        var cut = Render<ProjectAuditHistory>(p => p.Add(c => c.ProjectId, "project-a").Add(c => c.Tenant, "tenant-a"));
        cut.Find("[data-testid='project-audit-load']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='project-audit-row']").TextContent.ShouldContain("stable-a"));
        cut.Find("[data-testid='project-audit-next']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='project-audit-row']").TextContent.ShouldContain("opaque-receipt"));
        cut.Markup.ShouldNotContain("stable-a");
        cut.FindAll("[data-testid='project-audit-next']").ShouldBeEmpty();
    }

    [Fact]
    public void DefaultTaskRouteWithholdsControlsAndSendsNoPoll()
    {
        var scope = new SupportedReadTestContext();
        Services.AddSingleton(scope.Queries);
        var cut = Render<ProjectTaskDiagnostics>(p => p.Add(c => c.TaskId, "task-a"));
        cut.Markup.ShouldContain("Task status is not yet available");
        cut.FindAll("[data-testid='project-task-poll']").ShouldBeEmpty();
        scope.Client.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void SwitchingTenantDuringAPollClearsThePriorTaskObservation()
    {
        Services.Configure<ProjectOperatorPreviewOptions>(options => options.Enabled = true);
        var scope = new SupportedReadTestContext();
        scope.Client.SubmitQueryAsync<ProjectTaskStatusResponse>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<SubmitQueryRequest>().Tenant.ShouldBe("tenant-a");
                scope.SetTenant("tenant-b");
                var old = new ProjectTaskStatusResponse("Available", "sensitive-task", "Running", "InProgress",
                    ["PollTask"], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, null);
                return new EventStoreQueryResult<ProjectTaskStatusResponse>(null, old, false, null);
            });
        Services.AddSingleton(scope.Queries);
        var cut = Render<ProjectTaskDiagnostics>(p => p.Add(c => c.TaskId, "sensitive-task"));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("safe_denial"));
        cut.FindAll("[data-testid='project-task-state']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("InProgress");
    }
}
