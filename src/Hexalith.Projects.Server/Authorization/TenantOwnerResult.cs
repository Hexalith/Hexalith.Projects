namespace Hexalith.Projects.Server.Authorization;

internal sealed record TenantOwnerResult(string TenantId, string PrincipalId, string Operation,
    string ActorClass, string Origin, bool Allowed, long SourceVersion, string ObservationId,
    DateTimeOffset ObservedAt);
