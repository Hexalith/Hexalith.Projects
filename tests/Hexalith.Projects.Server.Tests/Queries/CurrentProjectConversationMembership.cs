namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Projects.Server.Conversations;

/// <summary>Controlled native membership authority; never advisory scoring evidence.</summary>
internal sealed class CurrentProjectConversationMembership : IProjectConversationMembershipDirectory
{
    public Task<bool> IsCurrentMemberAsync(string tenantId, string projectId, string conversationId,
        string originalActorId, string correlationId, CancellationToken cancellationToken)
        => Task.FromResult(tenantId == "tenant-a" && conversationId == "bound-conversation"
            && !string.IsNullOrWhiteSpace(projectId) && !string.IsNullOrWhiteSpace(originalActorId));
}
