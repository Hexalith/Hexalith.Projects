namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Projects.Server.Conversations;

/// <summary>Models a native reassignment between initial admission and final disclosure.</summary>
internal sealed class ChangingProjectConversationMembership : IProjectConversationMembershipDirectory
{
    public int Checks { get; private set; }
    public Task<bool> IsCurrentMemberAsync(string tenantId, string projectId, string conversationId,
        string originalActorId, string correlationId, CancellationToken cancellationToken)
        => Task.FromResult(++Checks == 1);
}
