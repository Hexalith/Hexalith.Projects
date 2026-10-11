namespace Hexalith.Projects.Contracts.Commands;

/// <summary>Revokes the current exact ChatBot project grant issuance.</summary>
public sealed record RevokeProjectChatBotAuthority(
    string PrincipalId, string Operation, string ActorClass, string Origin, string GrantId);
