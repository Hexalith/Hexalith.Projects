namespace Hexalith.Projects.Server.Authorization;

internal sealed record PartyResult(string TenantId, string PrincipalId, string PartyId,
    string Operation, string ActorClass, string Origin, bool Allowed, long SourceVersion,
    string ObservationId, DateTimeOffset ObservedAt);
