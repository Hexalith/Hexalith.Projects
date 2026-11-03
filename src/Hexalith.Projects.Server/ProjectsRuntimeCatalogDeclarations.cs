using System.Text.Json;

using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Server.Projections.ProjectReads;

[assembly: RouteCatalogDeclarationSource(typeof(Hexalith.Projects.Server.ProjectsRuntimeCatalogDeclarations))]

namespace Hexalith.Projects.Server;

/// <summary>Declares the installed bespoke processor, all computed queries, named projections and retained legacy callback.</summary>
public static class ProjectsRuntimeCatalogDeclarations
{
    /// <summary>Gets the exact twelve processor payload bindings and complete typed intent policies.</summary>
    public static IReadOnlyList<CommandIntentDeclaration> Commands { get; } =
    [
        Command<CreateProjectPayload>("create-project", ProjectsServerModule.CreateProjectCommandType),
        Command<UpdateProjectSetupPayload>("update-project-setup", ProjectsServerModule.UpdateProjectSetupCommandType),
        Command<ArchiveProjectPayload>("archive-project", ProjectsServerModule.ArchiveProjectCommandType),
        Command<RestoreProjectPayload>("restore-project", ProjectsServerModule.RestoreProjectCommandType),
        Command<SetProjectFolderPayload>("set-project-folder", ProjectsServerModule.SetProjectFolderCommandType),
        Command<LinkFileReferencePayload>("link-file-reference", ProjectsServerModule.LinkFileReferenceCommandType),
        Command<UnlinkFileReferencePayload>("unlink-file-reference", ProjectsServerModule.UnlinkFileReferenceCommandType),
        Command<LinkMemoryPayload>("link-memory", ProjectsServerModule.LinkMemoryCommandType),
        Command<UnlinkMemoryPayload>("unlink-memory", ProjectsServerModule.UnlinkMemoryCommandType),
        Command<ConfirmProjectResolutionPayload>("confirm-project-resolution", ProjectsServerModule.ConfirmProjectResolutionCommandType),
        Command<GrantProjectChatBotAuthority>("grant-chatbot-authority", ProjectsServerModule.GrantProjectChatBotAuthorityCommandType),
        Command<RevokeProjectChatBotAuthority>("revoke-chatbot-authority", ProjectsServerModule.RevokeProjectChatBotAuthorityCommandType),
    ];

    /// <summary>Gets all installed query and projection declarations beside the actual processor inventory.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Routes { get; } =
    [
        .. Commands.Select(command => command.Route),
        Query<GetConversationStartSetupQuery>("conversation-start-setup", ProjectsServerModule.GetConversationStartSetupQueryType),
        Query<GetProjectContextQuery>("get-project-context", ProjectsServerModule.GetProjectContextQueryType),
        Query<ExplainContextSelectionQuery>("explain-context-selection", ProjectsServerModule.ExplainContextSelectionQueryType),
        Query<ResolveProjectChatBotAuthority>("chatbot-authority", ResolveProjectChatBotAuthority.QueryType),
        Query<ListProjectsQuery>("list-projects", ListProjectsQuery.QueryType),
        Query<GetProjectQuery>("get-project", GetProjectQuery.QueryType),
        Query<ResolveProjectFromConversationQuery>("resolve-from-conversation", ResolveProjectFromConversationQuery.QueryType),
        Query<ResolveProjectFromAttachmentsQuery>("resolve-from-attachments", ResolveProjectFromAttachmentsQuery.QueryType),
        Projection<ConversationStartSetupProjectionHandler>("conversation-start-setup", ["conversation-start-setup"]),
        Projection<ProjectReadProjectionHandler>("project-reads", ["project-reads"]),
        LegacyProjection(),
    ];

    /// <summary>Gets each actual canonical receiver, preserving the legacy and named dispatch distinction.</summary>
    public static IReadOnlyDictionary<string, string> ReceiverMethods => Routes.ToDictionary(route => route.RouteId,
        route => route.Kind switch
        {
            "command" => "process",
            "query" => "query",
            _ => route.ProjectionType == ProjectsServerModule.ProjectionType ? "project" : "project/v2",
        }, StringComparer.Ordinal);

