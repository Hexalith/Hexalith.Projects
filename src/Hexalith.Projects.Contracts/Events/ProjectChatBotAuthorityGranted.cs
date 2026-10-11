namespace Hexalith.Projects.Contracts.Events;

/// <summary>Persists an exact project authority issuance with bounded lifetime.</summary>
public sealed record ProjectChatBotAuthorityGranted(
    string TenantId, string ProjectId, string PrincipalId, string Operation,
    string ActorClass, string Origin, string GrantId, DateTimeOffset ExpiresAt,
    string ActorPrincipalId, string CorrelationId, string TaskId,
    string IdempotencyKey, string IdempotencyFingerprint, DateTimeOffset OccurredAt) : IProjectEvent;
