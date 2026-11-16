namespace Hexalith.Projects.Server.Conversations;

/// <summary>Checks current native Conversation membership through the authorized owner read.</summary>
public interface IProjectConversationMembershipDirectory
{
    /// <summary>Requires exact Tenant, original actor, Conversation and native Project membership.</summary>
    Task<bool> IsCurrentMemberAsync(string tenantId, string projectId, string conversationId,
        string originalActorId, string correlationId, CancellationToken cancellationToken);
}
