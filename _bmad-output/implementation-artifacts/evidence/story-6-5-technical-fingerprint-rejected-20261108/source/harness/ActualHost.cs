using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Candidate = Story65.TechnicalFingerprint.Candidate;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class ActualHost : ITechnicalHost
{
    private readonly RouteCatalogImageDigestMemo _images = new();
    public string Name => "actual";
    public IObservation BeginObservation()
    {
        var images = RouteCatalogImageDigestScope.Enter(_images);
        try { return new Observation(images); }
        catch { images.Dispose(); throw; }
    }
    public RouteCatalogTechnicalHttpEndpoint[] Observe(MvcCorpus corpus)
    {
        var pass = new ActualPass(corpus.Services);
        var values = corpus.EligibleEndpoints.Select(e => pass.Describe(e) ?? throw new FixtureFailure("eligible-endpoint-became-null", "ActualHost.cs", 0)).ToArray();
        pass.Complete(); return values;
    }
    public MemoCounters Counters => default;
    public void Dispose() { _images.Dispose(); }
}
