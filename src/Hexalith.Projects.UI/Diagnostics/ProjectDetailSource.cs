namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.UI.Rendering;

/// <summary>Loads independent open, context and Conversation-start SDK observations.</summary>
public sealed class ProjectDetailSource(ProjectReadQueryClient queries) : IProjectDetailSource
{
    /// <inheritdoc/>
    public Task<ProjectDetailLoadResult> GetProjectDetailAsync(string projectId, CancellationToken cancellationToken)
        => InspectAsync(projectId, false, false, cancellationToken);

    /// <inheritdoc/>
    public async Task<ProjectDetailLoadResult> InspectAsync(string projectId, bool descriptive, bool quarantined, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        var initialAuthority = queries.CurrentContext;
        ProjectReadResult<GetProjectResponse> opened = await queries.QueryAsync<GetProjectResponse>(projectId,
            GetProjectQuery.QueryType, new GetProjectQuery(descriptive, quarantined), cancellationToken).ConfigureAwait(false);
        if (opened.Value is null || opened.Value.ProjectId != projectId || opened.Value.Snapshot.ResponseState == AdmissionResponseState.Denied)
            return ProjectDetailLoadResult.FromFeedback(ProjectConsoleFeedback.FailClosed("safe_denial"));
        ProjectReadResult<ProjectContextReadResponse> context = await queries.QueryAsync<ProjectContextReadResponse>(projectId,
            "Hexalith.Projects.Queries.GetProjectContext.v1", new GetProjectContextQuery(projectId), cancellationToken).ConfigureAwait(false);
        ProjectReadResult<ConversationStartSetupResponse> start = await queries.QueryAsync<ConversationStartSetupResponse>(projectId,
            "Hexalith.Projects.Queries.GetConversationStartSetup.v1", new GetConversationStartSetupQuery(projectId), cancellationToken).ConfigureAwait(false);
        var finalAuthority = queries.CurrentContext;
        if (initialAuthority is null || finalAuthority is null
            || initialAuthority.TenantId != finalAuthority.TenantId || initialAuthority.UserId != finalAuthority.UserId
            || context.TenantScope != opened.TenantScope || start.TenantScope != opened.TenantScope)
            return ProjectDetailLoadResult.FromFeedback(ProjectConsoleFeedback.FailClosed("safe_denial"));
        return new(null, null, null, [], opened.TenantScope, ProjectConsoleModes.ReadOnly)
        {
            Open = opened.Value,
            Context = context.Value,
            Start = start.Value,
            ContextFeedback = context.Feedback,
            StartFeedback = start.Feedback,
        };
    }
}
