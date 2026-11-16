namespace Hexalith.Projects.Client.Generated;

/// <summary>Preserves binary callable v1 overloads while adding explicit request metadata.</summary>
public partial class Client
{
    /// <inheritdoc/>
    public virtual Task<ProjectListResponse> ListProjectsAsync(Lifecycle? lifecycle, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness)
        => ListProjectsAsync(lifecycle, x_Correlation_Id, x_Hexalith_Freshness, inspectDescriptiveMetadata: false, CancellationToken.None);
    /// <inheritdoc/>
    public virtual Task<ProjectListResponse> ListProjectsAsync(Lifecycle? lifecycle, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken)
        => ListProjectsAsync(lifecycle, x_Correlation_Id, x_Hexalith_Freshness, inspectDescriptiveMetadata: false, cancellationToken);
    /// <inheritdoc/>
    public virtual Task<Project> GetProjectAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness)
        => GetProjectAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, inspectDescriptiveMetadata: false, CancellationToken.None);
    /// <inheritdoc/>
    public virtual Task<Project> GetProjectAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken)
        => GetProjectAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, inspectDescriptiveMetadata: false, cancellationToken);
    /// <inheritdoc/>
    public virtual Task<ProjectContext> GetProjectContextAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness)
        => GetProjectContextAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, conversationId: null!, CancellationToken.None);
    /// <inheritdoc/>
    public virtual Task<ProjectContext> GetProjectContextAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken)
        => GetProjectContextAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, conversationId: null!, cancellationToken);
    /// <inheritdoc/>
    public virtual Task<ProjectContextExplanation> GetProjectContextExplanationAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness)
        => GetProjectContextExplanationAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, conversationId: null!, CancellationToken.None);
    /// <inheritdoc/>
    public virtual Task<ProjectContextExplanation> GetProjectContextExplanationAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken)
        => GetProjectContextExplanationAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, conversationId: null!, cancellationToken);
    /// <inheritdoc/>
    public virtual Task<ConversationStartSetup> GetConversationStartSetupAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness)
        => GetConversationStartSetupAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, conversationId: null!, CancellationToken.None);
    /// <inheritdoc/>
    public virtual Task<ConversationStartSetup> GetConversationStartSetupAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken)
        => GetConversationStartSetupAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, conversationId: null!, cancellationToken);
    /// <inheritdoc/>
    public virtual Task<ProjectContext> RefreshProjectContextAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness)
        => RefreshProjectContextAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, conversationId: null!, CancellationToken.None);
    /// <inheritdoc/>
    public virtual Task<ProjectContext> RefreshProjectContextAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken)
        => RefreshProjectContextAsync(projectId, x_Correlation_Id, x_Hexalith_Freshness, conversationId: null!, cancellationToken);
}
