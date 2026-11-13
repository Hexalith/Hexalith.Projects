namespace Story65.DelegatedClientProof;

using Hexalith.EventStore.Contracts.Queries;

internal static class Assertions
{
    private static readonly string[] CallbackFaults = ["issuer-null", "issuer-throw", "issuer-source-fault", "issuer-source-cancel",
        "config-workload", "config-key", "config-timeout", "config-gateway", "factory-throw", "proof-clock-throw", "dapr-request-throw",
        "handler-throw", "buffer-throw", "json-malformed", "response-mismatch", "response-disposal-throw", "request-disposal-throw",
        "late-response-disposal", "late-request-disposal", "buffered-body-delay", "issuer-cooperative", "transport-cooperative",
        "buffer-cooperative", "issuer-uncooperative"];
    internal static void ForCase(DriverCase test, InvocationOutcome outcome)
    {
        var state = outcome.State; var trace = state.Trace.ToArray();
        bool expectsError = test.Fault is "null-query" or "issuer-throw" or "issuer-source-fault" or "issuer-source-cancel"
            or "config-workload" or "config-key" or "config-timeout" or "config-gateway" or "factory-throw" or "proof-clock-throw"
            or "dapr-request-throw" or "handler-throw" or "proof-key-invalid" or "buffer-throw" or "json-malformed"
            or "response-disposal-throw" or "request-disposal-throw" or "late-response-disposal" or "late-request-disposal"
            or "issuer-cooperative" or "transport-cooperative" or "buffer-cooperative" or "issuer-uncooperative"
            || test.Budget == "invalid-protected-synthetic";
        if (!expectsError) Check.Require(outcome.Error is null && outcome.Result is not null, "Expected-result case unexpectedly threw.");
        else Check.Require(outcome.Error is not null && outcome.Result is null, "Requested exception case returned a result.");
        if (test.Fault == "null-query") Check.Require(outcome.Error is ArgumentNullException, "Null input guard changed.");
        if (CallbackFaults.Contains(test.Fault, StringComparer.Ordinal)) Check.Require(state.Triggered == 1, "Requested fault/latency callback did not run exactly once.");
        else Check.Require(state.Triggered == 0, "Unexpected callback fault.");
        foreach (string singleton in new[] { "issuer.issue", "dapr.request", "factory.create", "http.handler", "request.dispose", "response.dispose" })
            Check.Require(trace.Count(row => row.Operation == singleton) <= 1, "Dependency or owned disposal ran more than once.");
        bool sent = trace.Any(row => row.Operation == "http.handler");
        bool created = trace.Any(row => row.Operation == "dapr.request") && test.Fault != "dapr-request-throw";
        Check.Require(state.RequestDisposed == (created ? 1 : 0), "Created request was not disposed exactly once, including pre-Send errors.");
        if (sent && test.Fault is not ("transport-cooperative" or "handler-throw")) Check.Require(state.ResponseDisposed == 1, "Owned response was not disposed exactly once.");
        long Sequence(string op) => trace.Single(row => row.Operation == op).Sequence;
        if (state.ResponseDisposed == 1) Check.Require(Sequence("response.dispose") < Sequence("request.dispose"), "Response/request disposal order changed.");
        if (test.Budget is not null && outcome.Error is null && state.RequestDisposed == 1)
            Check.Require(trace.Last(row => row.Operation == "clock.timestamp").Sequence > Sequence("request.dispose"), "Final budget check preceded owned disposal.");
        if (test.Fault is "late-response-disposal" or "late-request-disposal")
        {
            Check.Require(outcome.Error?.GetType() == typeof(OperationCanceledException) && !outcome.CallerCanceled, "Late disposal bypassed owner budget or consumed the caller budget.");
            Check.Require(Sequence("invoke.exit") > Sequence("request.dispose"), "Expired method escaped before owned disposal.");
            // The original canceled-token operand may short-circuit GetElapsedTime; do not invent a clock read.
            var timestamps = trace.Where(row => row.Operation == "clock.timestamp").ToArray();
            if (timestamps.Length > 1) Check.Require(timestamps[^1].Sequence > Sequence("request.dispose"), "Elapsed-time operand ran before disposal.");
            else Check.Require(trace.Single(row => row.Operation == "issuer.issue").Token.IsCancellationRequested, "No canceled token or elapsed-time check explained the owner expiry.");
        }
        if (test.Fault.EndsWith("cooperative", StringComparison.Ordinal))
            Check.Require(outcome.Error is OperationCanceledException && outcome.CallerCanceled, "Original two-second caller cancellation was not preserved.");
        if (test.Fault is "issuer-source-cancel") Check.Require(outcome.Error is OperationCanceledException, "Canceled ValueTask did not preserve its exception.");
        if (test.Fault is "issuer-source-fault" or "issuer-throw" or "config-workload" or "config-key" or "config-timeout" or "config-gateway"
            or "factory-throw" or "proof-clock-throw" or "dapr-request-throw" or "handler-throw" or "response-disposal-throw" or "request-disposal-throw")
            Check.Require(outcome.Error is FixtureFailure, "Injected exception type was not preserved.");
        if (test.Fault == "proof-key-invalid") Check.Require(outcome.Error is FormatException && !sent && created, "Malformed signing key failed at the wrong checkpoint.");
        if (test.Fault == "json-malformed") Check.Require(outcome.Error is System.Text.Json.JsonException && sent, "Malformed buffered JSON did not reach decode.");
        if (test.Fault == "buffer-throw") Check.Require(outcome.Error is HttpRequestException { InnerException: FixtureFailure }, "Buffer-copy failure did not preserve its expected wrapped primary exception.");
        if (test.Budget == "invalid-protected-synthetic") Check.Require(outcome.Error is InvalidOperationException && !trace.Any(row => row.Operation == "issuer.issue"), "Invalid budget advanced to acquisition.");
        if (outcome.Error is not null) return;
        var result = outcome.Result!;
        if (test.Fault is "missing-caller" or "foreign-tenant" or "duplicate-assertion") Check.Require(result.ErrorMessage == "safe-denial" && !sent, "Original caller guard changed.");
        else if (string.IsNullOrWhiteSpace(test.Workload) || test.Fault is "missing-key" or "whitespace-key" or "issuer-null")
            Check.Require(result.ErrorMessage == QueryAdapterFailureReason.QueryExecutionFailure && !sent, "Unavailable signing guard changed.");
        else if (test.Status != 200) Check.Require(result.ErrorMessage == (test.Status == 404 ? "safe-denial" : QueryAdapterFailureReason.QueryExecutionFailure), "Owner status mapping changed.");
        else if (test.Fault == "response-mismatch") Check.Require(result.ErrorMessage == QueryAdapterFailureReason.ResponseMismatch, "Response mismatch mapping changed.");
        else Check.Require(result.Success && result.GetPayload().GetProperty("allowed").GetBoolean() && state.ProofVerified, "Positive client fixture did not execute fully.");
        if (test.Fault == "buffered-body-delay") Check.Require(Sequence("response.buffer.end") < Sequence("response.dispose"), "Buffered body ownership changed.");
    }

