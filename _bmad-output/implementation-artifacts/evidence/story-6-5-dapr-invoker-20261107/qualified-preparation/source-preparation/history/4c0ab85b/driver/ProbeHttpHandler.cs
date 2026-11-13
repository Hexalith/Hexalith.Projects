namespace Story65.DaprInvokerProof;

using System.Net;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;

internal sealed class ProbeHttpHandler(FixtureState state, QueryEnvelope original) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        state.Observe("http.handler", request.Method + " " + request.RequestUri, token);
        state.RequestBytes = await request.Content!.ReadAsByteArrayAsync(token).ConfigureAwait(false);
        Check.Require(state.RequestBytes.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(original, SharedResources.Dapr.JsonSerializerOptions)),
            "Real Dapr lazy body bytes changed.");
        Check.Require(request.Options.TryGetValue(QueryCallerAuthorityBinding.RequestBindings, out var bindings), "Actual query authority binding was absent.");
        var expected = QueryCallerAuthorityBinding.Bind(original);
        Check.Require(bindings!.Count == expected.Count && expected.All(pair => bindings.TryGetValue(pair.Key, out string? value) && value == pair.Value),
            "Actual fresh authority binding changed.");
        state.BindingsVerified = true;
        state.BindingsJson = JsonSerializer.Serialize(bindings.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        state.Observe("request.bindings", state.BindingsJson);
        await state.ProbeAsync().ConfigureAwait(false);
        state.ThrowIf("transport");
        if (state.Test.Fault == "logger") { state.MarkFault("transport-for-logger"); throw new FixtureFailure(); }
        if (state.Test.Fault == "transport-catalog") { state.MarkFault("transport-catalog"); throw new RouteCatalogUnavailableException("synthetic fixture fault"); }
        if (state.Test.Fault == "transport-cancel") { state.MarkFault("transport-cancel"); state.Caller.Cancel(); token.ThrowIfCancellationRequested(); }
        HttpStatusCode status = state.Test.Fault switch
        { "status-403" => HttpStatusCode.Forbidden, "status-503" => HttpStatusCode.ServiceUnavailable, _ => HttpStatusCode.OK };
        if (state.Test.Fault is "status-403" or "status-503") state.MarkFault(state.Test.Fault);
        var result = new QueryResult(true, "{\"allowed\":true}"u8.ToArray(), null,
            state.Test.Fault == "projection" ? "synthetic-foreign-projection" : state.Projection,
            new QueryResponseMetadata(ETag: "synthetic-etag", IsStale: false, ProjectionVersion: "synthetic-v1", WarningCodes: ["synthetic-warning"]));
        byte[] bytes = state.Test.Fault switch
        { "json-malformed" => "{"u8.ToArray(), "json-null" => "null"u8.ToArray(), _ => JsonSerializer.SerializeToUtf8Bytes(result, JsonSerializerOptions.Web) };
        if (state.Test.Fault is "json-malformed" or "json-null" or "projection") state.MarkFault(state.Test.Fault);
        state.Responses++;
        return new HttpResponseMessage(status) { Content = new ProbeResponseContent(bytes, state) };
    }
}
