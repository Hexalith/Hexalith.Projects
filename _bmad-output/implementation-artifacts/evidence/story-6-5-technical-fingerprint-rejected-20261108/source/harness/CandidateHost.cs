using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Candidate = Story65.TechnicalFingerprint.Candidate;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class CandidateHost : ITechnicalHost
{
    private readonly RouteCatalogImageDigestMemo _images = new();
    internal readonly Candidate.FingerprintMemoOwner Fingerprints = new();
    public string Name => "candidate";
    public IObservation BeginObservation()
    {
        var images = RouteCatalogImageDigestScope.Enter(_images);
        try { return new Observation(images, Candidate.FingerprintScope.Enter(Fingerprints)); }
        catch { images.Dispose(); throw; }
    }
    public RouteCatalogTechnicalHttpEndpoint[] Observe(MvcCorpus corpus)
    {
        var pass = new CandidatePass(corpus.Services);
        var values = corpus.EligibleEndpoints.Select(e => pass.Describe(e) ?? throw new FixtureFailure("eligible-endpoint-became-null", "CandidateHost.cs", 0)).ToArray();
        pass.Complete(); return values;
    }
    public MemoCounters Counters { get { var s = Fingerprints.Snapshot(); return new(s.Entries, s.ChargedBytes, s.Hits, s.Stores, s.ConstraintFallbacks, s.UnavailableScopeFallbacks, s.UnsupportedKeyFallbacks); } }
    public void Dispose() { Fingerprints.Dispose(); _images.Dispose(); }
}
