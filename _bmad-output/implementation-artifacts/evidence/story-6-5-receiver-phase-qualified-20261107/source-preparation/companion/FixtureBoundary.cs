using Hexalith.EventStore.Contracts.Routing;

namespace Hexalith.PrivateReceiverFixture;

/// <summary>Supplies an independent prior boundary without admitting remote work.</summary>
internal sealed class FixtureBoundary : IRouteCatalogRemoteBoundary
{
    /// <inheritdoc/>
    public void Enroll() { }
    /// <inheritdoc/>
    public void Retain() { }
    /// <inheritdoc/>
    public void Complete() { }
}
