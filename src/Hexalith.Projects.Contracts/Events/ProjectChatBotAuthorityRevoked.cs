namespace Hexalith.Projects.Contracts.Events;

/// <summary>Persists revocation of the current exact project authority issuance.</summary>
public sealed record ProjectChatBotAuthorityRevoked(
    string TenantId, string ProjectId, string PrincipalId, string Operation,
    string ActorClass, string Origin, string GrantId,
    string ActorPrincipalId, string CorrelationId, string TaskId,
    string IdempotencyKey, string IdempotencyFingerprint, DateTimeOffset OccurredAt) : IProjectEvent;
