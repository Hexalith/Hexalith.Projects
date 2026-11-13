namespace Story65.InvocationFixture;

using System.Net;
using Hexalith.EventStore.ServiceDefaults.Routing;

internal static class FixtureAssertions
{
    internal static void Check(CaseState state, Exception? error, HttpResponseMessage? response, int fences,
        int requestBefore, int responseBefore, int insideFences, int readsBefore, int readsAfter)
    {
        string name = state.Name;
        bool refusal = name is "unsupported-basic-refusal" or "undeclared-target-refusal"
            or "borrowed-observer-refusal" or "held-scope-closed-refusal";
        bool canceled = name is "pre-cancel-before-acquire" or "cancel-before-enrollment" or "transport-cancel-retains";
        bool releaseFault = name is "release-fault-overrides-response" or "release-fault-overrides-validation";
        bool transportFault = name == "transport-fault-retains";
        bool bufferFault = name == "outer-buffer-fault";
        if (refusal)
        {
            Verify.That(error is RouteCatalogUnavailableException, "Expected the original SDK refusal.");
            string message = name switch
            {
                "unsupported-basic-refusal" => "The Basic Dapr invocation form is unsupported.",
                "undeclared-target-refusal" => "The Dapr invocation target is outside the committed catalog and auxiliary inventory.",
                "held-scope-closed-refusal" => "Nested metadata requires a live trusted SDK receiver scope.",
                _ => "synthetic-observer-refusal",
            };
            Verify.That(error!.Message == message && ((RouteCatalogUnavailableException)error).Category == RouteCatalogRefusalReason.Disagreement,
                "Refusal did not reach the intended original validation checkpoint.");
        }
        else if (canceled)
        {
            Verify.That(error is OperationCanceledException, "Expected original cancellation.");
            Verify.That(((OperationCanceledException)error!).CancellationToken == state.Caller.Token, "Cancellation token changed.");
        }
        else if (releaseFault || transportFault)
            Verify.That(error is IOException && ReferenceEquals(error, state.Injected), "Injected original error identity changed.");
        else if (bufferFault)
            Verify.That(error is HttpRequestException && ReferenceEquals(error.InnerException, state.Injected), "Default HTTP buffering error changed.");
        else Verify.That(error is null, "Healthy or uncooperative original call failed.");
        if (error is not null) Verify.That(response is null, "An exceptional call exposed a successful result.");
        else Verify.That(ReferenceEquals(response, state.Response) && response!.StatusCode == HttpStatusCode.OK, "Response reference or status changed.");

        int expectedTransport = name is "owned-success" or "borrowed-success" or "held-success" or "transport-fault-retains"
            or "transport-cancel-retains" or "release-fault-overrides-response" or "control-nochildren" or "outer-buffer-success"
            or "outer-buffer-fault" or "ordinary-redirect-reentry" or "held-transport" or "held-release" ? 1 : 0;
        Verify.That(state.Transports == expectedTransport, "Exact leased or control transport branch was not exercised.");
        Verify.That(state.Ordinary == (name == "ordinary-direct-nochildren" ? 1 : 0), "Ordinary path or redirect classification changed.");
        bool noAuthority = name is "unsupported-basic-refusal" or "pre-cancel-before-acquire" or "ordinary-direct-nochildren" or "control-nochildren";
        Verify.That(noAuthority ? state.Reads == 0 && state.Writes == 0 && state.ObserverCalls == 0 && state.TrustCalls == 0
            : state.Reads > 0 && state.Writes > 0 && state.ObserverCalls > 0 && state.TrustCalls > 0,
            "Authority observations were skipped or introduced on the wrong branch.");
        bool retained = name is "transport-fault-retains" or "transport-cancel-retains" || releaseFault;
        int validations = noAuthority ? 0 : name is "borrowed-success" or "borrowed-observer-refusal" or "held-success" ? 2 : 1;
        int trusts = name == "borrowed-observer-refusal" ? 1 : validations;
        Verify.That(state.ObserverCalls == validations && state.TrustCalls == trusts, "Fresh host/trust validation count changed.");
        int reads = noAuthority ? 0 : name is "transport-fault-retains" or "transport-cancel-retains" ? 2 : 3;
        int writes = noAuthority ? 0 : name is "transport-fault-retains" or "transport-cancel-retains" ? 1 : 2;
        Verify.That(state.Reads == reads && state.Writes == writes, "In-memory admission/release observation count changed.");
        Verify.That(state.Acquisitions == (name == "cancel-before-enrollment" ? 1 : 0), "Cancellation wrapper did not acquire exactly once.");
        Verify.That(fences == (retained ? 1 : 0), "Durable fence ownership or retention changed.");
        int releases = noAuthority || name is "transport-fault-retains" or "transport-cancel-retains" ? 0 : 1;
        Verify.That(state.Releases == releases, "Release callback count changed.");
        if (name is "borrowed-success" or "borrowed-observer-refusal" or "held-success" or "held-scope-closed-refusal")
            Verify.That(state.ScopePathVerified && insideFences == 1 && readsAfter == readsBefore,
                "The genuine borrowed/held scope was not preserved or acquired another fence.");
        else Verify.That(!state.ScopePathVerified, "An unrelated case claimed a borrowed scope.");
        if (name == "held-scope-closed-refusal")
            Verify.That(error is RouteCatalogUnavailableException && state.Transports == 0 && state.Faults.Count == 0,
                "Detached closed receiver admitted work or failed at an injected site.");
        if (name == "borrowed-observer-refusal")
            Verify.That(ReferenceEquals(error, state.Injected) && state.Faults.GetValueOrDefault("observer") == 1,
                "Fresh validation did not reach the changing observer.");
        if (releaseFault) Verify.That(state.Faults.GetValueOrDefault("release") == 1, "Release failure did not override the pending return/refusal.");
        if (transportFault) Verify.That(state.Faults.GetValueOrDefault("transport") == 1, "Transport injection did not fire exactly once.");
        if (name == "transport-cancel-retains") Verify.That(ReferenceEquals(error, state.Injected)
            && state.Faults.GetValueOrDefault("transport-cancel") == 1, "Transport cancellation did not fire exactly once.");
        if (bufferFault) Verify.That(state.Faults.GetValueOrDefault("buffer") == 1, "Default buffering fault did not fire.");
        int expectedFaults = releaseFault || transportFault || bufferFault || name is "transport-cancel-retains" or "borrowed-observer-refusal" ? 1 : 0;
        Verify.That(state.Faults.Count == expectedFaults && state.Faults.Values.All(count => count == 1), "Unexpected or repeated injected site.");
        Verify.That(requestBefore == (name == "ordinary-redirect-reentry" ? 1 : 0), "SDK changed caller request-content ownership.");
        Verify.That(responseBefore == (bufferFault ? 1 : 0), "SDK changed caller response ownership.");
        Verify.That(state.BodyReads == (name.StartsWith("outer-buffer-", StringComparison.Ordinal) ? 1 : 0), "Inner handler unexpectedly buffered a response.");
        Verify.That(state.HandlerReturned == (error is null || bufferFault), "Handler return versus outer buffering boundary changed.");
        Verify.That(state.HeldWait == (name is "held-transport" or "held-release"), "Held await branch was not exercised.");
        bool expectedCancel = canceled || name is "held-transport" or "held-release";
        Verify.That(state.Caller.IsCancellationRequested == expectedCancel, "Unexpected caller cancellation state.");
        Verify.That(state.NewEntries == (name == "ordinary-redirect-reentry" ? 2 : 1)
            && state.Connections == (name == "ordinary-redirect-reentry" ? 1 : 0), "Actual redirect recursion was not exercised.");
        var rows = state.Trace.ToArray();
        int transportStart = Array.FindIndex(rows, row => row.Operation == "transport.begin");
        int transportReturn = Array.FindIndex(rows, row => row.Operation == "transport.return");
        int release = Array.FindIndex(rows, row => row.Operation == "store.release");
        if (state.Releases == 1 && transportReturn >= 0)
            Verify.That(release > transportReturn, "Durable release preceded successful transport completion.");
        if (name.StartsWith("outer-buffer-", StringComparison.Ordinal))
        {
            int returned = Array.FindIndex(rows, row => row.Operation == "handler.return");
            int buffered = Array.FindIndex(rows, row => row.Operation == "response.buffer");
            Verify.That(transportStart >= 0 && transportReturn > transportStart && release > transportReturn && returned > release && buffered > returned,
                "Default body buffering was attributed inside primary transport or before release.");
        }
    }
}
