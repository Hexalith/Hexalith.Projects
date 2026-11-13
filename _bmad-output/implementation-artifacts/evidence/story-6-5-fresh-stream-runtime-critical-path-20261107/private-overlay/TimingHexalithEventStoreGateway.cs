using System.Diagnostics;
using System.Text.Json;
namespace Hexalith.PrivateFocusedTiming;
internal sealed class TimingHexalithEventStoreGateway : IDisposable
{
    private static readonly object Sync = new();
    private static readonly AsyncLocal<long> Current = new();
    private static long Sequence;
    private static int Records;
    private static int Samples;
    private static readonly Dictionary<(long Parent, string Stage), (long Count, long Total, long Units, long Min, long Max, long First, long Last)> Counts = new();
    private readonly long Id = Interlocked.Increment(ref Sequence);
    private readonly long Parent = Current.Value;
    private readonly long Started = Stopwatch.GetTimestamp();
    private readonly string Stage;
    private readonly bool Root;
    private readonly bool Aggregate;
    private int Closed;
    private TimingHexalithEventStoreGateway(string stage, bool root = false, bool aggregate = false) { Stage = stage; Root = root; Aggregate = aggregate; if (!aggregate) { Current.Value = Id; Write("begin", 0); } }
    internal static TimingHexalithEventStoreGateway? Start(string stage) => File.Exists("/tmp/story65-fresh-stream-overlay-20261107/collect.enabled") ? new(stage) : null;
    internal static TimingHexalithEventStoreGateway? Counted(string stage) => Current.Value != 0 ? new(stage, aggregate: true) : null;
    internal static TimingHexalithEventStoreGateway? Advisory() => File.Exists("/tmp/story65-fresh-stream-overlay-20261107/collect.enabled") && Interlocked.Increment(ref Samples) <= 3 ? new("advisory.root", true) : null;
    internal static void Open() { try { File.WriteAllText("/tmp/story65-fresh-stream-overlay-20261107/collect.enabled", "enabled"); } catch { } }
    internal static void Counter(string stage, long units = 0)
    {
        if (Current.Value == 0) return;
        try { Add(Current.Value, stage, 0, units, Stopwatch.GetTimestamp()); } catch { }
    }
    private static void Add(long parent, string stage, long elapsed, long units, long started)
    {
        lock (Sync) { var key = (parent, stage); Counts.TryGetValue(key, out var old); long now = Stopwatch.GetTimestamp();
            Counts[key] = (old.Count + 1, old.Total + elapsed, old.Units + units, old.Count == 0 ? units : Math.Min(old.Min, units), old.Count == 0 ? units : Math.Max(old.Max, units), old.Count == 0 ? started : old.First, now); }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref Closed, 1) != 0) return;
        if (Aggregate)
        {
            try { Add(Parent, Stage, Stopwatch.GetTimestamp() - Started, 0, Started); } catch { }
            return;
        }
        Write("end", Stopwatch.GetTimestamp() - Started); Current.Value = Parent;
        try { lock (Sync)
        {
            foreach (var entry in Counts.Where(entry => entry.Key.Parent == Id).ToArray())
            {
                if (Interlocked.Increment(ref Records) <= 250000)
                    File.AppendAllText("/tmp/story65-fresh-stream-overlay-20261107/Hexalith.EventStore.Gateway-" + Environment.ProcessId + ".jsonl", JsonSerializer.Serialize(new { component = "Hexalith.EventStore.Gateway", pid = Environment.ProcessId, stage = entry.Key.Stage, state = "aggregate", parent = Id, count = entry.Value.Count, elapsedTicks = entry.Value.Total, units = entry.Value.Units, minUnits = entry.Value.Min, maxUnits = entry.Value.Max, firstTicks = entry.Value.First, lastTicks = entry.Value.Last, frequency = Stopwatch.Frequency }) + Environment.NewLine);
                Counts.Remove(entry.Key);
            }
        } } catch { }
        if (Root && Volatile.Read(ref Samples) >= 3)
        {
            try { File.Delete("/tmp/story65-fresh-stream-overlay-20261107/collect.enabled"); File.WriteAllText("/tmp/story65-fresh-stream-overlay-20261107/sample-complete", "3"); } catch { }
        }
    }
    private void Write(string state, long elapsed)
    {
        try
        {
            if (Interlocked.Increment(ref Records) > 250000) return;
            var activity = Activity.Current;
            lock (Sync) File.AppendAllText("/tmp/story65-fresh-stream-overlay-20261107/Hexalith.EventStore.Gateway-" + Environment.ProcessId + ".jsonl",
                JsonSerializer.Serialize(new { component = "Hexalith.EventStore.Gateway", pid = Environment.ProcessId, stage = Stage, state,
                    ordinal = Id, parent = Parent, ticks = Stopwatch.GetTimestamp(), elapsedTicks = elapsed, frequency = Stopwatch.Frequency,
                    traceId = activity?.TraceId.ToHexString(), spanId = activity?.SpanId.ToHexString(), parentSpanId = activity?.ParentSpanId.ToHexString() }) + Environment.NewLine);
        }
        catch { }
    }
}
