namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Server.Queries;

internal sealed class AdvisoryTestConversationDirectory : IProjectAdvisoryConversationDirectory
{
    public int Calls { get; private set; }
    public Func<int, QueryResult>? OnRead { get; set; }
    public Task<QueryResult> ReadAsync(string tenantId, string conversationId, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(OnRead?.Invoke(Calls) ?? Result(new(new(conversationId, "project-a", "Project project-b", "Open"), false)));
    }
    public static QueryResult Result(ConversationAdvisoryMetadataResult value)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(value, JsonSerializerOptions.Web));
}
