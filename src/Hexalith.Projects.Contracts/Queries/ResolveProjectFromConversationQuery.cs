namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Suggests Projects using an authorized committed Conversation observation.</summary>
/// <param name="ConversationId">The exact owner resource to read.</param>
/// <param name="IncludeArchived">Whether authorized archived candidates may qualify.</param>
public sealed record ResolveProjectFromConversationQuery(string ConversationId, bool IncludeArchived = false)
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.ResolveProjectFromConversation.v1";
}
