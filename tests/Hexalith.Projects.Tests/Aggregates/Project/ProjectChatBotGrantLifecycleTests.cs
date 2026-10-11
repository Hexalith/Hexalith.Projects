using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

namespace Hexalith.Projects.Tests.Aggregates.Project;

/// <summary>Checks replay of exact project authority issuance and revocation.</summary>
public sealed class ProjectChatBotGrantLifecycleTests
{
    private const string Tenant = "tenant-alpha";
    private const string Project = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public void RevokedGrantCannotAuthorizeAndOldIssuanceCannotBeReused()
    {
        var identity = new ProjectIdentity(Tenant, new ProjectId(Project));
        ProjectState state = ProjectState.Empty.Apply([
            new ProjectCreated(Tenant, Project, "Project", null, null, ProjectLifecycle.Active,
                "owner", "corr-0", "task-0", "message-0", "fingerprint-0", DateTimeOffset.UnixEpoch),
            new ProjectChatBotAuthorityGranted(Tenant, Project, "actor-alpha", "RecordGovernedNote",
                "human", "Ui", "grant-1", DateTimeOffset.UnixEpoch.AddDays(1),
                "owner", "corr-1", "task-1", "message-1", "fingerprint-1", DateTimeOffset.UnixEpoch),
            new ProjectChatBotAuthorityRevoked(Tenant, Project, "actor-alpha", "RecordGovernedNote",
                "human", "Ui", "grant-1", "owner", "corr-2", "task-2", "message-2",
                "fingerprint-2", DateTimeOffset.UnixEpoch.AddHours(1))], identity);

        string scope = ChatBotGrantScope.For("actor-alpha", "RecordGovernedNote", "human", "Ui");
        state.ChatBotGrants[scope].Revoked.ShouldBeTrue();
        state.ChatBotGrantIssuanceIds.ShouldContain("grant-1");

        Should.Throw<InvalidOperationException>(() => ProjectStateApply.Apply(state,
            new ProjectChatBotAuthorityGranted(Tenant, Project, "actor-alpha", "RecordGovernedNote",
                "human", "Ui", "grant-1", DateTimeOffset.UnixEpoch.AddDays(2),
                "owner", "corr-3", "task-3", "message-3", "fingerprint-3",
                DateTimeOffset.UnixEpoch.AddHours(2)), identity));
    }
}
