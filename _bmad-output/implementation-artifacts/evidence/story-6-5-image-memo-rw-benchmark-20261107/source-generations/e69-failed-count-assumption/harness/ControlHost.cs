namespace Story65.ImageMemo.Harness;
using System.Buffers;
using System.Reflection;
using Variant = Story65.ImageMemo.Control;
internal sealed class ControlHost : IMemoHost
{
    private readonly Variant.RouteCatalogImageDigestMemo memo = new();
    public string Name => "control";
    public Type MemoType => typeof(Variant.RouteCatalogImageDigestMemo);
    public Type ScopeType => typeof(Variant.RouteCatalogImageDigestScope);
    public Type FreshType => typeof(Variant.RouteCatalogFreshStreamImageDigest);
    public Type TechnicalType => typeof(Variant.TechnicalHttpActionObservation);
    public int Count => memo.Count;
    public int RetainedBytes => memo.RetainedBytes;
    public bool IsClosed => memo.IsClosed;
    public object GateForTesting => MemoType.GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(memo)!;
    public IObservation BeginObservation()
    {
        var scope = Variant.RouteCatalogImageDigestScope.Enter(memo);
        return new Observation(scope, () => scope.CacheHits);
    }
    public IDisposable? SuppressCurrent() => Variant.RouteCatalogImageDigestScope.Suppress();
    public bool TryCompute(Assembly assembly, ReadOnlySpan<byte> current, out string digest, out bool reused)
        => memo.TryCompute(assembly, current, out digest, out reused);
    public string ComputeStream(Assembly assembly, Stream stream, ArrayPool<byte>? pool = null)
        => pool is null ? Variant.RouteCatalogFreshStreamImageDigest.Compute(assembly, stream)
            : Variant.RouteCatalogFreshStreamImageDigest.Compute(assembly, stream, pool);
    public string ComputeDomain(Assembly assembly)
    {
        using FileStream image = File.OpenRead(assembly.Location);
        return Variant.RouteCatalogFreshStreamImageDigest.Compute(assembly, image);
    }
    public string ComputeTechnical(Assembly assembly, ArrayPool<byte>? pool = null)
        => pool is null ? Variant.TechnicalHttpActionObservation.AssemblyIdentity(assembly)
            : Variant.TechnicalHttpActionObservation.AssemblyIdentity(assembly, pool);
    public void Dispose() => memo.Dispose();
}
