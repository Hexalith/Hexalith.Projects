namespace Story65.InvocationFixture;

internal sealed class ReturnBoundaryHandler(CaseState state) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        HttpResponseMessage response = await base.SendAsync(request, token).ConfigureAwait(false);
        Verify.That(ReferenceEquals(response, state.Response) && state.ResponseDisposals == 0, "Handler changed response identity or disposed caller response.");
        state.HandlerReturned = true; state.Observe("handler.return", token: token); return response;
    }
}
