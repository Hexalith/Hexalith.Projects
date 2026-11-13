namespace Story65.InvocationTransportProof;

using System.Diagnostics;
using System.Reflection;

// Operates ONLY on the helper inside the private SDK assembly, never a driver-compiled helper copy.
internal sealed class CounterControl(SdkVariant variant)
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
    private Type CounterType => variant.Assembly.GetType("Hexalith.PrivateCaptureCounter.CaptureCounters", true)!;
    private object? Begin() => variant.Assembly.GetType("Hexalith.PrivateCaptureCounter.InvocationTransportBootstrap", true)!.GetMethod("Begin", Flags)!.Invoke(null, null);
    internal long Read(string name) => Convert.ToInt64(CounterType.GetField(name, Flags)!.GetValue(null));
    internal async Task ReadyAsync(string mode)
    {
        (Begin() as IDisposable)?.Dispose();
        Console.WriteLine("READY:" + Stopwatch.Frequency + ":" + Stopwatch.GetTimestamp());
        if (mode == "disabled") return;
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        while (Read("WindowEnd") == 0) await Task.Delay(5, timeout.Token).ConfigureAwait(false);
    }
    internal async Task FloodAsync()
    {
        long drop = Read("QueueDropped"), closed = Read("ClosedDropped"), disposed = 0;
        long until = Stopwatch.GetTimestamp() + 3 * Stopwatch.Frequency;
        for (int attempt = 0; attempt < 1000000 && Stopwatch.GetTimestamp() < until; attempt++)
        {
            var capture = Begin() as IDisposable; Check.Require(capture is not null, "Window ended during bounded pre-cap preparation.");
            capture!.Dispose(); disposed++;
            if (disposed - (Read("QueueDropped") - drop) - (Read("ClosedDropped") - closed) >= 65538) break;
            if (attempt % 1024 == 0) await Task.Delay(1).ConfigureAwait(false);
        }
        Check.Require(disposed - (Read("QueueDropped") - drop) - (Read("ClosedDropped") - closed) >= 65538, "Actual cap was not proven before cases.");
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(1));
        object queue = CounterType.GetField("Queue", Flags)!.GetValue(null)!;
        object reader = queue.GetType().GetProperty("Reader")!.GetValue(queue)!;
        while ((int)reader.GetType().GetProperty("Count")!.GetValue(reader)! != 0) await Task.Delay(1, timeout.Token).ConfigureAwait(false);
        Check.Require(Read("IoFailures") == 0 && Read("ClosedDropped") == closed && Read("WorkerClosed") == 0, "Accepted-row cap proof was not an open ordinary sink.");
    }
    internal async Task FinishAsync(string mode)
    {
        if (mode == "disabled") return;
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(32));
        while (Read("WorkerClosed") == 0) await Task.Delay(10, timeout.Token).ConfigureAwait(false);
        Check.Require(Read("Active") == 0 && Read("Completed") == Read("Admitted"), "Private SDK retained unfinished captures.");
        Console.WriteLine("CASES_DONE");
        Check.Require(await Console.In.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false) == "EXIT", "No complete closed-receipt acknowledgement.");
    }
}