    /// <summary>Gets the actual typed operation schemas; owner queries and projection protocols remain internal.</summary>
    public static IReadOnlyList<RouteOperationContract> Operations => Routes.Select(route =>
    {
        Type request = route.Kind == "projection"
            ? route.ProjectionType == ProjectsServerModule.ProjectionType ? typeof(ProjectionRequest) : typeof(ProjectionDispatchRequest)
            : typeof(ProjectsRuntimeCatalogDeclarations).Assembly.GetType(route.Execution!.HandlerType)
                ?? typeof(ListProjectsQuery).Assembly.GetType(route.Execution!.HandlerType, throwOnError: true)!;
        Type response = route.Kind switch
        {
            "command" => typeof(SubmitCommandResponse),
            "projection" => route.ProjectionType == ProjectsServerModule.ProjectionType ? typeof(ProjectionResponse) : typeof(ProjectionDispatchResponse),
            _ => QueryResultType(route.MessageType!),
        };
        bool ownerOnly = route.Kind == "projection" || route.MessageType == ResolveProjectChatBotAuthority.QueryType;
        return RouteOperationSchema.Create(route, request, response, ownerOnly ? "internal" : "public",
            route.Kind == "command" ? CommandEligibility(route.MessageType!) : "agent-eligible", JsonSerializerOptions.Web);
    }).ToArray();

    private static string CommandEligibility(string type) => type switch
    {
        ProjectsServerModule.ArchiveProjectCommandType or ProjectsServerModule.RestoreProjectCommandType
            or ProjectsServerModule.SetProjectFolderCommandType or ProjectsServerModule.UnlinkFileReferenceCommandType
            or ProjectsServerModule.UnlinkMemoryCommandType or ProjectsServerModule.ConfirmProjectResolutionCommandType
            or ProjectsServerModule.GrantProjectChatBotAuthorityCommandType or ProjectsServerModule.RevokeProjectChatBotAuthorityCommandType
            => "confirmation-required",
        ProjectsServerModule.CreateProjectCommandType or ProjectsServerModule.UpdateProjectSetupCommandType
            or ProjectsServerModule.LinkFileReferenceCommandType or ProjectsServerModule.LinkMemoryCommandType => "UI-only",
        _ => throw new InvalidOperationException("An installed command has no approved private-catalog surface classification."),
    };

    private static CommandIntentDeclaration Command<T>(string id, string discriminator) => new(
        new("projects-" + id, "command", ProjectsServerModule.DomainName, discriminator, null, "v1", 1,
            "workload", EventStoreOperationIdentifiers.DomainServiceProcess, null, [],
            new(typeof(T).FullName!, "exact", [discriminator], "domain-service", null, [])),
        "hexalith:projects:" + id + ":intent-v1", "projects:" + id, "projects:" + id + ":policy-v1",
        new("projects-" + id + "-descriptor-v1", 1,
            [new("target", "string"), new("semantic-payload", "string"), new("acting-subject", "string")]),
        IdempotencyReplayRetentionTier.Mutation, true);

    private static RouteContractDeclaration Query<T>(string id, string discriminator) => new("projects-" + id,
        "query", ProjectsServerModule.DomainName, discriminator, null, "v1", null, "workload",
        EventStoreOperationIdentifiers.DomainServiceQuery, null, [],
        new(typeof(T).FullName!, "exact", [discriminator], "domain-service", "computed", [], RequiresDelegatedContext: discriminator == ResolveProjectChatBotAuthority.QueryType));

    private static RouteContractDeclaration Projection<T>(string name, string[] outputs) => new("projects-projection-" + name,
        "projection", ProjectsServerModule.DomainName, null, name, "v1", null, "workload",
        EventStoreOperationIdentifiers.DomainServiceProject, null, [],
        new(typeof(T).FullName!, "exact", [name], "domain-service", null, outputs));

    private static RouteContractDeclaration LegacyProjection() => new("projects-projection-project", "projection",
        ProjectsServerModule.DomainName, null, ProjectsServerModule.ProjectionType, "v1", null, "workload",
        EventStoreOperationIdentifiers.DomainServiceProject, null, [],
        new(typeof(ProjectProjectionHandler).FullName!, "exact", [ProjectsServerModule.ProjectionType], "domain-service", null,
            [ProjectsServerModule.ProjectionType]));

    private static Type QueryResultType(string type) => type switch
    {
        ProjectsServerModule.GetConversationStartSetupQueryType => typeof(ConversationStartSetupResponse),
        ProjectsServerModule.GetProjectContextQueryType => typeof(ProjectContextReadResponse),
        ProjectsServerModule.ExplainContextSelectionQueryType => typeof(ExplainContextSelectionResponse),
        _ when type == ResolveProjectChatBotAuthority.QueryType => typeof(ProjectChatBotAuthorityResult),
        ListProjectsQuery.QueryType => typeof(ProjectListPage),
        GetProjectQuery.QueryType => typeof(GetProjectResponse),
        ResolveProjectFromConversationQuery.QueryType or ResolveProjectFromAttachmentsQuery.QueryType => typeof(ProjectAdvisoryResolutionResponse),
        _ => throw new InvalidOperationException("An installed Projects query has no result schema."),
    };
}
