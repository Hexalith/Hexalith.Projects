using Hexalith.EventStore.Contracts.Routing;
namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Provides a non-null synthetic enclosing caller boundary without granting receiver authority.</summary>
internal sealed class FixtureBoundary : IRouteCatalogRemoteBoundary
{
    public void Enroll() => throw new FixtureFailure("unexpected-caller-enrollment", nameof(FixtureBoundary), 0);
    public void Retain() => throw new FixtureFailure("unexpected-caller-retention", nameof(FixtureBoundary), 0);
    public void Complete() => throw new FixtureFailure("unexpected-caller-completion", nameof(FixtureBoundary), 0);
}
