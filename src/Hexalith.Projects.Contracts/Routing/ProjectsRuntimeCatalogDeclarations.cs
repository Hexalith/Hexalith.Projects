using System.Text.Json;

using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server;

[assembly: RouteCatalogDeclarationSource(typeof(Hexalith.Projects.Contracts.Routing.ProjectsRuntimeCatalogDeclarations))]

namespace Hexalith.Projects.Contracts.Routing;

/// <summary>Declares the installed bespoke processor, all computed queries, named projections and retained legacy callback.</summary>
public static class ProjectsRuntimeCatalogDeclarations
{
    /// <summary>Gets the exact twelve processor payload bindings and complete typed intent policies.</summary>
    public static IReadOnlyList<CommandIntentDeclaration> Commands { get; } =
    [
        Command<CreateProjectPayload>("create-project", ProjectsRouteIdentity.CreateProjectCommandType),
        Command<UpdateProjectSetupPayload>("update-project-setup", ProjectsRouteIdentity.UpdateProjectSetupCommandType),
        Command<ArchiveProjectPayload>("archive-project", ProjectsRouteIdentity.ArchiveProjectCommandType),
        Command<RestoreProjectPayload>("restore-project", ProjectsRouteIdentity.RestoreProjectCommandType),
        Command<SetProjectFolderPayload>("set-project-folder", ProjectsRouteIdentity.SetProjectFolderCommandType),
        Command<LinkFileReferencePayload>("link-file-reference", ProjectsRouteIdentity.LinkFileReferenceCommandType),
        Command<UnlinkFileReferencePayload>("unlink-file-reference", ProjectsRouteIdentity.UnlinkFileReferenceCommandType),
        Command<LinkMemoryPayload>("link-memory", ProjectsRouteIdentity.LinkMemoryCommandType),
        Command<UnlinkMemoryPayload>("unlink-memory", ProjectsRouteIdentity.UnlinkMemoryCommandType),
        Command<ConfirmProjectResolutionPayload>("confirm-project-resolution", ProjectsRouteIdentity.ConfirmProjectResolutionCommandType),
        Command<GrantProjectChatBotAuthority>("grant-chatbot-authority", ProjectsRouteIdentity.GrantProjectChatBotAuthorityCommandType),
        Command<RevokeProjectChatBotAuthority>("revoke-chatbot-authority", ProjectsRouteIdentity.RevokeProjectChatBotAuthorityCommandType),
    ];

    /// <summary>Gets all installed query and projection declarations beside the actual processor inventory.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Routes { get; } =
    [
        .. Commands.Select(command => command.Route),
        Query<GetConversationStartSetupQuery>("conversation-start-setup", ProjectsRouteIdentity.GetConversationStartSetupQueryType),
        Query<GetProjectContextQuery>("get-project-context", ProjectsRouteIdentity.GetProjectContextQueryType),
        Query<ExplainContextSelectionQuery>("explain-context-selection", ProjectsRouteIdentity.ExplainContextSelectionQueryType),
        Query<ResolveProjectChatBotAuthority>("chatbot-authority", ResolveProjectChatBotAuthority.QueryType),
        Query<ListProjectsQuery>("list-projects", ListProjectsQuery.QueryType),
        Query<ListProjectsV2Query>("list-projects-v2", ListProjectsV2Query.QueryType),
        Query<GetProjectAuditQuery>("get-project-audit", GetProjectAuditQuery.QueryType),
        Query<GetProjectTaskStatusQuery>("get-project-task-status", GetProjectTaskStatusQuery.QueryType),
        Query<GetProjectQuery>("get-project", GetProjectQuery.QueryType),
        Query<ResolveProjectFromConversationQuery>("resolve-from-conversation", ResolveProjectFromConversationQuery.QueryType),
        Query<ResolveProjectFromAttachmentsQuery>("resolve-from-attachments", ResolveProjectFromAttachmentsQuery.QueryType),
        Projection("conversation-start-setup", "Hexalith.Projects.Server.Projections.ConversationStartSetup.ConversationStartSetupProjectionHandler", ["conversation-start-setup"]),
        Projection("project-reads", "Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadProjectionHandler", ["project-reads"]),
        LegacyProjection(),
    ];

    /// <summary>Gets each actual canonical receiver, preserving the legacy and named dispatch distinction.</summary>
    public static IReadOnlyDictionary<string, string> ReceiverMethods => Routes.ToDictionary(route => route.RouteId,
        route => route.Kind switch
        {
            "command" => "process",
            "query" => "query",
            _ => route.ProjectionType == ProjectsRouteIdentity.ProjectionType ? "project" : "project/v2",
        }, StringComparer.Ordinal);

