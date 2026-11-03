using System.Net;
using System.Text.Json;
using Hexalith.EventStore.ServiceDefaults.Routing;
internal sealed class StateTransport(RouteCatalogActivationState initial) : HttpMessageHandler
{
    private byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(initial, JsonSerializerOptions.Web);
    private long version = 1;
    internal int Reads { get; private set; }
    internal int Writes { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (request.Method == HttpMethod.Get)
        {
            Reads++;
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes.ToArray()) };
            response.Headers.TryAddWithoutValidation("ETag", version.ToString()); return response;
        }
        if (request.Method != HttpMethod.Post) throw new InvalidOperationException("Non-state request refused");
        using var json = JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
        var row = json.RootElement[0];
        if (row.GetProperty("etag").GetString() != version.ToString()) return new(HttpStatusCode.Conflict);
        bytes = JsonSerializer.SerializeToUtf8Bytes(row.GetProperty("value")); version++; Writes++;
        return new(HttpStatusCode.NoContent);
    }
}
