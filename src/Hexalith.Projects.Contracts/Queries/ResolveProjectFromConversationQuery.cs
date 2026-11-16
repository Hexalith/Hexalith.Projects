namespace Hexalith.Projects.Contracts.Queries;

using System.ComponentModel;
using System.Text.Json.Serialization;
using Hexalith.McpCli.Abstractions;

/// <summary>Suggests Projects using an authorized committed Conversation observation.</summary>
/// <param name="ConversationId">The exact owner resource to read.</param>
/// <param name="IncludeArchived">Whether authorized archived candidates may qualify.</param>
[HexalithQuery("Trace unselected advisory Project candidates from an authorized committed Conversation. Coverage and freshness are explicit; suggestions never authorize selection, context or confirmation.",
    Name = "trace-conversation", Domain = "projects", WireType = QueryType, AggregateId = "projects",
    Example = """{"conversationId":"conversation-a"}""")]
[Hexalith.EventStore.Contracts.Queries.QueryResultContract(typeof(ProjectAdvisoryResolutionResponse), "projects", "Hexalith.Projects.Queries.ResolveProjectFromConversation.v1")]
public sealed record ResolveProjectFromConversationQuery(
    [property: Description("Exact opaque Conversation owner identifier; authority comes from the credential."), JsonPropertyName("conversationId"), HexalithIdentifier]
    string ConversationId,
    [property: Description("Include authorized Archived candidates; defaults to false."), JsonPropertyName("includeArchived")]
    bool IncludeArchived = false)
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Projects.Queries.ResolveProjectFromConversation.v1";
}
