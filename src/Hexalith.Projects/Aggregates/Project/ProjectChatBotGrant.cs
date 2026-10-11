namespace Hexalith.Projects.Aggregates.Project;

/// <summary>The last issuance for an exact project authority scope.</summary>
public sealed record ProjectChatBotGrant(string GrantId, DateTimeOffset ExpiresAt, bool Revoked);
