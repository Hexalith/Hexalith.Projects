namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Requests the current grant for an exact project and bound ChatBot requester.</summary>
public sealed record ResolveProjectChatBotAuthority(
    string TenantId, string PrincipalId, string ProjectId, string Operation,
    string ActorClass, string Origin)
{
    /// <summary>Gets the exact query type registered with EventStore.</summary>
    public static string QueryType => typeof(ResolveProjectChatBotAuthority).FullName!;
}
