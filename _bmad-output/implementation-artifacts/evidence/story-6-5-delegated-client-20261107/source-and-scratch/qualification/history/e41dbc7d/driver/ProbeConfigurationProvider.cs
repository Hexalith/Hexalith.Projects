namespace Story65.DelegatedClientProof;

using Microsoft.Extensions.Configuration;

internal sealed class ProbeConfigurationProvider(FixtureState state, Dictionary<string, string?> data) : ConfigurationProvider
{
    public override void Load() => Data = data;
    public override bool TryGet(string key, out string? value)
    {
        state.Observe("configuration.read", key);
        state.Trigger(key switch
        {
            "Authentication:WorkloadIssuer:Workload" => "config-workload",
            "Authentication:BoundStreamRead:PrivateKey" => "config-key",
            "EventStore:OwnerQueries:OperationTimeout" => "config-timeout",
            "Authentication:QueryAuthority:GatewayWorkloadId" => "config-gateway",
            _ => "never",
        });
        return base.TryGet(key, out value);
    }
}
