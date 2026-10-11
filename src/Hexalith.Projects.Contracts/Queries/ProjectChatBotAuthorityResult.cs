namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Metadata-only current grant evidence from the owning project stream.</summary>
public sealed record ProjectChatBotAuthorityResult(
    string TenantId, string PrincipalId, string ProjectId, string Operation,
    string ActorClass, string Origin, bool Allowed, string? GrantId,
    long SourceVersion, string ObservationId, DateTimeOffset ObservedAt,
    DateTimeOffset? ExpiresAt);