    /// <summary>Gets the actual typed operation schemas; owner queries and projection protocols remain internal.</summary>
    public static IReadOnlyList<RouteOperationContract> Operations => Routes.Select(route =>
    {
        Type request = route.Kind == "projection"
            ? route.ProjectionType == ProjectsRouteIdentity.ProjectionType ? typeof(ProjectionRequest) : typeof(ProjectionDispatchRequest)
            : typeof(ProjectsRuntimeCatalogDeclarations).Assembly.GetType(route.Execution!.HandlerType, throwOnError: true)!;
        Type response = route.Kind switch
        {
            "command" => typeof(SubmitCommandResponse),
            "projection" => route.ProjectionType == ProjectsRouteIdentity.ProjectionType ? typeof(ProjectionResponse) : typeof(ProjectionDispatchResponse),
            _ => QueryResultType(route.MessageType!),
        };
        bool ownerOnly = route.Kind == "projection" || route.MessageType == ResolveProjectChatBotAuthority.QueryType;
        return RouteOperationSchema.Create(route, request, response, ownerOnly ? "internal" : "public",
            route.Kind == "command" ? CommandEligibility(route.MessageType!) : "agent-eligible", JsonSerializerOptions.Web);
    }).ToArray();

    private static string CommandEligibility(string type) => type switch
    {
        ProjectsRouteIdentity.ArchiveProjectCommandType or ProjectsRouteIdentity.RestoreProjectCommandType
            or ProjectsRouteIdentity.SetProjectFolderCommandType or ProjectsRouteIdentity.UnlinkFileReferenceCommandType
            or ProjectsRouteIdentity.UnlinkMemoryCommandType or ProjectsRouteIdentity.ConfirmProjectResolutionCommandType
            or ProjectsRouteIdentity.GrantProjectChatBotAuthorityCommandType or ProjectsRouteIdentity.RevokeProjectChatBotAuthorityCommandType
            => "confirmation-required",
        ProjectsRouteIdentity.CreateProjectCommandType or ProjectsRouteIdentity.UpdateProjectSetupCommandType
            or ProjectsRouteIdentity.LinkFileReferenceCommandType or ProjectsRouteIdentity.LinkMemoryCommandType => "UI-only",
        _ => throw new InvalidOperationException("An installed command has no approved private-catalog surface classification."),
    };

    private static CommandIntentDeclaration Command<T>(string id, string discriminator) => new(
        new("projects-" + id, "command", ProjectsRouteIdentity.DomainName, discriminator, null, "v1", 1,
            "workload", EventStoreOperationIdentifiers.DomainServiceProcess, null, [],
            new(typeof(T).FullName!, "exact", [discriminator], "domain-service", null, [])),
        "hexalith:projects:" + id + ":intent-v1", "projects:" + id, "projects:" + id + ":policy-v1",
        new("projects-" + id + "-descriptor-v1", 1,
            [new("target", "string"), new("semantic-payload", "string"), new("acting-subject", "string")]),
        IdempotencyReplayRetentionTier.Mutation, true);

    private static RouteContractDeclaration Query<T>(string id, string discriminator) => new("projects-" + id,
        "query", ProjectsRouteIdentity.DomainName, discriminator, null, "v1", null, "workload",
        EventStoreOperationIdentifiers.DomainServiceQuery, null, [],
        new(typeof(T).FullName!, "exact", [discriminator], "domain-service", "computed", [], RequiresDelegatedContext: discriminator == ResolveProjectChatBotAuthority.QueryType));

    // The catalog records the receiver names without loading the Projects server.
    private static RouteContractDeclaration Projection(string name, string handlerType, string[] outputs) => new("projects-projection-" + name,
        "projection", ProjectsRouteIdentity.DomainName, null, name, "v1", null, "workload",
        EventStoreOperationIdentifiers.DomainServiceProject, null, [],
        new(handlerType, "exact", [name], "domain-service", null, outputs));

    private static RouteContractDeclaration LegacyProjection() => new("projects-projection-project", "projection",
        ProjectsRouteIdentity.DomainName, null, ProjectsRouteIdentity.ProjectionType, "v1", null, "workload",
        EventStoreOperationIdentifiers.DomainServiceProject, null, [],
        new("Hexalith.Projects.Server.ProjectProjectionHandler", "exact", [ProjectsRouteIdentity.ProjectionType], "domain-service", null,
            [ProjectsRouteIdentity.ProjectionType]));

    private static Type QueryResultType(string type)
    {
        if (type == ResolveProjectChatBotAuthority.QueryType) return typeof(ProjectChatBotAuthorityResult);
        return typeof(ListProjectsQuery).Assembly.GetTypes()
            .Select(query => query.GetCustomAttributes(typeof(Hexalith.EventStore.Contracts.Queries.QueryResultContractAttribute), false)
                .Cast<Hexalith.EventStore.Contracts.Queries.QueryResultContractAttribute>().SingleOrDefault())
            .Single(contract => contract?.QueryType == type)!.ResultType;
    }
}
