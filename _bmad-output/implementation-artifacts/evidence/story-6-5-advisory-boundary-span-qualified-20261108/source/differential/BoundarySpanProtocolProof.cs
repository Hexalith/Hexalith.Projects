namespace Story65.AdvisoryWaveProof;

using System.Diagnostics;
using Hexalith.PrivateCaptureCounter;

// Direct synthetic adapter checks; these do not claim original callbacks ran.
internal static class BoundarySpanProtocolProof
{
    internal static async Task<List<BoundarySpanCase>> RunAsync()
    {
        List<BoundarySpanCase> results = [];
        static AdvisoryTerminalCapture New()
        {
            var state = AdvisoryTerminalCapture.Begin();
            TraceLog.Require(state is not null, "Boundary protocol capture not admitted.");
            return state!;
        }
        BoundarySpanCase Finish(string name, AdvisoryTerminalCapture state, long[] expected, int[] stages)
        {
            state.Dispose();
            long[] values = CounterControl.TerminalValues(state)!;
            long[] ticks = CounterControl.ElapsedValues(state)!;
            TraceLog.Require(values.SequenceEqual(expected), "Boundary proof changed terminal fields.");
            BoundarySpanExpectations.CheckMask(ticks, stages);
            var result = new BoundarySpanCase(name, values, ticks, stages);
            results.Add(result);
            return result;
        }
        static async Task<long> HoldAsync()
        {
            long start = Stopwatch.GetTimestamp();
            await Task.Delay(5).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            return Stopwatch.GetTimestamp() - start;
        }
        static void ExcludesGap(BoundarySpanCase result, long gap)
            => TraceLog.Require(result.SpanTicks.Skip(1).Sum() <= result.SpanTicks[0] - gap,
                "Selected spans absorbed a separately held omitted gap.");

        var all = New();
        foreach (var marker in new[] { AdvisoryBoundary.InitialScope, AdvisoryBoundary.EnvelopeValidation,
            AdvisoryBoundary.InitialInputs, AdvisoryBoundary.Index, AdvisoryBoundary.Discovery,
            AdvisoryBoundary.FinalInputs, AdvisoryBoundary.FinalCandidates, AdvisoryBoundary.FinalScope })
        {
            all.Enter(marker);
            await HoldAsync().ConfigureAwait(false);
        }
        all.Enter(AdvisoryBoundary.Response); all.ResponseReturned(false); all.Close(false, false);
        Finish("six-selected-markers", all, [10, 0, 1, 0, 0, 0], [1, 2, 3, 5, 6, 7]);

        var gap = New(); gap.Enter(AdvisoryBoundary.InitialScope);
        long scopeHold = await HoldAsync().ConfigureAwait(false);
        gap.Enter(AdvisoryBoundary.EnvelopeValidation);
        long gapStart = Stopwatch.GetTimestamp(); await Task.Delay(35).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        long gapTicks = Stopwatch.GetTimestamp() - gapStart;
        gap.Enter(AdvisoryBoundary.InitialInputs); await HoldAsync().ConfigureAwait(false);
        gap.Enter(AdvisoryBoundary.Response); gap.ResponseReturned(false); gap.Close(false, false);
        var gapResult = Finish("envelope-gap-excluded", gap, [10, 0, 1, 0, 0, 0], [1, 2]);
        TraceLog.Require(gapResult.SpanTicks[1] >= scopeHold, "Selected scope hold was not bracketed.");
        ExcludesGap(gapResult, gapTicks);

        var failed = New(); failed.Enter(AdvisoryBoundary.Index); await HoldAsync().ConfigureAwait(false);
        failed.Failure(AdvisoryExceptionKind.Other);
        failed.Enter(AdvisoryBoundary.FinalInputs); await HoldAsync().ConfigureAwait(false);
        failed.Failure(AdvisoryExceptionKind.Cancellation); // Stops the new span; first failure remains Index/Other.
        gapStart = Stopwatch.GetTimestamp(); await Task.Delay(35).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        gapTicks = Stopwatch.GetTimestamp() - gapStart;
        failed.Enter(AdvisoryBoundary.Response); failed.Termination(AdvisoryTermination.OperationFallbackEntered);
        failed.ResponseReturned(true); failed.Close(false, false);
        ExcludesGap(Finish("failure-ends-before-fallback", failed, [10, 3, 6, 2, 0, 0], [5, 7]), gapTicks);

        var closed = New(); closed.Enter(AdvisoryBoundary.FinalInputs); await HoldAsync().ConfigureAwait(false);
        closed.Close(false, true);
        var closedResult = Finish("close-dispose-idempotent", closed, [5, 0, 0, 0, 0, 1], [5]);
        closed.Close(true, false); closed.Dispose();
        TraceLog.Require(closedResult.SpanTicks.SequenceEqual(CounterControl.ElapsedValues(closed)!), "Repeated close changed spans.");

        var repeated = New(); repeated.Enter(AdvisoryBoundary.InitialInputs);
        long firstHold = await HoldAsync().ConfigureAwait(false); repeated.Enter(AdvisoryBoundary.EnvelopeValidation);
        gapStart = Stopwatch.GetTimestamp();
        repeated.Enter(AdvisoryBoundary.InitialInputs); await Task.Delay(35).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        repeated.Enter(AdvisoryBoundary.Index); repeated.Enter(AdvisoryBoundary.Discovery);
        gapTicks = Stopwatch.GetTimestamp() - gapStart;
        repeated.Enter(AdvisoryBoundary.Response); repeated.ResponseReturned(false); repeated.Close(false, false);
        var repeatResult = Finish("repeat-disables-future-spans", repeated, [10, 0, 1, 0, 0, 0], [2]);
        TraceLog.Require(repeatResult.SpanTicks[2] >= firstHold, "First interval was not retained.");
        ExcludesGap(repeatResult, gapTicks);

        var invalid = New(); invalid.Enter(AdvisoryBoundary.InitialInputs); await HoldAsync().ConfigureAwait(false);
        invalid.Enter((AdvisoryBoundary)12); invalid.Enter((AdvisoryBoundary)(-1));
        invalid.Failure(AdvisoryExceptionKind.None); invalid.Failure((AdvisoryExceptionKind)3);
        invalid.Termination((AdvisoryTermination)8); invalid.Termination((AdvisoryTermination)(-1));
        invalid.Enter(AdvisoryBoundary.Index); await HoldAsync().ConfigureAwait(false); invalid.Close(false, false);
        Finish("invalid-enums-ignored", invalid, [3, 0, 0, 0, 0, 0], [2, 7]);

        var omitted = New(); omitted.Enter(AdvisoryBoundary.Discovery); await HoldAsync().ConfigureAwait(false);
        omitted.Enter(AdvisoryBoundary.InitialConversation);
        gapStart = Stopwatch.GetTimestamp(); await Task.Delay(35).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        omitted.Enter(AdvisoryBoundary.FinalConversation); omitted.Enter(AdvisoryBoundary.Response);
        gapTicks = Stopwatch.GetTimestamp() - gapStart; omitted.Close(false, false);
        ExcludesGap(Finish("unselected-markers-end-span", omitted, [10, 0, 0, 0, 0, 0], [3]), gapTicks);

        var index = New(); index.Enter(AdvisoryBoundary.Index);
        long indexHold = await HoldAsync().ConfigureAwait(false);
        index.Enter(AdvisoryBoundary.Discovery); await HoldAsync().ConfigureAwait(false);
        index.Enter(AdvisoryBoundary.FinalInputs); await HoldAsync().ConfigureAwait(false);
        index.Enter(AdvisoryBoundary.FinalCandidates); await HoldAsync().ConfigureAwait(false);
        index.Enter(AdvisoryBoundary.FinalScope);
        gapStart = Stopwatch.GetTimestamp(); await Task.Delay(35).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        gapTicks = Stopwatch.GetTimestamp() - gapStart; index.Close(false, false);
        var indexResult = Finish("index-selected-final-scope-gap", index, [7, 0, 0, 0, 0, 0], [3, 5, 6, 7]);
        TraceLog.Require(indexResult.SpanTicks[7] >= indexHold, "Index hold was not bracketed by stage7.");
        ExcludesGap(indexResult, gapTicks);

        var cancellation = New(); using CancellationTokenSource canceled = new(); canceled.Cancel();
        cancellation.Enter(AdvisoryBoundary.FinalCandidates);
        try { await Task.Delay(1, canceled.Token).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            cancellation.Failure(AdvisoryExceptionKind.Cancellation);
            cancellation.Termination(AdvisoryTermination.OperationFallbackEntered);
        }
        cancellation.Enter(AdvisoryBoundary.Response); cancellation.ResponseReturned(true); cancellation.Close(false, true);
        Finish("final-marker-cancellation-no-callback-claim", cancellation, [10, 6, 6, 1, 0, 1], [6]);

        var late = New(); late.Enter(AdvisoryBoundary.FinalCandidates); await HoldAsync().ConfigureAwait(false);
        late.Close(false, false);
        var lateResult = Finish("late-events-cannot-change-spans", late, [6, 0, 0, 0, 0, 0], [6]);
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            late.Enter(AdvisoryBoundary.Index); late.Failure(AdvisoryExceptionKind.Other);
            late.Termination(AdvisoryTermination.OperationFallbackEntered); late.Close(true, true); late.Dispose();
        }))).WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        TraceLog.Require(lateResult.Values.SequenceEqual(CounterControl.TerminalValues(late)!)
            && lateResult.SpanTicks.SequenceEqual(CounterControl.ElapsedValues(late)!), "Late work changed closed state.");
        TraceLog.Require(results.Count == 10, "Boundary protocol case count changed.");
        return results;
    }
}
