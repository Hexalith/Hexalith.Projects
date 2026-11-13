namespace Story65.AdvisoryWaveProof;

using System.Collections.Concurrent;

internal sealed class TraceLog : IDisposable
{
    private readonly ConcurrentQueue<TraceEntry> _entries = new();
    private readonly ConcurrentDictionary<string, int> _ordinals = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<int, int> _activeByWave = new();
    private readonly ConcurrentDictionary<int, int> _maximumByWave = new();
    private long _clock;
    private int _active;
    internal Func<TraceEntry, Task>? Before;
    internal Func<TraceEntry, Task>? After;
    internal TraceEntry[] Entries => _entries.ToArray();
    internal int Active => Volatile.Read(ref _active);
    internal IReadOnlyDictionary<int, int> Maxima => _maximumByWave;
    internal int Next(string key) => _ordinals.AddOrUpdate(key, 1, static (_, old) => old + 1);

    internal async Task<T> CallAsync<T>(int wave, string operation, string item, CancellationToken token, Func<Task<T>> action, string argument = "")
    {
        TraceEntry entry = new(wave, operation, item, token, Interlocked.Increment(ref _clock)) { Argument = argument };
        entry.Registration = token.Register(static state => Interlocked.Exchange(ref ((TraceEntry)state!).CancellationObserved, 1), entry);
        _entries.Enqueue(entry);
        Interlocked.Increment(ref _active);
        int active = _activeByWave.AddOrUpdate(wave, 1, static (_, old) => old + 1);
        _maximumByWave.AddOrUpdate(wave, active, (_, old) => Math.Max(old, active));
        try
        {
            if (Before is not null) await Before(entry).ConfigureAwait(false);
            T result = await action().ConfigureAwait(false);
            if (After is not null) await After(entry).ConfigureAwait(false);
            return result;
        }
        catch (Exception exception) { entry.Error = exception.GetType().FullName!; throw; }
        finally
        {
            entry.Ended = Interlocked.Increment(ref _clock);
            _activeByWave.AddOrUpdate(wave, 0, static (_, old) => old - 1);
            Interlocked.Decrement(ref _active);
        }
    }

    internal async Task DrainAsync()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        while (Active != 0) await Task.Delay(5, timeout.Token).ConfigureAwait(false);
    }

    internal void AssertPartialOrders()
    {
        TraceEntry[] entries = Entries;
        foreach (TraceEntry entry in entries)
        {
            Require(entry.Ended > entry.Started, "Dependency did not finish.");
            Require(entry.CanBeCanceled, "Executor lost the dependency cancellation token.");
        }
        foreach (var wave in entries.Where(e => e.Wave is >= 1 and <= 7).GroupBy(e => e.Wave))
            foreach (var later in entries.Where(e => e.Wave > wave.Key && e.Wave <= 7))
                Require(wave.All(earlier => earlier.Ended < later.Started), "Sequential waves overlapped.");
        foreach (int wave in new[] { 4, 6 })
            foreach (var item in entries.Where(e => e.Wave == wave).GroupBy(e => e.Item, StringComparer.Ordinal))
            {
                TraceEntry? auth = item.SingleOrDefault(e => e.Operation == "project-authorize");
                TraceEntry? detail = item.SingleOrDefault(e => e.Operation == "detail");
                TraceEntry? folder = item.SingleOrDefault(e => e.Operation == "anchor-folder");
                if (detail is not null) Require(auth is not null && auth.Ended < detail.Started, "Detail preceded exact project authority.");
                if (folder is not null) Require(detail is not null && detail.Ended < folder.Started, "Anchor owner preceded fresh detail.");
            }
        foreach (var maximum in Maxima) Require(maximum.Value <= 4, "More than four dependency readers.");
    }

    internal static void Require(bool condition, string message,
        [System.Runtime.CompilerServices.CallerFilePath] string file = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    { if (!condition) throw new ProofAssertionFailure(file, line); }
    public void Dispose() { foreach (TraceEntry entry in Entries) entry.Dispose(); }
}
