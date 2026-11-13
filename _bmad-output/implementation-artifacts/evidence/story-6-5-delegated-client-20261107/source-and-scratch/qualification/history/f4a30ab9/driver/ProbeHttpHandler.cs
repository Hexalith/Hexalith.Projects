namespace Story65.DelegatedClientProof;

using System.Net;
using System.Text.Json;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;

internal sealed class ProbeHttpHandler(FixtureState state, string publicKey) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        state.Observe("http.handler", request.Method + " " + request.RequestUri, token);
        if (state.Cohort is not null) await state.Cohort.ArriveAsync(token).ConfigureAwait(false);
        await state.FakeAwaitAsync().ConfigureAwait(false);
        if (state.Test.Fault == "transport-cooperative")
        { Interlocked.Increment(ref state.Triggered); await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false); }
        byte[] bytes = await request.Content!.ReadAsByteArrayAsync(token).ConfigureAwait(false);
        state.RequestBytes = bytes;
        var sent = JsonSerializer.Deserialize<BoundOwnerQueryRequest>(bytes, JsonSerializerOptions.Web)!;
        string proof = request.Headers.GetValues(BoundOwnerQueryProof.HeaderName).Single();
        state.Assertion = request.Headers.GetValues(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).Single();
        state.ProofVerified = BoundOwnerQueryProof.Verify(sent, proof, state.Test.Workload, publicKey, DateTimeOffset.UtcNow);
        Check.Require(state.ProofVerified, "Fresh complete-scope signature did not verify.");
        state.Trigger("handler-throw");
        var dto = new SubmitQueryResponse("synthetic-owner-correlation", JsonSerializer.SerializeToElement(new { allowed = true }));
        if (state.Test.Fault == "response-mismatch") { Interlocked.Increment(ref state.Triggered); dto = dto with { Success = false }; }
        byte[] body = state.Test.Fault == "json-malformed" ? "{"u8.ToArray() : JsonSerializer.SerializeToUtf8Bytes(dto, JsonSerializerOptions.Web);
        if (state.Test.Fault == "json-malformed") Interlocked.Increment(ref state.Triggered);
        return new HttpResponseMessage((HttpStatusCode)state.Test.Status) { Content = new ProbeResponseContent(body, state) };
    }
}
