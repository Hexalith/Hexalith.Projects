namespace Story65.OutboundAssertionProof;

using System.Text.Json;
using Hexalith.EventStore.ServiceDefaults.Authentication;

internal static class Assertions
{
    private static string[] Values(string headers, string name)
    {
        using var json = JsonDocument.Parse(headers);
        return json.RootElement.EnumerateArray().Where(h => string.Equals(h.GetProperty("Key").GetString(), name, StringComparison.OrdinalIgnoreCase))
            .SelectMany(h => h.GetProperty("Values").EnumerateArray().Select(v => v.GetString()!)).ToArray();
    }
    internal static void ForCase(DriverCase test, InvocationOutcome outcome)
    {
        FixtureState state = outcome.State; string fault = test.Fault;
        bool canonical = test.Route != "api/v1/commands" && fault != "null";
        bool injectedFailure = fault is "resolve-other" or "issuer-sync-fault" or "issuer-async-fault" or "base" or "logger-resolution" or "logger";
        bool invalid = fault is "cannot-bind" or "missing-command" or "issuer-invalid";
        bool canceled = fault is "issuer-cancel" or "base-cancel";
        if (fault == "null") Check.Require(outcome.Error is ArgumentNullException { ParamName: "request" }, "Original protected null guard changed.");
        else if (injectedFailure) Check.Require(outcome.Error is FixtureFailure, "Injected unhandled exception was mapped/replaced.");
        else if (invalid) Check.Require(outcome.Error is InvalidOperationException, "Original invalid-operation boundary changed.");
        else if (canceled) Check.Require(outcome.Error is OperationCanceledException && outcome.CallerCanceled && outcome.ErrorTokenIsCaller, "Original cancellation token/exception changed.");
        else if (fault == "buffer") Check.Require(outcome.Error is HttpRequestException { InnerException: FixtureFailure }
            && ReferenceEquals(outcome.Error.InnerException, state.LastInjectedError), "Outer HttpClient buffering failure did not preserve its owned inner fault.");
        else Check.Require(outcome.Error is null && outcome.ResponseReferencePreserved, "Expected original response unexpectedly threw/changed.");
        if (outcome.Error is not null && state.LastInjectedError is not null && fault != "buffer")
            Check.Require(ReferenceEquals(outcome.Error, state.LastInjectedError), "Original injected exception identity was not propagated.");
        int faultCount = fault is "" or "direct-sync" or "null" ? 0 : 1;
        Check.Require(state.Faults.Values.Sum() == faultCount && state.Faults.Values.All(v => v == 1)
            && (faultCount == 0 || state.Faults.ContainsKey(fault)), "Expected branch/fault was not exercised exactly once.");
        int resolutions = canonical ? 1 : 0;
        bool noIssuer = fault is "missing" or "missing-command" or "resolve-invalid" or "resolve-other" or "missing-logger" or "logger-resolution" or "logger";
        int issues = canonical && !noIssuer && fault != "cannot-bind" ? 1 : 0;
        bool unavailable = fault is "missing" or "resolve-invalid" or "issuer-null" or "missing-logger" or "logger-resolution" or "logger";
        bool beforeBaseFailure = fault is "resolve-other" or "issuer-sync-fault" or "issuer-async-fault" or "issuer-cancel"
            or "issuer-invalid" or "cannot-bind" or "missing-command" or "logger-resolution" or "logger" or "null";
        int baseCalls = beforeBaseFailure ? 0 : 1;
        Check.Require(state.IssuerResolutions == resolutions && state.Issues == issues && state.BaseCalls == baseCalls,
            "Original issuer/base branch/read counts changed.");
        Check.Require(state.CanBindReads == (test.Route == "process" && test.Bindings && !noIssuer ? 1 : 0), "Original command/query short-circuit CanBindResources reads changed.");
        Check.Require(state.LoggerResolutions == (unavailable ? 1 : 0)
            && state.Logs.Count == (unavailable && fault is not ("missing-logger" or "logger-resolution") ? 1 : 0), "Original generated logger/fallback effects changed.");
        Check.Require(outcome.RequestDisposedBeforeCaller == 0 && outcome.ResponseDisposedBeforeCaller == (fault == "buffer" ? 1 : 0),
            "Selected handler took ownership of caller messages or hid outer HttpClient disposal.");
        Check.Require(outcome.RequestContentPreserved && outcome.OptionsPreserved && state.RequestDisposed == 1 && state.ResponseDisposed == 1,
            "Original URI/content/options/caller disposal changed.");
        Check.Require(Values(outcome.FinalHeaders, "X-Synthetic-Unrelated").SequenceEqual(["synthetic-unrelated"]), "Unrelated header changed.");
        if (!canonical) Check.Require(outcome.FinalHeaders == outcome.InitialHeaders, "Noncanonical/null request credentials changed.");
        else
        {
            Check.Require(Values(outcome.FinalHeaders, "Authorization").Length == 0, "Forwarded bearer remained on canonical request.");
            bool issued = issues == 1 && fault is not ("issuer-null" or "issuer-sync-fault" or "issuer-async-fault" or "issuer-cancel" or "issuer-invalid");
            Check.Require(Values(outcome.FinalHeaders, EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName)
                .SequenceEqual(issued ? [FixtureState.FreshAssertion] : []), "Original assertion removal/replacement changed.");
        }
        if (state.BaseCalls != 0) Check.Require(state.OriginalRequestAtBase, "Base did not receive original request reference.");
        if (outcome.Error is null || fault == "buffer") Check.Require(state.OriginalResponseAtBoundary && state.ReturnedBeforeBuffer, "Original response did not cross handler boundary intact.");
        if (test.OuterBuffer) Check.Require(state.Buffers == 1 && state.ReturnedBeforeBuffer, "Default body buffering did not follow handler return.");
        else Check.Require(state.Buffers == 0, "Selected handler unexpectedly buffered the response body.");
        if (test.Slow) Check.Require(outcome.WaitHeld && outcome.CallerCanceled && outcome.Error is null && outcome.ResponseReferencePreserved,
            "Original uncooperative await gained cancellation/response changes.");
        foreach (var source in state.Sources) Check.Require(source.Completion.IsCompleted && source.Results == 1, "ValueTask source completion/consumption was not joined exactly once.");
    }
    internal static void Compare(InvocationOutcome ordinary, InvocationOutcome candidate)
    {
        Check.Require(ordinary.Error?.GetType() == candidate.Error?.GetType() && ordinary.Error?.Message == candidate.Error?.Message,
            "Original exception kind/message changed.");
        Check.Require(ordinary.ResponseReferencePreserved == candidate.ResponseReferencePreserved && ordinary.FinalHeaders == candidate.FinalHeaders
            && ordinary.InitialHeaders == candidate.InitialHeaders && ordinary.RequestContentPreserved == candidate.RequestContentPreserved
            && ordinary.OptionsPreserved == candidate.OptionsPreserved, "Original message/header/options effects changed.");
        Check.Require(ordinary.RequestDisposedBeforeCaller == candidate.RequestDisposedBeforeCaller && ordinary.ResponseDisposedBeforeCaller == candidate.ResponseDisposedBeforeCaller
            && ordinary.State.Logs.SequenceEqual(candidate.State.Logs), "Original ownership/log effects changed.");
        Check.Require(ordinary.CallerCanceled == candidate.CallerCanceled && ordinary.ErrorTokenIsCaller == candidate.ErrorTokenIsCaller
            && ordinary.WaitHeld == candidate.WaitHeld, "Original cancellation/held behavior changed.");
        Observation[] a = ordinary.State.Trace.ToArray(), b = candidate.State.Trace.ToArray();
        Check.Require(a.Length == b.Length, "Original observation count changed.");
        for (int index = 0; index < a.Length; index++)
        {
            Check.Require(a[index].Operation == b[index].Operation && a[index].Argument == b[index].Argument
                && a[index].Cancellable == b[index].Cancellable && a[index].CanceledAtRead == b[index].CanceledAtRead, "Original read/await order/input changed.");
            for (int other = 0; other < a.Length; other++)
                Check.Require((a[index].Token == a[other].Token) == (b[index].Token == b[other].Token), "Per-run token equality graph changed.");
        }
    }
}
