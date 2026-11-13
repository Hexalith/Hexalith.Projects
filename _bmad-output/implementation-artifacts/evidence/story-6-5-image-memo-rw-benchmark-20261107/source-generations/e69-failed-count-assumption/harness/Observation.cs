namespace Story65.ImageMemo.Harness;
internal sealed class Observation(IDisposable scope, Func<int> hits) : IObservation
{
    public int CacheHits => hits();
    public Type ScopeType => scope.GetType();
    public void Dispose() => scope.Dispose();
}
