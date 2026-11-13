namespace Story65.InvocationFixture;

using Hexalith.EventStore.Contracts.Routing;

/// <summary>A synthetic prior shared boundary used only to verify original scope restoration.</summary>
internal sealed class FixtureBoundary : IRouteCatalogRemoteBoundary
{
    public void Enroll() { }
    public void Retain() { }
    public void Complete() { }
}
