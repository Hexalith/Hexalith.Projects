namespace Hexalith.Projects.Contracts.Routing;

/// <summary>Shares stable EventStore identities between the compiled catalog and its server.</summary>
internal static class ProjectsRouteIdentity
{
    /// <summary>The fully qualified <c>ArchiveProject</c> command type discriminator on the wire.</summary>
    internal const string ArchiveProjectCommandType = "Hexalith.Projects.Commands.ArchiveProject";

    /// <summary>The fully qualified <c>ConfirmProjectResolution</c> command type discriminator on the wire.</summary>
    internal const string ConfirmProjectResolutionCommandType = "Hexalith.Projects.Commands.ConfirmProjectResolution";

    /// <summary>The fully qualified <c>CreateProject</c> command type discriminator on the wire.</summary>
    internal const string CreateProjectCommandType = "Hexalith.Projects.Commands.CreateProject";

    /// <summary>The canonical EventStore domain segment for project aggregates.</summary>
    internal const string DomainName = "projects";

    /// <summary>The supported Explain Context Selection query type.</summary>
    internal const string ExplainContextSelectionQueryType = "Hexalith.Projects.Queries.ExplainContextSelection.v1";

    /// <summary>The supported Conversation-start setup query type.</summary>
    internal const string GetConversationStartSetupQueryType = "Hexalith.Projects.Queries.GetConversationStartSetup.v1";

    /// <summary>The supported Get Project Context query type.</summary>
    internal const string GetProjectContextQueryType = "Hexalith.Projects.Queries.GetProjectContext.v1";

    /// <summary>The exact protected project-grant issuance discriminator.</summary>
    internal const string GrantProjectChatBotAuthorityCommandType = "Hexalith.Projects.Contracts.Commands.GrantProjectChatBotAuthority";

    /// <summary>The fully qualified <c>LinkFileReference</c> command type discriminator on the wire.</summary>
    internal const string LinkFileReferenceCommandType = "Hexalith.Projects.Commands.LinkFileReference";

    /// <summary>The fully qualified <c>LinkMemory</c> command type discriminator on the wire.</summary>
    internal const string LinkMemoryCommandType = "Hexalith.Projects.Commands.LinkMemory";

    /// <summary>The stable EventStore projection type for the Project aggregate state.</summary>
    internal const string ProjectionType = "project";

    /// <summary>The fully qualified <c>RestoreProject</c> command type discriminator on the wire.</summary>
    internal const string RestoreProjectCommandType = "Hexalith.Projects.Commands.RestoreProject";

    /// <summary>The exact protected project-grant revocation discriminator.</summary>
    internal const string RevokeProjectChatBotAuthorityCommandType = "Hexalith.Projects.Contracts.Commands.RevokeProjectChatBotAuthority";

    /// <summary>The fully qualified <c>SetProjectFolder</c> command type discriminator on the wire.</summary>
    internal const string SetProjectFolderCommandType = "Hexalith.Projects.Commands.SetProjectFolder";

    /// <summary>The fully qualified <c>UnlinkFileReference</c> command type discriminator on the wire.</summary>
    internal const string UnlinkFileReferenceCommandType = "Hexalith.Projects.Commands.UnlinkFileReference";

    /// <summary>The fully qualified <c>UnlinkMemory</c> command type discriminator on the wire.</summary>
    internal const string UnlinkMemoryCommandType = "Hexalith.Projects.Commands.UnlinkMemory";

    /// <summary>The fully qualified <c>UpdateProjectSetup</c> command type discriminator on the wire.</summary>
    internal const string UpdateProjectSetupCommandType = "Hexalith.Projects.Commands.UpdateProjectSetup";
}
