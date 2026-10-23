namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using System.Collections.Concurrent;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Server.Queries;

internal sealed class AdvisoryTestConversationDirectory : IProjectAdvisoryConversationDirectory
{
    private int _calls;
    public int Calls => Volatile.Read(ref _calls);
    public ConcurrentBag<(string TenantId, string ConversationId)> Arguments { get; } = [];
    public Func<int, QueryResult>? OnRead { get; set; }
    public Task<QueryResult> ReadAsync(string tenantId, string conversationId, CancellationToken cancellationToken)
    {
        Arguments.Add((tenantId, conversationId));
        int call = Interlocked.Increment(ref _calls);
        return Task.FromResult(OnRead?.Invoke(call) ?? Result(new(new(conversationId, "project-a", "Project project-b", "Open"), false)));
    }
    public static QueryResult Result(ConversationAdvisoryMetadataResult value)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(value, JsonSerializerOptions.Web));
}
