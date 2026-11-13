namespace Story65.OutboundAssertionProof;

using Hexalith.EventStore.ServiceDefaults.Authentication;

internal sealed class ProbePrimaryHandler(FixtureState state) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        state.BaseCalls++; state.OriginalRequestAtBase = ReferenceEquals(request, state.Request);
        state.HeadersAtBase = HeaderSnapshot.Of(request); state.Observe("base.begin", state.HeadersAtBase, token);
        Check.Require(state.OriginalRequestAtBase && state.RequestDisposed == 0 && state.ResponseDisposed == 0, "Selected handler changed original ownership before base.");
        state.BaseEntered.TrySetResult();
        if (state.Test.Fault == "held-base") await state.ReleaseBase.Task.ConfigureAwait(false);
        await state.ProbeAsync().ConfigureAwait(false); state.ThrowIf("base");
        if (state.Test.Fault == "base-cancel") { state.Caller.Cancel(); throw state.Inject("base-cancel", new OperationCanceledException(token)); }
        state.Observe("base.return", token: token); return state.Response;
    }
}
