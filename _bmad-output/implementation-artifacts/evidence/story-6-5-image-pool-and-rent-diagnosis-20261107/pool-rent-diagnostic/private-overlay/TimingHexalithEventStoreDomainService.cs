using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Security.Cryptography;
namespace Hexalith.PrivateFocusedTiming;
internal sealed class TimingHexalithEventStoreDomainService : IDisposable
{
    private sealed class Range(long start) { internal long Start = start; internal long End; }
    private static readonly object Sync = new();
    private static readonly AsyncLocal<TimingHexalithEventStoreDomainService?> Active = new();
    private static readonly Dictionary<(long Anchor, string Stage, string ParentStage, string Phase), (long Count, long Total, long Self, long Units, long Min, long Max, long First, long Last)> Counts = new();
    private static long Sequence, LastResource;
    private static int Records, Samples;
    private static readonly bool Resources = typeof(TimingHexalithEventStoreDomainService).Assembly.GetName().Name == "Hexalith.EventStore.ServiceDefaults";
    private readonly long Id = Interlocked.Increment(ref Sequence);
    private readonly long Started = Stopwatch.GetTimestamp();
    private readonly TimingHexalithEventStoreDomainService? Prior = Active.Value;
    private readonly object ChildrenGate = new();
    private List<Range>? Children;
    private readonly Range? ParentSlot;
    private readonly string Stage, Phase;
    private readonly bool Aggregate, Root;
    private readonly long Anchor, Parent;
    private int Closed;
    private TimingHexalithEventStoreDomainService(string stage, bool aggregate = false, bool root = false)
    {
        Stage = stage; Aggregate = aggregate; Root = root;
        Parent = Prior?.Anchor ?? 0; Anchor = aggregate ? Parent : Id;
        Phase = stage == "technical.observe" ? "initial" : stage == "technical.complete" ? "complete" : Prior?.Phase ?? "none";
        if (Prior is not null) { ParentSlot = new(Started); lock (Prior.ChildrenGate) (Prior.Children ??= []).Add(ParentSlot); }
        Active.Value = this;
        if (!aggregate) Write("begin", 0, 0);
    }
    internal static TimingHexalithEventStoreDomainService? Start(string stage) { try { return File.Exists("/tmp/story65-pool-rent-overlay-20261107/collect.enabled") ? new(stage) : null; } catch { return null; } }
    internal static TimingHexalithEventStoreDomainService? Counted(string stage) { try { return Active.Value?.Anchor > 0 ? new(stage, aggregate: true) : null; } catch { return null; } }
    internal static TimingHexalithEventStoreDomainService? Advisory() { try { return File.Exists("/tmp/story65-pool-rent-overlay-20261107/collect.enabled") && Interlocked.Increment(ref Samples) <= 3 ? new("advisory.root", root: true) : null; } catch { return null; } }
    internal static void Open() { try { File.WriteAllText("/tmp/story65-pool-rent-overlay-20261107/collect.enabled", "multiple"); } catch { } }
    internal static void Counter(string stage, long units = 0)
    {
        try { if (Active.Value is { Anchor: > 0 } parent) Add(parent.Anchor, stage, parent.Stage, parent.Phase, 0, 0, units, Stopwatch.GetTimestamp()); } catch { }
    }
    private static void Add(long anchor, string stage, string parentStage, string phase, long total, long self, long units, long started)
    {
        lock (Sync) { var key = (anchor, stage, parentStage, phase); Counts.TryGetValue(key, out var old); long now = Stopwatch.GetTimestamp();
            Counts[key] = (old.Count + 1, old.Total + total, old.Self + self, old.Units + units, old.Count == 0 ? units : Math.Min(old.Min, units), old.Count == 0 ? units : Math.Max(old.Max, units), old.Count == 0 ? started : old.First, now); }
    }
    private long Self(long ended)
    {
        Range[] children; lock (ChildrenGate) children = Children?.ToArray() ?? [];
        long union = 0, from = 0, until = 0;
        foreach (var child in children.OrderBy(child => child.Start))
        {
            long start = Math.Max(Started, child.Start), end = Math.Min(ended, Volatile.Read(ref child.End) > 0 ? Volatile.Read(ref child.End) : ended);
            if (end <= start) continue;
            if (until == 0) { from = start; until = end; }
            else if (start <= until) until = Math.Max(until, end);
            else { union += until - from; from = start; until = end; }
        }
        if (until > 0) union += until - from;
        return Math.Max(0, ended - Started - union);
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref Closed, 1) != 0) return;
        long ended = Stopwatch.GetTimestamp();
        if (ParentSlot is not null) Interlocked.Exchange(ref ParentSlot.End, ended);
        try
        {
            long self = Self(ended);
            if (Aggregate) Add(Anchor, Stage, Prior?.Stage ?? "none", Phase, ended - Started, self, 0, Started);
            else
            {
                Write("end", ended - Started, self);
                lock (Sync)
                {
                    foreach (var entry in Counts.Where(entry => entry.Key.Anchor == Id || entry.Key.Anchor == 0).ToArray())
                    {
                        if (Interlocked.Increment(ref Records) <= 250000)
                            File.AppendAllText("/tmp/story65-pool-rent-overlay-20261107/Hexalith.EventStore.DomainService-" + Environment.ProcessId + ".jsonl", JsonSerializer.Serialize(new { component = "Hexalith.EventStore.DomainService", pid = Environment.ProcessId, stage = entry.Key.Stage, state = "aggregate", parent = Id, parentStage = entry.Key.ParentStage, phase = entry.Key.Phase, count = entry.Value.Count, elapsedTicks = entry.Value.Total, selfTicks = entry.Value.Self, units = entry.Value.Units, minUnits = entry.Value.Min, maxUnits = entry.Value.Max, firstTicks = entry.Value.First, lastTicks = entry.Value.Last, frequency = Stopwatch.Frequency }) + Environment.NewLine);
                        Counts.Remove(entry.Key);
                    }
                }
            }
        }
        catch { }
        finally { if (ReferenceEquals(Active.Value, this)) Active.Value = Prior; }
        if (Root && Volatile.Read(ref Samples) >= 3)
        { try { File.Delete("/tmp/story65-pool-rent-overlay-20261107/collect.enabled"); File.WriteAllText("/tmp/story65-pool-rent-overlay-20261107/sample-complete", "3"); } catch { } }
    }
    private void Write(string state, long elapsed, long self)
    {
        try
        {
            if (Interlocked.Increment(ref Records) > 250000) return;
            var activity = Activity.Current;
            lock (Sync) File.AppendAllText("/tmp/story65-pool-rent-overlay-20261107/Hexalith.EventStore.DomainService-" + Environment.ProcessId + ".jsonl", JsonSerializer.Serialize(new { component = "Hexalith.EventStore.DomainService", pid = Environment.ProcessId, stage = Stage, state, ordinal = Id, parent = Parent, parentStage = Prior?.Stage ?? "none", phase = Phase, ticks = Stopwatch.GetTimestamp(), elapsedTicks = elapsed, selfTicks = self, frequency = Stopwatch.Frequency, traceId = activity?.TraceId.ToHexString(), spanId = activity?.SpanId.ToHexString(), parentSpanId = activity?.ParentSpanId.ToHexString() }) + Environment.NewLine);
            Resource();
        }
        catch { }
    }
    private static void Resource()
    {
        if (!Resources) return;
        try
        {
            long now = Stopwatch.GetTimestamp(), prior = Volatile.Read(ref LastResource);
            if (now - prior < Stopwatch.Frequency / 10 || Interlocked.CompareExchange(ref LastResource, now, prior) != prior) return;
            using var process = Process.GetCurrentProcess();
            ThreadPool.GetMinThreads(out int minimumWorkers, out int minimumIo); ThreadPool.GetMaxThreads(out int maximumWorkers, out int maximumIo);
            string? raw = Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT"); string processorOverride = raw is null ? "absent" : int.TryParse(raw, out int count) && count > 0 ? count.ToString(System.Globalization.CultureInfo.InvariantCulture) : "non-numeric";
            var memory = GC.GetGCMemoryInfo();
            lock (Sync) if (Interlocked.Increment(ref Records) <= 250000)
                File.AppendAllText("/tmp/story65-pool-rent-overlay-20261107/Hexalith.EventStore.DomainService-" + Environment.ProcessId + ".jsonl", JsonSerializer.Serialize(new { component = "Hexalith.EventStore.DomainService", pid = Environment.ProcessId, stage = "runtime.sample", state = "resource", ticks = now, frequency = Stopwatch.Frequency, environmentProcessorCount = Environment.ProcessorCount, processorOverride, processCpuTicks = process.TotalProcessorTime.Ticks, cpuTickFrequency = TimeSpan.TicksPerSecond, threadPoolThreads = ThreadPool.ThreadCount, threadPoolPending = ThreadPool.PendingWorkItemCount, threadPoolCompleted = ThreadPool.CompletedWorkItemCount, minimumWorkers, minimumIo, maximumWorkers, maximumIo, gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2), allocatedBytes = GC.GetTotalAllocatedBytes(precise: false), heapBytes = memory.HeapSizeBytes, fragmentedBytes = memory.FragmentedBytes }) + Environment.NewLine);
        }
        catch { }
    }
    internal static bool Equal(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, string stage) { using var timing = Counted(stage); return left.SequenceEqual(right); }
    internal static byte[] Sha(ReadOnlySpan<byte> bytes, string stage) { using var timing = Counted(stage); return SHA256.HashData(bytes); }
    internal static byte[] JsonBytes<T>(T value, string stage) { using var timing = Counted(stage); return JsonSerializer.SerializeToUtf8Bytes(value); }
    internal static string Schema(JsonSerializerOptions options, Type type, string stage) { using var timing = Counted(stage); return options.GetJsonSchemaAsNode(type).ToJsonString(); }

    private readonly record struct PoolContext(bool Record, long Anchor, string ParentStage, string Phase);
    private static PoolContext PoolObservation()
    {
        try
        {
            var active = Active.Value;
            if (active is { Anchor: > 0 }) return new(true, active.Anchor, active.Stage, active.Phase);
            return File.Exists("/tmp/story65-pool-rent-overlay-20261107/collect.enabled") ? new(true, 0, "outside-coarse", "none") : default;
        }
        catch { return default; }
    }
    private static void PoolCount(PoolContext context, string stage, long units)
    { try { if (context.Record) Add(context.Anchor, stage, context.ParentStage, context.Phase, 0, 0, units, Stopwatch.GetTimestamp()); } catch { } }
    internal static void PoolSelection(bool open, bool absent)
    {
        var context = PoolObservation();
        PoolCount(context, open ? "pool.selection.private" : "pool.selection.ordinary", 0);
        PoolCount(context, absent ? "pool.selection.scope.absent" : open ? "pool.selection.scope.open" : "pool.selection.scope.closed", 0);
        if (open) PoolCount(context, "pool.selection.capacity", 2);
    }
    internal static System.Buffers.ArrayPool<byte> WrapPool(System.Buffers.ArrayPool<byte> inner, string label, int capacity)
        => new ObservedPool(inner, label, capacity);
    private sealed class ObservedPool : System.Buffers.ArrayPool<byte>
    {
        private readonly System.Buffers.ArrayPool<byte> Inner;
        private readonly int Capacity;
        private readonly string RentCount, Requested, Actual, Allocated, Allocating, Outstanding, LifetimePeak, CapacityLabel, Returned, ClearReturned, FailedRent, FailedReturn;
        private long Loans, Peak;
        internal ObservedPool(System.Buffers.ArrayPool<byte> inner, string label, int capacity)
        {
            Inner = inner; Capacity = capacity;
            RentCount = label + ".rent.success"; Requested = label + ".rent.minimum"; Actual = label + ".rent.length";
            Allocated = label + ".rent.allocated"; Allocating = label + ".rent.allocating"; Outstanding = label + ".rent.outstanding";
            LifetimePeak = label + ".rent.lifetime-peak"; CapacityLabel = label + ".capacity"; Returned = label + ".return.success";
            ClearReturned = label + ".return.clear"; FailedRent = label + ".rent.failure"; FailedReturn = label + ".return.failure";
        }
        public override byte[] Rent(int minimumLength)
        {
            byte[] result;
            long before = GC.GetAllocatedBytesForCurrentThread();
            try { result = Inner.Rent(minimumLength); }
            catch { PoolCount(PoolObservation(), FailedRent, 0); throw; }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            long loans = Interlocked.Increment(ref Loans), peak = Volatile.Read(ref Peak);
            while (loans > peak) { long old = Interlocked.CompareExchange(ref Peak, loans, peak); if (old == peak) break; peak = old; }
            var context = PoolObservation();
            PoolCount(context, RentCount, 0); PoolCount(context, Requested, minimumLength); PoolCount(context, Actual, result.Length);
            PoolCount(context, Allocated, allocated); if (allocated > 0) PoolCount(context, Allocating, allocated);
            PoolCount(context, Outstanding, loans); PoolCount(context, LifetimePeak, Volatile.Read(ref Peak)); PoolCount(context, CapacityLabel, Capacity);
            return result;
        }
        public override void Return(byte[] array, bool clearArray = false)
        {
            try { Inner.Return(array, clearArray); }
            catch { PoolCount(PoolObservation(), FailedReturn, 0); throw; }
            Interlocked.Decrement(ref Loans);
            var context = PoolObservation(); PoolCount(context, Returned, 0); PoolCount(context, ClearReturned, clearArray ? 1 : 0);
        }
    }
}
