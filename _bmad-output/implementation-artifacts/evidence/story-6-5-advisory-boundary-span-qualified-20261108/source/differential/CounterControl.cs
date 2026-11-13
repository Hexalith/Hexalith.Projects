namespace Story65.AdvisoryWaveProof;

using System.Diagnostics;
using System.Reflection;
using Hexalith.PrivateCaptureCounter;

// Harness-only observation of fixed numeric helper fields. No application values or callbacks cross the recorder boundary.
internal static class CounterControl
{
    private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
    internal static long Read(string field) => Convert.ToInt64(typeof(CaptureCounters).GetField(field, Flags)!.GetValue(null));
    internal static long[]? TerminalValues(AdvisoryTerminalCapture? terminal)
    {
        if (terminal is null) return null;
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        var capture = (CaptureCounters)typeof(AdvisoryTerminalCapture).GetField("_capture", instance)!.GetValue(terminal)!;
        TraceLog.Require((int)typeof(CaptureCounters).GetField("_disposed", instance)!.GetValue(capture)! == 1,
            "Terminal proof read before original capture disposal.");
        return ((long[])typeof(CaptureCounters).GetField("_counts", instance)!.GetValue(capture)!).ToArray();
    }
    internal static long[]? ElapsedValues(AdvisoryTerminalCapture? terminal)
    {
        if (terminal is null) return null;
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        var capture = (CaptureCounters)typeof(AdvisoryTerminalCapture).GetField("_capture", instance)!.GetValue(terminal)!;
        TraceLog.Require((int)typeof(CaptureCounters).GetField("_disposed", instance)!.GetValue(capture)! == 1,
            "Span proof read before original capture disposal.");
        return ((long[])typeof(CaptureCounters).GetField("_elapsed", instance)!.GetValue(capture)!).ToArray();
    }
    internal static async Task ReadyAsync(string mode)
    {
        CaptureCounters.Begin(Role.Unsupported); // Starts the exact helper without admitting a business capture.
        Console.WriteLine("READY:" + Stopwatch.Frequency + ":" + Stopwatch.GetTimestamp());
        if (mode == "disabled") return;
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        while (Read("WindowEnd") == 0) await Task.Delay(5, timeout.Token).ConfigureAwait(false);
    }
    internal static async Task FloodForCapAsync()
    {
        long until = Stopwatch.GetTimestamp() + 3 * Stopwatch.Frequency;
        long droppedBefore = Read("QueueDropped"), closedBefore = Read("ClosedDropped"), disposed = 0;
        for (int attempts = 0; attempts < 1000000 && Stopwatch.GetTimestamp() < until; attempts++)
        {
            var capture = CaptureCounters.Begin(Role.Projects);
            TraceLog.Require(capture is not null, "Window closed during bounded pre-cap preparation.");
            // A Whole row always exists. No Count() or interval/allocation API is invoked by this scalar prefill.
            capture!.Dispose(); disposed++;
            long enqueued = disposed - (Read("QueueDropped") - droppedBefore) - (Read("ClosedDropped") - closedBefore);
            if (enqueued >= 65538) break;
            if (attempts % 1024 == 0) await Task.Delay(1).ConfigureAwait(false);
        }
        long accepted = disposed - (Read("QueueDropped") - droppedBefore) - (Read("ClosedDropped") - closedBefore);
        TraceLog.Require(accepted >= 65538, "Pre-cap accepted-row proof was not reached within its bound.");
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(1));
        while (QueueCount() != 0) await Task.Delay(1, timeout.Token).ConfigureAwait(false);
        TraceLog.Require(Read("IoFailures") == 0 && Read("ClosedDropped") == closedBefore && Read("WorkerClosed") == 0,
            "Accepted rows cannot prove an ordinary sink cap after I/O failure or closure.");
        // At least 65,537 rows have now been processed (at most one is in flight), proving a cap before real cases.
    }
    private static int QueueCount()
    {
        object queue = typeof(CaptureCounters).GetField("Queue", Flags)!.GetValue(null)!;
        object reader = queue.GetType().GetProperty("Reader")!.GetValue(queue)!;
        return (int)reader.GetType().GetProperty("Count")!.GetValue(reader)!;
    }
    internal static async Task FinishAsync(string mode)
    {
        if (mode == "disabled") return;
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(32));
        while (Read("WorkerClosed") == 0) await Task.Delay(10, timeout.Token).ConfigureAwait(false);
        TraceLog.Require(Read("Active") == 0, "Helper retained an active business capture.");
        TraceLog.Require(Read("Completed") == Read("Admitted"), "Helper completion accounting changed.");
        // Keep the process alive until the controller observes a complete closed receipt, not merely WorkerClosed.
        Console.WriteLine("CASES_DONE");
        string? command = await Console.In.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        TraceLog.Require(command == "EXIT", "Controller did not confirm the closed sink.");
    }
}
