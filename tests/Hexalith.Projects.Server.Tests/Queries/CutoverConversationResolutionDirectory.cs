namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server.Conversations;
using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using TenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;

/// <summary>Supplies the identical controlled advisory owner observation to the retained REST path.</summary>
internal sealed class CutoverConversationResolutionDirectory : IProjectConversationResolutionDirectory
{
    /// <inheritdoc/>
    public Task<ConversationResolutionMetadata> ReadConversationMetadataAsync(ConversationId conversationId, TenantId tenantId,
        CallerPrincipalId caller, string correlationId, CancellationToken cancellationToken = default)
        => Task.FromResult(new ConversationResolutionMetadata(conversationId.Value, "project-a", "Project project-b", ReferenceState.Included));
}
