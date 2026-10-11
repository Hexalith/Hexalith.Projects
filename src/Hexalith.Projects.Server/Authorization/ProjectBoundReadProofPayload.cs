namespace Hexalith.Projects.Server.Authorization;

internal sealed record ProjectBoundReadProofPayload(int Version, string Caller, string Tenant,
    string Domain, string AggregateId, long IssuedAt, long ExpiresAt, string Nonce);