    internal static void Compare(InvocationOutcome original, InvocationOutcome changed)
    {
        Check.Require(original.Error?.GetType() == changed.Error?.GetType(), "Exception type changed.");
        if (original.Error is ArgumentException left && changed.Error is ArgumentException right) Check.Require(left.ParamName == right.ParamName, "Exception parameter changed.");
        Check.Require(original.CallerCanceled == changed.CallerCanceled, "Caller cancellation state changed.");
        Check.Require(original.ErrorTokenIsCaller == changed.ErrorTokenIsCaller, "Exception caller-token identity changed.");
        Check.Require(original.ChildAfterReturn == changed.ChildAfterReturn, "Instantaneous acquisition child completion state changed.");
        Check.Require(original.UnboundedHeldAfterCallerCancel == changed.UnboundedHeldAfterCallerCancel, "Unbounded caller wait semantics changed.");
        if (original.Result is { } first && changed.Result is { } second)
            Check.Require(first.Success == second.Success && first.ErrorMessage == second.ErrorMessage && first.ProjectionType == second.ProjectionType
                && (first.PayloadBytes is null) == (second.PayloadBytes is null)
                && (first.PayloadBytes ?? []).AsSpan().SequenceEqual(second.PayloadBytes ?? [])
                && System.Text.Json.JsonSerializer.Serialize(first.Metadata) == System.Text.Json.JsonSerializer.Serialize(second.Metadata), "Result bytes/envelope changed.");
        else Check.Require(original.Result is null && changed.Result is null, "Result/exception shape changed.");
        Check.Require((original.State.RequestBytes is null) == (changed.State.RequestBytes is null)
            && (original.State.RequestBytes ?? []).AsSpan().SequenceEqual(changed.State.RequestBytes ?? [])
            && original.State.Assertion == changed.State.Assertion && original.State.ProofVerified == changed.State.ProofVerified, "Signed request semantics changed.");
        var a = original.State.Trace.ToArray(); var b = changed.State.Trace.ToArray(); Check.Require(a.Length == b.Length, "Dependency read count changed.");
        for (int n = 0; n < a.Length; n++)
        {
            Check.Require(a[n].Operation == b[n].Operation && a[n].Argument == b[n].Argument && a[n].CanBeCanceled == b[n].CanBeCanceled
                && a[n].CanceledAtRead == b[n].CanceledAtRead, "Dependency read/order/input/cancellation changed.");
            for (int other = 0; other < a.Length; other++) Check.Require((a[n].Token == a[other].Token) == (b[n].Token == b[other].Token), "Per-run token equality graph changed.");
        }
    }
}
