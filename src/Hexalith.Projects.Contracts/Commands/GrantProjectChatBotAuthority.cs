namespace Hexalith.Projects.Contracts.Commands;

/// <summary>Issues an exact, expiring ChatBot project grant after current owner admission.</summary>
public sealed record GrantProjectChatBotAuthority(
    string PrincipalId, string Operation, string ActorClass, string Origin,
    string GrantId, DateTimeOffset ExpiresAt);
