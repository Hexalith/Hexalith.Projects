using Hexalith.EventStore.ServiceDefaults.Routing;
using Candidate = Story65.TechnicalFingerprint.Candidate;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class Observation(RouteCatalogImageDigestScope images, Candidate.FingerprintScope? fingerprints = null) : IObservation
{
    public int ImageHits => images.CacheHits;
    public void Dispose() { fingerprints?.Dispose(); images.Dispose(); }
}
