namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>One fixed semantic expectation; no runtime catalog/request values are retained in output.</summary>
internal sealed record CaseSpec(string Name, int Entries, int Acquires, int NextCalls, int Releases, int Faults, int Mask, string? ErrorType);
