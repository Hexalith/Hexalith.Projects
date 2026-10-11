namespace Hexalith.Projects.Server.Authorization;

internal sealed record TenantOwnerRequest(string TenantId, string PrincipalId, string Operation,
    string ActorClass, string Origin);
