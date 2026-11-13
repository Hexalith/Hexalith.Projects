using System.Diagnostics;
using System.Text.Json;
namespace Hexalith.PrivateOwnerTiming;
/// <summary>Private bounded timing observer; records no request data.</summary>
internal sealed class Story65TimingServer : IDisposable
{
    private static readonly object _gate = new();
    private static readonly AsyncLocal<long> _current = new();
    private static long _sequence;
    private static int _records;
    private readonly long _ordinal = Interlocked.Increment(ref _sequence);
    private readonly long _parent = _current.Value;
    private readonly long _started = Stopwatch.GetTimestamp();
    private readonly string _stage;
    private int _disposed;
    private Story65TimingServer(string stage) { _stage = stage; _current.Value = _ordinal; Write("begin", 0); }
    internal static Story65TimingServer Start(string stage) => new(stage);
    public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0) { Write("end", Stopwatch.GetTimestamp() - _started); _current.Value = _parent; } }
    private void Write(string state, long duration)
    {
        try
        {
            if (Interlocked.Increment(ref _records) > 200000) return;
            lock (_gate) File.AppendAllText("/tmp/story65-owner-stage-overlay-20261107/Hexalith.Folders.Server-" + Environment.ProcessId + ".jsonl",
                JsonSerializer.Serialize(new { component = "Hexalith.Folders.Server", pid = Environment.ProcessId,
                    stage = _stage, state, ordinal = _ordinal, parent = _parent,
                    ticks = Stopwatch.GetTimestamp(), elapsedTicks = duration, frequency = Stopwatch.Frequency }) + Environment.NewLine);
        }
        catch { }
    }
}
