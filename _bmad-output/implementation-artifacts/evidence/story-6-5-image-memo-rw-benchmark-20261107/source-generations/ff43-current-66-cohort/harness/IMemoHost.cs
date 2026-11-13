namespace Story65.ImageMemo.Harness;
using System.Buffers;
using System.Reflection;
internal interface IMemoHost : IDisposable
{
    string Name { get; }
    Type MemoType { get; }
    Type ScopeType { get; }
    Type FreshType { get; }
    Type TechnicalType { get; }
    int Count { get; }
    int RetainedBytes { get; }
    bool IsClosed { get; }
    object GateForTesting { get; }
    IObservation BeginObservation();
    IDisposable? SuppressCurrent();
    bool TryCompute(Assembly assembly, ReadOnlySpan<byte> current, out string digest, out bool reused);
    string ComputeStream(Assembly assembly, Stream stream, ArrayPool<byte>? pool = null);
    string ComputeDomain(Assembly assembly);
    string ComputeTechnical(Assembly assembly, ArrayPool<byte>? pool = null);
}
