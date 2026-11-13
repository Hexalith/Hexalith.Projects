namespace Story65.DelegatedClientProof;

using System.Text.Json;
using Hexalith.EventStore.ServiceDefaults.Authentication;

internal sealed class ProbeIssuer(FixtureState state) : IWorkloadAssertionIssuer
{
    public bool CanBindResources => true;
    public ValueTask<string?> IssueAsync(WorkloadAssertionRequest request, CancellationToken cancellationToken = default)
    {
        state.Observe("issuer.issue", JsonSerializer.Serialize(request), cancellationToken);
        if (state.Canary) Interlocked.CompareExchange(ref FlowCanaryState.FirstProbe,
            ExecutionContext.IsFlowSuppressed() ? 1 : 0, -1);
        state.IssuerEntered.TrySetResult(); state.Trigger("issuer-throw");
        if (state.Test.Fault == "issuer-null") { Interlocked.Increment(ref state.Triggered); return ValueTask.FromResult<string?>(null); }
        if (state.Test.ValueSource == "sync" && !state.Canary) return ValueTask.FromResult<string?>("synthetic-owner-assertion");
        var source = new AssertionValueSource(state, cancellationToken); state.Sources.Add(source); return source.Value;
    }
}
