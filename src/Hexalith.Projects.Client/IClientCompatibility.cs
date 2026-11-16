namespace Hexalith.Projects.Client.Generated;

/// <summary>Preserves v1 client signatures with safe unbound/uninspected defaults.</summary>
public partial interface IClient
{
    /// <summary>Retains the original safe-default overload.</summary>
    Task<ProjectListResponse> ListProjectsAsync(Lifecycle? lifecycle, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness);
    /// <summary>Retains the original safe-default overload.</summary>
    Task<ProjectListResponse> ListProjectsAsync(Lifecycle? lifecycle, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken);
    /// <summary>Retains the original safe-default overload.</summary>
    Task<Project> GetProjectAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness);
    /// <summary>Retains the original safe-default overload.</summary>
    Task<Project> GetProjectAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken);
    /// <summary>Retains the original safe-default overload.</summary>
    Task<ProjectContext> GetProjectContextAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness);
    /// <summary>Retains the original safe-default overload.</summary>
    Task<ProjectContext> GetProjectContextAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken);
    /// <summary>Retains the original safe-default overload.</summary>
    Task<ProjectContextExplanation> GetProjectContextExplanationAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness);
    /// <summary>Retains the original safe-default overload.</summary>
    Task<ProjectContextExplanation> GetProjectContextExplanationAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken);
    /// <summary>Retains the original safe-default overload.</summary>
    Task<ConversationStartSetup> GetConversationStartSetupAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness);
    /// <summary>Retains the original safe-default overload.</summary>
    Task<ConversationStartSetup> GetConversationStartSetupAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken);
    /// <summary>Retains the original safely unbound Refresh overload.</summary>
    Task<ProjectContext> RefreshProjectContextAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness);
    /// <summary>Retains the original safely unbound Refresh overload.</summary>
    Task<ProjectContext> RefreshProjectContextAsync(string? projectId, string? x_Correlation_Id, ReadConsistencyClass? x_Hexalith_Freshness, CancellationToken cancellationToken);
}
