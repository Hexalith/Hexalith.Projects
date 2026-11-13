namespace Story65.OutboundAssertionProof;

using System.Text.Json;
using Hexalith.EventStore.ServiceDefaults.Authentication;

internal sealed class ProbeIssuer(FixtureState state) : IWorkloadAssertionIssuer
{
    public bool CanBindResources
    {
        get
        {
            state.CanBindReads++; state.Observe("issuer.can-bind");
            if (state.Test.Fault == "cannot-bind") { state.Mark("cannot-bind"); return false; }
            return state.Test.Route != "query"; // Query must never read this property, even when it would be false.
        }
    }
    public ValueTask<string?> IssueAsync(WorkloadAssertionRequest request, CancellationToken cancellationToken = default)
    {
        state.Issues++; state.IssuedRequest = request;
        state.Observe("issuer.issue", JsonSerializer.Serialize(request), cancellationToken);
        Check.Require(request.Audience == state.Audience && request.Operation == state.Operation
            && ReferenceEquals(request.Bindings, state.ExpectedBindings), "Original audience/operation/binding reference changed.");
        if (state.Canary) Interlocked.CompareExchange(ref FlowCanaryState.FirstIssuerProbe, ExecutionContext.IsFlowSuppressed() ? 1 : 0, -1);
        state.IssuerEntered.TrySetResult();
        if (state.Test.Fault == "issuer-invalid") throw state.Inject("issuer-invalid", new InvalidOperationException("synthetic fixture fault"));
        if (state.Test.Fault == "issuer-null") { state.Mark("issuer-null"); return ValueTask.FromResult<string?>(null); }
        if (state.Test.Fault == "direct-sync" && !state.Canary) return ValueTask.FromResult<string?>(FixtureState.FreshAssertion);
        var source = new AssertionValueSource(state, cancellationToken); state.Sources.Add(source); return source.Value;
    }
}
