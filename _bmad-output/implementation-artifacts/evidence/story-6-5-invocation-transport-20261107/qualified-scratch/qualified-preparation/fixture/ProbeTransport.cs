namespace Story65.InvocationFixture;

using System.Text.Json;

internal sealed class ProbeTransport(CaseState state, bool ordinary) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (ordinary) state.Ordinary++; else state.Transports++;
        state.Observe(ordinary ? "ordinary.begin" : "transport.begin", JsonSerializer.Serialize(new
        {
            Method = request.Method.Method, Uri = request.RequestUri!.AbsoluteUri,
            Headers = request.Headers.OrderBy(h => h.Key, StringComparer.Ordinal).Select(h => new { h.Key, Values = h.Value.ToArray() }),
        }), token);
        state.TransportEntered.TrySetResult();
        if (state.Name == "held-transport") await state.TransportContinue.Task.ConfigureAwait(false);
        await state.YieldAsync().ConfigureAwait(false);
        if (state.Name == "transport-fault-retains") throw state.Fault("transport", new IOException("synthetic-transport-fault"));
        if (state.Name == "transport-cancel-retains")
        { state.Caller.Cancel(); throw state.Fault("transport-cancel", new OperationCanceledException(token)); }
        state.Observe(ordinary ? "ordinary.return" : "transport.return", token: token);
        return state.Response;
    }
}
