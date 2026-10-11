namespace Hexalith.Projects.Server.Authorization;

internal sealed record PartyRequest(string TenantId, string PrincipalId, string PartyId,
    string Operation, string ActorClass, string Origin);
