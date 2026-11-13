namespace Story65.AdvisoryWaveProof;

using System.Text.Json;
using Hexalith.PrivateCaptureCounter;

internal static class TerminalProtocolProof
{
    internal static async Task<int> RunAsync(string[] args)
    {
        await CounterControl.ReadyAsync("active").ConfigureAwait(false);
        List<long[]> rows = [];
        List<long[]> spans = [];
        long before = CounterControl.Read("Admitted");
        AdvisoryTerminalCapture New()
        {
            var state = AdvisoryTerminalCapture.Begin();
            TraceLog.Require(state is not null, "Protocol proof was not admitted.");
            return state!;
        }
        void Closed(AdvisoryTerminalCapture state, long[] expected)
        {
            state.Dispose();
            long[] actual = CounterControl.TerminalValues(state)!;
            TraceLog.Require(actual.SequenceEqual(expected), "Terminal protocol source values changed.");
            rows.Add(actual);
            spans.Add(CounterControl.ElapsedValues(state)!);
        }

        var repeated = New();
        repeated.Enter(AdvisoryBoundary.Index);
        repeated.Failure(AdvisoryExceptionKind.Other);
        repeated.Enter(AdvisoryBoundary.Response);
        repeated.Termination(AdvisoryTermination.OperationFallbackEntered);
        repeated.ResponseReturned(true); // Fallback classification and its saved failure survive Response.
        repeated.Close(false, false);
        repeated.Close(true, true);
        Closed(repeated, [10, 3, 6, 2, 0, 0]);
        repeated.Dispose();

        var parallel = New();
        using Barrier rendezvous = new(4);
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            TraceLog.Require(rendezvous.SignalAndWait(TimeSpan.FromSeconds(2)), "Protocol workers did not rendezvous.");
            parallel.Enter(AdvisoryBoundary.Discovery);
            parallel.Failure(AdvisoryExceptionKind.Cancellation);
        }))).ConfigureAwait(false);
        parallel.Enter(AdvisoryBoundary.Response);
        parallel.ResponseReturned(true);
        parallel.Close(false, false);
        Closed(parallel, [10, 4, 2, 1, 0, 0]);

        var late = New();
        late.Enter(AdvisoryBoundary.FinalCandidates);
        late.Close(false, true);
        Closed(late, [6, 0, 0, 0, 0, 1]);
        long[] frozen = CounterControl.TerminalValues(late)!;
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            late.Enter(AdvisoryBoundary.Response);
            late.Failure(AdvisoryExceptionKind.Other);
            late.Termination(AdvisoryTermination.DenialReturnEntered);
            late.ResponseReturned(false);
            late.Close(true, true);
            late.Dispose();
        }))).ConfigureAwait(false);
        TraceLog.Require(frozen.SequenceEqual(CounterControl.TerminalValues(late)!), "Late work changed a closed record.");

        var invalid = New();
        invalid.Enter((AdvisoryBoundary)12);
        invalid.Enter((AdvisoryBoundary)(-1));
        invalid.Failure(AdvisoryExceptionKind.None);
        invalid.Failure((AdvisoryExceptionKind)3);
        invalid.Termination((AdvisoryTermination)8);
        invalid.Termination((AdvisoryTermination)(-1));
        invalid.Close(false, false);
        Closed(invalid, [0, 0, 0, 0, 0, 0]);
        var boundaryCases = await BoundarySpanProtocolProof.RunAsync().ConfigureAwait(false);
        rows.AddRange(boundaryCases.Select(test => test.Values));
        spans.AddRange(boundaryCases.Select(test => test.SpanTicks));
        TraceLog.Require(CounterControl.Read("Admitted") - before == 14, "Protocol capture count changed.");
        await CounterControl.FinishAsync("active").ConfigureAwait(false);
        await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(new
        {
            Cases = 14, Passed = 14, ErrorType = "none", Captures = 14,
            OriginalTerminalProtocolCases = 4, BoundarySpanCases = 10, SpanTicks = spans, BoundaryCases = boundaryCases,
            TerminalValues = rows, SyntheticOnly = true, ApplicationValuesPersisted = false,
        })).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { cases = 14, passed = 14, errorType = "none" }));
        return 0;
    }
}
