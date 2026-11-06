namespace Hexalith.Projects.UI.Tests.Authentication;

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

/// <summary>Records synthetic authority on real SDK sends and returns an empty independent observation.</summary>
internal sealed class QueryRecordingHttpMessageHandler : HttpMessageHandler
{
    internal ConcurrentQueue<(string Tenant, string? Credential)> Sends { get; } = new();
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using JsonDocument document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        Sends.Enqueue((document.RootElement.GetProperty("tenant").GetString()!, request.Headers.Authorization?.Parameter));
        await Task.Yield();
        return new(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {"correlationId":"synthetic-observation","payload":{"items":[],"cursor":null,"hasMore":false,"snapshot":{"responseState":"Complete","asOf":"2026-11-06T00:00:00Z","projectVersion":0,"components":[],"recoveryActions":[]}}}
            """, Encoding.UTF8, "application/json")
        };
    }
}
