using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;

namespace Hexalith.EventStore.Aspire;

/// <summary>Attaches ordinary Aspire health/dependency annotations after the integration creates real native sidecars.</summary>
/// <param name="builder">The owning AppHost builder.</param>
/// <param name="peers">The exact generated sidecars and registered health checks.</param>
internal sealed class DevelopmentSqliteDiscoveryStartupSubscriber(
    IDistributedApplicationBuilder builder, IReadOnlyList<(ProjectResource Owner, string ResourceName, string HealthKey)> peers)
    : IDistributedApplicationEventingSubscriber
{
    /// <inheritdoc/>
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        _ = eventing.Subscribe<BeforeStartEvent>((startup, _) =>
        {
            ConfigureModel(startup.Model);
            return Task.CompletedTask;
        });
        return Task.CompletedTask;
    }

    /// <summary>Requires one actual executable per selected owner and preserves all existing dependencies.</summary>
    internal void ConfigureModel(DistributedApplicationModel model)
    {
        ExecutableResource? previous = null;
        foreach (var peer in peers)
        {
            var actual = model.Resources.OfType<ExecutableResource>().Where(resource => resource.Name == peer.ResourceName).ToArray();
            if (actual.Length != 1) throw new InvalidOperationException("A selected SQLite sidecar is missing or ambiguous.");
            if (!actual[0].Annotations.OfType<ResourceRelationshipAnnotation>().Any(relation => relation.Type == "Parent" && ReferenceEquals(relation.Resource, peer.Owner))
                || Path.GetFileName(actual[0].Command) is not ("daprd" or "daprd.exe"))
                throw new InvalidOperationException("SQLite startup readiness requires each actual owner's native daprd executable.");
            var resource = builder.CreateResourceBuilder(actual[0]).WithHealthCheck(peer.HealthKey);
            if (previous is not null) resource.WaitFor(builder.CreateResourceBuilder(previous));
            previous = actual[0];
        }
    }
}
