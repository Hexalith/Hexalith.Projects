namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>Test-owned fingerprints of actual Redis values and concurrency versions; never domain payloads.</summary>
/// <param name="Domain">Hashed physical key set and exact value fingerprints, including events and ETags.</param>
/// <param name="Audit">Separate inspection/safe-denial audit fingerprints.</param>
public sealed record PersistedReadObservation(IReadOnlyDictionary<string, string> Domain, IReadOnlyDictionary<string, string> Audit);
