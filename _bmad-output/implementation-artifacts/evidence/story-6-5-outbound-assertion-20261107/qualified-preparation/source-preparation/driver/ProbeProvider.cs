namespace Story65.OutboundAssertionProof;

using Hexalith.EventStore.ServiceDefaults.Authentication;

internal sealed class ProbeProvider(FixtureState state, Type loggerType, object logger) : IServiceProvider
{
    private readonly ProbeIssuer issuer = new(state);
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IWorkloadAssertionIssuer))
        {
            state.IssuerResolutions++; state.Observe("service.issuer", HeaderSnapshot.Of(state.Request));
            Check.Require(!state.Request.Headers.Contains("Authorization")
                && !state.Request.Headers.Contains(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName), "Original credentials were not removed before issuer resolution.");
            if (state.Canary) Interlocked.CompareExchange(ref FlowCanaryState.FirstProbe, ExecutionContext.IsFlowSuppressed() ? 1 : 0, -1);
            if (state.Test.Fault == "resolve-invalid") throw state.Inject("resolve-invalid", new InvalidOperationException("synthetic private resolution detail"));
            state.ThrowIf("resolve-other");
            if (state.Test.Fault is "missing" or "missing-command") { state.Mark(state.Test.Fault); return null; }
            if (state.Test.Fault is "missing-logger" or "logger-resolution" or "logger") return null;
            return issuer;
        }
        if (serviceType == loggerType)
        {
            state.LoggerResolutions++; state.Observe("service.logger", serviceType.GetGenericArguments().Single().FullName!);
            state.ThrowIf("logger-resolution");
            if (state.Test.Fault == "missing-logger") { state.Mark("missing-logger"); return null; }
            return logger;
        }
        throw new ProofAssertionFailure("ProbeProvider.cs", 0);
    }
}
