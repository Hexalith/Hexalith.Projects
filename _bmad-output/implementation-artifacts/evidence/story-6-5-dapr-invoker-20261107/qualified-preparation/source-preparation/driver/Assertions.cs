namespace Story65.DaprInvokerProof;

using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Routing;

internal static class Assertions
{
    internal static void ForCase(DriverCase test, InvocationOutcome outcome)
    {
        FixtureState state = outcome.State;
        string fault = test.Fault;
        bool catalog = fault is "undeclared" or "missing-caller" or "transport-catalog" or "status-503" or "projection" or "release-catalog";
        bool canceled = fault is "transport-cancel" or "buffer-cancel" or "release-cancel" or "held-acquire";
        bool injected = fault is "acquire" or "resolver" or "release" or "logger";
        bool mapped = fault is "dapr" or "binding-poison" or "factory" or "transport" or "buffer-io" or "json-malformed" or "response-dispose" or "request-dispose";
        if (fault == "null") Check.Require(outcome.Error is ArgumentNullException && outcome.Result is null, "Original null guard changed.");
        else if (catalog) Check.Require(outcome.Error is RouteCatalogUnavailableException && outcome.Result is null, "Catalog exception was mapped or replaced.");
        else if (canceled) Check.Require(outcome.Error is OperationCanceledException && outcome.Result is null && outcome.CallerCanceled, "Original cancellation propagation changed.");
        else if (injected) Check.Require(outcome.Error is FixtureFailure && outcome.Result is null, "Outside-catch fixture error did not propagate.");
        else Check.Require(outcome.Error is null && outcome.Result is not null, "Expected-result invocation unexpectedly threw.");
        if (mapped) Check.Require(outcome.Result is { Success: false, ErrorMessage: QueryAdapterFailureReason.QueryExecutionFailure, PayloadBytes: null, Metadata: null }, "General HTTP failure was not bounded/mapped.");
        if (fault == "status-403") Check.Require(outcome.Result is { Success: false, ErrorMessage: "safe-denial" }, "Original safe-denial mapping changed.");
        if (fault == "json-null") Check.Require(outcome.Result is { Success: false, ErrorMessage: QueryAdapterFailureReason.ResponseMismatch }, "Original JSON-null mapping changed.");
        if (fault == "registration-null") Check.Require(outcome.Result is { Success: false } && outcome.Result.ErrorMessage!.StartsWith("No domain service is registered for tenant '", StringComparison.Ordinal), "Original missing-registration failure changed.");
        if (fault is "" or "held-release" or "cancel-release") Check.Require(outcome.Result is { Success: true, Metadata: not null } && outcome.Result.ProjectionType == state.Projection, "Healthy invoker result/metadata changed.");
        int faults = fault == "null" || fault == "" ? 0 : fault == "logger" ? 2 : 1;
        Check.Require(state.Faults.Values.Sum() == faults && state.Faults.Values.All(value => value == 1), "Requested fault/held branch did not fire exactly once.");
        if (faults != 0) Check.Require(state.Faults.ContainsKey(fault), "Expected fault site was not observed.");
        if (fault == "logger") Check.Require(state.Faults.ContainsKey("transport-for-logger"), "Logger override did not follow actual transport failure.");
        int acquired = test.Legacy || fault == "null" ? 0 : 1;
        int released = acquired == 1 && fault != "acquire" ? 1 : 0;
        int resolved = test.Legacy && fault != "null" ? 1 : 0;
        Check.Require(state.Acquisitions == acquired && state.Releases == released && state.Resolutions == resolved, "Original acquire/resolve/release count changed.");
        if (released != 0) Check.Require(state.RemoteBoundaryRestoredAtRelease, "Original remote boundary was not restored before lease release.");
        Check.Require(state.Requests == state.RequestDisposed && state.Responses == state.ResponseDisposed, "Request/response ownership leaked or disposed twice.");
        Check.Require(state.Logs.Count == (mapped || fault == "logger" ? 1 : 0), "Original log/catch boundary changed.");
        if (state.Requests != 0) Check.Require(state.RequestUsesOriginalQuery, "Dapr factory did not receive the original shared query instance.");
        if (state.RequestArgumentsJson is not null)
        {
            using var arguments = JsonDocument.Parse(state.RequestArgumentsJson);
            var root = arguments.RootElement;
            Check.Require(root.GetProperty("Method").GetString() == "POST"
                && root.GetProperty("Parameters").GetArrayLength() == 0
                && root.GetProperty("App").GetString() == (test.Legacy ? "synthetic-legacy-app" : SharedResources.Target.AppId)
                && root.GetProperty("Path").GetString() == (test.Legacy ? "query" : "synthetic-committed-query"),
                "Committed/legacy request target or original POST forwarding changed.");
        }
        if (state.RequestBytes is not null) Check.Require(state.BindingsVerified && state.BindingsJson is not null, "Original fresh binding was not verified.");
        if (fault == "binding-poison") Check.Require(state.Requests == 1 && state.RequestDisposed == 1 && state.Responses == 0
            && state.Trace.All(entry => entry.Operation != "client.factory") && state.Logs.Single().Contains("ArgumentNullException", StringComparison.Ordinal),
            "Poisoned actual Bind did not fail after lazy request creation and before client creation.");
        if (fault == "held-acquire") Check.Require(outcome.WaitHeldForAcquire, "Uncooperative original Acquire was not awaited.");
        if (fault is "held-release" or "cancel-release") Check.Require(outcome.WaitHeldForRelease, "Original release lifetime ended early.");
        if (fault == "cancel-release") Check.Require(outcome.CallerCanceled && outcome.Error is null && outcome.Result!.Success, "A new post-release cancellation check was invented.");
        Observation[] trace = state.Trace.ToArray();
        long At(string operation) => trace.First(entry => entry.Operation == operation).Sequence;
        if (state.Responses != 0) Check.Require(At("response.dispose") < At("request.dispose"), "Original response-before-request cleanup changed.");
        if (state.Requests != 0 && released != 0) Check.Require(At("request.dispose") < At("lease.release.begin"), "Original HTTP-before-lease cleanup changed.");
        if (state.Logs.Count != 0 && released != 0) Check.Require(At("logger") < At("lease.release.begin"), "Lease release moved into the HTTP catch boundary.");
        if (resolved != 0) Check.Require(trace.Single(entry => entry.Operation == "resolver").Argument.EndsWith("|v1", StringComparison.Ordinal), "Original omitted resolver-version default changed.");
    }
    internal static void Compare(InvocationOutcome ordinary, InvocationOutcome candidate)
    {
        Check.Require(ordinary.Error?.GetType() == candidate.Error?.GetType(), "Shared exception kind changed.");
        Check.Require((ordinary.Result is null) == (candidate.Result is null), "Result null shape changed.");
        if (ordinary.Result is { } first && candidate.Result is { } second)
        {
            Check.Require(first.Success == second.Success && first.ErrorMessage == second.ErrorMessage && first.ProjectionType == second.ProjectionType, "Result status/error/projection changed.");
            Check.Require((first.PayloadBytes is null) == (second.PayloadBytes is null)
                && (first.PayloadBytes ?? []).AsSpan().SequenceEqual(second.PayloadBytes ?? []), "Result payload bytes/null shape changed.");
            Check.Require((first.Metadata is null) == (second.Metadata is null) && JsonSerializer.Serialize(first.Metadata) == JsonSerializer.Serialize(second.Metadata), "Result metadata changed.");
        }
        Check.Require((ordinary.State.RequestBytes is null) == (candidate.State.RequestBytes is null)
            && (ordinary.State.RequestBytes ?? []).AsSpan().SequenceEqual(candidate.State.RequestBytes ?? []), "Submitted byte/null shape changed.");
        Check.Require(ordinary.State.RequestArgumentsJson == candidate.State.RequestArgumentsJson && ordinary.State.BindingsJson == candidate.State.BindingsJson
            && ordinary.State.Logs.SequenceEqual(candidate.State.Logs), "Original factory/binding/log effects changed.");
        Check.Require(ordinary.CallerCanceled == candidate.CallerCanceled && ordinary.ErrorTokenIsCaller == candidate.ErrorTokenIsCaller
            && ordinary.WaitHeldForAcquire == candidate.WaitHeldForAcquire && ordinary.WaitHeldForRelease == candidate.WaitHeldForRelease, "Cancellation/held lifetime changed.");
        Observation[] a = ordinary.State.Trace.ToArray(), b = candidate.State.Trace.ToArray();
        Check.Require(a.Length == b.Length, "Original observation count changed.");
        for (int index = 0; index < a.Length; index++)
        {
            Check.Require(a[index].Operation == b[index].Operation && a[index].Argument == b[index].Argument
                && a[index].Cancellable == b[index].Cancellable && a[index].CanceledAtRead == b[index].CanceledAtRead, "Original observation order/input changed.");
            for (int other = 0; other < a.Length; other++)
                Check.Require((a[index].Token == a[other].Token) == (b[index].Token == b[other].Token), "Per-run token equality graph changed.");
        }
    }
}
