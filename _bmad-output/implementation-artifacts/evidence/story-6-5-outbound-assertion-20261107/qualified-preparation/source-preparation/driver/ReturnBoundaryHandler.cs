namespace Story65.OutboundAssertionProof;

internal sealed class ReturnBoundaryHandler(FixtureState state) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        HttpResponseMessage response = await base.SendAsync(request, token).ConfigureAwait(false);
        state.OriginalResponseAtBoundary = ReferenceEquals(response, state.Response);
        Check.Require(state.OriginalResponseAtBoundary && state.ResponseDisposed == 0 && state.RequestDisposed == 0,
            "Selected handler did not return the original caller-owned response.");
        state.ReturnedBeforeBuffer = true; state.Observe("selected-handler.return", token: token);
        return response;
    }
}
