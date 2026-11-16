namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Projects.Server.Conversations;

/// <summary>Controlled native membership authority; never advisory scoring evidence.</summary>
internal sealed class CurrentProjectConversationMembership(Func<bool>? decision = null, Func<Task<bool>>? asyncDecision = null) : IProjectConversationMembershipDirectory
{
    public async Task<bool> IsCurrentMemberAsync(string tenantId, string projectId, string conversationId,
        string originalActorId, string correlationId, CancellationToken cancellationToken)
        => (decision?.Invoke() ?? true) && tenantId == "tenant-a" && conversationId == "bound-conversation"
            && !string.IsNullOrWhiteSpace(projectId) && !string.IsNullOrWhiteSpace(originalActorId)
            && (asyncDecision is null || await asyncDecision().WaitAsync(cancellationToken).ConfigureAwait(false));
}
