namespace Story65.ImageMemo.Harness;
internal interface IObservation : IDisposable
{
    int CacheHits { get; }
    Type ScopeType { get; }
}
