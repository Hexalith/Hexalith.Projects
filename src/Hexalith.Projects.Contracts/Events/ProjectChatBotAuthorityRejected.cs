using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Projects.Contracts.Events;

/// <summary>Records a metadata-only refusal of a project authority change.</summary>
public sealed record ProjectChatBotAuthorityRejected(string ReasonCode) : IRejectionEvent;
