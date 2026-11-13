using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>One fixed synthetic rejection for the real terminal-refusal producer path.</summary>
internal sealed record RefusalPayload(string Reason) : IRejectionEvent;
