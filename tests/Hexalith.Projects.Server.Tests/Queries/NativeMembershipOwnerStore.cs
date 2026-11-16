namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server.Projections;

/// <summary>Controlled native owner projection source for actual authenticated transport tests.</summary>
internal sealed class NativeMembershipOwnerStore(ConversationProjectedReadModels models) : IConversationProjectionReadStore
{
    public ConversationProjectedReadModels Models { get; set; } = models;
    public ValueTask<ConversationProjectedReadModels?> ReadAsync(TenantId tenantId, ConversationId conversationId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<ConversationProjectedReadModels?>(conversationId.Value == "bound-conversation" ? Models : null);
    public ValueTask<ConversationProjectionIndexSnapshot> ListAsync(TenantId tenantId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Membership never enumerates Conversations.");
    public ValueTask<IReadOnlySet<string>> ValidatePageAsync(TenantId tenantId, ConversationProjectionIndexSnapshot snapshot,
        IReadOnlyList<ConversationSummaryProjectionV1> page, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Membership never pages Conversations.");
}
